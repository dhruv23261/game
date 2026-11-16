using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MobControl.Combat;
using MobControl.UI;
using MobControl.Core;

namespace MobControl.Level
{
    /// <summary>
    /// COMBAT SYSTEM: army fire (tracers), bullet hits, damage, deaths, rewards, explosions.
    /// </summary>
    public partial class ArmyRunLevel
    {
        // ═══════════════════════════════════════════ SHOOTING ══
        //  shotsPerSec = soldiers × 1.8 × fireMultiplier. ≤34 tracers/s drawn; each carries its share of damage.

        private void UpdateShooting(float dt)
        {
            float shotsPerSec = soldierCount * UNIT_FIRE_RATE * fireMultiplier;
            float visualRate = Mathf.Min(shotsPerSec, MAX_VISUAL_BULLETS);
            if (visualRate <= 0.01f) return;
            float damagePerVisual = shotsPerSec / visualRate;
            float interval = 1f / visualRate;

            visualShotTimer += dt;
            int guard = 0;
            while (visualShotTimer >= interval && guard++ < 8)
            {
                visualShotTimer -= interval;
                Vector3 origin;
                int pick = Random.Range(-1, units.Count);
                if (pick < 0 || units[pick].tr == null)
                    origin = runner != null ? runner.GetMuzzlePosition() : LeaderPos + Vector3.up;
                else
                    origin = units[pick].tr.position + new Vector3(0.12f, soldierHeight * 0.55f, 0.45f);
                FireBullet(origin, damagePerVisual * PlayerRunnerController.RollBulletDamage());
                if (guard <= 2) fx?.MuzzleFlash(origin);
            }

            gunSoundTimer -= dt;
            if (gunSoundTimer <= 0f)
            {
                gunSoundTimer = 0.2f;
                Snd?.PlayGunfire(soldierCount > 30);
            }
        }

        private void FireBullet(Vector3 origin, float damage)
        {
            Transform t = bulletPool.Count > 0 ? bulletPool.Pop() : MakeTracer(s_tracer, new Vector3(0.13f, 0.13f, 1.4f));
            t.SetParent(root, true);
            t.gameObject.SetActive(true);
            t.position = origin;
            t.rotation = Quaternion.identity;
            bullets.Add(new Bullet { tr = t, damage = damage, startZ = origin.z });
        }

        private Transform MakeTracer(Material m, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Tracer";
            RemoveCollider(go);
            EnsureWhiteVertexColors(go);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.localScale = scale;
            return go.transform;
        }

        /// <summary>Bullets fly straight +Z and hit the nearest live target whose |x − bulletX| &lt; halfWidth.</summary>
        private void UpdateBullets(float dt)
        {
            float step = BULLET_SPEED * dt;
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                var b = bullets[i];
                if (b.tr == null) { bullets.RemoveAt(i); continue; }
                Vector3 p = b.tr.position;
                float z0 = p.z, z1 = p.z + step;
                Target hit = null;
                float hitZ = float.PositiveInfinity;
                for (int t = 0; t < targets.Count; t++)
                {
                    var tg = targets[t];
                    if (!tg.alive || tg.tr == null || (tg.kind == Kind.Enemy && !tg.spawned)) continue;
                    Vector3 tp = tg.tr.position;
                    if (tp.z < 0.3f) continue;
                    if (tp.z + tg.depth < z0 || tp.z - tg.depth > z1) continue;
                    if (Mathf.Abs(tp.x - p.x) > tg.halfWidth) continue;
                    if (tp.z < hitZ) { hitZ = tp.z; hit = tg; }
                }

                if (hit != null)
                {
                    Vector3 hp = new Vector3(p.x, p.y, hitZ - hit.depth * 0.6f);
                    fx?.BulletHit(hp, HitColor(hit.kind));
                    if (hit.kind == Kind.Enemy || hit.kind == Kind.Gunner || hit.kind == Kind.Brute || hit.kind == Kind.Boss) Snd?.PlayBulletHit(hp);
                    DamageTarget(hit, b.damage);
                    ReleaseBullet(b.tr);
                    bullets.RemoveAt(i);
                    continue;
                }

                p.z = z1;
                b.tr.position = p;
                if (p.z - b.startZ > BULLET_RANGE) { ReleaseBullet(b.tr); bullets.RemoveAt(i); }
            }
        }

        private static Color HitColor(Kind k)
        {
            switch (k)
            {
                case Kind.Ice: return new Color(0.6f, 0.9f, 1f);
                case Kind.Barrel: case Kind.Cage: return new Color(1f, 0.8f, 0.4f);
                default: return new Color(1f, 0.45f, 0.3f);
            }
        }

        private void ClearBullets()
        {
            for (int i = 0; i < bullets.Count; i++) ReleaseBullet(bullets[i].tr);
            bullets.Clear();
            foreach (var s in enemyShots) if (s.tr != null) { s.tr.gameObject.SetActive(false); shotPool.Push(s.tr); }
            enemyShots.Clear();
        }

        private void ReleaseBullet(Transform t)
        {
            if (t == null) return;
            t.gameObject.SetActive(false);
            bulletPool.Push(t);
        }

        // ═══════════════════════════════════════════ DAMAGE & DEATHS ══

        private void DamageTarget(Target t, float dmg)
        {
            if (!t.alive) return;
            t.hp -= dmg;
            t.punch = 1f;
            if (t.label != null) t.label.text = Mathf.Max(0, Mathf.CeilToInt(t.hp)).ToString();
            if (t.kind == Kind.Brute || t.kind == Kind.Boss) OnBigDamaged(t);
            else if (t.kind == Kind.Enemy || t.kind == Kind.Gunner) Flash(t);          // hit reaction: white flash + punch
            else ApplyDamageLook(t);                                                    // props crack as they weaken
            if (t.hp > 0f) return;
            KillTarget(t, false);
        }

        /// <summary>A target is destroyed. crashed = the army ran into it (no reward).</summary>
        private void KillTarget(Target t, bool crashed)
        {
            t.alive = false;
            if (t.tr == null) return;
            Vector3 pos = t.tr.position;
            if (!crashed && t.coins > 0) AddCoins(t.coins, pos + Vector3.up);

            switch (t.kind)
            {
                case Kind.Enemy:
                case Kind.Gunner:
                    Snd?.PlayZombieDie(false, pos);
                    if (t.kind == Kind.Gunner) fx?.Chunks(pos + Vector3.up * 0.5f, new Color(0.78f, 0.68f, 0.46f), 10, 4f);
                    FlingAndRelease(t);
                    break;

                case Kind.Barrel:
                    fx?.Chunks(pos + Vector3.up * 0.8f, new Color(0.6f, 0.38f, 0.18f), 14, 6f);
                    fx?.Explosion(pos, 0.5f);
                    Snd?.PlayExplosion(pos);
                    if (!crashed) GiveReward(t, pos);
                    Destroy(t.tr.gameObject);
                    break;

                case Kind.Cage:
                    if (t.freesGiant) { SpawnAlly(pos); if (t.helpLabel != null) t.helpLabel.gameObject.SetActive(false); }
                    fx?.Chunks(pos + Vector3.up * 1f, new Color(0.55f, 0.58f, 0.62f), 12, 5f);
                    fx?.Sparkle(pos + Vector3.up, new Color(0.5f, 0.85f, 1f), 20, 1f);
                    Snd?.PlayExplosion(pos);
                    if (!crashed) GiveReward(t, pos);
                    Destroy(t.tr.gameObject);
                    break;

                case Kind.Explosive:
                    fx?.Explosion(pos, 1.3f);
                    fx?.Chunks(pos + Vector3.up, new Color(0.85f, 0.15f, 0.1f), 10, 8f);
                    Snd?.PlayExplosion(pos);
                    Shake(0.2f);
                    Destroy(t.tr.gameObject);
                    t.tr = null;
                    ExplosionDamage(pos);        // may chain into other explosive barrels
                    break;

                case Kind.Ice:
                    fx?.Chunks(pos + Vector3.up * 0.8f, new Color(0.6f, 0.9f, 1f), 18, 6f);
                    fx?.Sparkle(pos + Vector3.up, new Color(0.8f, 0.95f, 1f), 16, 1.5f);
                    Snd?.PlayExplosion(pos);
                    if (t.unlocksGate != null && t.unlocksGate.panel != null) t.unlocksGate.panel.gameObject.SetActive(true);
                    Destroy(t.tr.gameObject);
                    break;

                case Kind.Brute:
                case Kind.Boss:
                    OnBigKilled(t);
                    break;
            }
        }

        private void GiveReward(Target t, Vector3 pos)
        {
            if (t.reward == Reward.Soldiers)
            {
                SpawnFloatText(pos + Vector3.up * 2.4f, (t.kind == Kind.Cage ? "FREED +" : "+") + t.rewardValue, new Color(0.45f, 1f, 0.5f));
                Snd?.PlayUpgradeSuccess();
                ChangeSoldiers(soldierCount + t.rewardValue, pos);
            }
            else if (t.reward == Reward.FireRate)
            {
                fireMultiplier *= FIRE_UP_MULT;
                SpawnFloatText(pos + Vector3.up * 2.4f, "FIRE UP!", new Color(1f, 0.72f, 0.2f));
                fx?.Sparkle(pos + Vector3.up, new Color(1f, 0.75f, 0.2f), 20, 1f);
                Snd?.PlayGunUpgrade();
            }
        }

        /// <summary>Explosive barrel: everything within 3.6 m takes 30 damage (chains into other barrels).</summary>
        private void ExplosionDamage(Vector3 pos)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var o = targets[i];
                if (!o.alive || o.tr == null || (o.kind == Kind.Enemy && !o.spawned)) continue;
                if ((o.tr.position - pos).sqrMagnitude <= EXPLOSION_RADIUS * EXPLOSION_RADIUS)
                    DamageTarget(o, EXPLOSION_DAMAGE);
            }
        }

        /// <summary>Dead red soldiers are thrown back with a spin, then vanish in a puff.</summary>
        private void FlingAndRelease(Target t)
        {
            Transform tr = t.tr;
            t.tr = null;
            tr.SetParent(root, true);
            dying.Add(new Dying
            {
                tr = tr,
                vel = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(4.5f, 6.5f), Random.Range(3f, 5.5f)),
                spin = new Vector3(Random.Range(-520f, -300f), Random.Range(-200f, 200f), Random.Range(-200f, 200f)),
                t = 0f
            });
        }

        private void UpdateDying(float dt)
        {
            for (int i = dying.Count - 1; i >= 0; i--)
            {
                var d = dying[i];
                if (d.tr == null) { dying.RemoveAt(i); continue; }
                d.t += dt;
                d.vel.y -= 20f * dt;
                d.tr.position += d.vel * dt;
                d.tr.Rotate(d.spin * dt, Space.World);
                if (d.t > 0.55f || d.tr.position.y < -0.5f)
                {
                    fx?.EnemyDeath(d.tr.position + Vector3.up * 0.4f);
                    d.tr.gameObject.SetActive(false);
                    d.tr.SetParent(poolRoot, false);
                    if (d.tr.name.StartsWith("DarkLegion")) enemyPool.Push(d.tr.gameObject);
                    else Destroy(d.tr.gameObject);   // gunner bunkers etc.
                    dying.RemoveAt(i);
                }
            }
        }

        // ═══════════════════════════════════════════ HIT REACTIONS & DAMAGE LOOK ══

        private MaterialPropertyBlock tintBlock;
        private static readonly int ID_COLOR = Shader.PropertyToID("_Color");

        /// <summary>Enemy hit: 0.07 s white flash (cleared in UpdatePunches).</summary>
        private void Flash(Target t)
        {
            if (t.rends == null) return;
            t.flashT = 0.07f;
            SetTint(t.rends, new Color(2.2f, 2.2f, 2.2f, 1f));
        }

        /// <summary>
        /// Containers / barrels / cages: every 25 % of HP lost they crack one step —
        /// darker colour, a tilt, and chunks breaking off.
        /// </summary>
        private void ApplyDamageLook(Target t)
        {
            if (t.maxHp <= 0f || t.tr == null) return;
            float lost = 1f - Mathf.Clamp01(t.hp / t.maxHp);
            int stage = Mathf.Min(3, Mathf.FloorToInt(lost * 4f));
            if (stage <= t.damageStage) return;
            t.damageStage = stage;
            Color c = Color.Lerp(Color.white, new Color(0.5f, 0.46f, 0.44f, 1f), stage / 3f);
            if (t.rends != null) SetTint(t.rends, c);
            if (t.visual != null)
                t.visual.localRotation = Quaternion.Euler(Random.Range(-3f, 3f) * stage, Random.Range(-6f, 6f) * stage, Random.Range(-3f, 3f) * stage);
            Color chunk = t.kind == Kind.Ice ? new Color(0.6f, 0.85f, 1f) : (t.kind == Kind.Barrel ? new Color(0.6f, 0.38f, 0.18f) : new Color(0.6f, 0.62f, 0.66f));
            fx?.Chunks(t.tr.position + Vector3.up * 1.2f, chunk, 4 + stage * 2, 3.5f);
            t.punch = 1.6f;
        }

        private void SetTint(Renderer[] rends, Color c)
        {
            if (tintBlock == null) tintBlock = new MaterialPropertyBlock();
            foreach (var r in rends)
            {
                if (r == null || r.gameObject.name == "BlobShadow" || r is ParticleSystemRenderer) continue;
                r.GetPropertyBlock(tintBlock);
                tintBlock.SetColor(ID_COLOR, c);
                r.SetPropertyBlock(tintBlock);
            }
        }

        private static void ClearTint(Renderer[] rends)
        {
            if (rends == null) return;
            foreach (var r in rends) if (r != null) r.SetPropertyBlock(null);
        }
    }
}
