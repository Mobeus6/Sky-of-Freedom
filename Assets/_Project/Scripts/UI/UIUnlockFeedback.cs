using System.Collections.Generic;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public sealed class UIUnlockFeedback : MonoBehaviour
    {
        private static readonly Dictionary<string, bool> states = new Dictionary<string, bool>();
        private static readonly HashSet<string> pending = new HashSet<string>();
        private static GameManager observedGame;
        private static string account;
        private static float nextSnapshot;
        private string key;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            states.Clear();
            pending.Clear();
            observedGame = null;
            account = null;
            nextSnapshot = 0f;
        }

        public static void Tick()
        {
            GameManager game = GameManager.Instance;
            if (game == null || !game.IsGameReady || game.IsAccountTransition)
            {
                Reset();
                return;
            }
            if (observedGame != game || account != game.PublicId)
            {
                Reset();
                observedGame = game;
                account = game.PublicId;
            }
            if (Time.unscaledTime < nextSnapshot) return;
            nextSnapshot = Time.unscaledTime + 0.25f;
            var database = game.Database != null ? game.Database.Database : null;
            if (database == null || game.License == null || game.Research == null) return;
            if (database.Components != null)
                foreach (var item in database.Components) ObserveProduct(game, item);
            if (database.DroneModels != null)
                foreach (var item in database.DroneModels) ObserveProduct(game, item);
            if (database.Licenses != null)
                foreach (var license in database.Licenses)
                    if (license != null) Observe("license:" + license.ID, !game.License.IsLocked(license));
            if (database.Researches != null)
                foreach (var research in database.Researches)
                {
                    if (research == null) continue;
                    var state = game.Research.GetState(research.ID);
                    if (state != null) Observe("research:" + research.ID, state.IsUnlocked || state.IsCompleted);
                }
        }

        private static void ObserveProduct(GameManager game, IProducible item)
        {
            if (item != null) Observe("product:" + item.ID,
                ProductionManager.MeetsFactoryLevel(item) && game.License.CanProduce(item));
        }

        private static void Observe(string id, bool unlocked)
        {
            if (states.TryGetValue(id, out bool previous) && !previous && unlocked) pending.Add(id);
            if (!unlocked) pending.Remove(id);
            states[id] = unlocked;
        }

        public static void Bind(Component owner, string id)
        {
            if (!owner.TryGetComponent<UIUnlockFeedback>(out var feedback))
                feedback = owner.gameObject.AddComponent<UIUnlockFeedback>();
            feedback.key = id;
        }

        public static bool Visible(RectTransform rect)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy) return false;
            foreach (Canvas canvas in rect.GetComponentsInParent<Canvas>())
                if (!canvas.isActiveAndEnabled) return false;
            foreach (CanvasGroup group in rect.GetComponentsInParent<CanvasGroup>())
                if (group.alpha < 0.95f) return false;
            foreach (RectMask2D mask in rect.GetComponentsInParent<RectMask2D>())
            {
                if (!mask.isActiveAndEnabled) continue;
                var viewport = (RectTransform)mask.transform;
                Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, rect);
                Rect area = viewport.rect;
                if (bounds.max.x < area.xMin || bounds.min.x > area.xMax ||
                    bounds.max.y < area.yMin || bounds.min.y > area.yMax) return false;
            }
            return true;
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(key) || !pending.Contains(key)) return;
            var game = GameManager.Instance;
            if (game == null || !game.IsGameReady || game.IsAccountTransition) return;
            var rect = transform as RectTransform;
            if (!Visible(rect)) return;
            pending.Remove(key);
            UIActionFlash.Show(rect, new Color(0.4f, 0.85f, 1f, 0.22f), 0.75f, GetComponent<Image>());
        }
    }
}
