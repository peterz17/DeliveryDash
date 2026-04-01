using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public PowerUpType Type;

    Vector3 basePosition;
    Vector3 baseScale;
    const float BobAmplitude = 0.15f;
    const float BobSpeed     = 2f;
    const float PulsePeriod  = 1.2f;
    float bobOffset;

    SpriteRenderer glowRenderer;

    void Awake()
    {
        basePosition = transform.position;
        baseScale    = transform.localScale;
        bobOffset    = Random.Range(0f, Mathf.PI * 2f);
    }

    void Start()
    {
        glowRenderer = CreateGlow();
    }

    SpriteRenderer CreateGlow()
    {
        var go = new GameObject("Glow");
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * 2.2f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite     = BuildGlowDisc();
        sr.sortingOrder = GetComponent<SpriteRenderer>() != null
            ? GetComponent<SpriteRenderer>().sortingOrder - 1
            : 9;
        sr.color = Color.clear;
        return sr;
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
        // Bob (original vertical movement)
        float y = basePosition.y + Mathf.Sin(Time.time * BobSpeed + bobOffset) * BobAmplitude;
        transform.position = new Vector3(basePosition.x, y, basePosition.z);

        // Scale pulse
        float pulse = 0.95f + 0.10f * (Mathf.Sin(Time.time * (Mathf.PI * 2f / PulsePeriod)) * 0.5f + 0.5f);
        transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y * pulse, baseScale.z);

        // Glow pulse
        if (glowRenderer != null)
        {
            Color glowColor = GetGlowColor();
            float glowAlpha = 0.35f + 0.20f * (Mathf.Sin(Time.time * 2.5f) * 0.5f + 0.5f);
            glowRenderer.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowAlpha);
        }
    }

    Color GetGlowColor()
    {
        switch (Type)
        {
            case PowerUpType.Shield: return new Color(0f, 0.8f, 1f);
            case PowerUpType.Rocket: return new Color(1f, 0.5f, 0f);
            case PowerUpType.Clock:  return new Color(0.6f, 1f, 0.2f);
            default:                 return Color.white;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        player.PickupPowerUp(Type);
        Destroy(gameObject);
    }
}
