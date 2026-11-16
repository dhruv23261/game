using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MobControl.Level
{
    /// <summary>
    /// BIG ENEMIES
    ///
    ///  THE BRUTE (stage 4 mini-boss) — 2× soldier size, 900 HP.
    ///    When he is 18 m away the road stops (arena). He walks into the army and every 3.2 s
    ///    slams the ground: a red circle appears under the army, 1 s later everything inside dies
    ///    (up to 22 % of the army). MOVE OUT OF THE CIRCLE. In contact he also stomps (2 / s).
    ///    Kill him → road starts again ("GO!").
    ///
    ///  MASTER TYRANT (stage 6 final boss) — 4× soldier size, 12000 HP.
    ///    Entrance: road stops, he drops from the sky → shockwave, dust, roar, HP bar.
    ///    Phase 1 (100–66 %): stays 8 m away, slam every 3.4 s (up to 30 %).
    ///    At 66 %: summons 14 soldiers.
    ///    At 33 %: ENRAGED — glows red, summons 18, walks into the army, slams every 2 s, bigger circle.
    ///    Death: slow motion, chain of explosions, he falls backwards → VICTORY.
    /// </summary>
    public partial class ArmyRunLevel
    {
        private const float BRUTE_HP = 900f;
        private const float BRUTE_HEIGHT_X = 2.0f;
        private const float MASTER_HP = 12000f;
        private const float MASTER_HEIGHT_X = 4.0f;
        private const float MASTER_TRIGGER_Z = 478f;   // metres run before the Master drops in
        private const float BIG_ARENA_Z = 18f;

        private class Big
        {
            public Target t;
            public bool isMaster, engaged, landed, rage, summoned66, summoned33;
            public float walkSpeed, slamInterval, slamTimer, slamRadius, slamFrac, stompTimer;
            public int stompKills;
            public float dropT;
            public Transform warn, warnFill;
            public Vector3 warnPos;
            public float warnT, warnDur;
            public Renderer[] rends;
            public float flashT;
            public string name;
            public Transform bar, barFill;          // overhead HP bar
            public TextMeshPro barText;
            public float barY;
        }

        private readonly List<Big> bigs = new List<Big>();
        private Big master;
        private bool masterSpawned;
        private Big deathBig;
        private float deathT, deathFxTimer;
        private MaterialPropertyBlock mpb;

        // ═════════════════════════════════════ Spawning ══

        private void AddBrute(float z, float hp)
        {
            var go = NewOnTrack("Boss_Brute", 0f, z);
            var body = Instantiate(enemyTemplate, go.transform);
            body.name = "Visual";
            body.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            body.transform.localScale = Vector3.one * ((enemyIsGenerated ? 1f : UNIT_SCALE) * BRUTE_HEIGHT_X);
            body.SetActive(true);
            foreach (var a in body.GetComponentsInChildren<Animator>(true)) a.speed = 0.8f;
            ArmyRunFX.AddBlobShadow(go.transform, 2.4f);
            var t = new Target { kind = Kind.Brute, tr = go.transform, visual = body.transform, halfWidth = 1.0f, depth = 0.8f, hp = hp, maxHp = hp, baseScale = body.transform.localScale, spawned = true };
            targets.Add(t);
            bigs.Add(new Big
            {
                t = t, name = "THE BRUTE", walkSpeed = 1.6f, slamInterval = 3.2f, slamTimer = 2f, slamRadius = 2.6f, slamFrac = 0.22f,
                stompKills = 2, rends = body.GetComponentsInChildren<Renderer>(true), landed = true
            });
            AttachBigBar(bigs[bigs.Count - 1], soldierHeight * BRUTE_HEIGHT_X + 0.8f, 2.2f);
        }

        /// <summary>Road stops and the Master drops from the sky 22 m in front of the army.</summary>
        private void SpawnMaster()
        {
            masterSpawned = true;
            arenaActive = true;
            if (EndlessRoadManager.Instance != null) EndlessRoadManager.Instance.SetRunning(false);

            var go = new GameObject("Boss_MasterTyrant");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(0f, 26f, LeaderPos.z + 22f);
            var body = Instantiate(bossTemplate, go.transform);
            body.name = "Visual";
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            body.SetActive(true);
            foreach (var a in body.GetComponentsInChildren<Animator>(true)) a.speed = 0.7f;
            ArmyRunFX.AddBlobShadow(go.transform, 4.2f);

            float bHp = currentLevel == 2 ? 14000f : MASTER_HP;
            string bName = currentLevel == 2 ? "SKELETON OVERLORD" : "MASTER TYRANT";

            var t = new Target
            {
                kind = Kind.Boss, tr = go.transform, visual = body.transform, halfWidth = 1.7f, depth = 1.1f,
                hp = bHp, maxHp = bHp, baseScale = body.transform.localScale, spawned = true
            };
            targets.Add(t);
            master = new Big
            {
                t = t, isMaster = true, name = bName, walkSpeed = 1.2f, slamInterval = 3.4f, slamTimer = 2.5f,
                slamRadius = 3.3f, slamFrac = 0.30f, stompKills = 4, rends = body.GetComponentsInChildren<Renderer>(true), engaged = true
            };
            bigs.Add(master);
            AttachBigBar(master, soldierHeight * MASTER_HEIGHT_X + 0.9f, 3.4f);

            ZombieRunnerUIManagerSafe()?.TriggerBossAlert();
            ShowBanner("FINAL STAGE", bName, "Dodge the red circles!");
        }

        private static MobControl.UI.ZombieRunnerUIManager ZombieRunnerUIManagerSafe() => MobControl.UI.ZombieRunnerUIManager.Instance;

        // ═════════════════════════════════════ Per frame ══

        private void UpdateBigEnemies(float dt)
        {
            if (!masterSpawned && travelled >= MASTER_TRIGGER_Z) SpawnMaster();

            Vector3 lp = LeaderPos;
            float R = CrowdRadius;
            for (int i = 0; i < bigs.Count; i++)
            {
                var b = bigs[i];
                if (b.t == null || !b.t.alive || b.t.tr == null) continue;
                Transform tr = b.t.tr;

                // ── Master entrance: fall from the sky ──
                if (!b.landed)
                {
                    b.dropT += dt;
                    float k = Mathf.Clamp01(b.dropT / 0.6f);
                    Vector3 p = tr.position;
                    p.y = Mathf.Lerp(26f, 0f, k * k);
                    tr.position = p;
                    if (k >= 1f) Land(b);
                    continue;
                }

                // ── Brute: arena starts when he gets close ──
                if (!b.engaged)
                {
                    if (tr.position.z - lp.z > BIG_ARENA_Z) continue;
                    b.engaged = true;
                    arenaActive = true;
                    if (EndlessRoadManager.Instance != null) EndlessRoadManager.Instance.SetRunning(false);
                    Snd?.PlayBossSpawn();
                    Shake(0.2f);
                    ShowBossBar(b.name, b.t.hp, b.t.maxHp);
                    SpawnFloatText(tr.position + Vector3.up * (soldierHeight * BRUTE_HEIGHT_X + 2f), "BRUTE!", new Color(1f, 0.3f, 0.2f));
                }

                // ── Walk ──
                float keep = (b.isMaster && !b.rage) ? 8f : R + 1.3f;
                Vector3 pos = tr.position;
                if (pos.z - lp.z > keep) pos.z -= b.walkSpeed * dt;
                pos.x = Mathf.MoveTowards(pos.x, Mathf.Clamp(lp.x, -2.5f, 2.5f), 0.9f * dt);
                tr.position = pos;

                // ── Contact stomp ──
                if (pos.z - lp.z <= R + 1.5f)
                {
                    b.stompTimer -= dt;
                    if (b.stompTimer <= 0f)
                    {
                        b.stompTimer = 1f;
                        b.t.punch = 1f;
                        fx?.Dust(new Vector3(pos.x, 0f, pos.z - 1f), 0.8f);
                        Snd?.PlayZombieAttack(pos);
                        Shake(0.12f);
                        ChangeSoldiers(soldierCount - b.stompKills, pos + Vector3.back * 1.5f);
                    }
                }

                // ── Ground slam (telegraphed) ──
                UpdateSlam(b, dt, lp, R);

                // ── Hit flash / rage glow ──
                UpdateTint(b, dt);
            }
        }

        private void Land(Big b)
        {
            b.landed = true;
            Vector3 p = b.t.tr.position;
            fx?.Shockwave(p, 16f, new Color(1f, 0.8f, 0.5f, 1f));
            fx?.Dust(p, 1.8f);
            fx?.Explosion(p, 0.8f);
            Snd?.PlayExplosion(p);
            Snd?.PlayBossSpawn();
            Shake(0.5f);
            ShowBossBar(b.name, b.t.hp, b.t.maxHp);
            // the impact knocks back the front of the army
            ChangeSoldiers(soldierCount - Mathf.Min(3, soldierCount - 1), LeaderPos + Vector3.forward * 2f);
        }

        private void UpdateSlam(Big b, float dt, Vector3 lp, float R)
        {
            if (b.warn == null)
            {
                b.slamTimer -= dt;
                if (b.slamTimer > 0f) return;
                // telegraph: red circle under the army
                b.warnPos = new Vector3(lp.x, 0.06f, lp.z + 0.3f);
                b.warnT = 0f;
                b.warnDur = b.rage ? 0.8f : 1.0f;
                var w = new GameObject("SlamWarning").transform;
                w.SetParent(root, false);
                w.position = b.warnPos;
                CreatePrimitive(PrimitiveType.Cylinder, w, "Ring", Vector3.zero, new Vector3(b.slamRadius * 2f, 0.01f, b.slamRadius * 2f), s_warn);
                var fill = CreatePrimitive(PrimitiveType.Cylinder, w, "Fill", Vector3.up * 0.01f, new Vector3(0.1f, 0.01f, 0.1f), FlatMat(new Color(1f, 0.2f, 0.1f, 0.55f)));
                b.warn = w;
                b.warnFill = fill.transform;
                b.t.punch = 1f;
                Snd?.PlayZombieAttack(b.t.tr.position);
                return;
            }

            b.warnT += dt;
            float k = Mathf.Clamp01(b.warnT / b.warnDur);
            if (b.warnFill != null) b.warnFill.localScale = new Vector3(b.slamRadius * 2f * k, 0.01f, b.slamRadius * 2f * k);
            if (k < 1f) return;

            // SLAM
            Vector3 c = b.warnPos;
            fx?.Shockwave(c, b.slamRadius * 2.6f, new Color(1f, 0.45f, 0.25f, 1f));
            fx?.Dust(c, 1.2f);
            fx?.Explosion(c, 0.6f);
            Snd?.PlayExplosion(c);
            Shake(0.3f);
            float dist = Mathf.Abs(lp.x - c.x);
            float reach = b.slamRadius + R * 0.6f;
            if (dist < reach)
            {
                float frac = Mathf.Clamp01(1f - dist / reach);
                int kills = Mathf.Max(1, Mathf.CeilToInt(soldierCount * b.slamFrac * frac));
                SpawnFloatText(c + Vector3.up * 2.5f, $"-{Mathf.Min(kills, soldierCount)}", new Color(1f, 0.3f, 0.3f));
                ChangeSoldiers(soldierCount - kills, c);
            }
            else SpawnFloatText(new Vector3(lp.x, 2.6f, lp.z + 1f), "DODGED!", new Color(1f, 0.95f, 0.4f));

            Destroy(b.warn.gameObject);
            b.warn = null;
            b.slamTimer = b.slamInterval;
        }

        private void UpdateTint(Big b, float dt)
        {
            if (b.rends == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            Color? tint = null;
            if (b.flashT > 0f) { b.flashT -= dt; tint = new Color(2.2f, 2.2f, 2.2f, 1f); }
            else if (b.rage) { float p = 0.5f + 0.5f * Mathf.Sin(Time.time * 8f); tint = new Color(1.4f + p * 0.6f, 0.45f, 0.4f, 1f); }

            foreach (var r in b.rends)
            {
                if (r == null || r.name == "BlobShadow") continue;
                if (tint.HasValue)
                {
                    r.GetPropertyBlock(mpb);
                    mpb.SetColor("_Color", tint.Value);
                    r.SetPropertyBlock(mpb);
                }
                else r.SetPropertyBlock(null);
            }
        }

        // ═════════════════════════════════════ Damage / phases ══

        private Big FindBig(Target t)
        {
            for (int i = 0; i < bigs.Count; i++) if (bigs[i].t == t) return bigs[i];
            return null;
        }

        private void OnBigDamaged(Target t)
        {
            var b = FindBig(t);
            if (b == null) return;
            b.flashT = 0.06f;
            UpdateBossBar(Mathf.Max(0f, t.hp), t.maxHp);
            if (!b.isMaster || t.hp <= 0f) return;

            float ratio = t.hp / t.maxHp;
            if (ratio <= 0.66f && !b.summoned66)
            {
                b.summoned66 = true;
                Summon(t.tr.position, 14, 4f);
                ShowBanner("", "REINFORCEMENTS!", "");
                Snd?.PlayBossSpawn();
            }
            if (ratio <= 0.33f && !b.summoned33)
            {
                b.summoned33 = true;
                b.rage = true;
                b.walkSpeed *= 1.7f;
                b.slamInterval = 2.0f;
                b.slamRadius = 4.0f;
                b.slamFrac = 0.35f;
                Summon(t.tr.position, 18, 4f);
                ShowBanner("", "MASTER ENRAGED!", "");
                fx?.Explosion(t.tr.position + Vector3.up * 2f, 0.7f);
                Shake(0.35f);
                Snd?.PlayBossSpawn();
            }
        }

        /// <summary>Red soldiers pour out from behind the boss and charge.</summary>
        private void Summon(Vector3 bossPos, int count, float hp)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 world = bossPos + new Vector3(Random.Range(-3.8f, 3.8f), 0f, Random.Range(1.5f, 5f));
                world.x = Mathf.Clamp(world.x, -ZONE_HALF_WIDTH, ZONE_HALF_WIDTH);
                targets.Add(new Target
                {
                    kind = Kind.Enemy, halfWidth = 0.32f, hp = hp, maxHp = hp,
                    spawnLocal = trackRoot.InverseTransformPoint(new Vector3(world.x, 0f, world.z))
                });
                if (i % 3 == 0) fx?.Dust(world, 0.5f);
            }
        }

        private void OnBigKilled(Target t)
        {
            var b = FindBig(t);
            if (b != null && b.warn != null) { Destroy(b.warn.gameObject); b.warn = null; }
            Vector3 pos = t.tr.position;

            if (b == null || !b.isMaster)
            {
                // Brute: big explosion, road resumes
                fx?.Explosion(pos + Vector3.up, 1.6f);
                fx?.Chunks(pos + Vector3.up * 1.5f, new Color(0.85f, 0.15f, 0.1f), 24, 8f);
                Snd?.PlayZombieDie(true, pos);
                Shake(0.35f);
                Destroy(t.tr.gameObject);
                t.tr = null;
                HideBossBar();
                AddCoins(50, pos + Vector3.up * 2f);
                arenaActive = false;
                if (EndlessRoadManager.Instance != null) EndlessRoadManager.Instance.SetRunning(true);
                ShowBanner("", "GO!", "");
                return;
            }

            // Master: slow-motion finale
            finished = true;
            ClearBullets();
            for (int i = 0; i < targets.Count; i++)
                if (targets[i].alive && targets[i].kind == Kind.Enemy && targets[i].spawned) DamageTarget(targets[i], 9999f);
            Snd?.PlayZombieDie(true, pos);
            AddCoins(200, pos + Vector3.up * 3f);
            Time.timeScale = 0.3f;
            deathBig = b;
            deathT = 0f;
            deathFxTimer = 0f;
            UpdateBossBar(0f, t.maxHp);
        }

        /// <summary>Master death: 1.3 s (real time) of slow motion, explosions, falling back; then Victory.</summary>
        private void UpdateBossDeath()
        {
            if (deathBig == null) return;
            float udt = Time.unscaledDeltaTime;
            deathT += udt;
            deathFxTimer -= udt;
            Transform tr = deathBig.t.tr;
            if (tr != null)
            {
                Vector3 c = tr.position + Vector3.up * soldierHeight * MASTER_HEIGHT_X * 0.5f;
                if (deathFxTimer <= 0f)
                {
                    deathFxTimer = 0.18f;
                    fx?.Explosion(c + Random.insideUnitSphere * 2f, 0.7f);
                    Snd?.PlayExplosion(c);
                    Shake(0.25f);
                }
                float k = Mathf.Clamp01(deathT / 1.2f);
                if (deathBig.t.visual != null)
                    deathBig.t.visual.localRotation = Quaternion.Euler(-80f * k * k, 180f, 0f);
            }
            if (deathT < 1.3f) return;

            Time.timeScale = 1f;
            if (tr != null)
            {
                Vector3 p = tr.position;
                fx?.Explosion(p + Vector3.up * 1.5f, 2.6f);
                fx?.Sparkle(p + Vector3.up * 2f, new Color(1f, 0.85f, 0.25f), 70, 3f);
                fx?.Chunks(p + Vector3.up * 2f, new Color(0.85f, 0.15f, 0.1f), 30, 10f);
                Snd?.PlayExplosion(p);
                Shake(0.6f);
                Destroy(tr.gameObject);
                deathBig.t.tr = null;
            }
            HideBossBar();
            ShowBanner("", "VICTORY!", "");
            deathBig = null;
            finishTimer = 1.6f;
        }

        // ═════════════════════════════════════ Overhead HP bar ══

        /// <summary>Red bar + big HP number floating over the boss's head (always faces the camera).</summary>
        private void AttachBigBar(Big b, float y, float width)
        {
            var bar = new GameObject("BossHpBar").transform;
            bar.SetParent(root, false);
            CreateBox(bar, "Back", Vector3.zero, new Vector3(width + 0.12f, 0.36f, 0.02f), FlatMat(new Color(0.08f, 0.02f, 0.02f, 0.95f)));
            var fillPivot = NewChild(bar, "FillPivot");
            fillPivot.localPosition = new Vector3(-width * 0.5f, 0f, -0.012f);
            CreateBox(fillPivot, "Fill", new Vector3(width * 0.5f, 0f, 0f), new Vector3(width, 0.26f, 0.02f), FlatMat(new Color(0.95f, 0.15f, 0.12f)));
            b.barText = CreateLabel(bar, Mathf.CeilToInt(b.t.hp).ToString(), new Vector3(0f, 0.62f, -0.02f), 13f, Color.white, new Color(0.3f, 0f, 0f));
            b.bar = bar;
            b.barFill = fillPivot;
            b.barY = y;
        }

        private void UpdateBigBars(Quaternion face)
        {
            for (int i = 0; i < bigs.Count; i++)
            {
                var b = bigs[i];
                if (b.bar == null) continue;
                bool show = b.t != null && b.t.alive && b.t.tr != null;
                if (b.bar.gameObject.activeSelf != show) b.bar.gameObject.SetActive(show);
                if (!show) continue;
                b.bar.position = b.t.tr.position + Vector3.up * b.barY;
                b.bar.rotation = face;
                b.barFill.localScale = new Vector3(Mathf.Clamp01(b.t.hp / b.t.maxHp), 1f, 1f);
                string txt = Mathf.Max(0, Mathf.CeilToInt(b.t.hp)).ToString();
                if (b.barText.text != txt) b.barText.text = txt;
            }
        }
    }
}
