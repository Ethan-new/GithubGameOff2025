using UnityEngine;

/// <summary>
/// Manages the player's money. Tracks kills and provides events for UI updates.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    [Header("Money Settings")]
    [SerializeField] private int moneyPerKill = 10;

    private int currentMoney = 0;

    /// <summary>
    /// The current player money.
    /// </summary>
    public int CurrentMoney => currentMoney;

    /// <summary>
    /// Event that is called when the money changes. Passes the new money value.
    /// </summary>
    public System.Action<int> OnMoneyChanged;

    /// <summary>
    /// Singleton instance for easy access from other scripts.
    /// </summary>
    public static MoneyManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // Keep MoneyManager alive across scene loads if needed
            // Uncomment if you want persistent money across scenes:
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("Multiple MoneyManager instances found. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Adds money for a kill. Called when an enemy dies.
    /// </summary>
    public void AddKillMoney()
    {
        AddMoney(moneyPerKill);
    }

    /// <summary>
    /// Adds the specified amount to the money.
    /// </summary>
    /// <param name="amount">The amount of money to add.</param>
    public void AddMoney(int amount)
    {
        if (amount <= 0)
            return;

        currentMoney += amount;
        OnMoneyChanged?.Invoke(currentMoney);
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Money: ${currentMoney} (+${amount})");
        #endif
    }

    /// <summary>
    /// Spends money. Returns true if the player had enough money, false otherwise.
    /// </summary>
    /// <param name="amount">The amount of money to spend.</param>
    /// <returns>True if the money was spent successfully, false if insufficient funds.</returns>
    public bool SpendMoney(int amount)
    {
        if (amount <= 0)
            return false;

        if (currentMoney < amount)
            return false;

        currentMoney -= amount;
        OnMoneyChanged?.Invoke(currentMoney);
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Money: ${currentMoney} (-${amount})");
        #endif
        return true;
    }

    /// <summary>
    /// Resets the money to zero.
    /// </summary>
    public void ResetMoney()
    {
        currentMoney = 0;
        OnMoneyChanged?.Invoke(currentMoney);
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("Money reset to $0");
        #endif
    }

    /// <summary>
    /// Sets the money to a specific value.
    /// </summary>
    /// <param name="money">The money value to set.</param>
    public void SetMoney(int money)
    {
        currentMoney = Mathf.Max(0, money);
        OnMoneyChanged?.Invoke(currentMoney);
    }
}


