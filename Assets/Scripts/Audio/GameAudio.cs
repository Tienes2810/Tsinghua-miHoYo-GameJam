using UnityEngine;

namespace PerspectivePuzzle.Audio
{
    public class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }

        AudioSource musicSource;
        AudioSource sfxSource;
        AudioSource stepSource;

        AudioClip bgAmbience;
        AudioClip navTouch;
        AudioClip bridgeDrop;
        AudioClip buttonTouch;
        AudioClip doorSlide;
        AudioClip levelComplete;
        AudioClip pressureSwitch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoInit()
        {
            if (Instance == null)
            {
                var go = new GameObject("[GameAudio]");
                Instance = go.AddComponent<GameAudio>();
                DontDestroyOnLoad(go);
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.35f;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 0.80f;

            stepSource = gameObject.AddComponent<AudioSource>();
            stepSource.loop = false;
            stepSource.playOnAwake = false;
            stepSource.spatialBlend = 0f;
            stepSource.volume = 0.50f;

            LoadClips();
            PlayAmbience();
        }

        void LoadClips()
        {
            bgAmbience = Resources.Load<AudioClip>("Audio/Music/bg_ambience");
            if (bgAmbience == null)
                bgAmbience = Resources.Load<AudioClip>("Audio/SFX/bg_ambience");

            navTouch = Resources.Load<AudioClip>("Audio/SFX/navtouch");
            bridgeDrop = Resources.Load<AudioClip>("Audio/SFX/bridgedrop");
            buttonTouch = Resources.Load<AudioClip>("Audio/SFX/buttontouch");
            doorSlide = Resources.Load<AudioClip>("Audio/SFX/door_slide_on");
            levelComplete = Resources.Load<AudioClip>("Audio/SFX/level_complete");
            pressureSwitch = Resources.Load<AudioClip>("Audio/SFX/pressureswitch");
        }

        public void PlayAmbience()
        {
            if (musicSource == null)
                return;
            if (bgAmbience != null && (!musicSource.isPlaying || musicSource.clip != bgAmbience))
            {
                musicSource.clip = bgAmbience;
                musicSource.Play();
            }
        }

        public void PlayClick(float pitch = 1f)
        {
            PlaySfx(navTouch, 0.7f, pitch * Random.Range(0.96f, 1.04f));
        }

        public void PlayAlign()
        {
            PlaySfx(bridgeDrop, 0.9f, 1f);
        }

        public void PlayButton()
        {
            PlaySfx(buttonTouch, 0.8f, 1f);
        }

        public void PlayDoor()
        {
            PlaySfx(doorSlide, 0.85f, 1f);
        }

        public void PlaySuccess()
        {
            PlaySfx(levelComplete, 0.95f, 1f);
        }

        public void PlaySwitch()
        {
            PlaySfx(pressureSwitch, 0.75f, Random.Range(0.95f, 1.05f));
        }

        public void PlayDogBark()
        {
            // Procedural alert barking tone if audio clip not present
            PlaySfx(pressureSwitch, 1f, 0.65f);
        }

        public void PlayStep()
        {
            if (buttonTouch == null || stepSource == null)
                return;
            stepSource.pitch = Random.Range(1.2f, 1.4f);
            stepSource.PlayOneShot(buttonTouch, 0.12f);
        }

        void PlaySfx(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || sfxSource == null)
                return;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume * 0.8f);
        }
    }
}
