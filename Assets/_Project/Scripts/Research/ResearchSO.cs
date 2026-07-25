using UnityEngine;

namespace SkyOfFreedom.Data
{
    [CreateAssetMenu(fileName = "RES_", menuName = "Sky of Freedom/Research")]
    public class ResearchSO : DataSO
    {
        [Header("General")]
        [SerializeField] private string researchName;
        [SerializeField, TextArea(2, 4)] private string description;

        [Header("Classification")]
        [SerializeField] private ResearchBranch branch;
        [SerializeField] private ResearchCategory category;
        [SerializeField] private int tier = 1;

        [Header("Requirements")]
        [SerializeField] private int requiredFactoryLevel = 1;
        [SerializeField] private int cost = 1000;
        [SerializeField] private float researchTime = 60f;

        [Header("Dependencies")]
        [SerializeField] private ResearchSO[] prerequisites;

        [Header("Effects")]
        [SerializeField, TextArea(2, 4)] private string effectDescription;
        [SerializeField, TextArea(2, 4)] private string unlockDescription;

        #region Properties

        public string ResearchName => researchName;
        public string Description => description;

        public ResearchBranch Branch => branch;
        public ResearchCategory Category => category;
        public int Tier => tier;

        public int RequiredFactoryLevel => requiredFactoryLevel;
        public int Cost => cost;
        public float ResearchTime => researchTime;

        public ResearchSO[] Prerequisites => prerequisites;

        public string EffectDescription => effectDescription;
        public string UnlockDescription => unlockDescription;

        #endregion
        public void SetData(
    string id,
    string researchName,
    string description,
    ResearchBranch branch,
    ResearchCategory category,
    int tier,
    int requiredFactoryLevel,
    int cost,
    float researchTime,
    ResearchSO[] prerequisites,
    string effectDescription,
    string unlockDescription)
        {
#if UNITY_EDITOR
            SetID(id);
#endif

            this.researchName = researchName;
            this.description = description;

            this.branch = branch;
            this.category = category;
            this.tier = tier;

            this.requiredFactoryLevel = requiredFactoryLevel;
            this.cost = cost;
            this.researchTime = researchTime;

            this.prerequisites = prerequisites;

            this.effectDescription = effectDescription;
            this.unlockDescription = unlockDescription;
        }
    }
}