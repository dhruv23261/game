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

        // Swarm lane (main road minus the +1 strip on the right) and the strip itself
        private const float SWARM_L = -3.2f, SWARM_R = 1.75f;
        private const float STRIP_X = 2.85f;
        private const float PLAQUE_SPACING = 1.15f;    // signs almost touch: one continuous column, like the reference
        private const float PLAQUE_VIEW_AHEAD = 44f;   // signs exist on screen only from 8 m behind to 44 m ahead

        /// <summary>
        /// LEVEL 1 — "CAVE SPIKE ROLLER" (Mob Control reference video).
        /// A +1 strip runs down the right edge the whole way; the main lane is a solid red carpet of mobs.
        /// Hug the strip to grow, shoot the carpet to push forward, free the giant, survive the rollers,
        /// kill the yellow Brute, then the Master Tyrant.
        /// </summary>
        private void BuildTrack()
        {
            stages.Clear();
            const float SP = PLAQUE_SPACING;

            // ALLY: offered twice — a 60 s helper in stage 1 and a champion right before the Master Tyrant.
            // +1 STRIP PROGRESSION (the whole level is one continuous strip down the right edge, with red minus runs):
            //   stage 1  +1 (blue, teaches the strip)      stage 2  +2
            //   stage 3  +3 → orange +10 into the Brute     stage 4  +5 → orange +20 → +99 jackpot before the Master
            // Leaving the strip to dodge a roller costs you growth: that is the decision the level asks for.

            // ───────────── STAGE 1 · THE HORDE — grab +1s, mow the red carpet, free the giant ─────────────
            AddStage(0f, "THE HORDE", "Hug the +1 strip on the right!");
            AddPlaqueStrip(5f, 128f, STRIP_X, 1, SP, 3, 24f);
            AddSwarmRamp(34f, 60f, SWARM_L, SWARM_R, 0.6f, 1.3f);       // thin front: learn to shoot while growing
            AddAllyCage(66f, -3.3f, 40, 60f);                            // ally #1: helps for 60 s
            AddSwarm(60f, 74f, -1.0f, SWARM_R, 1.3f);
            AddSwarmRamp(74f, 128f, SWARM_L, SWARM_R, 1.3f, 2.1f);

            // ───────────── STAGE 2 · SPIKE ROLLER — rollers flatten the swarm (and you) ─────────────
            AddStage(130f, "SPIKE ROLLER", "Let the roller crush them — dodge it!");
            AddPlaqueStrip(131f, 210f, STRIP_X, 2, SP, 6, 20f);
            AddRoller(142f, -1, 1.6f, 0f);                               // left half: free dodge
            AddSwarmRamp(148f, 210f, SWARM_L, SWARM_R, 2.2f, 3.0f, 0.03f);
            AddRoller(180f, 1, 2.0f, 1.2f);                              // right half: leave the strip or pay

            // ───────────── STAGE 3 · THE BRUTE — giant yellow brute inside the swarm ─────────────
            AddStage(214f, "THE BRUTE", "Melt the Brute — dodge his slam!");
            AddPlaqueStrip(215f, 270f, STRIP_X, 3, SP, 10, 18f);
            AddPlaqueStrip(271f, 300f, STRIP_X, 10, SP, 25, 14f);
            AddSwarmRamp(224f, 300f, SWARM_L, SWARM_R, 2.6f, 3.4f, 0.05f);
            AddBrute(262f, BRUTE_HP);

            // ───────────── STAGE 4 · FINAL PUSH — orange strip against the thickest swarm ─────────────
            AddStage(304f, "FINAL PUSH", "Orange +20s! Break the last wall!");
            AddPlaqueStrip(305f, 360f, STRIP_X, 5, SP, 15, 16f);
            AddPlaqueStrip(361f, 397f, STRIP_X, 20, SP, 50, 14f);
            AddPlaqueStrip(398f, 404f, STRIP_X, 99, SP);                    // jackpot: no traps
            AddSwarmRamp(312f, 392f, SWARM_L, SWARM_R, 3.0f, 3.8f, 0.08f);
            AddRoller(334f, -1, 2.2f, 0.4f);
            AddRoller(374f, 1, 2.6f, 2.0f);
            AddAllyCage(400f, -3.3f, 60);                                // ally #2: your champion for the boss fight
            AddSwarm(392f, 410f, -1.0f, SWARM_R, 3.0f, 0.05f);

            // ───────────── STAGE 5 · MASTER TYRANT — ArmyRunLevel.Boss.cs ─────────────
            levelLength = MASTER_TRIGGER_Z;
            AddStage(MASTER_TRIGGER_Z, "MASTER TYRANT", "Destroy the Master!");
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
        /// <summary>
        /// A stretch of the +1 strip: a sign every 'spacing' metres. If minusValue > 0, every 'minusEvery' metres a run
        /// of 3 RED minus signs replaces the plus signs (never in the first 12 m of a stretch) — steer off the strip to
        /// dodge them and you skip a few plus signs: a real choice instead of free growth.
        /// </summary>
        private void AddPlaqueStrip(float z0, float z1, float x, int value, float spacing = 2.4f, int minusValue = 0, float minusEvery = 20f)
        {
            int period = Mathf.Max(6, Mathf.RoundToInt(minusEvery / spacing));
            const int RUN = 3;
            int i = 0;
            for (float z = z0; z <= z1 + 0.01f; z += spacing, i++)
            {
                bool minus = minusValue > 0 && z - z0 > 12f && (i % period) >= period - RUN;
                AddSign(z, x, minus ? '-' : '+', minus ? minusValue : value);
            }
        }

        /// <summary>One strip sign: blue (+ under 10) / orange (+10 and up) / red (minus). Text printed on the sign.</summary>
        private void AddSign(float z, float x, char op, int value)
        {
            bool minus = op == '-';
            bool orange = !minus && value >= 10;
            // unlit + saturated so the bright snow light can't wash them out
            Material panel = FlatMat(minus ? new Color(0.92f, 0.12f, 0.1f) : orange ? new Color(1f, 0.55f, 0.06f) : new Color(0.13f, 0.43f, 0.96f));
            Material rim = FlatMat(minus ? new Color(1f, 0.82f, 0.78f) : orange ? new Color(1f, 0.93f, 0.6f) : new Color(0.92f, 0.97f, 1f));
            Material post = ArmyRunEnvironment.LitMat("SignPost", new Color(0.3f, 0.33f, 0.4f), 0.5f);
            Color outline = minus ? new Color(0.4f, 0f, 0f) : orange ? new Color(0.55f, 0.22f, 0f) : new Color(0.02f, 0.16f, 0.48f);
            string txt = (minus ? "-" : "+") + value;
            float fontSize = value >= 10 ? 5.2f : 6.2f;

            var go = NewOnTrack($"Plaque{txt}", x, z);
            CreateBox(go.transform, "Post", new Vector3(0f, 0.2f, 0.2f), new Vector3(0.9f, 0.4f, 0.08f), post);
            // the sign leans back 50° so it faces the high camera — a big readable rectangle, not a sliver
            var sign = NewChild(go.transform, "Sign");
            sign.localPosition = new Vector3(0f, 0.55f, 0f);
            sign.localRotation = Quaternion.Euler(50f, 0f, 0f);
            CreateBox(sign, "Rim", new Vector3(0f, 0f, 0.02f), new Vector3(1.32f, 0.92f, 0.05f), rim);
            var p = CreateBox(sign, "Panel", new Vector3(0f, 0f, -0.01f), new Vector3(1.18f, 0.78f, 0.05f), panel);
            if (minus)   // skull-ish warning stripes so red reads as danger at a glance
                for (int k = 0; k < 2; k++)
                    CreateBox(sign, "Stripe", new Vector3(0f, k == 0 ? 0.31f : -0.31f, -0.04f), new Vector3(1.18f, 0.07f, 0.02f), s_band);
            var label = CreateLabel(sign, txt, new Vector3(0f, 0.03f, -0.06f), fontSize, Color.white, outline);
            label.transform.localRotation = Quaternion.identity;        // flat on the sign, facing the camera with it
            if (z > PLAQUE_VIEW_AHEAD) go.SetActive(false);              // UpdateGates switches it on as it comes into view
            gates.Add(new Gate
            {
                tr = go.transform, halfWidth = 0.75f, op = op, value = value, panel = p.GetComponent<Renderer>(), label = label,
                baseX = x, plaque = true,
                color = minus ? new Color(1f, 0.3f, 0.25f) : orange ? new Color(1f, 0.75f, 0.25f) : new Color(0.45f, 0.8f, 1f)
            });
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
        private void AddAllyCage(float z, float x, int hp, float allyLife = -1f)
        {
            var go = NewOnTrack("AllyCage", x, z);
            // concrete alcove sticking out of the left side of the road (stays after the cage breaks)
            Material concrete = ArmyRunEnvironment.LitMat("Alcove", new Color(0.62f, 0.64f, 0.68f), 0.15f);
            Material concreteDark = ArmyRunEnvironment.LitMat("AlcoveDark", new Color(0.45f, 0.47f, 0.52f), 0.1f);
            CreateBox(go.transform, "AlcoveFloor", new Vector3(-0.9f, -0.05f, 0f), new Vector3(3.8f, 0.1f, 3.4f), concrete);
            CreateBox(go.transform, "AlcoveBase", new Vector3(-0.9f, -3.2f, 0f), new Vector3(3.6f, 6.2f, 3.2f), concreteDark);
            CreateBox(go.transform, "AlcoveBack", new Vector3(-0.9f, 1.2f, 1.6f), new Vector3(3.8f, 2.6f, 0.25f), concrete);
            CreateBox(go.transform, "AlcoveSide", new Vector3(-2.75f, 1.2f, 0f), new Vector3(0.25f, 2.6f, 3.4f), concrete);

            var vis = NewChild(go.transform, "Visual");
            Material bars = ArmyRunEnvironment.LitMat("CageBars", new Color(1f, 0.78f, 0.15f), 0.6f);
            for (int i = 0; i < 7; i++)   // yellow fence facing the camera
            {
                float bx = -1.9f + i * 0.5f;
                CreatePrimitive(PrimitiveType.Cylinder, vis, "Bar", new Vector3(bx, 0.7f, -1.45f), new Vector3(0.11f, 0.7f, 0.11f), bars);
            }
            for (int i = 0; i < 6; i++)   // and toward the road
                CreatePrimitive(PrimitiveType.Cylinder, vis, "Bar", new Vector3(1.0f, 0.7f, -1.3f + i * 0.5f), new Vector3(0.11f, 0.7f, 0.11f), bars);
            CreateBox(vis, "Rail", new Vector3(-0.45f, 1.42f, -1.45f), new Vector3(3.1f, 0.14f, 0.14f), bars);
            CreateBox(vis, "RailSide", new Vector3(1.0f, 1.42f, 0f), new Vector3(0.14f, 0.14f, 2.8f), bars);

            bool gen = allyGenTemplate != null;
            var giant = Instantiate(gen ? allyGenTemplate : unitTemplate, vis);
            giant.transform.localPosition = new Vector3(-0.5f, 0f, 0.2f);
            giant.transform.localRotation = Quaternion.Euler(0f, 160f, 0f);
            giant.transform.localScale = Vector3.one * (gen ? 1f : UNIT_SCALE * ALLY_SCALE);
            giant.SetActive(true);
            var giantRig = giant.GetComponent<ArmyRigBase>();
            if (giantRig != null) giantRig.helpPose = true;                 // waving HELP! behind the fence
            if (!gen) TintMaterials(giant, new Color(0.45f, 0.7f, 1f));   // blue giant like the reference
            ArmyRunFX.AddBlobShadow(go.transform, 3.2f);
            var help = CreateLabel(go.transform, "HELP!", new Vector3(0.9f, 4.4f, 0f), 15f, new Color(1f, 0.88f, 0.2f), new Color(0.35f, 0.15f, 0f));
            var label = CreateLabel(go.transform, hp.ToString(), new Vector3(-0.4f, 3.6f, -0.8f), 10f, Color.white, new Color(0.05f, 0.2f, 0.45f));
            var barRends = new System.Collections.Generic.List<Renderer>();
            foreach (var r in vis.GetComponentsInChildren<MeshRenderer>(true))
                if (!r.transform.IsChildOf(giant.transform)) barRends.Add(r);
            var t = new Target
            {
                kind = Kind.Cage, tr = go.transform, visual = vis, halfWidth = 1.3f, depth = 1.1f, hp = hp, maxHp = hp, label = label,
                freesGiant = true, helpLabel = help, rends = barRends.ToArray(), coins = 10, allyLife = allyLife
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
        /// <summary>
        /// Spiked roller on ONE half of the road: side -1 = left half, +1 = right half (over the +1 strip, so it
        /// forces a choice). It slides ±0.35 m and spins inside its half; can't be shot — move to the other half.
        /// </summary>
        private void AddRoller(float z, int side, float speed, float phase)
        {
            const float halfLen = 1.45f;
            float center = side * 1.95f;
            var go = NewOnTrack("SpikedRoller", 0f, z);
            // warning stripes + a red danger glow only on the roller's half
            Material glow = FlatMat(new Color(1f, 0.15f, 0.1f, 0.28f));
            CreateBox(go.transform, "DangerZone", new Vector3(center, 0.012f, 0f), new Vector3(3.5f, 0.01f, 2.6f), glow);
            for (int i = 0; i < 5; i++)
                CreateBox(go.transform, "Warn", new Vector3(center - 1.6f + i * 0.8f, 0.02f, -1.6f), new Vector3(0.42f, 0.02f, 0.42f), i % 2 == 0 ? s_hazard : s_band);
            var arm = NewChild(go.transform, "Arm");
            arm.localPosition = new Vector3(center, 0f, 0f);
            var log = NewChild(arm, "Log");
            log.localPosition = new Vector3(0f, 0.7f, 0f);
            log.localRotation = Quaternion.Euler(0f, 0f, 90f);      // cylinder axis → world X
            CreatePrimitive(PrimitiveType.Cylinder, log, "Core", Vector3.zero, new Vector3(1.0f, halfLen, 1.0f), s_log);
            for (int i = 0; i < 7; i++)
                for (int k = 0; k < 6; k++)
                {
                    float y = -halfLen + 0.25f + i * (halfLen * 2f - 0.5f) / 6f;
                    var spike = CreateBox(log, "Spike", Quaternion.Euler(0f, k * 60f + i * 20f, 0f) * new Vector3(0f, 0f, 0.55f) + Vector3.up * y,
                        new Vector3(0.12f, 0.12f, 0.45f), s_spike);
                    spike.transform.localRotation = Quaternion.Euler(0f, k * 60f + i * 20f, 0f);
                }
            CreatePrimitive(PrimitiveType.Cylinder, log, "CapA", new Vector3(0f, halfLen, 0f), new Vector3(1.15f, 0.06f, 1.15f), s_hazard);
            CreatePrimitive(PrimitiveType.Cylinder, log, "CapB", new Vector3(0f, -halfLen, 0f), new Vector3(1.15f, 0.06f, 1.15f), s_hazard);
            ArmyRunEnvironment.CombineChildren(log);
            ArmyRunFX.AddBlobShadow(arm, halfLen * 2f);
            rollers.Add(new Roller { tr = arm, log = log, halfLen = halfLen, speed = speed, phase = phase, center = center, amp = 0.35f });
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
