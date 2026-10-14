using System;
using UnityEngine;

namespace FarmingSystem
{
    /// <summary>
    /// Centralized currency management system for the player.
    /// Provides strict validation, overflow protection, events, and transactional operations.
    /// Direct mutation of balance is restricted to controlled APIs: AddCoins(), TrySpendCoins(), SetCoins().
    /// </summary>
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        [Header("Starting Balance")]
        [Tooltip("Initial coins given to player on new game.")]
        [SerializeField] private long startingCoins = 100;

        [Header("Runtime Balance")]
        [SerializeField] private long currentCoins;

        // Events
        public event Action<long> OnCoinsChanged;
        public event Action<long> OnCoinsAdded;
        public event Action<long> OnCoinsSpent;

        public long Coins => currentCoins;
        public const long MaxCoins = 999_999_999; // Clean cap to avoid UI overflow and mathematical overflow

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentCoins = Math.Max(0, startingCoins);
        }

        private void Start()
        {
            OnCoinsChanged?.Invoke(currentCoins);
        }

        /// <summary>
        /// Adds coins to the player's balance. Rejects non-positive amounts and clamps safely to MaxCoins.
        /// </summary>
        public bool AddCoins(long amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[Currency] Attempted to add invalid amount: {amount}");
                return false;
            }

            checked
            {
                currentCoins = Math.Min(MaxCoins, currentCoins + amount);
            }

            OnCoinsAdded?.Invoke(amount);
            OnCoinsChanged?.Invoke(currentCoins);
            return true;
        }

        /// <summary>
        /// Attempts to deduct coins from the player's balance. Fails if balance is insufficient or amount is invalid.
        /// </summary>
        public bool TrySpendCoins(long amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[Currency] Attempted to spend invalid amount: {amount}");
                return false;
            }

            if (currentCoins < amount)
            {
                return false; // Insufficient funds
            }

            currentCoins -= amount;
            OnCoinsSpent?.Invoke(amount);
            OnCoinsChanged?.Invoke(currentCoins);
            return true;
        }

        public bool CanAfford(long amount)
        {
            return amount >= 0 && currentCoins >= amount;
        }

        /// <summary>
        /// Sets the balance directly. Used exclusively for persistence / GameStateSnapshot restoration.
        /// </summary>
        public void SetCoins(long amount)
        {
            currentCoins = Math.Max(0, Math.Min(MaxCoins, amount));
            OnCoinsChanged?.Invoke(currentCoins);
        }

        public string GetFormattedCoins()
        {
            return currentCoins.ToString("N0") + " G";
        }
    }
}
