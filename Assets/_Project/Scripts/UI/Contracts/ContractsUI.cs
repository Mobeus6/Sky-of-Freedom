using System.Collections.Generic;
using SkyOfFreedom.Contracts;
using SkyOfFreedom.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI.Contracts
{
    public class ContractsUI : MonoBehaviour
    {
        [Header("Contract Card")]
        [SerializeField] private ContractCardUI contractCardPrefab;

        [Header("Contract Detail")]
        [SerializeField] private ContractDetailUI contractDetailUI;

        [Header("Buttons")]
        [SerializeField] private Button availableButton;
        [SerializeField] private Button inProgressButton;
        [SerializeField] private Button completedButton;

        [Header("Button Highlights")]
        [SerializeField] private GameObject availableHighlight;
        [SerializeField] private GameObject inProgressHighlight;
        [SerializeField] private GameObject completedHighlight;

        [Header("Content Views")]
        [SerializeField] private GameObject availableView;
        [SerializeField] private GameObject inProgressView;
        [SerializeField] private GameObject completedView;

        [Header("Content Containers")]
        [SerializeField] private Transform availableContent;
        [SerializeField] private Transform inProgressContent;
        [SerializeField] private Transform completedContent;

        private ContractManager contractManager;

        private void Awake()
        {
            if (availableButton != null)
            {
                availableButton.onClick.AddListener(
                    ShowAvailable);
            }

            if (inProgressButton != null)
            {
                inProgressButton.onClick.AddListener(
                    ShowInProgress);
            }

            if (completedButton != null)
            {
                completedButton.onClick.AddListener(
                    ShowCompleted);
            }
        }

        private void Start()
        {
            contractManager =
                GameManager.Instance.Contracts;

            if (contractManager == null)
            {
                Debug.LogError(
                    "ContractsUI: ContractManager was not found in GameManager.",
                    this);

                return;
            }

            contractManager.OnContractsChanged -=
                OnContractsChanged;

            contractManager.OnContractsChanged +=
                OnContractsChanged;

            Invoke(
                nameof(Initialize),
                0f);

            if (contractDetailUI != null)
            {
                contractDetailUI.SetAcceptCallback(
                    AcceptContract);
            }
        }

        private void OnDestroy()
        {
            if (availableButton != null)
            {
                availableButton.onClick.RemoveListener(
                    ShowAvailable);
            }

            if (inProgressButton != null)
            {
                inProgressButton.onClick.RemoveListener(
                    ShowInProgress);
            }

            if (completedButton != null)
            {
                completedButton.onClick.RemoveListener(
                    ShowCompleted);
            }

            if (contractManager != null)
            {
                contractManager.OnContractsChanged -=
                    OnContractsChanged;
            }

            if (contractDetailUI != null)
            {
                contractDetailUI.SetAcceptCallback(
                    null);
            }
        }

        private void Initialize()
        {
            RefreshContracts();

            ShowAvailable();

            ShowFirstContract(
                contractManager.AvailableContracts);
        }

        private void OnContractsChanged()
        {
            RefreshContracts();

            if (inProgressView != null &&
                inProgressView.activeSelf)
            {
                ShowFirstContract(
                    contractManager.ActiveContracts);
            }
        }

        private void ShowAvailable()
        {
            availableView.SetActive(true);
            inProgressView.SetActive(false);
            completedView.SetActive(false);

            availableHighlight.SetActive(true);
            inProgressHighlight.SetActive(false);
            completedHighlight.SetActive(false);

            ShowFirstContract(
                contractManager.AvailableContracts);
        }

        private void AcceptContract(
            ContractInstance contract)
        {
            if (contract == null ||
                contractManager == null)
            {
                return;
            }

            contractManager.AcceptContract(
                contract);

            ShowInProgress();
        }

        private void ShowInProgress()
        {
            availableView.SetActive(false);
            inProgressView.SetActive(true);
            completedView.SetActive(false);

            availableHighlight.SetActive(false);
            inProgressHighlight.SetActive(true);
            completedHighlight.SetActive(false);

            ShowFirstContract(
                contractManager.ActiveContracts);
        }

        private void ShowCompleted()
        {
            availableView.SetActive(false);
            inProgressView.SetActive(false);
            completedView.SetActive(true);

            availableHighlight.SetActive(false);
            inProgressHighlight.SetActive(false);
            completedHighlight.SetActive(true);

            ShowFirstContract(
                contractManager.CompletedContracts);
        }

        public void RefreshContracts()
        {
            if (contractManager == null)
                return;

            ClearContent(
                availableContent);

            ClearContent(
                inProgressContent);

            ClearContent(
                completedContent);

            CreateCards(
                contractManager.AvailableContracts,
                availableContent);

            CreateCards(
                contractManager.ActiveContracts,
                inProgressContent);

            CreateCards(
                contractManager.CompletedContracts,
                completedContent);
        }

        private void CreateCards(
            IReadOnlyList<ContractInstance> contracts,
            Transform content)
        {
            if (contracts == null ||
                content == null)
            {
                return;
            }

            if (contractCardPrefab == null)
            {
                Debug.LogError(
                    "ContractsUI: Contract Card Prefab reference is missing.",
                    this);

                return;
            }

            foreach (ContractInstance contract in contracts)
            {
                if (contract == null)
                    continue;

                ContractCardUI card =
                    Instantiate(
                        contractCardPrefab,
                        content);

                card.Setup(
                    contract,
                    SelectContract);
            }
        }

        private void SelectContract(
            ContractInstance contract)
        {
            if (contract == null)
                return;

            if (contractDetailUI == null)
            {
                Debug.LogError(
                    "ContractsUI: Contract Detail UI reference is missing.",
                    this);

                return;
            }

            contractDetailUI.Show(
                contract);
        }

        private void ShowFirstContract(
            IReadOnlyList<ContractInstance> contracts)
        {
            if (contractDetailUI == null)
                return;

            if (contracts == null ||
                contracts.Count == 0)
            {
                contractDetailUI.Hide();
                return;
            }

            ContractInstance firstContract =
                contracts[0];

            if (firstContract == null)
            {
                contractDetailUI.Hide();
                return;
            }

            contractDetailUI.Show(
                firstContract);
        }

        private void ClearContent(
            Transform content)
        {
            if (content == null)
                return;

            for (int i = content.childCount - 1;
                 i >= 0;
                 i--)
            {
                Destroy(
                    content.GetChild(i).gameObject);
            }
        }
    }
}