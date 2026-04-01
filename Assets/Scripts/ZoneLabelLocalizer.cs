using UnityEngine;
using System.Collections.Generic;

public class ZoneLabelLocalizer : MonoBehaviour
{
    public string localizationKey;

    TextMesh textMesh;
    Color baseColor;

    static float globalOpacity = 1f;
    static readonly List<ZoneLabelLocalizer> instances = new();

    void Awake()
    {
        textMesh = GetComponent<TextMesh>();
        if (textMesh != null) baseColor = textMesh.color;
        globalOpacity = PlayerPrefs.GetFloat("ZoneLabelOpacity", 1f);
    }

    void OnEnable()
    {
        instances.Add(this);
        LocalizationManager.OnLanguageChanged += Refresh;
    }

    void Start()
    {
        Refresh();
        ApplyOpacity();
    }

    void OnDisable()
    {
        instances.Remove(this);
        LocalizationManager.OnLanguageChanged -= Refresh;
    }

    void Refresh()
    {
        if (textMesh == null) return;
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(localizationKey))
            textMesh.text = LocalizationManager.Instance.Get(localizationKey);
    }

    void ApplyOpacity()
    {
        if (textMesh == null) return;
        textMesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, globalOpacity);
    }

    public static void SetGlobalOpacity(float opacity)
    {
        globalOpacity = Mathf.Clamp01(opacity);
        foreach (var inst in instances)
            if (inst != null) inst.ApplyOpacity();
    }
}
