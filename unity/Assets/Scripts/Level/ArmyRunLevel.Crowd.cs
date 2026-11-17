using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MobControl.Combat;
using MobControl.UI;
using MobControl.Core;

namespace MobControl.Level
{
    /// <summary>
    /// CROWD CONTROLLER + CROWD FORMATION: the player army — sunflower formation around the commander,
    /// growing / shrinking (ChangeSoldiers is the only place the count changes), pooled soldier instances.
    /// </summary>
    public partial class ArmyRunLevel
    {
        // ═══════════════════════════════════════════ ARMY ══

        private Vector3 SlotOffset(int slot)
        {
            float r = UNIT_SPACING * Mathf.Sqrt(slot);
            float a = slot * 2.39996323f;           // golden angle → even sunflower packing
            return new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r * 0.85f);
        }

        private void UpdateFormation(float dt)
        {
            Vector3 lp = LeaderPos;
            float k = 1f - Mathf.Exp(-11f * dt);
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (u.tr == null) continue;
                Vector3 off = SlotOffset(i + 1);
                Vector3 target = lp + off;
                if (clashTimer > 0f && off.z > -0.3f)       // front half of the army charges the enemy
                    target = Vector3.Lerp(target, new Vector3(clashPos.x, 0f, clashPos.z), 0.35f);
                target.x = Mathf.Clamp(target.x, -ZONE_HALF_WIDTH, ZONE_HALF_WIDTH);
                target.y = 0f;
                u.tr.position = Vector3.Lerp(u.tr.position, target, k);
                if (u.pop < 1f)
                {
                    u.pop = Mathf.Min(1f, u.pop + dt * 4f);
                    float s = Mathf.Sin(u.pop * Mathf.PI * 0.5f) * (1f + 0.25f * Mathf.Sin(u.pop * Mathf.PI));
                    u.tr.localScale = Vector3.one * (UNIT_SCALE * s);
                }
            }
        }

        /// <summary>Drawn soldiers = min(soldierCount − 1, 55). Losses remove the soldiers nearest hitPos.</summary>
        private void SyncVisibleUnits(Vector3 hitPos, bool instant = false)
        {
            int desired = Mathf.Clamp(soldierCount - 1, 0, MAX_VISIBLE_UNITS);
            while (units.Count > desired)
            {
                int best = units.Count - 1;
                float bestD = float.PositiveInfinity;
                for (int i = 0; i < units.Count; i++)
                {
                    if (units[i].tr == null) { best = i; break; }
                    float d = (units[i].tr.position - hitPos).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = i; }
                }
                var u = units[best];
                units.RemoveAt(best);
                if (u.tr != null)
                {
                    if (hasArmyAnim && u.anim != null && fallen.Count < 24)
                    {
                        u.anim.SetTrigger(P_DIE);            // falls over with the Death clip, then puffs away
                        fallen.Add(new Fallen { tr = u.tr, t = 0f });
                        fx?.BulletHit(u.tr.position + Vector3.up * 0.6f, new Color(0.5f, 0.75f, 1f));
                    }
                    else
                    {
                        fx?.AllyDeath(u.tr.position + Vector3.up * 0.6f);
                        ReleaseUnit(u.tr.gameObject);
                    }
                }
            }
            Vector3 lp = LeaderPos;
            bool any = false;
            while (units.Count < desired)
            {
                GameObject go = AcquireUnit();
                Vector3 p = lp + SlotOffset(units.Count + 1);
                if (!instant) p += new Vector3(0f, 0f, 1.5f);
                go.transform.position = p;
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = instant ? Vector3.one * UNIT_SCALE : Vector3.one * 0.01f;
                units.Add(new Unit { tr = go.transform, anim = go.GetComponentInChildren<Animator>(), pop = instant ? 1f : 0f });
                any = true;
            }
            if (any && !instant) fx?.Sparkle(lp + Vector3.up, new Color(0.5f, 0.85f, 1f), 12, CrowdRadius);
            if (countLabel != null) countLabel.text = soldierCount.ToString();
        }

        /// <summary>The ONLY place the army size changes: updates drawn soldiers, number, feedback, defeat.</summary>
        private void ChangeSoldiers(int newCount, Vector3 hitPos)
        {
            if (finished) return;
            int old = soldierCount;
            soldierCount = Mathf.Clamp(newCount, 0, 9999);
            if (soldierCount != old) countLabelPunch = 1f;
            if (soldierCount < old) { Snd?.PlayPlayerHurt(); Shake(0.05f); }
            SyncVisibleUnits(hitPos);
            if (soldierCount <= 0) Lose();
        }

        // ═══════════════════════════════════════════ Soldier instances ══

        private GameObject AcquireUnit()
        {
            GameObject go = unitPool.Count > 0 ? unitPool.Pop() : Instantiate(unitTemplate);
            go.name = "Army_Soldier";
            go.transform.SetParent(root, false);
            go.transform.localRotation = Quaternion.identity;
            go.SetActive(true);
            var a = go.GetComponentInChildren<Animator>();
            if (hasArmyAnim && a != null)
            {
                a.Rebind();                                   // pooled soldier may have died before
                a.SetBool(P_MOVING, running);
            }
            RandomizeAnimator(go);
            return go;
        }

        private void ReleaseUnit(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            go.transform.SetParent(poolRoot, false);
            unitPool.Push(go);
        }

        private static void RandomizeAnimator(GameObject go)
        {
            foreach (var a in go.GetComponentsInChildren<Animator>(true))
            {
                if (a.GetComponentInParent<ArmyClipLooper>() != null) continue;
                a.speed = Random.Range(0.9f, 1.12f);
                a.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                if (a.runtimeAnimatorController == null || !a.isActiveAndEnabled) continue;
                int state = a.GetCurrentAnimatorStateInfo(0).fullPathHash;
                if (state != 0) a.Play(state, 0, Random.value);
            }
        }

        /// <summary>Copy of the player's own soldier (model + rifle + animation). red = Dark Legion recolour.</summary>
        private GameObject BuildPlayerCloneTemplate(bool red)
        {
            var tmpl = new GameObject(red ? "DarkLegion_Template" : "ArmyUnit_Template");
            tmpl.SetActive(false);
            tmpl.transform.SetParent(transform, false);

            bool built = false;
            var r = runner != null ? runner : PlayerRunnerController.Instance;
            if (r != null)
            {
                Animator anim = r.GetComponentInChildren<Animator>(true);
                if (anim != null)
                {
                    CloneRelative(anim.transform, r.transform, tmpl.transform);
                    built = true;
                    foreach (Transform child in r.transform)
                    {
                        if (child == anim.transform) continue;
                        string n = child.name;
                        if ((n.Contains("Rifle") || n.Contains("Gun") || n.Contains("Weapon")) && child.GetComponentInChildren<MeshRenderer>(true) != null)
                            CloneRelative(child, r.transform, tmpl.transform);
                    }
                }
            }
            if (!built) BuildTemplateFromResources(tmpl.transform);
            StripTemplate(tmpl);

            if (red)
            {
                foreach (var smr in tmpl.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.sharedMaterial == null) continue;
                    if (s_enemyBodyMat == null) s_enemyBodyMat = BuildRedMaterial(smr.sharedMaterial);
                    var mats = smr.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = s_enemyBodyMat;
                    smr.sharedMaterials = mats;
                }
            }
            return tmpl;
        }

        private static void CloneRelative(Transform src, Transform srcRoot, Transform dstParent)
        {
            GameObject copy = Instantiate(src.gameObject, dstParent);
            copy.name = src.name;
            copy.transform.localPosition = srcRoot.InverseTransformPoint(src.position);
            copy.transform.localRotation = Quaternion.Inverse(srcRoot.rotation) * src.rotation;
            Vector3 rs = srcRoot.lossyScale, ss = src.lossyScale;
            copy.transform.localScale = new Vector3(ss.x / Mathf.Max(1e-5f, rs.x), ss.y / Mathf.Max(1e-5f, rs.y), ss.z / Mathf.Max(1e-5f, rs.z));
        }

        private static void BuildTemplateFromResources(Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>("Characters/Player_FiringRifle") ?? Resources.Load<GameObject>("Characters/PlayerMob");
            if (prefab == null)
            {
                var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                cap.transform.SetParent(parent, false);
                cap.transform.localPosition = Vector3.up * 0.8f;
                cap.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
                return;
            }
            GameObject vis = Instantiate(prefab, parent);
            vis.name = "Visual_Mob";
            vis.transform.localScale = Vector3.one * 68f;
            Material mat = Resources.Load<Material>("Materials/PlayerMaterial");
            var smr = vis.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && mat != null) smr.sharedMaterial = mat;
            Animator anim = vis.GetComponent<Animator>() ?? vis.AddComponent<Animator>();
            anim.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animations/MobAnimator");
            anim.applyRootMotion = false;
        }

        /// <summary>Visuals only: removes scripts, lights, particles, physics and badges from a cloned soldier.</summary>
        private static void StripTemplate(GameObject tmpl)
        {
            var kill = new List<GameObject>();
            foreach (var l in tmpl.GetComponentsInChildren<Light>(true)) kill.Add(l.gameObject);
            foreach (var p in tmpl.GetComponentsInChildren<ParticleSystem>(true)) kill.Add(p.gameObject);
            foreach (var t in tmpl.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n.Contains("FlashBurst") || n.Contains("SquadCountBadge") || n.Contains("HealthBar") || n.StartsWith("BadgePop_") || n == "BlobShadow")
                    kill.Add(t.gameObject);
            }
            foreach (var go in kill) if (go != null && go != tmpl) DestroyImmediate(go);
            foreach (var mb in tmpl.GetComponentsInChildren<MonoBehaviour>(true)) if (mb != null) DestroyImmediate(mb);
            foreach (var c in tmpl.GetComponentsInChildren<Collider>(true)) if (c != null) DestroyImmediate(c);
            foreach (var rb in tmpl.GetComponentsInChildren<Rigidbody>(true)) if (rb != null) DestroyImmediate(rb);
            foreach (var lr in tmpl.GetComponentsInChildren<LineRenderer>(true)) if (lr != null) DestroyImmediate(lr);
            foreach (var tr in tmpl.GetComponentsInChildren<TrailRenderer>(true)) if (tr != null) DestroyImmediate(tr);
            foreach (var a in tmpl.GetComponentsInChildren<Animator>(true)) { a.applyRootMotion = false; a.cullingMode = AnimatorCullingMode.CullUpdateTransforms; }
            foreach (var rend in tmpl.GetComponentsInChildren<Renderer>(true))
            {
                rend.enabled = true;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }
            foreach (var smr in tmpl.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.quality = SkinQuality.Bone2;             // 2 bones per vertex: much cheaper skinning for a crowd
                smr.updateWhenOffscreen = false;
            }
        }

        /// <summary>GPU-copies the soldier texture and turns the blue uniform red.</summary>
        private static Material BuildRedMaterial(Material src)
        {
            var m = new Material(src) { name = "M_DarkLegion_Red" };
            Texture main = src.mainTexture;
            bool ok = false;
            if (main != null)
            {
                RenderTexture rt = null;
                RenderTexture prev = RenderTexture.active;
                try
                {
                    int w = Mathf.Min(1024, main.width), h = Mathf.Min(1024, main.height);
                    rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                    Graphics.Blit(main, rt);
                    RenderTexture.active = rt;
                    var tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    Color32[] px = tex.GetPixels32();
                    for (int i = 0; i < px.Length; i++)
                    {
                        Color32 c = px[i];
                        if (c.b > c.r + 18 && c.b > c.g + 6)
                            px[i] = new Color32((byte)Mathf.Min(255, c.b + 25), (byte)(c.g * 0.35f), (byte)(c.r * 0.4f), c.a);
                    }
                    tex.SetPixels32(px);
                    tex.Apply(true);
                    m.mainTexture = tex;
                    ok = true;
                }
                catch (System.Exception e) { Debug.LogWarning("[ArmyRunLevel] Red recolour failed, using tint: " + e.Message); }
                finally
                {
                    RenderTexture.active = prev;
                    if (rt != null) RenderTexture.ReleaseTemporary(rt);
                }
            }
            if (!ok) m.color = new Color(1f, 0.32f, 0.3f, 1f);
            return m;
        }

        // ═══════════════════════════════════════════ ANIMATION (Idle / Run / Attack / Die) ══
        //  Uses Resources/ArmyRun/ArmyAnimator — built by the editor tool from the player's own clips.
        //  Only the clones use it; the player keeps its original MobAnimator.

        private static RuntimeAnimatorController s_armyAnim;
        private bool hasArmyAnim;
        private static readonly int P_MOVING = Animator.StringToHash("Moving");
        private static readonly int P_ATTACK = Animator.StringToHash("Attack");
        private static readonly int P_DIE = Animator.StringToHash("Die");

        private void ApplyArmyAnimator(GameObject tmpl)
        {
            if (s_armyAnim == null) s_armyAnim = Resources.Load<RuntimeAnimatorController>("ArmyRun/ArmyAnimator");
            if (s_armyAnim == null) return;
            foreach (var a in tmpl.GetComponentsInChildren<Animator>(true)) a.runtimeAnimatorController = s_armyAnim;
            hasArmyAnim = true;
        }

        private void SetArmyMoving(bool moving)
        {
            if (!hasArmyAnim) return;
            foreach (var u in units) if (u.anim != null && u.anim.isActiveAndEnabled) u.anim.SetBool(P_MOVING, moving);
        }

        /// <summary>While fighting hand-to-hand, a few front soldiers throw punches (Fist Fight clip) every 0.3 s.</summary>
        private void UpdateClashAnimations(float dt)
        {
            if (clashTimer > 0f) clashTimer -= dt;
            if (!hasArmyAnim || clashTimer <= 0f || units.Count == 0) return;
            attackAnimTimer -= dt;
            if (attackAnimTimer > 0f) return;
            attackAnimTimer = 0.3f;
            for (int k = 0; k < 3; k++)
            {
                var u = units[Random.Range(0, units.Count)];
                if (u.anim != null && u.anim.isActiveAndEnabled) u.anim.SetTrigger(P_ATTACK);
            }
        }

        /// <summary>Marks a hand-to-hand fight at pos for the next 0.25 s (army leans in and punches).</summary>
        private void Clash(Vector3 pos)
        {
            clashPos = pos;
            clashTimer = 0.25f;
        }

        /// <summary>Fallen soldiers lie on the road (scrolling away with it), sink after 0.9 s and puff out.</summary>
        private void UpdateFallen(float dt)
        {
            float scroll = (running && !finished && !arenaActive) ? scrollSpeedNow * dt : 0f;
            for (int i = fallen.Count - 1; i >= 0; i--)
            {
                var f = fallen[i];
                if (f.tr == null) { fallen.RemoveAt(i); continue; }
                f.t += dt;
                Vector3 p = f.tr.position;
                p.z -= scroll;
                if (f.t > 0.9f) p.y -= dt * 1.5f;
                f.tr.position = p;
                if (f.t > 1.25f)
                {
                    fx?.AllyDeath(p + Vector3.up * 0.4f);
                    ReleaseUnit(f.tr.gameObject);
                    fallen.RemoveAt(i);
                }
            }
        }

        /// <summary>CameraController: the existing SmoothCameraFollow, following the army sideways (60 %).</summary>
        private void UpdateCamera(float dt)
        {
            if (camFollow == null) return;
            camX = Mathf.Lerp(camX, CAM_CENTER_X + LeaderPos.x * CAM_FOLLOW_X, 1f - Mathf.Exp(-3f * dt));
            camFollow.SetCameraParameters(new Vector3(camX, CAM_HEIGHT, LeaderPos.z - CAM_BACK), new Vector3(CAM_PITCH, 0f, 0f));
        }
    }
}
