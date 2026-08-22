using System;

namespace SkyOfFreedom.Data
{
    [Serializable]
    public class ResearchState
    {
        public string ResearchID;

        public bool IsUnlocked;

        public bool IsResearching;

        public bool IsCompleted;

        public float Progress;

        public float RemainingTime;

        public float TotalResearchTime;
    }
}