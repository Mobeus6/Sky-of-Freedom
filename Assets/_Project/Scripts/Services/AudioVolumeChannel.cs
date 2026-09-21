using UnityEngine;

namespace SkyOfFreedom.Services
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioVolumeChannel : MonoBehaviour
    {
        public enum Channel { Music, SoundEffects }
        [SerializeField] private Channel channel = Channel.SoundEffects;
        private AudioSource source;
        private float baseVolume;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            baseVolume = source.volume;
        }

        private void OnEnable()
        {
            AudioSettings.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            AudioSettings.Changed -= Apply;
            if (source != null) source.volume = baseVolume;
        }

        private void Apply()
        {
            if (source != null)
                source.volume = baseVolume * (channel == Channel.Music ? AudioSettings.Music : AudioSettings.Effects);
        }
    }
}
