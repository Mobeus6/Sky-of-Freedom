using UnityEngine;
using SkyOfFreedom.Data;

namespace SkyOfFreedom.Contracts
{
    [CreateAssetMenu(fileName = "CTR-", menuName = "Sky of Freedom/Contracts/Contract")]
    public class ContractSO : DataSO
    {
        [Header("General")]
        [SerializeField] private string contractName;
        [SerializeField, TextArea] private string description;

        [Header("Target")]
        [SerializeField] private ContractTargetType targetType;
        [SerializeField] private ComponentSO component;
        [SerializeField] private DroneModelSO droneModel;

        [Header("Requirements")]
        [SerializeField] private ResearchSO requiredResearch;
        [SerializeField] private LicenseSO requiredLicense;
        [SerializeField] private int requiredReputation;

        [Header("Reputation")]
        [SerializeField] private int reputationReward;
        [SerializeField] private int reputationPenalty;
        [SerializeField] private int minQuantity = 1;
        [SerializeField] private int maxQuantity = 5;
        public string TargetID
        {
            get
            {
                switch (targetType)
                {
                    case ContractTargetType.Component:
                        return component != null ? component.ID : string.Empty;

                    case ContractTargetType.Drone:
                        return droneModel != null ? droneModel.ID : string.Empty;

                    default:
                        return string.Empty;
                }
            }
        }
        public int MinQuantity => minQuantity;
        public int MaxQuantity => maxQuantity;

        public string ContractName => contractName;
        public string Description => description;

        public ContractTargetType TargetType => targetType;
        public ComponentSO Component => component;
        public DroneModelSO DroneModel => droneModel;

        public ResearchSO RequiredResearch => requiredResearch;
        public LicenseSO RequiredLicense => requiredLicense;
        public int RequiredReputation => requiredReputation;

        public int ReputationReward => reputationReward;
        public int ReputationPenalty => reputationPenalty;

#if UNITY_EDITOR
        public void SetData(
            string id,
            string contractName,
            string description,
            ContractTargetType targetType,
            ComponentSO component,
            DroneModelSO droneModel,
            ResearchSO requiredResearch,
            LicenseSO requiredLicense,
            int requiredReputation,
            int reputationReward,
            int reputationPenalty)
        {
            SetID(id);

            this.contractName = contractName;
            this.description = description;

            this.targetType = targetType;
            this.component = component;
            this.droneModel = droneModel;

            this.requiredResearch = requiredResearch;
            this.requiredLicense = requiredLicense;
            this.requiredReputation = requiredReputation;

            this.reputationReward = reputationReward;
            this.reputationPenalty = reputationPenalty;
        }
#endif
    }
}