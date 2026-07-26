using UnityEngine;

namespace SkyOfFreedom.Data
{
    [CreateAssetMenu(fileName = "License", menuName = "Sky of Freedom/Data/License")]
    public class LicenseSO : DataSO
    {
        [Header("General")]
        [SerializeField] private string licenseName;
        [SerializeField, TextArea] private string description;

        [Header("Unlock")]
        [SerializeField] private ComponentSO unlockedComponent;

        [Header("Requirements")]
        [SerializeField] private int requiredFactoryLevel;
        [SerializeField] private int purchaseCost;

        public string LicenseName => licenseName;
        public string Description => description;
        public ComponentSO UnlockedComponent => unlockedComponent;
        public int RequiredFactoryLevel => requiredFactoryLevel;
        public int PurchaseCost => purchaseCost;

        public void SetData(
            string id,
            string licenseName,
            string description,
            ComponentSO unlockedComponent,
            ResearchSO requiredResearch,
            int requiredFactoryLevel,
            int purchaseCost)
        {
            SetID(id);

            this.licenseName = licenseName;
            this.description = description;
            this.unlockedComponent = unlockedComponent;
            this.requiredFactoryLevel = requiredFactoryLevel;
            this.purchaseCost = purchaseCost;
        }
    }
}