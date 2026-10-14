using System;
using UnityEngine;

namespace FarmingSystem
{
    /// <summary>
    /// Player Inventory controller managing unified inventory storage (Hotbar + Main Storage).
    /// Features:
    /// - 9 Hotbar slots (indices 0..8, controlled with 1-9 keys and mouse scroll).
    /// - 27 Main Inventory storage slots (indices 9..35, 3x9 grid).
    /// - Seamless cursor drag-and-drop, swapping, and stacking.
    /// - Data-driven ScriptableObject integration for crops, tools, and special items.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [Header("Inventory Dimensions")]
        [Range(1, 10)]
        [SerializeField] private int hotbarSize = 9;
        [SerializeField] private int mainInventorySize = 27;

        [Header("Inventory Storage")]
        [SerializeField] private InventorySlot[] slots;
        [SerializeField] private int selectedSlotIndex = 0;

        [Header("Default Starting Loadout (Optional)")]
        [SerializeField] private ItemData startingHoe;
        [SerializeField] private ItemData startingWateringCan;
        [SerializeField] private ItemData startingSeedPack;
        [SerializeField] private int startingSeedCount = 15;
        [Tooltip("Pocket Watch special item — placed in starting loadout.")]
        [SerializeField] private ItemData startingPocketWatch;
        [Tooltip("Shovel tool — placed in starting loadout to dig up crops/seeds.")]
        [SerializeField] private ItemData startingShovel;

        [Header("Extra Starting Seeds for Testing (Optional)")]
        [SerializeField] private ItemData[] extraStartingItems;

        // Events
        public event Action OnInventoryChanged;
        public event Action<int> OnSelectedSlotChanged;

        public int HotbarSize => hotbarSize;
        public int MainInventorySize => mainInventorySize;
        public int TotalSlots => hotbarSize + mainInventorySize;
        public int SelectedSlotIndex => selectedSlotIndex;
        public InventorySlot[] Slots => slots;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeSlots();
        }

        private void Start()
        {
            GiveDefaultStartingItems();
            OnInventoryChanged?.Invoke();
            OnSelectedSlotChanged?.Invoke(selectedSlotIndex);
        }

        private void Update()
        {
            HandleHotbarInput();
        }

        public void InitializeSlots()
        {
            int total = TotalSlots;
            if (slots == null || slots.Length != total)
            {
                var newSlots = new InventorySlot[total];
                for (int i = 0; i < total; i++)
                {
                    if (slots != null && i < slots.Length && slots[i] != null)
                        newSlots[i] = slots[i];
                    else
                        newSlots[i] = new InventorySlot();
                }
                slots = newSlots;
            }
        }

        private void GiveDefaultStartingItems()
        {
            if (slots == null || slots.Length == 0) InitializeSlots();

            // Slot 0: Starting Hoe
            if (startingHoe != null && slots[0].IsEmpty)
            {
                slots[0].Set(startingHoe, 1);
            }

            // Slot 1: Watering Can
            if (startingWateringCan != null && slots.Length > 1 && slots[1].IsEmpty)
            {
                slots[1].Set(startingWateringCan, 1);
            }

            // Slot 2: Turnip Seeds
            if (startingSeedPack != null && slots.Length > 2 && slots[2].IsEmpty)
            {
                slots[2].Set(startingSeedPack, startingSeedCount);
            }

            // Slot 3: Pocket Watch
            if (startingPocketWatch != null && slots.Length > 3 && slots[3].IsEmpty)
            {
                slots[3].Set(startingPocketWatch, 1);
            }

            // Slot 4: Shovel
#if UNITY_EDITOR
            if (startingShovel == null)
            {
                startingShovel = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/ScriptableObjects/Items/Shovel.asset");
            }
#endif
            if (startingShovel != null && slots.Length > 4 && slots[4].IsEmpty)
            {
                slots[4].Set(startingShovel, 1);
            }

            // Additional starting items in subsequent slots
            if (extraStartingItems != null)
            {
                int nextSlot = 5;
                for (int i = 0; i < extraStartingItems.Length && nextSlot < slots.Length; i++)
                {
                    var item = extraStartingItems[i];
                    if (item != null)
                    {
                        while (nextSlot < slots.Length && !slots[nextSlot].IsEmpty) nextSlot++;
                        if (nextSlot < slots.Length)
                        {
                            int count = item.IsSeed ? 5 : (item.IsTool || item.IsSpecial ? 1 : 3);
                            slots[nextSlot].Set(item, count);
                            nextSlot++;
                        }
                    }
                }
            }
        }

        private void HandleHotbarInput()
        {
            // Number keys 1-9 for Hotbar selection
            for (int i = 0; i < hotbarSize; i++)
            {
                KeyCode key = KeyCode.Alpha1 + i;
                if (Input.GetKeyDown(key))
                {
                    SelectSlot(i);
                    return;
                }
            }

            // Mouse scroll wheel
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (scroll > 0.01f)
            {
                // Scroll up -> previous hotbar slot
                int newIndex = selectedSlotIndex - 1;
                if (newIndex < 0) newIndex = hotbarSize - 1;
                SelectSlot(newIndex);
            }
            else if (scroll < -0.01f)
            {
                // Scroll down -> next hotbar slot
                int newIndex = (selectedSlotIndex + 1) % hotbarSize;
                SelectSlot(newIndex);
            }
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= hotbarSize) return;
            if (selectedSlotIndex == index) return;

            selectedSlotIndex = index;
            OnSelectedSlotChanged?.Invoke(selectedSlotIndex);
        }

        public InventorySlot GetSelectedSlot()
        {
            if (selectedSlotIndex < 0 || selectedSlotIndex >= slots.Length) return null;
            return slots[selectedSlotIndex];
        }

        public ItemData GetSelectedItem()
        {
            var slot = GetSelectedSlot();
            return slot != null ? slot.Item : null;
        }

        public InventorySlot GetSlot(int index)
        {
            if (slots == null || index < 0 || index >= slots.Length) return null;
            return slots[index];
        }

        /// <summary>
        /// Moves, stacks, or swaps items between two inventory slots.
        /// Handles max stack constraints and prevents item loss.
        /// </summary>
        public bool MoveOrStack(int fromIndex, int toIndex)
        {
            if (slots == null) return false;
            if (fromIndex < 0 || fromIndex >= slots.Length) return false;
            if (toIndex < 0 || toIndex >= slots.Length) return false;
            if (fromIndex == toIndex) return false;

            var fromSlot = slots[fromIndex];
            var toSlot = slots[toIndex];

            if (fromSlot == null || fromSlot.IsEmpty) return false;

            // Target is empty -> move entire stack
            if (toSlot.IsEmpty)
            {
                toSlot.Set(fromSlot.Item, fromSlot.Quantity);
                fromSlot.Clear();
                NotifyInventoryChanged();
                return true;
            }

            // Target has the same item and item is stackable -> stack
            if (toSlot.Item == fromSlot.Item && toSlot.Item.maxStack > 1)
            {
                int maxStack = toSlot.Item.maxStack;
                int space = maxStack - toSlot.Quantity;
                if (space > 0)
                {
                    int transfer = Mathf.Min(space, fromSlot.Quantity);
                    toSlot.Add(transfer);
                    fromSlot.Remove(transfer);
                    NotifyInventoryChanged();
                    return true;
                }
            }

            // Different item or target is at max stack -> swap slots
            var tempItem = toSlot.Item;
            int tempQty = toSlot.Quantity;

            toSlot.Set(fromSlot.Item, fromSlot.Quantity);
            fromSlot.Set(tempItem, tempQty);

            NotifyInventoryChanged();
            return true;
        }

        /// <summary>
        /// Swaps the items in two slots directly.
        /// </summary>
        public bool SwapSlots(int slotA, int slotB)
        {
            if (slots == null) return false;
            if (slotA < 0 || slotA >= slots.Length) return false;
            if (slotB < 0 || slotB >= slots.Length) return false;
            if (slotA == slotB) return false;

            var a = slots[slotA];
            var b = slots[slotB];

            var tempItem = a.Item;
            int tempQty = a.Quantity;

            a.Set(b.Item, b.Quantity);
            b.Set(tempItem, tempQty);

            NotifyInventoryChanged();
            return true;
        }

        /// <summary>
        /// Splits half of the stack from fromIndex into toIndex (or adds 1 if splitting 1).
        /// </summary>
        public bool SplitStack(int fromIndex, int toIndex)
        {
            if (slots == null) return false;
            if (fromIndex < 0 || fromIndex >= slots.Length) return false;
            if (toIndex < 0 || toIndex >= slots.Length) return false;
            if (fromIndex == toIndex) return false;

            var fromSlot = slots[fromIndex];
            var toSlot = slots[toIndex];

            if (fromSlot == null || fromSlot.IsEmpty || fromSlot.Quantity <= 1) return false;

            int amountToMove = Mathf.CeilToInt(fromSlot.Quantity / 2f);

            if (toSlot.IsEmpty)
            {
                toSlot.Set(fromSlot.Item, amountToMove);
                fromSlot.Remove(amountToMove);
                NotifyInventoryChanged();
                return true;
            }
            else if (toSlot.Item == fromSlot.Item)
            {
                int space = toSlot.Item.maxStack - toSlot.Quantity;
                if (space > 0)
                {
                    int actualMove = Mathf.Min(space, amountToMove);
                    toSlot.Add(actualMove);
                    fromSlot.Remove(actualMove);
                    NotifyInventoryChanged();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds an item to the inventory. First tries to stack into existing matching slots,
        /// then fills empty slots. Checks hotbar first, then main inventory.
        /// </summary>
        public bool AddItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;

            int remaining = amount;

            // 1. Try stacking into existing slots of the same item
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty && slots[i].Item == item && slots[i].Quantity < item.maxStack)
                {
                    remaining = slots[i].Add(remaining);
                    if (remaining <= 0)
                    {
                        NotifyInventoryChanged();
                        return true;
                    }
                }
            }

            // 2. Put into first empty slot
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty)
                {
                    int toAdd = Mathf.Min(remaining, item.maxStack);
                    slots[i].Set(item, toAdd);
                    remaining -= toAdd;
                    if (remaining <= 0)
                    {
                        NotifyInventoryChanged();
                        return true;
                    }
                }
            }

            NotifyInventoryChanged();
            return remaining == 0;
        }

        /// <summary>
        /// Checks whether the inventory has enough space to hold the specified quantity of an item
        /// without modifying inventory contents.
        /// </summary>
        public bool CanAddItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;
            if (slots == null) return false;

            int remaining = amount;

            // 1. Check existing matching slots with space
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty && slots[i].Item == item && slots[i].Quantity < item.maxStack)
                {
                    remaining -= (item.maxStack - slots[i].Quantity);
                    if (remaining <= 0) return true;
                }
            }

            // 2. Check empty slots
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty)
                {
                    remaining -= item.maxStack;
                    if (remaining <= 0) return true;
                }
            }

            return remaining <= 0;
        }

        public bool ConsumeSelectedItem(int amount = 1)
        {
            var slot = GetSelectedSlot();
            if (slot == null || slot.IsEmpty) return false;

            slot.Remove(amount);
            NotifyInventoryChanged();
            return true;
        }

        public bool RemoveItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;

            int remainingToRemove = amount;

            for (int i = slots.Length - 1; i >= 0; i--)
            {
                if (!slots[i].IsEmpty && slots[i].Item == item)
                {
                    int removed = slots[i].Remove(remainingToRemove);
                    remainingToRemove -= removed;
                    if (remainingToRemove <= 0)
                    {
                        NotifyInventoryChanged();
                        return true;
                    }
                }
            }

            NotifyInventoryChanged();
            return remainingToRemove <= 0;
        }

        public int CountItem(ItemData item)
        {
            if (item == null || slots == null) return 0;
            int total = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty && slots[i].Item == item)
                {
                    total += slots[i].Quantity;
                }
            }
            return total;
        }

        public bool HasItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;
            return CountItem(item) >= amount;
        }

        /// <summary>
        /// Gathers all distinct sellable crop items currently present in the player's inventory
        /// (both Hotbar and Main Storage) paired with their total owned quantities.
        /// </summary>
        public System.Collections.Generic.Dictionary<ItemData, int> GetSellableCrops()
        {
            var result = new System.Collections.Generic.Dictionary<ItemData, int>();
            if (slots == null) return result;

            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                if (!s.IsEmpty && s.Item != null && s.Item.IsSellableToVendor)
                {
                    if (result.ContainsKey(s.Item))
                        result[s.Item] += s.Quantity;
                    else
                        result[s.Item] = s.Quantity;
                }
            }
            return result;
        }

        public void NotifyInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
            OnSelectedSlotChanged?.Invoke(selectedSlotIndex);
        }
    }
}
