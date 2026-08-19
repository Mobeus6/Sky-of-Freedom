using SkyOfFreedom.Contracts;
using SkyOfFreedom.Managers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI.Contracts
{
    public class ContractReadyNotificationUI : MonoBehaviour
    {
        [Header("Notification")]
        [SerializeField] private GameObject notificationContainer;

        [SerializeField] private TMP_Text notificationText;

        [SerializeField] private Button submitContractButton;

        [SerializeField] private Slider slider;

        [Header("Settings")]
        [SerializeField, Min(1f)]
        private float displayDuration = 15f;

        private ContractManager contractManager;

        private readonly Queue<ContractInstance>
            pendingContracts =
            new Queue<ContractInstance>();

        private ContractInstance currentContract;

        private float remainingTime;

        private void Start()
        {
            contractManager =
                GameManager.Instance != null
                    ? GameManager.Instance.Contracts
                    : null;

            if (contractManager == null)
            {
                Debug.LogError(
                    "ContractReadyNotificationUI: ContractManager was not found.",
                    this);

                return;
            }

            contractManager.OnContractReadyForSubmission -=
                OnContractReadyForSubmission;

            contractManager.OnContractReadyForSubmission +=
                OnContractReadyForSubmission;

            if (submitContractButton != null)
            {
                submitContractButton.onClick.AddListener(
                    OnSubmitContractClicked);
            }

            HideNotification();
        }

        private void OnDestroy()
        {
            if (contractManager != null)
            {
                contractManager.OnContractReadyForSubmission -=
                    OnContractReadyForSubmission;
            }

            if (submitContractButton != null)
            {
                submitContractButton.onClick.RemoveListener(
                    OnSubmitContractClicked);
            }
        }

        private void Update()
        {
            if (currentContract == null)
                return;

            remainingTime -= Time.deltaTime;

            if (slider != null)
            {
                slider.value =
                    Mathf.Clamp01(
                        remainingTime /
                        displayDuration);
            }

            if (remainingTime <= 0f)
            {
                FinishNotification();
            }
        }

        private void OnContractReadyForSubmission(
            ContractInstance contract)
        {
            if (contract == null)
                return;

            if (contract == currentContract)
                return;

            if (pendingContracts.Contains(contract))
                return;

            if (currentContract == null)
            {
                ShowNotification(contract);
                return;
            }

            pendingContracts.Enqueue(contract);
        }

        private void ShowNotification(
            ContractInstance contract)
        {
            currentContract =
                contract;

            remainingTime =
                displayDuration;

            if (notificationText != null)
            {
                notificationText.text =
                    "You can now submit your contract";
            }

            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value = 1f;
            }

            if (notificationContainer != null)
            {
                notificationContainer.SetActive(true);
            }
        }

        private void OnSubmitContractClicked()
        {
            if (currentContract == null)
                return;

            if (contractManager == null)
                return;

            contractManager.TrySubmitContract(
                currentContract);

            FinishNotification();
        }

        private void FinishNotification()
        {
            HideNotification();

            if (pendingContracts.Count > 0)
            {
                ContractInstance nextContract =
                    pendingContracts.Dequeue();

                if (nextContract != null &&
                    contractManager != null &&
                    contractManager.CanSubmitContract(
                        nextContract))
                {
                    ShowNotification(
                        nextContract);

                    return;
                }
            }
        }

        private void HideNotification()
        {
            currentContract = null;
            remainingTime = 0f;

            if (slider != null)
            {
                slider.value = 0f;
            }

            if (notificationContainer != null)
            {
                notificationContainer.SetActive(false);
            }
        }
    }
}