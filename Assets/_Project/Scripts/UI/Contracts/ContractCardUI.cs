using System;
using SkyOfFreedom.Contracts;
using SkyOfFreedom.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI.Contracts
{
    public class ContractCardUI : MonoBehaviour
    {
        [Header("Contract")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text contractNameText;
        [SerializeField] private TMP_Text deadlineText;

        [Header("Rewards")]
        [SerializeField] private TMP_Text financeRewardText;
        [SerializeField] private TMP_Text reputationRewardText;

        [Header("Tier Visual")]
        [SerializeField] private CardTierVisual tierVisual;

        [Header("Button")]
        [SerializeField] private Button button;

        private ContractInstance contract;
        private Action<ContractInstance> onClicked;

        public ContractInstance Contract => contract;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.AddListener(OnClicked);
            }
            else
            {
                Debug.LogError(
                    "ContractCardUI: Button component was not found.",
                    this);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
            }
        }

        public void Setup(
            ContractInstance contractInstance,
            Action<ContractInstance> clickCallback)
        {
            if (contractInstance == null)
            {
                return;
            }

            contract = contractInstance;
            onClicked = clickCallback;

            UpdateIcon();
            UpdateName();
            UpdateDeadline();
            UpdateRewards();
            UpdateTierVisual();
        }

        public void Setup(
            ContractInstance contractInstance)
        {
            Setup(
                contractInstance,
                null);
        }

        private void OnClicked()
        {
            if (contract == null)
            {
                return;
            }

            onClicked?.Invoke(contract);
        }

        private void UpdateIcon()
        {
            if (icon == null || contract.Template == null)
            {
                return;
            }

            icon.sprite = contract.Template.Icon;
            icon.enabled = contract.Template.Icon != null;
        }

        private void UpdateName()
        {
            if (contractNameText == null ||
                contract.Template == null)
            {
                return;
            }

            contractNameText.text =
                contract.Template.ContractName;
        }

        private void UpdateDeadline()
        {
            if (deadlineText == null)
            {
                return;
            }

            float remainingHours =
                Mathf.Max(
                    0f,
                    (float)(
                        contract.ExpireAt -
                        DateTime.UtcNow).TotalHours);

            int days =
                Mathf.FloorToInt(
                    remainingHours / 24f);

            int hours =
                Mathf.FloorToInt(
                    remainingHours % 24f);

            deadlineText.text =
                $"{days} D {hours} H";
        }

        private void UpdateRewards()
        {
            if (financeRewardText != null)
            {
                financeRewardText.text =
                    contract.Reward.ToString("N0");
            }

            if (reputationRewardText != null &&
                contract.Template != null)
            {
                reputationRewardText.text =
                    contract.Template.ReputationReward
                        .ToString("N0");
            }
        }

        private void UpdateTierVisual()
        {
            if (tierVisual == null)
            {
                return;
            }

            int tier =
                GetContractTier();

            tierVisual.SetTier(tier);
        }

        private int GetContractTier()
        {
            if (contract == null ||
                contract.Template == null)
            {
                return 1;
            }

            switch (contract.Template.TargetType)
            {
                case ContractTargetType.Drone:

                    if (contract.Template.DroneModel != null)
                    {
                        return (int)
                            contract.Template.DroneModel.DroneTier;
                    }

                    break;

                case ContractTargetType.Component:

                    if (contract.Template.Component != null)
                    {
                        return (int)
                            contract.Template.Component.Tier;
                    }

                    break;
            }

            return 1;
        }
    }
}