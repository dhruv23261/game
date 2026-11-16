using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MobControl.Level
{
    /// <summary>
    /// LEVEL 1 LAYOUT — 6 dense stages (Mob Control pacing: something on screen every second).
    /// z = metres from the start (the army reaches an object when it has run z metres).
    /// x: the main bridge is -3.6 … +3.6. Pair gates at ±1.7, triple gates at -2.3 / 0 / +2.3, plaque strips at ±2.75.
    /// </summary>
    public partial class ArmyRunLevel
    {
        private struct Stage { public float z; public string title; public string hint; }
        private readonly List<Stage> stages = new List<Stage>();
        private int stageIndex = -1;

        private const float PAIR_X = 1.7f, PAIR_HALF = 1.6f;
        private const float TRIPLE_X = 2.3f, TRIPLE_HALF = 1.1f;

        private void BuildTrack()
        {
            stages.Clear();
            float L = -PAIR_X, R = PAIR_X;
            const float PLAQUE_R = 2.75f, PLAQUE_L = -2.75f;

            if (currentLevel == 2)
            {
                BuildLevel2Track();
                return;
            }

            // ───────────── STAGE 1 · BOOT CAMP — collect +1s on the right, shoot the red river on the left ─────────────
            AddStage(0f, "BOOT CAMP", "Grab the +1s — shoot the red river!");
            AddPlaqueStrip(14f, 62f, PLAQUE_R, 1);
            AddStream(26f, 70f, -3.0f, -0.4f, 0.45f, 1f, 0f);
            AddContainer(40f, -1.6f, 5, 2.4f, ContainerColor.Red);
            AddGatePair(76f, '+', 10, 'x', 2);
            AddHorde(88f, 0f, 10, 2f, 0);

            // ───────────── STAGE 2 · RESCUE — free the giant ally ─────────────
            AddStage(98f, "RESCUE", "Shoot the cage — free the giant!");
            AddAllyCage(118f, -1.9f, 30);
            AddStream(104f, 168f, -3.0f, -0.3f, 0.6f, 2f, 0.05f);
            AddPlaqueStrip(104f, 162f, PLAQUE_R, 1);
            AddGunner(150f, 0.4f, 12);
            AddGateTriple(176f, 'x', 3, '+', 20, '-', 10);

            // ───────────── STAGE 3 · SPIKE ROLLER — plaques switch sides, rollers sweep ─────────────
            AddStage(186f, "SPIKE ROLLER", "Dodge the roller!");
            AddRoller(198f, 1.6f, 3.0f, 0f);
            AddPlaqueStrip(206f, 252f, PLAQUE_L, 2);
            AddStream(206f, 256f, 0.4f, 3.0f, 0.6f, 2f, 0.08f);
            AddExplosive(228f, 1.6f, 6);
            AddExplosive(244f, 2.4f, 6);
            AddRoller(260f, 1.8f, 4.0f, 1.2f);
            Gate g3 = AddGate(278f, L, 'x', 3, PAIR_HALF);
            Gate g25 = AddGate(278f, R, '+', 25, PAIR_HALF);
            g3.pair = g25; g25.pair = g3;
            AddContainer(273f, L, 60, PAIR_HALF * 2f, ContainerColor.Blue, g3);

            // ───────────── STAGE 4 · THE BRUTE — mini-boss ─────────────
            AddStage(288f, "THE BRUTE", "Dodge his ground slam!");
            AddHorde(300f, 0f, 10, 3f, 2);
            AddBrute(312f, BRUTE_HP);
            AddPlaqueStrip(325f, 362f, PLAQUE_R, 5);
            AddBarrel(345f, -1.7f, 60, Reward.FireRate, 0);
            AddCage(356f, -1.4f, 60, 15);
            AddGatePair(372f, 'x', 2, '+', 40);

            // ───────────── STAGE 5 · LEGION RIVER — full-width flood, +10 plaques for the brave ─────────────
            AddStage(382f, "LEGION RIVER", "Hold the line!");
            AddStream(392f, 452f, -3.0f, 3.0f, 1.0f, 3f, 0.12f);
            AddPlaqueStrip(394f, 446f, PLAQUE_R, 10);
            AddGunner(430f, -2.2f, 16);
            AddGunner(436f, 0.2f, 16);
            AddGateTriple(464f, 'x', 2, '+', 60, '-', 40);

            // ───────────── STAGE 6 · MASTER TYRANT — ArmyRunLevel.Boss.cs ─────────────
            levelLength = MASTER_TRIGGER_Z;
            AddStage(MASTER_TRIGGER_Z, "MASTER TYRANT", "Destroy the Master!");
        }

        private void BuildLevel2Track()
        {
            stages.Clear();
            float L = -PAIR_X, R = PAIR_X;
            const float PLAQUE_R = 2.75f, PLAQUE_L = -2.75f;

            // ───────────── STAGE 1 · CAVE MULTIPLIER RAMP (Inspired by Reference Video) ─────────────
            AddStage(0f, "MULTIPLIER RAMP", "Fire right to multiply — blast the red flood!");
            AddPlaqueStrip(12f, 64f, PLAQUE_R, 2);
            AddStream(22f, 72f, -3.0f, -0.3f, 0.5f, 1.2f, 0f);
            AddExplosive(38f, 0f, 6);
            AddContainer(44f, -1.6f, 8, 2.4f, ContainerColor.Red);
            AddGatePair(74f, 'x', 3, '+', 25);
            AddHorde(86f, 0f, 12, 2.5f, 2);

            // ───────────── STAGE 2 · CHAMPION RESCUE — free the giant ally ─────────────
            AddStage(96f, "CHAMPION RESCUE", "Shoot the HELP cage to unleash the Giant Champion!");
            AddAllyCage(114f, -1.9f, 25);
            AddStream(102f, 168f, -3.0f, -0.3f, 0.65f, 2.2f, 0.06f);
            AddPlaqueStrip(102f, 160f, PLAQUE_R, 2);
            AddGunner(148f, 0.4f, 16);
            AddGateTriple(174f, 'x', 4, '+', 35, '-', 20);

            // ───────────── STAGE 3 · CAVE SPIKE ROLLERS — brutal sweeping hazard ─────────────
            AddStage(184f, "CAVE SPIKE ROLLERS", "Dodge the brutal sweeping rollers!");
            AddRoller(196f, 1.8f, 3.8f, 0f);
            AddPlaqueStrip(204f, 250f, PLAQUE_L, 5);
            AddStream(204f, 254f, 0.4f, 3.0f, 0.65f, 2.2f, 0.08f);
            AddExplosive(224f, 1.6f, 8);
            AddExplosive(242f, -1.6f, 8);
            AddRoller(258f, 2.0f, 4.6f, 1.4f);
            Gate g4 = AddGate(276f, L, 'x', 4, PAIR_HALF);
            Gate g40 = AddGate(276f, R, '+', 40, PAIR_HALF);
            g4.pair = g40; g40.pair = g4;
            AddContainer(271f, L, 80, PAIR_HALF * 2f, ContainerColor.Blue, g4);

            // ───────────── STAGE 4 · THE CAVE BRUTE — 1000 HP mini-boss showdown ─────────────
            AddStage(286f, "THE CAVE BRUTE", "Unleash your Giant Champion against the Brute!");
            AddHorde(298f, 0f, 14, 3f, 3);
            AddBrute(310f, 1000f);
            AddPlaqueStrip(324f, 362f, PLAQUE_R, 10);
            AddBarrel(344f, -1.5f, 80, Reward.FireRate, 0);
            AddCage(355f, -1.4f, 70, 20);
            AddGatePair(370f, 'x', 3, '+', 50);

            // ───────────── STAGE 5 · THE RED HORDE & +99 GATES ─────────────
            AddStage(380f, "THE RED HORDE", "Hold back the swarm with +99 Multipliers!");
            AddStream(390f, 450f, -3.2f, 3.2f, 1.2f, 3.6f, 0.15f);
            AddPlaqueStrip(392f, 444f, PLAQUE_R, 20);
            AddGunner(428f, -2.2f, 20);
            AddGunner(434f, 0.2f, 20);
            AddGateTriple(462f, 'x', 4, '+', 99, '-', 50);

            // ───────────── STAGE 6 · SKELETON TYRANT — Final Boss Battle ─────────────
            levelLength = MASTER_TRIGGER_Z;
            AddStage(MASTER_TRIGGER_Z, "SKELETON TYRANT", "Obliterate the Colossal Overlord!");
        }

        private void AddStage(float z, string title, string hint) => stages.Add(new Stage { z = z, title = title, hint = hint });

        /// <summary>Shows "STAGE n / 6" banners in order as the army reaches each stage.</summary>
        private void UpdateStages()
        {
            int next = stageIndex + 1;
            if (next >= stages.Count) return;
            if (travelled + 12f < stages[next].z && next > 0) return;
            stageIndex = next;
            var s = stages[next];
            if (s.title == "MASTER TYRANT") return;  // the boss shows its own banner when it lands
            ShowBanner($"STAGE {next + 1} / {stages.Count}", s.title, s.hint);
        }

        // ═════════════════════════════════════ Mob Control pieces ══

        /// <summary>
        /// "+1 strip": a column of small blue (+1/+2) or orange (+5/+10) plaques along one edge, every 2.4 m.
        /// Each plaque the commander steers through adds its value. Every 15 collected = weapon LV +1.
        /// </summary>
        private void AddPlaqueStrip(float z0, float z1, float x, int value)
        {
            bool orange = value >= 5;
            Material panel = orange ? FlatMat(new Color(1f, 0.6f, 0.12f, 0.9f)) : FlatMat(new Color(0.22f, 0.58f, 1f, 0.92f));
            Material edge = orange ? FlatMat(new Color(1f, 0.85f, 0.4f)) : FlatMat(new Color(0.75f, 0.9f, 1f));
            for (float z = z0; z <= z1; z += 2.4f)
            {
                var go = NewOnTrack($"Plaque+{value}", x, z);
                CreateBox(go.transform, "Post", new Vector3(0f, 0.25f, 0.05f), new Vector3(0.12f, 0.5f, 0.12f), s_band);
                var p = CreateBox(go.transform, "Panel", new Vector3(0f, 0.75f, 0f), new Vector3(1.3f, 0.62f, 0.06f), panel);
                CreateBox(go.transform, "Edge", new Vector3(0f, 1.07f, -0.01f), new Vector3(1.34f, 0.06f, 0.07f), edge);
                CreateBox(go.transform, "EdgeB", new Vector3(0f, 0.43f, -0.01f), new Vector3(1.34f, 0.06f, 0.07f), edge);
                var label = CreateLabel(go.transform, "+" + value, new Vector3(0f, 0.78f, -0.08f), 7f, Color.white,
                    orange ? new Color(0.5f, 0.2f, 0f) : new Color(0.02f, 0.15f, 0.45f));
                gates.Add(new Gate
                {
                    tr = go.transform, halfWidth = 0.75f, op = '+', value = value, panel = p.GetComponent<Renderer>(), label = label,
                    baseX = x, plaque = true, color = orange ? new Color(1f, 0.7f, 0.25f) : new Color(0.45f, 0.8f, 1f)
                });
            }
        }

        /// <summary>
        /// A "red river": enemies scattered between z0..z1 inside the x band at 'density' per metre.
        /// They walk slowly (0.7×) and drift into the army when close — a constant stream to shoot.
        /// </summary>
        private void AddStream(float z0, float z1, float xMin, float xMax, float density, float hp, float heavyChance)
        {
            int n = Mathf.RoundToInt((z1 - z0) * density);
            for (int i = 0; i < n; i++)
            {
                bool heavy = Random.value < heavyChance;
                targets.Add(new Target
                {
                    kind = Kind.Enemy, halfWidth = heavy ? 0.42f : 0.32f,
                    hp = heavy ? hp * 3f : hp, maxHp = heavy ? hp * 3f : hp,
                    heavy = heavy, contactKills = heavy ? 2 : 1, speedMul = 0.7f,
                    spawnLocal = new Vector3(Random.Range(xMin, xMax), 0f, Mathf.Lerp(z0, z1, (i + Random.value) / n))
                });
            }
        }

        /// <summary>
        /// Rescue cage: a GIANT copy of your soldier shouts "HELP!". Shoot the cage open (HP) →
        /// the giant joins and fights in front of your army (see ArmyRunLevel.Ally.cs).
        /// </summary>
        private void AddAllyCage(float z, float x, int hp)
        {
            var go = NewOnTrack("AllyCage", x, z);
            var vis = NewChild(go.transform, "Visual");
            CreateBox(vis, "Floor", new Vector3(0f, 0.1f, 0f), new Vector3(2.6f, 0.2f, 2.2f), s_steel);
            CreateBox(vis, "Roof", new Vector3(0f, 3.35f, 0f), new Vector3(2.7f, 0.16f, 2.3f), s_steel);
            Material bars = ArmyRunEnvironment.LitMat("CageBars", new Color(1f, 0.78f, 0.15f), 0.6f);
            for (int i = 0; i < 7; i++)
            {
                float bx = -1.2f + i * 0.4f;
                CreatePrimitive(PrimitiveType.Cylinder, vis, "Bar", new Vector3(bx, 1.7f, -1.05f), new Vector3(0.09f, 1.6f, 0.09f), bars);
                CreatePrimitive(PrimitiveType.Cylinder, vis, "Bar", new Vector3(bx, 1.7f, 1.05f), new Vector3(0.09f, 1.6f, 0.09f), bars);
            }
            CreateBox(vis, "Rail", new Vector3(0f, 2.6f, -1.05f), new Vector3(2.6f, 0.1f, 0.1f), bars);
            var giant = Instantiate(unitTemplate, vis);
            giant.transform.localPosition = new Vector3(0f, 0.2f, 0.1f);
            giant.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            giant.transform.localScale = Vector3.one * UNIT_SCALE * ALLY_SCALE;
            giant.SetActive(true);
            ArmyRunFX.AddBlobShadow(go.transform, 3.2f);
            var help = CreateLabel(go.transform, "HELP!", new Vector3(0.6f, 4.3f, 0f), 14f, new Color(1f, 0.88f, 0.2f), new Color(0.35f, 0.15f, 0f));
            var label = CreateLabel(go.transform, hp.ToString(), new Vector3(0f, 3.75f, -0.6f), 10f, Color.white, new Color(0.05f, 0.2f, 0.45f));
            var t = new Target
            {
                kind = Kind.Cage, tr = go.transform, visual = vis, halfWidth = 1.3f, depth = 1.1f, hp = hp, maxHp = hp, label = label,
                freesGiant = true, helpLabel = help, rends = vis.GetComponentsInChildren<MeshRenderer>(true), coins = 10
            };
            targets.Add(t);
        }

        // ═════════════════════════════════════ Gates ══

        private void AddGatePair(float z, char opL, int vL, char opR, int vR)
        {
            Gate a = AddGate(z, -PAIR_X, opL, vL, PAIR_HALF);
            Gate b = AddGate(z, PAIR_X, opR, vR, PAIR_HALF);
            a.pair = b; b.pair = a;
        }

        private void AddGateTriple(float z, char o1, int v1, char o2, int v2, char o3, int v3)
        {
            Gate a = AddGate(z, -TRIPLE_X, o1, v1, TRIPLE_HALF);
            Gate b = AddGate(z, 0f, o2, v2, TRIPLE_HALF);
            Gate c = AddGate(z, TRIPLE_X, o3, v3, TRIPLE_HALF);
            a.pair = b; a.pair2 = c; b.pair = a; b.pair2 = c; c.pair = a; c.pair2 = b;
        }

        private Gate AddGate(float z, float x, char op, int value, float halfWidth, float moveAmp = 0f, float moveSpeed = 0f)
        {
            bool bad = op == '-';
            var go = new GameObject($"Gate_{op}{value}");
            go.transform.SetParent(trackRoot, false);
            go.transform.localPosition = new Vector3(x, 0f, z);

            float w = halfWidth * 2f - 0.14f, h = 2.3f;
            CreateBox(go.transform, "PostL", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.18f, h, 0.24f), s_frame);
            CreateBox(go.transform, "PostR", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.18f, h, 0.24f), s_frame);
            CreateBox(go.transform, "TopBar", new Vector3(0f, h, 0f), new Vector3(w + 0.18f, 0.2f, 0.26f), s_frame);
            CreateBox(go.transform, "Glow", new Vector3(0f, h - 0.05f, -0.14f), new Vector3(w, 0.08f, 0.02f), FlatMat(bad ? new Color(1f, 0.5f, 0.5f) : new Color(0.6f, 0.9f, 1f)));
            var panel = CreateBox(go.transform, "Panel", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, 0.05f), bad ? s_gateRed : s_gateBlue);

            string txt = op == 'x' ? $"x{value}" : (bad ? $"-{value}" : $"+{value}");
            var label = CreateLabel(go.transform, txt, new Vector3(0f, h * 0.55f, -0.12f), halfWidth < 1.3f ? 10f : 12f, Color.white,
                bad ? new Color(0.45f, 0.02f, 0.02f) : new Color(0.02f, 0.18f, 0.5f));

            var gate = new Gate
            {
                tr = go.transform, halfWidth = halfWidth, op = op, value = value, panel = panel.GetComponent<Renderer>(), label = label,
                baseX = x, moveAmp = moveAmp, moveSpeed = moveSpeed,
                color = bad ? new Color(1f, 0.35f, 0.3f) : new Color(0.45f, 0.8f, 1f)
            };
            gates.Add(gate);
            return gate;
        }

        // ═════════════════════════════════════ Shootable props ══

        /// <summary>Wooden barrel: number = HP. Reward when shot open.</summary>
        private void AddBarrel(float z, float x, int hp, Reward reward, int rewardValue)
        {
            var go = NewOnTrack("Barrel", x, z);
            var vis = NewChild(go.transform, "Visual");
            CreatePrimitive(PrimitiveType.Cylinder, vis, "Body", new Vector3(0f, 0.7f, 0f), new Vector3(1.05f, 0.7f, 1.05f), s_wood);
            CreatePrimitive(PrimitiveType.Cylinder, vis, "BandTop", new Vector3(0f, 1.1f, 0f), new Vector3(1.1f, 0.06f, 1.1f), s_band);
            CreatePrimitive(PrimitiveType.Cylinder, vis, "BandBot", new Vector3(0f, 0.3f, 0f), new Vector3(1.1f, 0.06f, 1.1f), s_band);
            var mark = CreateBox(vis, "RewardMark", new Vector3(0f, 0.72f, -0.53f), new Vector3(0.45f, 0.45f, 0.04f),
                reward == Reward.FireRate ? FlatMat(new Color(1f, 0.55f, 0.1f)) : s_rune);
            mark.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            ArmyRunFX.AddBlobShadow(go.transform, 1.6f);
            var label = CreateLabel(go.transform, hp.ToString(), new Vector3(0f, 2.1f, 0f), 11f, Color.white, new Color(0.25f, 0.12f, 0.02f));
            targets.Add(new Target { kind = Kind.Barrel, tr = go.transform, visual = vis, halfWidth = 0.7f, hp = hp, maxHp = hp, label = label, reward = reward, rewardValue = rewardValue, rends = vis.GetComponentsInChildren<Renderer>(true), coins = 5 });
        }

        /// <summary>Red explosive barrel: blows up everything within 3.6 m (chains).</summary>
        private void AddExplosive(float z, float x, int hp)
        {
            var go = NewOnTrack("ExplosiveBarrel", x, z);
            var vis = NewChild(go.transform, "Visual");
            CreatePrimitive(PrimitiveType.Cylinder, vis, "Body", new Vector3(0f, 0.65f, 0f), new Vector3(0.9f, 0.65f, 0.9f), s_redBarrel);
            CreatePrimitive(PrimitiveType.Cylinder, vis, "Hazard1", new Vector3(0f, 0.95f, 0f), new Vector3(0.93f, 0.08f, 0.93f), s_hazard);
            CreatePrimitive(PrimitiveType.Cylinder, vis, "Hazard2", new Vector3(0f, 0.35f, 0f), new Vector3(0.93f, 0.08f, 0.93f), s_hazard);
            CreatePrimitive(PrimitiveType.Cylinder, vis, "Lid", new Vector3(0f, 1.31f, 0f), new Vector3(0.7f, 0.02f, 0.7f), s_band);
            ArmyRunFX.AddBlobShadow(go.transform, 1.4f);
            var label = CreateLabel(go.transform, hp.ToString(), new Vector3(0f, 2.0f, 0f), 10f, new Color(1f, 0.85f, 0.3f), new Color(0.4f, 0f, 0f));
            targets.Add(new Target { kind = Kind.Explosive, tr = go.transform, visual = vis, halfWidth = 0.6f, hp = hp, maxHp = hp, label = label, rends = vis.GetComponentsInChildren<MeshRenderer>(true), coins = 2 });
        }

        /// <summary>Steel cage with captured blue soldiers. Shoot it open → +N soldiers.</summary>
        private void AddCage(float z, float x, int hp, int freed)
        {
            var go = NewOnTrack("PrisonerCage", x, z);
            var vis = NewChild(go.transform, "Visual");
            CreateBox(vis, "Floor", new Vector3(0f, 0.08f, 0f), new Vector3(2f, 0.16f, 1.6f), s_steel);
            CreateBox(vis, "Roof", new Vector3(0f, 2.05f, 0f), new Vector3(2.1f, 0.14f, 1.7f), s_steel);
            for (int i = 0; i < 6; i++)
            {
                float bx = -0.95f + i * 0.38f;
                CreatePrimitive(PrimitiveType.Cylinder, vis, "Bar", new Vector3(bx, 1.05f, -0.78f), new Vector3(0.07f, 0.95f, 0.07f), s_steel);
                CreatePrimitive(PrimitiveType.Cylinder, vis, "Bar", new Vector3(bx, 1.05f, 0.78f), new Vector3(0.07f, 0.95f, 0.07f), s_steel);
            }
            for (int i = 0; i < 3; i++)
            {
                var p = Instantiate(unitTemplate, vis);
                p.transform.localPosition = new Vector3(-0.55f + i * 0.55f, 0.16f, Random.Range(-0.2f, 0.2f));
                p.transform.localRotation = Quaternion.Euler(0f, 180f + Random.Range(-30f, 30f), 0f);
                p.transform.localScale = Vector3.one * UNIT_SCALE * 0.9f;
                p.SetActive(true);
            }
            ArmyRunFX.AddBlobShadow(go.transform, 2.6f);
            var label = CreateLabel(go.transform, hp.ToString(), new Vector3(0f, 2.9f, 0f), 11f, Color.white, new Color(0.05f, 0.2f, 0.45f));
            targets.Add(new Target { kind = Kind.Cage, tr = go.transform, visual = vis, halfWidth = 1.0f, depth = 0.8f, hp = hp, maxHp = hp, label = label, reward = Reward.Soldiers, rewardValue = freed, rends = vis.GetComponentsInChildren<MeshRenderer>(true), coins = 5 });
        }

        /// <summary>Ice block (frozen blue container): locks the gate behind it until shot to 0.</summary>
        private void AddIce(float z, float x, int hp, float width, Gate unlocks)
            => AddContainer(z, x, hp, width, ContainerColor.Ice, unlocks);

        private enum ContainerColor { Ice, Blue, Red, Yellow }

        /// <summary>
        /// Shipping container with a number = HP (a "3" needs 3 hits). Cracks, darkens and drops chunks
        /// in 4 steps as it is shot; bursts apart at 0. Can lock a gate behind it.
        /// </summary>
        private void AddContainer(float z, float x, int hp, float width, ContainerColor colour, Gate unlocks = null)
        {
            Material body, rib;
            switch (colour)
            {
                case ContainerColor.Blue: body = ArmyRunEnvironment.LitMat("CBlue", new Color(0.18f, 0.45f, 0.85f), 0.45f); rib = ArmyRunEnvironment.LitMat("CBlueRib", new Color(0.12f, 0.32f, 0.65f), 0.45f); break;
                case ContainerColor.Red: body = ArmyRunEnvironment.LitMat("CRed", new Color(0.85f, 0.22f, 0.18f), 0.45f); rib = ArmyRunEnvironment.LitMat("CRedRib", new Color(0.62f, 0.14f, 0.12f), 0.45f); break;
                case ContainerColor.Yellow: body = ArmyRunEnvironment.LitMat("CYel", new Color(0.98f, 0.72f, 0.15f), 0.45f); rib = ArmyRunEnvironment.LitMat("CYelRib", new Color(0.8f, 0.52f, 0.08f), 0.45f); break;
                default: body = s_ice; rib = s_iceRib; break;
            }
            var go = NewOnTrack(colour == ContainerColor.Ice ? "IceBlock" : "Container", x, z);
            var vis = NewChild(go.transform, "Visual");
            CreateBox(vis, "Body", new Vector3(0f, 0.8f, 0f), new Vector3(width - 0.1f, 1.6f, 1.4f), body);
            int ribs = Mathf.Max(3, Mathf.RoundToInt(width * 1.4f));
            for (int i = 0; i < ribs; i++)
            {
                float rx = -width * 0.5f + 0.25f + i * (width - 0.5f) / (ribs - 1);
                CreateBox(vis, "Rib", new Vector3(rx, 0.8f, -0.71f), new Vector3(0.08f, 1.5f, 0.04f), rib);
            }
            CreateBox(vis, "Frame", new Vector3(0f, 1.6f, -0.7f), new Vector3(width - 0.05f, 0.1f, 0.06f), rib);
            CreateBox(vis, "Top", new Vector3(0f, 1.61f, 0f), new Vector3(width - 0.15f, 0.04f, 1.35f),
                colour == ContainerColor.Ice ? FlatMat(new Color(0.9f, 0.98f, 1f)) : rib);
            ArmyRunFX.AddBlobShadow(go.transform, width + 0.4f);
            var label = CreateLabel(go.transform, hp.ToString(), new Vector3(0f, 2.5f, 0f), 12f, Color.white,
                colour == ContainerColor.Red ? new Color(0.45f, 0.05f, 0.05f) : new Color(0.05f, 0.25f, 0.5f));
            if (unlocks != null && unlocks.panel != null) unlocks.panel.gameObject.SetActive(false);
            targets.Add(new Target
            {
                kind = Kind.Ice, tr = go.transform, visual = vis, halfWidth = width * 0.5f, depth = 0.75f, hp = hp, maxHp = hp,
                label = label, unlocksGate = unlocks, rends = vis.GetComponentsInChildren<Renderer>(true), coins = 3
            });
        }

        // ═════════════════════════════════════ Enemies ══

        /// <summary>A block of red soldiers that will charge. 'heavies' of them are bigger (3× HP, kill 2 on contact).</summary>
        private void AddHorde(float z, float centerX, int count, float hpEach, int heavies, float maxHalf = -1f)
        {
            if (maxHalf < 0f) maxHalf = Mathf.Abs(centerX) < 0.01f ? ZONE_HALF_WIDTH - 0.4f : 1.6f;
            float spacing = 0.6f;
            int cols = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(count * 1.8f)), 1, Mathf.FloorToInt(maxHalf * 2f / spacing) + 1);
            var horde = new Horde();
            horde.label = CreateLabel(root, count.ToString(), Vector3.zero, 13f, new Color(1f, 0.35f, 0.3f), new Color(0.25f, 0f, 0f));
            horde.label.gameObject.SetActive(false);
            hordes.Add(horde);
            for (int i = 0; i < count; i++)
            {
                int row = i / cols, col = i % cols;
                int inRow = Mathf.Min(cols, count - row * cols);
                float x = centerX + (col - (inRow - 1) * 0.5f) * spacing + Random.Range(-0.08f, 0.08f);
                bool heavy = i >= count - heavies;
                Target member;
                targets.Add(member = new Target
                {
                    kind = Kind.Enemy, halfWidth = heavy ? 0.42f : 0.32f,
                    hp = heavy ? hpEach * 3f : hpEach, maxHp = heavy ? hpEach * 3f : hpEach,
                    heavy = heavy, contactKills = heavy ? 2 : 1,
                    spawnLocal = new Vector3(Mathf.Clamp(x, -ZONE_HALF_WIDTH, ZONE_HALF_WIDTH), 0f, z + row * spacing + Random.Range(-0.1f, 0.1f)),
                    horde = horde
                });
                horde.members.Add(member);
            }
        }

        /// <summary>Red rifleman behind a sandbag wall. Fires at your army; must be shot through the sandbags.</summary>
        private void AddGunner(float z, float x, int hp)
        {
            var go = NewOnTrack("GunnerBunker", x, z);
            var vis = NewChild(go.transform, "Visual");
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 4 - row; i++)
                {
                    var bag = CreatePrimitive(PrimitiveType.Sphere, vis, "Sandbag",
                        new Vector3(-0.75f + i * 0.5f + row * 0.25f, 0.2f + row * 0.32f, -0.55f), new Vector3(0.6f, 0.36f, 0.42f), s_sand);
                    bag.transform.localRotation = Quaternion.Euler(0f, Random.Range(-8f, 8f), 0f);
                }
            var soldier = Instantiate(enemyTemplate, vis);
            soldier.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            soldier.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            soldier.transform.localScale = Vector3.one * (enemyIsGenerated ? 1.05f : UNIT_SCALE * 1.05f);
            soldier.SetActive(true);
            var muzzle = NewChild(go.transform, "Muzzle");
            muzzle.localPosition = new Vector3(0.15f, soldierHeight * 0.6f, -0.6f);
            ArmyRunFX.AddBlobShadow(go.transform, 2f);
            var label = CreateLabel(go.transform, hp.ToString(), new Vector3(0f, soldierHeight + 1.3f, 0f), 10f, new Color(1f, 0.6f, 0.5f), new Color(0.35f, 0f, 0f));
            targets.Add(new Target { kind = Kind.Gunner, tr = go.transform, visual = vis, halfWidth = 1.0f, depth = 0.7f, hp = hp, maxHp = hp, label = label, muzzle = muzzle, fireTimer = 0.6f, rends = vis.GetComponentsInChildren<Renderer>(true), coins = 4 });
        }

        // ═════════════════════════════════════ Hazards ══

        /// <summary>Spiked roller sweeping left↔right across the bridge (can't be shot — dodge it).</summary>
        private void AddRoller(float z, float halfLen, float speed, float phase)
        {
            var go = NewOnTrack("SpikedRoller", 0f, z);
            // warning stripes on the road
            for (int i = 0; i < 10; i++)
                CreateBox(go.transform, "Warn", new Vector3(-4.3f + i * 0.96f, 0.015f, 0f), new Vector3(0.5f, 0.02f, 0.5f), i % 2 == 0 ? s_hazard : s_band);
            var arm = NewChild(go.transform, "Arm");
            arm.localPosition = new Vector3(0f, 0f, 0f);
            var log = NewChild(arm, "Log");
            log.localPosition = new Vector3(0f, 0.7f, 0f);
            log.localRotation = Quaternion.Euler(0f, 0f, 90f);      // cylinder axis → world X
            CreatePrimitive(PrimitiveType.Cylinder, log, "Core", Vector3.zero, new Vector3(1.0f, halfLen, 1.0f), s_log);
            for (int i = 0; i < 9; i++)
                for (int k = 0; k < 6; k++)
                {
                    float y = -halfLen + 0.25f + i * (halfLen * 2f - 0.5f) / 8f;
                    var spike = CreateBox(log, "Spike", Quaternion.Euler(0f, k * 60f + i * 20f, 0f) * new Vector3(0f, 0f, 0.55f) + Vector3.up * y,
                        new Vector3(0.12f, 0.12f, 0.45f), s_spike);
                    spike.transform.localRotation = Quaternion.Euler(0f, k * 60f + i * 20f, 0f);
                }
            CreatePrimitive(PrimitiveType.Cylinder, log, "CapA", new Vector3(0f, halfLen, 0f), new Vector3(1.15f, 0.06f, 1.15f), s_hazard);
            CreatePrimitive(PrimitiveType.Cylinder, log, "CapB", new Vector3(0f, -halfLen, 0f), new Vector3(1.15f, 0.06f, 1.15f), s_hazard);
            ArmyRunEnvironment.CombineChildren(log);   // 60 spikes → a couple of draw calls, still spins
            ArmyRunFX.AddBlobShadow(arm, halfLen * 2f);
            rollers.Add(new Roller { tr = arm, log = log, halfLen = halfLen, speed = speed, phase = phase });
        }

        // ═════════════════════════════════════ Scenery army (enemy bridge) ══

        private void AddParade(float z, int rows)
        {
            Mesh rowMesh = ArmyRunModels.HordeRowMesh(8, 0.62f, soldierHeight);
            Material mat = ArmyRunModels.HordeMaterial();
            for (int r = 0; r < rows; r += 2)
            {
                var go = new GameObject("ParadeRows");
                go.transform.SetParent(trackRoot, false);
                go.transform.localPosition = new Vector3(ArmyRunEnvironment.ENEMY_LANE_X, 0f, z + r * 0.62f);
                go.AddComponent<MeshFilter>().sharedMesh = rowMesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                parade.Add(new ParadeRow { tr = go.transform, phase = r * 0.7f });
            }
        }

        // ═════════════════════════════════════ helpers ══

        private GameObject NewOnTrack(string name, float x, float z)
        {
            var go = new GameObject(name);
            go.transform.SetParent(trackRoot, false);
            go.transform.localPosition = new Vector3(x, 0f, z);
            return go;
        }

        private static Transform NewChild(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }
    }
}
