using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Data
{
    [CreateAssetMenu(
        fileName = "PlayerStartConfig",
        menuName = "Sky of Freedom/Player Start Config"
    )]
    public class PlayerStartConfigSO : ScriptableObject
    {
        [Header("Starting Economy")]
        [SerializeField] private long startingMoney = 1000;
        [SerializeField] private int startingReputation = 0;

        [Header("Starting Materials")]
        [SerializeField] private List<StartingMaterial> materials = new List<StartingMaterial>();

        public long StartingMoney => startingMoney;
        public int StartingReputation => startingReputation;
        public IReadOnlyList<StartingMaterial> Materials => materials;

        [Serializable]
        public class StartingMaterial
        {
            [SerializeField] private string materialId;
            [SerializeField] private int quantity = 1;

            public string MaterialId => materialId;
            public int Quantity => quantity;
        }
    }
}