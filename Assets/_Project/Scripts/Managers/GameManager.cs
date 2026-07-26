using SkyOfFreedom.Warehouse;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkyOfFreedom.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Managers")]
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private FactoryManager factoryManager;
        [SerializeField] private ResearchManager researchManager;
        [SerializeField] private WarehouseManager inventoryManager;

        public WarehouseManager Inventory => inventoryManager;
        public TimeManager Time => timeManager;
        public EconomyManager Economy => economyManager;
        public FactoryManager Factory => factoryManager;
        public ResearchManager Research => researchManager;

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
        }

        private void OnDestroy()
        {
            ShutdownManagers();
        }

        private void InitializeManagers()
        {
            economyManager?.Initialize();
            factoryManager?.Initialize();
            researchManager?.Initialize();
            inventoryManager?.Initialize();
            SceneManager.LoadScene("MainMenu");
        }

        private void ShutdownManagers()
        {

            factoryManager?.Shutdown();
            economyManager?.Shutdown();
            inventoryManager?.Shutdown();
        }
    }
}