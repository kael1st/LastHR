using System;
using UnityEngine;

namespace FarmingSystem
{
    /// <summary>
    /// Player Inventory controller managing hotbar slots, selection, and starting items.
    /// Supports hotkeys 1-8, mouse scroll wheel, and programmatic UI selection.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [Header("Hotbar Configuration")]
        [Range(1, 10)]
        [SerializeField] private int hotbarSize = 8;
        [SerializeField] private InventorySlot[] slots;
        [SerializeField] private int selectedSlotIndex = 0;

        [Header("Default Starting Loadout (Optional)")]
        [SerializeField] private ItemData startingHoe;
        [SerializeField] private ItemData startingWateringCan;
        [SerializeField] private ItemData startingSeedPack;
        [SerializeField] private int startingSeedCount = 15;
        [Tooltip("Pocket Watch special item — placed in slot 4 at game start.")]
        [SerializeField] private ItemData startingPocketWatch;

        // Events
        public event Action OnInventoryChanged;
        public event Action<int> OnSelectedSlotChanged;

        public int HotbarSize => hotbarSize;
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

        private void InitializeSlots()
        {
            if (slots == null || slots.Length != hotbarSize)
            {
                slots = new InventorySlot[hotbarSize];
                for (int i = 0; i < hotbarSize; i++)
                {
                    slots[i] = new InventorySlot();
                }
            }
        }

        private void GiveDefaultStartingItems()
        {
            // Give starting hoe if slot 0 is empty
            if (startingHoe != null && slots[0].IsEmpty)
            {
                slots[0].Set(startingHoe, 1);
            }

            // Give watering can in slot 1 if empty
            if (startingWateringCan != null && slots[1].IsEmpty)
            {
                slots[1].Set(startingWateringCan, 1);
            }

            // Give seeds in slot 2 if empty
            if (startingSeedPack != null && slots[2].IsEmpty)
            {
                slots[2].Set(startingSeedPack, startingSeedCount);
            }

            // Give Pocket Watch in slot 3 if empty (unique item, maxStack = 1)
            if (startingPocketWatch != null && slots.Length > 3 && slots[3].IsEmpty)
            {
                slots[3].Set(startingPocketWatch, 1);
            }
        }

        private void HandleHotbarInput()
        {
            // Number keys 1-8
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
                // Scroll up -> previous slot
                int newIndex = selectedSlotIndex - 1;
                if (newIndex < 0) newIndex = hotbarSize - 1;
                SelectSlot(newIndex);
            }
            else if (scroll < -0.01f)
            {
                // Scroll down -> next slot
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
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }

            // 2. Put into first empty slot
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty)
                {
                    slots[i].Set(item, 0);
                    remaining = slots[i].Add(remaining);
                    if (remaining <= 0)
                    {
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }

            OnInventoryChanged?.Invoke();
            return remaining == 0;
        }

        public bool ConsumeSelectedItem(int amount = 1)
        {
            var slot = GetSelectedSlot();
            if (slot == null || slot.IsEmpty) return false;

            slot.Remove(amount);
            OnInventoryChanged?.Invoke();
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
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }

            OnInventoryChanged?.Invoke();
            return remainingToRemove <= 0;
        }

        public void NotifyInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }
    }
}
