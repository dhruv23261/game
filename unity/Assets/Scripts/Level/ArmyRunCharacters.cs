using UnityEngine;

namespace MobControl.Level
{
    /// <summary>
    /// Hand-built Mob Control style big characters (no Meshy needed):
    ///   • BRUTE  — yellow skin, red war-paint stripes, red shorts, red mohawk      (stage-3 mini boss)
    ///   • GIANT  — blue skin, white spiky hair, grey shorts                         (HELP! cage ally)
    ///   • TYRANT — dark red skin, black shorts and boots, gold crown, bone horns    (final boss)
    ///
    /// Gorilla proportions: massive chest and shoulders, tiny head sunk between the traps, popeye forearms
    /// and huge fists hanging almost to the ground, short thick legs. Built from primitives on a small bone
    /// hierarchy (hips → legs, torso → head, arms → forearms) and animated by <see cref="ArmyBruteRig"/>.
    /// Faces +Z, feet on y = 0, exactly 'height' metres tall.
    /// </summary>
    public static class ArmyRunCharacters
    {
        public enum Style { Brute, Giant, Tyrant }

        private struct Palette
        {
            public Color skin, skinDark, shorts, belt, accent, hair, feet;
        }

        private static Palette PaletteOf(Style s)
        {
            switch (s)
            {
                case Style.Giant:
                    return new Palette
                    {
                        skin = new Color(0.30f, 0.60f, 1f), skinDark = new Color(0.18f, 0.40f, 0.82f),
                        shorts = new Color(0.40f, 0.43f, 0.50f), belt = new Color(0.22f, 0.24f, 0.30f),
                        accent = new Color(0.20f, 0.22f, 0.30f), hair = new Color(0.96f, 0.97f, 1f), feet = new Color(0.18f, 0.40f, 0.82f)
                    };
                case Style.Tyrant:
                    return new Palette
                    {
                        skin = new Color(0.78f, 0.16f, 0.12f), skinDark = new Color(0.52f, 0.08f, 0.07f),
                        shorts = new Color(0.12f, 0.12f, 0.15f), belt = new Color(1f, 0.78f, 0.2f),
                        accent = new Color(1f, 0.78f, 0.2f), hair = new Color(0.95f, 0.9f, 0.78f), feet = new Color(0.1f, 0.1f, 0.12f)
                    };
                default:
                    return new Palette
                    {
                        skin = new Color(1f, 0.80f, 0.16f), skinDark = new Color(0.92f, 0.62f, 0.08f),
                        shorts = new Color(0.86f, 0.13f, 0.10f), belt = new Color(0.55f, 0.06f, 0.05f),
                        accent = new Color(0.86f, 0.13f, 0.10f), hair = new Color(0.86f, 0.13f, 0.10f), feet = new Color(0.92f, 0.62f, 0.08f)
                    };
            }
        }

        /// <summary>Inactive template (child of parent) with an <see cref="ArmyBruteRig"/> on its root.</summary>
        public static GameObject BuildTemplate(Style style, float height, Transform parent)
        {
            var p = PaletteOf(style);
            Material skin = ArmyRunEnvironment.LitMat("Chr_Skin", p.skin, 0.45f);
            Material skinDark = ArmyRunEnvironment.LitMat("Chr_SkinDark", p.skinDark, 0.35f);
            Material shorts = ArmyRunEnvironment.LitMat("Chr_Shorts", p.shorts, 0.25f);
            Material belt = ArmyRunEnvironment.LitMat("Chr_Belt", p.belt, 0.5f);
            Material accent = ArmyRunEnvironment.LitMat("Chr_Accent", p.accent, style == Style.Tyrant ? 0.8f : 0.35f);
            Material hair = ArmyRunEnvironment.LitMat("Chr_Hair", p.hair, 0.3f);
            Material feet = ArmyRunEnvironment.LitMat("Chr_Feet", p.feet, 0.3f);
            Material white = ArmyRunEnvironment.LitMat("Chr_EyeWhite", Color.white, 0.6f);
            Material black = ArmyRunEnvironment.LitMat("Chr_Black", new Color(0.05f, 0.04f, 0.05f), 0.3f);
            Material mouth = ArmyRunEnvironment.LitMat("Chr_Mouth", new Color(0.35f, 0.05f, 0.06f), 0.2f);

            var tmpl = new GameObject(style + "_Template");
            tmpl.SetActive(false);
            tmpl.transform.SetParent(parent, false);
            var body = Bone(tmpl.transform, "Body", Vector3.zero);
            var rig = tmpl.AddComponent<ArmyBruteRig>();

            // ── hips, shorts, legs ──
            var hips = Bone(body, "Hips", new Vector3(0f, 0.36f, 0f));
            P(hips, PrimitiveType.Sphere, new Vector3(0f, 0.02f, 0f), new Vector3(0.50f, 0.25f, 0.37f), shorts);
            P(hips, PrimitiveType.Cylinder, new Vector3(0f, 0.11f, 0.005f), new Vector3(0.47f, 0.028f, 0.35f), belt);
            if (style == Style.Tyrant)
                P(hips, PrimitiveType.Cube, new Vector3(0f, 0.11f, 0.17f), new Vector3(0.1f, 0.07f, 0.03f), accent);   // gold buckle
            Transform[] legs = new Transform[2];
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                var leg = Bone(hips, side == 0 ? "LegL" : "LegR", new Vector3(sx * 0.13f, 0f, 0f));
                P(leg, PrimitiveType.Sphere, new Vector3(0f, -0.03f, 0f), new Vector3(0.21f, 0.16f, 0.21f), shorts);   // short leg
                P(leg, PrimitiveType.Sphere, new Vector3(0f, -0.13f, 0f), new Vector3(0.18f, 0.2f, 0.18f), skin);      // thigh
                P(leg, PrimitiveType.Sphere, new Vector3(0f, -0.24f, 0.005f), new Vector3(0.15f, 0.17f, 0.15f), skin); // calf
                P(leg, PrimitiveType.Sphere, new Vector3(0f, -0.33f, 0.045f), new Vector3(0.16f, 0.085f, 0.25f), feet);
                legs[side] = leg;
            }

            // ── torso: belly, chest, pecs, traps, abs ──
            var torso = Bone(hips, "Torso", new Vector3(0f, 0.08f, 0f));
            P(torso, PrimitiveType.Sphere, new Vector3(0f, 0.09f, 0.02f), new Vector3(0.46f, 0.32f, 0.38f), skin);   // belly
            P(torso, PrimitiveType.Sphere, new Vector3(0f, 0.30f, 0f), new Vector3(0.68f, 0.38f, 0.45f), skin);      // chest
            P(torso, PrimitiveType.Sphere, new Vector3(0f, 0.27f, -0.06f), new Vector3(0.62f, 0.34f, 0.38f), skinDark); // back
            for (int side = -1; side <= 1; side += 2)
                P(torso, PrimitiveType.Sphere, new Vector3(side * 0.12f, 0.31f, 0.16f), new Vector3(0.27f, 0.19f, 0.14f), skin); // pecs
            P(torso, PrimitiveType.Sphere, new Vector3(0f, 0.46f, -0.03f), new Vector3(0.38f, 0.17f, 0.28f), skin);  // traps
            for (int r = 0; r < 3; r++)
                for (int side = -1; side <= 1; side += 2)
                    P(torso, PrimitiveType.Sphere, new Vector3(side * 0.05f, 0.16f - r * 0.06f, 0.19f - r * 0.005f), new Vector3(0.08f, 0.05f, 0.03f), skinDark); // abs
            if (style == Style.Brute)
                for (int side = -1; side <= 1; side += 2)
                    for (int k = 0; k < 2; k++)
                        P(torso, PrimitiveType.Cube, new Vector3(side * 0.13f, 0.35f - k * 0.06f, 0.225f), new Vector3(0.17f, 0.028f, 0.03f), accent,
                          new Vector3(0f, 0f, side * 22f));                                                  // war paint

            // ── head ──
            var head = Bone(torso, "Head", new Vector3(0f, 0.47f, 0.07f));
            P(head, PrimitiveType.Sphere, new Vector3(0f, 0.08f, 0f), new Vector3(0.2f, 0.21f, 0.2f), skin);
            P(head, PrimitiveType.Sphere, new Vector3(0f, 0.015f, 0.04f), new Vector3(0.17f, 0.1f, 0.15f), skin);    // big jaw
            for (int side = -1; side <= 1; side += 2)
            {
                P(head, PrimitiveType.Sphere, new Vector3(side * 0.045f, 0.085f, 0.085f), new Vector3(0.06f, 0.065f, 0.035f), white);
                P(head, PrimitiveType.Sphere, new Vector3(side * 0.04f, 0.083f, 0.1f), new Vector3(0.03f, 0.036f, 0.02f), black);
                P(head, PrimitiveType.Cube, new Vector3(side * 0.047f, 0.125f, 0.095f), new Vector3(0.075f, 0.02f, 0.025f), black,
                  new Vector3(0f, 0f, side * 22f));                                                          // angry brows
                P(head, PrimitiveType.Sphere, new Vector3(side * 0.1f, 0.08f, 0f), new Vector3(0.035f, 0.05f, 0.03f), skinDark); // ears
            }
            P(head, PrimitiveType.Cube, new Vector3(0f, 0.025f, 0.11f), new Vector3(0.075f, 0.022f, 0.02f), mouth);
            P(head, PrimitiveType.Cube, new Vector3(0f, 0.034f, 0.116f), new Vector3(0.06f, 0.008f, 0.01f), white);  // teeth
            BuildHair(head, style, hair, accent);

            // ── arms ──
            Transform[] arms = new Transform[2], fores = new Transform[2];
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                var arm = Bone(torso, side == 0 ? "ArmL" : "ArmR", new Vector3(sx * 0.35f, 0.37f, 0f));
                P(arm, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.26f, 0.25f, 0.26f), skin);                  // deltoid
                P(arm, PrimitiveType.Sphere, new Vector3(0f, -0.14f, 0f), new Vector3(0.21f, 0.3f, 0.21f), skin);    // upper arm
                P(arm, PrimitiveType.Sphere, new Vector3(0f, -0.12f, 0.05f), new Vector3(0.17f, 0.17f, 0.15f), skin); // biceps
                if (style == Style.Brute)
                {
                    P(arm, PrimitiveType.Cylinder, new Vector3(0f, -0.1f, 0f), new Vector3(0.222f, 0.012f, 0.222f), accent);
                    P(arm, PrimitiveType.Cylinder, new Vector3(0f, -0.16f, 0f), new Vector3(0.222f, 0.012f, 0.222f), accent);
                }
                var fore = Bone(arm, "Fore", new Vector3(0f, -0.27f, 0f));
                P(fore, PrimitiveType.Sphere, new Vector3(0f, -0.13f, 0.005f), new Vector3(0.24f, 0.31f, 0.24f), skin); // popeye forearm
                P(fore, PrimitiveType.Cylinder, new Vector3(0f, -0.255f, 0f), new Vector3(0.205f, 0.03f, 0.205f), accent); // wristband
                P(fore, PrimitiveType.Sphere, new Vector3(0f, -0.36f, 0.01f), new Vector3(0.28f, 0.25f, 0.28f), skin);  // FIST
                P(fore, PrimitiveType.Sphere, new Vector3(0f, -0.355f, 0.085f), new Vector3(0.22f, 0.12f, 0.14f), skinDark); // knuckles
                arms[side] = arm;
                fores[side] = fore;
            }

            rig.hips = hips; rig.torso = torso; rig.head = head;
            rig.legL = legs[0]; rig.legR = legs[1];
            rig.armL = arms[0]; rig.armR = arms[1];
            rig.foreL = fores[0]; rig.foreR = fores[1];
            rig.SetRest();

            // ~50 primitives → one mesh per material per bone (bones still move independently)
            foreach (var bone in new[] { hips, legs[0], legs[1], torso, head, arms[0], arms[1], fores[0], fores[1] })
                MergeParts(bone);

            // normalise: feet on y = 0, exactly 'height' tall, centred
            tmpl.SetActive(true);
            Bounds b = ArmyRunModels.MeasureBounds(body.gameObject);
            tmpl.SetActive(false);
            if (b.size.y > 1e-4f)
            {
                float s = height / b.size.y;
                body.localScale = Vector3.one * s;
                Vector3 c = tmpl.transform.InverseTransformPoint(b.center);
                Vector3 min = tmpl.transform.InverseTransformPoint(b.min);
                body.localPosition = new Vector3(-c.x * s, -min.y * s, 0f);
            }
            return tmpl;
        }

        private static void BuildHair(Transform head, Style style, Material hair, Material accent)
        {
            switch (style)
            {
                case Style.Brute:     // red mohawk
                    for (int i = 0; i < 5; i++)
                        P(head, PrimitiveType.Cube, new Vector3(0f, 0.19f - Mathf.Abs(i - 1.5f) * 0.008f, 0.07f - i * 0.04f),
                          new Vector3(0.035f, 0.08f, 0.05f), hair, new Vector3(-20f + i * 12f, 0f, 0f));
                    break;
                case Style.Giant:     // white spiky hair swept back
                    for (int i = 0; i < 7; i++)
                    {
                        float a = (i - 3) * 22f;
                        Vector3 dir = Quaternion.Euler(-35f - Mathf.Abs(i - 3) * 6f, a, 0f) * Vector3.up;
                        P(head, PrimitiveType.Sphere, new Vector3(0f, 0.15f, -0.01f) + dir * 0.05f, new Vector3(0.05f, 0.16f, 0.05f), hair,
                          Quaternion.LookRotation(Vector3.forward, dir).eulerAngles);
                    }
                    P(head, PrimitiveType.Sphere, new Vector3(0f, 0.15f, 0f), new Vector3(0.19f, 0.09f, 0.19f), hair);
                    break;
                case Style.Tyrant:    // gold crown + bone horns
                    P(head, PrimitiveType.Cylinder, new Vector3(0f, 0.17f, 0f), new Vector3(0.17f, 0.025f, 0.17f), accent);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 60f * Mathf.Deg2Rad;
                        P(head, PrimitiveType.Cube, new Vector3(Mathf.Sin(a) * 0.075f, 0.215f, Mathf.Cos(a) * 0.075f), new Vector3(0.03f, 0.06f, 0.03f), accent,
                          new Vector3(0f, i * 60f, 45f));
                    }
                    for (int side = -1; side <= 1; side += 2)
                    {
                        P(head, PrimitiveType.Sphere, new Vector3(side * 0.1f, 0.15f, 0.01f), new Vector3(0.045f, 0.11f, 0.045f), hair, new Vector3(0f, 0f, -side * 40f));
                        P(head, PrimitiveType.Sphere, new Vector3(side * 0.14f, 0.21f, 0.02f), new Vector3(0.03f, 0.08f, 0.03f), hair, new Vector3(0f, 0f, side * 10f));
                    }
                    break;
            }
        }

        /// <summary>Combines the bone's direct primitive children into one mesh per material, parented to the bone.</summary>
        private static void MergeParts(Transform bone)
        {
            var groups = new System.Collections.Generic.Dictionary<Material, System.Collections.Generic.List<CombineInstance>>();
            var parts = new System.Collections.Generic.List<GameObject>();
            Matrix4x4 toBone = bone.worldToLocalMatrix;
            foreach (Transform child in bone)
            {
                var mf = child.GetComponent<MeshFilter>();
                var mr = child.GetComponent<MeshRenderer>();
                if (mf == null || mr == null || mf.sharedMesh == null || child.childCount > 0) continue;
                if (!groups.TryGetValue(mr.sharedMaterial, out var list)) groups[mr.sharedMaterial] = list = new System.Collections.Generic.List<CombineInstance>();
                list.Add(new CombineInstance { mesh = mf.sharedMesh, transform = toBone * child.localToWorldMatrix });
                parts.Add(child.gameObject);
            }
            foreach (var go in parts) Object.DestroyImmediate(go);
            foreach (var kv in groups)
            {
                var mesh = new Mesh { name = bone.name + "_" + kv.Key.name };
                mesh.CombineMeshes(kv.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                var part = new GameObject("Mesh_" + kv.Key.name);
                part.transform.SetParent(bone, false);
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = part.AddComponent<MeshRenderer>();
                r.sharedMaterial = kv.Key;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static Transform Bone(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        private static void P(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());   // template is instantiated this same frame
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    /// <summary>
    /// What Level 1 needs from any big-character rig (Brute, Giant, Master): walk flags + punch / slam / die.
    /// The level only talks to this, so a different model rig can replace <see cref="ArmyBruteRig"/> later.
    /// </summary>
    public abstract class ArmyRigBase : MonoBehaviour
    {
        public bool forceWalk;     // walk even when not moving in its parent (the ally keeps pace with the army)
        public bool helpPose;      // both arms up, waving (HELP! cage)
        public abstract void Punch();
        public abstract void Slam(float windup);
        public abstract void Die();
    }

    /// <summary>
    /// Procedural animation for <see cref="ArmyRunCharacters"/>: heavy gorilla walk (auto from movement or
    /// forceWalk), breathing idle, alternating punches, two-handed ground slam (wind-up lasts as long as
    /// the slam telegraph), HELP! waving and a fall-over death. Only bones are moved, never the root.
    /// </summary>
    public class ArmyBruteRig : ArmyRigBase
    {
        public Transform hips, torso, head, armL, armR, foreL, foreR, legL, legR;

        [SerializeField] private float hipsY;
        private float phase, walk, punchL = -1f, punchR = -1f, slamT = -1f, slamDur = 1f, dieT = -1f;
        private bool nextLeft;
        private Vector3 lastOwn, lastParent;
        private bool hasLast;

        private const float PUNCH_TIME = 0.42f, SLAM_STRIKE = 0.18f, SLAM_HOLD = 0.35f;

        public void SetRest() { if (hips != null) hipsY = hips.localPosition.y; }
        public override void Punch() { if (nextLeft) punchL = 0f; else punchR = 0f; nextLeft = !nextLeft; }
        public override void Slam(float windup) { slamT = 0f; slamDur = Mathf.Max(0.3f, windup); }
        public override void Die() { dieT = 0f; }

        private void OnEnable() { hasLast = false; phase = Random.value * 6f; }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || hips == null) return;

            // ── how fast are we walking (own or parent movement, excluding the scrolling world) ──
            Vector3 own = transform.localPosition, par = transform.parent != null ? transform.parent.localPosition : Vector3.zero;
            float speed = hasLast ? ((own - lastOwn).magnitude + (par - lastParent).magnitude) / dt : 0f;
            lastOwn = own; lastParent = par; hasLast = true;
            float want = (forceWalk || speed > 0.15f) && dieT < 0f ? 1f : 0f;
            walk = Mathf.MoveTowards(walk, want, dt * 4f);
            phase += dt * 7.5f * Mathf.Max(walk, 0.15f);

            float t = Time.time;
            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
            float breathe = Mathf.Sin(t * 2.3f);

            // ── legs / hips ──
            legL.localRotation = Quaternion.Euler(-s * 34f * walk, 0f, 0f);
            legR.localRotation = Quaternion.Euler(s * 34f * walk, 0f, 0f);
            Vector3 hp = hips.localPosition;
            hp.y = hipsY + Mathf.Abs(c) * 0.035f * walk - 0.01f * (1f - walk) * (breathe * 0.5f + 0.5f);
            float torsoPitch = 8f * walk + breathe * 1.5f, torsoYaw = s * 9f * walk, torsoRoll = s * 4f * walk;

            // ── arms: swing opposite the legs, slight outward roll ──
            float armSwing = s * 30f * walk;
            Quaternion aL = Quaternion.Euler(armSwing + breathe * 2f, 0f, -14f - breathe * 1.5f);
            Quaternion aR = Quaternion.Euler(-armSwing + breathe * 2f, 0f, 14f + breathe * 1.5f);
            Quaternion fL = Quaternion.Euler(-20f - Mathf.Max(0f, -s) * 25f * walk, 0f, 0f);
            Quaternion fR = Quaternion.Euler(-20f - Mathf.Max(0f, s) * 25f * walk, 0f, 0f);

            if (helpPose)   // HELP! — both arms up, waving
            {
                float w = Mathf.Sin(t * 11f) * 18f;
                aL = Quaternion.Euler(-160f + w, 0f, -28f);
                aR = Quaternion.Euler(-160f - w, 0f, 28f);
                fL = fR = Quaternion.Euler(-25f, 0f, 0f);
                torsoPitch = -6f; torsoYaw = Mathf.Sin(t * 5f) * 8f;
            }

            // ── punches ──
            if (punchL >= 0f) { punchL += dt; PunchPose(punchL, ref aL, ref fL, -1f, ref torsoYaw); if (punchL > PUNCH_TIME) punchL = -1f; }
            if (punchR >= 0f) { punchR += dt; PunchPose(punchR, ref aR, ref fR, 1f, ref torsoYaw); if (punchR > PUNCH_TIME) punchR = -1f; }

            // ── ground slam: arms rise during the telegraph, smash down when it lands ──
            if (slamT >= 0f)
            {
                slamT += dt;
                float up, pitch;
                if (slamT < slamDur) { float k = Mathf.SmoothStep(0f, 1f, slamT / slamDur); up = Mathf.Lerp(0f, -175f, k); pitch = Mathf.Lerp(0f, -14f, k); }
                else if (slamT < slamDur + SLAM_STRIKE) { float k = (slamT - slamDur) / SLAM_STRIKE; up = Mathf.Lerp(-175f, -45f, k * k); pitch = Mathf.Lerp(-14f, 32f, k); hp.y -= 0.06f * k; }
                else if (slamT < slamDur + SLAM_STRIKE + SLAM_HOLD) { up = -45f; pitch = 32f; hp.y -= 0.06f; }
                else { slamT = -1f; up = 0f; pitch = 0f; }
                aL = Quaternion.Euler(up, 0f, -18f);
                aR = Quaternion.Euler(up, 0f, 18f);
                fL = fR = Quaternion.Euler(-10f, 0f, 0f);
                torsoPitch += pitch; torsoYaw = 0f;
            }

            // ── death: topple backwards ──
            if (dieT >= 0f)
            {
                dieT += dt;
                float k = Mathf.SmoothStep(0f, 1f, dieT / 0.7f);
                hips.localRotation = Quaternion.Euler(-82f * k, 0f, 0f);
                hp.y = Mathf.Lerp(hipsY, hipsY * 0.45f, k);
                aL = Quaternion.Euler(-60f * k, 0f, -50f * k); aR = Quaternion.Euler(-60f * k, 0f, 50f * k);
            }

            hips.localPosition = hp;
            torso.localRotation = Quaternion.Euler(torsoPitch, torsoYaw, torsoRoll);
            if (head != null) head.localRotation = Quaternion.Euler(-torsoPitch * 0.5f, -torsoYaw * 0.5f, Mathf.Sin(t * 1.7f) * 2f);
            armL.localRotation = aL; armR.localRotation = aR;
            foreL.localRotation = fL; foreR.localRotation = fR;
        }

        /// <summary>Wind-up (arm back, elbow bent) → straight jab forward → recover.</summary>
        private static void PunchPose(float time, ref Quaternion arm, ref Quaternion fore, float side, ref float torsoYaw)
        {
            float k = time / PUNCH_TIME;
            float a, f;
            if (k < 0.3f) { float q = k / 0.3f; a = Mathf.Lerp(0f, 35f, q); f = Mathf.Lerp(-20f, -105f, q); }
            else if (k < 0.55f) { float q = (k - 0.3f) / 0.25f; q = 1f - (1f - q) * (1f - q); a = Mathf.Lerp(35f, -100f, q); f = Mathf.Lerp(-105f, -5f, q); }
            else { float q = (k - 0.55f) / 0.45f; a = Mathf.Lerp(-100f, 0f, q); f = Mathf.Lerp(-5f, -20f, q); }
            arm = Quaternion.Euler(a, 0f, side * 10f);
            fore = Quaternion.Euler(f, 0f, 0f);
            torsoYaw += side * -14f * Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);
        }
    }
}
