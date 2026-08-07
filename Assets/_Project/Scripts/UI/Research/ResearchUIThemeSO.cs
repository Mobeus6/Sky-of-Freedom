using UnityEngine;

namespace SkyOfFreedom.UI
{
    [CreateAssetMenu(
        fileName = "ResearchUITheme",
        menuName = "Sky of Freedom/UI/Research Theme")]
    public class ResearchUIThemeSO : ScriptableObject
    {
        [Header("Node")]
        [SerializeField]
        private Color background =
            new(0.141f, 0.169f, 0.200f);

        [Header("Tier Colors")]
        [SerializeField]
        private Color tier1 =
            new(0.33f, 0.78f, 0.33f);

        [SerializeField]
        private Color tier2 =
            new(0.27f, 0.56f, 0.95f);

        [SerializeField]
        private Color tier3 =
            new(0.70f, 0.42f, 0.95f);

        [SerializeField]
        private Color tier4 =
            new(1.00f, 0.62f, 0.16f);

        [SerializeField]
        private Color tier5 =
            new(0.95f, 0.25f, 0.25f);

        [Header("Connection Colors")]
        [SerializeField]
        private Color lockedConnection =
            new(0.35f, 0.35f, 0.35f);

        [SerializeField]
        private Color tier1Connection =
            new(0.33f, 0.78f, 0.33f);

        [SerializeField]
        private Color tier2Connection =
            new(0.27f, 0.56f, 0.95f);

        [SerializeField]
        private Color tier3Connection =
            new(0.70f, 0.42f, 0.95f);

        [SerializeField]
        private Color tier4Connection =
            new(1.00f, 0.62f, 0.16f);

        [SerializeField]
        private Color tier5Connection =
            new(0.95f, 0.25f, 0.25f);

        [Header("Selection")]
        [SerializeField]
        private Color selectedOutline =
            Color.white;

        public Color Background => background;

        public Color SelectedOutline => selectedOutline;

        public Color GetTierColor(int tier)
        {
            return tier switch
            {
                1 => tier1,
                2 => tier2,
                3 => tier3,
                4 => tier4,
                5 => tier5,
                _ => tier1
            };
        }

        public Color GetConnectionColor(bool locked, int tier)
        {
            if (locked)
                return lockedConnection;

            return tier switch
            {
                1 => tier1Connection,
                2 => tier2Connection,
                3 => tier3Connection,
                4 => tier4Connection,
                5 => tier5Connection,
                _ => tier1Connection
            };
        }
    }
}