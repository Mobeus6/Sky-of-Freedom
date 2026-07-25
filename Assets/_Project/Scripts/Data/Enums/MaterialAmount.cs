using UnityEngine;

namespace SkyOfFreedom.Data
{
    [System.Serializable]
    public class MaterialAmount
    {
        [SerializeField]
        private MaterialSO material;

        [SerializeField]
        [Min(1)]
        private int amount;

        public MaterialSO Material
        {
            get
            {
                return material;
            }
        }

        public int Amount
        {
            get
            {
                return amount;
            }
        }
    }
}