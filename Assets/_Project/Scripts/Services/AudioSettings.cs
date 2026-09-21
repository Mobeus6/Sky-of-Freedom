using System;
using UnityEngine;

namespace SkyOfFreedom.Services
{
    public static class AudioSettings
    {
        private const string MusicKey = "SkyOfFreedom.Audio.Music";
        private const string EffectsKey = "SkyOfFreedom.Audio.Effects";
        public static event Action Changed;
        public static float Music => Read(MusicKey);
        public static float Effects => Read(EffectsKey);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEvents() { Changed = null; }

        private static float Read(string key)
        {
            float value = PlayerPrefs.GetFloat(key, 1f);
            return float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
        }

        public static void SetMusic(float value) { Set(MusicKey, value); }
        public static void SetEffects(float value) { Set(EffectsKey, value); }
        private static void Set(string key, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            // Two small local preferences; persist even if Android kills the app later.
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
