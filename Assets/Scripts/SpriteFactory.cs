using UnityEngine;

public static class SpriteFactory
{
    static Sprite cachedAuraDisc;

    public static Sprite AuraDisc()
    {
        if (cachedAuraDisc != null) return cachedAuraDisc;

        const int S = 32;
        var px = new Color[S * S];
        float cx = S * 0.5f, cy = S * 0.5f, r = S * 0.5f - 1f;

        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (dist <= r)
                {
                    float alpha = 1f - Mathf.Clamp01((dist - (r - 4f)) / 4f);
                    px[y * S + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(px);
        tex.Apply();
        cachedAuraDisc = Sprite.Create(tex, new Rect(0, 0, S, S), Vector2.one * 0.5f, 100f);
        return cachedAuraDisc;
    }
}
