using System;
using System.Collections.Generic;
using UnityEngine;
using SkyOfFreedom.Data;

namespace SkyOfFreedom.Managers
{
    public class ResearchManager : MonoBehaviour
    {
        [Header("Database")]
        [SerializeField] private GameDatabase gameDatabase;

        private readonly Dictionary<string, ResearchState> researchStates =
            new Dictionary<string, ResearchState>();

        private ResearchState activeResearch;

        public event Action<ResearchSO> OnResearchStarted;
        public event Action<ResearchSO> OnResearchCancelled;
        public event Action<ResearchSO> OnResearchCompleted;
        public event Action<ResearchSO> OnResearchUnlocked;

        #region Properties

        public GameDatabase Database => gameDatabase;

        public ResearchState ActiveResearch => activeResearch;

        public IReadOnlyDictionary<string, ResearchState> ResearchStates => researchStates;

        #endregion

        #region Unity

        private void Awake()
        {
            Initialize();
        }

        #endregion

        #region Initialization

        public void Initialize()
        {
            researchStates.Clear();
            activeResearch = null;

            if (gameDatabase == null)
            {
                Debug.LogError("Game Database is missing.");
                return;
            }

            gameDatabase.Initialize();

            foreach (ResearchSO research in gameDatabase.Researches)
            {
                if (research == null)
                    continue;

                ResearchState state = new ResearchState();

                state.ResearchID = research.ID;
                state.IsUnlocked = research.Prerequisites.Length == 0;
                state.IsCompleted = false;
                state.IsResearching = false;
                state.Progress = 0f;
                state.RemainingTime = research.ResearchTime;

                researchStates.Add(state.ResearchID, state);
            }
        }

        #endregion

        #region Getters

        public ResearchSO GetResearch(string researchID)
        {
            return gameDatabase.GetResearch(researchID);
        }

        public ResearchState GetState(string researchID)
        {
            researchStates.TryGetValue(researchID, out ResearchState state);
            return state;
        }

        public bool IsUnlocked(string researchID)
        {
            return GetState(researchID)?.IsUnlocked ?? false;
        }

        public bool IsCompleted(string researchID)
        {
            return GetState(researchID)?.IsCompleted ?? false;
        }

        public bool IsResearching(string researchID)
        {
            return GetState(researchID)?.IsResearching ?? false;
        }

        #endregion

        #region Research

        public bool HasActiveResearch()
        {
            return activeResearch != null;
        }

        public bool StartResearch(ResearchSO research)
        {
            if (research == null)
                return false;

            if (activeResearch != null)
                return false;

            if (!researchStates.TryGetValue(research.ID, out ResearchState state))
                return false;

            if (!state.IsUnlocked)
                return false;

            if (state.IsCompleted)
                return false;

            state.IsResearching = true;
            state.Progress = 0f;
            state.RemainingTime = research.ResearchTime;

            activeResearch = state;

            OnResearchStarted?.Invoke(research);

            return true;
        }

        public void CancelResearch()
        {
            if (activeResearch == null)
                return;

            ResearchSO research =
                gameDatabase.GetResearch(activeResearch.ResearchID);

            activeResearch.IsResearching = false;
            activeResearch.Progress = 0f;
            activeResearch.RemainingTime = research.ResearchTime;

            activeResearch = null;

            OnResearchCancelled?.Invoke(research);
        }

        public void CompleteResearch()
        {
            if (activeResearch == null)
                return;

            ResearchSO research =
                gameDatabase.GetResearch(activeResearch.ResearchID);

            activeResearch.IsResearching = false;
            activeResearch.IsCompleted = true;
            activeResearch.Progress = 1f;
            activeResearch.RemainingTime = 0f;

            activeResearch = null;

            OnResearchCompleted?.Invoke(research);
        }

        public void UnlockResearch(string researchID)
        {
            if (!researchStates.TryGetValue(researchID, out ResearchState state))
                return;

            if (state.IsUnlocked)
                return;

            state.IsUnlocked = true;

            ResearchSO research =
                gameDatabase.GetResearch(researchID);

            OnResearchUnlocked?.Invoke(research);
        }

        #endregion

        #region Save / Load

        public List<ResearchState> GetSaveData()
        {
            return new List<ResearchState>(researchStates.Values);
        }

        public void LoadSaveData(List<ResearchState> saveData)
        {
            if (saveData == null)
                return;

            foreach (ResearchState savedState in saveData)
            {
                if (!researchStates.ContainsKey(savedState.ResearchID))
                    continue;

                researchStates[savedState.ResearchID] = savedState;

                if (savedState.IsResearching)
                {
                    activeResearch = savedState;
                }
            }
        }

        #endregion
    }
}