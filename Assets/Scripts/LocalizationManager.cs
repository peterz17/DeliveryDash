using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public enum Language { English, Thai }

    public Language CurrentLanguage { get; private set; }

    public static event System.Action OnLanguageChanged;

    [System.Serializable]
    class LocalizationEntry { public string key; public string value; }

    [System.Serializable]
    class LocalizationFile { public LocalizationEntry[] entries; }

    private Dictionary<string, string> _en;
    private Dictionary<string, string> _th;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadDictionaries();

        int saved = PlayerPrefs.GetInt("language", 0);
        CurrentLanguage = saved == 1 ? Language.Thai : Language.English;
    }

    void LoadDictionaries()
    {
        _en = LoadFromJson("Localization/en");
        _th = LoadFromJson("Localization/th");
    }

    Dictionary<string, string> LoadFromJson(string resourcePath)
    {
        var result = new Dictionary<string, string>();
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            Debug.LogWarning($"LocalizationManager: could not load '{resourcePath}'");
            return result;
        }
        LocalizationFile file = JsonUtility.FromJson<LocalizationFile>(asset.text);
        if (file == null || file.entries == null) return result;
        foreach (var entry in file.entries)
        {
            if (!string.IsNullOrEmpty(entry.key))
                result[entry.key] = entry.value ?? string.Empty;
        }
        return result;
    }

    public void ToggleLanguage()
    {
        CurrentLanguage = CurrentLanguage == Language.English ? Language.Thai : Language.English;
        PlayerPrefs.SetInt("language", CurrentLanguage == Language.Thai ? 1 : 0);
        PlayerPrefs.Save();
        if (OnLanguageChanged != null)
            OnLanguageChanged.Invoke();
    }

    public string Get(string key)
    {
        Dictionary<string, string> dict = CurrentLanguage == Language.Thai ? _th : _en;
        string value;
        if (dict.TryGetValue(key, out value))
            return value;
        return key;
    }

    public string GetDestination(string internalName)
    {
        switch (internalName)
        {
            case "House A": return Get("dest_house_a");
            case "House B": return Get("dest_house_b");
            case "Shop":    return Get("dest_shop");
            case "Cafe":    return Get("dest_cafe");
            default:        return internalName;
        }
    }
}
