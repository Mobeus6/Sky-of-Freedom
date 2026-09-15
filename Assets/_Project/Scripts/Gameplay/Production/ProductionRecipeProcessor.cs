using System;
using System.Collections.Generic;
using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;

namespace SkyOfFreedom.Production
{
    public static class ProductionRecipeProcessor
    {
        public static bool TryGetRequirements(IProducible item, int quantity,
            out Dictionary<string, int> requirements)
        {
            requirements = new Dictionary<string, int>();
            if (item == null || quantity <= 0) return false;
            try
            {
                if (item is ComponentSO component)
                {
                    if (component.Recipe == null) return false;
                    foreach (var entry in component.Recipe)
                    {
                        if (entry == null || entry.Material == null ||
                            string.IsNullOrWhiteSpace(entry.Material.ID) || entry.Amount <= 0)
                            return false;
                        AddRequirement(requirements, entry.Material.ID, entry.Amount, quantity);
                    }
                }
                else if (item is DroneModelSO drone)
                {
                    if (drone.Components == null) return false;
                    foreach (var entry in drone.Components)
                    {
                        if (entry == null || entry.Component == null ||
                            string.IsNullOrWhiteSpace(entry.Component.ID) || entry.Amount <= 0)
                            return false;
                        AddRequirement(requirements, entry.Component.ID, entry.Amount, quantity);
                    }
                }
                else return false;
            }
            catch (OverflowException) { return false; }
            return true;
        }

        private static void AddRequirement(Dictionary<string, int> requirements,
            string id, int amount, int quantity)
        {
            requirements.TryGetValue(id, out int current);
            requirements[id] = checked(current + checked(amount * quantity));
        }

        public static bool CanProduce(IProducible item, int quantity)
        {
            var warehouse = GameManager.Instance?.Warehouse;
            if (warehouse == null || !TryGetRequirements(item, quantity, out var required))
                return false;
            foreach (var entry in required)
                if (!warehouse.HasItem(entry.Key, entry.Value)) return false;
            return true;
        }

        public static bool Consume(IProducible item, int quantity)
        {
            return ConsumeIngredients(item, quantity);
        }

        public static int GetMaxQuantity(IProducible item, int limit)
        {
            var warehouse = GameManager.Instance?.Warehouse;
            if (warehouse == null || !TryGetRequirements(item, 1, out var required))
                return 0;
            int maximum = Math.Max(0, limit);
            foreach (var entry in required)
                maximum = Math.Min(maximum, warehouse.GetQuantity(entry.Key) / entry.Value);
            return maximum;
        }

        private static bool ConsumeIngredients(IProducible item, int quantity)
        {
            var warehouse = GameManager.Instance?.Warehouse;
            if (warehouse == null || !TryGetRequirements(item, quantity, out var required))
                return false;
            foreach (var entry in required)
                if (!warehouse.HasItem(entry.Key, entry.Value)) return false;

            Dictionary<string, int> removed = new Dictionary<string, int>();
            foreach (var entry in required)
            {
                if (!warehouse.RemoveItem(entry.Key, entry.Value))
                {
                    foreach (var undo in removed)
                        warehouse.SetQuantity(undo.Key,
                            checked(warehouse.GetQuantity(undo.Key) + undo.Value));
                    return false;
                }
                removed.Add(entry.Key, entry.Value);
            }
            return true;
        }

        public static void Refund(IProducible item, int quantity)
        {
            var warehouse = GameManager.Instance?.Warehouse;
            if (warehouse == null || !TryGetRequirements(item, quantity, out var required))
                throw new InvalidOperationException("Cannot refund production ingredients.");
            // Restore consumed ingredients even if capacity changed during a callback.
            foreach (var entry in required)
                warehouse.SetQuantity(entry.Key,
                    checked(warehouse.GetQuantity(entry.Key) + entry.Value));
        }
    }
}
