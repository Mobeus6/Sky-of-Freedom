using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;

namespace SkyOfFreedom.Production
{
    public static class ProductionRecipeProcessor
    {
        public static bool CanProduce(IProducible item, int quantity)
        {
            if (item == null || quantity <= 0)
                return false;

            if (item is ComponentSO component)
            {
                return GameManager.Instance.Warehouse.HasMaterials(
                    component.Recipe,
                    quantity);
            }

            if (item is DroneModelSO drone)
            {
                return GameManager.Instance.Warehouse.HasComponents(
                    drone.Components,
                    quantity);
            }

            return true;
        }

        public static bool Consume(IProducible item, int quantity)
        {
            if (item == null || quantity <= 0)
                return false;

            if (item is ComponentSO component)
            {
                return GameManager.Instance.Warehouse.RemoveMaterials(
                    component.Recipe,
                    quantity);
            }

            if (item is DroneModelSO drone)
            {
                return GameManager.Instance.Warehouse.RemoveComponents(
                    drone.Components,
                    quantity);
            }

            return true;
        }
    }
}