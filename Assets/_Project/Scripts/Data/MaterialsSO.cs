using UnityEngine;

namespace SkyOfFreedom.Data
{
    [CreateAssetMenu(
     fileName = "Material",
     menuName = "Sky of Freedom/Material")]
    public class MaterialSO : DataSO
    {
        [Header("General")]
        [SerializeField]
        private string materialName;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        [TextArea]
        private string description;
        [SerializeField]
        private int tier;

        [Header("Economy")]
        [SerializeField]
        private int basePrice;

        [Header("Storage")]
        [SerializeField]
        private int maxStack = 999;
        [SerializeField] private int storageSize = 1;

        public int StorageSize => storageSize;
        public int Tier => tier;
        public string MaterialName => materialName;
        public Sprite Icon => icon;
        public string Description => description;
        public int BasePrice => basePrice;
        public int MaxStack => maxStack;
    }
}
