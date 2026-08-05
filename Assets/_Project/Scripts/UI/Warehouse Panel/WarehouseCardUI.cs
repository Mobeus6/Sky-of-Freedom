using System;
using SkyOfFreedom.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class WarehouseCardUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private TMP_Text tierText;
        [SerializeField] private Button button;
        [SerializeField] private CardTierVisual visual;
        [SerializeField] private GameObject _sourceProduction;
        [SerializeField] private GameObject _sourceMarket;
        [SerializeField] private GameObject _sourceAssemble;

        private DataSO item;

        public event Action<DataSO> Selected;

        private void Awake()
        {
            button.onClick.AddListener(OnClicked);
        }

        public void Setup(DataSO data, int quantity)
        {
            item = data;

            switch (data)
            {
                case MaterialSO material:

                    icon.sprite = material.Icon;
                    nameText.text = material.MaterialName;
                    quantityText.text = quantity.ToString();

                    _sourceMarket.SetActive(true);
                    _sourceProduction.SetActive(false);
                    _sourceAssemble.SetActive(false);

                    tierText.gameObject.SetActive(true);
                    tierText.text = $"T{material.Tier}";

                    visual.SetTier(material.Tier);

                    break;

                case ComponentSO component:

                    icon.sprite = component.Icon;
                    nameText.text = component.Name;
                    quantityText.text = quantity.ToString();
                    _sourceMarket.gameObject.SetActive(false);
                    _sourceProduction.gameObject.SetActive(true);
                    _sourceAssemble.gameObject.SetActive(false);
                    tierText.gameObject.SetActive(true);
                    tierText.text = $"T{component.Tier}";
                    visual.SetTier(component.Tier);

                    break;

                case DroneModelSO drone:

                    icon.sprite = drone.Icon;
                    nameText.text = drone.Name;
                    quantityText.text = quantity.ToString();
                    _sourceMarket.gameObject.SetActive(false);
                    _sourceProduction.gameObject.SetActive(false);
                    _sourceAssemble.gameObject.SetActive(true);
                    tierText.gameObject.SetActive(true);
                    tierText.text = $"T{drone.Tier}";
                    visual.SetTier(drone.Tier);

                    break;
            }
        }

        private void OnClicked()
        {
            Selected?.Invoke(item);
        }
    }
}