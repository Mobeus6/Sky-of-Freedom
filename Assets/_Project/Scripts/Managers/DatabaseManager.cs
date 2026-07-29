using SkyOfFreedom.Managers;
using UnityEngine;

namespace SkyOfFreedom.Data
{
    public class DatabaseManager : BaseManager
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
        public override void Initialize()
        {
            if (IsInitialized)
                return;

            database.Initialize();

            base.Initialize();
        }
        public override void Shutdown()
        {
            base.Shutdown();
        }
    }
}