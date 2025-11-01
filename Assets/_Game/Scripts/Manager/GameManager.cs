#if APPSFLYER_SDK
using System.Collections.Generic;
#endif
using UnityEngine;

/// <summary>
/// GameManager is a singleton MonoBehaviour responsible for coordinating initialization and event handling
/// between FirebaseManager, AdjustManager, and AppsFlyerManager. It listens for key events such as
/// Firebase initialization, Adjust attribution changes, deferred deeplink reception, and AppsFlyer conversion data.
/// The class ensures that attribution and conversion data are forwarded to FirebaseManager only after
/// Firebase is initialized, caching them if necessary until initialization completes.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    private static GameManager _instance;
    public static GameManager Instance
    {
        get { return _instance; }
    }
    #endregion

    #region Fields

#if ADJUST_SDK || APPSFLYER_SDK
    private bool IsFirebaseInitialized;
#endif
#if ADJUST_SDK
    private AdjustSdk.AdjustAttribution attribution = null;
#endif

#if APPSFLYER_SDK
    private Dictionary<string, object> conversionData = null;
#endif

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
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.OnFirebaseInitialized += OnFirebaseInitialized;
        }

#if ADJUST_SDK
        if (AdjustManager.Instance != null)
        {
            AdjustManager.Instance.OnAttributionChanged += OnAttributionChanged;
            AdjustManager.Instance.OnDeferredDeeplinkReceived += OnDeferredDeeplinkReceived;
        }
#endif

#if APPSFLYER_SDK
        if (AppsFlyerManager.Instance != null)
        {
            AppsFlyerManager.Instance.OnConversionDataSuccess += OnConversionDataSuccess;
        }
#endif

        if (AdManager.Instance != null)
        {
            AdManager.Instance.OnAdInitialized += OnAdInitialized;
            AdManager.Instance.OnAdImpression += FirebaseManager.Instance.OnAdImpression;
        }
    }

    private void OnDestroy()
    {
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.OnFirebaseInitialized -= OnFirebaseInitialized;
        }

#if ADJUST_SDK
        if (AdjustManager.Instance != null)
        {
            AdjustManager.Instance.OnAttributionChanged -= OnAttributionChanged;
            AdjustManager.Instance.OnDeferredDeeplinkReceived -= OnDeferredDeeplinkReceived;
        }
#endif

#if APPSFLYER_SDK
        if (AppsFlyerManager.Instance != null)
        {
            AppsFlyerManager.Instance.OnConversionDataSuccess -= OnConversionDataSuccess;
        }
#endif

        if (AdManager.Instance != null)
        {
            AdManager.Instance.OnAdInitialized -= OnAdInitialized;
            AdManager.Instance.OnAdImpression -= FirebaseManager.Instance.OnAdImpression;
        }
    }
    #endregion

    #region Event Handlers

    private void OnFirebaseInitialized()
    {
#if ADJUST_SDK || APPSFLYER_SDK
        IsFirebaseInitialized = true;
#endif

        if (FirebaseManager.Instance != null)
        {
#if ADJUST_SDK
            FirebaseManager.Instance.OnAttributionChanged(this.attribution);
#endif
#if APPSFLYER_SDK
            FirebaseManager.Instance.OnConversionDataSuccess(this.conversionData);
#endif
        }
    }

#if ADJUST_SDK
    private void OnAttributionChanged(AdjustSdk.AdjustAttribution attribution)
    {
        if (IsFirebaseInitialized)
        {
            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.OnAttributionChanged(attribution);
            }
        }
        else
        {
            this.attribution = attribution;
        }
    }

    private void OnDeferredDeeplinkReceived(string deeplinkURL)
    {
        // Handle deferred deeplink if needed
    }
#endif

#if APPSFLYER_SDK
    private void OnConversionDataSuccess(Dictionary<string, object> conversionData)
    {
        if (IsFirebaseInitialized)
        {
            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.OnConversionDataSuccess(conversionData);
            }
        }
        else
        {
            this.conversionData = conversionData;
        }
    }
#endif

    private void OnAdInitialized()
    {
#if ADJUST_SDK
        if (AdjustManager.Instance != null)
        {
            AdjustManager.Instance.InitAdjustApp();
        }
#endif

#if APPSFLYER_SDK
        if (AppsFlyerManager.Instance != null)
        {
            AppsFlyerManager.Instance.InitAppsflyerApp();
        }
#endif
    }

    #endregion
}
