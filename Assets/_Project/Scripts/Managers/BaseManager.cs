using UnityEngine;

namespace SkyOfFreedom.Managers
{
    public abstract class BaseManager : MonoBehaviour
    {
        public bool IsInitialized { get; private set; }

        public virtual void Initialize()
        {
            if (IsInitialized)
                return;

            IsInitialized = true;
        }

        public virtual void Shutdown()
        {
            if (!IsInitialized)
                return;

            IsInitialized = false;
        }
    }
}