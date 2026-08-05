using SkyOfFreedom.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Managers
{
    public class MarketManager : BaseManager
    {
        private readonly Dictionary<string, float> priceModifiers = new();
        public event Action<string> OnPriceChanged;
        public override void Initialize()
        {
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

            return Mathf.RoundToInt(basePrice * modifier);
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
    }
}