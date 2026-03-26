using UnityEngine;

namespace MakeItOut.Runtime.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource _sfxSource;

        [Header("Clips")]
        public AudioClip SwitchClip;
        public AudioClip JumpClip;
        public AudioClip LandClip;
        public AudioClip ExitFoundClip;
        public AudioClip UiClickClip;

        public static void EnsureInstance()
        {
            if (Instance != null)
                return;

            AudioManager existing = FindObjectOfType<AudioManager>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            GameObject go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
                _sfxSource.spatialBlend = 0f;
            }
        }

        public void PlaySwitch() => Play(SwitchClip, 0.7f);
        public void PlayJump() => Play(JumpClip, 0.5f);
        public void PlayLand(float intensity) => Play(LandClip, Mathf.Lerp(0.1f, 0.6f, Mathf.Clamp01(intensity / 15f)));
        public void PlayExitFound() => Play(ExitFoundClip, 0.9f);
        public void PlayUiClick() => Play(UiClickClip, 0.4f);

        private void Play(AudioClip clip, float volume)
        {
            if (_sfxSource != null && clip != null)
                _sfxSource.PlayOneShot(clip, volume);
        }
    }
}
