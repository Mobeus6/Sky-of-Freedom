using SkyOfFreedom.Data;
using System;
using UnityEngine;

namespace SkyOfFreedom.UI
{
    [Serializable]
    public class ResearchLayout
    {
        [SerializeField] private ResearchSO research;
        [SerializeField] private Vector2 position;

        public ResearchSO Research => research;
        public Vector2 Position => position;
    }
}