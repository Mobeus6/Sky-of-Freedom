using System;
using SkyOfFreedom.Contracts;
using SkyOfFreedom.Data;
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

        private PlayerDataService playerDataService;

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

        private async void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);

            if (playerStartConfig == null)
            {
                Debug.LogError(
                    "PlayerStartConfig is not assigned to GameManager.",
                    this
                );

                return;
            }

            playerDataService = new PlayerDataService(
                playerStartConfig
            );

            try
            {
                await authenticationService.InitializeAsync();

                await authenticationService.SignInAsGuestAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return;
            }

            InitializeManagers();

            SeedTemporaryTestInventory();
        }

        private void OnDestroy()
        {
            ShutdownManagers();
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

        private void SeedTemporaryTestInventory()
        {
            if (warehouseManager == null)
            {
                Debug.LogError(
                    "WarehouseManager is not assigned to GameManager.",
                    this
                );

                return;
            }

            warehouseManager.AddItem("MAT-PLASTIC", 15);
            warehouseManager.AddItem("MAT-ALUMINUM", 15);
            warehouseManager.AddItem("MAT-CARBON", 15);
            warehouseManager.AddItem("MAT-COPPER", 15);
            warehouseManager.AddItem("MAT-PCB", 15);
            warehouseManager.AddItem("MAT-BATTERY-CELL", 15);
            warehouseManager.AddItem("MAT-GLASS", 15);
            warehouseManager.AddItem("MAT-STEEL", 15);
            warehouseManager.AddItem("MAT-MAGNET", 15);
            warehouseManager.AddItem("MAT-MICROCHIP", 15);
            warehouseManager.AddItem("MAT-SILICONE", 15);

            if (databaseManager == null ||
                databaseManager.Database == null)
            {
                Debug.LogError(
                    "GameDatabase is not available.",
                    this
                );

                return;
            }

            foreach (ComponentSO component
                     in databaseManager.Database.Components)
            {
                if (component == null)
                {
                    continue;
                }

                warehouseManager.AddItem(component.ID, 10);
            }
        }
    }
}