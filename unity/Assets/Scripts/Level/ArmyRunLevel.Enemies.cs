using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MobControl.Combat;
using MobControl.UI;
using MobControl.Core;

namespace MobControl.Level
{
    /// <summary>
    /// ENEMY CONTROLLER: charging red soldiers, enemy-group counters, gunners and their shots, the parade.
    /// </summary>
    public partial class ArmyRunLevel
    {
        // ═══════════════════════════════════════════ ENEMIES ══

        /// <summary>Red soldiers: appear 60 m out, charge, drift toward the army; contact trades 1-for-1 (heavies kill 2).</summary>
        private void UpdateEnemies(float dt)
        {
            Vector3 lp = LeaderPos;
            float radius = CrowdRadius;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t.kind != Kind.Enemy || !t.alive) continue;
                if (!t.spawned)
                {
                    if (trackRoot.position.z + t.spawnLocal.z > ENEMY_ACTIVATE_Z) continue;
                    SpawnEnemy(t, trackRoot, t.spawnLocal);
                }
                if (t.tr == null) continue;

                Vector3 p = t.tr.localPosition;
                p.z -= ENEMY_CHARGE_SPEED * t.speedMul * (t.heavy ? 0.8f : 1f) * dt;
                if (t.tr.position.z - lp.z < 25f)
                    p.x = Mathf.MoveTowards(p.x, Mathf.Clamp(lp.x, p.x - 1.5f, p.x + 1.5f), 0.8f * dt);
                p.x = Mathf.Clamp(p.x, -ZONE_HALF_WIDTH, ZONE_HALF_WIDTH);
                t.tr.localPosition = p;

                Vector3 wp = t.tr.position;
                float dz = wp.z - lp.z;
                if (dz < radius + 3f && dz > -radius && Mathf.Abs(wp.x - lp.x) < radius + 1.5f)
                    Clash(wp);                                   // close: the army rushes in and punches
                if (dz < radius * 0.75f && dz > -radius && Mathf.Abs(wp.x - lp.x) < radius + 0.25f)
                {
                    t.alive = false;
                    fx?.EnemyDeath(wp + Vector3.up * 0.5f);
                    ReleaseEnemyNow(t);
                    Snd?.PlayZombieAttack(wp);
                    ChangeSoldiers(soldierCount - t.contactKills, wp);
                    continue;
                }
                if (dz < -6f) { t.alive = false; ReleaseEnemyNow(t); }
            }
        }

        private void SpawnEnemy(Target t, Transform parent, Vector3 localPos)
        {
            GameObject go = enemyPool.Count > 0 ? enemyPool.Pop() : Instantiate(enemyTemplate);
            go.name = t.heavy ? "DarkLegion_Heavy" : "DarkLegion_Soldier";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            float s = (enemyIsGenerated ? 1f : UNIT_SCALE) * (t.heavy ? 1.3f : 1f);
            go.transform.localScale = Vector3.one * s;
            go.SetActive(true);
            RandomizeAnimator(go);
            t.tr = go.transform;
            t.visual = go.transform;
            t.baseScale = go.transform.localScale;
            t.spawned = true;
            t.rends = go.GetComponentsInChildren<Renderer>(true);
            ClearTint(t.rends);
            t.coins = t.heavy ? 3 : 1;
        }

        private void ReleaseEnemyNow(Target t)
        {
            if (t.tr == null) return;
            t.tr.gameObject.SetActive(false);
            t.tr.SetParent(poolRoot, false);
            enemyPool.Push(t.tr.gameObject);
            t.tr = null;
        }

        /// <summary>Gunners behind sandbags: every 1.5 s within 34 m they shoot one of your soldiers.</summary>
        private void UpdateGunners(float dt)
        {
            Vector3 lp = LeaderPos;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t.kind != Kind.Gunner || !t.alive || t.tr == null) continue;
                float dz = t.tr.position.z - lp.z;
                if (dz > GUNNER_RANGE || dz < 2f) continue;
                t.fireTimer -= dt;
                if (t.fireTimer > 0f) continue;
                t.fireTimer = GUNNER_INTERVAL * Random.Range(0.85f, 1.15f);

                Vector3 from = t.muzzle != null ? t.muzzle.position : t.tr.position + Vector3.up * 1.2f;
                Transform aim = units.Count > 0 ? units[Random.Range(0, units.Count)].tr : (runner != null ? runner.transform : null);
                Vector3 aimPos = aim != null ? aim.position + Vector3.up * 0.7f : lp;
                Transform shot = shotPool.Count > 0 ? shotPool.Pop() : MakeTracer(s_enemyTracer, new Vector3(0.14f, 0.14f, 1.1f));
                shot.SetParent(root, true);
                shot.gameObject.SetActive(true);
                shot.position = from;
                shot.rotation = Quaternion.LookRotation(aimPos - from);
                enemyShots.Add(new EnemyShot { tr = shot, from = from, aim = aim, aimPos = aimPos, t = 0f, dur = Mathf.Max(0.15f, (aimPos - from).magnitude / ENEMY_SHOT_SPEED) });
                fx?.MuzzleFlash(from);
                Snd?.PlayGunfire(true);
            }
        }

        private void UpdateEnemyShots(float dt)
        {
            for (int i = enemyShots.Count - 1; i >= 0; i--)
            {
                var s = enemyShots[i];
                s.t += dt;
                Vector3 target = s.aim != null && s.aim.gameObject.activeInHierarchy ? s.aim.position + Vector3.up * 0.7f : s.aimPos;
                float k = Mathf.Clamp01(s.t / s.dur);
                if (s.tr != null)
                {
                    s.tr.position = Vector3.Lerp(s.from, target, k);
                    Vector3 dir = target - s.from;
                    if (dir.sqrMagnitude > 0.001f) s.tr.rotation = Quaternion.LookRotation(dir);
                }
                if (k >= 1f)
                {
                    if (s.tr != null) { s.tr.gameObject.SetActive(false); shotPool.Push(s.tr); }
                    enemyShots.RemoveAt(i);
                    fx?.BulletHit(target, new Color(1f, 0.3f, 0.2f));
                    ChangeSoldiers(soldierCount - 1, target);
                }
            }
        }

        // ═══════════════════════════════════════════ PARADE (enemy bridge) ══

        private void UpdateParade(float dt)
        {
            float march = (running && !finished && !arenaActive) ? 1.6f * dt : 0f;
            float t = Time.time * 9f;
            for (int i = 0; i < parade.Count; i++)
            {
                var p = parade[i];
                if (p.tr == null) continue;
                Vector3 lp = p.tr.localPosition;
                lp.z -= march;
                lp.y = Mathf.Abs(Mathf.Sin(t + p.phase)) * 0.07f;
                p.tr.localPosition = lp;
                bool visible = p.tr.position.z > -14f;
                if (p.tr.gameObject.activeSelf != visible) p.tr.gameObject.SetActive(visible);
            }
        }

        // ═══════════════════════════════════════════ ENEMY GROUP COUNTERS ══

        /// <summary>
        /// Every red squad shows how many are left (big red number above the squad's centre).
        /// When it reaches 0: "CLEARED!" + bonus coins.
        /// </summary>
        private void UpdateHordes()
        {
            for (int h = 0; h < hordes.Count; h++)
            {
                var hd = hordes[h];
                if (hd.cleared || hd.label == null) continue;
                int alive = 0;
                bool anySpawned = false;
                Vector3 sum = Vector3.zero;
                foreach (var m in hd.members)
                {
                    if (!m.alive) continue;
                    alive++;
                    Vector3 p = m.tr != null ? m.tr.position : trackRoot.TransformPoint(m.spawnLocal);
                    if (m.spawned) anySpawned = true;
                    sum += p;
                }
                if (alive == 0)
                {
                    hd.cleared = true;
                    Vector3 at = hd.label.transform.position;
                    hd.label.gameObject.SetActive(false);
                    int bonus = Mathf.Max(2, hd.members.Count / 2);
                    SpawnFloatText(at, "CLEARED!", new Color(1f, 0.9f, 0.35f));
                    AddCoins(bonus, at);
                    continue;
                }
                Vector3 c = sum / alive;
                bool show = anySpawned && c.z > -2f;
                if (hd.label.gameObject.activeSelf != show) hd.label.gameObject.SetActive(show);
                hd.label.transform.position = new Vector3(c.x, soldierHeight + 1.9f, c.z + 0.5f);
                string txt = alive.ToString();
                if (hd.label.text != txt) hd.label.text = txt;
            }
        }
    }
}
