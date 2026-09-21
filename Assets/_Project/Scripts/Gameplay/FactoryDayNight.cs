using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SkyOfFreedom.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FactoryDayNight : MonoBehaviour
    {
        [Header("Clock (continues while the game is closed)")]
        [SerializeField, Min(1f)] private float cycleMinutes = 24f;
        [SerializeField, Range(0f, 24f)] private float hourOffset = 0f;
        [Header("Lighting")]
        [SerializeField] private Light sun;
        [SerializeField, Range(0f, 1f)] private float nightLightRatio = 0.12f;
        [SerializeField] private Color nightAmbient = new Color(0.13f, 0.18f, 0.28f);
        [SerializeField] private Light[] nightLights = Array.Empty<Light>();
        public float Hour { get; private set; }

        private Quaternion originalRotation;
        private Color originalLightColor, originalAmbient, originalSky, originalEquator, originalGround;
        private float originalIntensity, originalExposure;
        private AmbientMode originalAmbientMode;
        private Material originalSkybox, runtimeSkybox;
        private Light originalSun;
        private bool[] originalNightStates;
        private bool captured;
        private float nextUpdate;
        private Scene lightingScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistration()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Game") return;
            Light candidate = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<FactoryDayNight>(true) != null) return;
                foreach (Light light in root.GetComponentsInChildren<Light>(false))
                    if (light.type == LightType.Directional && light.enabled &&
                        (candidate == null || light.intensity > candidate.intensity))
                        candidate = light;
            }
            if (candidate != null) candidate.gameObject.AddComponent<FactoryDayNight>();
        }

        public static float CalculateHour(DateTime utc, double minutes, double offset)
        {
            if (double.IsNaN(minutes) || double.IsInfinity(minutes) || minutes < 1d) minutes = 24d;
            if (double.IsNaN(offset) || double.IsInfinity(offset)) offset = 0d;
            double seconds = (utc.ToUniversalTime() - new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            double hour = seconds / (minutes * 60d) * 24d + offset;
            return (float)((hour % 24d + 24d) % 24d);
        }

        private void OnEnable()
        {
            if (sun == null) sun = GetComponent<Light>();
            if (sun == null || sun.type != LightType.Directional) return;
            lightingScene = gameObject.scene;
            nightLights = nightLights ?? Array.Empty<Light>();
            originalRotation = sun.transform.rotation;
            originalLightColor = sun.color;
            originalIntensity = sun.intensity;
            originalSun = RenderSettings.sun;
            originalAmbientMode = RenderSettings.ambientMode;
            originalAmbient = RenderSettings.ambientLight;
            originalSky = RenderSettings.ambientSkyColor;
            originalEquator = RenderSettings.ambientEquatorColor;
            originalGround = RenderSettings.ambientGroundColor;
            originalSkybox = RenderSettings.skybox;
            if (originalSkybox != null && originalSkybox.HasProperty("_Exposure"))
            {
                runtimeSkybox = new Material(originalSkybox);
                runtimeSkybox.name = originalSkybox.name + " (Day Night Runtime)";
                originalExposure = runtimeSkybox.GetFloat("_Exposure");
                RenderSettings.skybox = runtimeSkybox;
            }
            originalNightStates = new bool[nightLights.Length];
            for (int i = 0; i < nightLights.Length; i++)
                if (nightLights[i] != null) originalNightStates[i] = nightLights[i].enabled;
            captured = true;
            Apply();
        }

        private void Update()
        {
            if (!captured || Time.unscaledTime < nextUpdate) return;
            nextUpdate = Time.unscaledTime + 0.1f;
            Apply();
        }

        private void Apply()
        {
            Hour = CalculateHour(DateTime.UtcNow, cycleMinutes, hourOffset);
            float elevation = Mathf.Sin((Hour - 6f) / 24f * Mathf.PI * 2f);
            float daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.12f, 0.3f, elevation));
            // One shadow-casting light: it acts as a soft moon during the night.
            float pitch = (Hour - 6f) * 15f + (elevation < 0f ? 180f : 0f);
            sun.transform.rotation = Quaternion.Euler(pitch, originalRotation.eulerAngles.y, 0f);
            sun.intensity = originalIntensity * Mathf.Lerp(nightLightRatio, 1f, daylight);
            Color dusk = Color.Lerp(new Color(1f, 0.62f, 0.38f), originalLightColor,
                Mathf.InverseLerp(0f, 0.5f, elevation));
            sun.color = Color.Lerp(new Color(0.55f, 0.68f, 1f), dusk, daylight);
            RenderSettings.sun = sun;
            // Ambient colors avoid expensive realtime GI rebakes on a phone.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(nightAmbient, originalSky, daylight);
            RenderSettings.ambientEquatorColor = Color.Lerp(nightAmbient * 0.7f, originalEquator, daylight);
            RenderSettings.ambientGroundColor = Color.Lerp(nightAmbient * 0.4f, originalGround, daylight);
            if (runtimeSkybox != null)
                runtimeSkybox.SetFloat("_Exposure", originalExposure * Mathf.Lerp(0.06f, 1f, daylight));
            for (int i = 0; i < nightLights.Length; i++)
                if (nightLights[i] != null && nightLights[i] != sun)
                    nightLights[i].enabled = daylight < 0.35f;
        }

        private void OnDisable()
        {
            if (!captured) return;
            captured = false;
            if (sun != null)
            {
                sun.transform.rotation = originalRotation;
                sun.color = originalLightColor;
                sun.intensity = originalIntensity;
            }
            if (SceneManager.GetActiveScene() == lightingScene)
            {
                if (RenderSettings.sun == sun) RenderSettings.sun = originalSun;
                RenderSettings.ambientMode = originalAmbientMode;
                RenderSettings.ambientLight = originalAmbient;
                RenderSettings.ambientSkyColor = originalSky;
                RenderSettings.ambientEquatorColor = originalEquator;
                RenderSettings.ambientGroundColor = originalGround;
            }
            if (runtimeSkybox != null)
            {
                if (RenderSettings.skybox == runtimeSkybox) RenderSettings.skybox = originalSkybox;
                Destroy(runtimeSkybox);
                runtimeSkybox = null;
            }
            for (int i = 0; i < nightLights.Length; i++)
                if (nightLights[i] != null) nightLights[i].enabled = originalNightStates[i];
        }
    }
}
