using UnityEngine;
using SkyOfFreedom.Managers;

namespace SkyOfFreedom.Production
{
    public class ProductionZoneBootstrap : MonoBehaviour
    {
        [SerializeField] private ProductionZone[] zones;

        private void Start()
        {
            foreach (var zone in zones)
            {
                GameManager.Instance.Production.RegisterZone(zone);
            }
        }
    }
}