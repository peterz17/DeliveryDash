using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class UIManager
{
    GameObject unlockPopup;

    public void ShowCarSelectScreen()
    {
        SetAllScreens(carSelect: true);
        PopulateCarItems();
        UpdateCoinDisplay(GameManager.Instance != null ? GameManager.Instance.Coins : 0);
        WireCarSelectDebugButtons();
    }

    void WireCarSelectDebugButtons()
    {
        if (carSelectScreen == null) return;
        var unlockBtn = carSelectScreen.transform.Find("UnlockAllButton")?.GetComponent<Button>();
        var resetBtn = carSelectScreen.transform.Find("ResetAllButton")?.GetComponent<Button>();

        if (unlockBtn != null)
        {
            unlockBtn.onClick.RemoveAllListeners();
            unlockBtn.onClick.AddListener(() =>
            {
                GM(g => { g.UnlockAllCars(); PopulateCarItems(); UpdateCoinDisplay(g.Coins); });
            });
            AddClickSound(unlockBtn);
        }
        if (resetBtn != null)
        {
            resetBtn.onClick.RemoveAllListeners();
            resetBtn.onClick.AddListener(() =>
            {
                GM(g => { g.ResetAllCars(); PopulateCarItems(); UpdateCoinDisplay(g.Coins); });
            });
            AddClickSound(resetBtn);
        }
    }

    public void UpdateCoinDisplay(int coins)
    {
        if (coinBalanceText != null)
            coinBalanceText.text = string.Format(LocalizationManager.L("hud_coins", "Coins: {0}"), coins);
        if (hudCoinText != null)
            hudCoinText.text = string.Format(LocalizationManager.L("hud_coins", "Coins: {0}"), coins);
    }

    void PopulateCarItems()
    {
        if (carSelectContent == null) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.carCatalog == null) return;

        for (int i = carSelectContent.childCount - 1; i >= 0; i--)
            Destroy(carSelectContent.GetChild(i).gameObject);

        var grid = carSelectContent.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            bool landscape = Screen.width > Screen.height;
            grid.constraintCount = landscape ? 5 : 3;
        }

        foreach (var car in gm.carCatalog)
        {
            if (car == null) continue;
            CreateCarCard(car, gm);
        }
    }

    void CreateCarCard(CarData car, GameManager gm)
    {
        bool unlocked = gm.IsCarUnlocked(car);
        bool selected = gm.selectedCar == car;

        var card = new GameObject(car.carId, typeof(RectTransform));
        card.transform.SetParent(carSelectContent, false);
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(280, 480);

        var bg = card.AddComponent<Image>();
        bg.color = selected ? new Color(0.10f, 0.55f, 0.20f, 1f)
                 : unlocked ? new Color(0.12f, 0.13f, 0.22f, 1f)
                 : new Color(0.08f, 0.08f, 0.12f, 1f);

        if (selected) CreateSelectedBadge(card.transform);
        CreateCarIcon(card.transform, car, unlocked);
        CreateCarName(card.transform, car);
        CreateCarStats(card.transform, car);
        CreateCarButton(card.transform, car, gm, unlocked, selected);
    }

    void CreateSelectedBadge(Transform parent)
    {
        var badgeGO = new GameObject("SelectedBadge", typeof(RectTransform));
        badgeGO.transform.SetParent(parent, false);
        var badgeRT = badgeGO.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0.60f, 0.90f);
        badgeRT.anchorMax = new Vector2(1.00f, 1.00f);
        badgeRT.offsetMin = badgeRT.offsetMax = Vector2.zero;
        badgeGO.AddComponent<Image>().color = new Color(0.1f, 0.8f, 0.2f, 1f);

        var badgeTxt = new GameObject("Txt", typeof(RectTransform));
        badgeTxt.transform.SetParent(badgeGO.transform, false);
        var badgeTxtRT = badgeTxt.GetComponent<RectTransform>();
        badgeTxtRT.anchorMin = Vector2.zero; badgeTxtRT.anchorMax = Vector2.one;
        badgeTxtRT.offsetMin = badgeTxtRT.offsetMax = Vector2.zero;
        var badgeTMP = badgeTxt.AddComponent<TextMeshProUGUI>();
        badgeTMP.text = "\u2713";
        badgeTMP.fontSize = 20;
        badgeTMP.fontStyle = FontStyles.Bold;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        badgeTMP.color = Color.white;
    }

    void CreateCarIcon(Transform parent, CarData car, bool unlocked)
    {
        var iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(parent, false);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.10f, 0.48f);
        iconRT.anchorMax = new Vector2(0.90f, 0.92f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        var iconImg = iconGO.AddComponent<Image>();
        if (car.carSprite != null)
        {
            iconImg.sprite = car.carSprite;
            iconImg.preserveAspect = true;
            iconImg.color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
        }
        else
            iconImg.color = new Color(0.5f, 0.5f, 0.5f, 1f);
    }

    void CreateCarName(Transform parent, CarData car)
    {
        var nameGO = new GameObject("Name", typeof(RectTransform));
        nameGO.transform.SetParent(parent, false);
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.05f, 0.40f);
        nameRT.anchorMax = new Vector2(0.95f, 0.48f);
        nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = LocalizationManager.L(car.localizationKey, car.carId);
        nameTMP.fontSize = 22;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color = Color.white;
    }

    void CreateCarStats(Transform parent, CarData car)
    {
        CreateStatRow(parent, LocalizationManager.L("car_stat_speed", "SPD"), car.displaySpeed, 0.31f, 0.39f);
        CreateStatRow(parent, "\u2665 +" + car.bonusHearts, car.bonusHearts, 0.23f, 0.30f);
        CreateStatRow(parent, LocalizationManager.L("car_stat_size", "SIZE"), car.displaySize, 0.15f, 0.22f);
    }

    void CreateCarButton(Transform parent, CarData car, GameManager gm, bool unlocked, bool selected)
    {
        var btnGO = new GameObject("Btn", typeof(RectTransform));
        btnGO.transform.SetParent(parent, false);
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.05f, 0.03f);
        btnRT.anchorMax = new Vector2(0.95f, 0.11f);
        btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;
        var btnImg = btnGO.AddComponent<Image>();
        var btn = btnGO.AddComponent<Button>();

        var btnLabelGO = new GameObject("Label", typeof(RectTransform));
        btnLabelGO.transform.SetParent(btnGO.transform, false);
        var btnLabelRT = btnLabelGO.GetComponent<RectTransform>();
        btnLabelRT.anchorMin = Vector2.zero;
        btnLabelRT.anchorMax = Vector2.one;
        btnLabelRT.offsetMin = btnLabelRT.offsetMax = Vector2.zero;
        var btnLabel = btnLabelGO.AddComponent<TextMeshProUGUI>();
        btnLabel.fontSize = 18;
        btnLabel.fontStyle = FontStyles.Bold;
        btnLabel.alignment = TextAlignmentOptions.Center;
        btnLabel.color = Color.white;

        if (selected)
        {
            btnImg.color = new Color(0.1f, 0.65f, 0.2f, 1f);
            btnLabel.text = LocalizationManager.L("car_selected", "SELECTED");
            btn.interactable = false;
        }
        else if (unlocked)
        {
            btnImg.color = new Color(0.15f, 0.45f, 0.75f, 1f);
            btnLabel.text = LocalizationManager.L("car_select", "SELECT");
            var c = car;
            btn.onClick.AddListener(() => { gm.SelectCar(c); PopulateCarItems(); });
            AddClickSound(btn);
        }
        else
        {
            bool canUnlock = gm.CanUnlockCar(car);
            btnImg.color = canUnlock ? new Color(0.75f, 0.60f, 0.10f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
            btnLabel.text = string.Format(LocalizationManager.L("car_cost_fmt", "{0} Coins"), car.unlockCost);
            btnLabel.color = canUnlock ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            var c = car;
            btn.onClick.AddListener(() => ShowBuyConfirmPopup(c));
            AddClickSound(btn);
            btn.interactable = canUnlock;

            bool hasReq = car.requiredLevel > 0 || car.requiredEndlessTier10 > 0;
            if (hasReq)
            {
                btnRT.anchorMin = new Vector2(0.05f, 0.06f);
                btnRT.anchorMax = new Vector2(0.95f, 0.14f);
                CreateDetailsButton(parent, car);
            }
        }
    }

    void CreateDetailsButton(Transform parent, CarData car)
    {
        var detBtnGO = new GameObject("DetailBtn", typeof(RectTransform));
        detBtnGO.transform.SetParent(parent, false);
        var detRT = detBtnGO.GetComponent<RectTransform>();
        detRT.anchorMin = new Vector2(0.10f, 0.00f);
        detRT.anchorMax = new Vector2(0.90f, 0.06f);
        detRT.offsetMin = detRT.offsetMax = Vector2.zero;
        detBtnGO.AddComponent<Image>().color = new Color(0.20f, 0.25f, 0.40f, 1f);
        var detBtn = detBtnGO.AddComponent<Button>();

        var detLabelGO = new GameObject("Label", typeof(RectTransform));
        detLabelGO.transform.SetParent(detBtnGO.transform, false);
        var detLabelRT = detLabelGO.GetComponent<RectTransform>();
        detLabelRT.anchorMin = Vector2.zero;
        detLabelRT.anchorMax = Vector2.one;
        detLabelRT.offsetMin = detLabelRT.offsetMax = Vector2.zero;
        var detLabel = detLabelGO.AddComponent<TextMeshProUGUI>();
        detLabel.text = LocalizationManager.L("car_details", "DETAILS");
        detLabel.fontSize = 14;
        detLabel.alignment = TextAlignmentOptions.Center;
        detLabel.color = new Color(0.8f, 0.85f, 1f, 1f);
        var carRef = car;
        detBtn.onClick.AddListener(() => ShowUnlockPopup(carRef));
        AddClickSound(detBtn);
    }

    void ShowBuyConfirmPopup(CarData car)
    {
        if (unlockPopup != null) Destroy(unlockPopup);
        var gm = GameManager.Instance;
        if (gm == null) return;

        unlockPopup = CreatePopupOverlay(carSelectScreen.transform);
        var panelGO = CreatePopupPanel(unlockPopup.transform, new Vector2(0.15f, 0.25f), new Vector2(0.85f, 0.75f));

        string carName = LocalizationManager.L(car.localizationKey, car.carId);

        CreatePopupText(panelGO.transform, "Title",
            string.Format(LocalizationManager.L("car_buy_confirm", "Buy {0}?"), carName),
            32, FontStyles.Bold, new Color(0.85f, 0.65f, 0.20f),
            new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.90f));

        var costGO = CreatePopupText(panelGO.transform, "Cost",
            string.Format(LocalizationManager.L("car_buy_cost", "Cost: {0} coins\nYour coins: {1}"), car.unlockCost, gm.Coins),
            24, FontStyles.Normal, Color.white,
            new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.62f));
        var costTMP = costGO.GetComponent<TextMeshProUGUI>();
        costTMP.enableAutoSizing = true;
        costTMP.fontSizeMin = 14;
        costTMP.fontSizeMax = 24;

        var carRef = car;
        CreatePopupButton(panelGO.transform, "YesBtn",
            LocalizationManager.L("car_buy_yes", "YES"),
            new Color(0.15f, 0.55f, 0.15f),
            new Vector2(0.08f, 0.08f), new Vector2(0.45f, 0.30f),
            () => {
                if (gm.TryUnlockCar(carRef))
                {
                    gm.SelectCar(carRef);
                    if (unlockPopup != null) Destroy(unlockPopup);
                    PopulateCarItems();
                    UpdateCoinDisplay(gm.Coins);
                }
            });

        CreatePopupButton(panelGO.transform, "NoBtn",
            LocalizationManager.L("car_buy_no", "NO"),
            new Color(0.55f, 0.15f, 0.15f),
            new Vector2(0.55f, 0.08f), new Vector2(0.92f, 0.30f),
            () => { if (unlockPopup != null) Destroy(unlockPopup); });
    }

    void ShowUnlockPopup(CarData car)
    {
        if (unlockPopup != null) Destroy(unlockPopup);
        var gm = GameManager.Instance;
        if (gm == null) return;

        unlockPopup = CreatePopupOverlay(carSelectScreen.transform);
        var panelGO = CreatePopupPanel(unlockPopup.transform, new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.85f));

        string carName = LocalizationManager.L(car.localizationKey, car.carId);
        CreatePopupText(panelGO.transform, "Title",
            string.Format(LocalizationManager.L("car_unlock_title", "Unlock {0}"), carName),
            32, FontStyles.Bold, new Color(0.85f, 0.65f, 0.20f),
            new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f));

        float y = 0.72f;
        const float rowH = 0.10f;

        bool coinsOk = gm.Coins >= car.unlockCost;
        AddReqRow(panelGO.transform, coinsOk,
            string.Format(LocalizationManager.L("car_cost_fmt", "{0} Coins"), car.unlockCost),
            $"({gm.Coins}/{car.unlockCost})", y, y + rowH);
        y -= rowH + 0.02f;

        if (car.requiredLevel > 0)
        {
            bool heartOk = LevelData.GetUnlockedLevel(GameMode.Heart) >= car.requiredLevel;
            AddReqRow(panelGO.transform, heartOk,
                string.Format(LocalizationManager.L("car_req_heart", "Heart Mode Lv.{0}"), car.requiredLevel),
                heartOk ? "\u2713" : $"(Lv.{LevelData.GetUnlockedLevel(GameMode.Heart) + 1})", y, y + rowH);
            y -= rowH + 0.02f;

            bool rushOk = LevelData.GetUnlockedLevel(GameMode.Rush) >= car.requiredLevel;
            AddReqRow(panelGO.transform, rushOk,
                string.Format(LocalizationManager.L("car_req_rush", "Rush Mode Lv.{0}"), car.requiredLevel),
                rushOk ? "\u2713" : $"(Lv.{LevelData.GetUnlockedLevel(GameMode.Rush) + 1})", y, y + rowH);
            y -= rowH + 0.02f;
        }

        if (car.requiredEndlessTier10 > 0)
        {
            int count = LevelData.GetEndlessTier10Count();
            bool ok = count >= car.requiredEndlessTier10;
            AddReqRow(panelGO.transform, ok,
                string.Format(LocalizationManager.L("car_req_endless", "Endless Tier 10 x{0}"), car.requiredEndlessTier10),
                $"({count}/{car.requiredEndlessTier10})", y, y + rowH);
        }

        CreatePopupButton(panelGO.transform, "CloseBtn",
            LocalizationManager.L("btn_close", "Close"),
            new Color(0.3f, 0.3f, 0.4f),
            new Vector2(0.3f, 0.03f), new Vector2(0.7f, 0.12f),
            () => { if (unlockPopup != null) Destroy(unlockPopup); });
    }

    // ── Popup builder helpers ───────────────────────────────────────────────

    GameObject CreatePopupOverlay(Transform parent)
    {
        var go = new GameObject("Popup", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.08f, 0.92f);
        return go;
    }

    GameObject CreatePopupPanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("Panel", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0.12f, 0.13f, 0.22f, 0.95f);
        return go;
    }

    GameObject CreatePopupText(Transform parent, string name, string text,
        float fontSize, FontStyles style, Color color,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        return go;
    }

    void CreatePopupButton(Transform parent, string name, string label, Color bgColor,
        Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = bgColor;
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        AddClickSound(btn);

        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(go.transform, false);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text = label;
        lbl.fontSize = 26; lbl.fontStyle = FontStyles.Bold;
        lbl.alignment = TextAlignmentOptions.Center; lbl.color = Color.white;
    }

    void AddReqRow(Transform parent, bool completed, string label, string status, float yMin, float yMax)
    {
        var rowGO = new GameObject("ReqRow", typeof(RectTransform));
        rowGO.transform.SetParent(parent, false);
        var rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.05f, yMin);
        rowRT.anchorMax = new Vector2(0.95f, yMax);
        rowRT.offsetMin = rowRT.offsetMax = Vector2.zero;

        var iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(rowGO.transform, false);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0f, 0.1f);
        iconRT.anchorMax = new Vector2(0.08f, 0.9f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        var iconTMP = iconGO.AddComponent<TextMeshProUGUI>();
        iconTMP.text = completed ? "\u2713" : "\u2717";
        iconTMP.fontSize = 24;
        iconTMP.alignment = TextAlignmentOptions.Center;
        iconTMP.color = completed ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(rowGO.transform, false);
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0.10f, 0f);
        labelRT.anchorMax = new Vector2(0.70f, 1f);
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = label;
        labelTMP.fontSize = 22;
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        labelTMP.color = completed ? new Color(0.7f, 0.9f, 0.7f) : Color.white;
        labelTMP.enableAutoSizing = true;
        labelTMP.fontSizeMin = 12;
        labelTMP.fontSizeMax = 22;

        var statGO = new GameObject("Status", typeof(RectTransform));
        statGO.transform.SetParent(rowGO.transform, false);
        var statRT = statGO.GetComponent<RectTransform>();
        statRT.anchorMin = new Vector2(0.72f, 0f);
        statRT.anchorMax = new Vector2(1f, 1f);
        statRT.offsetMin = statRT.offsetMax = Vector2.zero;
        var statTMP = statGO.AddComponent<TextMeshProUGUI>();
        statTMP.text = status;
        statTMP.fontSize = 18;
        statTMP.alignment = TextAlignmentOptions.MidlineRight;
        statTMP.color = completed ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.9f, 0.7f, 0.3f);
    }

    void CreateStatRow(Transform parent, string label, int pips, float yMin, float yMax)
    {
        var rowGO = new GameObject("Stat", typeof(RectTransform));
        rowGO.transform.SetParent(parent, false);
        var rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.05f, yMin);
        rowRT.anchorMax = new Vector2(0.95f, yMax);
        rowRT.offsetMin = rowRT.offsetMax = Vector2.zero;

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(rowGO.transform, false);
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0f);
        labelRT.anchorMax = new Vector2(0.35f, 1f);
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = label;
        labelTMP.fontSize = 13;
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        labelTMP.color = new Color(0.85f, 0.85f, 0.90f, 1f);

        for (int i = 0; i < 5; i++)
        {
            var pipGO = new GameObject("Pip", typeof(RectTransform));
            pipGO.transform.SetParent(rowGO.transform, false);
            var pipRT = pipGO.GetComponent<RectTransform>();
            float px = 0.38f + i * 0.12f;
            pipRT.anchorMin = new Vector2(px, 0.15f);
            pipRT.anchorMax = new Vector2(px + 0.10f, 0.85f);
            pipRT.offsetMin = pipRT.offsetMax = Vector2.zero;
            var pipImg = pipGO.AddComponent<Image>();
            pipImg.color = i < pips ? new Color(0.2f, 0.9f, 0.3f, 1f) : new Color(0.25f, 0.25f, 0.30f, 1f);
        }
    }
}
