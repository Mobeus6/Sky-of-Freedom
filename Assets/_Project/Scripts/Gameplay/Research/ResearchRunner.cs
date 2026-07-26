using UnityEngine;
using SkyOfFreedom.Data;

namespace SkyOfFreedom.Managers
{
    public class ResearchRunner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ResearchManager researchManager;

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTick += OnTick;
            }
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTick -= OnTick;
            }
        }

        private void OnTick(float deltaTime)
        {
            if (researchManager == null)
                return;

            if (!researchManager.HasActiveResearch())
                return;

            ResearchState state = researchManager.ActiveResearch;

            if (state == null)
                return;

            ResearchSO research =
                researchManager.GetResearch(state.ResearchID);

            if (research == null)
                return;

            state.RemainingTime -= deltaTime;

            if (state.RemainingTime < 0f)
                state.RemainingTime = 0f;

            state.Progress =
                1f - (state.RemainingTime / research.ResearchTime);

            if (state.Progress > 1f)
                state.Progress = 1f;

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

            ResearchState state = researchManager.ActiveResearch;

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