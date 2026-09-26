using System;
using System.Threading.Tasks;

using SkyOfFreedom.Contracts;
using SkyOfFreedom.Data;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Production;
using SkyOfFreedom.Services;
using SkyOfFreedom.Warehouse;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private LocalSaveStore localSaves;
        private bool localSaveDirty;
        private float nextLocalSaveRetry;
        public bool HasLocalSaveError { get; private set; }
        public bool HasCloudSaveConflict { get; private set; }

        private bool isGameReady;
        private bool isSaving;
        private Task saveTask = Task.CompletedTask;
        public bool IsAccountTransition { get; private set; }
        private bool hasPendingSave;
        private bool isDestroyed;
        private bool managersStarted;
        private bool advancingOffline;
        private bool applicationPaused;
        private float nextProductionCheckpoint;
        private float nextSaveRetry;
        private const float ProductionCheckpointSeconds = 15f;

        public bool IsGameReady => isGameReady;
        public DateTime? ProductionPausedAtUtc { get; private set; }

        public bool HasSaveError { get; private set; }
        public string SaveStatusText => HasCloudSaveConflict
            ? "Save conflict. Restart to restore progress."
            : HasLocalSaveError ? "Local backup failed. Check device storage."
            : HasSaveError
            ? "Not saved. Retrying…"
            : (isSaving || hasPendingSave ? "Saving…" : string.Empty);

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
            localSaves = new LocalSaveStore(System.IO.Path.Combine(Application.persistentDataPath, "SaveRecovery"));
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (Instance == this)
            {
                _ = StartGameAsync();
            }
        }

        private void Update()
        {
            if (!isGameReady || isDestroyed || IsAccountTransition) return;
            float now = UnityEngine.Time.unscaledTime;
            if (now >= nextProductionCheckpoint)
            {
                nextProductionCheckpoint = now + ProductionCheckpointSeconds;
                if ((productionManager != null && productionManager.HasRunningTasks) ||
                    (researchManager != null && researchManager.HasActiveResearch()))
                    RequestSave();
            }
            if (hasPendingSave && !isSaving && !HasCloudSaveConflict && now >= nextSaveRetry)
                saveTask = SavePendingDataAsync();
        }

        private void LateUpdate()
        {
            // Run after complete synchronous operations, including every resource/statistics event.
            // Unlike cloud uploads this is not blocked by an in-flight or failed network request.
            if (isGameReady && !IsAccountTransition && localSaveDirty &&
                UnityEngine.Time.unscaledTime >= nextLocalSaveRetry)
                CaptureLocalSnapshot();
        }

        private PlayerData CaptureLocalSnapshot()
        {
            playerDataService.CaptureFromManagers(this);
            string json = JsonUtility.ToJson(playerDataService.CurrentData);
            PersistLocal(json, true);
            return JsonUtility.FromJson<PlayerData>(json);
        }

        private void PersistLocal(string json, bool pending)
        {
            try
            {
                localSaves.Write(playerDataService.CurrentData.Account.PlayerId, json,
                    cloudSaveService.ConfirmedFingerprint, pending);
                localSaveDirty = false;
                HasLocalSaveError = false;
                nextLocalSaveRetry = 0;
            }
            catch (Exception exception)
            {
                localSaveDirty = true;
                HasLocalSaveError = true;
                nextLocalSaveRetry = UnityEngine.Time.unscaledTime + 5f;
                Debug.LogException(exception, this);
            }
        }

        private async Task UploadSnapshotAsync(PlayerData snapshot)
        {
            // The snapshot is independent of CurrentData while gameplay continues.
            string json = JsonUtility.ToJson(snapshot);
            await cloudSaveService.SavePlayerDataAsync(snapshot);
            try { localSaves.Acknowledge(snapshot.Account.PlayerId, json); }
            catch (Exception exception)
            {
                HasLocalSaveError = true;
                localSaveDirty = true;
                nextLocalSaveRetry = UnityEngine.Time.unscaledTime + 5f;
                Debug.LogException(exception, this);
            }
        }

        private void OnApplicationQuit()
        {
            if (Instance == this && isGameReady && !IsAccountTransition)
                CaptureLocalSnapshot();
        }

        public async Task StartGameAsync()
        {
            if (isDestroyed || IsAccountTransition || isGameReady || IsLoading ||
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

                if (databaseManager == null || timeManager == null ||
                    researchManager == null || contractManager == null ||
                    factoryStatisticsManager == null ||
                    economyManager == null || factoryManager == null ||
                    productionManager == null ||
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

                DateTime resumeAt = DateTime.UtcNow;
                AdvanceOfflineProgress(
                    OfflineProgress.Elapsed(playerDataService.CurrentData.Production.LastProcessedAtUtc, resumeAt),
                    OfflineProgress.Elapsed(playerDataService.CurrentData.Research.LastProcessedAtUtc, resumeAt));
                isGameReady = true;
                if (applicationPaused) ProductionPausedAtUtc = resumeAt;
                // Commit the fully restored state before any asynchronous upload or gameplay.
                CaptureLocalSnapshot();
                RequestSave();
                SetLoadingStage(1f, "Ready!");
                Debug.Log($"Game ready. PublicId: {PublicId}", this);
            }
            catch (Exception exception)
            {
                if (!isDestroyed)
                {
                    isGameReady = false;
                    HasLoadingError = true;
                    CanRetryLoading = networkStage;
                    LoadingStatus = networkStage
                        ? "Unable to load data. Check your connection and try again."
                        : "Unable to prepare the game. Restart the game. If the problem persists, contact support.";
                    if (exception is System.IO.IOException || exception is UnauthorizedAccessException)
                        LoadingStatus = "Unable to read or write the local backup. Check device storage. Recovery files were kept.";
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
            applicationPaused = pauseStatus;
            if (!isGameReady || IsAccountTransition) return;
            if (pauseStatus && !ProductionPausedAtUtc.HasValue)
                ProductionPausedAtUtc = DateTime.UtcNow;
            if (!pauseStatus && ProductionPausedAtUtc.HasValue)
            {
                double elapsed = Math.Max(0, (DateTime.UtcNow - ProductionPausedAtUtc.Value).TotalSeconds);
                ProductionPausedAtUtc = null;
                AdvanceOfflineProgress(elapsed, elapsed);
                RequestSave();
            }
            if (pauseStatus)
            {
                CaptureLocalSnapshot();
                // Try again immediately when the app is backgrounded.
                nextSaveRetry = 0f;
                RequestSave();
            }
        }

        private void AdvanceOfflineProgress(double productionSeconds, double researchSeconds)
        {
            advancingOffline = true;
            try
            {
                OfflineProgress.Advance(productionSeconds, researchSeconds,
                    researchManager.GetRemainingSeconds(),
                    productionManager.AdvanceOffline, researchManager.AdvanceTime);
            }
            finally { advancingOffline = false; }
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
            if (loadedData?.Account != null && !string.IsNullOrEmpty(loadedData.Account.PlayerId) &&
                loadedData.Account.PlayerId != playerId)
                throw new InvalidOperationException("Cloud progress belongs to another account.");

            LocalSaveRecord local = localSaves.Read(playerId);
            bool useLocal = local != null && local.Pending &&
                LocalSaveStore.Fingerprint(local.Json) != cloudSaveService.ConfirmedFingerprint;
            if (LocalSaveStore.NeedsChoice(local, cloudSaveService.LastLoadedJson))
            {
                LoadingStatus = "Restoring progress…";
                PlayerData localData = ReadLocalPlayer(local, playerId);
                useLocal = SkyOfFreedom.Services.SaveProgressSelection.PreferLocal(localData, loadedData);
                if (isDestroyed) return;
                // Keep both candidates before replacing the active recovery slots.
                localSaves.Archive(local);
                if (cloudSaveService.LastLoadedJson != null)
                    localSaves.Archive(new LocalSaveRecord
                    {
                        Sequence = 1, PlayerId = playerId, Pending = false,
                        BaseFingerprint = cloudSaveService.ConfirmedFingerprint, Json = cloudSaveService.LastLoadedJson
                    });
            }
            if (useLocal) loadedData = ReadLocalPlayer(local, playerId);

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
                    useLocal ||
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

            }
            // Recovery is committed locally before initialization/offline production.
            // Cloud upload occurs only after managers have successfully restored this snapshot.
            localSaves.Write(playerId, JsonUtility.ToJson(playerDataService.CurrentData),
                cloudSaveService.ConfirmedFingerprint, needsSave);
        }

        private static PlayerData ReadLocalPlayer(LocalSaveRecord record, string playerId)
        {
            PlayerData data;
            try { data = JsonUtility.FromJson<PlayerData>(record.Json); }
            catch (ArgumentException exception)
            {
                throw new System.IO.InvalidDataException("Invalid local progress. Backup was preserved.", exception);
            }
            if (data?.Account == null || data.Account.PlayerId != playerId || data.Version != 1)
                throw new System.IO.InvalidDataException("Invalid or unsupported local progress. Backup was preserved.");
            return data;
        }


        public Task SignOutAccountAsync()
        {
            if (!authenticationService.IsGoogleLinked)
                throw new InvalidOperationException("Only a linked account can sign out.");
            return ChangeAccountAsync(null);
        }

        public Task RestoreGoogleAccountAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ArgumentException("Google credential is missing.");
            return ChangeAccountAsync(idToken);
        }

        private async Task ChangeAccountAsync(string googleToken)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (developerResetBusy) throw new InvalidOperationException("Wait for the zone reset to finish.");
#endif
            if (IsAccountTransition || !isGameReady || isDestroyed || HasCloudSaveConflict)
                throw new InvalidOperationException("Account change is not available.");

            Scene menuScene = SceneManager.GetActiveScene();
            if (menuScene.name != "MainMenu" || menuScene.buildIndex < 0)
                throw new InvalidOperationException("Account changes require MainMenu in the build scene list.");

            // A scene reload must replace all managers, not retain external state.
            MonoBehaviour[] managers = {
                databaseManager, productionManager, timeManager, economyManager,
                marketManager, factoryManager, researchManager, warehouseManager,
                licenseManager, contractManager, factoryStatisticsManager
            };
            foreach (MonoBehaviour manager in managers)
            {
                if (manager != null && !manager.transform.IsChildOf(transform))
                    throw new InvalidOperationException(
                        "All managers must belong to the persistent GameManager hierarchy.");
            }

            // Also suspend helper behaviours such as ResearchRunner during the save.
            managers = GetComponentsInChildren<MonoBehaviour>(true);
            string oldPlayerId = authenticationService.PlayerId;
            IsAccountTransition = true;
            isGameReady = false;
            hasPendingSave = false;
            float previousTimeScale = UnityEngine.Time.timeScale;
            UnityEngine.Time.timeScale = 0f;
            bool[] enabledStates = new bool[managers.Length];
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i] == null) continue;
                enabledStates[i] = managers[i].enabled;
                managers[i].enabled = false;
            }

            bool accountChanged = false;
            try
            {
                // Await the existing request before making a final save.
                await saveTask;
                if (isDestroyed) throw new InvalidOperationException("GameManager was destroyed.");
                if (!authenticationService.IsSignedIn ||
                    authenticationService.PlayerId != oldPlayerId)
                    throw new InvalidOperationException("The active player changed unexpectedly.");

                await UploadSnapshotAsync(CaptureLocalSnapshot());
                if (isDestroyed) throw new InvalidOperationException("GameManager was destroyed.");

                if (googleToken == null)
                {
                    authenticationService.SignOutAndClearSession();
                }
                else
                {
                    await authenticationService.SignInToExistingGoogleAccountAsync(googleToken);
                }

                accountChanged = true;
                UnsubscribeFromSaveEvents();
                if (managersStarted)
                {
                    ShutdownManagers();
                    managersStarted = false;
                }
                playerDataService.Clear();

                // Retire this instance before the fresh scene creates its replacement.
                Instance = null;
                Destroy(gameObject);
                while (!isDestroyed) await Task.Yield();
                UnityEngine.Time.timeScale = previousTimeScale;
                AsyncOperation reload = SceneManager.LoadSceneAsync(menuScene.buildIndex);
                if (reload == null)
                    throw new InvalidOperationException("Could not reload MainMenu.");
                await reload;
            }
            catch (Exception exception)
            {
                if (exception is Unity.Services.CloudSave.CloudSaveConflictException)
                    HasCloudSaveConflict = true;
                bool oldAccountRestored = !accountChanged &&
                    authenticationService.IsSignedIn &&
                    authenticationService.PlayerId == oldPlayerId;

                if (!isDestroyed && oldAccountRestored)
                {
                    for (int i = 0; i < managers.Length; i++)
                        if (managers[i] != null) managers[i].enabled = enabledStates[i];
                    IsAccountTransition = false;
                    isGameReady = true;
                }
                // Otherwise keep saving/play blocked until the game is restarted.
                throw;
            }
            finally
            {
                UnityEngine.Time.timeScale = previousTimeScale;
            }
        }

        private void InitializeManagers()
        {
            databaseManager?.Initialize();
            economyManager?.Initialize();
            factoryManager?.Initialize();
            researchManager?.Initialize();
            warehouseManager?.Initialize();
            marketManager?.Initialize();
            licenseManager?.Initialize();
            productionManager?.Initialize();
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
            productionManager.OnProductionChanged += RequestSave;
            contractManager.OnContractsChanged += RequestSave;
            factoryStatisticsManager.OnStatisticsChanged += RequestSave;
            researchManager.OnResearchStarted += OnResearchChanged;
            researchManager.OnResearchCancelled += OnResearchChanged;
            researchManager.OnResearchCompleted += OnResearchChanged;
            researchManager.OnResearchUnlocked += OnResearchChanged;
        }

        private void UnsubscribeFromSaveEvents()
        {
            if (factoryStatisticsManager != null)
                factoryStatisticsManager.OnStatisticsChanged -= RequestSave;
            if (researchManager != null)
            {
                researchManager.OnResearchStarted -= OnResearchChanged;
                researchManager.OnResearchCancelled -= OnResearchChanged;
                researchManager.OnResearchCompleted -= OnResearchChanged;
                researchManager.OnResearchUnlocked -= OnResearchChanged;
            }
            if (contractManager != null)
                contractManager.OnContractsChanged -= RequestSave;
            if (productionManager != null)
                productionManager.OnProductionChanged -= RequestSave;
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

        private void OnResearchChanged(ResearchSO research)
        {
            RequestSave();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool developerResetBusy;
        public string DeveloperResetBlockReason()
        {
            if (!isGameReady || IsAccountTransition || isDestroyed || applicationPaused)
                return "Wait for the game to finish loading.";
            if (isSaving || hasPendingSave || HasSaveError || HasLocalSaveError || HasCloudSaveConflict)
                return "Resolve save errors and wait for saving to finish first.";
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return "An internet connection is required.";
            if (productionManager == null || researchManager == null || warehouseManager == null || factoryManager == null)
                return "Managers are not ready.";
            foreach (var zone in productionManager.Zones)
                if (zone != null && zone.TaskCount > 0)
                    return "Finish or cancel all production and assembly tasks first.";
            if (researchManager.HasActiveResearch()) return "Finish the active research first.";
            var config = databaseManager?.Database?.WarehouseConfig;
            if (config == null) return "Warehouse configuration is missing.";
            long capacity = Math.Max(0, config.GetCapacity(1)) + (long)researchManager.GetStorageCapacityBonus();
            if (warehouseManager.CurrentCapacity > capacity)
                return "Empty some warehouse space first. Level 1 capacity: " + capacity;
            return string.Empty;
        }

        public async Task<string> DeveloperResetZonesAsync()
        {
            if (developerResetBusy) return "A reset is already running.";
            string blocked = DeveloperResetBlockReason();
            if (!string.IsNullOrEmpty(blocked)) return blocked;
            developerResetBusy = true;
            bool changed = false;
            try
            {
                // Capture and verify a separate, permanent pre-reset copy before any mutation.
                var before = CaptureLocalSnapshot();
                if (HasLocalSaveError) return "Backup failed. No levels changed.";
                string json = JsonUtility.ToJson(before, true);
                string folder = System.IO.Path.Combine(Application.persistentDataPath, "DeveloperBackups");
                System.IO.Directory.CreateDirectory(folder);
                string path = System.IO.Path.Combine(folder, "zones-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") +
                    "-" + Guid.NewGuid().ToString("N") + ".json");
                System.IO.File.WriteAllText(path, json);
                if (System.IO.File.ReadAllText(path) != json) throw new System.IO.IOException("Backup verification failed.");
                Debug.Log("Developer zone reset backup: " + path, this);
                changed = true;
                factoryManager.SetLevel(FactoryZoneType.Production, 1);
                factoryManager.SetLevel(FactoryZoneType.Assembly, 1);
                factoryManager.SetLevel(FactoryZoneType.Research, 1);
                factoryManager.SetLevel(FactoryZoneType.Warehouse, 1);
                nextSaveRetry = 0f;
                RequestSave();
                await saveTask;
                if (isDestroyed || IsAccountTransition || HasSaveError || HasLocalSaveError || HasCloudSaveConflict || hasPendingSave)
                    return "Levels reset locally, but saving is NOT confirmed. Keep this device online; do not use another device. Check save status.";
                return "Saved locally and to cloud. All four zones are level 1. Other progress unchanged.\nBackup: " + path;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return changed ? "Reset may be applied; saving is not confirmed. Check save status before continuing."
                    : "Reset cancelled: backup could not be created. No levels changed.";
            }
            finally { developerResetBusy = false; }
        }
#endif

        private void RequestSave()
        {
            if (isDestroyed || IsAccountTransition || advancingOffline ||
                !isGameReady ||
                playerDataService == null ||
                !playerDataService.HasData)
            {
                return;
            }

            hasPendingSave = true;
            localSaveDirty = true;

            if (isSaving || HasCloudSaveConflict || UnityEngine.Time.unscaledTime < nextSaveRetry)
            {
                return;
            }

            saveTask = SavePendingDataAsync();
        }

        private async Task SavePendingDataAsync()
        {
            isSaving = true;

            try
            {
                while (hasPendingSave && !isDestroyed && !IsAccountTransition)
                {
                    // Coalesce synchronous changes: ingredients + queue / product + warehouse.
                    await Task.Yield();
                    if (isDestroyed || IsAccountTransition) break;
                    hasPendingSave = false;

                    await UploadSnapshotAsync(CaptureLocalSnapshot());
                    HasSaveError = false;
                    nextSaveRetry = 0f;
                }
            }
            catch (Exception exception)
            {
                if (!isDestroyed)
                {
                    if (exception is Unity.Services.CloudSave.CloudSaveConflictException)
                        HasCloudSaveConflict = true;
                    hasPendingSave = true;
                    nextSaveRetry = UnityEngine.Time.unscaledTime + 5f;
                    HasSaveError = true;
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
