using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MobControl.Audio
{
    /// <summary>
    /// Professional AAA Audio System for Zombie Squad.
    /// Features:
    /// - Intense Cinematic Horror-Action BGM for Menus & Home Screen (gives goosebumps!)
    /// - Automatic Combat Audio Ducking: Stops BGM in-game so real gunshots & zombie screams dominate
    /// - Real military gunfire & rifle shots with natural pitch variation
    /// - Real zombie death screams, shrieks, guttural groans, boss roars & blood splatters
    /// - Real Kenney CC0 UI clicks, coin chimes & upgrade fanfares
    /// </summary>
    public class ZombieSoundManager : MonoBehaviour
    {
        private static ZombieSoundManager s_instance;
        public static ZombieSoundManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = Object.FindAnyObjectByType<ZombieSoundManager>();
                    if (s_instance == null)
                    {
                        GameObject go = new GameObject("[ZombieSoundManager]");
                        s_instance = go.AddComponent<ZombieSoundManager>();
                        if (Application.isPlaying) DontDestroyOnLoad(go);
                    }
                }
                return s_instance;
            }
        }

        [Header("Audio Sources")]
        private AudioSource bgmSource;
        private AudioSource uiSource;
        private List<AudioSource> sfxPool = new List<AudioSource>();
        private const int SFX_POOL_SIZE = 12; // High polyphony for rapid gunfire & crowd splats
        private int currentSfxIndex = 0;

        [Header("Guns & Combat")]
        private AudioClip clipGunfire;
        private AudioClip clipGunfireHeavy;

        [Header("Zombie SFX")]
        private AudioClip clipDeathScreech;
        private AudioClip clipDeathGroan;
        private AudioClip clipBossRoar;
        private AudioClip clipZombieSplat;
        private AudioClip clipZombieAttack;
        private AudioClip clipZombieMoan;

        [Header("UI & Economy")]
        private AudioClip clipUiClick;
        private AudioClip clipUpgrade;
        private AudioClip clipCoin;
        private AudioClip clipDiamond;
        private AudioClip clipGatePass;

        [Header("Soundtrack")]
        private AudioClip clipBgmHorror;

        [Header("Settings")]
        private bool isMusicEnabled = true;
        private bool isSfxEnabled = true;
        private float musicVolume = 0.65f;
        private float sfxVolume = 0.90f;
        private bool isCombatMode = false;
        private Coroutine bgmFadeRoutine = null;

        public bool IsMusicEnabled => isMusicEnabled;
        public bool IsSfxEnabled => isSfxEnabled;
        public bool IsCombatMode => isCombatMode;

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            InitializeAudio();
        }

        private void Start()
        {
            if (isMusicEnabled && !isCombatMode)
            {
                PlayBGM();
            }
        }

        private void InitializeAudio()
        {
            isMusicEnabled = PlayerPrefs.GetInt("ZombieSquad_Music_Enabled", 1) == 1;
            isSfxEnabled = PlayerPrefs.GetInt("ZombieSquad_SFX_Enabled", 1) == 1;

            // 1. Setup BGM AudioSource
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = musicVolume;
            bgmSource.spatialBlend = 0f;

            // 2. Setup UI AudioSource
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.loop = false;
            uiSource.playOnAwake = false;
            uiSource.volume = sfxVolume;
            uiSource.spatialBlend = 0f;

            // 3. Setup SFX Polyphonic Pool
            for (int i = 0; i < SFX_POOL_SIZE; i++)
            {
                AudioSource s = gameObject.AddComponent<AudioSource>();
                s.loop = false;
                s.playOnAwake = false;
                s.volume = sfxVolume;
                s.spatialBlend = 0f;
                sfxPool.Add(s);
            }

            // 4. Load High Quality Audio Clips
            // Gunshots
            clipGunfire = Resources.Load<AudioClip>("Audio/sfx_gunfire_real") 
                       ?? Resources.Load<AudioClip>("Audio/sfx_gunfire");
            clipGunfireHeavy = Resources.Load<AudioClip>("Audio/sfx_gunfire_boost")
                            ?? Resources.Load<AudioClip>("Audio/sfx_shot_rifle") 
                            ?? Resources.Load<AudioClip>("Audio/sfx_gunfire_heavy");

            // Zombie Reactions
            clipDeathScreech = Resources.Load<AudioClip>("Audio/sfx_zombie_death_screech")
                            ?? Resources.Load<AudioClip>("Audio/sfx_zombie_die");
            clipDeathGroan = Resources.Load<AudioClip>("Audio/sfx_zombie_death_groan");
            clipBossRoar = Resources.Load<AudioClip>("Audio/sfx_zombie_boss_roar");
            clipZombieSplat = Resources.Load<AudioClip>("Audio/sfx_zombie_splat");
            clipZombieAttack = Resources.Load<AudioClip>("Audio/sfx_zombie_angry_attack")
                            ?? Resources.Load<AudioClip>("Audio/sfx_zombie_groan");
            clipZombieMoan = Resources.Load<AudioClip>("Audio/sfx_zombie_moan");

            // UI & Economy
            clipUiClick = Resources.Load<AudioClip>("Audio/sfx_ui_click_real")
                       ?? Resources.Load<AudioClip>("Audio/sfx_ui_click");
            clipUpgrade = Resources.Load<AudioClip>("Audio/sfx_upgrade");
            clipCoin = Resources.Load<AudioClip>("Audio/sfx_coin");
            clipDiamond = Resources.Load<AudioClip>("Audio/sfx_diamond");
            clipGatePass = Resources.Load<AudioClip>("Audio/sfx_gate_pass");

            // BGM (Royal Battlefield Theme)
            clipBgmHorror = Resources.Load<AudioClip>("Audio/bgm_royal_battlefield")
                         ?? Resources.Load<AudioClip>("Audio/bgm_zombie_theme")
                         ?? Resources.Load<AudioClip>("Audio/bgm_runner_action");

            Debug.Log($"[ZombieSoundManager] Ready! Real Guns: {(clipGunfire != null)}, Death Screams: {(clipDeathScreech != null)}, Royal BGM: {(clipBgmHorror != null)}");
        }

        #region BGM Management & Combat Audio Mode
        public void PlayBGM()
        {
            if (bgmSource == null || clipBgmHorror == null || !isMusicEnabled || isCombatMode) return;

            if (bgmSource.clip != clipBgmHorror)
            {
                bgmSource.clip = clipBgmHorror;
            }

            if (!bgmSource.isPlaying)
            {
                bgmSource.volume = musicVolume;
                bgmSource.Play();
            }
        }

        public void StopBGM()
        {
            if (bgmSource != null) bgmSource.Stop();
        }

        /// <summary>
        /// When player enters gameplay (Level 1, etc.), mute/stop the music so ONLY real gunfire and zombies echo.
        /// When player returns to Home Screen / Menus, restore the intense horror soundtrack.
        /// </summary>
        public void SetGameplayCombatMode(bool inCombat)
        {
            isCombatMode = inCombat;

            if (bgmFadeRoutine != null) StopCoroutine(bgmFadeRoutine);

            if (inCombat)
            {
                // In game: Fade out music fast (0.35s) so gunshots & screaming zombies take full focus!
                bgmFadeRoutine = StartCoroutine(FadeBgmRoutine(0f, 0.35f, true));
            }
            else
            {
                // In menus: Restore the atmospheric horror zombie theme
                if (isMusicEnabled)
                {
                    if (bgmSource != null && !bgmSource.isPlaying)
                    {
                        if (clipBgmHorror != null) bgmSource.clip = clipBgmHorror;
                        bgmSource.Play();
                    }
                    bgmFadeRoutine = StartCoroutine(FadeBgmRoutine(musicVolume, 0.5f, false));
                }
            }
        }

        private IEnumerator FadeBgmRoutine(float targetVol, float duration, bool stopOnZero)
        {
            if (bgmSource == null) yield break;
            float startVol = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, targetVol, elapsed / duration);
                yield return null;
            }

            bgmSource.volume = targetVol;
            if (stopOnZero && targetVol <= 0.01f)
            {
                bgmSource.Stop();
            }
        }

        public void ToggleMusic()
        {
            isMusicEnabled = !isMusicEnabled;
            PlayerPrefs.SetInt("ZombieSquad_Music_Enabled", isMusicEnabled ? 1 : 0);
            PlayerPrefs.Save();

            if (!isMusicEnabled)
            {
                StopBGM();
            }
            else if (!isCombatMode)
            {
                PlayBGM();
            }
        }

        public void ToggleSFX()
        {
            isSfxEnabled = !isSfxEnabled;
            PlayerPrefs.SetInt("ZombieSquad_SFX_Enabled", isSfxEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }
        #endregion

        #region SFX Playback
        private AudioSource GetAvailableSfxSource()
        {
            if (sfxPool == null || sfxPool.Count == 0) return null;
            AudioSource src = sfxPool[currentSfxIndex];
            currentSfxIndex = (currentSfxIndex + 1) % sfxPool.Count;
            return src;
        }

        public void PlayClip(AudioClip clip, float volumeScale = 1.0f, float pitch = 1.0f)
        {
            if (!isSfxEnabled || clip == null) return;

            AudioSource src = GetAvailableSfxSource();
            if (src != null)
            {
                src.pitch = pitch;
                src.PlayOneShot(clip, sfxVolume * volumeScale);
            }
        }

        private float lastGunfireTime = -1f;

        /// <summary>
        /// Kid-friendly cartoon arcade pop blaster sound.
        /// Playful, bouncy, round, and pleasant for children and casual players.
        /// </summary>
        public void PlayGunfire(bool isHeavy = false)
        {
            if (!isSfxEnabled) return;

            // Minimum interval between shots to prevent audio mud / clipping when rapid firing
            float minInterval = isHeavy ? 0.052f : 0.075f;
            if (Time.unscaledTime - lastGunfireTime < minInterval) return;
            lastGunfireTime = Time.unscaledTime;

            if (clipGunfire == null) clipGunfire = Resources.Load<AudioClip>("Audio/sfx_gunfire_real") ?? Resources.Load<AudioClip>("Audio/sfx_gunfire");
            if (clipGunfireHeavy == null) clipGunfireHeavy = Resources.Load<AudioClip>("Audio/sfx_gunfire_boost") ?? Resources.Load<AudioClip>("Audio/sfx_shot_rifle") ?? clipGunfire;

            AudioClip clip = isHeavy ? (clipGunfireHeavy ?? clipGunfire) : clipGunfire;
            if (clip == null) return;

            float pitch = isHeavy ? Random.Range(1.02f, 1.08f) : Random.Range(0.97f, 1.05f);
            float vol = isHeavy ? 0.55f : 0.48f;
            PlayClip(clip, vol, pitch);
        }

        private float lastZombieDieTime = -1f;

        /// <summary>
        /// Kid-friendly comic defeat pop / bubble poof.
        /// Screams, shrieks, and groans have been COMPLETELY REMOVED.
        /// </summary>
        public void PlayZombieDie(bool isBoss = false)
        {
            if (!isSfxEnabled) return;

            // Rate-limit defeat pop so wiping out 10 zombies at once doesn't blow out audio
            if (Time.unscaledTime - lastZombieDieTime < 0.055f) return;
            lastZombieDieTime = Time.unscaledTime;

            if (clipZombieSplat == null)
            {
                clipZombieSplat = Resources.Load<AudioClip>("Audio/sfx_zombie_splat") ?? Resources.Load<AudioClip>("Audio/sfx_zombie_die");
            }

            if (clipZombieSplat != null)
            {
                float pitch = isBoss ? Random.Range(0.85f, 0.95f) : Random.Range(0.95f, 1.15f);
                PlayClip(clipZombieSplat, 0.55f, pitch);
            }
        }

        /// <summary>
        /// Aggressive groans and pig-like shrieks are completely disabled for kid-friendly gameplay.
        /// </summary>
        public void PlayZombieGroan()
        {
            // Kept silent as requested (no screaming or pig-like shrieking)
        }

        private float lastClickTime = -1f;

        public void PlayUIClick()
        {
            if (!isSfxEnabled) return;
            if (Time.unscaledTime - lastClickTime < 0.04f) return;
            lastClickTime = Time.unscaledTime;

            if (clipUiClick == null)
            {
                clipUiClick = Resources.Load<AudioClip>("Audio/sfx_ui_click");
            }

            if (uiSource != null && clipUiClick != null)
            {
                uiSource.pitch = Random.Range(0.97f, 1.03f);
                uiSource.PlayOneShot(clipUiClick, sfxVolume * 1.0f);
            }
            else if (clipUiClick != null)
            {
                PlayClip(clipUiClick, 1.0f, Random.Range(0.97f, 1.03f));
            }
        }

        public void PlayUpgradeSuccess()
        {
            PlayClip(clipUpgrade, 1.0f, 1.0f);
        }

        public void PlayCoinSpend()
        {
            PlayClip(clipCoin, 0.95f, Random.Range(0.98f, 1.05f));
        }

        public void PlayDiamondSpend()
        {
            PlayClip(clipDiamond, 1.0f, Random.Range(0.98f, 1.04f));
        }

        public void PlayGatePass()
        {
            PlayClip(clipGatePass, 1.0f, 1.0f);
        }
        #endregion
    }
}
