using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MobControl.Combat;
using MobControl.UI;
using MobControl.Core;

namespace MobControl.Level
{
    /// <summary>
    /// BUILDERS: materials, primitives, labels, floating text, hit punches.
    /// </summary>
    public partial class ArmyRunLevel
    {
        // ═══════════════════════════════════════════ small visuals ══

        private void UpdatePunches(float dt)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t.flashT > 0f)
                {
                    t.flashT -= dt;
                    if (t.flashT <= 0f) ClearTint(t.rends);
                }
                if (t.punch <= 0f || t.visual == null) continue;
                t.punch = Mathf.Max(0f, t.punch - dt * 8f);
                t.visual.localScale = t.baseScale * (1f + 0.12f * t.punch);
            }
            if (countLabelPunch > 0f) countLabelPunch = Mathf.Max(0f, countLabelPunch - dt * 5f);
        }

        private readonly Stack<TextMeshPro> floatPool = new Stack<TextMeshPro>();

        private void SpawnFloatText(Vector3 pos, string text, Color color, float size = 8f)
        {
            TextMeshPro tmp;
            if (floatPool.Count > 0)
            {
                tmp = floatPool.Pop();
                tmp.gameObject.SetActive(true);
                tmp.transform.position = pos;
                tmp.text = text;
                tmp.fontSize = size;
                tmp.color = color;
            }
            else tmp = CreateLabel(root, text, pos, size, color, new Color(0f, 0f, 0f, 0.85f));
            floatTexts.Add(new FloatText { tmp = tmp, life = 1.1f });
        }

        private void UpdateFloatTexts(float dt)
        {
            for (int i = floatTexts.Count - 1; i >= 0; i--)
            {
                var f = floatTexts[i];
                f.life -= dt;
                if (f.tmp == null || f.life <= 0f)
                {
                    if (f.tmp != null) { f.tmp.gameObject.SetActive(false); floatPool.Push(f.tmp); }   // pooled
                    floatTexts.RemoveAt(i);
                    continue;
                }
                f.tmp.transform.position += Vector3.up * (2.4f * dt);
                Color c = f.tmp.color; c.a = Mathf.Clamp01(f.life * 2.5f); f.tmp.color = c;
                f.tmp.transform.localScale = Vector3.one * (1f + Mathf.Clamp01((f.life - 0.9f) * 5f) * 0.45f);
            }
        }

        // ═══════════════════════════════════════════ Builders ══

        private static void EnsureMaterials()
        {
            if (s_font == null)
            {
                s_font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Fredoka-Bold SDF");
                // dynamic atlas: create every glyph we print up front, so no label ever samples a missing glyph
                if (s_font != null) s_font.TryAddCharacters("0123456789+-x×!?/.:, ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
            }
            if (s_wood != null) return;
            s_wood = ArmyRunEnvironment.LitMat("Wood", new Color(0.64f, 0.40f, 0.18f), 0.15f);
            s_band = ArmyRunEnvironment.LitMat("Band", new Color(0.22f, 0.22f, 0.26f), 0.6f);
            s_redBarrel = ArmyRunEnvironment.LitMat("RedBarrel", new Color(0.86f, 0.12f, 0.1f), 0.55f);
            s_hazard = ArmyRunEnvironment.LitMat("Hazard", new Color(1f, 0.82f, 0.1f), 0.4f);
            s_ice = ArmyRunEnvironment.LitMat("Ice", new Color(0.45f, 0.78f, 1f), 0.92f);
            s_iceRib = ArmyRunEnvironment.LitMat("IceRib", new Color(0.3f, 0.6f, 0.92f), 0.6f);
            s_frame = ArmyRunEnvironment.LitMat("GateFrame", new Color(0.88f, 0.94f, 1f), 0.65f);
            s_steel = ArmyRunEnvironment.LitMat("Steel", new Color(0.5f, 0.53f, 0.58f), 0.7f);
            s_sand = ArmyRunEnvironment.LitMat("Sandbag", new Color(0.78f, 0.68f, 0.46f), 0.05f);
            s_log = ArmyRunEnvironment.LitMat("Log", new Color(0.45f, 0.3f, 0.17f), 0.1f);
            s_spike = ArmyRunEnvironment.LitMat("Spike", new Color(0.75f, 0.77f, 0.8f), 0.8f);
            s_rune = FlatMat(new Color(0.3f, 1f, 0.4f));
            s_tracer = GlowMat(new Color(1f, 0.85f, 0.25f), 2.2f);       // additive → bright glowing bullet stream
            s_enemyTracer = GlowMat(new Color(1f, 0.25f, 0.12f), 2f);
            s_gateBlue = FlatMat(new Color(0.25f, 0.6f, 1f, 0.5f));
            s_gateRed = FlatMat(new Color(1f, 0.18f, 0.18f, 0.5f));
            s_warn = FlatMat(new Color(1f, 0.1f, 0.05f, 0.45f));
        }

        /// <summary>Additive glow (the project's VFX_FireAdditive shader, falls back to unlit colour).</summary>
        private static Material GlowMat(Color c, float glow)
        {
            Shader sh = Shader.Find("ZombieSquad/VFX_FireAdditive");
            if (sh == null) return FlatMat(c);
            var m = new Material(sh) { name = "Glow" };
            m.SetColor("_TintColor", c);
            m.SetFloat("_GlowIntensity", glow);
            return m;
        }

        /// <summary>Unlit, alpha-capable colour (Sprites/Default is always in builds).</summary>
        private static Material FlatMat(Color c)
        {
            int key = ((int)(c.r * 255) << 24) ^ ((int)(c.g * 255) << 16) ^ ((int)(c.b * 255) << 8) ^ (int)(c.a * 255);
            if (s_flatMats.TryGetValue(key, out var cached) && cached != null) return cached;
            var m = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color")) { color = c };
            s_flatMats[key] = m;
            return m;
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat)
            => CreatePrimitive(PrimitiveType.Cube, parent, name, localPos, size, mat);

        private static GameObject CreatePrimitive(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            RemoveCollider(go);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            EnsureWhiteVertexColors(go);
            return go;
        }

        private static readonly Dictionary<Mesh, Mesh> s_whiteMeshes = new Dictionary<Mesh, Mesh>();

        /// <summary>Sprites/Default multiplies by vertex colour; built-in primitives have none, so give them white ones.</summary>
        private static void EnsureWhiteVertexColors(GameObject go)
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;
            Mesh src = mf.sharedMesh;
            if (!s_whiteMeshes.TryGetValue(src, out Mesh white) || white == null)
            {
                white = Instantiate(src);
                white.name = src.name + "_White";
                var cols = new Color32[white.vertexCount];
                for (int i = 0; i < cols.Length; i++) cols[i] = new Color32(255, 255, 255, 255);
                white.colors32 = cols;
                s_whiteMeshes[src] = white;
            }
            mf.sharedMesh = white;
        }

        private static void RemoveCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
        }

        private static TextMeshPro CreateLabel(Transform parent, string text, Vector3 pos, float size, Color color, Color outline)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var tmp = go.AddComponent<TextMeshPro>();
            if (s_font != null) tmp.font = s_font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(12f, 3f);
            tmp.color = color;
            Material m = LabelMaterial(outline);
            if (m != null) tmp.fontSharedMaterial = m;               // shared: hundreds of labels, one material each colour
            else { tmp.outlineWidth = 0.28f; tmp.outlineColor = outline; }
            return tmp;
        }

        private static readonly Dictionary<int, Material> s_labelMats = new Dictionary<int, Material>();
        private static Texture s_labelAtlas;

        /// <summary>The dynamic atlas can be rebuilt/resized: point every shared label material at the live one.</summary>
        private static void SyncLabelAtlas()
        {
            if (s_font == null) return;
            Texture atlas = s_font.atlasTexture;
            if (atlas == s_labelAtlas) return;
            s_labelAtlas = atlas;
            foreach (var m in s_labelMats.Values) if (m != null) m.SetTexture(ShaderUtilities.ID_MainTex, atlas);
        }

        /// <summary>Bold SDF text with a thick outline and a soft drop shadow (Mob Control sign style).</summary>
        private static Material LabelMaterial(Color outline)
        {
            if (s_font == null || s_font.material == null) return null;
            int key = ((int)(outline.r * 255) << 16) ^ ((int)(outline.g * 255) << 8) ^ (int)(outline.b * 255);
            if (s_labelMats.TryGetValue(key, out var cached) && cached != null) return cached;
            var m = new Material(s_font.material) { name = "Label_" + key };
            m.SetTexture(ShaderUtilities.ID_MainTex, s_font.atlasTexture);
            m.EnableKeyword(ShaderUtilities.Keyword_Outline);
            m.SetFloat(ShaderUtilities.ID_FaceDilate, 0.12f);
            m.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.26f);
            m.SetColor(ShaderUtilities.ID_OutlineColor, outline);
            if (m.HasProperty(ShaderUtilities.ID_UnderlayColor))
            {
                m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.45f));
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.35f);
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.45f);
                m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.3f);
            }
            s_labelMats[key] = m;
            return m;
        }
    }
}
