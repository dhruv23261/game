import re

with open('unity/Assets/Scripts/UI/ZombieRunnerUIManager.cs', 'r') as f:
    code = f.read()

# 1. New CreateModularTopHeader implementation
new_header = """private GameObject CreateModularTopHeader(Transform parent, string titleStr, System.Action onBack)
        {
            GameObject headerObj = new GameObject("TopHUD_ModularHeader");
            headerObj.transform.SetParent(parent, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 1f);
            hRt.anchorMax = new Vector2(0.5f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, 0f);
            hRt.sizeDelta = new Vector2(960f, 175f);

            // 0. Mascot Soldier Peeking on Top-Left
            Sprite mascotSp = LoadSpriteFromUI(
                "UI/CharactersDynamic/02_header_soldier",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/02_soldier_character"
            );
            if (mascotSp != null)
            {
                GameObject mascotObj = new GameObject("HeaderMascot_Soldier");
                mascotObj.transform.SetParent(headerObj.transform, false);
                RectTransform mRt = mascotObj.AddComponent<RectTransform>();
                mRt.anchorMin = new Vector2(0f, 1f);
                mRt.anchorMax = new Vector2(0f, 1f);
                mRt.pivot = new Vector2(0.5f, 0.5f);
                mRt.anchoredPosition = new Vector2(175f, 12f);
                mRt.sizeDelta = new Vector2(250f, 185f);
                Image mImg = mascotObj.AddComponent<Image>();
                mImg.sprite = mascotSp;
                mImg.preserveAspect = true;
                mImg.raycastTarget = false;
            }

            // 1. Zombie Squad 3D Logo Centered on Top
            Sprite logoSp = LoadSpriteFromUI(
                "UI/CharactersDynamic/01_logo",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/01_zombie_squad_logo"
            );
            if (logoSp != null)
            {
                GameObject logoObj = new GameObject("HeaderLogo_ZombieSquad");
                logoObj.transform.SetParent(headerObj.transform, false);
                RectTransform lRt = logoObj.AddComponent<RectTransform>();
                lRt.anchorMin = new Vector2(0.5f, 1f);
                lRt.anchorMax = new Vector2(0.5f, 1f);
                lRt.pivot = new Vector2(0.5f, 0.5f);
                lRt.anchoredPosition = new Vector2(18f, 15f);
                lRt.sizeDelta = new Vector2(330f, 145f);
                Image lImg = logoObj.AddComponent<Image>();
                lImg.sprite = logoSp;
                lImg.preserveAspect = true;
                lImg.raycastTarget = false;
            }

            // 2. HUD Bar Strip below the logo
            GameObject hudStrip = new GameObject("HUD_BarStrip");
            hudStrip.transform.SetParent(headerObj.transform, false);
            RectTransform hudRt = hudStrip.AddComponent<RectTransform>();
            hudRt.anchorMin = new Vector2(0f, 0f);
            hudRt.anchorMax = new Vector2(1f, 0f);
            hudRt.pivot = new Vector2(0.5f, 0f);
            hudRt.anchoredPosition = new Vector2(0f, 0f);
            hudRt.sizeDelta = new Vector2(0f, 85f);

            // Back button (Beveled 3D blue back button)
            Sprite backSp = LoadSpriteFromUI(
                "UI/CharactersDynamic/05_back_button",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/08_back_button_icon",
                "UI/Btn_3D_Blue"
            );
            GameObject backObj = new GameObject("Btn_Back");
            backObj.transform.SetParent(hudStrip.transform, false);
            RectTransform bRt = backObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0.5f);
            bRt.anchorMax = new Vector2(0f, 0.5f);
            bRt.pivot = new Vector2(0f, 0.5f);
            bRt.anchoredPosition = new Vector2(25f, 0f);
            bRt.sizeDelta = new Vector2(85f, 75f);

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
                titleObj.transform.SetParent(hudStrip.transform, false);
                RectTransform tRt = titleObj.AddComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0f, 0.5f);
                tRt.anchorMax = new Vector2(0f, 0.5f);
                tRt.pivot = new Vector2(0f, 0.5f);
                tRt.anchoredPosition = new Vector2(125f, 0f);
                tRt.sizeDelta = new Vector2(340f, 75f);

                TextMeshProUGUI tTmp = titleObj.AddComponent<TextMeshProUGUI>();
                tTmp.enabled = false;
                if (s_globalChildishFont != null) tTmp.font = s_globalChildishFont;
                tTmp.text = titleStr;
                tTmp.fontSize = 36f;
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
            curGroup.transform.SetParent(hudStrip.transform, false);
            RectTransform cgRt = curGroup.AddComponent<RectTransform>();
            cgRt.anchorMin = new Vector2(1f, 0.5f);
            cgRt.anchorMax = new Vector2(1f, 0.5f);
            cgRt.pivot = new Vector2(1f, 0.5f);
            cgRt.anchoredPosition = new Vector2(-15f, 0f);
            cgRt.sizeDelta = new Vector2(430f, 75f);

            HorizontalLayoutGroup hlg = curGroup.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15f;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 1. Coin Pill (Uses authentic 10_coin_bar with built-in coin star and plus button!)
            CreateCurrencyPill(
                curGroup.transform,
                "CoinPill",
                "UI/CharactersDynamic/10_coin_bar",
                GetPlayerCoins().ToString("N0"),
                new Color(1f, 0.96f, 0.65f, 1f),
                allCoinsDisplays,
                () =>
                {
                    ShowZombieSquadShopScreen();
                    SelectShopTab("Coins");
                }
            );

            // 2. Diamond Pill (Uses authentic 11_diamond_bar with built-in diamond gem and plus button!)
            CreateCurrencyPill(
                curGroup.transform,
                "DiamondPill",
                "UI/CharactersDynamic/11_diamond_bar",
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
        }"""

# 2. New BuildZombieSquadCharactersScreenPanel
new_chars = """private void BuildZombieSquadCharactersScreenPanel(Transform canvasRoot)
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

            // 1. Central Responsive Content Container
            GameObject contentContainer = new GameObject("CharactersContainer");
            contentContainer.transform.SetParent(charactersPanel.transform, false);
            RectTransform ccRt = contentContainer.AddComponent<RectTransform>();
            ccRt.anchorMin = new Vector2(0.5f, 0.5f);
            ccRt.anchorMax = new Vector2(0.5f, 0.5f);
            ccRt.pivot = new Vector2(0.5f, 0.5f);
            ccRt.anchoredPosition = new Vector2(0f, -10f);
            ccRt.sizeDelta = new Vector2(960f, 1460f);

            // Side Hazard Columns (Left and Right)
            Sprite leftHazard = LoadSpriteFromUI("UI/VictoryScreen/20_left_hazard_border");
            Sprite rightHazard = LoadSpriteFromUI("UI/VictoryScreen/21_right_hazard_border");
            if (leftHazard != null)
            {
                GameObject lCol = new GameObject("SideCol_Left");
                lCol.transform.SetParent(contentContainer.transform, false);
                RectTransform lcRt = lCol.AddComponent<RectTransform>();
                lcRt.anchorMin = new Vector2(0f, 0f);
                lcRt.anchorMax = new Vector2(0f, 1f);
                lcRt.pivot = new Vector2(0f, 0.5f);
                lcRt.anchoredPosition = new Vector2(-15f, -30f);
                lcRt.sizeDelta = new Vector2(55f, -180f);
                Image lcImg = lCol.AddComponent<Image>();
                lcImg.sprite = leftHazard;
                lcImg.preserveAspect = false;
                lcImg.raycastTarget = false;
            }
            if (rightHazard != null)
            {
                GameObject rCol = new GameObject("SideCol_Right");
                rCol.transform.SetParent(contentContainer.transform, false);
                RectTransform rcRt = rCol.AddComponent<RectTransform>();
                rcRt.anchorMin = new Vector2(1f, 0f);
                rcRt.anchorMax = new Vector2(1f, 1f);
                rcRt.pivot = new Vector2(1f, 0.5f);
                rcRt.anchoredPosition = new Vector2(15f, -30f);
                rcRt.sizeDelta = new Vector2(55f, -180f);
                Image rcImg = rCol.AddComponent<Image>();
                rcImg.sprite = rightHazard;
                rcImg.preserveAspect = false;
                rcImg.raycastTarget = false;
            }

            // 2. Top Unified Header (Mascot Soldier + Zombie Squad Logo + Back + "CHARACTERS" + Currency Pills)
            CreateModularTopHeader(contentContainer.transform, "CHARACTERS", () => ShowZombieSquadHomeScreen());

            // 3. Center Showcase Chamber (Pedestal + Active Hero + Arrows)
            GameObject showcaseChamber = new GameObject("ShowcaseChamber");
            showcaseChamber.transform.SetParent(contentContainer.transform, false);
            RectTransform scRt = showcaseChamber.AddComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0.5f, 1f);
            scRt.anchorMax = new Vector2(0.5f, 1f);
            scRt.pivot = new Vector2(0.5f, 1f);
            scRt.anchoredPosition = new Vector2(0f, -170f);
            scRt.sizeDelta = new Vector2(910f, 475f);

            // Frame (12_main_showcase_panel_clean.png)
            Image scImg = showcaseChamber.AddComponent<Image>();
            Sprite scSp = LoadSpriteFromUI("UI/CharactersDynamic/12_main_showcase_panel_clean", "UI/CharactersDynamic/12_main_showcase_panel");
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

            // Glowing Pedestal (14_glow_platform_clean.png) - Clean, ZERO baked characters
            GameObject pedObj = new GameObject("GlowPedestal");
            pedObj.transform.SetParent(showcaseChamber.transform, false);
            RectTransform pRt = pedObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0f);
            pRt.anchorMax = new Vector2(0.5f, 0f);
            pRt.pivot = new Vector2(0.5f, 0f);
            pRt.anchoredPosition = new Vector2(0f, 18f);
            pRt.sizeDelta = new Vector2(510f, 110f);
            Image pedImg = pedObj.AddComponent<Image>();
            Sprite pedSp = LoadSpriteFromUI("UI/CharactersDynamic/14_glow_platform_clean", "UI/CharactersDynamic/14_glow_platform");
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
            aRt.anchoredPosition = new Vector2(0f, 28f);
            aRt.sizeDelta = new Vector2(380f, 420f);

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
            laRt.anchoredPosition = new Vector2(25f, -15f);
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
            raRt.anchoredPosition = new Vector2(-25f, -15f);
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

            // 4. Action / Equip Button below chamber
            GameObject actBtnObj = new GameObject("Btn_EquipHero");
            actBtnObj.transform.SetParent(contentContainer.transform, false);
            RectTransform abRt = actBtnObj.AddComponent<RectTransform>();
            abRt.anchorMin = new Vector2(0.5f, 1f);
            abRt.anchorMax = new Vector2(0.5f, 1f);
            abRt.pivot = new Vector2(0.5f, 1f);
            abRt.anchoredPosition = new Vector2(0f, -655f);
            abRt.sizeDelta = new Vector2(440f, 85f);

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

            // 5. 5 Hero Cards (Row 1: Soldier, Ranger, Heavy; Row 2: Sniper, Medic)
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
            r1Rt.sizeDelta = new Vector2(910f, 265f);

            HorizontalLayoutGroup hlg1 = row1.AddComponent<HorizontalLayoutGroup>();
            hlg1.spacing = 20f;
            hlg1.childAlignment = TextAnchor.MiddleCenter;
            hlg1.childControlWidth = false;
            hlg1.childControlHeight = false;
            hlg1.childForceExpandWidth = false;
            hlg1.childForceExpandHeight = false;

            CreateModularHeroCard(row1.transform, "Soldier",
                "UI/CharactersDynamic/33_character_card_frame_clean",
                "UI/CharactersDynamic/22_green_soldier_portrait");

            CreateModularHeroCard(row1.transform, "Ranger",
                "UI/CharactersDynamic/33_character_card_frame_clean",
                "UI/CharactersDynamic/23_red_soldier_portrait");

            CreateModularHeroCard(row1.transform, "Heavy",
                "UI/CharactersDynamic/33_character_card_frame_clean",
                "UI/CharactersDynamic/24_yellow_soldier_portrait");

            // Row 2 (2 cards)
            GameObject row2 = new GameObject("HeroCards_Row2");
            row2.transform.SetParent(contentContainer.transform, false);
            RectTransform r2Rt = row2.AddComponent<RectTransform>();
            r2Rt.anchorMin = new Vector2(0.5f, 1f);
            r2Rt.anchorMax = new Vector2(0.5f, 1f);
            r2Rt.pivot = new Vector2(0.5f, 1f);
            r2Rt.anchoredPosition = new Vector2(0f, -1035f);
            r2Rt.sizeDelta = new Vector2(910f, 265f);

            HorizontalLayoutGroup hlg2 = row2.AddComponent<HorizontalLayoutGroup>();
            hlg2.spacing = 30f;
            hlg2.childAlignment = TextAnchor.MiddleCenter;
            hlg2.childControlWidth = false;
            hlg2.childControlHeight = false;
            hlg2.childForceExpandWidth = false;
            hlg2.childForceExpandHeight = false;

            CreateModularHeroCard(row2.transform, "Sniper",
                "UI/CharactersDynamic/33_character_card_frame_clean",
                "UI/CharactersDynamic/25_purple_soldier_portrait");

            CreateModularHeroCard(row2.transform, "Medic",
                "UI/CharactersDynamic/33_character_card_frame_clean",
                "UI/CharactersDynamic/26_pink_medic_portrait");

            // 6. Bottom Decorative Metal Trim (Unified with Rocks & Grass)
            Sprite cTrimSp = LoadSpriteFromUI("UI/CharactersDynamic/30_bottom_metal_trim", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/26_bottom_metal_trim");
            if (cTrimSp != null)
            {
                GameObject trimObj = new GameObject("BottomTrim");
                trimObj.transform.SetParent(contentContainer.transform, false);
                RectTransform tRt = trimObj.AddComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0f, 0f);
                tRt.anchorMax = new Vector2(1f, 0f);
                tRt.pivot = new Vector2(0.5f, 0f);
                tRt.anchoredPosition = new Vector2(0f, 0f);
                tRt.sizeDelta = new Vector2(20f, 95f);
                Image trimImg = trimObj.AddComponent<Image>();
                trimImg.sprite = cTrimSp;
                trimImg.preserveAspect = false;
                trimImg.raycastTarget = false;
            }

            UpdateCharactersScreenVisuals();
        }"""

# 3. New CreateModularHeroCard
new_card = """private GameObject CreateModularHeroCard(Transform parent, string heroKey, string cardSpritePath, string portraitSpritePath)
        {
            GameObject cardObj = new GameObject($"Card_{heroKey}");
            cardObj.transform.SetParent(parent, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(275f, 265f);

            // Card Frame (Base blue cyber rounded rectangle with sunburst)
            Image cardBg = cardObj.AddComponent<Image>();
            Sprite cardSp = LoadSpriteFromUI(cardSpritePath, "UI/CharactersDynamic/33_character_card_frame_clean");
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

            // Hero Portrait inside card
            Sprite portSp = LoadSpriteFromUI(portraitSpritePath);
            GameObject portObj = new GameObject("Portrait");
            portObj.transform.SetParent(cardObj.transform, false);
            RectTransform pRt = portObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.08f, 0.26f);
            pRt.anchorMax = new Vector2(0.92f, 0.94f);
            pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
            Image portImg = portObj.AddComponent<Image>();
            if (portSp != null) portImg.sprite = portSp;
            portImg.preserveAspect = true;
            portImg.raycastTarget = false;

            // Status / Price Button Pill at bottom of card
            GameObject pillObj = new GameObject("PricePill");
            pillObj.transform.SetParent(cardObj.transform, false);
            RectTransform pillRt = pillObj.AddComponent<RectTransform>();
            pillRt.anchorMin = new Vector2(0.08f, 0.05f);
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

            // Selection Glow Border (Hollow rounded frame - active ONLY when selected)
            GameObject selBorder = new GameObject("SelectionBorder");
            selBorder.transform.SetParent(cardObj.transform, false);
            RectTransform sbRt = selBorder.AddComponent<RectTransform>();
            sbRt.anchorMin = Vector2.zero;
            sbRt.anchorMax = Vector2.one;
            sbRt.offsetMin = new Vector2(-6f, -6f);
            sbRt.offsetMax = new Vector2(6f, 6f);
            Image sbImg = selBorder.AddComponent<Image>();
            Sprite borderSp = LoadSpriteFromUI("UI/CharactersDynamic/17_selection_glow_border");
            if (borderSp != null)
            {
                sbImg.sprite = borderSp;
                sbImg.type = Image.Type.Sliced;
                sbImg.color = Color.white;
            }
            else
            {
                sbImg.color = Color.clear;
            }
            sbImg.raycastTarget = false;
            charCardSelectionBorders[heroKey] = selBorder;
            selBorder.SetActive(heroKey == selectedCharacterKey);

            return cardObj;
        }"""

# 4. New UpdateCharactersScreenVisuals
new_upd = """public void UpdateCharactersScreenVisuals()
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

                Sprite charSpr = Resources.Load<Sprite>($"UI/Hero_{selectedCharacterKey}_Full")
                              ?? LoadSpriteFromFile($"Resources/UI/Hero_{selectedCharacterKey}_Full.png")
                              ?? Resources.Load<Sprite>($"UI/Hero_{selectedCharacterKey}_Icon")
                              ?? LoadSpriteFromFile($"Resources/UI/Hero_{selectedCharacterKey}_Icon.png");
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

            foreach (var kvp in charCardSelectionBorders)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetActive(kvp.Key == selectedCharacterKey);
                }
            }

            if (charPreviewEquipBtnText != null && charPreviewEquipBtnBg != null)
            {
                Outline btnOutline = charPreviewEquipBtnText.GetComponent<Outline>();

                if (isEquipped)
                {
                    charPreviewEquipBtnBg.sprite = LoadSpriteFromUI("UI/CharactersDynamic/27_equipped_button", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/13_green_purchase_button_blank", "UI/Btn_3D_Green");
                    charPreviewEquipBtnBg.color = Color.white;
                    charPreviewEquipBtnText.text = "EQUIPPED";
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
                        charPreviewEquipBtnText.text = $"UNLOCK ({lData.priceDiamonds} GEMS)";
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
                            pillTxt.text = $"{hd.priceDiamonds} GEMS";
                        }
                        pillBg.color = Color.white;
                    }
                    pillTxt.color = Color.white;
                }
            }
        }"""

# 5. New BuildZombieSquadShopScreenPanel
new_shop = """private void BuildZombieSquadShopScreenPanel(Transform canvasRoot)
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

            // 1. Central Responsive Modal Container
            GameObject modalContainer = new GameObject("ShopModalContainer");
            modalContainer.transform.SetParent(shopPanel.transform, false);
            RectTransform mcRt = modalContainer.AddComponent<RectTransform>();
            mcRt.anchorMin = new Vector2(0.5f, 0.5f);
            mcRt.anchorMax = new Vector2(0.5f, 0.5f);
            mcRt.pivot = new Vector2(0.5f, 0.5f);
            mcRt.anchoredPosition = new Vector2(0f, -10f);
            mcRt.sizeDelta = new Vector2(960f, 1460f);

            // Side Hazard Columns (Left and Right)
            Sprite leftHazard = LoadSpriteFromUI("UI/VictoryScreen/20_left_hazard_border");
            Sprite rightHazard = LoadSpriteFromUI("UI/VictoryScreen/21_right_hazard_border");
            if (leftHazard != null)
            {
                GameObject lCol = new GameObject("SideCol_Left");
                lCol.transform.SetParent(modalContainer.transform, false);
                RectTransform lcRt = lCol.AddComponent<RectTransform>();
                lcRt.anchorMin = new Vector2(0f, 0f);
                lcRt.anchorMax = new Vector2(0f, 1f);
                lcRt.pivot = new Vector2(0f, 0.5f);
                lcRt.anchoredPosition = new Vector2(-15f, -30f);
                lcRt.sizeDelta = new Vector2(55f, -180f);
                Image lcImg = lCol.AddComponent<Image>();
                lcImg.sprite = leftHazard;
                lcImg.preserveAspect = false;
                lcImg.raycastTarget = false;
            }
            if (rightHazard != null)
            {
                GameObject rCol = new GameObject("SideCol_Right");
                rCol.transform.SetParent(modalContainer.transform, false);
                RectTransform rcRt = rCol.AddComponent<RectTransform>();
                rcRt.anchorMin = new Vector2(1f, 0f);
                rcRt.anchorMax = new Vector2(1f, 1f);
                rcRt.pivot = new Vector2(1f, 0.5f);
                rcRt.anchoredPosition = new Vector2(15f, -30f);
                rcRt.sizeDelta = new Vector2(55f, -180f);
                Image rcImg = rCol.AddComponent<Image>();
                rcImg.sprite = rightHazard;
                rcImg.preserveAspect = false;
                rcImg.raycastTarget = false;
            }

            // 2. Top Unified Header (Mascot Soldier + Zombie Squad Logo + Back + "SHOP" + Currency Pills)
            CreateModularTopHeader(modalContainer.transform, "SHOP", () => ShowZombieSquadHomeScreen());

            // 3. Product Grid (2x2) using GridLayoutGroup
            GameObject gridObj = new GameObject("ProductGrid");
            gridObj.transform.SetParent(modalContainer.transform, false);
            RectTransform gRt = gridObj.AddComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0.5f, 1f);
            gRt.anchorMax = new Vector2(0.5f, 1f);
            gRt.pivot = new Vector2(0.5f, 1f);
            gRt.anchoredPosition = new Vector2(0f, -170f);
            gRt.sizeDelta = new Vector2(910f, 1180f);

            GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(425f, 545f);
            glg.spacing = new Vector2(25f, 25f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;
            glg.childAlignment = TextAnchor.MiddleCenter;

            shopCoinsContainer = gridObj; // Cache for validity check

            // 4 Dynamic Product Cards with transparent art and unique buy button names for compatibility checks
            CreateShopProductCard(gridObj.transform, "Card_1000", "1,000",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/product_coins_1k",
                "₹ 80", 80, 1000, "Btn_Buy_1000_Coins");

            CreateShopProductCard(gridObj.transform, "Card_5000", "5,000",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/product_coins_5k",
                "₹ 200", 200, 5000, "Btn_Buy_5000_Coins");

            CreateShopProductCard(gridObj.transform, "Card_20000", "20,000",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/product_chest_20k",
                "₹ 1,000", 1000, 20000, "Btn_Buy_20000_Coins");

            CreateShopProductCard(gridObj.transform, "Card_50000", "50,000",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/product_chest_50k",
                "₹ 2,000", 2000, 50000, "Btn_Buy_50000_Coins");

            // 4. Bottom Decorative Metal Trim
            Sprite trimSp = LoadSpriteFromUI("UI/CharactersDynamic/30_bottom_metal_trim", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/26_bottom_metal_trim");
            if (trimSp != null)
            {
                GameObject trimObj = new GameObject("BottomTrim");
                trimObj.transform.SetParent(modalContainer.transform, false);
                RectTransform tRt = trimObj.AddComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0f, 0f);
                tRt.anchorMax = new Vector2(1f, 0f);
                tRt.pivot = new Vector2(0.5f, 0f);
                tRt.anchoredPosition = new Vector2(0f, 0f);
                tRt.sizeDelta = new Vector2(20f, 95f);
                Image trimImg = trimObj.AddComponent<Image>();
                trimImg.sprite = trimSp;
                trimImg.preserveAspect = false;
                trimImg.raycastTarget = false;
            }

            // 5. Checkout Modal
            BuildShopCheckoutModal(shopPanel.transform);
        }"""

# Execute replacements
code = re.sub(r'private GameObject CreateModularTopHeader\(Transform parent, string titleStr, System\.Action onBack\).*?return headerObj;\s*\}', new_header, code, flags=re.DOTALL)
code = re.sub(r'private void BuildZombieSquadCharactersScreenPanel\(Transform canvasRoot\).*?UpdateCharactersScreenVisuals\(\);\s*\}', new_chars, code, flags=re.DOTALL)
code = re.sub(r'private GameObject CreateModularHeroCard\(Transform parent, string heroKey, string cardSpritePath, string portraitSpritePath\).*?return cardObj;\s*\}', new_card, code, flags=re.DOTALL)
code = re.sub(r'public void UpdateCharactersScreenVisuals\(\).*?pillTxt\.color = Color\.white;\s*\}\s*\}\s*\}', new_upd, code, flags=re.DOTALL)
code = re.sub(r'private void BuildZombieSquadShopScreenPanel\(Transform canvasRoot\).*?BuildShopCheckoutModal\(shopPanel\.transform\);\s*\}', new_shop, code, flags=re.DOTALL)

with open('unity/Assets/Scripts/UI/ZombieRunnerUIManager.cs', 'w') as f:
    f.write(code)

print("ZombieRunnerUIManager.cs successfully updated with perfect modular UI architecture!")
