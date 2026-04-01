using System.Collections.Generic;
using UnityEngine;
using TMPro;

// ── Supported languages ──────────────────────────────────────────────────────
// Add new languages here. Each one needs a matching LanguageFontEntry in the
// Inspector before it will receive a different font.
public enum SupportedLanguage
{
    English,
    Thai,
    Japanese,
    Korean,
    ChineseSimplified,
    ChineseTraditional,
}

// ── Per-language font configuration ──────────────────────────────────────────
// Drag your TMP Font Assets into these slots in the Inspector.
[System.Serializable]
public class LanguageFontEntry
{
    // Which language this entry covers
    public SupportedLanguage language;

    // The main font used for this language.
    // Drag your language-specific TMP_FontAsset here.
    public TMP_FontAsset primaryFont;

    // Extra fallback fonts searched after primaryFont fails to find a glyph.
    // Useful for mixed-script text (e.g., Latin + Thai, CJK + Latin).
    // Leave empty if your primaryFont already has its own fallbacks configured.
    public List<TMP_FontAsset> fallbackFonts;
}

// ── FontLocalizationManager ───────────────────────────────────────────────────
// Add this MonoBehaviour to a scene GameObject.
// Configure font entries in the Inspector, then call SetLanguage() or let it
// sync automatically with LocalizationManager when that toggles language.
public class FontLocalizationManager : MonoBehaviour
{
    public static FontLocalizationManager Instance { get; private set; }

    // Fires after fonts have been applied to all registered TMP_Text components.
    public static event System.Action<SupportedLanguage> OnFontLanguageChanged;

    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Font Entries")]
    // Add one entry per language. Drag your TMP Font Assets into each entry.
    public List<LanguageFontEntry> fontEntries = new List<LanguageFontEntry>();

    [Header("Settings")]
    // If the requested language has no font assigned, fall back to this one.
    public SupportedLanguage defaultFallbackLanguage = SupportedLanguage.English;

    // ── Runtime state ────────────────────────────────────────────────────────
    SupportedLanguage currentLanguage = SupportedLanguage.English;
    readonly List<TMP_Text> registeredTexts = new List<TMP_Text>();

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Sync automatically whenever LocalizationManager switches language
        LocalizationManager.OnLanguageChanged += SyncFromLocalizationManager;
    }

    void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= SyncFromLocalizationManager;
    }

    // Map LocalizationManager.Language → SupportedLanguage
    void SyncFromLocalizationManager()
    {
        if (LocalizationManager.Instance == null) return;

        SupportedLanguage mapped;
        switch (LocalizationManager.Instance.CurrentLanguage)
        {
            case LocalizationManager.Language.Thai:    mapped = SupportedLanguage.Thai;    break;
            default:                                   mapped = SupportedLanguage.English; break;
        }
        SetLanguage(mapped);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public SupportedLanguage CurrentLanguage => currentLanguage;

    // Switch language and re-apply fonts to every registered TMP_Text.
    public void SetLanguage(SupportedLanguage language)
    {
        currentLanguage = language;
        ApplyFontToAllRegisteredTexts();
        if (OnFontLanguageChanged != null)
            OnFontLanguageChanged.Invoke(language);
    }

    // Returns the primary TMP_FontAsset for the given language.
    // Falls back to defaultFallbackLanguage, then returns null with a warning.
    public TMP_FontAsset GetPrimaryFont(SupportedLanguage language)
    {
        var entry = FindEntry(language);
        if (entry != null && entry.primaryFont != null)
            return entry.primaryFont;

        if (language != defaultFallbackLanguage)
        {
            var fallbackEntry = FindEntry(defaultFallbackLanguage);
            if (fallbackEntry != null && fallbackEntry.primaryFont != null)
                return fallbackEntry.primaryFont;
        }

        return null;
    }

    // Returns the fallback font list for the given language (empty if not set).
    public List<TMP_FontAsset> GetFallbackFonts(SupportedLanguage language)
    {
        var entry = FindEntry(language);
        if (entry != null && entry.fallbackFonts != null)
            return entry.fallbackFonts;
        return new List<TMP_FontAsset>();
    }

    // Applies the currently active language font to a single TMP_Text.
    // Sets tmp.font to the primary font and updates that font's fallback list.
    public void ApplyFont(TMP_Text target)
    {
        if (target == null) return;

        TMP_FontAsset font = GetPrimaryFont(currentLanguage);
        if (font == null)
        {
            Debug.LogWarning(
                $"[FontLocalizationManager] No font configured for '{currentLanguage}' " +
                $"or fallback '{defaultFallbackLanguage}'. Keeping existing font on '{target.name}'.",
                target);
            return;
        }

        target.font = font;

        // Update the primary font's fallback list with language-specific fallbacks.
        // This modifies the shared font asset, which is intentional — all text using
        // this font asset will benefit from the same fallbacks.
        var fallbacks = GetFallbackFonts(currentLanguage);
        font.fallbackFontAssetTable = fallbacks;
    }

    // Re-applies fonts to every registered TMP_Text, pruning destroyed entries.
    public void ApplyFontToAllRegisteredTexts()
    {
        for (int i = registeredTexts.Count - 1; i >= 0; i--)
        {
            if (registeredTexts[i] == null) { registeredTexts.RemoveAt(i); continue; }
            ApplyFont(registeredTexts[i]);
        }
    }

    // Called by LocalizedFontText components on OnEnable.
    public void Register(TMP_Text text)
    {
        if (text == null) return;
        if (!registeredTexts.Contains(text))
            registeredTexts.Add(text);
        ApplyFont(text);
    }

    // Called by LocalizedFontText components on OnDisable.
    public void Unregister(TMP_Text text)
    {
        registeredTexts.Remove(text);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    LanguageFontEntry FindEntry(SupportedLanguage language)
    {
        foreach (var entry in fontEntries)
            if (entry.language == language)
                return entry;
        return null;
    }
}
