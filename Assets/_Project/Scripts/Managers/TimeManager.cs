using System;
using UnityEngine;

namespace SkyOfFreedom.Managers
{
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField]
        private float timeScale = 1f;

        public event Action<float> OnTick;

        public float DeltaTime => Time.deltaTime * timeScale;

        public float TimeScale
        {
            get => timeScale;
            set => timeScale = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            OnTick?.Invoke(DeltaTime);
        }

        public void Pause()
        {
            timeScale = 0f;
        }

        public void Resume()
        {
            timeScale = 1f;
        }

        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Max(0f, scale);
        }
    }
}