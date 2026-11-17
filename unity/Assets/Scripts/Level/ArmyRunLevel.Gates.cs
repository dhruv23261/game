using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MobControl.Combat;
using MobControl.UI;
using MobControl.Core;

namespace MobControl.Level
{
    /// <summary>
    /// GATE CONTROLLER + OBSTACLE CONTROLLER: gate triggering, spiked rollers, crashing into props.
    /// </summary>
    public partial class ArmyRunLevel
    {
        // ═══════════════════════════════════════════ GATES & HAZARDS ══

        /// <summary>A gate counts when it reaches the commander and his x is inside it. Its partner gates switch off.</summary>
        private void UpdateGates()
        {
            Vector3 lp = LeaderPos;
            float t = Time.time;
            for (int i = 0; i < gates.Count; i++)
            {
                var g = gates[i];
                if (g.tr == null) continue;
                if (g.moveAmp > 0f && !g.used)
                {
                    Vector3 p = g.tr.localPosition;
                    p.x = g.baseX + Mathf.Sin(t * g.moveSpeed) * g.moveAmp;
                    g.tr.localPosition = p;
                }
                if (g.plaque)                                  // only ~40 signs are ever switched on
                {
                    float dzp = g.tr.position.z - lp.z;
                    bool show = dzp < PLAQUE_VIEW_AHEAD && dzp > -8f;
                    if (g.tr.gameObject.activeSelf != show) g.tr.gameObject.SetActive(show);
                }
                if (g.used) continue;
                Vector3 gp = g.tr.position;
                if (gp.z > lp.z + 0.2f) continue;

                g.used = true;
                if (g.plaque)                                  // strip sign: the army's edge touching it counts
                {
                    if (Mathf.Abs(lp.x - gp.x) <= g.halfWidth + CrowdRadius * 0.6f) { CollectPlaque(g, gp); if (g.op == '+') missedPlus = 0; }
                    else if (g.op == '+')
                    {
                        plaqueCombo = 0;                       // skipped a plus sign: combo broken
                        if (++missedPlus >= 8 && Time.time > nextStripHint)   // drifting away from the strip: nudge
                        {
                            nextStripHint = Time.time + 9f;
                            missedPlus = 0;
                            SpawnFloatText(new Vector3(Mathf.Min(lp.x + 1.6f, 2.2f), 2.4f, lp.z + 2.5f), "GRAB THE +1s  >>", new Color(0.55f, 0.85f, 1f), 7f);
                        }
                    }
                    continue;
                }
                if (g.pair != null) g.pair.used = true;
                if (g.pair2 != null) g.pair2.used = true;
                bool locked = g.panel != null && !g.panel.gameObject.activeSelf;
                if (locked || Mathf.Abs(lp.x - gp.x) > g.halfWidth) continue;

                int n = soldierCount;
                if (g.op == '+') n += g.value;
                else if (g.op == 'x') n *= g.value;
                else n -= g.value;

                string pop = g.op == 'x' ? $"x{g.value}" : (g.op == '-' ? $"-{g.value}" : $"+{g.value}");
                SpawnFloatText(gp + Vector3.up * 2.8f, pop, g.op == '-' ? new Color(1f, 0.3f, 0.3f) : new Color(0.5f, 0.9f, 1f));
                fx?.Chunks(gp + Vector3.up * 1.2f, g.color, 14, 4f);
                fx?.Sparkle(gp + Vector3.up * 1.2f, g.color, 18, g.halfWidth);
                Snd?.PlayGatePass();
                if (g.panel != null) g.panel.gameObject.SetActive(false);
                if (g.label != null) g.label.gameObject.SetActive(false);
                ChangeSoldiers(n, gp);
            }
        }

        /// <summary>
        /// Spiked rollers hold ONE half of the road (left or right) and slide/spin inside it — the other half is
        /// always safe. When one crosses the army line, the overlapped share loses up to 25 %.
        /// </summary>
        private void UpdateRollers(float dt)
        {
            Vector3 lp = LeaderPos;
            float R = CrowdRadius;
            for (int i = 0; i < rollers.Count; i++)
            {
                var r = rollers[i];
                if (r.tr == null) continue;
                Vector3 p = r.tr.localPosition;
                float x = r.center + Mathf.Sin(Time.time * r.speed + r.phase) * r.amp;
                p.x = x;
                r.tr.localPosition = p;
                if (r.log != null) r.log.Rotate(Vector3.up, 600f * dt, Space.Self);   // menacing spin
                float rdz = r.tr.position.z - lp.z;
                if (!r.warned && rdz < 24f)                    // heads-up as it comes into view
                {
                    r.warned = true;
                    SpawnFloatText(r.tr.position + Vector3.up * 2.2f, "DANGER!", new Color(1f, 0.3f, 0.2f), 9f);
                }
                if (rdz < 45f && rdz > -6f) SwarmCrush(x - r.halfLen, x + r.halfLen, r.tr.position.z, 0.75f);   // flattens the swarm (on screen only)

                if (r.resolved) continue;
                float wz = r.tr.position.z;
                if (wz > lp.z + 0.3f) continue;
                r.resolved = true;
                float overlap = Mathf.Max(0f, Mathf.Min(x + r.halfLen, lp.x + R) - Mathf.Max(x - r.halfLen, lp.x - R));
                if (overlap <= 0f) { SpawnFloatText(new Vector3(lp.x, 2.5f, lp.z + 1f), "DODGED!", new Color(1f, 0.95f, 0.4f)); continue; }
                int kills = Mathf.Max(1, Mathf.CeilToInt(soldierCount * 0.25f * Mathf.Clamp01(overlap / (2f * R))));
                fx?.Chunks(new Vector3(x, 0.8f, lp.z), new Color(0.5f, 0.35f, 0.2f), 10, 5f);
                SpawnFloatText(new Vector3(x, 2.6f, lp.z + 1f), $"-{Mathf.Min(kills, soldierCount)}", new Color(1f, 0.3f, 0.3f));
                Shake(0.1f);
                ChangeSoldiers(soldierCount - kills, new Vector3(x, 0f, lp.z));
            }
        }

        /// <summary>Running into an unbroken barrel / cage / ice / explosive / bunker costs soldiers.</summary>
        private void UpdateObstacleContacts()
        {
            Vector3 lp = LeaderPos;
            float radius = CrowdRadius;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (!t.alive || t.tr == null) continue;
                if (t.kind != Kind.Barrel && t.kind != Kind.Ice && t.kind != Kind.Cage && t.kind != Kind.Explosive && t.kind != Kind.Gunner) continue;
                Vector3 tp = t.tr.position;
                if (tp.z - lp.z > radius * 0.6f + 0.6f) continue;

                if (Mathf.Abs(tp.x - lp.x) < t.halfWidth + radius * 0.6f)
                {
                    int cost;
                    switch (t.kind)
                    {
                        case Kind.Ice: cost = Mathf.CeilToInt(t.hp / 2f); break;
                        case Kind.Explosive: cost = 3 + Mathf.CeilToInt(soldierCount * 0.15f); break;
                        case Kind.Gunner: cost = 3; break;
                        default: cost = Mathf.CeilToInt(t.hp / 4f); break;
                    }
                    cost = Mathf.Max(1, cost);
                    SpawnFloatText(tp + Vector3.up * 2.4f, $"-{Mathf.Min(cost, soldierCount)}", new Color(1f, 0.3f, 0.3f));
                    if (t.kind == Kind.Explosive || t.kind == Kind.Gunner) KillTarget(t, true);
                    else
                    {
                        t.alive = false;
                        fx?.Chunks(tp + Vector3.up * 0.8f, t.kind == Kind.Ice ? new Color(0.6f, 0.9f, 1f) : new Color(0.55f, 0.35f, 0.18f), 12, 5f);
                        if (t.unlocksGate != null && t.unlocksGate.panel != null) t.unlocksGate.panel.gameObject.SetActive(true);
                        DestroyTargetObject(t);
                    }
                    Shake(0.12f);
                    ChangeSoldiers(soldierCount - cost, tp);
                }
                else if (tp.z < lp.z - 4f)
                {
                    t.alive = false; // dodged
                }
            }
        }

        // ═══════════════════════════════════════════ PLAQUES & WEAPON LEVEL ══
        private int plaquesCollected, weaponLevel = 1;
        private const int PLAQUES_PER_LEVEL = 15;
        private const float LEVEL_FIRE_MULT = 1.12f;

        /// <summary>+N plaque: soldiers join at once, panel pops, every 15 plaques the weapon levels up.</summary>
        private int plaqueCombo, missedPlus;
        private float nextStripHint;
        private const int COMBO_STEP = 20;

        private void CollectPlaque(Gate g, Vector3 gp)
        {
            if (g.panel != null) (g.panel.transform.parent != null ? g.panel.transform.parent : g.panel.transform).gameObject.SetActive(false);
            if (g.label != null) g.label.gameObject.SetActive(false);

            if (g.op == '-')                               // red trap sign
            {
                plaqueCombo = 0;
                fx?.Chunks(gp + Vector3.up * 0.7f, new Color(1f, 0.25f, 0.2f), 8, 3.5f);
                fx?.BulletHit(gp + Vector3.up * 0.7f, new Color(1f, 0.3f, 0.2f));
                SpawnFloatText(gp + new Vector3(-0.8f, 1.3f, 0f), "-" + g.value, new Color(1f, 0.28f, 0.22f), g.value >= 10 ? 8f : 7f);
                Shake(0.12f);
                ChangeSoldiers(soldierCount - g.value, gp);
                return;
            }

            fx?.Sparkle(gp + Vector3.up * 0.8f, g.color, 6, 0.5f);
            fx?.Chunks(gp + Vector3.up * 0.6f, g.color, 4, 2.5f);
            SpawnFloatText(gp + new Vector3(-0.7f, 1.2f, 0f), "+" + g.value, new Color(1f, 0.9f, 0.25f), g.value >= 10 ? 7f : 6f);
            Snd?.PlayCoinSpend();
            ChangeSoldiers(soldierCount + g.value, gp);

            plaquesCollected++;
            plaqueCombo++;
            if (plaqueCombo % COMBO_STEP == 0)               // PERFECT streak: bonus soldiers + gold burst
            {
                int bonus = plaqueCombo / 4;
                Vector3 lp0 = LeaderPos;
                fx?.Sparkle(lp0 + Vector3.up * 1.2f, new Color(1f, 0.85f, 0.3f), 24, CrowdRadius);
                SpawnFloatText(lp0 + new Vector3(0f, 2.8f, 1.2f), $"PERFECT x{plaqueCombo}  +{bonus}", new Color(1f, 0.85f, 0.25f), 8f);
                Snd?.PlayUpgradeSuccess();
                ChangeSoldiers(soldierCount + bonus, lp0);
            }
            if (plaquesCollected % PLAQUES_PER_LEVEL == 0 && weaponLevel < 10)
            {
                weaponLevel++;
                fireMultiplier *= LEVEL_FIRE_MULT;
                Vector3 lp = LeaderPos;
                fx?.Shockwave(lp, 6f, new Color(1f, 0.85f, 0.3f, 1f));
                fx?.Sparkle(lp + Vector3.up, new Color(1f, 0.85f, 0.3f), 30, CrowdRadius);
                SpawnFloatText(lp + new Vector3(1.4f, 2.2f, 0.6f), $"WEAPON LV {weaponLevel}", new Color(1f, 0.88f, 0.3f), 7f);
                Snd?.PlayGunUpgrade();
            }
        }
    }
}
