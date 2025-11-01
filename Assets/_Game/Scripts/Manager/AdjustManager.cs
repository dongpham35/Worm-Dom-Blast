#if ADJUST_SDK
using AdjustSdk;
using System;
using UnityEngine;

/// <summary>
/// AdjustManager
/// 
/// # Adjust SDK Installation Guide (Unity Package Manager)
///
/// ## Steps to Install Adjust SDK
/// 1. Open Unity Editor.
/// 2. Go to Window -> Package Manager.
/// 3. In the Package Manager window, click the + button (top left) and selectAdd package from git URL...
/// 4. Enter the following URL:
///    https://github.com/adjust/unity_sdk.git?path=Assets/Adjust
/// 5. Click Add to start the installation.
///
/// ## Notes
/// - After installation, the Adjust SDK will be available in your project under <b>Assets/Adjust</b>.
/// - Make sure to set your Adjust App Token in your code for each platform:
///   - Android: "z2uzavexo45c"
///   - iOS: "hf8c122hzlds"
///
/// ## Reference
/// - Official Adjust Unity SDK documentation: https://github.com/adjust/unity_sdk
///
/// This guide helps you quickly integrate the Adjust SDK using the Unity Package Manager.
/// </summary>
public class AdjustManager : MonoBehaviour
{
    #region Singleton

    private static AdjustManager _instance;

    public static AdjustManager Instance
    {
        get
        {
            return _instance;
        }
    }
    #endregion

    #region Fields
    [SerializeField]
    private AdjustEnvironment adjustEnvironment;
    [SerializeField]
    private AdjustLogLevel adjustLogLevel;

    // Thay bằng App Token của bạn
#if UNITY_ANDROID
    private readonly string APP_TOKEN = "";
#elif UNITY_IOS
    private readonly string APP_TOKEN = "";
#else
    private readonly string APP_TOKEN = "";
#endif

    // Listener Actions for data handling
    public Action<AdjustAttribution> OnAttributionChanged;
    public Action<string> OnDeferredDeeplinkReceived;
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
    #endregion

    #region Initialization

    public void InitAdjustApp()
    {
        // Dùng AdjustEnvironment.Sandbox cho test, Production cho app thật
        AdjustConfig adjustConfig = new AdjustConfig(APP_TOKEN, adjustEnvironment);

        // Bật log để debug
        adjustConfig.LogLevel = adjustLogLevel;

        adjustConfig.IsSendingInBackgroundEnabled = true; // Gửi dữ liệu khi app chạy ngầm
        // Cho phép các callback attribution
        adjustConfig.AttributionChangedDelegate = AttributionChangedCallback;
        adjustConfig.DeferredDeeplinkDelegate = DeferredDeeplinkCallback;
        Adjust.InitSdk(adjustConfig);

        AdjustThirdPartySharing adjustThirdPartySharing = new AdjustThirdPartySharing(true);
        Adjust.TrackThirdPartySharing(adjustThirdPartySharing);
        Adjust.Enable();

        Adjust.GetAttribution(AttributionChangedCallback);
    }

    public void AttributionChangedCallback(AdjustAttribution attributionData)
    {
        // Trigger the attribution changed event
        OnAttributionChanged?.Invoke(attributionData);
    }

    private void DeferredDeeplinkCallback(string deeplinkURL)
    {
        // Trigger the deferred deeplink received event
        OnDeferredDeeplinkReceived?.Invoke(deeplinkURL);
    }
    #endregion

    public void TrackEvent(AdjustEvent adjustEvent)
    {
        // Gửi event đến Adjust
        Adjust.TrackEvent(adjustEvent);
    }

}

#endif