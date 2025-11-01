#if MAX_SDK
using System;

/// <summary>
/// MaxAdManager integrates AppLovin MAX ads for Unity. 
/// Installation guide:
/// 1. Download the SDK package from https://drive.horusvn.com/s/xM8sgB4xWDjspbZ.
/// 2. Import the package into your Unity project (__Assets > Import Package > Custom Package__).
/// 3. Set your AppLovin ad unit IDs in this script.
/// 4. Attach MaxAdManager to a GameObject in your initial scene.
/// 5. Call Initialized() at startup to initialize the SDK and ad types.
/// 6. Use provided methods to load and show Interstitial and Rewarded ads.
/// </summary>
public class MaxAdManager : AdManager
{

#if UNITY_IOS
    private const string InterstitialAdUnitId = "";
    private const string RewardedAdUnitId = "";
    private const string AppOpenAdUnitId = "";
    private const string BannerAdUnitId = "";
    private const string InterstitialRewardAdUnitId = "";
    private const string AdBreakInterstitialAdUnitId = "";
#else
    private const string InterstitialAdUnitId = "";
    private const string RewardedAdUnitId = "";
    private const string AppOpenAdUnitId = "";
    private const string BannerAdUnitId = "";
    private const string InterstitialRewardAdUnitId = "";
    private const string AdBreakInterstitialAdUnitId = "";
#endif

    protected override void Initialized()
    {
        MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdk.SdkConfiguration sdkConfiguration) =>
        {
            InitializeInterstitialAds();
            InitializeRewardedAds();
            InitializeBannerAds();

            OnAdInitialized?.Invoke();
        };

        MaxSdk.InitializeSdk();
    }

    #region Banner Ad Methods
    public override void InitializeBannerAds()
    {
        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerLoaded;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerLoadFailed;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;
        MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerClicked;
    }

    private void OnBannerClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        if (OnBannerAdClicked != null)
        {
            OnBannerAdClicked();
        }
    }

    private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        TrackAdRevenue(adInfo, 3);
    }

    private void OnBannerLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo info)
    {
        isBannerLoaded = false;
    }

    private void OnBannerLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        isBannerLoaded = true;
    }

    public override bool BannerAdIsLoaded()
    {
        return isBannerLoaded;
    }

    protected override void LoadBannerAd()
    {
        MaxSdk.CreateBanner(BannerAdUnitId, MaxSdkBase.BannerPosition.BottomCenter);
        MaxSdk.StartBannerAutoRefresh(BannerAdUnitId);
    }

    protected override void ShowBannerAd()
    {
        MaxSdk.ShowBanner(BannerAdUnitId);
    }

    public override void HideBannerAd()
    {
        MaxSdk.HideBanner(BannerAdUnitId);
    }

    #endregion

    #region Interstitial Ad Methods

    protected override void InitializeInterstitialAds()
    {
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialFailedToDisplayEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialDismissedEvent;
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaidEvent;
    }

    private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        if (OnInterstitialLoaded != null)
        {
            OnInterstitialLoaded();
        }

        // Reset retry attempt
        interstitialRetryAttempt = 0;
    }

    private void OnInterstitialFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo info)
    {
        interstitialRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, interstitialRetryAttempt));

        Invoke("LoadInterstitial", (float)retryDelay);

        if (OnInterstitialFailedToLoad != null)
        {
            OnInterstitialFailedToLoad();
        }
    }

    private void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_IOS
        AudioListener.pause = true;
        Time.timeScale = 0;
#endif
        if (OnInterstitialOpened != null)
        {
            OnInterstitialOpened();
        }
    }

    private void OnInterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        LoadInterstitial();

        if (OnInterstitialShowFailed != null)
        {
            OnInterstitialShowFailed();
        }
    }

    private void OnInterstitialDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_IOS
        AudioListener.pause = false;
        Time.timeScale = 1;
#endif
        LoadInterstitial();

        if (OnInterstitialClosed != null)
        {
            OnInterstitialClosed(adInfo != null ? adInfo.Revenue : 0);
        }
    }

    private void OnInterstitialRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        TrackAdRevenue(adInfo, 2);

        if (OnInterstitialShowSucceeded != null)
        {
            OnInterstitialShowSucceeded();
        }
    }

    public override bool InterstitialIsLoaded()
    {
        return MaxSdk.IsInterstitialReady(InterstitialAdUnitId);
    }

    protected override void LoadInterstitial()
    {
        if (!InterstitialIsLoaded())
        {
            MaxSdk.LoadInterstitial(InterstitialAdUnitId);
        }
    }
    protected override void ShowInterstitial()
    {
        if (InterstitialIsLoaded())
        {
            MaxSdk.ShowInterstitial(InterstitialAdUnitId);
        }
    }
    #endregion

    #region Rewarded Ad Methods

    protected override void InitializeRewardedAds()
    {
        // Attach callbacks
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdDismissedEvent;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;

        // Load the first rewarded ad after 7 seconds for performance
        Invoke("LoadRewardedAd", 7.0f);
    }

    private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        //Debug.Log("Rewarded ad loaded");
        if (OnVideoLoaded != null)
        {
            OnVideoLoaded();
        }
        // Reset retry attempt
        rewardedRetryAttempt = 0;
    }

    private void OnRewardedAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        rewardedRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, rewardedRetryAttempt));

        Invoke("LoadRewardedAd", (float)retryDelay);
    }

    private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {

        LoadRewardedAd();

        if (OnVideoFailedToLoad != null)
        {
            OnVideoFailedToLoad();
        }
    }

    private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_IOS
        AudioListener.pause = true;
        Time.timeScale = 0;
#endif

        if (OnRewardedAdOpening != null)
        {
            OnRewardedAdOpening();
        }
    }

    private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {

    }

    private void OnRewardedAdDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_IOS
        AudioListener.pause = false;
        Time.timeScale = 1;
#endif
        LoadRewardedAd();

        if (OnVideoClosed != null)
        {
            OnVideoClosed();
        }
    }

    private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
        if (OnVideoRewarded != null)
        {
            OnVideoRewarded(adInfo != null ? adInfo.Revenue : 0);
        }
    }

    internal void ReardedVideoOnAdRewardedEvent(object p)
    {
        if (OnVideoRewarded != null)
        {
            OnVideoRewarded(0);
        }
    }

    private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        TrackAdRevenue(adInfo, 1);
    }


    public override bool RewardVideoIsLoaded()
    {
        return MaxSdk.IsRewardedAdReady(RewardedAdUnitId);
    }

    protected override void LoadRewardedAd()
    {
        if (!RewardVideoIsLoaded())
        {
            MaxSdk.LoadRewardedAd(RewardedAdUnitId);
        }
    }

    protected override void ShowRewardedVideo()
    {
        if (RewardVideoIsLoaded())
        {
            MaxSdk.ShowRewardedAd(RewardedAdUnitId);
        }
    }

    #endregion

    #region Adimpression Tracking
    private void TrackAdRevenue(MaxSdkBase.AdInfo impressionData, int adType)
    {
        if (impressionData != null)
        {
#if UNITY_ANDROID
            if (!storeChecker || impressionData.Revenue == 1)
            {
                return;
            }
#endif

            TrackAdRevenue(ConvertAdInfoToImpressionData(impressionData), adType);
        }
    }


    private AdImpressionData ConvertAdInfoToImpressionData(MaxSdkBase.AdInfo adInfo, string adEventName = "")
    {
        return new AdImpressionData(
            adPlatform: "AppLovin", // Fixed value for AppLovin
            adSource: adInfo.NetworkName ?? "", // Network name
            adUnitName: adInfo.AdUnitIdentifier ?? "", // Ad unit ID
            adFormat: adInfo.AdFormat ?? "", // Ad format
            currency: "USD", // Default currency
            value: adInfo.Revenue, // Revenue value
            precision: adInfo.RevenuePrecision ?? "", // Revenue precision
            adEventName: adEventName // Event name, can be set by caller
        );
    }
    #endregion
}
#endif