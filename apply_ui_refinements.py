import re

with open('unity/Assets/Scripts/UI/ZombieRunnerUIManager.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Add charCardSelectionBorders dictionary field
target_field = 'private Dictionary<string, Image> charCardPriceBgs = new Dictionary<string, Image>();'
replacement_field = target_field + '\n        private Dictionary<string, GameObject> charCardSelectionBorders = new Dictionary<string, GameObject>();'

if target_field in code and 'charCardSelectionBorders' not in code:
    code = code.replace(target_field, replacement_field, 1)
    print('Added charCardSelectionBorders field.')

# 2. Update CreateCurrencyPill & CreateModularTopHeader
old_header_methods = re.search(r'private GameObject CreateModularTopHeader\(.*?private void CreateTopCurrencyHeader', code, re.DOTALL)
if old_header_methods:
    new_header_methods = '''private GameObject CreateModularTopHeader(Transform parent, string titleStr, System.Action onBack)
        {
            GameObject headerObj = new GameObject("TopHUD_ModularHeader");
            headerObj.transform.SetParent(parent, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -40f);
            hRt.sizeDelta = new Vector2(0f, 95f);

            // Back button (Beveled 3D blue back button)
            Sprite backSp = LoadSpriteFromUI(
                "UI/CharactersDynamic/05_back_button",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/08_back_button_icon",
                "UI/Btn_3D_Blue"
            );
            GameObject backObj = new GameObject("Btn_Back");
            backObj.transform.SetParent(headerObj.transform, false);
            RectTransform bRt = backObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0.5f);
            bRt.anchorMax = new Vector2(0f, 0.5f);
            bRt.pivot = new Vector2(0f, 0.5f);
            bRt.anchoredPosition = new Vector2(35f, 0f);
            bRt.sizeDelta = new Vector2(90f, 75f);

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
                tRt.anchoredPosition = new Vector2(140f, 0f);
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
            cgRt.sizeDelta = new Vector2(440f, 75f);

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
        }

        private GameObject CreateCurrencyPill(
            Transform parent,
            string name,
            string barSpritePath,
            string initialValue,
            Color textColor,
            List<TextMeshProUGUI> registerList,
            System.Action onPlusClick)
        {
            GameObject pillObj = new GameObject(name);
            pillObj.transform.SetParent(parent, false);
            RectTransform pRt = pillObj.AddComponent<RectTransform>();
            pRt.sizeDelta = new Vector2(205f, 68f);

            // Pill background (Authentic 10_coin_bar or 11_diamond_bar)
            Image bgImg = pillObj.AddComponent<Image>();
            Sprite barSp = LoadSpriteFromUI(barSpritePath, "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/04_empty_currency_bar");
            if (barSp != null) bgImg.sprite = barSp;
            bgImg.preserveAspect = true;
            bgImg.raycastTarget = false;

            // Plus Button clickable overlay on right
            GameObject plusObj = new GameObject("Btn_Plus");
            plusObj.transform.SetParent(pillObj.transform, false);
            RectTransform plusRt = plusObj.AddComponent<RectTransform>();
            plusRt.anchorMin = new Vector2(0.74f, 0.05f);
            plusRt.anchorMax = new Vector2(0.98f, 0.95f);
            plusRt.offsetMin = Vector2.zero;
            plusRt.offsetMax = Vector2.zero;

            Image plusImg = plusObj.AddComponent<Image>();
            plusImg.color = new Color(1f, 1f, 1f, 0.001f); // Transparent click target over the plus icon
            plusImg.raycastTarget = true;
            Button plusBtn = plusObj.AddComponent<Button>();
            plusBtn.targetGraphic = plusImg;
            if (onPlusClick != null)
            {
                plusBtn.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(pillObj.transform, 0.10f, () => onPlusClick()));
                });
            }

            // Dynamic TMP Text precisely centered in middle capsule slot
            GameObject txtObj = new GameObject("ValueText");
            txtObj.transform.SetParent(pillObj.transform, false);
            RectTransform tRt = txtObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.26f, 0f);
            tRt.anchorMax = new Vector2(0.74f, 1f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.enabled = false;
            if (s_globalChildishFont != null) tmp.font = s_globalChildishFont;
            tmp.text = initialValue;
            tmp.fontSize = 20f;
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

        private void CreateTopCurrencyHeader'''
    code = code[:old_header_methods.start()] + new_header_methods + code[old_header_methods.end() - len('private void CreateTopCurrencyHeader'):]
    print('Updated CreateModularTopHeader and CreateCurrencyPill.')

# 3. Update Showcase Chamber in BuildZombieSquadCharactersScreenPanel to use clean pedestal and clean frame
code = code.replace(
    'Sprite scSp = LoadSpriteFromUI("UI/CharactersDynamic/12_main_showcase_panel");',
    'Sprite scSp = LoadSpriteFromUI("UI/CharactersDynamic/12_main_showcase_panel_clean", "UI/CharactersDynamic/12_main_showcase_panel");'
)
code = code.replace(
    'Sprite pedSp = LoadSpriteFromUI("UI/CharactersDynamic/14_glow_platform");',
    'Sprite pedSp = LoadSpriteFromUI("UI/CharactersDynamic/14_glow_platform_clean", "UI/CharactersDynamic/14_glow_platform");'
)
print('Updated Showcase frame & pedestal to clean assets.')

# 4. Update CreateModularHeroCard with SelectionBorder
old_card_method = re.search(r'private GameObject CreateModularHeroCard\(.*?return cardObj;\s*\}', code, re.DOTALL)
if old_card_method:
    new_card_method = '''private GameObject CreateModularHeroCard(Transform parent, string heroKey, string cardSpritePath, string portraitSpritePath)
        {
            GameObject cardObj = new GameObject($"Card_{heroKey}");
            cardObj.transform.SetParent(parent, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(280f, 280f);

            // Selection Glow Border (Enabled when this hero is selected)
            GameObject selBorder = new GameObject("SelectionBorder");
            selBorder.transform.SetParent(cardObj.transform, false);
            RectTransform sbRt = selBorder.AddComponent<RectTransform>();
            sbRt.anchorMin = Vector2.zero;
            sbRt.anchorMax = Vector2.one;
            sbRt.offsetMin = new Vector2(-6f, -6f);
            sbRt.offsetMax = new Vector2(6f, 6f);
            Image sbImg = selBorder.AddComponent<Image>();
            sbImg.color = new Color(0f, 0.95f, 1f, 0.85f);
            sbImg.raycastTarget = false;
            Outline sbOutl = selBorder.AddComponent<Outline>();
            sbOutl.effectColor = new Color(0.1f, 1f, 0.5f, 0.95f);
            sbOutl.effectDistance = new Vector2(3f, -3f);
            charCardSelectionBorders[heroKey] = selBorder;
            selBorder.SetActive(heroKey == selectedCharacterKey);

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
        }'''
    code = code[:old_card_method.start()] + new_card_method + code[old_card_method.end():]
    print('Updated CreateModularHeroCard with selection border.')

# 5. Update UpdateCharactersScreenVisuals to refresh selection borders
target_update_visuals = 'bool isEquipped = (selectedCharacterKey == currentEquipped);'
replacement_update_visuals = '''bool isEquipped = (selectedCharacterKey == currentEquipped);

            foreach (var kvp in charCardSelectionBorders)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetActive(kvp.Key == selectedCharacterKey);
                }
            }'''
if target_update_visuals in code:
    code = code.replace(target_update_visuals, replacement_update_visuals, 1)
    print('Updated UpdateCharactersScreenVisuals with card border toggles.')

# 6. Update CreateShopProductCard to render icon_rupee.png + price number
old_shop_card = re.search(r'private GameObject CreateShopProductCard\(.*?return cardObj;\s*\}', code, re.DOTALL)
if old_shop_card:
    new_shop_card = '''private GameObject CreateShopProductCard(
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

            // Price Container with icon_rupee and Price Number
            GameObject pBox = new GameObject("PriceContainer");
            pBox.transform.SetParent(btnObj.transform, false);
            RectTransform pbRt = pBox.AddComponent<RectTransform>();
            pbRt.anchorMin = Vector2.zero;
            pbRt.anchorMax = Vector2.one;
            pbRt.offsetMin = Vector2.zero;
            pbRt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup phlg = pBox.AddComponent<HorizontalLayoutGroup>();
            phlg.spacing = 8f;
            phlg.childAlignment = TextAnchor.MiddleCenter;
            phlg.childControlWidth = false;
            phlg.childControlHeight = false;
            phlg.childForceExpandWidth = false;
            phlg.childForceExpandHeight = false;

            // Crisp Rupee Icon
            Sprite rupeeSp = LoadSpriteFromUI("UI/icon_rupee");
            if (rupeeSp != null)
            {
                GameObject rIconObj = new GameObject("RupeeIcon");
                rIconObj.transform.SetParent(pBox.transform, false);
                RectTransform riRt = rIconObj.AddComponent<RectTransform>();
                riRt.sizeDelta = new Vector2(24f, 30f);
                Image riImg = rIconObj.AddComponent<Image>();
                riImg.sprite = rupeeSp;
                riImg.preserveAspect = true;
                riImg.raycastTarget = false;
            }

            // Price Number TMP
            GameObject pTextObj = new GameObject("PriceText");
            pTextObj.transform.SetParent(pBox.transform, false);
            RectTransform ptRt = pTextObj.AddComponent<RectTransform>();
            ptRt.sizeDelta = new Vector2(130f, 45f);

            TextMeshProUGUI ptTmp = pTextObj.AddComponent<TextMeshProUGUI>();
            ptTmp.enabled = false;
            if (s_globalChildishFont != null) ptTmp.font = s_globalChildishFont;
            string cleanPriceNum = priceStr.Replace("₹", "").Replace("Rs.", "").Trim();
            ptTmp.text = cleanPriceNum;
            ptTmp.fontSize = 32f;
            ptTmp.fontStyle = FontStyles.Bold;
            ptTmp.alignment = TextAlignmentOptions.MidlineLeft;
            ptTmp.color = Color.white;
            ptTmp.raycastTarget = false;
            ptTmp.enabled = true;

            Outline ptOutl = pTextObj.AddComponent<Outline>();
            ptOutl.effectColor = new Color(0.04f, 0.22f, 0.08f, 1f);
            ptOutl.effectDistance = new Vector2(2f, -2f);

            return cardObj;
        }'''
    code = code[:old_shop_card.start()] + new_shop_card + code[old_shop_card.end():]
    print('Updated CreateShopProductCard with Rupee icon.')

with open('unity/Assets/Scripts/UI/ZombieRunnerUIManager.cs', 'w', encoding='utf-8') as f:
    f.write(code)
print('All UI refinements applied successfully!')
