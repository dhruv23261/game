using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MobControl.Combat
{
    /// <summary>
    /// Futuristic, silky-smooth reward arrival sequence:
    /// - Glowing flying energy core arcing toward player
    /// - Pre-cached VFX textures, shaders, and materials (Zero frame freeze / stutter on impact)
    /// - High-tech orbital sky energy beam for weapon overcharge
    /// - Flat cybernetic road ripples & electric sparks
    /// - Childish, playful, bouncy cartoon-game 3D celebratory text with Fredoka-Bold SDF font
    /// </summary>
    public class FlyingRewardSequence : MonoBehaviour
    {
        // Pre-cached resources to eliminate any runtime pause or disk I/O hitch
        private static Shader s_vfxShader;
        private static Texture2D s_sparkTex;
        private static Texture2D s_beamTex;
        private static Texture2D s_padTex;
        private static TMP_FontAsset s_childishFont;
        private static bool s_isPrewarmed = false;

        public static void Launch(Vector3 startPos, PlayerRunnerController hero, GateRewardType rewardType, int multiplier, float gunSpeedMultiplier)
        {
            if (hero == null) return;
            EnsureStaticResources();

            GameObject obj = new GameObject("VFX_FlyingRewardSequence");
            obj.transform.position = startPos;
            var seq = obj.AddComponent<FlyingRewardSequence>();
            seq.StartFlight(startPos, hero, rewardType, multiplier, gunSpeedMultiplier);
        }

        private static void EnsureStaticResources()
        {
            if (s_isPrewarmed) return;

            s_vfxShader = Shader.Find("ZombieSquad/VFX_FireAdditive") 
                       ?? Shader.Find("Sprites/Default") 
                       ?? Shader.Find("Mobile/Particles/Additive");

            s_sparkTex = LoadTextureCached("Textures/VFX/tex_spark_ember");
            s_beamTex = LoadTextureCached("Textures/VFX/tex_cyber_energy_beam");
            s_padTex = LoadTextureCached("Textures/VFX/tex_cyber_teleport_pad");

            // Load Childish / Playful Cartoon Font (Fredoka-Bold SDF)
            s_childishFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Fredoka-Bold SDF")
                          ?? Resources.Load<TMP_FontAsset>("Fredoka-Bold SDF");

#if UNITY_EDITOR
            if (s_childishFont == null)
            {
                s_childishFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fredoka-Bold SDF.asset");
            }
#endif

            s_isPrewarmed = true;
        }

        private static Texture2D LoadTextureCached(string relPath)
        {
            Texture2D tex = Resources.Load<Texture2D>(relPath);
#if UNITY_EDITOR
            if (tex == null)
            {
                tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/" + relPath + ".png");
            }
#endif
            return tex;
        }

        private void StartFlight(Vector3 startPos, PlayerRunnerController hero, GateRewardType rewardType, int multiplier, float gunSpeedMultiplier)
        {
            StartCoroutine(FlightRoutine(startPos, hero, rewardType, multiplier, gunSpeedMultiplier));
        }

        private IEnumerator FlightRoutine(Vector3 startPos, PlayerRunnerController hero, GateRewardType rewardType, int multiplier, float gunSpeedMultiplier)
        {
            bool isGun = (rewardType == GateRewardType.GunUpgrade);
            // Glowing Cyber Purple for gun boost; Radiant Yellow for squad multiplier
            Color themeColor = isGun ? new Color(0.85f, 0.25f, 1.0f, 1f) : new Color(1f, 0.85f, 0.15f, 1f);

            // 1. Build the Glowing Flying Reward Core
            GameObject orbVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orbVisual.name = "OrbVisual";
            orbVisual.transform.SetParent(transform, false);
            orbVisual.transform.localScale = Vector3.one * 0.38f;
            Collider c = orbVisual.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Material orbMat = new Material(s_vfxShader ?? Shader.Find("Sprites/Default"));
            orbMat.color = themeColor;
            if (s_sparkTex != null) orbMat.mainTexture = s_sparkTex;
            orbVisual.GetComponent<Renderer>().sharedMaterial = orbMat;

            // 2. Trailing Sparkle Particle System on the Orb
            ParticleSystem trailPs = orbVisual.AddComponent<ParticleSystem>();
            var main = trailPs.main;
            main.startLifetime = 0.30f;
            main.startSpeed = 0.5f;
            main.startSize = 0.14f;
            main.startColor = themeColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = true;

            var emission = trailPs.emission;
            emission.rateOverTime = 25f;

            var shape = trailPs.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var psRenderer = orbVisual.GetComponent<ParticleSystemRenderer>();
            if (s_vfxShader != null) psRenderer.sharedMaterial = new Material(s_vfxShader);
            trailPs.Play();

            // 3. Parabolic Flight Motion toward Player Hero
            float duration = 0.42f;
            float elapsed = 0f;
            Vector3 origin = startPos + Vector3.up * 0.8f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 targetPos = (hero != null) 
                    ? hero.transform.position + new Vector3(0f, 1.0f, 0.2f) 
                    : origin;

                // High energetic arc
                float heightArc = Mathf.Sin(t * Mathf.PI) * 1.75f;
                Vector3 currentPos = Vector3.Lerp(origin, targetPos, t);
                currentPos.y += heightArc;

                transform.position = currentPos;
                orbVisual.transform.Rotate(Vector3.forward, 360f * Time.deltaTime);

                yield return null;
            }

            // 5. Impact at the Player Hero / Squad Position!
            Vector3 impactPos = (hero != null) ? hero.transform.position : transform.position;
            Destroy(orbVisual);

            // Trigger futuristic arrival VFX with pre-cached materials (Zero stutter)
            if (isGun)
            {
                SpawnOrbitalSkyBeam(impactPos, themeColor);
            }
            else
            {
                SpawnCyberGroundRipple(impactPos, themeColor);
            }

            // Apply Reward Gameplay Logic
            if (hero != null && !hero.IsDead)
            {
                if (isGun)
                {
                    hero.UpgradeGunAbility(gunSpeedMultiplier, enableColorfulBullets: true);
                    SpawnFloatingPowerUpBadge(impactPos + Vector3.up * 2.1f, "purple_boost_15s", themeColor, 1.35f);
                }
                else
                {
                    float gunMult = Mathf.Max(2f, (float)multiplier);
                    hero.ApplyGunSpeedMultiplier(gunMult);

                    int currentSquad = hero.TotalSquadSize;
                    int alliesToAdd = (currentSquad < 2) ? 1 : 0;
                    if (alliesToAdd > 0)
                    {
                        hero.SpawnSquadAllies(alliesToAdd);
                    }

                    SpawnFloatingPowerUpBadge(impactPos + Vector3.up * 2.1f, "score_2x", themeColor, 1.35f);
                }
            }

            Destroy(gameObject, 2.0f);
        }

        private static void SpawnOrbitalSkyBeam(Vector3 pos, Color themeCol)
        {
            GameObject beamObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            beamObj.name = "VFX_OrbitalSkyBeam";
            beamObj.transform.position = pos + new Vector3(0f, 5.0f, 0f);
            beamObj.transform.localScale = new Vector3(0.85f, 10.0f, 1f);
            Collider c = beamObj.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Material beamMat = new Material(s_vfxShader ?? Shader.Find("Sprites/Default"));
            beamMat.color = themeCol;
            if (s_beamTex != null) beamMat.mainTexture = s_beamTex;
            beamObj.GetComponent<Renderer>().sharedMaterial = beamMat;

            beamObj.AddComponent<SkyBeamAnimator>().Initialize(pos, themeCol);
        }

        private static void SpawnCyberGroundRipple(Vector3 pos, Color themeCol)
        {
            GameObject rippleObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            rippleObj.name = "VFX_CyberGroundRipple";
            rippleObj.transform.position = new Vector3(pos.x, 0.03f, pos.z);
            rippleObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            rippleObj.transform.localScale = Vector3.one * 0.8f;
            Collider c = rippleObj.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Material ripMat = new Material(s_vfxShader ?? Shader.Find("Sprites/Default"));
            ripMat.color = themeCol;
            if (s_padTex != null) ripMat.mainTexture = s_padTex;
            rippleObj.GetComponent<Renderer>().sharedMaterial = ripMat;

            rippleObj.AddComponent<GroundRippleAnimator>().Initialize(themeCol);
        }

        public static void SpawnFloatingPowerUpBadge(Vector3 pos, string badgeName, Color glowColor, float targetWorldWidth = 1.35f)
        {
            Sprite sp = Resources.Load<Sprite>($"UI/PowerUps/{badgeName}");
            if (sp == null)
            {
                Debug.LogWarning($"[FlyingRewardSequence] Badge sprite UI/PowerUps/{badgeName} not found!");
                return;
            }

            GameObject badgeObj = new GameObject($"BadgePop_{badgeName}");
            badgeObj.transform.position = pos;

            SpriteRenderer sr = badgeObj.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = 55;

            // Ambient glowing backplane halo
            GameObject haloObj = new GameObject("HaloBacking");
            haloObj.transform.SetParent(badgeObj.transform, false);
            haloObj.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            haloObj.transform.localScale = Vector3.one * 1.12f;
            SpriteRenderer haloSr = haloObj.AddComponent<SpriteRenderer>();
            haloSr.sprite = sp;
            haloSr.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.45f);
            haloSr.sortingOrder = 54;

            float baseW = (sp.bounds.size.x > 0.01f) ? sp.bounds.size.x : 4.72f;
            float normalizedScale = targetWorldWidth / baseW;

            badgeObj.AddComponent<FloatingBadgeBannerAnimator>().Initialize(pos, sr, haloSr, normalizedScale);
        }

        private static void SpawnFloatingCyberBanner(Vector3 pos, string richText, Color glowCol)
        {
            GameObject bannerObj = new GameObject("Celebratory3DBanner");
            bannerObj.transform.position = pos;

            TextMeshPro tmp = bannerObj.AddComponent<TextMeshPro>();
            if (s_childishFont != null) tmp.font = s_childishFont;
            tmp.text = richText;
            tmp.fontSize = 5.4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.35f;
            tmp.outlineColor = new Color(0.10f, 0.02f, 0.20f, 1f);

            bannerObj.AddComponent<FloatingBannerAnimator>().Initialize(pos);
        }
    }

    public class SkyBeamAnimator : MonoBehaviour
    {
        private Vector3 targetGroundPos;
        private float elapsed = 0f;
        private float duration = 0.28f;
        private Renderer rend;
        private MaterialPropertyBlock mpb;
        private Color baseColor;

        public void Initialize(Vector3 groundPos, Color color)
        {
            targetGroundPos = groundPos;
            baseColor = color;
            rend = GetComponent<Renderer>();
            mpb = new MaterialPropertyBlock();
            if (Camera.main != null)
            {
                Vector3 euler = Camera.main.transform.eulerAngles;
                transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float width = Mathf.Lerp(1.2f, 0f, t);
            transform.localScale = new Vector3(width, 10.0f, 1f);

            if (rend != null && mpb != null)
            {
                rend.GetPropertyBlock(mpb);
                Color c = baseColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                mpb.SetColor("_Color", c);
                rend.SetPropertyBlock(mpb);
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }

    public class GroundRippleAnimator : MonoBehaviour
    {
        private float elapsed = 0f;
        private float duration = 0.35f;
        private Renderer rend;
        private MaterialPropertyBlock mpb;
        private Color baseColor;

        public void Initialize(Color color)
        {
            baseColor = color;
            rend = GetComponent<Renderer>();
            mpb = new MaterialPropertyBlock();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.Lerp(0.8f, 3.2f, t);
            transform.localScale = new Vector3(scale, scale, 1f);

            if (rend != null && mpb != null)
            {
                rend.GetPropertyBlock(mpb);
                Color c = baseColor;
                c.a = Mathf.Lerp(0.9f, 0f, t);
                mpb.SetColor("_Color", c);
                rend.SetPropertyBlock(mpb);
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }

    public class FloatingBannerAnimator : MonoBehaviour
    {
        private Vector3 startPos;
        private float elapsed = 0f;
        private float duration = 1.35f;
        private TextMeshPro tmp;

        public void Initialize(Vector3 pos)
        {
            startPos = pos;
            tmp = GetComponent<TextMeshPro>();
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Childish, juicy elastic cartoon overshoot bounce: 0 -> 1.38 -> 0.94 -> 1.06 -> 1.0
            float scale;
            if (t < 0.22f)
            {
                float subT = t / 0.22f;
                scale = Mathf.Lerp(0f, 1.38f, Mathf.Sin(subT * Mathf.PI * 0.5f));
            }
            else if (t < 0.40f)
            {
                float subT = (t - 0.22f) / 0.18f;
                scale = Mathf.Lerp(1.38f, 0.94f, subT);
            }
            else if (t < 0.55f)
            {
                float subT = (t - 0.40f) / 0.15f;
                scale = Mathf.Lerp(0.94f, 1.06f, subT);
            }
            else
            {
                float subT = (t - 0.55f) / 0.45f;
                scale = Mathf.Lerp(1.06f, 1.0f, subT);
            }

            // Playful cartoon tilt oscillation
            float wobble = (t < 0.65f) ? Mathf.Sin(elapsed * 16f) * (1f - t) * 6.5f : 0f;

            transform.localScale = Vector3.one * scale;

            // Float upward smoothly with deceleration
            float floatY = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.6f;
            transform.position = startPos + Vector3.up * floatY;

            // Face camera with playful tilt
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation * Quaternion.Euler(0f, 0f, wobble);
            }

            // Smooth fadeout in final 25% of duration
            if (t > 0.75f && tmp != null)
            {
                float fade = 1f - ((t - 0.75f) / 0.25f);
                Color c = tmp.color;
                c.a = fade;
                tmp.color = c;
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }

    public class FloatingBadgeBannerAnimator : MonoBehaviour
    {
        private Vector3 startPos;
        private float elapsed = 0f;
        private float duration = 1.35f;
        private SpriteRenderer mainSr;
        private SpriteRenderer haloSr;
        private float baseScale = 0.28f;

        public void Initialize(Vector3 pos, SpriteRenderer sr, SpriteRenderer halo, float scale)
        {
            startPos = pos;
            mainSr = sr;
            haloSr = halo;
            baseScale = scale;
            transform.localScale = Vector3.zero;
        }

        private void Start()
        {
            // Guaranteed self-destruction so it can NEVER persist or stay fixed in the world
            Destroy(gameObject, duration + 0.1f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Juicy cartoon elastic bounce: 0 -> 1.30 -> 0.96 -> 1.03 -> 1.0
            float bounce;
            if (t < 0.20f)
            {
                float subT = t / 0.20f;
                bounce = Mathf.Lerp(0f, 1.30f, Mathf.Sin(subT * Mathf.PI * 0.5f));
            }
            else if (t < 0.38f)
            {
                float subT = (t - 0.20f) / 0.18f;
                bounce = Mathf.Lerp(1.30f, 0.96f, subT);
            }
            else if (t < 0.52f)
            {
                float subT = (t - 0.38f) / 0.14f;
                bounce = Mathf.Lerp(0.96f, 1.03f, subT);
            }
            else
            {
                float subT = (t - 0.52f) / 0.48f;
                bounce = Mathf.Lerp(1.03f, 1.0f, subT);
            }

            transform.localScale = Vector3.one * (bounce * baseScale);

            // Smooth upward float
            float floatY = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.4f;
            transform.position = startPos + Vector3.up * floatY;

            // Face camera with subtle energetic tilt
            float wobble = (t < 0.60f) ? Mathf.Sin(elapsed * 15f) * (1f - t) * 4.0f : 0f;
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation * Quaternion.Euler(0f, 0f, wobble);
            }

            // Smooth fadeout in final 25% of duration
            if (t > 0.75f)
            {
                float fade = 1f - ((t - 0.75f) / 0.25f);
                if (mainSr != null)
                {
                    Color mc = mainSr.color;
                    mc.a = fade;
                    mainSr.color = mc;
                }
                if (haloSr != null)
                {
                    Color hc = haloSr.color;
                    hc.a = fade * 0.45f;
                    haloSr.color = hc;
                }
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }

    public class CameraFacingBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
}

