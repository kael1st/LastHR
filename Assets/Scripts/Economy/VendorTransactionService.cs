using System;
using UnityEngine;

namespace FarmingSystem
{
    /// <summary>
    /// Transaction service that executes atomic sales between the player's inventory and currency.
    /// Handles all business validation, reentrancy locking, and data integrity.
    /// </summary>
    public static class VendorTransactionService
    {
        private static bool isProcessing = false;

        public struct TransactionResult
        {
            public bool Success;
            public ItemData Item;
            public int QuantitySold;
            public int UnitPrice;
            public long TotalPayout;
            public string Message;

            public static TransactionResult Fail(string message)
            {
                return new TransactionResult
                {
                    Success = false,
                    Item = null,
                    QuantitySold = 0,
                    UnitPrice = 0,
                    TotalPayout = 0,
                    Message = message
                };
            }
        }

        /// <summary>
        /// Executes an atomic sale of harvested crops.
        /// Deducts items from PlayerInventory and credits coins to CurrencyManager in one logical transaction.
        /// </summary>
        public static TransactionResult SellItem(PlayerInventory inventory, ItemData item, int quantity)
        {
            if (isProcessing)
            {
                return TransactionResult.Fail("Transaction is currently processing. Please wait.");
            }

            try
            {
                isProcessing = true;

                // 1. Validate Inventory reference
                if (inventory == null)
                {
                    return TransactionResult.Fail("Player inventory is unavailable.");
                }

                // 2. Validate Item reference
                if (item == null)
                {
                    return TransactionResult.Fail("No item specified for sale.");
                }

                // 3. Validate Item Eligibility (only harvested crops with valid sell price)
                if (!item.IsSellableToVendor)
                {
                    if (item.IsSeed)
                        return TransactionResult.Fail("Seeds cannot be sold to the vendor.");
                    if (item.IsTool)
                        return TransactionResult.Fail("Tools cannot be sold.");
                    return TransactionResult.Fail($"{item.displayName} is not eligible for sale.");
                }

                // 4. Validate Quantity
                if (quantity <= 0)
                {
                    return TransactionResult.Fail("Sale quantity must be at least 1.");
                }

                int owned = inventory.CountItem(item);
                if (owned <= 0)
                {
                    return TransactionResult.Fail($"You do not have any {item.displayName} to sell.");
                }

                if (quantity > owned)
                {
                    return TransactionResult.Fail($"Cannot sell {quantity}x {item.displayName}. You only own {owned}.");
                }

                // 5. Validate Unit Price & Total Payout Calculation (Overflow Safe)
                int unitPrice = item.sellPrice;
                if (unitPrice <= 0)
                {
                    return TransactionResult.Fail($"{item.displayName} has no commercial value.");
                }

                long totalPayout;
                try
                {
                    checked
                    {
                        totalPayout = (long)unitPrice * quantity;
                    }
                }
                catch (OverflowException)
                {
                    return TransactionResult.Fail("Transaction amount is too large.");
                }

                // 6. Deduct item from inventory
                bool removed = inventory.RemoveItem(item, quantity);
                if (!removed)
                {
                    return TransactionResult.Fail("Failed to remove items from inventory. Transaction aborted.");
                }

                // 7. Award coins
                if (CurrencyManager.Instance != null)
                {
                    CurrencyManager.Instance.AddCoins(totalPayout);
                }

                return new TransactionResult
                {
                    Success = true,
                    Item = item,
                    QuantitySold = quantity,
                    UnitPrice = unitPrice,
                    TotalPayout = totalPayout,
                    Message = $"Sold {quantity}x {item.displayName} for {totalPayout:N0} G!"
                };
            }
            finally
            {
                isProcessing = false;
            }
        }
    }
}
