#if IS_SDK
using System;
using Unity.Services.LevelPlay;
using UnityEngine;


/// <summary>
/// ISAdManager integrates IronSource ads for Unity.
/// Installation guide:
/// 1. Window->Package Manager in Unity. Look for “Ads Mediation” and click the install button.
/// 2. Set your IronSource app key below.
/// 3. Attach ISAdManager to a GameObject in your initial scene.
/// 4. Call Initialized() at startup to initialize the SDK and ad types.
/// 5. Use provided methods to load and show Interstitial and Rewarded ads.
/// </summary>
public class ISAdManager : AdManager
{
#if UNITY_IOS
    private const string InterstitialAdUnitId = "";
    private const string RewardedAdUnitId = "";
    private const string AppOpenAdUnitId = "";
    private const string BannerAdUnitId = "";
    private const string InterstitialRewardAdUnitId = "";
    private const string AdBreakInterstitialAdUnitId = "";
    private const string AppKey = "";
#else
    private const string InterstitialAdUnitId = "";
    private const string RewardedAdUnitId = "";
    private const string AppOpenAdUnitId = "";
    private const string BannerAdUnitId = "";
    private const string InterstitialRewardAdUnitId = "";
    private const string AdBreakInterstitialAdUnitId = "";
    private const string AppKey = "";
#endif


    private LevelPlayRewardedAd rewardedAd;
    private LevelPlayInterstitialAd interstitialAd;

    protected override void Initialized()
    {
        // Register OnInitFailed and OnInitSuccess listeners
        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
        // SDK init
        LevelPlay.Init(AppKey, "UserId");
    }

    private void SdkInitializationFailedEvent(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay SDK Initialization Failed: {error.ToString()}");
    }

    private void SdkInitializationCompletedEvent(LevelPlayConfiguration configuration)
    {
        InitializeInterstitialAds();
        InitializeRewardedAds();

        LevelPlay.OnImpressionDataReady += TrackAdRevenue;

        OnAdInitialized?.Invoke();
    }

    #region Interstitial Ad Methods

    protected override void InitializeInterstitialAds()
    {
        //Create InterstitialAd instance
        interstitialAd = new LevelPlayInterstitialAd(InterstitialAdUnitId);

        //Subscribe InterstitialAd events
        interstitialAd.OnAdLoaded += OnInterstitialLoadedEvent;
        interstitialAd.OnAdLoadFailed += OnInterstitialFailedEvent;
        interstitialAd.OnAdDisplayed += OnInterstitialDisplayedEvent;
        interstitialAd.OnAdDisplayFailed += OnInterstitialFailedToDisplayEvent;
        //interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
        interstitialAd.OnAdClosed += OnInterstitialDismissedEvent;
        //interstitialAd.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;
    }

    private void OnInterstitialLoadedEvent(LevelPlayAdInfo adInfo)
    {
        if (OnInterstitialLoaded != null)
        {
            OnInterstitialLoaded();
        }

        // Reset retry attempt
        interstitialRetryAttempt = 0;
    }

    private void OnInterstitialFailedEvent(LevelPlayAdError error)
    {
        interstitialRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, interstitialRetryAttempt));

        Invoke("LoadInterstitial", (float)retryDelay);

        if (OnInterstitialFailedToLoad != null)
        {
            OnInterstitialFailedToLoad();
        }
    }

    private void OnInterstitialDisplayedEvent(LevelPlayAdInfo info)
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

    private void OnInterstitialFailedToDisplayEvent(LevelPlayAdInfo info, LevelPlayAdError error)
    {
        LoadInterstitial();

        if (OnInterstitialShowFailed != null)
        {
            OnInterstitialShowFailed();
        }
    }

    private void OnInterstitialDismissedEvent(LevelPlayAdInfo adInfo)
    {
#if UNITY_IOS
        AudioListener.pause = false;
        Time.timeScale = 1;
#endif
        LoadInterstitial();

        if (OnInterstitialClosed != null)
        {
            OnInterstitialClosed(adInfo != null ? (double)adInfo.Revenue : 0);
        }
    }


    public override bool InterstitialIsLoaded()
    {
        return interstitialAd.IsAdReady();
    }

    protected override void LoadInterstitial()
    {
        if (!InterstitialIsLoaded())
        {
            interstitialAd.LoadAd();
        }
    }

    protected override void ShowInterstitial()
    {
        if (InterstitialIsLoaded())
        {
            interstitialAd.ShowAd();
        }
    }

    #endregion

    #region Rewarded Ad Methods

    protected override void InitializeRewardedAds()
    {
        rewardedAd = new LevelPlayRewardedAd(RewardedAdUnitId);
        rewardedAd.OnAdLoaded += OnRewardedAdLoadedEvent;
        rewardedAd.OnAdLoadFailed += OnRewardedAdFailedEvent;
        rewardedAd.OnAdDisplayed += OnRewardedAdDisplayedEvent;
        rewardedAd.OnAdDisplayFailed += OnRewardedAdFailedToDisplayEvent;
        rewardedAd.OnAdRewarded += OnRewardedAdReceivedRewardEvent;
        rewardedAd.OnAdClicked += OnRewardedAdClickedEvent;
        rewardedAd.OnAdClosed += OnRewardedAdDismissedEvent;
        rewardedAd.OnAdInfoChanged += OnRewardedAdInfoChanged;
    }

    private void OnRewardedAdLoadedEvent(LevelPlayAdInfo adInfo)
    {
        rewardedRetryAttempt = 0;
        OnVideoLoaded?.Invoke();
    }

    private void OnRewardedAdFailedEvent(LevelPlayAdError error)
    {
        rewardedRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, rewardedRetryAttempt));
        Invoke(nameof(LoadRewardedAd), (float)retryDelay);

    }

    private void OnRewardedAdFailedToDisplayEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        LoadRewardedAd();

        if (OnVideoFailedToLoad != null)
        {
            OnVideoFailedToLoad();
        }
    }

    private void OnRewardedAdDisplayedEvent(LevelPlayAdInfo adInfo)
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

    private void OnRewardedAdClickedEvent(LevelPlayAdInfo placement)
    {

    }

    private void OnRewardedAdDismissedEvent(LevelPlayAdInfo adInfo)
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

    private void OnRewardedAdReceivedRewardEvent(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        if (OnVideoRewarded != null)
        {
            OnVideoRewarded(adInfo != null ? (double)adInfo.Revenue : 0);
        }
    }

    private void OnRewardedAdInfoChanged(LevelPlayAdInfo info)
    {

    }

    public override bool RewardVideoIsLoaded()
    {
        return rewardedAd.IsAdReady();
    }

    protected override void LoadRewardedAd()
    {
        rewardedAd.LoadAd();
    }

    protected override void ShowRewardedVideo()
    {
        if (RewardVideoIsLoaded())
        {
            rewardedAd.ShowAd();
        }
    }

    #endregion

    #region Ad Impression Data
    private void TrackAdRevenue(LevelPlayImpressionData impressionData)
    {
        if (impressionData != null)
        {
#if UNITY_ANDROID
            if (!storeChecker || impressionData.Revenue == 1)
            {
                return;
            }
#endif

            TrackAdRevenue(ConvertAdInfoToImpressionData(impressionData), 0);
        }
    }

    private AdImpressionData ConvertAdInfoToImpressionData(LevelPlayImpressionData adInfo, string adEventName = "")
    {
        return new AdImpressionData(
            adPlatform: "ironSource", // Fixed value for ironSource
            adSource: adInfo.AdNetwork ?? "", // Network name
            adUnitName: adInfo.MediationAdUnitId ?? "", // Ad unit ID
            adFormat: adInfo.AdFormat ?? "", // Ad format
            currency: "USD", // Default currency
            value: (double)adInfo.Revenue, // Revenue value
            precision: adInfo.Precision ?? "", // Revenue precision
            adEventName: adEventName // Event name, can be set by caller
        );
    }
    #endregion
}

#endif