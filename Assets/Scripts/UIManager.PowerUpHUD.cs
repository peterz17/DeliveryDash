using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class UIManager
{
    readonly Dictionary<string, Coroutine> powerUpTimerCoroutines = new Dictionary<string, Coroutine>();
    readonly Dictionary<string, TextMeshProUGUI> powerUpSlotTexts  = new Dictionary<string, TextMeshProUGUI>();

    public void ClearPowerUpTimers()
    {
        foreach (var kv in powerUpTimerCoroutines)
            if (kv.Value != null) StopCoroutine(kv.Value);
        powerUpTimerCoroutines.Clear();
        foreach (var kv in powerUpSlotTexts)
            if (kv.Value != null) kv.Value.gameObject.SetActive(false);
        if (powerUpHudPanel != null) powerUpHudPanel.SetActive(false);
    }

    void BuildPowerUpHudIfNeeded()
    {
        if (powerUpHudPanel != null) return;
        if (gameplayUI == null) return;

        var panel = new GameObject("PowerUpHudPanel", typeof(RectTransform));
        panel.transform.SetParent(gameplayUI.transform, false);
        AnchorRect(panel, new Vector2(0f, 0.68f), new Vector2(0.24f, 0.872f));

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(8, 4, 4, 4);

        var fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        powerUpHudPanel = panel;
        panel.SetActive(false);
    }

    TextMeshProUGUI GetOrCreatePowerUpSlot(string typeName)
    {
        if (powerUpSlotTexts.ContainsKey(typeName))
            return powerUpSlotTexts[typeName];

        if (powerUpHudPanel == null) BuildPowerUpHudIfNeeded();
        if (powerUpHudPanel == null) return null;

        var slotGO = new GameObject($"Slot_{typeName}", typeof(RectTransform));
        slotGO.transform.SetParent(powerUpHudPanel.transform, false);
        var le = slotGO.AddComponent<LayoutElement>();
        le.preferredHeight = 36f;
        var tmp = slotGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize    = 28;
        tmp.fontStyle   = FontStyles.Bold;
        tmp.alignment   = TextAlignmentOptions.MidlineLeft;
        tmp.color       = Color.white;
        tmp.raycastTarget = false;
        powerUpSlotTexts[typeName] = tmp;
        return tmp;
    }

    void HandlePowerUpActivated(string typeName, float duration)
    {
        string label = LocalizationManager.L("powerup_timer_" + typeName, typeName);
        if (powerUpTimerCoroutines.ContainsKey(typeName) && powerUpTimerCoroutines[typeName] != null)
            StopCoroutine(powerUpTimerCoroutines[typeName]);
        powerUpTimerCoroutines[typeName] = StartCoroutine(PowerUpTimerRoutine(typeName, label, duration));
    }

    void HandlePowerUpDeactivated(string typeName)
    {
        if (powerUpTimerCoroutines.ContainsKey(typeName) && powerUpTimerCoroutines[typeName] != null)
        {
            StopCoroutine(powerUpTimerCoroutines[typeName]);
            powerUpTimerCoroutines[typeName] = null;
        }
        if (powerUpSlotTexts.ContainsKey(typeName))
            powerUpSlotTexts[typeName].gameObject.SetActive(false);
        RefreshPowerUpHudVisibility();
    }

    IEnumerator PowerUpTimerRoutine(string typeName, string label, float duration)
    {
        var slot = GetOrCreatePowerUpSlot(typeName);
        if (slot == null) yield break;

        if (powerUpHudPanel != null) powerUpHudPanel.SetActive(true);
        slot.gameObject.SetActive(true);

        float remaining = duration;
        while (remaining > 0f)
        {
            slot.text = $"{label}  {remaining:0.0}s";
            float lerp = remaining / duration;
            slot.color = Color.Lerp(new Color(1f, 0.4f, 0.2f), Color.white, lerp);
            yield return null;
            remaining -= Time.deltaTime;
        }

        slot.gameObject.SetActive(false);
        if (powerUpTimerCoroutines.ContainsKey(typeName))
            powerUpTimerCoroutines[typeName] = null;
        RefreshPowerUpHudVisibility();
    }

    void RefreshPowerUpHudVisibility()
    {
        if (powerUpHudPanel == null) return;
        bool anyActive = false;
        foreach (var kv in powerUpSlotTexts)
        {
            if (kv.Value != null && kv.Value.gameObject.activeSelf)
            {
                anyActive = true;
                break;
            }
        }
        powerUpHudPanel.SetActive(anyActive);
    }
}
