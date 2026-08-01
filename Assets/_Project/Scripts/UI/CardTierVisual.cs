using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class CardTierVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image border;
        [SerializeField] private Image glowSmall;
        [SerializeField] private Image glowBig;
        [SerializeField] private Image badge;
        [SerializeField] private TMPro.TMP_Text title;

        public void SetTier(int tier)
        {
            Color borderColor;
            Color glowSmallColor;
            Color glowBigColor;
            Color textColor;

            switch (tier)
            {
                default:
                case 1:
                    borderColor = Hex("#2E7D32");
                    glowSmallColor = Hex("#4CAF50");
                    glowBigColor = Hex("#A5D6A7");
                    textColor = Hex("#81C784");
                    break;

                case 2:
                    borderColor = Hex("#1565C0");
                    glowSmallColor = Hex("#2196F3");
                    glowBigColor = Hex("#90CAF9");
                    textColor = Hex("#64B5F6");
                    break;

                case 3:
                    borderColor = Hex("#6A1B9A");
                    glowSmallColor = Hex("#9C27B0");
                    glowBigColor = Hex("#CE93D8");
                    textColor = Hex("#BA68C8");
                    break;

                case 4:
                    borderColor = Hex("#EF6C00");
                    glowSmallColor = Hex("#FF9800");
                    glowBigColor = Hex("#FFCC80");
                    textColor = Hex("#FFB74D");
                    break;

                case 5:
                    borderColor = Hex("#B71C1C");
                    glowSmallColor = Hex("#F44336");
                    glowBigColor = Hex("#EF9A9A");
                    textColor = Hex("#E57373");
                    break;
            }

            border.color = borderColor;

            glowSmall.color = glowSmallColor;

            glowBig.color = glowBigColor;

            badge.color = borderColor;

            title.color = textColor;
        }

        private Color Hex(string hex)
        {
            Color color;

            ColorUtility.TryParseHtmlString(hex, out color);

            return color;
        }
    }
}