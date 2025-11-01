using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class AdsSdkDefineTool : EditorWindow
{
    // Defines & package identifiers
    private const string DefineISSDK = "IS_SDK";
    private const string DefineLevelPlay = "LEVELPLAY_DEPENDENCIES_INSTALLED";
    private const string AdsMediationPackage = "com.unity.services.levelplay";

    private const string DefineMAXSDK = "MAX_SDK";
    private const string MaxPackage = "com.applovin.mediation.ads";

    private const string DefineADJUST = "ADJUST_SDK";
    private const string AdjustKey = "com.adjust.sdk";
    private const string AdjustUrl = "https://github.com/adjust/unity_sdk.git?path=Assets/Adjust";

    private const string DefineAPPSFLYER = "APPSFLYER_SDK";
    private const string AppsFlyerKey = "appsflyer-unity-plugin";
    private const string AppsFlyerUrl = "https://github.com/AppsFlyerSDK/appsflyer-unity-plugin.git#upm";

    // UI state
    private bool newISSDK;
    private bool newMAXSDK;
    private bool newADJUST;
    private bool newAPPSFLYER;

    // runtime status
    private static bool s_isBusy = false;
    private static string s_status = "Idle";
    private static float s_statusStartTime = 0f;
    private static bool s_cancelRequested = false;

    private string levelPlayStatus = "Unknown";
    private string maxStatus = "Unknown";
    private string adjustStatus = "Unknown";
    private string appsflyerStatus = "Unknown";

    // timeout in seconds for package manager requests
    private const float DEFAULT_TIMEOUT = 90f;

    // in-memory logs shown in the window (no file)
    private static List<string> s_logs = new List<string>();
    private Vector2 _logScroll;
    private const int MAX_LOG_LINES = 800;

    private const string PackagesManifestPath = "Packages/manifest.json";

    [MenuItem("Tools/Ads SDK Define Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<AdsSdkDefineTool>("Ads SDK Defines");
        window.LoadDefines();
        window.RefreshPackageStatuses();
    }

    private void LoadDefines()
    {
        var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);
        var arr = defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        newISSDK = arr.Contains(DefineISSDK);
        newMAXSDK = arr.Contains(DefineMAXSDK);
        newADJUST = arr.Contains(DefineADJUST);
        newAPPSFLYER = arr.Contains(DefineAPPSFLYER);

        // ensure mutual exclusivity in UI only between IS and MAX
        if (newISSDK && newMAXSDK) newMAXSDK = false;
    }

    private void OnGUI()
    {
        GUILayout.Label("Ads SDK Define Manager", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Build Target Group:", GUILayout.Width(140));
        EditorGUILayout.LabelField(EditorUserBuildSettings.selectedBuildTargetGroup.ToString());
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6);

        // Mutual exclusive toggles: IS <-> MAX
        newISSDK = EditorGUILayout.ToggleLeft($"IS_SDK (LevelPlay) [{levelPlayStatus}]", newISSDK);
        if (newISSDK && newMAXSDK) newMAXSDK = false; // enforce mutual exclusive

        newMAXSDK = EditorGUILayout.ToggleLeft($"MAX_SDK (AppLovin MAX) [{maxStatus}]", newMAXSDK);
        if (newMAXSDK && newISSDK) newISSDK = false; // enforce mutual exclusive

        GUILayout.Space(6);

        // Mutual exclusive toggles: ADJUST <-> APPSFLYER
        newADJUST = EditorGUILayout.ToggleLeft($"ADJUST_SDK [{adjustStatus}]", newADJUST);
        if (newADJUST && newAPPSFLYER) newAPPSFLYER = false; // enforce mutual exclusive

        newAPPSFLYER = EditorGUILayout.ToggleLeft($"APPSFLYER_SDK [{appsflyerStatus}]", newAPPSFLYER);
        if (newAPPSFLYER && newADJUST) newADJUST = false; // enforce mutual exclusive

        GUILayout.Space(10);

        // Disable Apply while busy
        EditorGUI.BeginDisabledGroup(s_isBusy);
        if (GUILayout.Button("Apply"))
        {
            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            ApplyDefines(buildTarget, newISSDK, newMAXSDK, newADJUST, newAPPSFLYER);
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Status") && !s_isBusy)
        {
            RefreshPackageStatuses();
            LoadDefines();
        }
        if (GUILayout.Button("Reload Defines") && !s_isBusy)
        {
            LoadDefines();
            AddLog("[Action] Reloaded defines from PlayerSettings.");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // If busy, show Cancel button and elapsed time
        if (s_isBusy)
        {
            if (GUILayout.Button("Cancel Operation"))
            {
                s_cancelRequested = true;
                AddLog("[User] Cancel requested.");
            }
        }

        string elapsed = s_isBusy ? $"{(Time.realtimeSinceStartup - s_statusStartTime):0.0}s" : "0.0s";
        EditorGUILayout.LabelField("Status: " + s_status + (s_isBusy ? $" (busy {elapsed})" : ""));

        GUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Flow:\n• Bật: cài package -> sau khi cài xong mới thêm define.\n• Tắt: gỡ define ngay -> sau đó gỡ package (nếu có).\nOnly one SDK can be enabled at a time for LevelPlay vs MAX; Adjust/AppsFlyer are independent.",
            MessageType.Info);

        GUILayout.Space(10);

        // Logs area (in-memory only)
        EditorGUILayout.LabelField("Tool Logs (latest at top):", EditorStyles.boldLabel);
        GUILayout.BeginVertical("box");
        _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(220));
        for (int i = s_logs.Count - 1; i >= 0; i--)
        {
            EditorGUILayout.LabelField(s_logs[i], EditorStyles.label);
        }
        EditorGUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Window Logs"))
        {
            s_logs.Clear();
        }
        if (GUILayout.Button("Copy Logs"))
        {
            GUIUtility.systemCopyBuffer = string.Join(Environment.NewLine, s_logs);
            AddLog("[Action] Logs copied to clipboard.");
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Refresh package installed/not-installed statuses (updates UI labels).
    /// Uses PackageManager list and falls back to manifest.json checks.
    /// </summary>
    private void RefreshPackageStatuses()
    {
        StartProcessing("Checking installed packages...");
        var listReq = Client.List(false);
        PollRequest(listReq, (r) =>
        {
            if (r.Status == StatusCode.Success)
            {
                var installed = ((ListRequest)r).Result;
                bool lp = installed.Any(p => p.name == AdsMediationPackage) || IsPackageInManifest(AdsMediationPackage);
                bool mx = installed.Any(p => p.name == MaxPackage) || IsPackageInManifest(MaxPackage);
                bool adj = installed.Any(p => p.name == AdjustKey) || IsPackageInManifest(AdjustKey);
                bool af = installed.Any(p => p.name == AppsFlyerKey) || IsPackageInManifest(AppsFlyerKey);

                levelPlayStatus = lp ? "Installed" : "Not installed";
                maxStatus = mx ? "Installed" : "Not installed";
                adjustStatus = adj ? "Installed" : "Not installed";
                appsflyerStatus = af ? "Installed" : "Not installed";

                AddLog($"Package status refreshed: LevelPlay={levelPlayStatus}, MAX={maxStatus}, Adjust={adjustStatus}, AppsFlyer={appsflyerStatus}");
            }
            else
            {
                levelPlayStatus = "Unknown";
                maxStatus = "Unknown";
                adjustStatus = "Unknown";
                appsflyerStatus = "Unknown";
                AddLog($"Failed to list packages: {r.Error?.message}");
            }
            StopProcessing("Idle");
            Repaint();
        },
        onTimeout: () =>
        {
            levelPlayStatus = "Unknown (timeout)";
            maxStatus = "Unknown (timeout)";
            adjustStatus = "Unknown (timeout)";
            appsflyerStatus = "Unknown (timeout)";
            AddLog("Refresh package list timed out.");
            StopProcessing("Idle (timeout)");
            Repaint();
        },
        timeoutSeconds: DEFAULT_TIMEOUT);
    }

    /// <summary>
    /// Apply toggles: handle IS, MAX, Adjust, AppsFlyer.
    /// </summary>
    private void ApplyDefines(BuildTargetGroup buildTarget, bool enableIS, bool enableMAX, bool enableADJUST, bool enableAPPSFLYER)
    {
        if (s_isBusy)
        {
            AddLog("Already processing. Wait until current operation finishes.");
            return;
        }

        // LEVELPLAY (IS)
        if (enableIS)
        {
            StartProcessing("Checking LevelPlay package...");
            var lr = Client.List(false);
            PollRequest(lr, (req) =>
            {
                bool installed = req.Status == StatusCode.Success && ((ListRequest)req).Result.Any(p => p.name == AdsMediationPackage);
                installed = installed || IsPackageInManifest(AdsMediationPackage);

                if (installed)
                {
                    AddLog("LevelPlay already installed -> adding defines.");
                    AddDefines(buildTarget, addIS: true, addMAX: false, addADJUST: false, addAPPSFLYER: false);
                    StopProcessing("Idle");
                    RefreshPackageStatuses();
                }
                else
                {
                    AddLog("Installing LevelPlay package...");
                    var addReq = Client.Add(AdsMediationPackage);
                    PollRequest(addReq, (r2) =>
                    {
                        if (r2.Status == StatusCode.Success)
                        {
                            AddLog("LevelPlay installed -> adding defines.");
                            AddDefines(buildTarget, addIS: true, addMAX: false, addADJUST: false, addAPPSFLYER: false);
                        }
                        else
                        {
                            AddLog("Failed to install LevelPlay: " + r2.Error?.message);
                        }
                        StopProcessing("Idle");
                        RefreshPackageStatuses();
                    },
                    onTimeout: () =>
                    {
                        AddLog("Installing LevelPlay timed out.");
                        StopProcessing("Idle (timeout)");
                        RefreshPackageStatuses();
                    },
                    timeoutSeconds: DEFAULT_TIMEOUT);
                }
            },
            onTimeout: () =>
            {
                AddLog("Checking LevelPlay package timed out.");
                StopProcessing("Idle (timeout)");
                RefreshPackageStatuses();
            },
            timeoutSeconds: DEFAULT_TIMEOUT);
        }
        else
        {
            // Disable IS: remove defines immediately, then remove package if present
            RemoveDefines(buildTarget, removeIS: true, removeMAX: false, removeADJUST: false, removeAPPSFLYER: false);

            StartProcessing("Checking LevelPlay package...");
            var lr = Client.List(false);
            PollRequest(lr, (req) =>
            {
                bool installed = req.Status == StatusCode.Success && ((ListRequest)req).Result.Any(p => p.name == AdsMediationPackage);
                installed = installed || IsPackageInManifest(AdsMediationPackage);
                if (installed)
                {
                    AddLog("Removing LevelPlay package...");
                    var remReq = Client.Remove(AdsMediationPackage);
                    PollRequest(remReq, (r2) =>
                    {
                        if (r2.Status == StatusCode.Success)
                            AddLog("LevelPlay removed.");
                        else
                            AddLog("Failed to remove LevelPlay: " + r2.Error?.message);
                        StopProcessing("Idle");
                        RefreshPackageStatuses();
                    },
                    onTimeout: () =>
                    {
                        AddLog("Removing LevelPlay timed out.");
                        StopProcessing("Idle (timeout)");
                        RefreshPackageStatuses();
                    },
                    timeoutSeconds: DEFAULT_TIMEOUT);
                }
                else
                {
                    AddLog("LevelPlay not installed.");
                    StopProcessing("Idle");
                    RefreshPackageStatuses();
                }
            },
            onTimeout: () =>
            {
                AddLog("Checking LevelPlay package timed out.");
                StopProcessing("Idle (timeout)");
                RefreshPackageStatuses();
            },
            timeoutSeconds: DEFAULT_TIMEOUT);
        }

        // MAX (AppLovin)
        if (enableMAX)
        {
            StartProcessing("Checking MAX package...");
            var lr2 = Client.List(false);
            PollRequest(lr2, (req) =>
            {
                bool installed = req.Status == StatusCode.Success && ((ListRequest)req).Result.Any(p => p.name == MaxPackage);
                installed = installed || IsPackageInManifest(MaxPackage);
                if (installed)
                {
                    AddLog("MAX already installed -> adding defines.");
                    AddDefines(buildTarget, addIS: false, addMAX: true, addADJUST: false, addAPPSFLYER: false);
                    StopProcessing("Idle");
                    RefreshPackageStatuses();
                }
                else
                {
                    AddLog("Installing MAX package...");
                    // If AppLovin registry missing, Client.Add may fail or hang; user manifest should include scopedRegistry for AppLovin
                    var addReq = Client.Add(MaxPackage);
                    PollRequest(addReq, (r2) =>
                    {
                        if (r2.Status == StatusCode.Success)
                        {
                            AddLog("MAX installed -> adding defines.");
                            AddDefines(buildTarget, addIS: false, addMAX: true, addADJUST: false, addAPPSFLYER: false);
                        }
                        else
                        {
                            AddLog("Failed to install MAX: " + r2.Error?.message);
                        }
                        StopProcessing("Idle");
                        RefreshPackageStatuses();
                    },
                    onTimeout: () =>
                    {
                        AddLog("Installing MAX timed out.");
                        StopProcessing("Idle (timeout)");
                        RefreshPackageStatuses();
                    },
                    timeoutSeconds: DEFAULT_TIMEOUT);
                }
            },
            onTimeout: () =>
            {
                AddLog("Checking MAX package timed out.");
                StopProcessing("Idle (timeout)");
                RefreshPackageStatuses();
            },
            timeoutSeconds: DEFAULT_TIMEOUT);
        }
        else
        {
            RemoveDefines(buildTarget, removeIS: false, removeMAX: true, removeADJUST: false, removeAPPSFLYER: false);

            StartProcessing("Checking MAX package...");
            var lr2 = Client.List(false);
            PollRequest(lr2, (req) =>
            {
                bool installed = req.Status == StatusCode.Success && ((ListRequest)req).Result.Any(p => p.name == MaxPackage);
                installed = installed || IsPackageInManifest(MaxPackage);
                if (installed)
                {
                    AddLog("Removing MAX package...");
                    var remReq = Client.Remove(MaxPackage);
                    PollRequest(remReq, (r2) =>
                    {
                        if (r2.Status == StatusCode.Success)
                            AddLog("MAX removed.");
                        else
                            AddLog("Failed to remove MAX: " + r2.Error?.message);
                        StopProcessing("Idle");
                        RefreshPackageStatuses();
                    },
                    onTimeout: () =>
                    {
                        AddLog("Removing MAX timed out.");
                        StopProcessing("Idle (timeout)");
                        RefreshPackageStatuses();
                    },
                    timeoutSeconds: DEFAULT_TIMEOUT);
                }
                else
                {
                    AddLog("MAX not installed.");
                    StopProcessing("Idle");
                    RefreshPackageStatuses();
                }
            },
            onTimeout: () =>
            {
                AddLog("Checking MAX package timed out.");
                StopProcessing("Idle (timeout)");
                RefreshPackageStatuses();
            },
            timeoutSeconds: DEFAULT_TIMEOUT);
        }

        // ADJUST
        if (enableADJUST)
        {
            StartProcessing("Checking Adjust package...");
            var lr3 = Client.List(false);
            PollRequest(lr3, (req) =>
            {
                bool installed = req.Status == StatusCode.Success && ((ListRequest)req).Result.Any(p => p.name == AdjustKey);
                installed = installed || IsPackageInManifest(AdjustKey);
                if (installed)
                {
                    AddLog("Adjust already installed -> adding define.");
                    AddDefines(buildTarget, addIS: false, addMAX: false, addADJUST: true, addAPPSFLYER: false);
                    StopProcessing("Idle");
                    RefreshPackageStatuses();
                }
                else
                {
                    AddLog("Installing Adjust package (git)...");
                    var addReq = Client.Add(AdjustUrl);
                    PollRequest(addReq, (r2) =>
                    {
                        if (r2.Status == StatusCode.Success)
                        {
                            AddLog("Adjust installed -> adding define.");
                            AddDefines(buildTarget, addIS: false, addMAX: false, addADJUST: true, addAPPSFLYER: false);
                        }
                        else
                        {
                            AddLog("Failed to install Adjust: " + r2.Error?.message);
                        }
                        StopProcessing("Idle");
                        RefreshPackageStatuses();
                    },
                    onTimeout: () =>
                    {
                        AddLog("Installing Adjust timed out.");
                        StopProcessing("Idle (timeout)");
                        RefreshPackageStatuses();
                    },
                    timeoutSeconds: DEFAULT_TIMEOUT);
                }
            },
            onTimeout: () =>
            {
                AddLog("Checking Adjust package timed out.");
                StopProcessing("Idle (timeout)");
                RefreshPackageStatuses();
            },
            timeoutSeconds: DEFAULT_TIMEOUT);
        }
        else
        {
            RemoveDefines(buildTarget, removeIS: false, removeMAX: false, removeADJUST: true, removeAPPSFLYER: false);

            StartProcessing("Checking Adjust package...");
            var lr3 = Client.List(false);
            PollRequest(lr3, (req) =>
            {
                bool installed = req.Status == StatusCode.Success && ((ListRequest)req).Result.Any(p => p.name == AdjustKey);
                installed = installed || IsPackageInManifest(AdjustKey);
                if (installed)
                {
                    AddLog("Removing Adjust package...");
                    var remReq = Client.Remove(AdjustKey);
                    PollRequest(remReq, (r2) =>
                    {
                        if (r2.Status == StatusCode.Success)
                            AddLog("Adjust removed.");
                        else
                            AddLog("Failed to remove Adjust: " + r2.Error?.message);
                        StopProcessing("Idle");
                        RefreshPackageStatuses();
                    },
                    onTimeout: () =>
                    {
                        AddLog("Removing Adjust timed out.");
                        StopProcessing("Idle (timeout)");
                        RefreshPackageStatuses();
                    },
                    timeoutSeconds: DEFAULT_TIMEOUT);
                }
                else
                {
                    AddLog("Adjust not installed.");
                    StopProcessing("Idle");
                    RefreshPackageStatuses();
                }
            },
            onTimeout: () =>
            {
                AddLog("Checking Adjust package timed out.");
                StopProcessing("Idle (timeout)");
                RefreshPackageStatuses();
            },
            timeoutSeconds: DEFAULT_TIMEOUT);
        }

        // APPSFLYER
        if (enableAPPSFLYER)
        {
            StartProcessing("Checking AppsFlyer package...");
            var lr4 = Client.List(false);
            PollRequest(lr4, (req) =>
            {
                bool installed = req.Status == StatusCode.Success && ((ListRequest)req).Result.Any(p => p.name == AppsFlyerKey);
                installed = installed || IsPackageInManifest(AppsFlyerKey);
                if (installed)
                {
                    AddLog("AppsFlyer already installed -> adding define.");
                    AddDefines(buildTarget, addIS: false, addMAX: false, addADJUST: false, addAPPSFLYER: true);
                    StopProcessing("Idle");
                    RefreshPackageStatuses();
                }
                else
                {
                    AddLog("Installing AppsFlyer package (git)...");
                    var addReq = Client.Add(AppsFlyerUrl);
                    PollRequest(addReq, (r2) =>
                    {
                        if (r2.Status == StatusCode.Success)
                        {
                            AddLog("AppsFlyer installed -> adding define.");
                            AddDefines(buildTarget, addIS: false, addMAX: false, addADJUST: false, addAPPSFLYER: true);
                        }
                        else
                        {
                            AddLog("Failed to install AppsFlyer: " + r2.Error?.message);
                        }
                        StopProcessing("Idle");
                        RefreshPackageStatuses();
                    },
                    onTimeout: () =>
                    {
                        AddLog("Installing AppsFlyer timed out.");
                        StopProcessing("Idle (timeout)");
                        RefreshPackageStatuses();
                    },
                    timeoutSeconds: DEFAULT_TIMEOUT);
                }
            },
            onTimeout: () =>
            {
                AddLog("Checking AppsFlyer package timed out.");
                StopProcessing("Idle (timeout)");
                RefreshPackageStatuses();
            },
            timeoutSeconds: DEFAULT_TIMEOUT);
        }
        else
        {
            RemoveDefines(buildTarget, removeIS: false, removeMAX: false, removeADJUST: false, removeAPPSFLYER: true);

            StartProcessing("Checking AppsFlyer package...");
            var lr4 = Client.List(false);
            PollRequest(lr4, (req) =>
            {
                bool installed = req.Status == StatusCode.Success && ((ListRequest)req).Result.Any(p => p.name == AppsFlyerKey);
                installed = installed || IsPackageInManifest(AppsFlyerKey);
                if (installed)
                {
                    AddLog("Removing AppsFlyer package...");
                    var remReq = Client.Remove(AppsFlyerKey);
                    PollRequest(remReq, (r2) =>
                    {
                        if (r2.Status == StatusCode.Success)
                            AddLog("AppsFlyer removed.");
                        else
                            AddLog("Failed to remove AppsFlyer: " + r2.Error?.message);
                        StopProcessing("Idle");
                        RefreshPackageStatuses();
                    },
                    onTimeout: () =>
                    {
                        AddLog("Removing AppsFlyer timed out.");
                        StopProcessing("Idle (timeout)");
                        RefreshPackageStatuses();
                    },
                    timeoutSeconds: DEFAULT_TIMEOUT);
                }
                else
                {
                    AddLog("AppsFlyer not installed.");
                    StopProcessing("Idle");
                    RefreshPackageStatuses();
                }
            },
            onTimeout: () =>
            {
                AddLog("Checking AppsFlyer package timed out.");
                StopProcessing("Idle (timeout)");
                RefreshPackageStatuses();
            },
            timeoutSeconds: DEFAULT_TIMEOUT);
        }

        // Ensure UI reflects current define choices
        LoadDefines();
    }

    // Poll any PackageManager Request until completion, then call onDone(Request)
    // If timeout occurs or user cancels, onTimeout is invoked.
    private static void PollRequest(Request request, Action<Request> onDone, Action onTimeout = null, float timeoutSeconds = DEFAULT_TIMEOUT)
    {
        if (request == null)
        {
            AddLog("[Error] Null request to poll.");
            onTimeout?.Invoke();
            return;
        }

        EditorApplication.CallbackFunction poll = null;
        float start = Time.realtimeSinceStartup;

        poll = () =>
        {
            // user requested cancel
            if (s_cancelRequested)
            {
                EditorApplication.update -= poll;
                s_cancelRequested = false;
                AddLog("[User] Operation cancelled by user.");
                onTimeout?.Invoke();
                StopProcessing("Cancelled");
                return;
            }

            // timeout
            if (!request.IsCompleted && (Time.realtimeSinceStartup - start) > timeoutSeconds)
            {
                EditorApplication.update -= poll;
                AddLog($"[Error] Package manager request timed out after {timeoutSeconds} seconds.");
                onTimeout?.Invoke();
                StopProcessing("Timeout");
                return;
            }

            if (!request.IsCompleted) return;

            EditorApplication.update -= poll;
            onDone?.Invoke(request);
        };

        EditorApplication.update += poll;
    }

    /// <summary>
    /// Add defines selectively.
    /// </summary>
    private static void AddDefines(BuildTargetGroup buildTarget, bool addIS, bool addMAX, bool addADJUST, bool addAPPSFLYER)
    {
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);
        var arr = defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        if (addIS)
        {
            if (!arr.Contains(DefineISSDK)) arr.Add(DefineISSDK);
            if (!arr.Contains(DefineLevelPlay)) arr.Add(DefineLevelPlay);
        }

        if (addMAX)
        {
            if (!arr.Contains(DefineMAXSDK)) arr.Add(DefineMAXSDK);
        }

        if (addADJUST)
        {
            if (!arr.Contains(DefineADJUST)) arr.Add(DefineADJUST);
        }

        if (addAPPSFLYER)
        {
            if (!arr.Contains(DefineAPPSFLYER)) arr.Add(DefineAPPSFLYER);
        }

        var joined = string.Join(";", arr);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, joined);
        AddLog("[Action] Updated defines (add): " + joined);
    }

    /// <summary>
    /// Remove defines selectively.
    /// </summary>
    private static void RemoveDefines(BuildTargetGroup buildTarget, bool removeIS, bool removeMAX, bool removeADJUST, bool removeAPPSFLYER)
    {
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);
        var arr = defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        bool changed = false;
        if (removeIS)
        {
            changed |= arr.RemoveAll(s => s == DefineISSDK || s == DefineLevelPlay) > 0;
        }
        if (removeMAX)
        {
            changed |= arr.RemoveAll(s => s == DefineMAXSDK) > 0;
        }
        if (removeADJUST)
        {
            changed |= arr.RemoveAll(s => s == DefineADJUST) > 0;
        }
        if (removeAPPSFLYER)
        {
            changed |= arr.RemoveAll(s => s == DefineAPPSFLYER) > 0;
        }

        if (changed)
        {
            var joined = string.Join(";", arr);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, joined);
            AddLog("[Action] Updated defines (remove): " + joined);
        }
        else
        {
            AddLog("[Action] No defines changed on remove.");
        }
    }

    /// <summary>
    /// Check manifest.json for a dependency key presence (safe string check).
    /// </summary>
    private static bool IsPackageInManifest(string packageKey)
    {
        try
        {
            string manifestFullPath = Path.Combine(Application.dataPath, "..", PackagesManifestPath);
            if (!File.Exists(manifestFullPath)) return false;
            string json = File.ReadAllText(manifestFullPath);
            return json.Contains($"\"{packageKey}\"");
        }
        catch (Exception ex)
        {
            AddLog("[Manifest] Error reading manifest: " + ex.Message);
            return false;
        }
    }

    #region Helpers: status / processing / logs
    private static void StartProcessing(string status)
    {
        s_isBusy = true;
        s_status = status;
        s_statusStartTime = Time.realtimeSinceStartup;
        try { EditorUtility.DisplayProgressBar("Ads SDK Tool", status, 0.5f); } catch { }
        RepaintAllWindows();
        AddLog("[Processing] " + status);
    }

    private static void StopProcessing(string status)
    {
        s_isBusy = false;
        s_status = status;
        try { EditorUtility.ClearProgressBar(); } catch { }
        RepaintAllWindows();
        AddLog("[Processing done] " + status);
    }

    private static void AddLog(string message)
    {
        // timestamp + push to list
        string time = DateTime.Now.ToString("HH:mm:ss");
        string entry = $"[{time}] {message}";
        s_logs.Add(entry);

        // keep list short
        if (s_logs.Count > MAX_LOG_LINES) s_logs.RemoveRange(0, s_logs.Count - MAX_LOG_LINES);

        // also print to Console for convenience
        Debug.Log("[AdsSdkDefineTool] " + message);

        // repaint windows so user sees new log
        RepaintAllWindows();
    }

    private static void RepaintAllWindows()
    {
        foreach (var w in Resources.FindObjectsOfTypeAll<AdsSdkDefineTool>())
        {
            w.Repaint();
        }
    }
    #endregion
}
