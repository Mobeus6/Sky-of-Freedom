using UnityEngine;

namespace SkyOfFreedom.UI
{
    // Compatibility shell for existing scene references. Layout is authored in prefabs.
    [DisallowMultipleComponent]
    public class ResponsiveGamePanelUI : MonoBehaviour
    {
        public enum PanelKind { Production, Warehouse, Contracts }
        public static void Attach(GameObject panel, PanelKind kind) { }
    }
}
