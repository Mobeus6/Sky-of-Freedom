using UnityEngine;

namespace SkyOfFreedom.Data
{
    [CreateAssetMenu(menuName = "Sky of Freedom/Data/License")]
    public class LicenseSO : DataSO
    {
        [SerializeField] private string licenseName;
        [SerializeField] private string description;
        [SerializeField] private ComponentSO unlockedComponent;
        [SerializeField] private ResearchSO requiredResearch;
        [SerializeField] private int requiredFactoryLevel;
        [SerializeField] private int purchaseCost;

        public string LicenseName => licenseName;
        public string Description => description;
        public ComponentSO UnlockedComponent => unlockedComponent;
        public ResearchSO RequiredResearch => requiredResearch;
        public int RequiredFactoryLevel => requiredFactoryLevel;
        public int PurchaseCost => purchaseCost;

#if UNITY_EDITOR
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
            this.requiredResearch = requiredResearch;
            this.requiredFactoryLevel = requiredFactoryLevel;
            this.purchaseCost = purchaseCost;
        }
#endif
    }
}