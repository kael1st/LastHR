using System;
using UnityEngine;

namespace FarmingSystem
{
    [Serializable]
    public class InventorySlot
    {
        [SerializeField] private ItemData item;
        [SerializeField] private int quantity;

        public ItemData Item => item;
        public int Quantity => quantity;
        public bool IsEmpty => item == null || quantity <= 0;

        public InventorySlot()
        {
            item = null;
            quantity = 0;
        }

        public InventorySlot(ItemData item, int quantity)
        {
            this.item = item;
            this.quantity = Mathf.Max(0, quantity);
            if (this.quantity == 0) this.item = null;
        }

        public void Set(ItemData newItem, int newQuantity)
        {
            item = newItem;
            quantity = Mathf.Max(0, newQuantity);
            if (quantity == 0) item = null;
        }

        public bool CanAdd(ItemData incomingItem, int amount)
        {
            if (incomingItem == null || amount <= 0) return false;
            if (IsEmpty) return true;
            if (item != incomingItem) return false;
            return quantity + amount <= item.maxStack;
        }

        public int Add(int amount)
        {
            if (item == null || amount <= 0) return amount;
            int space = item.maxStack - quantity;
            int toAdd = Mathf.Min(space, amount);
            quantity += toAdd;
            return amount - toAdd; // Return leftover that couldn't fit
        }

        public int Remove(int amount)
        {
            if (IsEmpty || amount <= 0) return 0;
            int removed = Mathf.Min(quantity, amount);
            quantity -= removed;
            if (quantity <= 0)
            {
                Clear();
            }
            return removed;
        }

        public void Clear()
        {
            item = null;
            quantity = 0;
        }
    }
}
