using UnityEngine;

namespace SkyOfFreedom.Data
{
    public abstract class DataSO : ScriptableObject
    {
        [SerializeField]
        private string id;

        public string ID => id;
    }
}