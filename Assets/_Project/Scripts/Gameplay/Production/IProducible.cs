namespace SkyOfFreedom.Data
{
    public interface IProducible
    {
        float ProductionTime { get; }

        int ProductionCost { get; }
    }
}