using System;
using System.Collections.Generic;
using UnityEngine;
using SkyOfFreedom.Data;
using SkyOfFreedom.Factory;

namespace SkyOfFreedom.Managers
{
    public class ResearchManager : MonoBehaviour
    {
        [Header("Database")]
        private GameDatabase gameDatabase;

        private EconomyManager economyManager;
        private FactoryManager factoryManager;

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

        public IReadOnlyDictionary<string, ResearchState> ResearchStates =>
            researchStates;

        #endregion

        #region Initialization

        public void Initialize()
        {
            researchStates.Clear();
            activeResearch = null;

            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager not found.");
                return;
            }

            gameDatabase =
                GameManager.Instance.Database.Database;

            economyManager =
                GameManager.Instance.Economy;

            factoryManager =
                GameManager.Instance.Factory;

            if (gameDatabase == null)
            {
                Debug.LogError("GameDatabase not found.");
                return;
            }

            CreateResearchStates();
            RefreshUnlockedResearches();
        }

        #endregion

        #region Getters

        public ResearchSO GetResearch(string researchID)
        {
            return gameDatabase.GetResearch(researchID);
        }

        public ResearchState GetState(string researchID)
        {
            researchStates.TryGetValue(
                researchID,
                out ResearchState state);

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

        #region Validation

        private void RefreshUnlockedResearches()
        {
            bool unlockedAnyResearch;

            do
            {
                unlockedAnyResearch = false;

                foreach (ResearchSO research in gameDatabase.Researches)
                {
                    if (research == null)
                        continue;

                    if (!TryGetResearchState(
                            research,
                            out ResearchState state))
                    {
                        continue;
                    }

                    if (state.IsUnlocked ||
                        state.IsCompleted)
                    {
                        continue;
                    }

                    if (factoryManager.Level <
                        research.RequiredFactoryLevel)
                    {
                        continue;
                    }

                    bool prerequisitesCompleted = true;

                    foreach (ResearchSO prerequisite
                             in research.Prerequisites)
                    {
                        if (prerequisite == null)
                            continue;

                        if (!IsCompleted(prerequisite.ID))
                        {
                            prerequisitesCompleted = false;
                            break;
                        }
                    }

                    if (!prerequisitesCompleted)
                        continue;

                    UnlockResearch(
                        research.ID);

                    unlockedAnyResearch = true;
                }

            } while (unlockedAnyResearch);
        }

        private void CreateResearchStates()
        {
            researchStates.Clear();

            foreach (ResearchSO research
                     in gameDatabase.Researches)
            {
                if (research == null)
                    continue;

                ResearchState state =
                    new ResearchState
                    {
                        ResearchID = research.ID,
                        IsUnlocked = false,
                        IsCompleted = false,
                        IsResearching = false,
                        Progress = 0f,
                        RemainingTime = research.ResearchTime,
                        TotalResearchTime = research.ResearchTime
                    };

                if (research.Prerequisites == null ||
                    research.Prerequisites.Length == 0)
                {
                    state.IsUnlocked = true;
                }

                researchStates.Add(
                    research.ID,
                    state);
            }
        }

        public bool CanStartResearch(
            ResearchSO research)
        {
            if (research == null)
                return false;

            if (activeResearch != null)
                return false;

            if (!TryGetResearchState(
                    research,
                    out ResearchState state))
            {
                return false;
            }

            if (state.IsCompleted)
                return false;

            if (!state.IsUnlocked)
                return false;

            if (factoryManager.Level <
                research.RequiredFactoryLevel)
            {
                return false;
            }

            if (!economyManager.HasMoney(
                    research.Cost))
            {
                return false;
            }

            foreach (ResearchSO prerequisite
                     in research.Prerequisites)
            {
                if (prerequisite == null)
                    continue;

                if (!IsCompleted(prerequisite.ID))
                    return false;
            }

            return true;
        }

        private bool TryGetResearchState(
            ResearchSO research,
            out ResearchState state)
        {
            state = null;

            if (research == null)
                return false;

            return researchStates.TryGetValue(
                research.ID,
                out state);
        }

        #endregion

        #region Research

        public bool HasActiveResearch()
        {
            return activeResearch != null;
        }

        public bool StartResearch(
            ResearchSO research)
        {
            if (!CanStartResearch(research))
                return false;

            if (!TryGetResearchState(
                    research,
                    out ResearchState state))
            {
                return false;
            }

            economyManager.SpendMoney(
                research.Cost);

            float researchDuration =
                GetResearchDuration(research);

            state.IsResearching = true;
            state.Progress = 0f;
            state.TotalResearchTime = researchDuration;
            state.RemainingTime = researchDuration;

            activeResearch = state;

            OnResearchStarted?.Invoke(
                research);

            return true;
        }

        private float GetResearchDuration(
            ResearchSO research)
        {
            if (research == null)
                return 0f;

            float duration =
                research.ResearchTime;

            if (factoryManager == null)
                return duration;

            if (factoryManager.ProgressionConfig == null)
                return duration;

            int researchZoneLevel =
                factoryManager.GetLevel(
                    FactoryZoneType.Research);

            if (factoryManager.ProgressionConfig
                .TryGetResearchZoneBonus(
                    researchZoneLevel,
                    out FactoryProgressionConfig.ResearchZoneLevelBonus bonus))
            {
                if (bonus.SpeedMultiplier > 0f)
                {
                    duration /=
                        bonus.SpeedMultiplier;
                }
            }

            return Mathf.Max(
                0f,
                duration);
        }

        public void CancelResearch()
        {
            if (activeResearch == null)
                return;

            ResearchSO research =
                gameDatabase.GetResearch(
                    activeResearch.ResearchID);

            activeResearch.IsResearching = false;
            activeResearch.Progress = 0f;

            activeResearch.TotalResearchTime =
                research.ResearchTime;

            activeResearch.RemainingTime =
                research.ResearchTime;

            activeResearch = null;

            OnResearchCancelled?.Invoke(
                research);
        }

        public void CompleteResearch()
        {
            if (activeResearch == null)
                return;

            ResearchSO research =
                gameDatabase.GetResearch(
                    activeResearch.ResearchID);

            activeResearch.IsResearching = false;
            activeResearch.IsCompleted = true;
            activeResearch.Progress = 1f;
            activeResearch.RemainingTime = 0f;

            activeResearch = null;

            OnResearchCompleted?.Invoke(
                research);

            RefreshUnlockedResearches();
        }

        public void UnlockResearch(
            string researchID)
        {
            if (!researchStates.TryGetValue(
                    researchID,
                    out ResearchState state))
            {
                return;
            }

            if (state.IsUnlocked)
                return;

            state.IsUnlocked = true;

            ResearchSO research =
                gameDatabase.GetResearch(
                    researchID);

            OnResearchUnlocked?.Invoke(
                research);
        }

        #endregion

        #region Save / Load

        public List<ResearchState> GetSaveData()
        {
            return new List<ResearchState>(
                researchStates.Values);
        }

        public void LoadSaveData(
            List<ResearchState> saveData)
        {
            if (saveData == null)
                return;

            foreach (ResearchState savedState
                     in saveData)
            {
                if (!researchStates.ContainsKey(
                        savedState.ResearchID))
                {
                    continue;
                }

                researchStates[savedState.ResearchID] =
                    savedState;

                if (savedState.IsResearching)
                {
                    activeResearch = savedState;
                }
            }
        }

        #endregion
    }
}