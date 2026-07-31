using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Warehouse;

namespace SkyOfFreedom.Production
{
    public static class ProductionRecipeProcessor
    {
        public static bool CanProduce(IProducible item, int quantity)
        {
            if (item == null || quantity <= 0)
                return false;

            WarehouseManager warehouse = GameManager.Instance.Warehouse;

            if (item is ComponentSO component)
            {
                return warehouse.HasMaterials(component.Recipe, quantity);
            }

            if (item is DroneModelSO drone)
            {
                return warehouse.HasComponents(drone.Components, quantity);
            }

            return false;
        }

        public static bool Consume(IProducible item, int quantity)
        {
            if (item == null || quantity <= 0)
                return false;

            WarehouseManager warehouse = GameManager.Instance.Warehouse;

            if (item is ComponentSO component)
            {
                return warehouse.RemoveMaterials(component.Recipe, quantity);
            }

            if (item is DroneModelSO drone)
            {
                return warehouse.RemoveComponents(drone.Components, quantity);
            }

            return false;
        }
    }
}