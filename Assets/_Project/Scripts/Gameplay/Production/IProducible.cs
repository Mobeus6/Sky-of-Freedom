namespace SkyOfFreedom.Data
{
    public interface IProducible
    {
        string ID { get; }

        float ProductionTime { get; }

        int ProductionCost { get; }
    }
}