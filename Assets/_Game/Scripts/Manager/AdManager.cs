using System;
using UnityEngine;

public class AdManager : MonoBehaviour
{
    #region Singleton

    protected static AdManager _instance;

    public static AdManager Instance
    {
        get
        {
            return _instance;
        }
    }
    #endregion

    #region Fields
    public enum BannerType : byte
    {
        None
    }
    public BannerType bannerType;

    public enum InterstitialType : byte
    {
        None
    }
    public InterstitialType interstitialType;

    public enum RewardedVideoType : byte
    {
        None
    }

    public RewardedVideoType rewardedVideoType;



    public Action OnBannerAdClicked;

    public Action OnVideoRequest;
    public Action OnVideoLoaded;
    public Action OnVideoFailedToLoad;
    public Action<double> OnVideoRewarded;
    public Action OnRewardedVideoAdShowFailedEvent;
    public Action OnRewardedAdOpening;
    public Action OnVideoClosed;

    public Action OnInterstitialRequest;
    public Action OnInterstitialLoaded;
    public Action OnInterstitialFailedToLoad;
    public Action<double> OnInterstitialClosed;
    public Action OnInterstitialShowSucceeded;
    public Action OnInterstitialShowFailed;
    public Action OnInterstitialOpened;

    protected bool isRewardedVideoAdShowFailedEvent = false;
    protected bool isVideoLoaded = false;
    protected bool isVideoRewarded = false;
    protected bool isInterRewarded = false;

    protected bool isBannerLoaded = false;
    protected bool isBannerLoading = false;

    protected int interstitialRetryAttempt;
    protected int rewardedRetryAttempt;

    protected bool storeChecker = true;
    public bool GetStoreChecker => storeChecker;

    public Action<AdImpressionData, int> OnAdImpression;
    public Action OnAdInitialized;
    #endregion

    #region Unity Lifecycle

    protected void Awake()
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

#if UNITY_ANDROID
        storeChecker = InstallerChecker();
#endif
    }

    protected void Start()
    {
        Initialized();
    }

    #endregion

    #region Init Methods
    protected virtual void Initialized()
    {

    }
    #endregion


    #region Banner Ad Methods
    public virtual void InitializeBannerAds()
    {

    }

    public virtual bool BannerAdIsLoaded()
    {
        return true;
    }

    protected virtual void LoadBannerAd()
    {

    }

    public void ShowBannerAd(BannerType bannerType)
    {
        this.bannerType = bannerType;
        ShowBannerAd();
    }

    protected virtual void ShowBannerAd()
    {

    }

    public virtual void HideBannerAd()
    {

    }

    public void RequestBannerAd()
    {
        LoadBannerAd();
    }
    #endregion


    #region Interstitial Ad Methods

    protected virtual void InitializeInterstitialAds()
    {

    }

    public virtual bool InterstitialIsLoaded()
    {
        return true;
    }

    protected virtual void LoadInterstitial()
    {

    }

    public void ShowInterstitial(InterstitialType interstitialType)
    {
        this.interstitialType = interstitialType;
        ShowInterstitial();
    }

    protected virtual void ShowInterstitial()
    {

    }

    public void RequestInterstitial()
    {
        // Load the first RewardedAd
        LoadInterstitial();
    }
    #endregion

    #region Rewarded Ad Methods
    protected virtual void InitializeRewardedAds()
    {

    }

    public virtual bool RewardVideoIsLoaded()
    {
        return true;
    }

    protected virtual void LoadRewardedAd()
    {

    }

    public void ShowRewardedVideo(RewardedVideoType rewardedVideoType)
    {
        this.rewardedVideoType = rewardedVideoType;

        ShowRewardedVideo();
    }

    protected virtual void ShowRewardedVideo()
    {

    }

    public void RequestRewardedVideo()
    {
        // Load the first RewardedAd
        LoadRewardedAd();

        if (OnVideoRequest != null)
        {
            OnVideoRequest();
        }
    }

    #endregion

    #region AdImpression Event
    protected void TrackAdRevenue(AdImpressionData impressionData, int adType)
    {
#if UNITY_ANDROID
        if (!storeChecker || impressionData.Value == 1)
        {
            return;
        }
#endif

        OnAdImpression?.Invoke(impressionData, adType);
    }
    #endregion

    #region Helper Methods
    protected bool InstallerChecker()
    {
        try
        {
            string installer = null;
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var pm = activity.Call<AndroidJavaObject>("getPackageManager");
                installer = pm.Call<string>("getInstallerPackageName", Application.identifier);
            }

            //Debug.Log("Installer: " + installer);

            if (installer == "com.android.vending")
            {
                //Debug.Log("Cài từ Google Play");
                return true;
            }
            else if (installer == "com.miui.packageinstaller" || installer == "com.android.packageinstaller")
            {
                //Debug.Log("Sideload qua file APK");
            }
            else if (installer == null)
            {
                //Debug.Log("Không xác định nguồn cài");
            }
            else
            {
                //Debug.Log("Nguồn khác: " + installer);
            }
        }
        catch { }

        return false;
    }
    #endregion
}

public struct AdImpressionData
{
    public string AdPlatform { get; set; }
    public string AdSource { get; set; }
    public string AdUnitName { get; set; }
    public string AdFormat { get; set; }
    public string Currency { get; set; }
    public double Value { get; set; }
    public string Precision { get; set; }
    public string AdEventName { get; set; }


    public AdImpressionData(
        string adPlatform,
        string adSource,
        string adUnitName,
        string adFormat,
        string currency,
        double value,
        string precision,
        string adEventName)
    {
        AdPlatform = adPlatform;
        AdSource = adSource;
        AdUnitName = adUnitName;
        AdFormat = adFormat;
        Currency = currency;
        Value = value;
        Precision = precision;
        AdEventName = adEventName;

    }
}
