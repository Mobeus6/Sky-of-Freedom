using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        private readonly Dictionary<string, InventoryItem> inventory =
            new Dictionary<string, InventoryItem>();

        public event Action<string, int> OnItemAdded;
        public event Action<string, int> OnItemRemoved;
        public event Action<string, int> OnItemChanged;

        public void Initialize()
        {
            inventory.Clear();

            Add("MAT-STEEL", 100);

            Debug.Log(GetAmount("MAT-STEEL"));

            Remove("MAT-STEEL", 25);

            Debug.Log(GetAmount("MAT-STEEL"));
        }

        public void Shutdown()
        {
            inventory.Clear();
        }

        public bool Has(string id, int amount = 1)
        {
            if (!inventory.TryGetValue(id, out InventoryItem item))
            {
                return false;
            }

            return item.Amount >= amount;
        }

        public int GetAmount(string id)
        {
            if (!inventory.TryGetValue(id, out InventoryItem item))
            {
                return 0;
            }

            return item.Amount;
        }

        public void SetAmount(string id, int amount)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogError("Inventory ID is null or empty.");
                return;
            }

            if (amount < 0)
            {
                Debug.LogError($"Inventory amount cannot be negative ({id}).");
                return;
            }

            if (amount == 0)
            {
                inventory.Remove(id);

                OnItemChanged?.Invoke(id, 0);

                return;
            }

            if (inventory.TryGetValue(id, out InventoryItem item))
            {
                item.SetAmount(amount);
            }
            else
            {
                inventory.Add(id, new InventoryItem(id, amount));
            }

            OnItemChanged?.Invoke(id, amount);
        }

        public void Add(string id, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogError("Inventory ID is null or empty.");
                return;
            }

            if (amount <= 0)
            {
                Debug.LogError("Amount must be greater than zero.");
                return;
            }

            if (inventory.TryGetValue(id, out InventoryItem item))
            {
                item.Add(amount);
            }
            else
            {
                item = new InventoryItem(id, amount);
                inventory.Add(id, item);
            }

            OnItemAdded?.Invoke(id, amount);
            OnItemChanged?.Invoke(id, item.Amount);
        }
        public bool Remove(string id, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogError("Inventory ID is null or empty.");
                return false;
            }

            if (amount <= 0)
            {
                Debug.LogError("Amount must be greater than zero.");
                return false;
            }

            if (!inventory.TryGetValue(id, out InventoryItem item))
            {
                return false;
            }

            if (!item.Remove(amount))
            {
                return false;
            }

            if (item.Amount == 0)
            {
                inventory.Remove(id);
            }

            OnItemRemoved?.Invoke(id, amount);
            OnItemChanged?.Invoke(id, GetAmount(id));

            return true;
        }

        public bool Contains(string id)
        {
            return inventory.ContainsKey(id);
        }

        public void Clear()
        {
            inventory.Clear();
        }

        public IReadOnlyCollection<InventoryItem> GetAllItems()
        {
            return inventory.Values;
        }

        public IReadOnlyDictionary<string, InventoryItem> GetItems()
        {
            return inventory;
        }

        public int ItemCount
        {
            get
            {
                return inventory.Count;
            }
        }
    }
}