using UnityEngine;

public class ZoneHighlight : MonoBehaviour
{
    SpriteRenderer sr;
    SpriteRenderer glowSr;
    Vector3 baseScale;
    Color baseColor;
    float flashTimer;
    bool isHighlighted;

    void Awake()
    {
        sr        = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        baseColor = sr != null ? sr.color : Color.white;
        enabled   = false;

        glowSr = CreateGlow();
    }

    SpriteRenderer CreateGlow()
    {
        var go = new GameObject("Glow");
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * 1.5f;

        var gsr = go.AddComponent<SpriteRenderer>();
        gsr.sprite       = BuildGlowDisc();
        gsr.sortingOrder = sr != null ? sr.sortingOrder - 1 : 0;
        gsr.color        = Color.clear;
        return gsr;
    }

    static Sprite BuildGlowDisc()
    {
        const int S = 64;
        var px = new Color[S * S];
        float cx = S * 0.5f, cy = S * 0.5f, r = S * 0.5f;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dist  = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float alpha = Mathf.Clamp01(1f - (dist / r));
                alpha = alpha * alpha;
                px[y * S + x] = new Color(1f, 1f, 1f, alpha);
            }
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), Vector2.one * 0.5f, 100f);
    }

    void Update()
    {
        float timeLeft   = GameManager.Instance != null ? GameManager.Instance.TimeRemaining : 999f;
        float pulseSpeed = flashTimer > 0f ? 14f : (timeLeft < 10f ? 8f : 4f);
        if (flashTimer > 0f) flashTimer -= Time.deltaTime;

        float t = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
        float scaleBoost = flashTimer > 0f ? 0.15f : 0.08f;
        transform.localScale = baseScale * (1f + t * scaleBoost);
        if (sr != null)
            sr.color = Color.Lerp(baseColor, Color.white, t * 0.45f);

        // Glow animation
        if (glowSr != null)
        {
            float glowPulse = 0.25f + 0.20f * t;
            float glowAlpha = isHighlighted ? glowPulse : 0f;
            glowSr.color = new Color(baseColor.r, baseColor.g, baseColor.b, glowAlpha);
            float glowScale = 1.5f + 0.12f * t;
            glowSr.transform.localScale = Vector3.one * glowScale;
        }
    }

    public void SetHighlighted(bool on)
    {
        isHighlighted = on;
        enabled = on;
        if (!on)
        {
            transform.localScale = baseScale;
            if (sr != null) sr.color = baseColor;
            if (glowSr != null) glowSr.color = Color.clear;
        }
    }

    public void TriggerFlash()
    {
        flashTimer = 1.2f;
    }
}
