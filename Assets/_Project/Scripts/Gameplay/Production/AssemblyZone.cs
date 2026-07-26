using SkyOfFreedom.Data;

namespace SkyOfFreedom.Production
{
    public class AssemblyZone : ProductionZoneBase
    {
        protected override bool CanProduce(IProducible target)
        {
            return target is DroneModelSO;
        }
    }
}