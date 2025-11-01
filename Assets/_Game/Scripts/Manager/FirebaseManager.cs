using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


/*
 * ===========================
 * FirebaseManager Install & Usage Summary
 * ===========================
 * 1. Download Firebase Unity SDK:
 *    - Official: https://firebase.google.com/docs/unity/setup
 *    - Horus: https://drive.horusvn.com/s/JwtJZBBjFGDDMTz
 *
 * 2. Import SDK into Unity:
 *    - Import .unitypackage files for Analytics, Crashlytics, Remote Config, Messaging, Core.
 *
 * 3. Add FirebaseManager Script:
 *    - Place this script in your project (e.g., Assets/_Game/Scripts/Manager/).
 *    - Attach to a GameObject in your main scene.
 *
 * 4. Configure Firebase Project Files:
 *    - Download google-services.json (Android) and/or GoogleService-Info.plist (iOS) from Firebase Console.
 *    - Add to Assets folder.
 *
 * 5. Project Settings:
 *    - Scripting Runtime: .NET Framework 4.x
 *    - API Compatibility Level: .NET 4.x
 *
 * 6. Run & Verify:
 *    - Play the scene.
 *    - Check FirebaseManager.Instance.IsInitialized for successful setup.
 *
 * 7. Usage:
 *    - Use FirebaseManager.Instance to log events, set user properties, and handle attribution/conversion callbacks.
 *
 * For troubleshooting and advanced setup, see: https://firebase.google.com/docs/unity/setup
 */
public class FirebaseManager : MonoBehaviour
{
    #region Singleton

    private static FirebaseManager _instance;

    public static FirebaseManager Instance
    {
        get
        {
            return _instance;
        }
    }

    public static bool IsInitialized
    {
        private set;
        get;
    }

    #endregion

    #region Fields

    private DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
    private const float RetryInitDelayTime = 60.0f;

    // Listener
    public Action OnFirebaseInitialized;

    public Firebase.FirebaseApp app = null;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (_instance != null)
        {
            DestroyImmediate(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(InitFirebaseApp());
    }

    #endregion

    #region Initialization

    private IEnumerator InitFirebaseApp()
    {
        while (!IsInitialized)
        {
            Task<DependencyStatus> task = null;
            try
            {
                task = FirebaseApp.CheckAndFixDependenciesAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"FirebaseManager: Exception during initialization: {e}");
                OnInitialized(false);
            }
            if (task != null)
            {
                yield return new WaitUntil(() => task.IsCompleted);
                dependencyStatus = task.Result;
                if (dependencyStatus == Firebase.DependencyStatus.Available)
                {
                    OnInitialized(true);
                }
                else
                {
                    OnInitialized(false);
                    yield return new WaitForSeconds(RetryInitDelayTime);
                }
            }
            else
            {
                yield return new WaitForSeconds(RetryInitDelayTime);
            }
        }
    }

    private void OnInitialized(bool success)
    {
        IsInitialized = success;
        if (success)
        {
            OnFirebaseInitialized?.Invoke();

            InitializeFirebaseCrashlytics();
            InitializeFirebaseAnalytics();
            InitializeFirebaseCloudMessaging();
            InitializeFirebaseRemoteConfig();
        }
    }

    public bool FirebaseAvailable()
    {
        return dependencyStatus == DependencyStatus.Available;
    }

    #endregion

    #region Crashlytics

    /// <summary>
    /// đang chiếm 1.2mb
    /// </summary>
    private void InitializeFirebaseCrashlytics()
    {
        app = Firebase.FirebaseApp.DefaultInstance;
        Crashlytics.ReportUncaughtExceptionsAsFatal = true;
        Crashlytics.IsCrashlyticsCollectionEnabled = true;
        Crashlytics.SetUserId(SystemInfo.deviceUniqueIdentifier);
    }

    #endregion

    #region Analytics

    void InitializeFirebaseAnalytics()
    {
        try
        {
            ReenablePersonalizedAdvertising();
        }
        catch
        {
        }
    }

    /// <summary>
    /// https://firebase.google.com/docs/analytics/configure-data-collection?platform=android#disable-personalization-as-user-property
    /// https://developers.google.com/tag-platform/security/guides/app-consent?consentmode=basic&platform=ios#default-consent
    /// </summary>
    public void ReenablePersonalizedAdvertising()
    {
        if (!FirebaseAvailable())
        {
            return;
        }

        SetUserProperty(FirebaseAnalytics.UserPropertyAllowAdPersonalizationSignals, "true");
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        FirebaseAnalytics.SetUserId(SystemInfo.deviceUniqueIdentifier);
    }

    public void SetUserProperty(string name, string property)
    {
        try
        {
            if (FirebaseAvailable())
            {
                FirebaseAnalytics.SetUserProperty(name, property);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void LogEvent(string name, params Parameter[] parameters)
    {
        try
        {
            if (FirebaseAvailable())
                FirebaseAnalytics.LogEvent(name, parameters);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
    #endregion

    #region CloudMessaging

    private void InitializeFirebaseCloudMessaging()
    {
        Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;
        Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;

        string topic = string.Empty;

        Firebase.Messaging.FirebaseMessaging.SubscribeAsync(topic)
            .ContinueWith(
                task =>
                {
                    LogTaskCompletion(task, "SubscribeAsync");
                });

        Firebase.Messaging.FirebaseMessaging.RequestPermissionAsync()
            .ContinueWith(
                task =>
                {
                    LogTaskCompletion(task, "RequestPermissionAsync");
                });
    }

    protected bool LogTaskCompletion(Task task, string operation)
    {
        bool complete = false;

        if (task.IsCanceled)
        {

        }
        else if (task.IsFaulted)
        {

        }
        else if (task.IsCompleted)
        {
            complete = true;
        }

        return complete;
    }

    public virtual void OnMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e)
    {

    }

    public virtual void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
    {

    }

    #endregion

    #region RemoteConfig

    private void InitializeFirebaseRemoteConfig()
    {
        Dictionary<string, object> defaults = new Dictionary<string, object>();

        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults)
            .ContinueWithOnMainThread(
                previousTask =>
                {
                    FetchDataAsync();
                });
    }

    private Task FetchDataAsync()
    {
        Task fetchTask = FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);
        return fetchTask.ContinueWithOnMainThread(FetchComplete);
    }

    private void FetchComplete(Task fetchTask)
    {
        if (!fetchTask.IsCompleted)
        {
            return;
        }

        var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
        var info = remoteConfig.Info;

        if (info.LastFetchStatus != LastFetchStatus.Success)
        {
            return;
        }

        remoteConfig.ActivateAsync()
            .ContinueWithOnMainThread(
                task =>
                {

                });
    }

    #endregion

    #region AdjustAttribution
#if ADJUST_SDK
    public void OnAttributionChanged(AdjustSdk.AdjustAttribution attribution)
    {
        try
        {
            if (attribution == null)
            {
                return;
            }
            string network = attribution?.Network ?? "Unknown";
            string campaign = attribution?.Campaign ?? "Unknown";
            string trackerToken = attribution?.TrackerToken ?? "Unknown";
            string trackerName = attribution?.TrackerName ?? "Unknown";
            string adgroup = attribution?.Adgroup ?? "Unknown";
            string creative = attribution?.Creative ?? "Unknown";
            string clickLabel = attribution?.ClickLabel ?? "Unknown";
            string costType = attribution?.CostType ?? "Unknown";
            double costAmount = attribution?.CostAmount ?? 0;
            string costCurrency = attribution?.CostCurrency ?? "Unknown";
            string fbInstallReferrer = attribution?.FbInstallReferrer ?? "Unknown";
            string jsonResponse = attribution?.JsonResponse.ToString() ?? "Unknown";

            SetUserProperty("network", network);
            SetUserProperty("campaign", campaign);
            SetUserProperty("adgroup", adgroup);
            SetUserProperty("creative", creative);
            SetUserProperty("clicklabel", clickLabel);


            LogEvent(
                "callback_adjust_atributes",
                new Parameter[]
                {
                new Parameter("trackertoken", trackerToken),
                new Parameter("trackername", trackerName),
                new Parameter("fbInstallReferrer", fbInstallReferrer),
                new Parameter("costType", costType),
                new Parameter("costAmount", costAmount.ToString()),
                new Parameter("costCurrency", costCurrency),
                });

        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
#endif
    #endregion

    #region AppsFlyer
    public void OnConversionDataSuccess(Dictionary<string, object> conversionDataDictionary)
    {
        if (conversionDataDictionary == null)
        {
            return;
        }

        string media_source = conversionDataDictionary.ContainsKey("media_source") && conversionDataDictionary["media_source"] != null ? conversionDataDictionary["media_source"].ToString() : "";
        string af_channel = conversionDataDictionary.ContainsKey("af_channel") && conversionDataDictionary["af_channel"] != null ? conversionDataDictionary["af_channel"].ToString() : "";
        string campaign = conversionDataDictionary.ContainsKey("campaign") && conversionDataDictionary["campaign"] != null ? conversionDataDictionary["campaign"].ToString() : "";

        string adgroup_id = conversionDataDictionary.ContainsKey("adgroup_id") && conversionDataDictionary["adgroup_id"] != null ? conversionDataDictionary["adgroup_id"].ToString() : "";
        string is_first_launch = conversionDataDictionary.ContainsKey("is_first_launch") && conversionDataDictionary["is_first_launch"] != null ? conversionDataDictionary["is_first_launch"].ToString() : "";
        string af_android_url = conversionDataDictionary.ContainsKey("af_android_url") && conversionDataDictionary["af_android_url"] != null ? conversionDataDictionary["af_android_url"].ToString() : "";
        string adset = conversionDataDictionary.ContainsKey("adset") && conversionDataDictionary["adset"] != null ? conversionDataDictionary["adset"].ToString() : "";
        string campaign_id = conversionDataDictionary.ContainsKey("campaign_id") && conversionDataDictionary["campaign_id"] != null ? conversionDataDictionary["campaign_id"].ToString() : "";
        string af_status = conversionDataDictionary.ContainsKey("af_status") && conversionDataDictionary["af_status"] != null ? conversionDataDictionary["af_status"].ToString() : "";
        string adset_id = conversionDataDictionary.ContainsKey("adset_id") && conversionDataDictionary["adset_id"] != null ? conversionDataDictionary["adset_id"].ToString() : "";
        int af_cost_value = -1;
        if (conversionDataDictionary.ContainsKey("af_cost_value") && conversionDataDictionary["af_cost_value"] != null)
        {
            int.TryParse(conversionDataDictionary["af_cost_value"].ToString(), out af_cost_value);
        }
        string http_referrer = conversionDataDictionary.ContainsKey("http_referrer") && conversionDataDictionary["http_referrer"] != null ? conversionDataDictionary["http_referrer"].ToString() : "";
        string af_ad = conversionDataDictionary.ContainsKey("af_ad") && conversionDataDictionary["af_ad"] != null ? conversionDataDictionary["af_ad"].ToString() : "";

        string af_ios_url = conversionDataDictionary.ContainsKey("af_ios_url") && conversionDataDictionary["af_ios_url"] != null ? conversionDataDictionary["af_ios_url"].ToString() : "";
        string deep_link_sub1 = conversionDataDictionary.ContainsKey("deep_link_sub1") && conversionDataDictionary["deep_link_sub1"] != null ? conversionDataDictionary["deep_link_sub1"].ToString() : "";
        string deep_link_value = conversionDataDictionary.ContainsKey("deep_link_value") && conversionDataDictionary["deep_link_value"] != null ? conversionDataDictionary["deep_link_value"].ToString() : "";
        string is_retargeting = conversionDataDictionary.ContainsKey("is_retargeting") && conversionDataDictionary["is_retargeting"] != null ? conversionDataDictionary["is_retargeting"].ToString() : "";

        string orig_cost = conversionDataDictionary.ContainsKey("orig_cost") && conversionDataDictionary["orig_cost"] != null ? conversionDataDictionary["orig_cost"].ToString() : "";
        string af_cost_currency = conversionDataDictionary.ContainsKey("af_cost_currency") && conversionDataDictionary["af_cost_currency"] != null ? conversionDataDictionary["af_cost_currency"].ToString() : "";
        string af_ad_type = conversionDataDictionary.ContainsKey("af_ad_type") && conversionDataDictionary["af_ad_type"] != null ? conversionDataDictionary["af_ad_type"].ToString() : "";

        string first_campaign = "";
        string first_media_source = "";
        string retarget_media_source = "";
        string other_media_source = "";

        bool is_first_launch_value = false;
        if (bool.TryParse(is_first_launch, out is_first_launch_value))
        {
            if (is_first_launch_value)
            {
                first_campaign = $"{campaign}";
                first_media_source = $"{media_source}";
            }
            else
            {
                bool is_retargeting_value = false;
                if (bool.TryParse(is_retargeting, out is_retargeting_value))
                {
                    if (is_retargeting_value)
                    {
                        retarget_media_source = $"{media_source}";
                    }
                    else
                    {
                        other_media_source = $"{media_source}";
                    }
                }
            }
        }

        SetUserProperty("media_source", $"{media_source}");
        SetUserProperty("af_channel", af_channel);
        SetUserProperty("campaign", campaign);

        if (!string.IsNullOrEmpty(first_campaign))
        {
            SetUserProperty("first_campaign", first_campaign);
        }

        if (!string.IsNullOrEmpty(first_media_source))
        {
            SetUserProperty("first_media_source", first_media_source);
        }
        if (!string.IsNullOrEmpty(retarget_media_source))
        {
            SetUserProperty("retarget_media_source", retarget_media_source);
        }
        if (!string.IsNullOrEmpty(other_media_source))
        {
            SetUserProperty("other_media_source", other_media_source);
        }

        LogEvent(
            "af_conversion_data",
            new Parameter[]
            {
                new Parameter("adgroup_id", adgroup_id),
                new Parameter("is_first_launch", is_first_launch),
                new Parameter("af_android_url", af_android_url),

                new Parameter("adset", adset),
                new Parameter("af_channel", af_channel),
                new Parameter("campaign_id", campaign_id),

                new Parameter("media_source", media_source),
                new Parameter("af_status", af_status),
                new Parameter("adset_id", adset_id),

                new Parameter("af_cost_value", af_cost_value),
                new Parameter("campaign", campaign),
                new Parameter("http_referrer", http_referrer),
                new Parameter("af_ad", af_ad),

                new Parameter("af_ios_url", af_ios_url),
                new Parameter("deep_link_sub1", deep_link_sub1),
                new Parameter("deep_link_value", deep_link_value),
                new Parameter("is_retargeting", is_retargeting),

                new Parameter("orig_cost", orig_cost),
                new Parameter("af_cost_currency", af_cost_currency),
                new Parameter("af_ad_type", af_ad_type)
            });
    }
    #endregion

    #region Ad
    // Reusable parameter arrays for frequently called methods
    private readonly Parameter[] _adImpressionParams = new Parameter[7];
    public void OnAdImpression(AdImpressionData impressionData, int adType)
    {
        try
        {
            // Reuse the same array instead of creating a new one each time
            _adImpressionParams[0] = new Parameter("ad_platform", impressionData.AdPlatform);
            _adImpressionParams[1] = new Parameter("ad_source", impressionData.AdSource);
            _adImpressionParams[2] = new Parameter("ad_unit_name", impressionData.AdUnitName);
            _adImpressionParams[3] = new Parameter("ad_format", impressionData.AdFormat);
            _adImpressionParams[4] = new Parameter("currency", "USD");
            _adImpressionParams[5] = new Parameter("value", impressionData.Value);
            _adImpressionParams[6] = new Parameter("precision", impressionData.Precision);

            LogEvent("ad_impression", _adImpressionParams);
            LogEvent("custom_ad_impression", _adImpressionParams);
            for (int i = 1; i <= 5; i++)
            {
                LogEvent($"custom_ad_impression_{i}", _adImpressionParams);
            }
        }
        catch { }
    }
    #endregion
}