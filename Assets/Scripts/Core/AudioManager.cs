using UnityEngine;

namespace HappyBirthday.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.75f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSources();
        }

        public void PlayMusic(AudioClip clip)
        {
            EnsureSources();
            if (clip == null || musicSource.clip == clip)
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public void PlaySfx(AudioClip clip)
        {
            EnsureSources();
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume);
            }
        }

        private void EnsureSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
                musicSource.volume = musicVolume;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.volume = sfxVolume;
            }
        }
    }
}
