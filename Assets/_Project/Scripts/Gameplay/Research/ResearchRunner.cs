using UnityEngine;
using SkyOfFreedom.Data;

namespace SkyOfFreedom.Managers
{
    public class ResearchRunner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ResearchManager researchManager;
        [SerializeField] private TimeManager timeManager;

        private void OnEnable()
        {
            if (timeManager == null)
            {
                if (GameManager.Instance != null)
                {
                    timeManager = GameManager.Instance.Time;
                }
            }

            if (timeManager != null)
            {
                timeManager.OnTick += OnTick;
            }
            else
            {
                Debug.LogError(
                    "[ResearchRunner] TimeManager reference is missing.");
            }
        }

        private void OnDisable()
        {
            if (timeManager != null)
            {
                timeManager.OnTick -= OnTick;
            }
        }

        private void OnTick(float deltaTime)
        {
            if (researchManager == null)
                return;

            if (!researchManager.HasActiveResearch())
                return;

            ResearchState state =
                researchManager.ActiveResearch;

            if (state == null)
                return;

            ResearchSO research =
                researchManager.GetResearch(
                    state.ResearchID);

            if (research == null)
                return;

            state.RemainingTime -= deltaTime;

            if (state.RemainingTime < 0f)
            {
                state.RemainingTime = 0f;
            }

            if (state.TotalResearchTime > 0f)
            {
                state.Progress =
                    1f -
                    (state.RemainingTime /
                     state.TotalResearchTime);
            }
            else
            {
                state.Progress = 1f;
            }

            state.Progress =
                Mathf.Clamp01(
                    state.Progress);

            if (state.RemainingTime <= 0f)
            {
                researchManager.CompleteResearch();
            }
        }

        public void FinishInstantly()
        {
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

            return researchManager.ActiveResearch.RemainingTime;
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