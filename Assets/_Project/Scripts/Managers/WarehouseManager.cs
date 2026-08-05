using SkyOfFreedom.Data;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Managers;
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
        private int currentCapacity;

        public int CurrentCapacity => currentCapacity;

        public int MaxCapacity
        {
            get
            {
                WarehouseConfigSO config =
                    GameManager.Instance.Database.Database.WarehouseConfig;

                if (config == null)
                    return 0;

                int level =
                    GameManager.Instance.Factory.GetLevel(
                        FactoryZoneType.Warehouse);

                return config.GetCapacity(level);
            }
        }

        public float CapacityPercent
        {
            get
            {
                if (MaxCapacity == 0)
                    return 0;

                return (float)CurrentCapacity / MaxCapacity;
            }
        }

        public bool Contains(string id) => warehouse.ContainsKey(id);

        public void Clear() => warehouse.Clear();

        public IReadOnlyCollection<WarehouseItem> GetAllItems() => warehouse.Values;

        public IReadOnlyDictionary<string, WarehouseItem> GetItems() => warehouse;

        public int StoredItemCount => warehouse.Count;
        public void Initialize()
        {
            warehouse.Clear();
            currentCapacity = 0; ;
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
        public bool HasMaterials(IReadOnlyList<MaterialAmount> recipe, int multiplier = 1)
        {
            if (recipe == null)
                return false;

            foreach (MaterialAmount material in recipe)
            {
                if (material == null || material.Material == null)
                    continue;

                if (!HasItem(material.Material.ID, material.Amount * multiplier))
                    return false;
            }

            return true;
        }

        public bool RemoveMaterials(IReadOnlyList<MaterialAmount> recipe, int multiplier = 1)
        {
            if (!HasMaterials(recipe, multiplier))
                return false;

            foreach (MaterialAmount material in recipe)
            {
                if (material == null || material.Material == null)
                    continue;

                RemoveItem(material.Material.ID, material.Amount * multiplier);
            }

            return true;
        }

        public bool HasComponents(IReadOnlyList<DroneComponent> components, int multiplier = 1)
        {
            if (components == null)
                return false;

            foreach (DroneComponent component in components)
            {
                if (component == null || component.Component == null)
                    continue;

                if (!HasItem(component.Component.ID,
                    component.Amount * multiplier))
                {
                    return false;
                }
            }

            return true;
        }
        public bool RemoveComponents(IReadOnlyList<DroneComponent> components, int multiplier = 1)
        {
            if (!HasComponents(components, multiplier))
                return false;

            foreach (DroneComponent component in components)
            {
                if (component == null || component.Component == null)
                    continue;

                RemoveItem(
                    component.Component.ID,
                    component.Amount * multiplier);
            }

            return true;
        }
        public void SetQuantity(string id, int quantity)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            int oldQuantity = GetQuantity(id);

            if (quantity <= 0)
            {
                warehouse.Remove(id);

                UpdateCurrentCapacity(id, oldQuantity, 0);

                OnItemChanged?.Invoke(id, 0);
                return;
            }

            if (warehouse.TryGetValue(id, out WarehouseItem item))
            {
                item.SetQuantity(quantity);
            }
            else
            {
                warehouse.Add(id, new WarehouseItem(id, quantity));
            }

            UpdateCurrentCapacity(id, oldQuantity, quantity);

            OnItemChanged?.Invoke(id, quantity);
        }

        public void AddItem(string id, int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(id) || quantity <= 0)
                return;
            if (!CanAdd(id, quantity))
                return;

            int oldQuantity = GetQuantity(id);

            WarehouseItem item;

            if (warehouse.TryGetValue(id, out item))
            {
                item.Add(quantity);
            }
            else
            {
                item = new WarehouseItem(id, quantity);
                warehouse.Add(id, item);
            }

            UpdateCurrentCapacity(id, oldQuantity, item.Quantity);
            OnItemAdded?.Invoke(id, quantity);
            OnItemChanged?.Invoke(id, item.Quantity);
        }

        public bool RemoveItem(string id, int quantity = 1)
        {
            if (!warehouse.TryGetValue(id, out WarehouseItem item))
                return false;

            int oldQuantity = item.Quantity;

            if (!item.Remove(quantity))
                return false;

            if (item.Quantity == 0)
                warehouse.Remove(id);

            UpdateCurrentCapacity(id, oldQuantity, GetQuantity(id));

            OnItemRemoved?.Invoke(id, quantity);
            OnItemChanged?.Invoke(id, GetQuantity(id));

            return true;
        }

        private int GetStorageSize(string id)
        {
            DataSO data =
                GameManager.Instance.Database.Database.GetData(id);

            if (data == null)
            {
                return 0;
            }

            int size = data switch
            {
                MaterialSO material => material.StorageSize,
                ComponentSO component => component.StorageSize,
                DroneModelSO drone => drone.StorageSize,
                _ => 0
            };

            return size;
        }
        public bool CanAdd(string id, int quantity = 1)
        {
            if (quantity <= 0)
                return false;

            int storageSize = GetStorageSize(id);

            if (storageSize <= 0)
                return false;

            int requiredCapacity = storageSize * quantity;

            return currentCapacity + requiredCapacity <= MaxCapacity;
        }
        private void UpdateCurrentCapacity(string id, int oldQuantity, int newQuantity)
        {
            int storageSize = GetStorageSize(id);

            currentCapacity -= oldQuantity * storageSize;
            currentCapacity += newQuantity * storageSize;

            if (currentCapacity < 0)
                currentCapacity = 0;
        }
    }

    
}
