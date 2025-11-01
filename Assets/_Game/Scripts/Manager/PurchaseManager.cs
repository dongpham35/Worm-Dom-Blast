using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;

public class PurchaseManager : MonoBehaviour
{
    #region Singleton

    protected static PurchaseManager _instance;

    public static PurchaseManager Instance
    {
        get
        {
            return _instance;
        }
    }
    #endregion

    #region Fields
    private StoreController m_StoreController;
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

    }
    #endregion


    public async Task InitializePurchasing()
    {
        if (IsStoreInitialized())
            return;

        m_StoreController = UnityIAPServices.StoreController();

        // Subscribe to purchase events
        m_StoreController.OnPurchasePending += OnPurchasePending;
        m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;

        try
        {
            // Connect to store services
            await m_StoreController.Connect();

            //Debug.Log("Unity IAP v5 initialization successful");

            // Fetch your products
            FetchProducts();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Store connection failed: {e}");
            // Your UI logic for showing initialization errors
            Debug.LogError("Failed to initialize store - check internet connection and store setup");
        }
    }

    private void FetchProducts()
    {
        m_StoreController.OnProductsFetched += OnProductsFetched;
        m_StoreController.OnPurchasesFetched += OnPurchasesFetched;

        var products = new List<ProductDefinition>
        {
            new ProductDefinition("premium_upgrade", ProductType.NonConsumable),
            new ProductDefinition("gold_coins_100", ProductType.Consumable),
            new ProductDefinition("monthly_subscription", ProductType.Subscription)
        };

        m_StoreController.FetchProducts(products);
        //Debug.Log("Products fetched and ready for purchase");
    }

    #region Purchase Event Handlers
    private void OnPurchasesFetched(Orders orders)
    {

    }

    private void OnProductsFetched(List<Product> list)
    {

    }

    private void OnPurchaseConfirmed(Order order)
    {
        switch (order)
        {
            case FailedOrder failedOrder:
                var failedProduct = failedOrder.CartOrdered.Items().First().Product.definition.id;

                // Your UI logic for showing purchase errors
                //Debug.LogError($"Purchase failed for {failedProduct}: {failedOrder.FailureReason}");
                break;

            case ConfirmedOrder confirmedOrder:
                var confirmedProduct = order.CartOrdered.Items().First().Product.definition.id;

                // Your UI logic for showing purchase success
                //Debug.Log($"Purchase successful: {confirmedProduct}");
                break;
        }
    }

    private void OnPurchasePending(PendingOrder order)
    {
        //Debug.Log("Purchase pending - processing...");

        // Reward the player BEFORE confirming purchase
        var productId = order.CartOrdered.Items().First().Product.definition.id;
        ProcessPurchaseReward(productId);
        //ProcessPurchaseReward1(productId);

        // Confirm the purchase (required)
        m_StoreController.ConfirmPurchase(order);
    }
    #endregion

    #region Purchase Processing

    private void ProcessPurchaseReward(string productId)
    {

    }

    #endregion

    #region Public Purchase Methods
    public void BuyProduct(string productId)
    {
        if (!IsStoreInitialized())
        {
            //Debug.LogError("Store controller not initialized");
            return;
        }

        var product = m_StoreController.GetProducts().FirstOrDefault(p => p.definition.id == productId);

        if (product != null)
        {
            //Debug.Log($"Initiating purchase: {productId}");
            m_StoreController.PurchaseProduct(product);
        }
        else
        {
            //Debug.LogError($"Product not found: {productId}");
        }
    }

    public void RestorePurchases()
    {
        if (IsStoreInitialized())
        {
            m_StoreController.RestoreTransactions(OnTransactionsRestored);
        }
    }

    private void OnTransactionsRestored(bool success, string error)
    {
        if (success)
        {
            //Debug.Log("Transactions restored successfully");
        }
        else
        {
            //Debug.LogError($"Transaction restoration failed: {error}");
        }
    }

    #endregion


    #region Helper Methods

    public bool IsStoreInitialized()
    {
        return m_StoreController != null;
    }

    public string GetProductPrice(string productId)
    {
        if (!IsStoreInitialized()) return "N/A";

        var product = m_StoreController.GetProducts().FirstOrDefault(p => p.definition.id == productId);
        return product?.metadata.localizedPriceString ?? "N/A";
    }

    public bool IsProductOwned(string productId)
    {
        // Fallback: try to get controller from Unity IAP if proxy wasn't initialized properly
        if (!IsStoreInitialized())
        {
            return false;
        }

        var product = m_StoreController.GetProducts().FirstOrDefault(p => p.definition.id == productId);
        return product != null && product.hasReceipt;

    }

    #endregion


    #region Game Logic (Your Implementation)

    private void ProcessPurchaseReward1(string productId)
    {
        Debug.Log($"Processing purchase reward: {productId}");

        switch (productId)
        {
            case "premium_upgrade":
                UnlockPremium();
                Debug.Log("Premium upgrade unlocked");
                break;

            case "gold_coins_100":
                AddCoins(100);
                Debug.Log("Added 100 gold coins");
                break;

            case "monthly_subscription":
                ActivateSubscription();
                Debug.Log("Monthly subscription activated");
                break;

            default:
                Debug.LogWarning($"Unknown product: {productId}");
                break;
        }

        // Adinmo automatically tracks this purchase in the background
    }

    private void BuyPremiumUpgrade()
    {
        BuyProduct("premium_upgrade");
    }

    private void BuyGoldCoins()
    {
        BuyProduct("gold_coins_100");
    }

    private void BuySubscription()
    {
        BuyProduct("monthly_subscription");
    }

    private void UnlockPremium()
    {
        // Your game logic for premium unlock
        //PlayerPrefs.SetInt("PremiumUnlocked", 1);
    }

    private void AddCoins(int amount)
    {
        // Your game logic for adding coins
        //int currentCoins = PlayerPrefs.GetInt("GoldCoins", 0);
        //PlayerPrefs.SetInt("GoldCoins", currentCoins + amount);
    }

    private void ActivateSubscription()
    {
        // Your game logic for subscription activation
        //PlayerPrefs.SetString("SubscriptionActive", System.DateTime.Now.ToString());
    }

    #endregion
}
