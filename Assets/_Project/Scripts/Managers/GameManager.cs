using SkyOfFreedom.Data;
using SkyOfFreedom.Production;
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
        [SerializeField] private FactoryManager factoryManager;
        [SerializeField] private ResearchManager researchManager;
        [SerializeField] private WarehouseManager warehouseManager;
        [SerializeField] private LicenseManager licenseManager;
        public DatabaseManager Database => databaseManager;
        public ProductionManager Production => productionManager;
        public WarehouseManager Warehouse => warehouseManager;
        public TimeManager Time => timeManager;
        public EconomyManager Economy => economyManager;
        public FactoryManager Factory => factoryManager;
        public ResearchManager Research => researchManager;
        public LicenseManager License => licenseManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManagers();
            warehouseManager.AddItem("MAT-PLASTIC", 999);
            warehouseManager.AddItem("MAT-ALUMINUM", 999);
            warehouseManager.AddItem("MAT-CARBON", 999);
            warehouseManager.AddItem("MAT-COPPER", 999);
            warehouseManager.AddItem("MAT-PCB", 999);
            warehouseManager.AddItem("MAT-BATTERY", 999);
            warehouseManager.AddItem("MAT-GLASS", 999);
            warehouseManager.AddItem("MAT-STEEL", 999);
            foreach (ComponentSO component in databaseManager.Database.Components)
            {
                warehouseManager.AddItem(component.ID, 99);
            }
        }

        private void OnDestroy()
        {
            ShutdownManagers();
        }
        private void Start()
        {
            SceneManager.LoadSceneAsync("MainMenu");
        }

        private void InitializeManagers()
        {
            databaseManager?.Initialize();
            productionManager?.Initialize();
            economyManager?.Initialize();
            factoryManager?.Initialize();
            researchManager?.Initialize();
            warehouseManager?.Initialize();
            licenseManager?.Initialize();
        }

        private void ShutdownManagers()
        {
            productionManager?.Shutdown();
            factoryManager?.Shutdown();
            economyManager?.Shutdown();
            warehouseManager?.Shutdown();
            licenseManager?.Shutdown();
            databaseManager?.Shutdown();
        }
    }
}