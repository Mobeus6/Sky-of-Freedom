using UnityEngine;

namespace SkyOfFreedom.UI
{
    // Compatibility shell. Configure GridLayoutGroup in the editor instead.
    [DisallowMultipleComponent]
    public class ResponsiveGridUI : MonoBehaviour
    {
        public static void Configure(Transform content, float minWidth, float height, int maxColumns = 20) { }
        public void Refresh() { }
    }
}
