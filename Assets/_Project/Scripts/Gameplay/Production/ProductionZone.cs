using SkyOfFreedom.Data;

namespace SkyOfFreedom.Production
{
    public class ProductionZone : ProductionZoneBase
    {
        protected override bool CanProduce(IProducible target)
        {
            return target is ComponentSO;
        }
    }
}