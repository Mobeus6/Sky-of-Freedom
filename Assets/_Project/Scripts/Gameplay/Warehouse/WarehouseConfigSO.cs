using UnityEngine;

[CreateAssetMenu(menuName = "Sky of Freedom/Configs/Warehouse Config")]
public class WarehouseConfigSO : ScriptableObject
{
    [SerializeField]
    private int[] capacities =
    {
        1000,
        2500,
        5000,
        10000,
        17500,
        27500,
        40000,
        60000,
        85000,
        120000
    };

    public int GetCapacity(int level)
    {
        level = Mathf.Clamp(level, 1, capacities.Length);

        return capacities[level - 1];
    }
}