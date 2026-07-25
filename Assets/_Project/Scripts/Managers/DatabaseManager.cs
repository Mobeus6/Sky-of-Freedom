using UnityEngine;

namespace SkyOfFreedom.Data
{
    public class DatabaseManager : MonoBehaviour
    {
        [SerializeField]
        private GameDatabase database;

        public GameDatabase Database
        {
            get
            {
                return database;
            }
        }

        private void Awake()
        {
            database.Initialize();
        }
    }
}