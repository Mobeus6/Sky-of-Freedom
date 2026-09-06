using System;
using System.Threading.Tasks;

using SkyOfFreedom.Contracts;
using SkyOfFreedom.Data;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Production;
using SkyOfFreedom.Services;
using SkyOfFreedom.Warehouse;
using UnityEngine;

namespace SkyOfFreedom.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Managers")]
        [SerializeField] private DatabaseManager databaseManager;
        [SerializeField] private ProductionManager productionManager;
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private MarketManager marketManager;
        [SerializeField] private FactoryManager factoryManager;
        [SerializeField] private ResearchManager researchManager;
        [SerializeField] private WarehouseManager warehouseManager;
        [SerializeField] private LicenseManager licenseManager;
        [SerializeField] private ContractManager contractManager;
        [SerializeField]
        private FactoryStatisticsManager factoryStatisticsManager;

        [Header("Player Data")]
        [SerializeField] private PlayerStartConfigSO playerStartConfig;

        private readonly AuthenticationService authenticationService =
            new AuthenticationService();

        private readonly PublicIdService publicIdService =
            new PublicIdService();

        private readonly CloudSaveService cloudSaveService =
            new CloudSaveService();

        private PlayerDataService playerDataService;

        private bool isGameReady;
        private bool isSaving;
        private bool hasPendingSave;
        private bool isDestroyed;
        private bool managersStarted;

        public bool IsGameReady => isGameReady;

        public string PublicId =>
            playerDataService?.CurrentData?.Account?.PublicId
            ?? string.Empty;

        public PlayerDataService PlayerData => playerDataService;
        public DatabaseManager Database => databaseManager;
        public ProductionManager Production => productionManager;
        public WarehouseManager Warehouse => warehouseManager;
        public TimeManager Time => timeManager;
        public EconomyManager Economy => economyManager;
        public MarketManager Market => marketManager;
        public FactoryManager Factory => factoryManager;
        public ResearchManager Research => researchManager;
        public LicenseManager License => licenseManager;
        public ContractManager Contracts => contractManager;
        public FactoryStatisticsManager Statistics =>
            factoryStatisticsManager;

        public float LoadingProgress { get; private set; }
        public string LoadingStatus { get; private set; } = "Preparing…";
        public bool IsLoading { get; private set; }
        public bool HasLoadingError { get; private set; }
        public bool CanRetryLoading { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (Instance == this)
            {
                _ = StartGameAsync();
            }
        }

        public async Task StartGameAsync()
        {
            if (isDestroyed || isGameReady || IsLoading ||
                (HasLoadingError && !CanRetryLoading))
            {
                return;
            }

            IsLoading = true;
            HasLoadingError = false;
            CanRetryLoading = false;
            bool networkStage = false;
            SetLoadingStage(0f, "Initializing services…");

            try
            {
                if (playerStartConfig == null)
                {
                    throw new InvalidOperationException(
                        "PlayerStartConfig is not assigned to GameManager."
                    );
                }

                if (economyManager == null || factoryManager == null ||
                    warehouseManager == null || licenseManager == null)
                {
                    throw new InvalidOperationException(
                        "Required managers are not assigned to GameManager."
                    );
                }

                playerDataService = new PlayerDataService(playerStartConfig);
                networkStage = true;

                await authenticationService.InitializeAsync();
                if (isDestroyed) return;

                SetLoadingStage(0.2f, "Signing in…");
                await authenticationService.SignInAsGuestAsync();
                if (isDestroyed) return;

                SetLoadingStage(0.4f, "Retrieving player ID…");
                string playerId = authenticationService.PlayerId;
                string publicId =
                    await publicIdService.GetOrCreatePublicIdAsync();
                if (isDestroyed) return;

                SetLoadingStage(0.6f, "Loading progress…");
                await LoadOrCreatePlayerDataAsync(playerId, publicId);
                if (isDestroyed) return;

                networkStage = false;
                SetLoadingStage(0.8f, "Preparing game…");

                // Give the loading UI a frame before synchronous initialization.
                await Task.Yield();
                if (isDestroyed) return;

                managersStarted = true;
                InitializeManagers();
                playerDataService.ApplyToManagers(this);
                SubscribeToSaveEvents();

                isGameReady = true;
                SetLoadingStage(1f, "Ready!");
                Debug.Log($"Game ready. PublicId: {PublicId}", this);
            }
            catch (Exception exception)
            {
                if (!isDestroyed)
                {
                    HasLoadingError = true;
                    CanRetryLoading = networkStage;
                    LoadingStatus = networkStage
                        ? "Unable to load data. Check your connection and try again."
                        : "Unable to prepare the game. Restart the game. If the problem persists, contact support.";
                    Debug.LogException(exception, this);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SetLoadingStage(float progress, string status)
        {
            LoadingProgress = progress;
            LoadingStatus = status;
        }

        private void OnDestroy()
        {
            isDestroyed = true;

            if (Instance != this)
            {
                return;
            }

            isGameReady = false;
            hasPendingSave = false;

            UnsubscribeFromSaveEvents();

            if (managersStarted)
            {
                ShutdownManagers();
            }

            Instance = null;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                RequestSave();
            }
        }

        private async Task LoadOrCreatePlayerDataAsync(
            string playerId,
            string publicId)
        {
            PlayerData loadedData =
                await cloudSaveService.LoadPlayerDataAsync();

            if (isDestroyed)
            {
                return;
            }

            if (!authenticationService.IsSignedIn ||
                authenticationService.PlayerId != playerId)
            {
                throw new InvalidOperationException(
                    "The player changed while loading player data."
                );
            }

            bool needsSave;

            if (loadedData != null)
            {
                if (loadedData.Account != null &&
                    !string.IsNullOrEmpty(loadedData.Account.PlayerId) &&
                    loadedData.Account.PlayerId != playerId)
                {
                    throw new InvalidOperationException(
                        "Loaded player data belongs to another player."
                    );
                }

                playerDataService.SetData(loadedData);

                PlayerAccountData account =
                    playerDataService.CurrentData.Account;

                needsSave =
                    account.PlayerId != playerId ||
                    account.PublicId != publicId;

                account.PlayerId = playerId;
                account.PublicId = publicId;
            }
            else
            {
                PlayerData newData =
                    playerDataService.CreateNewPlayerData(playerId);

                newData.Account.PublicId = publicId;
                needsSave = true;
            }

            if (needsSave)
            {
                playerDataService.CurrentData.Account.LastSaveAtUtc =
                    DateTime.UtcNow.ToString("O");

                await cloudSaveService.SavePlayerDataAsync(
                    playerDataService.CurrentData
                );
            }
        }

        private void InitializeManagers()
        {
            databaseManager?.Initialize();
            productionManager?.Initialize();
            economyManager?.Initialize();
            marketManager?.Initialize();
            factoryManager?.Initialize();
            researchManager?.Initialize();
            warehouseManager?.Initialize();
            licenseManager?.Initialize();
            contractManager?.Initialize();
            factoryStatisticsManager?.Initialize();
        }

        private void ShutdownManagers()
        {
            factoryStatisticsManager?.Shutdown();
            contractManager?.Shutdown();
            productionManager?.Shutdown();
            factoryManager?.Shutdown();
            economyManager?.Shutdown();
            marketManager?.Shutdown();
            warehouseManager?.Shutdown();
            licenseManager?.Shutdown();
            databaseManager?.Shutdown();
        }

        private void SubscribeToSaveEvents()
        {
            economyManager.OnMoneyChanged += OnMoneyChanged;
            economyManager.OnReputationChanged += OnReputationChanged;
            factoryManager.OnFactoryLevelChanged += OnFactoryLevelChanged;
            factoryManager.OnZoneLevelChanged += OnZoneLevelChanged;
            warehouseManager.OnItemChanged += OnWarehouseItemChanged;
            licenseManager.OnLicensePurchased += OnLicensePurchased;
        }

        private void UnsubscribeFromSaveEvents()
        {
            if (economyManager != null)
            {
                economyManager.OnMoneyChanged -= OnMoneyChanged;
                economyManager.OnReputationChanged -= OnReputationChanged;
            }

            if (factoryManager != null)
            {
                factoryManager.OnFactoryLevelChanged -= OnFactoryLevelChanged;
                factoryManager.OnZoneLevelChanged -= OnZoneLevelChanged;
            }

            if (warehouseManager != null)
            {
                warehouseManager.OnItemChanged -= OnWarehouseItemChanged;
            }

            if (licenseManager != null)
            {
                licenseManager.OnLicensePurchased -= OnLicensePurchased;
            }
        }

        private void OnMoneyChanged(long money)
        {
            RequestSave();
        }

        private void OnReputationChanged(int reputation)
        {
            RequestSave();
        }

        private void OnFactoryLevelChanged(int level)
        {
            RequestSave();
        }

        private void OnZoneLevelChanged(
            FactoryZoneType zoneType,
            int level)
        {
            RequestSave();
        }

        private void OnWarehouseItemChanged(
            string itemId,
            int quantity)
        {
            RequestSave();
        }

        private void OnLicensePurchased(LicenseSO license)
        {
            RequestSave();
        }

        private void RequestSave()
        {
            if (isDestroyed ||
                !isGameReady ||
                playerDataService == null ||
                !playerDataService.HasData)
            {
                return;
            }

            hasPendingSave = true;

            if (isSaving)
            {
                return;
            }

            _ = SavePendingDataAsync();
        }

        private async Task SavePendingDataAsync()
        {
            isSaving = true;

            try
            {
                while (hasPendingSave && !isDestroyed)
                {
                    hasPendingSave = false;

                    playerDataService.CaptureFromManagers(this);

                    await cloudSaveService.SavePlayerDataAsync(
                        playerDataService.CurrentData
                    );
                }
            }
            catch (Exception exception)
            {
                if (!isDestroyed)
                {
                    Debug.LogException(exception, this);
                }
            }
            finally
            {
                isSaving = false;
            }
        }
    }
}
