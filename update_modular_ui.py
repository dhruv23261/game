import re

with open('unity/Assets/Scripts/UI/ZombieRunnerUIManager.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Define Block A replacement (from SetupAmbientSceneryCover down to before LoadZombieSquadUpgradeScreenSprite)
pattern_block_a = re.compile(
    r'private void SetupAmbientSceneryCover\(GameObject panel, Sprite scenerySprite, float fallbackAspect\).*?#endregion\s*#region Zombie Squad Upgrade Screen',
    re.DOTALL
)

replacement_block_a = '''private void SetupAmbientSceneryCover(GameObject panel, Sprite scenerySprite, float fallbackAspect)
        {
            GameObject ambObj = new GameObject("AmbientSceneryCover");
            ambObj.transform.SetParent(panel.transform, false);
            ambObj.transform.SetAsFirstSibling();

            RectTransform ambRt = ambObj.AddComponent<RectTransform>();
            ambRt.anchorMin = Vector2.zero;
            ambRt.anchorMax = Vector2.one;
            ambRt.offsetMin = Vector2.zero;
            ambRt.offsetMax = Vector2.zero;

            // Ambient background must NEVER contain baked logos, text, or modal frames (which cause ghosting/double logos!)
            // Always use the pure clean scenery (sky, watchtowers, mountains, dirt road) as the ambient cover!
            Sprite cleanScenery = LoadSpriteFromUI("UI/HomeScreen_Scenery_Clean", "UI/HomeScreen/HomeScreen_Scenery_Clean");
            Sprite coverSprite = cleanScenery ?? scenerySprite;

            float aspect = (coverSprite != null && coverSprite.rect.height > 0)
                ? (coverSprite.rect.width / coverSprite.rect.height)
                : (1080f / 1920f);

            AspectRatioFitter arf = ambObj.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = aspect;

            Image img = ambObj.AddComponent<Image>();
            if (coverSprite != null) img.sprite = coverSprite;
            img.color = Color.white;
            img.preserveAspect = false; // EnvelopeParent guarantees full screen cover with zero distortion!
            img.raycastTarget = false;
        }

        private GameObject CreateModularTopHeader(Transform parent, string titleStr, System.Action onBack)
        {
            GameObject headerObj = new GameObject("TopHUD_ModularHeader");
            headerObj.transform.SetParent(parent, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -30f);
            hRt.sizeDelta = new Vector2(0f, 95f);

            // Back button (Beveled 3D blue back button)
            Sprite backSp = LoadSpriteFromUI(
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/08_back_button_icon",
                "UI/CharactersDynamic/05_back_button",
                "UI/Btn_3D_Blue"
            );
            GameObject backObj = new GameObject("Btn_Back");
            backObj.transform.SetParent(headerObj.transform, false);
            RectTransform bRt = backObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0.5f);
            bRt.anchorMax = new Vector2(0f, 0.5f);
            bRt.pivot = new Vector2(0f, 0.5f);
            bRt.anchoredPosition = new Vector2(30f, 0f);
            bRt.sizeDelta = new Vector2(85f, 85f);

            Image bImg = backObj.AddComponent<Image>();
            if (backSp != null) bImg.sprite = backSp;
            bImg.preserveAspect = true;
            bImg.raycastTarget = true;
            Button bBtn = backObj.AddComponent<Button>();
            bBtn.targetGraphic = bImg;
            bBtn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(backObj.transform, 0.10f, () => onBack?.Invoke()));
            });

            if (titleStr == "CHARACTERS") charactersBackBtnRect = bRt;
            if (titleStr == "SHOP") shopBackBtnRect = bRt;

            // Title Text TMP (SHOP / CHARACTERS)
            if (!string.IsNullOrEmpty(titleStr))
            {
                GameObject titleObj = new GameObject("Title_TMP");
                titleObj.transform.SetParent(headerObj.transform, false);
                RectTransform tRt = titleObj.AddComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0f, 0.5f);
                tRt.anchorMax = new Vector2(0f, 0.5f);
                tRt.pivot = new Vector2(0f, 0.5f);
                tRt.anchoredPosition = new Vector2(130f, 0f);
                tRt.sizeDelta = new Vector2(360f, 85f);

                TextMeshProUGUI tTmp = titleObj.AddComponent<TextMeshProUGUI>();
                tTmp.enabled = false;
                if (s_globalChildishFont != null) tTmp.font = s_globalChildishFont;
                tTmp.text = titleStr;
                tTmp.fontSize = 38f;
                tTmp.fontStyle = FontStyles.Bold;
                tTmp.alignment = TextAlignmentOptions.MidlineLeft;
                tTmp.color = Color.white;
                tTmp.raycastTarget = false;
                tTmp.enabled = true;

                Outline tOutline = titleObj.AddComponent<Outline>();
                tOutline.effectColor = new Color(0.01f, 0.08f, 0.28f, 1f);
                tOutline.effectDistance = new Vector2(2.5f, -2.5f);

                Shadow tShadow = titleObj.AddComponent<Shadow>();
                tShadow.effectColor = new Color(0.005f, 0.02f, 0.08f, 0.9f);
                tShadow.effectDistance = new Vector2(3f, -3f);
            }

            // Currency Pills Container with HorizontalLayoutGroup
            GameObject curGroup = new GameObject("CurrencyGroup");
            curGroup.transform.SetParent(headerObj.transform, false);
            RectTransform cgRt = curGroup.AddComponent<RectTransform>();
            cgRt.anchorMin = new Vector2(1f, 0.5f);
            cgRt.anchorMax = new Vector2(1f, 0.5f);
            cgRt.pivot = new Vector2(1f, 0.5f);
            cgRt.anchoredPosition = new Vector2(-25f, 0f);
            cgRt.sizeDelta = new Vector2(430f, 75f);

            HorizontalLayoutGroup hlg = curGroup.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 1. Coin Pill
            CreateCurrencyPill(
                curGroup.transform,
                "CoinPill",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/04_empty_currency_bar",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/05_coin_currency_icon",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/07_plus_button_icon",
                GetPlayerCoins().ToString("N0"),
                new Color(1f, 0.96f, 0.65f, 1f),
                allCoinsDisplays,
                () =>
                {
                    ShowZombieSquadShopScreen();
                    SelectShopTab("Coins");
                }
            );

            // 2. Diamond Pill
            CreateCurrencyPill(
                curGroup.transform,
                "DiamondPill",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/04_empty_currency_bar",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/06_diamond_currency_icon",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/07_plus_button_icon",
                GetPlayerDiamonds().ToString("N0"),
                new Color(0.55f, 0.90f, 1f, 1f),
                allDiamondsDisplays,
                () =>
                {
                    ShowZombieSquadShopScreen();
                    SelectShopTab("Diamonds");
                }
            );

            return headerObj;
        }

        private GameObject CreateCurrencyPill(
            Transform parent,
            string name,
            string barSpritePath,
            string iconSpritePath,
            string plusSpritePath,
            string initialValue,
            Color textColor,
            List<TextMeshProUGUI> registerList,
            System.Action onPlusClick)
        {
            GameObject pillObj = new GameObject(name);
            pillObj.transform.SetParent(parent, false);
            RectTransform pRt = pillObj.AddComponent<RectTransform>();
            pRt.sizeDelta = new Vector2(200f, 64f);

            // Pill background (9-sliced bar)
            Image bgImg = pillObj.AddComponent<Image>();
            Sprite barSp = LoadSpriteFromUI(barSpritePath, "UI/CharactersDynamic/10_coin_bar", "UI/Btn_3D_Blue");
            if (barSp != null) bgImg.sprite = barSp;
            bgImg.type = Image.Type.Sliced;
            bgImg.raycastTarget = false;

            // Currency Icon on left
            Sprite iconSp = LoadSpriteFromUI(iconSpritePath);
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(pillObj.transform, false);
            RectTransform iRt = iconObj.AddComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0f, 0.5f);
            iRt.anchorMax = new Vector2(0f, 0.5f);
            iRt.pivot = new Vector2(0f, 0.5f);
            iRt.anchoredPosition = new Vector2(8f, 0f);
            iRt.sizeDelta = new Vector2(50f, 50f);
            Image iImg = iconObj.AddComponent<Image>();
            if (iconSp != null) iImg.sprite = iconSp;
            iImg.preserveAspect = true;
            iImg.raycastTarget = false;

            // Plus Button on right
            Sprite plusSp = LoadSpriteFromUI(plusSpritePath, "UI/CharactersDynamic/08_plus_coin", "UI/Btn_3D_Green");
            GameObject plusObj = new GameObject("Btn_Plus");
            plusObj.transform.SetParent(pillObj.transform, false);
            RectTransform plusRt = plusObj.AddComponent<RectTransform>();
            plusRt.anchorMin = new Vector2(1f, 0.5f);
            plusRt.anchorMax = new Vector2(1f, 0.5f);
            plusRt.pivot = new Vector2(1f, 0.5f);
            plusRt.anchoredPosition = new Vector2(-6f, 0f);
            plusRt.sizeDelta = new Vector2(46f, 46f);
            Image plusImg = plusObj.AddComponent<Image>();
            if (plusSp != null) plusImg.sprite = plusSp;
            plusImg.preserveAspect = true;
            plusImg.raycastTarget = true;
            Button plusBtn = plusObj.AddComponent<Button>();
            plusBtn.targetGraphic = plusImg;
            if (onPlusClick != null)
            {
                plusBtn.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(plusObj.transform, 0.10f, () => onPlusClick()));
                });
            }

            // Dynamic TMP Text in the middle with auto-size
            GameObject txtObj = new GameObject("ValueText");
            txtObj.transform.SetParent(pillObj.transform, false);
            RectTransform tRt = txtObj.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(58f, 0f); // Right of icon
            tRt.offsetMax = new Vector2(-50f, 0f); // Left of plus button

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.enabled = false;
            if (s_globalChildishFont != null) tmp.font = s_globalChildishFont;
            tmp.text = initialValue;
            tmp.fontSize = 22f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 13f;
            tmp.fontSizeMax = 22f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            tmp.raycastTarget = false;
            tmp.enabled = true;

            Outline outline = txtObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.01f, 0.05f, 0.16f, 1f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            if (registerList != null) registerList.Add(tmp);

            return pillObj;
        }

        private void CreateTopCurrencyHeader(Transform parent, string titleStr, System.Action onBack, float yMin = 0.765f, float yMax = 0.812f)
        {
            CreateModularTopHeader(parent, titleStr, onBack);
        }
        #endregion

        #region Zombie Squad Characters Screen
        private void BuildZombieSquadCharactersScreenPanel(Transform canvasRoot)
        {
            if (charactersPanel != null) SafeDestroy(charactersPanel);
            Transform oldChars = canvasRoot.Find("ZombieSquadCharactersPanel");
            if (oldChars != null) SafeDestroy(oldChars.gameObject);

            charactersPanel = CreateFullPanel("ZombieSquadCharactersPanel", canvasRoot, new Color(0.01f, 0.03f, 0.08f, 1f));

            // 0. Clean Scenery Background (EnvelopeParent fills 100% of any phone/tablet display - zero black bars, zero baked UI!)
            SetupAmbientSceneryCover(charactersPanel, null, 1080f / 1920f);

            // Semi-transparent cinematic dark vignette over scenery for crisp UI contrast
            GameObject vig = new GameObject("CharactersVignette");
            vig.transform.SetParent(charactersPanel.transform, false);
            RectTransform vigRt = vig.AddComponent<RectTransform>();
            vigRt.anchorMin = Vector2.zero; vigRt.anchorMax = Vector2.one;
            vigRt.offsetMin = Vector2.zero; vigRt.offsetMax = Vector2.zero;
            Image vigImg = vig.AddComponent<Image>();
            vigImg.color = new Color(0.01f, 0.03f, 0.08f, 0.55f);
            vigImg.raycastTarget = false;

            // 1. Top HUD Header (Back, "CHARACTERS", Coins Pill, Diamonds Pill)
            CreateModularTopHeader(charactersPanel.transform, "CHARACTERS", () => ShowZombieSquadHomeScreen());

            // 2. Central Responsive Content Container
            GameObject contentContainer = new GameObject("CharactersContainer");
            contentContainer.transform.SetParent(charactersPanel.transform, false);
            RectTransform ccRt = contentContainer.AddComponent<RectTransform>();
            ccRt.anchorMin = new Vector2(0.5f, 0.5f);
            ccRt.anchorMax = new Vector2(0.5f, 0.5f);
            ccRt.pivot = new Vector2(0.5f, 0.5f);
            ccRt.anchoredPosition = new Vector2(0f, -40f);
            ccRt.sizeDelta = new Vector2(980f, 1420f);

            // 3. Top Mascot & Logo on Characters Modal
            Sprite logoSp = LoadSpriteFromUI("UI/CharactersDynamic/01_logo", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/01_zombie_squad_logo");
            Sprite soldierSp = LoadSpriteFromUI("UI/CharactersDynamic/02_header_soldier", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/02_soldier_character");

            GameObject logoHeader = new GameObject("LogoHeaderBanner");
            logoHeader.transform.SetParent(contentContainer.transform, false);
            RectTransform lhRt = logoHeader.AddComponent<RectTransform>();
            lhRt.anchorMin = new Vector2(0.5f, 1f);
            lhRt.anchorMax = new Vector2(0.5f, 1f);
            lhRt.pivot = new Vector2(0.5f, 1f);
            lhRt.anchoredPosition = new Vector2(0f, -5f);
            lhRt.sizeDelta = new Vector2(600f, 130f);

            if (soldierSp != null)
            {
                GameObject soldierObj = new GameObject("SoldierMascot");
                soldierObj.transform.SetParent(logoHeader.transform, false);
                RectTransform smRt = soldierObj.AddComponent<RectTransform>();
                smRt.anchorMin = new Vector2(0.08f, 0f);
                smRt.anchorMax = new Vector2(0.32f, 1.15f);
                smRt.offsetMin = Vector2.zero; smRt.offsetMax = Vector2.zero;
                Image smImg = soldierObj.AddComponent<Image>();
                smImg.sprite = soldierSp;
                smImg.preserveAspect = true;
                smImg.raycastTarget = false;
            }

            if (logoSp != null)
            {
                GameObject logoObj = new GameObject("ZombieSquadLogo");
                logoObj.transform.SetParent(logoHeader.transform, false);
                RectTransform lRt = logoObj.AddComponent<RectTransform>();
                lRt.anchorMin = new Vector2(0.30f, 0.05f);
                lRt.anchorMax = new Vector2(0.92f, 0.95f);
                lRt.offsetMin = Vector2.zero; lRt.offsetMax = Vector2.zero;
                Image lImg = logoObj.AddComponent<Image>();
                lImg.sprite = logoSp;
                lImg.preserveAspect = true;
                lImg.raycastTarget = false;
            }

            // 4. Center Showcase Chamber (Pedestal + Active Hero + Arrows)
            GameObject showcaseChamber = new GameObject("ShowcaseChamber");
            showcaseChamber.transform.SetParent(contentContainer.transform, false);
            RectTransform scRt = showcaseChamber.AddComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0.5f, 1f);
            scRt.anchorMax = new Vector2(0.5f, 1f);
            scRt.pivot = new Vector2(0.5f, 1f);
            scRt.anchoredPosition = new Vector2(0f, -140f);
            scRt.sizeDelta = new Vector2(920f, 490f);

            // Frame (12_main_showcase_panel.png)
            Image scImg = showcaseChamber.AddComponent<Image>();
            Sprite scSp = LoadSpriteFromUI("UI/CharactersDynamic/12_main_showcase_panel");
            if (scSp != null) scImg.sprite = scSp;
            scImg.type = Image.Type.Sliced;
            scImg.color = Color.white;
            scImg.raycastTarget = false;

            // Hero Name & Role TextMeshPro
            charPreviewNameText = CreateText("HeroName", showcaseChamber.transform, "SOLDIER", 34f, Color.white, TextAlignmentOptions.Center, true);
            RectTransform nameRt = charPreviewNameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.15f, 0.82f);
            nameRt.anchorMax = new Vector2(0.85f, 0.96f);
            nameRt.offsetMin = Vector2.zero; nameRt.offsetMax = Vector2.zero;
            Outline nameOutl = charPreviewNameText.gameObject.AddComponent<Outline>();
            nameOutl.effectColor = new Color(0.01f, 0.08f, 0.28f, 1f);
            nameOutl.effectDistance = new Vector2(2f, -2f);

            charPreviewRoleText = CreateText("HeroRole", showcaseChamber.transform, "ASSAULT SPECIALIST", 18f, new Color(0.35f, 0.90f, 1f, 1f), TextAlignmentOptions.Center, true);
            RectTransform roleRt = charPreviewRoleText.GetComponent<RectTransform>();
            roleRt.anchorMin = new Vector2(0.15f, 0.70f);
            roleRt.anchorMax = new Vector2(0.85f, 0.82f);
            roleRt.offsetMin = Vector2.zero; roleRt.offsetMax = Vector2.zero;

            // Glowing Pedestal (14_glow_platform.png) - Clean, ZERO baked characters
            GameObject pedObj = new GameObject("GlowPedestal");
            pedObj.transform.SetParent(showcaseChamber.transform, false);
            RectTransform pRt = pedObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0f);
            pRt.anchorMax = new Vector2(0.5f, 0f);
            pRt.pivot = new Vector2(0.5f, 0f);
            pRt.anchoredPosition = new Vector2(0f, 20f);
            pRt.sizeDelta = new Vector2(520f, 110f);
            Image pedImg = pedObj.AddComponent<Image>();
            Sprite pedSp = LoadSpriteFromUI("UI/CharactersDynamic/14_glow_platform");
            if (pedSp != null) pedImg.sprite = pedSp;
            pedImg.preserveAspect = true;
            pedImg.raycastTarget = false;

            // Selected Hero Avatar (Standing directly on top of pedestal)
            // Only ONE hero avatar exists in hierarchy - NO baked soldier, NO double character!
            GameObject avatarObj = new GameObject("HeroAvatar");
            avatarObj.transform.SetParent(showcaseChamber.transform, false);
            RectTransform aRt = avatarObj.AddComponent<RectTransform>();
            aRt.anchorMin = new Vector2(0.5f, 0f);
            aRt.anchorMax = new Vector2(0.5f, 0f);
            aRt.pivot = new Vector2(0.5f, 0.12f);
            aRt.anchoredPosition = new Vector2(0f, 30f);
            aRt.sizeDelta = new Vector2(380f, 430f);

            charPreviewHeroImage = avatarObj.AddComponent<Image>();
            charPreviewHeroImage.preserveAspect = true;
            charPreviewHeroImage.raycastTarget = false;

            Shadow avShadow = avatarObj.AddComponent<Shadow>();
            avShadow.effectColor = new Color(0.01f, 0.05f, 0.15f, 0.95f);
            avShadow.effectDistance = new Vector2(3f, -4f);

            // Carousel Navigation Arrows (< and >)
            Sprite leftArrowSp = LoadSpriteFromUI("UI/CharactersDynamic/15_left_arrow");
            Sprite rightArrowSp = LoadSpriteFromUI("UI/CharactersDynamic/16_right_arrow");

            GameObject leftArrowObj = new GameObject("Btn_Prev_Hero");
            leftArrowObj.transform.SetParent(showcaseChamber.transform, false);
            RectTransform laRt = leftArrowObj.AddComponent<RectTransform>();
            laRt.anchorMin = new Vector2(0f, 0.5f);
            laRt.anchorMax = new Vector2(0f, 0.5f);
            laRt.pivot = new Vector2(0f, 0.5f);
            laRt.anchoredPosition = new Vector2(25f, -20f);
            laRt.sizeDelta = new Vector2(75f, 110f);
            Image laImg = leftArrowObj.AddComponent<Image>();
            if (leftArrowSp != null) laImg.sprite = leftArrowSp;
            laImg.preserveAspect = true;
            laImg.raycastTarget = true;
            Button laBtn = leftArrowObj.AddComponent<Button>();
            laBtn.targetGraphic = laImg;
            laBtn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(leftArrowObj.transform, 0.10f, () => CycleHero(-1)));
            });

            GameObject rightArrowObj = new GameObject("Btn_Next_Hero");
            rightArrowObj.transform.SetParent(showcaseChamber.transform, false);
            RectTransform raRt = rightArrowObj.AddComponent<RectTransform>();
            raRt.anchorMin = new Vector2(1f, 0.5f);
            raRt.anchorMax = new Vector2(1f, 0.5f);
            raRt.pivot = new Vector2(1f, 0.5f);
            raRt.anchoredPosition = new Vector2(-25f, -20f);
            raRt.sizeDelta = new Vector2(75f, 110f);
            Image raImg = rightArrowObj.AddComponent<Image>();
            if (rightArrowSp != null) raImg.sprite = rightArrowSp;
            raImg.preserveAspect = true;
            raImg.raycastTarget = true;
            Button raBtn = rightArrowObj.AddComponent<Button>();
            raBtn.targetGraphic = raImg;
            raBtn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(rightArrowObj.transform, 0.10f, () => CycleHero(1)));
            });

            // 5. Action / Equip Button below chamber
            GameObject actBtnObj = new GameObject("Btn_EquipHero");
            actBtnObj.transform.SetParent(contentContainer.transform, false);
            RectTransform abRt = actBtnObj.AddComponent<RectTransform>();
            abRt.anchorMin = new Vector2(0.5f, 1f);
            abRt.anchorMax = new Vector2(0.5f, 1f);
            abRt.pivot = new Vector2(0.5f, 1f);
            abRt.anchoredPosition = new Vector2(0f, -645f);
            abRt.sizeDelta = new Vector2(440f, 95f);

            charPreviewEquipBtnBg = actBtnObj.AddComponent<Image>();
            charPreviewEquipBtnBg.type = Image.Type.Sliced;
            charPreviewEquipBtnBg.raycastTarget = true;

            charPreviewEquipBtn = actBtnObj.AddComponent<Button>();
            charPreviewEquipBtn.targetGraphic = charPreviewEquipBtnBg;
            charPreviewEquipBtn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(actBtnObj.transform, 0.12f, () => EquipSelectedCharacter()));
            });

            charPreviewEquipBtnText = CreateText("Txt", actBtnObj.transform, "EQUIPPED", 28f, Color.white, TextAlignmentOptions.Center, true);
            RectTransform abtRt = charPreviewEquipBtnText.GetComponent<RectTransform>();
            abtRt.anchorMin = Vector2.zero; abtRt.anchorMax = Vector2.one;
            abtRt.offsetMin = Vector2.zero; abtRt.offsetMax = Vector2.zero;

            Outline abOutl = charPreviewEquipBtnText.gameObject.AddComponent<Outline>();
            abOutl.effectColor = new Color(0.04f, 0.20f, 0.08f, 1f);
            abOutl.effectDistance = new Vector2(2f, -2f);

            // 6. 5 Hero Cards (Row 1: Soldier, Ranger, Heavy; Row 2: Sniper, Medic)
            charCardPriceTexts.Clear();
            charCardPriceBgs.Clear();

            // Row 1 (3 cards)
            GameObject row1 = new GameObject("HeroCards_Row1");
            row1.transform.SetParent(contentContainer.transform, false);
            RectTransform r1Rt = row1.AddComponent<RectTransform>();
            r1Rt.anchorMin = new Vector2(0.5f, 1f);
            r1Rt.anchorMax = new Vector2(0.5f, 1f);
            r1Rt.pivot = new Vector2(0.5f, 1f);
            r1Rt.anchoredPosition = new Vector2(0f, -755f);
            r1Rt.sizeDelta = new Vector2(920f, 290f);

            HorizontalLayoutGroup hlg1 = row1.AddComponent<HorizontalLayoutGroup>();
            hlg1.spacing = 20f;
            hlg1.childAlignment = TextAnchor.MiddleCenter;
            hlg1.childControlWidth = false;
            hlg1.childControlHeight = false;
            hlg1.childForceExpandWidth = false;
            hlg1.childForceExpandHeight = false;

            CreateModularHeroCard(row1.transform, "Soldier",
                "UI/CharactersDynamic/17_equipped_character_card",
                "UI/CharactersDynamic/22_green_soldier_portrait");

            CreateModularHeroCard(row1.transform, "Ranger",
                "UI/CharactersDynamic/18_red_character_card",
                "UI/CharactersDynamic/23_red_soldier_portrait");

            CreateModularHeroCard(row1.transform, "Heavy",
                "UI/CharactersDynamic/19_yellow_character_card",
                "UI/CharactersDynamic/24_yellow_soldier_portrait");

            // Row 2 (2 cards)
            GameObject row2 = new GameObject("HeroCards_Row2");
            row2.transform.SetParent(contentContainer.transform, false);
            RectTransform r2Rt = row2.AddComponent<RectTransform>();
            r2Rt.anchorMin = new Vector2(0.5f, 1f);
            r2Rt.anchorMax = new Vector2(0.5f, 1f);
            r2Rt.pivot = new Vector2(0.5f, 1f);
            r2Rt.anchoredPosition = new Vector2(0f, -1060f);
            r2Rt.sizeDelta = new Vector2(920f, 290f);

            HorizontalLayoutGroup hlg2 = row2.AddComponent<HorizontalLayoutGroup>();
            hlg2.spacing = 30f;
            hlg2.childAlignment = TextAnchor.MiddleCenter;
            hlg2.childControlWidth = false;
            hlg2.childControlHeight = false;
            hlg2.childForceExpandWidth = false;
            hlg2.childForceExpandHeight = false;

            CreateModularHeroCard(row2.transform, "Sniper",
                "UI/CharactersDynamic/20_purple_character_card",
                "UI/CharactersDynamic/25_purple_soldier_portrait");

            CreateModularHeroCard(row2.transform, "Medic",
                "UI/CharactersDynamic/21_pink_medic_card",
                "UI/CharactersDynamic/26_pink_medic_portrait");

            // 7. Bottom Decorative Metal Trim & Rocks
            Sprite cTrimSp = LoadSpriteFromUI("UI/CharactersDynamic/30_bottom_metal_trim", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/26_bottom_metal_trim");
            Sprite cLeftRock = LoadSpriteFromUI("UI/CharactersDynamic/31_left_rock_grass", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/27_left_rock_grass_decoration");
            Sprite cRightRock = LoadSpriteFromUI("UI/CharactersDynamic/32_right_rock_grass", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/28_right_rock_grass_decoration");

            if (cTrimSp != null)
            {
                GameObject trimObj = new GameObject("BottomTrim");
                trimObj.transform.SetParent(contentContainer.transform, false);
                RectTransform tRt = trimObj.AddComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0f, 0f);
                tRt.anchorMax = new Vector2(1f, 0f);
                tRt.pivot = new Vector2(0.5f, 0f);
                tRt.anchoredPosition = new Vector2(0f, 10f);
                tRt.sizeDelta = new Vector2(0f, 90f);
                Image trimImg = trimObj.AddComponent<Image>();
                trimImg.sprite = cTrimSp;
                trimImg.preserveAspect = false;
                trimImg.raycastTarget = false;
            }

            if (cLeftRock != null)
            {
                GameObject lrObj = new GameObject("LeftRock");
                lrObj.transform.SetParent(contentContainer.transform, false);
                RectTransform lrRt = lrObj.AddComponent<RectTransform>();
                lrRt.anchorMin = new Vector2(0f, 0f);
                lrRt.anchorMax = new Vector2(0f, 0f);
                lrRt.pivot = new Vector2(0f, 0f);
                lrRt.anchoredPosition = new Vector2(-20f, 5f);
                lrRt.sizeDelta = new Vector2(140f, 120f);
                Image lrImg = lrObj.AddComponent<Image>();
                lrImg.sprite = cLeftRock;
                lrImg.preserveAspect = true;
                lrImg.raycastTarget = false;
            }

            if (cRightRock != null)
            {
                GameObject rrObj = new GameObject("RightRock");
                rrObj.transform.SetParent(contentContainer.transform, false);
                RectTransform rrRt = rrObj.AddComponent<RectTransform>();
                rrRt.anchorMin = new Vector2(1f, 0f);
                rrRt.anchorMax = new Vector2(1f, 0f);
                rrRt.pivot = new Vector2(1f, 0f);
                rrRt.anchoredPosition = new Vector2(20f, 5f);
                rrRt.sizeDelta = new Vector2(140f, 120f);
                Image rrImg = rrObj.AddComponent<Image>();
                rrImg.sprite = cRightRock;
                rrImg.preserveAspect = true;
                rrImg.raycastTarget = false;
            }

            UpdateCharactersScreenVisuals();
        }

        private GameObject CreateModularHeroCard(Transform parent, string heroKey, string cardSpritePath, string portraitSpritePath)
        {
            GameObject cardObj = new GameObject($"Card_{heroKey}");
            cardObj.transform.SetParent(parent, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(280f, 280f);

            // Card frame
            Image cardBg = cardObj.AddComponent<Image>();
            Sprite cardSp = LoadSpriteFromUI(cardSpritePath);
            if (cardSp != null) cardBg.sprite = cardSp;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = Color.white;
            cardBg.raycastTarget = true;

            Button cardBtn = cardObj.AddComponent<Button>();
            cardBtn.targetGraphic = cardBg;
            cardBtn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(cardObj.transform, 0.12f, () => SelectCharacter(heroKey)));
            });

            // Hero Portrait
            Sprite portSp = LoadSpriteFromUI(portraitSpritePath);
            GameObject portObj = new GameObject("Portrait");
            portObj.transform.SetParent(cardObj.transform, false);
            RectTransform pRt = portObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.10f, 0.28f);
            pRt.anchorMax = new Vector2(0.90f, 0.94f);
            pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
            Image portImg = portObj.AddComponent<Image>();
            if (portSp != null) portImg.sprite = portSp;
            portImg.preserveAspect = true;
            portImg.raycastTarget = false;

            // Status / Price Button Pill at bottom of card
            GameObject pillObj = new GameObject("PricePill");
            pillObj.transform.SetParent(cardObj.transform, false);
            RectTransform pillRt = pillObj.AddComponent<RectTransform>();
            pillRt.anchorMin = new Vector2(0.08f, 0.06f);
            pillRt.anchorMax = new Vector2(0.92f, 0.28f);
            pillRt.offsetMin = Vector2.zero; pillRt.offsetMax = Vector2.zero;

            Image pillBg = pillObj.AddComponent<Image>();
            pillBg.type = Image.Type.Sliced;
            pillBg.raycastTarget = false;
            charCardPriceBgs[heroKey] = pillBg;

            TextMeshProUGUI pillTxt = CreateText("Txt", pillObj.transform, "", 20f, Color.white, TextAlignmentOptions.Center, true);
            RectTransform ptRt = pillTxt.GetComponent<RectTransform>();
            ptRt.anchorMin = Vector2.zero; ptRt.anchorMax = Vector2.one;
            ptRt.offsetMin = Vector2.zero; ptRt.offsetMax = Vector2.zero;

            Outline ptOutl = pillTxt.gameObject.AddComponent<Outline>();
            ptOutl.effectColor = new Color(0.01f, 0.05f, 0.15f, 1f);
            ptOutl.effectDistance = new Vector2(1.5f, -1.5f);

            charCardPriceTexts[heroKey] = pillTxt;

            return cardObj;
        }

        public void CycleHero(int direction)
        {
            string[] heroKeys = new string[] { "Soldier", "Ranger", "Heavy", "Sniper", "Medic" };
            int idx = System.Array.IndexOf(heroKeys, selectedCharacterKey);
            if (idx < 0) idx = 0;
            idx = (idx + direction + heroKeys.Length) % heroKeys.Length;
            SelectCharacter(heroKeys[idx]);
        }

        public void SelectCharacter(string heroKey)
        {
            selectedCharacterKey = heroKey;
            Debug.Log($"[ZombieSquad] Selected Character: {heroKey}");
            if (charPreviewHeroImage != null)
            {
                StartCoroutine(PunchScale(charPreviewHeroImage.transform, 0.15f, null));
            }
            UpdateCharactersScreenVisuals();
        }

        public void EquipSelectedCharacter()
        {
            if (string.IsNullOrEmpty(selectedCharacterKey)) selectedCharacterKey = "Soldier";
            bool isUnlocked = selectedCharacterKey == "Soldier" || PlayerPrefs.GetInt("HeroUnlocked_" + selectedCharacterKey, 0) == 1;

            if (!isUnlocked && heroes.TryGetValue(selectedCharacterKey, out HeroData hData))
            {
                if (hData.priceCoins > 0)
                {
                    if (GetPlayerCoins() >= hData.priceCoins)
                    {
                        AddPlayerCoins(-hData.priceCoins);
                        PlayerPrefs.SetInt("HeroUnlocked_" + selectedCharacterKey, 1);
                        PlayerPrefs.Save();
                        Debug.Log($"[ZombieSquad] Unlocked {selectedCharacterKey} for {hData.priceCoins} coins!");
                    }
                    else
                    {
                        Debug.Log($"[ZombieSquad] Need {hData.priceCoins - GetPlayerCoins()} more coins!");
                        return;
                    }
                }
                else if (hData.priceDiamonds > 0)
                {
                    if (GetPlayerDiamonds() >= hData.priceDiamonds)
                    {
                        AddPlayerDiamonds(-hData.priceDiamonds);
                        PlayerPrefs.SetInt("HeroUnlocked_" + selectedCharacterKey, 1);
                        PlayerPrefs.Save();
                        Debug.Log($"[ZombieSquad] Unlocked {selectedCharacterKey} for {hData.priceDiamonds} diamonds!");
                    }
                    else
                    {
                        Debug.Log($"[ZombieSquad] Need {hData.priceDiamonds - GetPlayerDiamonds()} more diamonds!");
                        return;
                    }
                }
            }

            PlayerPrefs.SetString("ZombieSquad_SelectedHero", selectedCharacterKey);
            PlayerPrefs.Save();
            if (playerRunner != null) playerRunner.ApplyEquippedHeroStats();
            Debug.Log($"[ZombieSquad] Equipped Hero: {selectedCharacterKey}");
            UpdateCharactersScreenVisuals();
        }

        public void UpdateCharactersScreenVisuals()
        {
            if (string.IsNullOrEmpty(selectedCharacterKey)) selectedCharacterKey = "Soldier";
            string currentEquipped = PlayerPrefs.GetString("ZombieSquad_SelectedHero", "Soldier");

            bool isSoldier = (selectedCharacterKey == "Soldier");

            // 1. Showcase Hero Figure and Name/Role
            if (charPreviewHeroImage != null)
            {
                charPreviewHeroImage.gameObject.SetActive(true);
                if (charPreviewNameText != null) charPreviewNameText.gameObject.SetActive(true);
                if (charPreviewRoleText != null) charPreviewRoleText.gameObject.SetActive(true);

                Sprite charSpr = LoadSpriteFromFile($"Resources/UI/Hero_{selectedCharacterKey}_Full.png")
                              ?? Resources.Load<Sprite>($"UI/Hero_{selectedCharacterKey}_Full")
                              ?? LoadSpriteFromFile($"Resources/UI/Hero_{selectedCharacterKey}_Icon.png")
                              ?? Resources.Load<Sprite>($"UI/Hero_{selectedCharacterKey}_Icon");
                if (charSpr != null) charPreviewHeroImage.sprite = charSpr;

                if (heroes.TryGetValue(selectedCharacterKey, out HeroData hData))
                {
                    if (charPreviewNameText != null) charPreviewNameText.text = hData.name.ToUpper();
                    if (charPreviewRoleText != null) charPreviewRoleText.text = hData.role.ToUpper();
                }
            }

            // 2. Action Button State (Equipped / Equip / Unlock)
            bool isCurrentHeroUnlocked = isSoldier || PlayerPrefs.GetInt("HeroUnlocked_" + selectedCharacterKey, 0) == 1;
            bool isEquipped = (selectedCharacterKey == currentEquipped);

            if (charPreviewEquipBtnText != null && charPreviewEquipBtnBg != null)
            {
                Outline btnOutline = charPreviewEquipBtnText.GetComponent<Outline>();

                if (isEquipped)
                {
                    charPreviewEquipBtnBg.sprite = LoadSpriteFromUI("UI/CharactersDynamic/27_equipped_button", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/13_green_purchase_button_blank", "UI/Btn_3D_Green");
                    charPreviewEquipBtnBg.color = Color.white;
                    charPreviewEquipBtnText.text = "✔ EQUIPPED";
                    if (btnOutline != null) btnOutline.effectColor = new Color(0.04f, 0.22f, 0.08f, 1f);
                }
                else if (!isCurrentHeroUnlocked && heroes.TryGetValue(selectedCharacterKey, out HeroData lData))
                {
                    charPreviewEquipBtnBg.color = Color.white;
                    if (lData.priceCoins > 0)
                    {
                        charPreviewEquipBtnBg.sprite = LoadSpriteFromUI("UI/CharactersDynamic/28_coin_button_blank_base", "UI/Btn_3D_Gold");
                        charPreviewEquipBtnText.text = $"UNLOCK ({lData.priceCoins:N0} COINS)";
                        if (btnOutline != null) btnOutline.effectColor = new Color(0.24f, 0.09f, 0.02f, 1f);
                    }
                    else
                    {
                        charPreviewEquipBtnBg.sprite = LoadSpriteFromUI("UI/CharactersDynamic/29_gem_button_blank_base", "UI/Btn_3D_Cyan");
                        charPreviewEquipBtnText.text = $"UNLOCK ({lData.priceDiamonds} 💎)";
                        if (btnOutline != null) btnOutline.effectColor = new Color(0.02f, 0.12f, 0.25f, 1f);
                    }
                }
                else
                {
                    charPreviewEquipBtnBg.color = Color.white;
                    charPreviewEquipBtnBg.sprite = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/14_blue_action_button_blank", "UI/Btn_3D_Blue");
                    charPreviewEquipBtnText.text = "EQUIP HERO";
                    if (btnOutline != null) btnOutline.effectColor = new Color(0.04f, 0.12f, 0.32f, 1f);
                }
            }

            // 3. Update All 5 Card Price Labels
            foreach (var kvp in heroes)
            {
                string hKey = kvp.Key;
                HeroData hd = kvp.Value;
                if (!charCardPriceTexts.TryGetValue(hKey, out TextMeshProUGUI pillTxt) || pillTxt == null) continue;
                charCardPriceBgs.TryGetValue(hKey, out Image pillBg);

                bool heroUnlocked = (hKey == "Soldier") || PlayerPrefs.GetInt("HeroUnlocked_" + hKey, 0) == 1;

                if (hKey == currentEquipped)
                {
                    if (pillBg != null)
                    {
                        pillBg.sprite = LoadSpriteFromUI("UI/CharactersDynamic/27_equipped_button", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/13_green_purchase_button_blank", "UI/Btn_3D_Green");
                        pillBg.color = Color.white;
                    }
                    pillTxt.text = "EQUIPPED";
                    pillTxt.color = Color.white;
                }
                else if (heroUnlocked)
                {
                    if (pillBg != null)
                    {
                        pillBg.sprite = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/14_blue_action_button_blank", "UI/Btn_3D_Blue");
                        pillBg.color = Color.white;
                    }
                    pillTxt.text = "READY";
                    pillTxt.color = Color.white;
                }
                else
                {
                    if (pillBg != null)
                    {
                        if (hd.priceCoins > 0)
                        {
                            pillBg.sprite = LoadSpriteFromUI("UI/CharactersDynamic/28_coin_button_blank_base", "UI/Btn_3D_Gold");
                            pillTxt.text = $"{hd.priceCoins:N0}";
                        }
                        else
                        {
                            pillBg.sprite = LoadSpriteFromUI("UI/CharactersDynamic/29_gem_button_blank_base", "UI/Btn_3D_Cyan");
                            pillTxt.text = $"{hd.priceDiamonds} 💎";
                        }
                        pillBg.color = Color.white;
                    }
                    pillTxt.color = Color.white;
                }
            }
        }
        #endregion

        #region Zombie Squad Upgrade Screen'''

code, count_a = pattern_block_a.subn(replacement_block_a, code)
print(f'Block A replaced: {count_a}')

# Define Block B replacement (BuildZombieSquadShopScreenPanel)
pattern_block_b = re.compile(
    r'private void BuildZombieSquadShopScreenPanel\(Transform canvasRoot\).*?private GameObject CreateShopPriceButton',
    re.DOTALL
)

replacement_block_b = '''private void BuildZombieSquadShopScreenPanel(Transform canvasRoot)
        {
            if (shopPanel != null) SafeDestroy(shopPanel);
            Transform oldShop = canvasRoot.Find("ZombieSquadShopPanel");
            if (oldShop != null) SafeDestroy(oldShop.gameObject);

            shopPanel = CreateFullPanel("ZombieSquadShopPanel", canvasRoot, new Color(0.01f, 0.03f, 0.08f, 1f));

            // 0. Clean Scenery Background with EnvelopeParent (Zero baked UI, zero distortion)
            SetupAmbientSceneryCover(shopPanel, null, 1080f / 1920f);

            // Semi-transparent cinematic dark vignette over scenery for crisp UI contrast
            GameObject vig = new GameObject("ShopVignette");
            vig.transform.SetParent(shopPanel.transform, false);
            RectTransform vigRt = vig.AddComponent<RectTransform>();
            vigRt.anchorMin = Vector2.zero; vigRt.anchorMax = Vector2.one;
            vigRt.offsetMin = Vector2.zero; vigRt.offsetMax = Vector2.zero;
            Image vigImg = vig.AddComponent<Image>();
            vigImg.color = new Color(0.01f, 0.03f, 0.08f, 0.60f);
            vigImg.raycastTarget = false;

            // 1. Top HUD Header (Back, "SHOP", Coins Pill, Diamonds Pill)
            CreateModularTopHeader(shopPanel.transform, "SHOP", () => ShowZombieSquadHomeScreen());

            // 2. Central Responsive Modal Container
            GameObject modalContainer = new GameObject("ShopModalContainer");
            modalContainer.transform.SetParent(shopPanel.transform, false);
            RectTransform mcRt = modalContainer.AddComponent<RectTransform>();
            mcRt.anchorMin = new Vector2(0.5f, 0.5f);
            mcRt.anchorMax = new Vector2(0.5f, 0.5f);
            mcRt.pivot = new Vector2(0.5f, 0.5f);
            mcRt.anchoredPosition = new Vector2(0f, -40f);
            mcRt.sizeDelta = new Vector2(980f, 1420f);

            // Modal Background Frame (Cyber deep blue with neon border)
            GameObject frameObj = new GameObject("ModalFrame");
            frameObj.transform.SetParent(modalContainer.transform, false);
            RectTransform mfRt = frameObj.AddComponent<RectTransform>();
            mfRt.anchorMin = Vector2.zero; mfRt.anchorMax = Vector2.one;
            mfRt.offsetMin = new Vector2(0f, 60f);
            mfRt.offsetMax = new Vector2(0f, -60f);
            Image mfImg = frameObj.AddComponent<Image>();
            shopScreenBgImage = mfImg;
            Sprite frameSp = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/09_empty_shop_card");
            if (frameSp != null) mfImg.sprite = frameSp;
            mfImg.type = Image.Type.Sliced;
            mfImg.color = new Color(0.85f, 0.95f, 1f, 1f);
            mfImg.raycastTarget = true;

            Shadow mfShadow = frameObj.AddComponent<Shadow>();
            mfShadow.effectColor = new Color(0.005f, 0.015f, 0.05f, 0.95f);
            mfShadow.effectDistance = new Vector2(0f, -8f);

            // 3. Top Mascot & Logo on Shop Modal
            Sprite logoSp = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/01_zombie_squad_logo", "UI/CharactersDynamic/01_logo");
            Sprite soldierSp = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/02_soldier_character", "UI/CharactersDynamic/02_header_soldier");

            GameObject logoHeader = new GameObject("LogoHeaderBanner");
            logoHeader.transform.SetParent(modalContainer.transform, false);
            RectTransform lhRt = logoHeader.AddComponent<RectTransform>();
            lhRt.anchorMin = new Vector2(0.5f, 1f);
            lhRt.anchorMax = new Vector2(0.5f, 1f);
            lhRt.pivot = new Vector2(0.5f, 1f);
            lhRt.anchoredPosition = new Vector2(0f, -10f);
            lhRt.sizeDelta = new Vector2(600f, 130f);

            if (soldierSp != null)
            {
                GameObject soldierObj = new GameObject("SoldierMascot");
                soldierObj.transform.SetParent(logoHeader.transform, false);
                RectTransform smRt = soldierObj.AddComponent<RectTransform>();
                smRt.anchorMin = new Vector2(0.08f, 0f);
                smRt.anchorMax = new Vector2(0.32f, 1.15f);
                smRt.offsetMin = Vector2.zero; smRt.offsetMax = Vector2.zero;
                Image smImg = soldierObj.AddComponent<Image>();
                smImg.sprite = soldierSp;
                smImg.preserveAspect = true;
                smImg.raycastTarget = false;
            }

            if (logoSp != null)
            {
                GameObject logoObj = new GameObject("ZombieSquadLogo");
                logoObj.transform.SetParent(logoHeader.transform, false);
                RectTransform lRt = logoObj.AddComponent<RectTransform>();
                lRt.anchorMin = new Vector2(0.30f, 0.05f);
                lRt.anchorMax = new Vector2(0.92f, 0.95f);
                lRt.offsetMin = Vector2.zero; lRt.offsetMax = Vector2.zero;
                Image lImg = logoObj.AddComponent<Image>();
                lImg.sprite = logoSp;
                lImg.preserveAspect = true;
                lImg.raycastTarget = false;
            }

            // 4. Product Grid (2x2) using GridLayoutGroup
            GameObject gridObj = new GameObject("ProductGrid");
            gridObj.transform.SetParent(modalContainer.transform, false);
            RectTransform gRt = gridObj.AddComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0.05f, 0.10f);
            gRt.anchorMax = new Vector2(0.95f, 0.88f);
            gRt.offsetMin = Vector2.zero; gRt.offsetMax = Vector2.zero;

            GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(415f, 515f);
            glg.spacing = new Vector2(30f, 30f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;
            glg.childAlignment = TextAnchor.MiddleCenter;

            shopCoinsContainer = gridObj; // Cache for validity check

            // 4 Dynamic Product Cards with unique buy button names for compatibility checks
            CreateShopProductCard(gridObj.transform, "Card_1000", "1,000",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/10_coin_stack_product_art",
                "₹ 80", 80, 1000, "Btn_Buy_1000_Coins");

            CreateShopProductCard(gridObj.transform, "Card_5000", "5,000",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/10_coin_stack_product_art",
                "₹ 200", 200, 5000, "Btn_Buy_5000_Coins");

            CreateShopProductCard(gridObj.transform, "Card_20000", "20,000",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/11_treasure_chest_product_art",
                "₹ 1,000", 1000, 20000, "Btn_Buy_20000_Coins");

            CreateShopProductCard(gridObj.transform, "Card_50000", "50,000",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/12_large_treasure_chest_product_art",
                "₹ 2,000", 2000, 50000, "Btn_Buy_50000_Coins");

            // 5. Bottom Decorative Metal Trim & Rocks
            Sprite trimSp = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/26_bottom_metal_trim", "UI/CharactersDynamic/30_bottom_metal_trim");
            Sprite leftRock = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/27_left_rock_grass_decoration", "UI/CharactersDynamic/31_left_rock_grass");
            Sprite rightRock = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/28_right_rock_grass_decoration", "UI/CharactersDynamic/32_right_rock_grass");

            if (trimSp != null)
            {
                GameObject trimObj = new GameObject("BottomTrim");
                trimObj.transform.SetParent(modalContainer.transform, false);
                RectTransform tRt = trimObj.AddComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0f, 0f);
                tRt.anchorMax = new Vector2(1f, 0f);
                tRt.pivot = new Vector2(0.5f, 0f);
                tRt.anchoredPosition = new Vector2(0f, 15f);
                tRt.sizeDelta = new Vector2(0f, 90f);
                Image trimImg = trimObj.AddComponent<Image>();
                trimImg.sprite = trimSp;
                trimImg.preserveAspect = false;
                trimImg.raycastTarget = false;
            }

            if (leftRock != null)
            {
                GameObject lrObj = new GameObject("LeftRock");
                lrObj.transform.SetParent(modalContainer.transform, false);
                RectTransform lrRt = lrObj.AddComponent<RectTransform>();
                lrRt.anchorMin = new Vector2(0f, 0f);
                lrRt.anchorMax = new Vector2(0f, 0f);
                lrRt.pivot = new Vector2(0f, 0f);
                lrRt.anchoredPosition = new Vector2(-20f, 5f);
                lrRt.sizeDelta = new Vector2(140f, 120f);
                Image lrImg = lrObj.AddComponent<Image>();
                lrImg.sprite = leftRock;
                lrImg.preserveAspect = true;
                lrImg.raycastTarget = false;
            }

            if (rightRock != null)
            {
                GameObject rrObj = new GameObject("RightRock");
                rrObj.transform.SetParent(modalContainer.transform, false);
                RectTransform rrRt = rrObj.AddComponent<RectTransform>();
                rrRt.anchorMin = new Vector2(1f, 0f);
                rrRt.anchorMax = new Vector2(1f, 0f);
                rrRt.pivot = new Vector2(1f, 0f);
                rrRt.anchoredPosition = new Vector2(20f, 5f);
                rrRt.sizeDelta = new Vector2(140f, 120f);
                Image rrImg = rrObj.AddComponent<Image>();
                rrImg.sprite = rightRock;
                rrImg.preserveAspect = true;
                rrImg.raycastTarget = false;
            }

            // 6. Checkout Modal
            BuildShopCheckoutModal(shopPanel.transform);
        }

        private GameObject CreateShopProductCard(
            Transform parent,
            string cardName,
            string amountStr,
            string productArtPath,
            string priceStr,
            int inrPrice,
            int coinsAmount,
            string buttonGameObjectName)
        {
            GameObject cardObj = new GameObject(cardName);
            cardObj.transform.SetParent(parent, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(415f, 515f);

            // Card frame (9-sliced 09_empty_shop_card.png)
            Image cardBg = cardObj.AddComponent<Image>();
            Sprite cardSp = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/09_empty_shop_card");
            if (cardSp != null) cardBg.sprite = cardSp;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = Color.white;
            cardBg.raycastTarget = true;

            // Top Quantity Label (TMP)
            GameObject qtyObj = new GameObject("QuantityLabel");
            qtyObj.transform.SetParent(cardObj.transform, false);
            RectTransform qRt = qtyObj.AddComponent<RectTransform>();
            qRt.anchorMin = new Vector2(0.08f, 0.80f);
            qRt.anchorMax = new Vector2(0.92f, 0.95f);
            qRt.offsetMin = Vector2.zero; qRt.offsetMax = Vector2.zero;

            TextMeshProUGUI qTmp = qtyObj.AddComponent<TextMeshProUGUI>();
            qTmp.enabled = false;
            if (s_globalChildishFont != null) qTmp.font = s_globalChildishFont;
            qTmp.text = amountStr;
            qTmp.fontSize = 42f;
            qTmp.fontStyle = FontStyles.Bold;
            qTmp.alignment = TextAlignmentOptions.Center;
            qTmp.color = Color.white;
            qTmp.raycastTarget = false;
            qTmp.enabled = true;

            Outline qOutl = qtyObj.AddComponent<Outline>();
            qOutl.effectColor = new Color(0.01f, 0.08f, 0.28f, 1f);
            qOutl.effectDistance = new Vector2(2f, -2f);

            // Product Art in center (preserveAspect = true)
            Sprite artSp = LoadSpriteFromUI(productArtPath);
            GameObject artObj = new GameObject("ProductArt");
            artObj.transform.SetParent(cardObj.transform, false);
            RectTransform aRt = artObj.AddComponent<RectTransform>();
            aRt.anchorMin = new Vector2(0.12f, 0.24f);
            aRt.anchorMax = new Vector2(0.88f, 0.80f);
            aRt.offsetMin = Vector2.zero; aRt.offsetMax = Vector2.zero;

            Image aImg = artObj.AddComponent<Image>();
            if (artSp != null) aImg.sprite = artSp;
            aImg.preserveAspect = true;
            aImg.raycastTarget = false;

            // Buy Button at bottom (13_green_purchase_button_blank.png)
            Sprite btnSp = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/13_green_purchase_button_blank", "UI/Btn_3D_Green");
            GameObject btnObj = new GameObject(buttonGameObjectName);
            btnObj.transform.SetParent(cardObj.transform, false);
            RectTransform bRt = btnObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.08f, 0.05f);
            bRt.anchorMax = new Vector2(0.92f, 0.22f);
            bRt.offsetMin = Vector2.zero; bRt.offsetMax = Vector2.zero;

            Image bImg = btnObj.AddComponent<Image>();
            if (btnSp != null) bImg.sprite = btnSp;
            bImg.type = Image.Type.Sliced;
            bImg.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bImg;
            btn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(btnObj.transform, 0.12f, () =>
                {
                    OpenShopCheckoutModal($"{coinsAmount:N0} COINS", inrPrice, () =>
                    {
                        AddPlayerCoins(coinsAmount);
                        ShowMultiplierPop($"🎉 +{coinsAmount:N0} COINS!");
                    });
                }));
            });

            // Price Label inside Buy Button (TMP)
            GameObject pTextObj = new GameObject("PriceText");
            pTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform ptRt = pTextObj.AddComponent<RectTransform>();
            ptRt.anchorMin = Vector2.zero; ptRt.anchorMax = Vector2.one;
            ptRt.offsetMin = Vector2.zero; ptRt.offsetMax = Vector2.zero;

            TextMeshProUGUI ptTmp = pTextObj.AddComponent<TextMeshProUGUI>();
            ptTmp.enabled = false;
            if (s_globalChildishFont != null) ptTmp.font = s_globalChildishFont;
            ptTmp.text = priceStr;
            ptTmp.fontSize = 32f;
            ptTmp.fontStyle = FontStyles.Bold;
            ptTmp.alignment = TextAlignmentOptions.Center;
            ptTmp.color = Color.white;
            ptTmp.raycastTarget = false;
            ptTmp.enabled = true;

            Outline ptOutl = pTextObj.AddComponent<Outline>();
            ptOutl.effectColor = new Color(0.04f, 0.22f, 0.08f, 1f);
            ptOutl.effectDistance = new Vector2(2f, -2f);

            return cardObj;
        }

        private GameObject CreateShopPriceButton'''

code, count_b = pattern_block_b.subn(replacement_block_b, code)
print(f'Block B replaced: {count_b}')

if count_a == 1 and count_b == 1:
    with open('unity/Assets/Scripts/UI/ZombieRunnerUIManager.cs', 'w', encoding='utf-8') as f:
        f.write(code)
    print('SUCCESS: Both blocks replaced successfully!')
else:
    print('ERROR: Replacement count mismatch! File not modified.')
