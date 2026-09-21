using UnityEngine;
using SkyOfFreedom.Data;

namespace SkyOfFreedom.Managers
{
    public class ResearchRunner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ResearchManager researchManager;
        [SerializeField] private TimeManager timeManager;

        private TimeManager subscribedTime;

        private bool CanRun =>
            GameManager.Instance != null &&
            GameManager.Instance.IsGameReady &&
            !GameManager.Instance.IsAccountTransition &&
            !GameManager.Instance.ProductionPausedAtUtc.HasValue;

        private void OnEnable()
        {
            TryConnect();
        }

        private void Update()
        {
            // Initialization is asynchronous; retry until services are ready.
            if (subscribedTime == null)
                TryConnect();
        }

        private void TryConnect()
        {
            if (!CanRun) return;
            if (researchManager == null)
                researchManager = GameManager.Instance.Research;
            if (timeManager == null)
                timeManager = GameManager.Instance.Time;
            if (researchManager == null || timeManager == null) return;
            if (subscribedTime == timeManager) return;

            Disconnect();
            subscribedTime = timeManager;
            subscribedTime.OnTick += OnTick;
        }

        private void Disconnect()
        {
            if (subscribedTime != null)
                subscribedTime.OnTick -= OnTick;
            subscribedTime = null;
        }

        private void OnDisable()
        {
            Disconnect();
        }

        private void OnTick(float deltaTime)
        {
            if (!CanRun || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                return;

            if (researchManager == null)
                return;

            researchManager.AdvanceTime(deltaTime);
        }

        public void FinishInstantly()
        {
            if (!CanRun) return;
            if (researchManager == null)
                return;

            if (!researchManager.HasActiveResearch())
                return;

            ResearchState state =
                researchManager.ActiveResearch;

            state.Progress = 1f;
            state.RemainingTime = 0f;

            researchManager.CompleteResearch();
        }

        public float GetRemainingTime()
        {
            if (researchManager == null)
                return 0f;

            if (!researchManager.HasActiveResearch())
                return 0f;

            return researchManager.ActiveResearch.RemainingTime / researchManager.GetResearchSpeedMultiplier();
        }

        public float GetProgress()
        {
            if (researchManager == null)
                return 0f;

            if (!researchManager.HasActiveResearch())
                return 0f;

            return researchManager.ActiveResearch.Progress;
        }
    }
}
