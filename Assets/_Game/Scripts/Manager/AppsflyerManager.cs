#if APPSFLYER_SDK
using AppsFlyerSDK;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AppsFlyerManager
///
/// # AppsFlyer SDK Installation Guide (Unity Package Manager)
///
/// ## Steps to Install AppsFlyer SDK
/// 1. Open Unity Editor.
/// 2. Go to __Window > Package Manager__.
/// 3. In the Package Manager window, click the __+__ button (top left) and select __Add package from git URL...__.
/// 4. Enter the following URL:
///    https://github.com/AppsFlyerSDK/appsflyer-unity-plugin.git#upm
/// 5. Click __Add__ to start the installation.
///
/// ## Notes
/// - After installation, the AppsFlyer SDK will be available in your project under <b>Assets/AppsFlyer</b>.
/// - Make sure to set your AppsFlyer Dev Key and iOS App ID in your code:
///   - Dev Key: "c38bttTkhaZ54NPREk34V8"
///   - iOS App ID: "id6746404278"
///
/// ## Reference
/// - Official AppsFlyer Unity SDK documentation: https://github.com/AppsFlyerSDK/appsflyer-unity-plugin
/// - AppsFlyer developer docs: https://dev.appsflyer.com/hc/docs/conversion-data-unity
///
/// This guide helps you quickly integrate the AppsFlyer SDK using the Unity Package Manager.
/// </summary>
public class AppsFlyerManager : MonoBehaviour, IAppsFlyerConversionData, IAppsFlyerPurchaseValidation, IAppsFlyerPurchaseRevenueDataSource, IAppsFlyerPurchaseRevenueDataSourceStoreKit2
{
    #region Singleton
    private static AppsFlyerManager _instance;
    public static AppsFlyerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AppsFlyerManager>();
            }
            return _instance;
        }
    }
    #endregion

    #region Fields
    /// <summary>
    /// AppsFlyer's Dev Key, which is accessible from the AppsFlyer dashboard.
    /// </summary>
    private string devKey = "c38bttTkhaZ54NPREk34V8";

    /// <summary>
    /// 	Your iTunes Application ID. (If your app is not for iOS the leave field empty)
    /// </summary>
    private string appID = "id6746404278";

    /// <summary>
    /// Set this to true to view the debug logs. (for development only!)
    /// </summary>
    private bool isDebug = false;

    /// <summary>
    /// Event triggered when conversion data is successfully received.
    /// </summary>
    public Action<Dictionary<string, object>> OnConversionDataSuccess;
    #endregion

    #region Unity Lifecycle Methods

    void Awake()
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

    // Start is called before the first frame update
    public void InitAppsflyerApp()
    {
        //Debug.Log("start");
        AppsFlyer.OnRequestResponse += AppsFlyerOnRequestResponse;

        // 1. Initialize AppsFlyer SDK    
        AppsFlyer.initSDK(devKey, appID, this);
        AppsFlyer.setIsDebug(isDebug);

        // 2. Initialize Purchase Connector
        AppsFlyerPurchaseConnector.init(this, Store.GOOGLE);
        ConfigurePurchaseConnector();

        // 4. Build and start observing
        AppsFlyerPurchaseConnector.build();
        AppsFlyerPurchaseConnector.startObservingTransactions();

        // App Tracking Transparency for iOS
#if UNITY_IOS && !UNITY_EDITOR
         AppsFlyer.waitForATTUserAuthorizationWithTimeoutInterval(60);
#endif

        // 5. Start AppsFlyer SDK
        AppsFlyer.startSDK();

#if UNITY_IOS && !UNITY_EDITOR
        //TODO for uninstall: https://github.com/AppsFlyerSDK/appsflyer-unity-plugin/blob/master/docs/UninstallMeasurement.md
        //StartCoroutine(RequestAuthorization());
#endif
    }

    #endregion

    #region Private Helper Methods

    private void ConfigurePurchaseConnector()
    {
        // Set sandbox mode for testing
        AppsFlyerPurchaseConnector.setIsSandbox(false);

        // Configure StoreKit version (iOS only) - SK1 is the default
        AppsFlyerPurchaseConnector.setStoreKitVersion(StoreKitVersion.SK2);

        // Enable automatic logging for subscriptions and in-app purchases
        AppsFlyerPurchaseConnector.setAutoLogPurchaseRevenue(
            AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsAutoRenewableSubscriptions,
            AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsInAppPurchases
        );

        // Enable purchase validation callbacks
        AppsFlyerPurchaseConnector.setPurchaseRevenueValidationListeners(true);

        // Set data sources for additional parameters (iOS) - SK1
        AppsFlyerPurchaseConnector.setPurchaseRevenueDataSource(this);
        // Set data sources for additional parameters (iOS) - SK2
        AppsFlyerPurchaseConnector.setPurchaseRevenueDataSourceStoreKit2(this);
    }

    void AppsFlyerOnRequestResponse(object sender, EventArgs e)
    {
        //var args = e as AppsFlyerRequestEventArgs;
        //AppsFlyer.AFLog("AppsFlyerOnRequestResponse", " status code " + args.statusCode);
    }

    #endregion

    #region AppsFlyerManager Methods

    /// <summary>
    /// {
    /// "adgroup_id": null,
    /// "af_adset_id": "174363054204",
    /// "af_ad_type": "AppDeepLink",
    /// "retargeting_conversion_type": "re-attribution",
    /// "orig_cost": "0.0",
    /// "network": "Display",
    /// "is_first_launch": true,
    /// "af_click_lookback": "30d",
    /// "af_cpi": null,
    /// "iscache": true,
    /// "external_account_id": 3668039466,
    /// "click_time": "2025-02-25 03:44:41.526",
    /// "adset": null,
    /// "match_type": "srn",
    /// "af_channel": "ACE_Display",
    /// "af_viewthrough_lookback": "1d",
    /// "campaign_id": "22063556391",
    /// "lat": "0",
    /// "install_time": "2025-02-25 04:06:11.537",
    /// "af_c_id": "22063556391",
    /// "agency": null,
    /// "media_source": "googleadwords_int",
    /// "ad_event_id": "CjdFQUlhSVFvYkNoTUlxOUxTcXZUZGl3TVYyRjRQQWgyanF4cGhFQUVZQVNBQUVnTDRPUERfQndFEhMIwdfCnvndiwMVqFYVCB1dowTNGOiQlf89IAIop87cmFI",
    /// "af_siteid": null,
    /// "af_status": "Non-organic",
    /// "af_sub1": null,
    /// "gclid": null,
    /// "referrer_gclid": null,
    /// "cost_cents_USD": "0",
    /// "af_ad_id": "",
    /// "af_reengagement_window": "30d",
    /// "af_sub5": null,
    /// "af_sub4": null,
    /// "af_adset": "Test1-gameplay",
    /// "click-timestamp": "1740455081526",
    /// "af_sub3": null,
    /// "af_sub2": null,
    /// "adset_id": null,
    /// "gbraid": null,
    /// "http_referrer": null,
    /// "campaign": "CM_Global_RET_Non-IAP_28\/12",
    /// "af_ad": "",
    /// "adgroup": null
    /// }
    /// </summary>
    /// <param name="conversionData"></param>
    public void onConversionDataSuccess(string conversionData)
    {
        if (string.IsNullOrEmpty(conversionData))
        {
            return;
        }

        Dictionary<string, object> conversionDataDictionary = null;
        try
        {
            conversionDataDictionary = AppsFlyer.CallbackStringToDictionary(conversionData);
        }
        catch (Exception)
        {
            // Optionally log or handle parsing error
        }

        OnConversionDataSuccess?.Invoke(conversionDataDictionary);
    }

    public void onConversionDataFail(string error)
    {
        //AppsFlyer.AFLog("didReceiveConversionDataWithError", error);
    }

    public void onAppOpenAttribution(string attributionData)
    {
        //AppsFlyer.AFLog("onAppOpenAttribution", attributionData);
        //Dictionary<string, object> attributionDataDictionary = AppsFlyer.CallbackStringToDictionary(attributionData);
        // add direct deeplink logic here
    }

    public void onAppOpenAttributionFailure(string error)
    {
        //AppsFlyer.AFLog("onAppOpenAttributionFailure", error);
    }

    public void didReceivePurchaseRevenueError(string error)
    {
        //Debug.LogError("Purchase validation error: " + error);
    }

    public Dictionary<string, object> PurchaseRevenueAdditionalParametersForProducts(HashSet<object> products, HashSet<object> transactions)
    {
        return new Dictionary<string, object>
        {
            ["implementation_type"] = "separate_repository",
            ["additional_param"] = "value",
            ["product_count"] = products.Count,
            ["transaction_count"] = transactions.Count
        };
    }

    public Dictionary<string, object> PurchaseRevenueAdditionalParametersStoreKit2ForProducts(HashSet<object> products, HashSet<object> transactions)
    {
        // Note: StoreKit 2 support depends on Purchase Connector version
        return new Dictionary<string, object>
        {
            ["implementation_type"] = "separate_repository_sk2",
            ["additional_param"] = "sk2_value",
            ["product_count"] = products.Count,
            ["transaction_count"] = transactions.Count
        };
    }

    public void didReceivePurchaseRevenueValidationInfo(string validationInfo)
    {

    }

    #endregion
}
#endif