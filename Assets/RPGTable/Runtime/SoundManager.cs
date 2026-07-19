using UnityEngine;
using System.Collections.Generic;

namespace RPGTable.Runtime
{
    public sealed class SoundManager : MonoBehaviour
    {
        private static SoundManager instance;
        public static SoundManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = GameObject.Find("SoundManager");
                    if (go == null)
                    {
                        go = new GameObject("SoundManager");
                    }
                    instance = go.AddComponent<SoundManager>();
                }
                return instance;
            }
        }

        private AudioSource musicSource;
        private AudioSource sfxSource;

        private AudioClip[] hitSounds;
        private AudioClip[] missSounds;
        private AudioClip[] musicTracks;
        private int currentMusicIndex = 0;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Create AudioSources
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = false; // We will handle transitions in Update
            musicSource.playOnAwake = false;
            musicSource.volume = 0.4f; // Set a reasonable background volume

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.8f;

            LoadAudioClips();

            // Subscribe to scene load events to dynamically ensure an AudioListener exists
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureAudioListener();
        }

        private void Start()
        {
            PlayNextMusicTrack();
        }

        private void Update()
        {
            // If music finished playing, play next track
            if (musicSource != null && !musicSource.isPlaying && musicTracks != null && musicTracks.Length > 0)
            {
                PlayNextMusicTrack();
            }
        }

        private void LoadAudioClips()
        {
            // Load hit sounds from Resources/Sound
            hitSounds = new AudioClip[]
            {
                Resources.Load<AudioClip>("Sound/slicing-sword-strike"),
                Resources.Load<AudioClip>("Sound/strong-swing-and-cut-in-half"),
                Resources.Load<AudioClip>("Sound/sword-saber-swing-in-the-air-2")
            };

            // Load miss sounds from Resources/Sound/miss
            missSounds = new AudioClip[]
            {
                Resources.Load<AudioClip>("Sound/miss/short-sharp-sword-strike")
            };

            // Load music tracks from Resources/Sound/music
            musicTracks = new AudioClip[]
            {
                Resources.Load<AudioClip>("Sound/music/echoes-of-ancient-realms_94978"),
                Resources.Load<AudioClip>("Sound/music/mystical-glade_94962")
            };

            Debug.Log($"[SoundManager] Loaded: hits={CountValid(hitSounds)}, misses={CountValid(missSounds)}, music={CountValid(musicTracks)}");
        }

        private int CountValid(AudioClip[] clips)
        {
            if (clips == null) return 0;
            int count = 0;
            foreach (var clip in clips)
            {
                if (clip != null) count++;
            }
            return count;
        }

        private void PlayNextMusicTrack()
        {
            if (musicTracks == null || musicTracks.Length == 0) return;

            // Find a valid track
            for (int i = 0; i < musicTracks.Length; i++)
            {
                var track = musicTracks[currentMusicIndex];
                if (track != null)
                {
                    musicSource.clip = track;
                    musicSource.Play();
                    Debug.Log($"[SoundManager] Playing music track: {track.name}");
                    currentMusicIndex = (currentMusicIndex + 1) % musicTracks.Length;
                    return;
                }
                currentMusicIndex = (currentMusicIndex + 1) % musicTracks.Length;
            }
        }

        public void PlayHit()
        {
            PlayRandomSFX(hitSounds);
        }

        public void PlayMiss()
        {
            PlayRandomSFX(missSounds);
        }

        /// <summary>Воспроизвести конкретный клип. Если null — сыграть дефолтный хит.</summary>
        public void PlayAbilitySound(AudioClip clip, bool fallbackToHit = true)
        {
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
            else if (fallbackToHit)
            {
                PlayRandomSFX(hitSounds);
            }
        }

        /// <summary>Воспроизвести дефолтный звук промаха, или конкретный клип если задан.</summary>
        public void PlayMissSound(AudioClip clip = null)
        {
            if (clip != null)
                sfxSource.PlayOneShot(clip);
            else
                PlayRandomSFX(missSounds);
        }

        private void PlayRandomSFX(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;

            var validClips = new List<AudioClip>();
            foreach (var clip in clips)
            {
                if (clip != null) validClips.Add(clip);
            }

            if (validClips.Count == 0) return;

            int index = Random.Range(0, validClips.Count);
            sfxSource.PlayOneShot(validClips[index]);
            Debug.Log($"[SoundManager] Playing SFX: {validClips[index].name}");
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            EnsureAudioListener();
        }

        private void EnsureAudioListener()
        {
            var listener = FindFirstObjectByType<AudioListener>();
            if (listener == null)
            {
                // Try to find the Main Camera
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    mainCam.gameObject.AddComponent<AudioListener>();
                    Debug.Log($"[SoundManager] Added AudioListener to Main Camera ({mainCam.name})");
                }
                else
                {
                    // Fallback: Add to any Camera
                    var anyCam = FindFirstObjectByType<Camera>();
                    if (anyCam != null)
                    {
                        anyCam.gameObject.AddComponent<AudioListener>();
                        Debug.Log($"[SoundManager] Added AudioListener to Camera ({anyCam.name})");
                    }
                    else
                    {
                        // Last resort fallback: Add to SoundManager itself
                        gameObject.AddComponent<AudioListener>();
                        Debug.Log("[SoundManager] No cameras found. Added AudioListener to SoundManager game object.");
                    }
                }
            }
        }
    }
}
