using UnityEngine;

namespace SkyOfFreedom.Data
{
    public interface IProducible
    {
        string ID { get; }

        string Name { get; }

        Sprite Icon { get; }

        float ProductionTime { get; }

        int ProductionCost { get; }

        string Description { get; }

    }
}