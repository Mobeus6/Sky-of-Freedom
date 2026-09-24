using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    // Uses Unity's active Selectable registry, including buttons created at runtime.
    // Does not modify onClick listeners, transitions, colours or existing transforms.
    [DefaultExecutionOrder(-10000)]
    public sealed class ButtonFeedbackInstaller : MonoBehaviour
    {
        private Selectable[] buffer = new Selectable[128];
        private static ButtonFeedbackInstaller instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (instance != null) return;
            var host = new GameObject("Button Feedback Installer");
            DontDestroyOnLoad(host);
            host.AddComponent<ButtonFeedbackInstaller>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Update()
        {
            UIUnlockFeedback.Tick();
            int total = Selectable.allSelectableCount;
            if (total > buffer.Length) buffer = new Selectable[Mathf.NextPowerOfTwo(total)];
            int count = Selectable.AllSelectablesNoAlloc(buffer);
            for (int i = 0; i < count; i++)
            {
                Button button = buffer[i] as Button;
                if (button != null && !button.TryGetComponent<ButtonFeedbackUI>(out _))
                    button.gameObject.AddComponent<ButtonFeedbackUI>();
                buffer[i] = null;
            }
        }
    }
}
