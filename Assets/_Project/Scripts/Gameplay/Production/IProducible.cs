using UnityEngine;

namespace SkyOfFreedom.Production
{
    public interface IProducible
    {
        string ID { get; }
        string Name { get; }
        string Description { get; }

        Sprite Icon { get; }

        int Tier { get; }

        float ProductionTime { get; }
        int ProductionCost { get; }
    }
}