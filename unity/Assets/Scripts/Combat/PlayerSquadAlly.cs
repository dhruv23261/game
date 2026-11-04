using UnityEngine;
using MobControl.Crowd;
using MobControl.Core;

namespace MobControl.Combat
{
    public class PlayerSquadAlly : MonoBehaviour
    {
        private PlayerRunnerController leader;
        private Vector3 formationOffset;
        private Animator animator;
        private float fireTimer = 0f;
        private float fireRate = 0.12f;
        private Vector3 muzzleOffset = new Vector3(0.18f, 0.70f, 0.65f);
        private GameObject bulletPrefab;
        private MobUnit mobUnit;
        private bool isInitialized = false;
        private GameObject equippedWeaponInstance;
        private Transform muzzleFirePoint;

        public PlayerRunnerController Leader => leader;

        public void SetFormationOffset(Vector3 newOffset)
        {
            formationOffset = newOffset;
        }

        public void Initialize(PlayerRunnerController leaderRef, Vector3 offset, GameObject bulletRef, Vector3 muzzleOff, float rate)
        {
            leader = leaderRef;
            formationOffset = offset;
            bulletPrefab = bulletRef;
            muzzleOffset = muzzleOff;
            fireRate = rate;

            animator = GetComponent<Animator>();
            mobUnit = GetComponent<MobUnit>();

            Material mat = Resources.Load<Material>("Materials/PlayerMaterial");
#if UNITY_EDITOR
            if (mat == null) mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlayerMaterial.mat");
#endif
            if (mat != null)
            {
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    if (r.GetComponent<LineRenderer>() == null && !r.name.Contains("Gun") && !r.name.Contains("M4") && !r.name.Contains("Receiver") && !r.name.Contains("Barrel") && !r.name.Contains("Magazine") && !r.name.Contains("Stock") && !r.name.Contains("Sight"))
                    {
                        r.sharedMaterial = mat;
                    }
                }
            }

            // Tag and set triggers on all colliders so they never block player or ally bullets
            gameObject.tag = "Player";
            foreach (var c in GetComponentsInChildren<Collider>(true))
            {
                c.isTrigger = true;
                c.gameObject.tag = "Player";
            }

            // Purge and destroy any Gun_M41D on cloned ally
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name.Contains("Gun_M41D"))
                {
                    if (Application.isPlaying) Destroy(t.gameObject);
                    else DestroyImmediate(t.gameObject);
                }
            }

            // Find equipped weapon if cloned
            equippedWeaponInstance = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t != null && (t.name.Contains("AssaultRifle") || t.name.Contains("Weapon")))
                {
                    equippedWeaponInstance = t.gameObject;
                    Transform gunMp = t.Find("MuzzleFirePoint");
                    if (gunMp != null)
                    {
                        muzzleFirePoint = gunMp;
                    }
                    break;
                }
            }

            if (muzzleFirePoint == null)
            {
                Transform rightHand = PlayerRunnerController.FindRightHandBone(transform);
                GameObject mpObj = new GameObject("MuzzleFirePoint");
                mpObj.transform.SetParent(rightHand != null ? rightHand : transform, false);
                mpObj.transform.localPosition = rightHand != null 
                    ? new Vector3(0.05f, 0.05f, 0.2f) 
                    : new Vector3(0.20f, 0.95f, 0.90f);
                muzzleFirePoint = mpObj.transform;
            }

            if (bulletPrefab == null)
            {
                bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet_9mm") ?? Resources.Load<GameObject>("Prefabs/bullet");
#if UNITY_EDITOR
                if (bulletPrefab == null)
                {
                    bulletPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet_9mm.prefab");
                }
#endif
            }

            if (animator != null)
            {
                animator.SetBool("isMoving", true);
            }

            PlayerMobVisualController visualCtrl = GetComponentInChildren<PlayerMobVisualController>();
            if (visualCtrl != null && leader != null)
            {
                visualCtrl.Initialize(leader, equippedWeaponInstance != null ? equippedWeaponInstance.transform : null, muzzleFirePoint);
            }

            isInitialized = true;
            StartCoroutine(FuturisticGroundEmergenceRoutine());
        }

        private float spawnYOffset = 0f;

        private System.Collections.IEnumerator FuturisticGroundEmergenceRoutine()
        {
            spawnYOffset = -1.65f;

            // 1. Futuristic Cyber Teleport Pad on the Road Surface
            GameObject padObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            padObj.name = "CyberTeleportPad";
            padObj.transform.position = transform.position + new Vector3(0f, 0.04f, 0f);
            padObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            padObj.transform.localScale = Vector3.one * 1.6f;
            Collider c = padObj.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Shader addShader = Shader.Find("ZombieSquad/VFX_FireAdditive") 
                            ?? Shader.Find("Sprites/Default") 
                            ?? Shader.Find("Mobile/Particles/Additive");
            Material padMat = new Material(addShader);
            padMat.color = new Color(0f, 1f, 0.92f, 0.90f);
            Texture2D padTex = Resources.Load<Texture2D>("Textures/VFX/tex_cyber_teleport_pad");
#if UNITY_EDITOR && !UNITY_ANDROID
            if (padTex == null)
            {
                string p = System.IO.Path.Combine(Application.dataPath, "Resources/Textures/VFX/tex_cyber_teleport_pad.png");
                if (System.IO.File.Exists(p))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(p);
                    padTex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                    padTex.LoadImage(bytes);
                }
            }
#endif
            if (padTex != null) padMat.mainTexture = padTex;
            padObj.GetComponent<Renderer>().sharedMaterial = padMat;

            // 2. Upward Data Particle Strands / Cyber Spores
            GameObject sparks = new GameObject("TeleportCyberSparks");
            sparks.transform.position = padObj.transform.position;
            sparks.transform.SetParent(padObj.transform, true);
            ParticleSystem ps = sparks.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.42f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, 4.5f);
            main.startSize = 0.12f;
            main.startColor = new Color(0f, 1f, 0.95f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 35f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.55f;
            var rend = sparks.GetComponent<ParticleSystemRenderer>();
            if (addShader != null) rend.sharedMaterial = new Material(addShader);
            ps.Play();

            float elapsed = 0f;
            float duration = 0.45f;

            // 3. Smooth Ground Rise (from -1.65m up to 0m)
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth cubic ease-out curve
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                spawnYOffset = Mathf.Lerp(-1.65f, 0f, ease);

                // Rotate cyber teleport pad rings
                if (padObj != null)
                {
                    padObj.transform.Rotate(0f, 0f, 160f * Time.deltaTime);
                    // Follow underneath the rising ally as leader runs
                    padObj.transform.position = new Vector3(transform.position.x, 0.04f, transform.position.z);
                }

                yield return null;
            }

            spawnYOffset = 0f;

            // 4. Firm Landing Pulse on Road
            if (Camera.main != null)
            {
                Camera.main.GetComponent<MobControl.Core.SmoothCameraFollow>()?.TriggerShake(0.08f);
            }

            // Dissolve the cyber pad smoothly
            if (padObj != null)
            {
                float dissolveTime = 0.22f;
                float dElapsed = 0f;
                while (dElapsed < dissolveTime)
                {
                    dElapsed += Time.deltaTime;
                    float dt = Mathf.Clamp01(dElapsed / dissolveTime);
                    if (padObj != null)
                    {
                        padObj.transform.localScale = Vector3.one * Mathf.Lerp(1.6f, 0f, dt);
                    }
                    yield return null;
                }
                if (padObj != null) Destroy(padObj);
            }
        }

        public void SetFireRate(float rate)
        {
            fireRate = Mathf.Max(0.04f, rate);
        }

        private bool isFallingInPit = false;
        private float verticalVelocity = 0f;
        private float targetPitX = 0f;

        public bool IsFallingInPit => isFallingInPit;

        public void TriggerFallIntoPit(Vector3 pitCenter)
        {
            if (isFallingInPit) return;
            isFallingInPit = true;
            targetPitX = pitCenter.x;
            verticalVelocity = -2.5f;

            if (leader != null)
            {
                leader.RemoveSquadAlly(this);
            }
        }

        private void Update()
        {
            if (isFallingInPit)
            {
                verticalVelocity -= 22f * Time.deltaTime;
                Vector3 fallPos = transform.position;
                fallPos.y += verticalVelocity * Time.deltaTime;
                fallPos.x = Mathf.MoveTowards(fallPos.x, targetPitX, 2.0f * Time.deltaTime);
                transform.position = fallPos;

                transform.Rotate(Vector3.forward, 240f * Time.deltaTime);
                transform.Rotate(Vector3.right, 180f * Time.deltaTime);

                if (fallPos.y <= -0.95f)
                {
                    if (leader != null)
                    {
                        leader.SpawnMemberLostEffect(fallPos);
                    }
                    Destroy(gameObject);
                }
                return;
            }

            if (!isInitialized || leader == null || leader.IsDead)
            {
                if (animator != null) animator.SetBool("isMoving", false);
                return;
            }

            // 1. Maintain formation position relative to player runner
            Vector3 targetPos = leader.transform.position + formationOffset;
            targetPos.x = Mathf.Clamp(targetPos.x, -2.4f, 2.4f);
            
            // Ultra-responsive, smooth frame-rate independent tracking
            Vector3 curPos = transform.position;
            curPos.z = targetPos.z;
            curPos.y = targetPos.y + spawnYOffset;
            curPos.x = Mathf.Lerp(curPos.x, targetPos.x, 1f - Mathf.Exp(-24.0f * Time.deltaTime));
            transform.position = curPos;

            // 2. Synchronize running / idle animation with leader
            bool shouldMove = !leader.IsBlocked;
            if (animator != null)
            {
                animator.SetBool("isMoving", shouldMove);
            }
        }

        public Vector3 GetMuzzlePosition()
        {
            if (muzzleFirePoint != null)
            {
                return muzzleFirePoint.position;
            }
            if (equippedWeaponInstance != null)
            {
                return equippedWeaponInstance.transform.position + equippedWeaponInstance.transform.forward * 0.52f + equippedWeaponInstance.transform.up * 0.08f;
            }
            return new Vector3(transform.position.x + 0.20f, transform.position.y + 0.95f, transform.position.z + 0.90f);
        }

        public void ShootBullet()
        {
            if (bulletPrefab == null)
            {
                bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet_9mm") ?? Resources.Load<GameObject>("Prefabs/bullet");
#if UNITY_EDITOR
                if (bulletPrefab == null)
                {
                    bulletPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet_9mm.prefab");
                }
#endif
            }

            Vector3 spawnPos = GetMuzzlePosition();
            Quaternion spawnRot = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            GameObject bullet = null;

            if (ObjectPooler.Instance != null)
            {
                bullet = ObjectPooler.Instance.SpawnFromPool("Bullet", spawnPos, spawnRot);
            }

            if (bullet == null && bulletPrefab != null)
            {
                bullet = Instantiate(bulletPrefab, spawnPos, spawnRot);
            }

            if (equippedWeaponInstance != null)
            {
                StartCoroutine(WeaponRecoilPunch(equippedWeaponInstance.transform));
            }
        }

        private System.Collections.IEnumerator WeaponRecoilPunch(Transform gunTr)
        {
            if (gunTr == null) yield break;
            Vector3 originalPos = gunTr.localPosition;
            gunTr.localPosition = originalPos - new Vector3(0f, 0f, 0.025f);
            yield return new WaitForSeconds(0.04f);
            if (gunTr != null)
            {
                gunTr.localPosition = originalPos;
            }
        }

        private void OnDestroy()
        {
            if (leader != null)
            {
                leader.RemoveSquadAlly(this);
            }
        }
    }
}
