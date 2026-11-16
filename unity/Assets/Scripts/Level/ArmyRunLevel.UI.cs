using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MobControl.Level
{
    /// <summary>
    /// Level-1 overlay UI (sits on top of the normal HUD, never blocks touches):
    ///  • Avatar + ARMY count pill (top-left) and COIN pill (top-right); coins fly into it from the world.
    ///  • Stage banner — "STAGE 2 / 6 · AMBUSH" + a one-line hint. Pops in, holds 1.6 s, fades out.
    ///  • Boss HP bar — name, red bar, "6230 / 9000". Shown during Brute / Master fights.
    /// </summary>
    public partial class ArmyRunLevel
    {
        private Canvas uiCanvas;
        private RectTransform bannerRt;
        private CanvasGroup bannerGroup;
        private TextMeshProUGUI bannerSmall, bannerBig, bannerHint;
        private float bannerT = -1f;

        private GameObject bossBar;
        private RectTransform bossFill, bossFillLag;
        private TextMeshProUGUI bossName, bossHpText;
        private float bossFrac = 1f, bossLagFrac = 1f;

        private const float BANNER_IN = 0.25f, BANNER_HOLD = 1.6f, BANNER_OUT = 0.4f;

        private void BuildUI()
        {
            var go = new GameObject("ArmyRun_UI");
            go.transform.SetParent(root, false);
            uiCanvas = go.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 40;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            // ── Stage banner ──
            bannerRt = NewRect("StageBanner", go.transform, new Vector2(0.5f, 1f), new Vector2(0f, -600f), new Vector2(980f, 230f));
            bannerGroup = bannerRt.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.blocksRaycasts = false;
            bannerGroup.interactable = false;
            var bg = NewRect("Ribbon", bannerRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 170f));
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.08f, 0.2f, 0.72f);
            bgImg.raycastTarget = false;
            var stripeTop = NewRect("StripeTop", bg, new Vector2(0.5f, 1f), new Vector2(0f, -3f), new Vector2(980f, 6f));
            AddImage(stripeTop, new Color(1f, 0.8f, 0.25f, 1f));
            var stripeBot = NewRect("StripeBot", bg, new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(980f, 6f));
            AddImage(stripeBot, new Color(1f, 0.8f, 0.25f, 1f));
            bannerSmall = NewText("Small", bannerRt, new Vector2(0f, 62f), 44f, new Color(1f, 0.82f, 0.3f));
            bannerBig = NewText("Big", bannerRt, new Vector2(0f, 0f), 96f, Color.white);
            bannerHint = NewText("Hint", bannerRt, new Vector2(0f, -100f), 40f, new Color(0.85f, 0.93f, 1f));
            bannerGroup.alpha = 0f;

            // ── Boss bar ──
            var barRt = NewRect("BossBar", go.transform, new Vector2(0.5f, 1f), new Vector2(0f, -390f), new Vector2(880f, 110f));
            bossBar = barRt.gameObject;
            bossName = NewText("Name", barRt, new Vector2(0f, 30f), 48f, new Color(1f, 0.45f, 0.3f));
            var frame = NewRect("Frame", barRt, new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(860f, 52f));
            AddImage(frame, new Color(0.08f, 0.02f, 0.02f, 0.9f));
            var inner = NewRect("Inner", frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(844f, 38f));
            AddImage(inner, new Color(0.25f, 0.05f, 0.05f, 1f));
            bossFillLag = FillRect("Lag", inner, new Color(1f, 0.85f, 0.4f, 1f));
            bossFill = FillRect("Fill", inner, new Color(0.92f, 0.16f, 0.12f, 1f));
            bossHpText = NewText("HP", frame, Vector2.zero, 32f, Color.white);
            bossBar.SetActive(false);

            BuildHudPills(go.transform);
            BuildVignette(go.transform);
        }

        private void ResetUI()
        {
            armyPill = null; coinPill = null; armyText = null; coinText = null; vignette = null; vignetteAlpha = 0f;
            flyingCoins.Clear(); coinPool.Clear();
            shownCoins = 0f;
            uiCanvas = null; bannerRt = null; bannerGroup = null; bossBar = null;
            bannerT = -1f;
            masterSpawned = false;
            deathBig = null;
        }

        // ═════════════════════════════════════ Banner ══

        private void ShowBanner(string small, string big, string hint)
        {
            if (bannerRt == null) return;
            bannerSmall.text = small;
            bannerBig.text = big;
            bannerHint.text = hint;
            bannerT = 0f;
        }

        private void UpdateUI()
        {
            float udt = Time.unscaledDeltaTime;
            if (bannerRt != null && bannerT >= 0f)
            {
                bannerT += udt;
                float a, s;
                if (bannerT < BANNER_IN) { float k = bannerT / BANNER_IN; a = k; s = Mathf.LerpUnclamped(0.5f, 1f, EaseOutBack(k)); }
                else if (bannerT < BANNER_IN + BANNER_HOLD) { a = 1f; s = 1f; }
                else { float k = Mathf.Clamp01((bannerT - BANNER_IN - BANNER_HOLD) / BANNER_OUT); a = 1f - k; s = 1f + 0.08f * k; }
                bannerGroup.alpha = a;
                bannerRt.localScale = Vector3.one * s;
                if (bannerT > BANNER_IN + BANNER_HOLD + BANNER_OUT) { bannerT = -1f; bannerGroup.alpha = 0f; }
            }

            UpdateHudPills(udt);
            UpdateVignette(udt);
            UpdateFlyingCoins(udt);

            if (bossBar != null && bossBar.activeSelf)
            {
                bossLagFrac = Mathf.MoveTowards(bossLagFrac, bossFrac, udt * 0.6f);
                SetFill(bossFill, bossFrac);
                SetFill(bossFillLag, Mathf.Max(bossFrac, bossLagFrac));
            }
        }

        // ═════════════════════════════════════ Boss bar ══

        private void ShowBossBar(string name, float hp, float max)
        {
            if (bossBar == null) return;
            bossBar.SetActive(true);
            bossName.text = name;
            bossFrac = bossLagFrac = Mathf.Clamp01(hp / Mathf.Max(1f, max));
            UpdateBossBar(hp, max);
        }

        private void UpdateBossBar(float hp, float max)
        {
            if (bossBar == null || !bossBar.activeSelf) return;
            bossFrac = Mathf.Clamp01(hp / Mathf.Max(1f, max));
            bossHpText.text = $"{Mathf.CeilToInt(hp)} / {Mathf.CeilToInt(max)}";
        }

        private void HideBossBar()
        {
            if (bossBar != null) bossBar.SetActive(false);
        }


        // ═════════════════════════════════════ HUD pills: avatar + army, coins ══
        //  Sits just under the existing top bar (pause · level · progress), portrait-safe.

        private RectTransform armyPill, coinPill, coinIconRt;
        private TextMeshProUGUI armyText, coinText;
        private int shownArmy = -1;
        private float shownCoins, armyPunch, coinPunch;
        private static Sprite s_coinSprite, s_circleSprite;

        private class FlyingCoin { public RectTransform rt; public Vector2 from, to; public float t, dur; public int value; }
        private readonly List<FlyingCoin> flyingCoins = new List<FlyingCoin>();
        private readonly Stack<RectTransform> coinPool = new Stack<RectTransform>();
        private float coinFlyThrottle;

        private void BuildHudPills(Transform canvas)
        {
            Sprite pillSprite = LoadUISprite("UI/MenuHD/Menu_Hud_Pill");
            Sprite portrait = LoadUISprite("UI/MenuHD/Menu_Portrait_Soldier") ?? LoadUISprite("UI/Hero_Soldier_Icon");
            if (s_coinSprite == null) s_coinSprite = MakeCoinSprite();
            if (s_circleSprite == null) s_circleSprite = MakeCircleSprite();

            // ── Avatar + ARMY count (top-left) ──
            armyPill = NewRect("ArmyPill", canvas, new Vector2(0f, 1f), new Vector2(250f, -235f), new Vector2(330f, 92f));
            var pillImg = armyPill.gameObject.AddComponent<Image>();
            pillImg.raycastTarget = false;
            if (pillSprite != null) { pillImg.sprite = pillSprite; pillImg.type = Image.Type.Sliced; pillImg.color = Color.white; }
            else pillImg.color = new Color(0.05f, 0.15f, 0.42f, 0.9f);
            var ring = NewRect("AvatarRing", armyPill, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(118f, 118f));
            var ringImg = ring.gameObject.AddComponent<Image>();
            ringImg.sprite = s_circleSprite; ringImg.color = new Color(1f, 0.82f, 0.25f); ringImg.raycastTarget = false;
            var mask = NewRect("AvatarMask", ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 104f));
            var maskImg = mask.gameObject.AddComponent<Image>();
            maskImg.sprite = s_circleSprite; maskImg.color = new Color(0.2f, 0.45f, 0.9f); maskImg.raycastTarget = false;
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            if (portrait != null)
            {
                var pic = NewRect("Portrait", mask, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(120f, 120f));
                var picImg = pic.gameObject.AddComponent<Image>();
                picImg.sprite = portrait; picImg.preserveAspect = true; picImg.raycastTarget = false;
            }
            var lbl = NewText("ArmyLabel", armyPill, new Vector2(40f, 20f), 26f, new Color(0.75f, 0.88f, 1f));
            lbl.text = "ARMY";
            lbl.rectTransform.sizeDelta = new Vector2(200f, 36f);
            armyText = NewText("ArmyCount", armyPill, new Vector2(40f, -14f), 46f, Color.white);
            armyText.rectTransform.sizeDelta = new Vector2(200f, 60f);

            // ── Coins (top-right) ──
            coinPill = NewRect("CoinPill", canvas, new Vector2(1f, 1f), new Vector2(-190f, -235f), new Vector2(280f, 84f));
            var cImg = coinPill.gameObject.AddComponent<Image>();
            cImg.raycastTarget = false;
            if (pillSprite != null) { cImg.sprite = pillSprite; cImg.type = Image.Type.Sliced; }
            else cImg.color = new Color(0.05f, 0.15f, 0.42f, 0.9f);
            coinIconRt = NewRect("CoinIcon", coinPill, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(96f, 96f));
            var icon = coinIconRt.gameObject.AddComponent<Image>();
            icon.sprite = s_coinSprite; icon.raycastTarget = false;
            coinText = NewText("Coins", coinPill, new Vector2(30f, 0f), 44f, new Color(1f, 0.9f, 0.45f));
            coinText.rectTransform.sizeDelta = new Vector2(200f, 60f);
            coinText.text = "0";
        }

        private void UpdateHudPills(float udt)
        {
            if (armyText != null && shownArmy != soldierCount)
            {
                if (shownArmy >= 0) armyPunch = 1f;
                shownArmy = soldierCount;
                armyText.text = soldierCount.ToString();
            }
            if (armyPill != null) { armyPunch = Mathf.Max(0f, armyPunch - udt * 5f); armyPill.localScale = Vector3.one * (1f + 0.12f * armyPunch); }
            if (coinText != null)
            {
                int shownInt = Mathf.RoundToInt(shownCoins);
                int target = coinsEarned - PendingCoins();
                if (shownInt != target)
                {
                    shownCoins = Mathf.MoveTowards(shownCoins, target, Mathf.Max(20f, Mathf.Abs(target - shownCoins) * 6f) * udt);
                    coinText.text = Mathf.RoundToInt(shownCoins).ToString();
                }
            }
            if (coinPill != null) { coinPunch = Mathf.Max(0f, coinPunch - udt * 6f); coinPill.localScale = Vector3.one * (1f + 0.15f * coinPunch); }
        }

        /// <summary>Award coins: counted now, shown by gold coins flying from the world position into the coin pill.</summary>
        private void AddCoins(int amount, Vector3 worldPos)
        {
            if (amount <= 0) return;
            coinsEarned += amount;
            if (cam == null || coinPill == null || uiCanvas == null) return;
            if (amount < 3 && Time.unscaledTime < coinFlyThrottle) return;   // single kills: max ~6 flights/s
            coinFlyThrottle = Time.unscaledTime + 0.16f;

            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            if (sp.z < 0f) return;
            var canvasRt = (RectTransform)uiCanvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out Vector2 from);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt,
                RectTransformUtility.WorldToScreenPoint(null, coinIconRt.position), null, out Vector2 to);
            int icons = Mathf.Clamp(amount, 1, 6);
            for (int i = 0; i < icons; i++)
            {
                RectTransform rt = coinPool.Count > 0 ? coinPool.Pop() : null;
                if (rt == null)
                {
                    rt = NewRect("FlyCoin", canvasRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));
                    var img = rt.gameObject.AddComponent<Image>();
                    img.sprite = s_coinSprite; img.raycastTarget = false;
                }
                rt.gameObject.SetActive(true);
                rt.SetAsLastSibling();
                Vector2 jitter = Random.insideUnitCircle * 40f;
                rt.anchoredPosition = from + jitter;
                flyingCoins.Add(new FlyingCoin { rt = rt, from = from + jitter, to = to, t = -i * 0.05f, dur = 0.55f + Random.Range(0f, 0.15f), value = 0 });
            }
        }

        private int PendingCoins() => 0;   // counter simply rolls up toward coinsEarned

        private void UpdateFlyingCoins(float udt)
        {
            for (int i = flyingCoins.Count - 1; i >= 0; i--)
            {
                var c = flyingCoins[i];
                c.t += udt;
                if (c.rt == null) { flyingCoins.RemoveAt(i); continue; }
                float k = Mathf.Clamp01(c.t / c.dur);
                float e = k * k * (3f - 2f * k);
                Vector2 mid = (c.from + c.to) * 0.5f + new Vector2(0f, 220f);       // arc upward
                Vector2 p = Vector2.Lerp(Vector2.Lerp(c.from, mid, e), Vector2.Lerp(mid, c.to, e), e);
                c.rt.anchoredPosition = p;
                c.rt.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.7f, e);
                if (k >= 1f)
                {
                    c.rt.gameObject.SetActive(false);
                    coinPool.Push(c.rt);
                    flyingCoins.RemoveAt(i);
                    coinPunch = 1f;
                    Snd?.PlayCoinSpend();
                }
            }
        }

        private static Sprite LoadUISprite(string path)
        {
            var sp = Resources.Load<Sprite>(path);
            if (sp != null) return sp;
            var tex = Resources.Load<Texture2D>(path);
            return tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)) : null;
        }

        /// <summary>Gold coin with rim, inner disc and shine (drawn in code — the project has no single-coin icon).</summary>
        private static Sprite MakeCoinSprite()
        {
            const int N = 96;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "CoinIcon", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            Color rim = new Color(0.78f, 0.45f, 0.05f), face = new Color(1f, 0.78f, 0.18f), inner = new Color(1f, 0.86f, 0.32f), shine = new Color(1f, 0.98f, 0.8f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c;
                    if (d > 0.98f) c = new Color(0, 0, 0, 0);
                    else if (d > 0.84f) c = rim;
                    else if (d > 0.66f) c = face;
                    else if (d > 0.6f) c = rim;
                    else c = Color.Lerp(inner, face, (dy + 1f) * 0.25f);
                    float sh = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(dx, dy), new Vector2(-0.35f, 0.4f)) / 0.28f);
                    if (d <= 0.84f) c = Color.Lerp(c, shine, sh * 0.8f);
                    float aa = Mathf.Clamp01((0.98f - d) * N * 0.5f);
                    c.a *= aa;
                    px[y * N + x] = c;
                }
            t.SetPixels32(px); t.Apply(true);
            return Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        }

        private static Sprite MakeCircleSprite()
        {
            const int N = 128;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "Circle", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                    float a = Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * N * 0.5f);
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply(true);
            return Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        }


        // ═════════════════════════════════════ Danger vignette ══
        //  Screen edges glow red while enemies are on the army, a slam circle is active, or the army is almost gone.

        private Image vignette;
        private float vignetteAlpha;

        private void BuildVignette(Transform canvas)
        {
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Vignette" };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f) / N * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / N * 2f - 1f);
                    float d = Mathf.Max(dx, dy * 0.92f);
                    float a = Mathf.Clamp01((d - 0.62f) / 0.38f);
                    px[y * N + x] = new Color32(255, 30, 20, (byte)(a * a * 255));
                }
            tex.SetPixels32(px); tex.Apply(false);
            var rt = NewRect("DangerVignette", canvas, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling();
            vignette = rt.gameObject.AddComponent<Image>();
            vignette.sprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
            vignette.raycastTarget = false;
            vignette.color = new Color(1f, 1f, 1f, 0f);
        }

        private void UpdateVignette(float udt)
        {
            if (vignette == null) return;
            float danger = 0f;
            if (running && !finished)
            {
                if (clashTimer > 0f) danger = 0.8f;
                for (int i = 0; i < bigs.Count; i++) if (bigs[i].warn != null) danger = 1f;
                if (soldierCount <= 3) danger = Mathf.Max(danger, 0.6f);
            }
            float target = danger * (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 9f));
            vignetteAlpha = Mathf.MoveTowards(vignetteAlpha, target, udt * 3f);
            vignette.color = new Color(1f, 1f, 1f, vignetteAlpha);
        }

        // ═════════════════════════════════════ Old HUD ══
        //  The zombie mode "HP 18 / 18" heart pill means nothing here (the army size is the life bar) → hide it.
        private GameObject hiddenHealthPill;

        private void HideZombieHealthPill()
        {
            var pill = GameObject.Find("HealthPill");
            if (pill != null && pill.activeSelf) { pill.SetActive(false); hiddenHealthPill = pill; }
        }

        private void RestoreZombieHealthPill()
        {
            if (hiddenHealthPill != null) hiddenHealthPill.SetActive(true);
            hiddenHealthPill = null;
        }

        // ═════════════════════════════════════ helpers ══

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        private static void AddImage(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
        }

        private static RectTransform FillRect(string name, RectTransform parent, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            AddImage(rt, c);
            return rt;
        }

        private static void SetFill(RectTransform rt, float frac)
        {
            if (rt == null) return;
            rt.anchorMax = new Vector2(Mathf.Clamp01(frac), 1f);
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, Vector2 pos, float size, Color color)
        {
            var rt = NewRect(name, parent, new Vector2(0.5f, 0.5f), pos, new Vector2(960f, size * 1.4f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (s_font != null) t.font = s_font;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = color;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.outlineWidth = 0.22f;
            t.outlineColor = new Color(0f, 0f, 0f, 0.9f);
            return t;
        }
    }
}
