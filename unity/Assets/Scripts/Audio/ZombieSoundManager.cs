using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MobControl.Audio
{
    /// <summary>
    /// AAA-style audio system for Zombie Squad.
    ///
    /// All sounds live in Resources/Audio/AAA and are CC0 (public domain) — see Resources/Audio/AAA/CREDITS.txt.
    ///
    /// Features:
    /// - Sound banks with several variations per event, picked randomly without immediate repeats
    /// - Per-event pitch / volume randomisation so rapid fire and hordes never sound robotic
    /// - Voice limiting per category (e.g. max 4 zombie deaths at once) + a polyphonic voice pool with oldest-voice stealing
    /// - Stereo panning from world X position (zombies on the left sound left)
    /// - Layered music: menu theme -> combat loop -> boss battle theme, with smooth crossfades
    /// - Ducking: music dips under big moments (boss roar, explosions, jingles) and goes muffled (low-pass) when the player is hit
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

        private const string AAA = "Audio/AAA/";
        private const int VOICE_COUNT = 24;

        // ---------------- Sound bank ----------------
        private class Bank
        {
            public AudioClip[] clips;
            public float volume;
            public float pitchMin, pitchMax;
            public float minInterval;   // seconds between triggers of this bank
            public int maxVoices;       // simultaneous voices of this bank
            public float lastTime = -99f;
            public int lastIndex = -1;

            public AudioClip Next()
            {
                if (clips == null || clips.Length == 0) return null;
                if (clips.Length == 1) return clips[0];
                int i = Random.Range(0, clips.Length);
                if (i == lastIndex) i = (i + 1 + Random.Range(0, clips.Length - 1)) % clips.Length;
                lastIndex = i;
                return clips[i];
            }
        }

        private class Voice
        {
            public AudioSource src;
            public Bank bank;
            public float startTime;
        }

        private Bank bGunRifle, bGunHeavy, bHitFlesh, bZombieDie, bZombieGroan, bZombieAttack, bPlayerHurt,
                     bBossRoar, bBossImpact, bExplosion, bGatePowerup, bGunEquip,
                     bUiClick, bUiBack, bUiError, bUiUpgrade, bCoin, bGem, bVictory, bDefeat, bStar;

        private readonly List<Voice> voices = new List<Voice>();
        private AudioSource uiSource;

        // ---------------- Music ----------------
        private AudioSource musicA, musicB;     // crossfade pair
        private AudioSource activeMusic;
        private AudioLowPassFilter musicLowPass;
        private AudioClip clipMenuMusic, clipCombatMusic, clipBossMusic;
        private float musicTarget = 0f;         // target volume of the active track (before ducking)
        private float duckAmount = 1f;          // 1 = no duck
        private float duckRecoverAt = 0f;
        private float muffleUntil = 0f;
        private Coroutine crossfadeRoutine;

        private const float MENU_MUSIC_VOL = 0.60f;
        private const float COMBAT_MUSIC_VOL = 0.30f;  // sits under the gunfire
        private const float BOSS_MUSIC_VOL = 0.42f;

        // ---------------- Settings / state ----------------
        private bool isMusicEnabled = true;
        private bool isSfxEnabled = true;
        private float sfxVolume = 0.9f;
        private bool isCombatMode = false;
        private bool isBossMusic = false;
        private float lastJingleTime = -99f;
        private float nextAmbientGroan = 0f;

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
            if (isMusicEnabled && !isCombatMode) PlayBGM();
        }

        private void InitializeAudio()
        {
            isMusicEnabled = PlayerPrefs.GetInt("ZombieSquad_Music_Enabled", 1) == 1;
            isSfxEnabled = PlayerPrefs.GetInt("ZombieSquad_SFX_Enabled", 1) == 1;

            // Music lives on its own child object so the low-pass filter only affects music, not SFX
            GameObject musicGo = new GameObject("Music");
            musicGo.transform.SetParent(transform, false);
            musicA = CreateMusicSource(musicGo);
            musicB = CreateMusicSource(musicGo);
            musicLowPass = musicGo.AddComponent<AudioLowPassFilter>();
            musicLowPass.cutoffFrequency = 22000f;
            activeMusic = musicA;

            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.spatialBlend = 0f;
            uiSource.priority = 32;

            for (int i = 0; i < VOICE_COUNT; i++)
            {
                AudioSource s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.loop = false;
                s.spatialBlend = 0f;
                s.priority = 128;
                voices.Add(new Voice { src = s });
            }

            //                     clips                                         vol   pitch range    interval voices
            bGunRifle     = MakeBank(Load("gun_rifle_", 5),                        0.55f, 0.95f, 1.06f, 0.060f, 6);
            bGunHeavy     = MakeBank(Load("gun_heavy_", 3),                        0.62f, 0.96f, 1.05f, 0.050f, 6);
            bHitFlesh     = MakeBank(Load("hit_flesh_", 4),                        0.42f, 0.90f, 1.12f, 0.045f, 4);
            bZombieDie    = MakeBank(Load("zombie_die_", 5),                       0.62f, 0.90f, 1.10f, 0.070f, 4);
            bZombieGroan  = MakeBank(Load("zombie_groan_", 8),                     0.40f, 0.88f, 1.08f, 0.500f, 2);
            bZombieAttack = MakeBank(Load("zombie_attack_", 3),                    0.55f, 0.92f, 1.08f, 0.250f, 2);
            bPlayerHurt   = MakeBank(Load("player_hurt_", 3),                      0.80f, 0.95f, 1.05f, 0.120f, 1);
            bBossRoar     = MakeBank(One("boss_roar"),                             1.00f, 0.97f, 1.00f, 1.000f, 1);
            bBossImpact   = MakeBank(Load("boss_impact_", 2),                      0.80f, 0.95f, 1.02f, 0.300f, 1);
            bExplosion    = MakeBank(Load("explosion_", 3),                        0.80f, 0.92f, 1.06f, 0.150f, 2);
            bGatePowerup  = MakeBank(One("gate_powerup"),                          0.75f, 1.00f, 1.00f, 0.100f, 2);
            bGunEquip     = MakeBank(One("gun_upgrade_equip"),                     0.80f, 1.00f, 1.00f, 0.200f, 1);
            bUiClick      = MakeBank(One("ui_click"),                              0.85f, 0.97f, 1.03f, 0.040f, 2);
            bUiBack       = MakeBank(One("ui_back"),                               0.80f, 0.98f, 1.02f, 0.040f, 1);
            bUiError      = MakeBank(One("ui_error"),                              0.70f, 1.00f, 1.00f, 0.150f, 1);
            bUiUpgrade    = MakeBank(One("ui_upgrade"),                            0.85f, 1.00f, 1.00f, 0.100f, 1);
            bCoin         = MakeBank(Load("coin_", 3),                             0.80f, 0.97f, 1.06f, 0.060f, 3);
            bGem          = MakeBank(Load("gem_", 2),                              0.80f, 0.97f, 1.05f, 0.060f, 2);
            bVictory      = MakeBank(One("jingle_victory"),                        0.95f, 1.00f, 1.00f, 1.000f, 1);
            bDefeat       = MakeBank(One("jingle_defeat"),                         0.95f, 1.00f, 1.00f, 1.000f, 1);
            bStar         = MakeBank(One("jingle_star"),                           0.80f, 1.00f, 1.00f, 0.080f, 2);

            clipMenuMusic = Resources.Load<AudioClip>("Audio/bgm_royal_battlefield")
                         ?? Resources.Load<AudioClip>("Audio/bgm_runner_action");
            clipCombatMusic = Resources.Load<AudioClip>(AAA + "music_combat");
            clipBossMusic = Resources.Load<AudioClip>(AAA + "music_boss");

            Debug.Log($"[ZombieSoundManager] AAA audio ready. Guns: {bGunRifle.clips.Length}, Zombie deaths: {bZombieDie.clips.Length}, " +
                      $"Combat music: {clipCombatMusic != null}, Boss music: {clipBossMusic != null}");
        }

        private AudioSource CreateMusicSource(GameObject host)
        {
            AudioSource s = host.AddComponent<AudioSource>();
            s.loop = true;
            s.playOnAwake = false;
            s.volume = 0f;
            s.spatialBlend = 0f;
            s.priority = 0;
            return s;
        }

        private static AudioClip[] Load(string prefix, int count)
        {
            var list = new List<AudioClip>(count);
            for (int i = 1; i <= count; i++)
            {
                AudioClip c = Resources.Load<AudioClip>($"{AAA}{prefix}{i:00}");
                if (c != null) list.Add(c);
            }
            return list.ToArray();
        }

        private static AudioClip[] One(string name)
        {
            AudioClip c = Resources.Load<AudioClip>(AAA + name);
            return c != null ? new[] { c } : new AudioClip[0];
        }

        private static Bank MakeBank(AudioClip[] clips, float vol, float pMin, float pMax, float interval, int maxVoices)
        {
            return new Bank { clips = clips, volume = vol, pitchMin = pMin, pitchMax = pMax, minInterval = interval, maxVoices = maxVoices };
        }

        // ================= Per-frame: music ducking, muffling, ambience =================
        private void Update()
        {
            float now = Time.unscaledTime;

            // Ducking recovers smoothly
            if (now > duckRecoverAt) duckAmount = Mathf.MoveTowards(duckAmount, 1f, Time.unscaledDeltaTime * 1.5f);
            if (activeMusic != null && crossfadeRoutine == null)
                activeMusic.volume = musicTarget * duckAmount;

            // Muffle (player hit) -> low-pass sweep back open
            if (musicLowPass != null)
            {
                float targetCut = now < muffleUntil ? 900f : 22000f;
                float speed = now < muffleUntil ? 60000f : 25000f;
                musicLowPass.cutoffFrequency = Mathf.MoveTowards(musicLowPass.cutoffFrequency, targetCut, speed * Time.unscaledDeltaTime);
            }

            // Ambient horde groans while fighting
            if (isCombatMode && isSfxEnabled && now >= nextAmbientGroan)
            {
                nextAmbientGroan = now + Random.Range(3.5f, 7.0f);
                if (now > 2f) Play(bZombieGroan, 0.55f, Random.Range(-0.7f, 0.7f));
            }
        }

        // ================= Core playback =================
        private Voice GetVoice(Bank bank)
        {
            // Respect per-bank voice limit: steal this bank's oldest voice if it is full
            int active = 0; Voice oldestOfBank = null;
            foreach (var v in voices)
            {
                if (v.bank == bank && v.src.isPlaying)
                {
                    active++;
                    if (oldestOfBank == null || v.startTime < oldestOfBank.startTime) oldestOfBank = v;
                }
            }
            if (bank != null && active >= bank.maxVoices && oldestOfBank != null) return oldestOfBank;

            Voice oldest = null;
            foreach (var v in voices)
            {
                if (!v.src.isPlaying) return v;
                if (oldest == null || v.startTime < oldest.startTime) oldest = v;
            }
            return oldest;
        }

        /// <param name="volumeScale">Extra multiplier on top of the bank volume.</param>
        /// <param name="pan">-1 (left) .. 1 (right).</param>
        private bool Play(Bank bank, float volumeScale = 1f, float pan = 0f, float pitchShift = 1f)
        {
            if (!isSfxEnabled || bank == null) return false;
            float now = Time.unscaledTime;
            if (now - bank.lastTime < bank.minInterval) return false;
            AudioClip clip = bank.Next();
            if (clip == null) return false;
            bank.lastTime = now;

            Voice v = GetVoice(bank);
            if (v == null) return false;
            v.src.Stop();
            v.src.clip = clip;
            v.src.volume = sfxVolume * bank.volume * volumeScale * Random.Range(0.9f, 1.0f);
            v.src.pitch = Random.Range(bank.pitchMin, bank.pitchMax) * pitchShift;
            v.src.panStereo = Mathf.Clamp(pan, -1f, 1f);
            v.src.Play();
            v.bank = bank;
            v.startTime = now;
            return true;
        }

        private void PlayUI(Bank bank)
        {
            if (!isSfxEnabled || bank == null) return;
            float now = Time.unscaledTime;
            if (now - bank.lastTime < bank.minInterval) return;
            AudioClip clip = bank.Next();
            if (clip == null) return;
            bank.lastTime = now;
            uiSource.pitch = Random.Range(bank.pitchMin, bank.pitchMax);
            uiSource.PlayOneShot(clip, sfxVolume * bank.volume);
        }

        /// <summary>Stereo pan from a world position relative to the main camera.</summary>
        private static float PanFor(Vector3 worldPos)
        {
            Camera cam = Camera.main;
            float dx = cam != null ? cam.transform.InverseTransformPoint(worldPos).x : worldPos.x;
            return Mathf.Clamp(dx / 4.5f, -1f, 1f) * 0.6f; // keep it subtle on phone speakers
        }

        private void Duck(float amount, float holdSeconds)
        {
            duckAmount = Mathf.Min(duckAmount, amount);
            duckRecoverAt = Mathf.Max(duckRecoverAt, Time.unscaledTime + holdSeconds);
        }

        /// <summary>Generic one-shot (kept for compatibility).</summary>
        public void PlayClip(AudioClip clip, float volumeScale = 1.0f, float pitch = 1.0f)
        {
            if (!isSfxEnabled || clip == null) return;
            Voice v = GetVoice(null);
            if (v == null) return;
            v.src.Stop();
            v.src.clip = clip;
            v.src.volume = sfxVolume * volumeScale;
            v.src.pitch = pitch;
            v.src.panStereo = 0f;
            v.src.Play();
            v.bank = null;
            v.startTime = Time.unscaledTime;
        }

        // ================= Music =================
        private void SwitchMusic(AudioClip clip, float volume, float fadeSeconds, float delay = 0f)
        {
            if (crossfadeRoutine != null) StopCoroutine(crossfadeRoutine);
            crossfadeRoutine = StartCoroutine(CrossfadeRoutine(clip, volume, fadeSeconds, delay));
        }

        private IEnumerator CrossfadeRoutine(AudioClip clip, float volume, float fade, float delay)
        {
            AudioSource from = activeMusic;
            bool sameClip = clip != null && from != null && from.clip == clip && from.isPlaying;
            AudioSource to = sameClip ? from : (from == musicA ? musicB : musicA);

            // Fade out the old track first if we are waiting (e.g. letting a jingle ring out)
            float fromStart = from != null ? from.volume : 0f;
            if (delay > 0f && !sameClip && from != null)
            {
                float t0 = 0f;
                while (t0 < Mathf.Min(0.4f, delay)) { t0 += Time.unscaledDeltaTime; from.volume = Mathf.Lerp(fromStart, 0f, t0 / 0.4f); yield return null; }
                from.Stop();
                fromStart = 0f;
                float rest = delay - t0;
                if (rest > 0f) yield return new WaitForSecondsRealtime(rest);
            }

            if (clip == null || !isMusicEnabled)
            {
                float tt = 0f;
                while (from != null && tt < fade) { tt += Time.unscaledDeltaTime; from.volume = Mathf.Lerp(fromStart, 0f, tt / fade); yield return null; }
                if (from != null) from.Stop();
                musicTarget = 0f;
                crossfadeRoutine = null;
                yield break;
            }

            if (!sameClip)
            {
                to.clip = clip;
                to.volume = 0f;
                to.Play();
            }

            float toStart = to.volume;
            float e = 0f;
            while (e < fade)
            {
                e += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(e / fade);
                to.volume = Mathf.Lerp(toStart, volume * duckAmount, k);
                if (!sameClip && from != null) from.volume = Mathf.Lerp(fromStart, 0f, k);
                yield return null;
            }
            if (!sameClip && from != null) { from.Stop(); from.volume = 0f; }

            activeMusic = to;
            musicTarget = volume;
            crossfadeRoutine = null;
        }

        public void PlayBGM()
        {
            if (!isMusicEnabled || isCombatMode) return;
            SwitchMusic(clipMenuMusic, MENU_MUSIC_VOL, 0.8f);
        }

        public void StopBGM()
        {
            if (crossfadeRoutine != null) { StopCoroutine(crossfadeRoutine); crossfadeRoutine = null; }
            if (musicA != null) musicA.Stop();
            if (musicB != null) musicB.Stop();
            musicTarget = 0f;
        }

        /// <summary>
        /// Gameplay: crossfade to the combat loop (sits low under the gunfire).
        /// Menus / results: back to the menu theme (after any victory/defeat jingle has rung out).
        /// </summary>
        public void SetGameplayCombatMode(bool inCombat)
        {
            if (inCombat == isCombatMode && (inCombat || activeMusic == null || activeMusic.isPlaying)) return;
            isCombatMode = inCombat;
            isBossMusic = false;
            muffleUntil = 0f;

            if (!isMusicEnabled) return;

            if (inCombat)
            {
                nextAmbientGroan = Time.unscaledTime + 2.5f;
                SwitchMusic(clipCombatMusic, COMBAT_MUSIC_VOL, 1.2f);
            }
            else
            {
                float sinceJingle = Time.unscaledTime - lastJingleTime;
                float delay = sinceJingle < 2.0f ? (2.0f - sinceJingle) : 0f;
                SwitchMusic(clipMenuMusic, MENU_MUSIC_VOL, 1.0f, delay);
            }
        }

        public void ToggleMusic()
        {
            isMusicEnabled = !isMusicEnabled;
            PlayerPrefs.SetInt("ZombieSquad_Music_Enabled", isMusicEnabled ? 1 : 0);
            PlayerPrefs.Save();

            if (!isMusicEnabled) StopBGM();
            else if (isCombatMode) SwitchMusic(isBossMusic ? clipBossMusic : clipCombatMusic, isBossMusic ? BOSS_MUSIC_VOL : COMBAT_MUSIC_VOL, 0.6f);
            else PlayBGM();
        }

        public void ToggleSFX()
        {
            isSfxEnabled = !isSfxEnabled;
            PlayerPrefs.SetInt("ZombieSquad_SFX_Enabled", isSfxEnabled ? 1 : 0);
            PlayerPrefs.Save();
            if (!isSfxEnabled)
                foreach (var v in voices) v.src.Stop();
        }

        // ================= Gameplay events =================

        /// <summary>Rifle shot (normal) or heavy boosted shot (gun upgrade active).</summary>
        public void PlayGunfire(bool isHeavy = false)
        {
            Play(isHeavy ? bGunHeavy : bGunRifle);
        }

        /// <summary>Bullet hits a zombie: wet impact.</summary>
        public void PlayBulletHit(Vector3 worldPos)
        {
            Play(bHitFlesh, 1f, PanFor(worldPos));
        }

        public void PlayZombieDie(bool isBoss = false)
        {
            PlayZombieDie(isBoss, Vector3.zero, false);
        }

        public void PlayZombieDie(bool isBoss, Vector3 worldPos, bool hasPosition = true)
        {
            float pan = hasPosition ? PanFor(worldPos) : 0f;
            if (isBoss)
            {
                // Boss death: big deep death + impact + explosion, music ducks for the moment
                bZombieDie.lastTime = -99f;
                Play(bZombieDie, 1.4f, pan, 0.72f);
                Play(bBossImpact, 1f);
                Play(bExplosion, 0.8f, pan);
                Duck(0.35f, 1.2f);
                return;
            }
            Play(bZombieDie, 1f, pan);
        }

        /// <summary>Zombie notices / starts attacking the player.</summary>
        public void PlayZombieGroan()
        {
            Play(bZombieGroan);
        }

        /// <summary>Zombie bite lands.</summary>
        public void PlayZombieAttack(Vector3 worldPos)
        {
            Play(bZombieAttack, 1f, PanFor(worldPos));
        }

        /// <summary>Player takes damage: punchy hit + music goes muffled for a moment.</summary>
        public void PlayPlayerHurt()
        {
            if (Play(bPlayerHurt))
            {
                muffleUntil = Time.unscaledTime + 0.35f;
                Duck(0.6f, 0.3f);
            }
        }

        /// <summary>Boss appears: roar + sub impact, then the boss battle theme kicks in.</summary>
        public void PlayBossSpawn()
        {
            Play(bBossImpact);
            Play(bBossRoar);
            Duck(0.25f, 2.2f);
            if (isMusicEnabled && clipBossMusic != null && !isBossMusic)
            {
                isBossMusic = true;
                SwitchMusic(clipBossMusic, BOSS_MUSIC_VOL, 1.5f, 0.6f);
            }
        }

        /// <summary>Destructible gate / barricade broken.</summary>
        public void PlayExplosion(Vector3 worldPos)
        {
            if (Play(bExplosion, 1f, PanFor(worldPos))) Duck(0.6f, 0.4f);
        }

        public void PlayGunUpgrade()
        {
            Play(bGunEquip);
            Play(bGatePowerup, 0.7f);
        }

        public void PlayGatePass()
        {
            Play(bGatePowerup);
        }

        public void PlayVictory()
        {
            StopAllGameplayVoices();
            lastJingleTime = Time.unscaledTime;
            Duck(0.0f, 1.8f);
            Play(bVictory);
        }

        public void PlayDefeat()
        {
            StopAllGameplayVoices();
            lastJingleTime = Time.unscaledTime;
            Duck(0.0f, 1.8f);
            Play(bDefeat);
        }

        /// <summary>Result-screen star pop (call once per star with index 0..2 for rising pitch).</summary>
        public void PlayStar(int index = 0)
        {
            bStar.lastTime = -99f;
            Play(bStar, 1f, 0f, 1f + 0.12f * Mathf.Clamp(index, 0, 4));
        }

        private void StopAllGameplayVoices()
        {
            foreach (var v in voices)
            {
                if (v.bank == bGunRifle || v.bank == bGunHeavy || v.bank == bZombieGroan || v.bank == bHitFlesh)
                    v.src.Stop();
            }
        }

        // ================= UI & economy =================
        public void PlayUIClick() { PlayUI(bUiClick); }
        public void PlayUIBack() { PlayUI(bUiBack); }
        public void PlayUIError() { PlayUI(bUiError); }
        public void PlayUpgradeSuccess() { PlayUI(bUiUpgrade); Play(bCoin, 0.6f); }
        public void PlayCoinSpend() { Play(bCoin); }
        public void PlayDiamondSpend() { Play(bGem); }
    }
}
