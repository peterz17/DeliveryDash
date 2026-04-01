using TMPro;
using UnityEngine;

// ── LocalizedFontText ─────────────────────────────────────────────────────────
// Add this component to any GameObject that has a TMP_Text (TextMeshProUGUI or
// TextMeshPro). It automatically registers with FontLocalizationManager and
// re-applies the correct font whenever the language changes.
//
// You do NOT need to touch any settings on this component — just add it and the
// FontLocalizationManager handles everything.
[RequireComponent(typeof(TMP_Text))]
public class LocalizedFontText : MonoBehaviour
{
    TMP_Text _tmp;

    void Awake()
    {
        _tmp = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        FontLocalizationManager.OnFontLanguageChanged += OnLanguageChanged;

        if (FontLocalizationManager.Instance != null)
            FontLocalizationManager.Instance.Register(_tmp);
        else
            // Manager not in scene yet — apply immediately once it appears
            // by subscribing; Register will also apply on first call
            Debug.LogWarning(
                "[LocalizedFontText] FontLocalizationManager not found in scene. " +
                "Add a FontLocalizationManager to your scene.", this);
    }

    void OnDisable()
    {
        FontLocalizationManager.OnFontLanguageChanged -= OnLanguageChanged;

        if (FontLocalizationManager.Instance != null)
            FontLocalizationManager.Instance.Unregister(_tmp);
    }

    void OnLanguageChanged(SupportedLanguage _)
    {
        if (FontLocalizationManager.Instance != null && _tmp != null)
            FontLocalizationManager.Instance.ApplyFont(_tmp);
    }
}
