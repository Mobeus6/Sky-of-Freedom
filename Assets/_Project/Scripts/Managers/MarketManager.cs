using SkyOfFreedom.Data;
using SkyOfFreedom.Warehouse;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Managers
{
    public class MarketManager : BaseManager
    {
        private readonly Dictionary<string, float> priceModifiers = new();
        public event Action<string> OnPriceChanged;
        private WarehouseManager warehouse;
        private EconomyManager economy;
        public override void Initialize()
        {
            warehouse = GameManager.Instance.Warehouse;
            economy = GameManager.Instance.Economy;

            base.Initialize();
        }

        public override void Shutdown()
        {
            priceModifiers.Clear();

            base.Shutdown();
        }

        public int GetBasePrice(DataSO data)
        {
            if (data == null)
                return 0;

            switch (data)
            {
                case MaterialSO material:
                    return material.BasePrice;

                case ComponentSO component:
                    return component.ProductionCost;

                case DroneModelSO drone:
                    return drone.ProductionCost;
            }

            return 0;
        }

        public float GetModifier(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return 1f;

            if (priceModifiers.TryGetValue(id, out float modifier))
                return modifier;

            return 1f;
        }

        public void SetModifier(string id, float modifier)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            modifier = Mathf.Max(0.1f, modifier);

            if (priceModifiers.TryGetValue(id, out float current))
            {
                if (Mathf.Approximately(current, modifier))
                    return;
            }

            priceModifiers[id] = modifier;

            OnPriceChanged?.Invoke(id);
        }

        public int GetCurrentPrice(DataSO data)
        {
            if (data == null)
                return 0;

            int basePrice = GetBasePrice(data);

            float modifier = GetModifier(data.ID);

            if (data is MaterialSO && GameManager.Instance != null &&
                GameManager.Instance.Research != null)
                modifier *= 1f - GameManager.Instance.Research.GetMaterialDiscountPercent() / 100f;

            // Keep paid materials purchasable and non-free after integer rounding.
            return basePrice > 0 ? Mathf.Max(1, Mathf.RoundToInt(basePrice * modifier)) : 0;
        }
        public int GetPriceDifference(DataSO data)
        {
            if (data == null)
                return 0;

            return GetCurrentPrice(data) - GetBasePrice(data);
        }

        public float GetPriceDifferencePercent(DataSO data)
        {
            if (data == null)
                return 0f;

            int basePrice = GetBasePrice(data);

            if (basePrice <= 0)
                return 0f;

            return (GetCurrentPrice(data) - basePrice) * 100f / basePrice;
        }
        public MarketTransactionResult BuyMaterial(MaterialSO material, int quantity)
        {
            if (material == null)
                return MarketTransactionResult.InvalidItem;

            if (quantity <= 0)
                return MarketTransactionResult.InvalidQuantity;

            long total = (long)GetCurrentPrice(material) * quantity;
            if (total > int.MaxValue)
                return MarketTransactionResult.NotEnoughMoney;
            int totalPrice = (int)total;

            if (!economy.HasMoney(totalPrice))
                return MarketTransactionResult.NotEnoughMoney;

            if (!warehouse.CanAdd(material.ID, quantity))
                return MarketTransactionResult.WarehouseFull;

            if (!economy.SpendMoney(totalPrice))
                return MarketTransactionResult.NotEnoughMoney;

            warehouse.AddItem(material.ID, quantity);

            return MarketTransactionResult.Success;
        }
        public MarketTransactionResult SellMaterial(MaterialSO material, int quantity)
        {
            if (material == null)
                return MarketTransactionResult.InvalidItem;

            if (quantity <= 0)
                return MarketTransactionResult.InvalidQuantity;

            if (!warehouse.HasItem(material.ID, quantity))
                return MarketTransactionResult.NotEnoughItems;

            int totalPrice = GetSellPrice(material) * quantity;

            warehouse.RemoveItem(material.ID, quantity);

            economy.AddMoney(totalPrice);

            return MarketTransactionResult.Success;
        }

        public int GetSellPrice(MaterialSO material)
        {
            if (material == null)
                return 0;

            // Supplier discounts affect purchases, not the market resale value.
            int marketPrice = Mathf.RoundToInt(GetBasePrice(material) * GetModifier(material.ID));
            return Mathf.RoundToInt(marketPrice * 0.8f);
        }

        public int GetMaxAffordable(MaterialSO material)
        {
            if (material == null)
                return 0;

            int price = GetCurrentPrice(material);

            if (price <= 0)
                return 0;

            return Mathf.FloorToInt((float)economy.Money / price);
        }

        public int GetMaxStorable(MaterialSO material)
        {
            if (material == null)
                return 0;

            if (material.StorageSize <= 0)
                return 0;

            int freeCapacity =
                warehouse.MaxCapacity - warehouse.CurrentCapacity;

            if (freeCapacity <= 0)
                return 0;

            return freeCapacity / material.StorageSize;
        }

        public int GetMaxBuyQuantity(MaterialSO material)
        {
            if (material == null)
                return 0;

            return Mathf.Min(
    GetMaxAffordable(material),
    GetMaxStorable(material),
    GetAvailableQuantity(material));
        }

        public int GetMaxSellQuantity(MaterialSO material)
        {
            if (material == null)
                return 0;

            return warehouse.GetQuantity(material.ID);
        }
        public int GetAvailableQuantity(MaterialSO material)
        {
            return int.MaxValue;
        }
    }
}
