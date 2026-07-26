using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Warehouse
{
    public class WarehouseManager : MonoBehaviour
    {
        private readonly Dictionary<string, WarehouseItem> warehouse =
            new Dictionary<string, WarehouseItem>();

        public event Action<string, int> OnItemAdded;
        public event Action<string, int> OnItemRemoved;
        public event Action<string, int> OnItemChanged;

        public void Initialize()
        {
            warehouse.Clear();
        }

        public void Shutdown()
        {
            warehouse.Clear();
        }

        public bool HasItem(string id, int quantity = 1)
        {
            if (!warehouse.TryGetValue(id, out WarehouseItem item))
                return false;

            return item.Quantity >= quantity;
        }

        public int GetQuantity(string id)
        {
            if (!warehouse.TryGetValue(id, out WarehouseItem item))
                return 0;

            return item.Quantity;
        }

        public void SetQuantity(string id, int quantity)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogError("Warehouse item ID is null or empty.");
                return;
            }

            if (quantity < 0)
            {
                Debug.LogError("Quantity cannot be negative.");
                return;
            }

            if (quantity == 0)
            {
                warehouse.Remove(id);
                OnItemChanged?.Invoke(id, 0);
                return;
            }

            if (warehouse.TryGetValue(id, out WarehouseItem item))
                item.SetQuantity(quantity);
            else
                warehouse.Add(id, new WarehouseItem(id, quantity));

            OnItemChanged?.Invoke(id, quantity);
        }

        public void AddItem(string id, int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(id) || quantity <= 0)
                return;

            if (warehouse.TryGetValue(id, out WarehouseItem item))
                item.Add(quantity);
            else
            {
                item = new WarehouseItem(id, quantity);
                warehouse.Add(id, item);
            }

            OnItemAdded?.Invoke(id, quantity);
            OnItemChanged?.Invoke(id, item.Quantity);
        }

        public bool RemoveItem(string id, int quantity = 1)
        {
            if (!warehouse.TryGetValue(id, out WarehouseItem item))
                return false;

            if (!item.Remove(quantity))
                return false;

            if (item.Quantity == 0)
                warehouse.Remove(id);

            OnItemRemoved?.Invoke(id, quantity);
            OnItemChanged?.Invoke(id, GetQuantity(id));

            return true;
        }

        public bool Contains(string id) => warehouse.ContainsKey(id);

        public void Clear() => warehouse.Clear();

        public IReadOnlyCollection<WarehouseItem> GetAllItems() => warehouse.Values;

        public IReadOnlyDictionary<string, WarehouseItem> GetItems() => warehouse;

        public int StoredItemCount => warehouse.Count;
    }
}
