using UnityEngine;

namespace SkyOfFreedom.Data
{
    public abstract class DataSO : ScriptableObject
    {
        [SerializeField]
        private string id;
#if UNITY_EDITOR
        public void SetID(string id)
        {
            this.id = id;
        }
#endif
        public string ID
        {
            get
            {
                return id;
            }
        }
    }
}