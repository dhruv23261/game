using UnityEngine;
using TMPro;

namespace MobControl.Level
{
    /// <summary>
    /// GIANT ALLY (the "HELP!" cage from Mob Control).
    ///
    ///  • A giant copy of your soldier (same model, 2.2× size) is locked in a yellow cage, shouting HELP!
    ///  • Shoot the cage to 0 HP → he breaks out ("ALLY JOINED!") and walks 3 m in front of your army.
    ///  • Any red soldier that reaches him is punched dead (Fist Fight clip); each one costs him 1 HP (heavy: 2).
    ///  • During boss fights he walks up to the boss and punches it: 30 damage every 0.25 s.
    ///  • 40 HP. At 0 he plays the Death clip, "ALLY DOWN", and sinks away.
    /// </summary>
    public partial class ArmyRunLevel
    {
        private const float ALLY_SCALE = 2.2f;
        private const float ALLY_HP = 40f;
        private const float ALLY_BOSS_HIT = 30f;
        private const float ALLY_REACH = 1.5f;

        private class Ally
        {
            public Transform tr;
            public Animator anim;
            public float hp;
            public TextMeshPro label;
            public float hitTimer, punchAnimTimer, deadT = -1f;
        }
        private Ally ally;

        private void SpawnAlly(Vector3 at)
        {
            var go = Instantiate(unitTemplate);
            go.name = "GiantAlly";
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(at.x, 0f, at.z);
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * UNIT_SCALE * ALLY_SCALE;
            go.SetActive(true);
            ally = new Ally { tr = go.transform, anim = go.GetComponentInChildren<Animator>(), hp = ALLY_HP };
            if (hasArmyAnim && ally.anim != null) { ally.anim.Rebind(); ally.anim.SetBool(P_MOVING, true); }
            ally.label = CreateLabel(root, $"ALLY {Mathf.CeilToInt(ally.hp)}", Vector3.zero, 9f, new Color(0.6f, 0.9f, 1f), new Color(0.02f, 0.15f, 0.45f));
            fx?.Shockwave(at, 6f, new Color(0.5f, 0.85f, 1f, 1f));
            fx?.Sparkle(at + Vector3.up * 2f, new Color(0.5f, 0.85f, 1f), 30, 1.5f);
            SpawnFloatText(at + Vector3.up * 4.5f, "ALLY JOINED!", new Color(0.5f, 0.9f, 1f), 15f);
            Snd?.PlayUpgradeSuccess();
            Shake(0.15f);
        }

        private void UpdateAlly(float dt)
        {
            if (ally == null || ally.tr == null) return;

            // ── dead: lie down, sink, disappear ──
            if (ally.deadT >= 0f)
            {
                ally.deadT += dt;
                if (ally.deadT > 1.2f) ally.tr.position += Vector3.down * dt * 2f;
                if (ally.deadT > 2f)
                {
                    fx?.AllyDeath(ally.tr.position + Vector3.up);
                    Destroy(ally.tr.gameObject);
                    if (ally.label != null) Destroy(ally.label.gameObject);
                    ally = null;
                }
                return;
            }

            Vector3 lp = LeaderPos;
            Vector3 goal = new Vector3(Mathf.Clamp(lp.x, -2.4f, 2.4f), 0f, lp.z + 3f + CrowdRadius * 0.5f);

            // boss fight: walk up to the closest big enemy and punch it
            Target bigT = null;
            for (int i = 0; i < bigs.Count; i++)
            {
                var b = bigs[i];
                if (b.t != null && b.t.alive && b.t.tr != null && b.landed && b.engaged && b.t.tr.position.z - lp.z < 22f) { bigT = b.t; break; }
            }
            if (bigT != null)
                goal = bigT.tr.position + new Vector3(0f, 0f, -(bigT.halfWidth + 1.1f));
            goal.y = 0f;

            Vector3 p = ally.tr.position;
            p = Vector3.MoveTowards(p, goal, 6f * dt);
            p.y = 0f;
            ally.tr.position = p;

            // punch red soldiers that reach him
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t.kind != Kind.Enemy || !t.alive || !t.spawned || t.tr == null) continue;
                Vector3 d = t.tr.position - p;
                if (d.z > ALLY_REACH || d.z < -1f || Mathf.Abs(d.x) > ALLY_REACH) continue;
                fx?.BulletHit(t.tr.position + Vector3.up * 0.8f, new Color(0.6f, 0.85f, 1f));
                AddCoins(t.coins, t.tr.position);
                KillTarget(t, true);
                ally.hp -= t.contactKills;
                PunchAnim();
                if (ally.hp <= 0f) break;
            }

            // punch the boss
            if (bigT != null && (bigT.tr.position - p).magnitude < bigT.halfWidth + 1.8f)
            {
                ally.hitTimer -= dt;
                if (ally.hitTimer <= 0f)
                {
                    ally.hitTimer = 0.25f;
                    fx?.BulletHit(bigT.tr.position + Vector3.up * soldierHeight * 1.5f + Vector3.back, new Color(0.6f, 0.85f, 1f));
                    Snd?.PlayBulletHit(bigT.tr.position);
                    PunchAnim();
                    DamageTarget(bigT, ALLY_BOSS_HIT);
                }
            }

            if (ally == null) return;
            if (ally.label != null)
            {
                ally.label.text = $"ALLY {Mathf.Max(0, Mathf.CeilToInt(ally.hp))}";
                ally.label.transform.position = p + Vector3.up * (soldierHeight * ALLY_SCALE + 0.9f);
                if (cam != null) ally.label.transform.rotation = cam.transform.rotation;
            }

            if (ally.hp <= 0f)
            {
                ally.deadT = 0f;
                if (hasArmyAnim && ally.anim != null) ally.anim.SetTrigger(P_DIE);
                SpawnFloatText(p + Vector3.up * 4f, "ALLY DOWN", new Color(0.6f, 0.8f, 1f), 13f);
                if (ally.label != null) ally.label.gameObject.SetActive(false);
            }
        }

        private void PunchAnim()
        {
            if (ally == null || !hasArmyAnim || ally.anim == null) return;
            if (Time.time < ally.punchAnimTimer) return;
            ally.punchAnimTimer = Time.time + 0.35f;
            ally.anim.SetTrigger(P_ATTACK);
        }

        /// <summary>"HELP!" bubbles bounce so the cage catches the eye.</summary>
        private void UpdateHelpBubbles()
        {
            float t = Time.time;
            for (int i = 0; i < targets.Count; i++)
            {
                var tg = targets[i];
                if (tg.helpLabel == null || !tg.alive) continue;
                float s = 1f + 0.12f * Mathf.Abs(Mathf.Sin(t * 6f));
                tg.helpLabel.transform.localScale = Vector3.one * s;
                if (cam != null) tg.helpLabel.transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 5f) * 8f);
            }
        }
    }
}
