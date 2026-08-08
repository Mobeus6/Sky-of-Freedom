using TMPro;
using UnityEngine;

namespace SkyOfFreedom.UI
{
    public class ResearchBranchLabelUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;

        public RectTransform RectTransform =>
            (RectTransform)transform;

        public void Initialize(string label)
        {
            labelText.text = label;
        }
    }
}