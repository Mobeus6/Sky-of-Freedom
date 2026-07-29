using SkyOfFreedom.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    [RequireComponent(typeof(Button))]
    public class CategoryButtonUI : MonoBehaviour
    {
        [SerializeField] private ProductionCategory category;

        private Button button;
        private ProductionPanelUI panel;

        public void Initialize(ProductionPanelUI productionPanel)
        {
            panel = productionPanel;

            button = GetComponent<Button>();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            panel.OpenCategory(category);
        }
    }
}