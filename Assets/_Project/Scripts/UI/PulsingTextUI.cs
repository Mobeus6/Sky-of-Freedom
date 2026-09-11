using TMPro;
using UnityEngine;

namespace SkyOfFreedom.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class PulsingTextUI : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float cycleDuration = 2.5f;

        [SerializeField, Range(0f, 1f)]
        private float minimumAlpha = 0.35f;

        private TMP_Text targetText;
        private float originalAlpha;
        private float elapsedTime;

        private void Awake()
        {
            targetText = GetComponent<TMP_Text>();
            originalAlpha = targetText.alpha;
        }

        private void OnEnable()
        {
            elapsedTime = 0f;

            if (targetText != null)
            {
                targetText.alpha = originalAlpha;
            }
        }

        private void Update()
        {
            float duration = Mathf.Max(0.1f, cycleDuration);

            elapsedTime = Mathf.Repeat(
                elapsedTime + Time.unscaledDeltaTime,
                duration
            );

            float phase = elapsedTime / duration;

            float brightness =
                0.5f + 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);

            targetText.alpha = originalAlpha * Mathf.Lerp(
                minimumAlpha,
                1f,
                brightness
            );
        }

        private void OnDisable()
        {
            if (targetText != null)
            {
                targetText.alpha = originalAlpha;
            }
        }
    }
}