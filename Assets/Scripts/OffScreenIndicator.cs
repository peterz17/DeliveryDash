using UnityEngine;

public class OffScreenIndicator : MonoBehaviour
{
    SpriteRenderer sr;
    Camera          cam;
    DeliveryZone[]  zones;

    const float EdgeMargin = 0.65f;  // world units inset from camera edge

    void Awake()
    {
        sr              = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = BuildArrowSprite();
        sr.sortingOrder = 20;
        sr.enabled      = false;
        transform.localScale = Vector3.one * 0.72f;
    }

    void Start()
    {
        cam   = Camera.main;
        zones = Object.FindObjectsByType<DeliveryZone>(FindObjectsSortMode.None);
    }

    void LateUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing || gm.player == null
            || !gm.player.HasPackage || string.IsNullOrEmpty(gm.CurrentDestination))
        {
            sr.enabled = false;
            return;
        }

        // Find target zone world position
        Vector3 targetPos = Vector3.zero;
        bool found = false;
        foreach (var zone in zones)
        {
            if (zone.destinationName == gm.CurrentDestination)
            {
                targetPos = zone.transform.position;
                found = true;
                break;
            }
        }

        if (!found) { sr.enabled = false; return; }

        // Check if target is on screen (5% inset to avoid flicker at edge)
        Vector3 vp = cam.WorldToViewportPoint(targetPos);
        bool onScreen = vp.z > 0f && vp.x >= 0.05f && vp.x <= 0.95f
                                   && vp.y >= 0.05f && vp.y <= 0.95f;
        if (onScreen) { sr.enabled = false; return; }

        // Clamp to camera edge (orthographic)
        Vector3 camPos = cam.transform.position;
        float halfH    = cam.orthographicSize  - EdgeMargin;
        float halfW    = halfH * cam.aspect     - EdgeMargin;

        Vector2 dir = new Vector2(targetPos.x - camPos.x, targetPos.y - camPos.y);
        if (dir == Vector2.zero) { sr.enabled = false; return; }

        // Ray-vs-AABB: find smallest t that hits a camera bound
        float tx = dir.x != 0f ? (dir.x > 0f ? halfW : -halfW) / dir.x : float.MaxValue;
        float ty = dir.y != 0f ? (dir.y > 0f ? halfH : -halfH) / dir.y : float.MaxValue;
        float t  = Mathf.Min(tx, ty);

        Vector3 edgePos = new Vector3(camPos.x + dir.x * t, camPos.y + dir.y * t, 0f);

        // TopBar occupies top 12.5% of viewport (anchor 0.875–1.0).
        // Convert TopBar bottom edge (vp y=0.875) to world Y and clamp below it.
        float topBarWorldY = camPos.y + (0.875f * 2f - 1f) * cam.orthographicSize;  // = camPos.y + 0.75 * orthoSize
        float maxWorldY    = topBarWorldY - EdgeMargin;
        if (edgePos.y > maxWorldY) edgePos.y = maxWorldY;

        transform.position = edgePos;

        // Rotate arrow toward target
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Light yellow indicator, pulse alpha + scale
        Color zoneColor = new Color(1f, 1f, 0.6f);
        float pulse = Mathf.Sin(Time.time * 5f) * 0.5f + 0.5f;
        float alpha = 0.72f + 0.28f * pulse;
        float scale = 0.68f + 0.08f * pulse;
        sr.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, alpha);
        transform.localScale = Vector3.one * scale;
        sr.enabled = true;
    }

    Color GetZoneColor(string destination)
    {
        foreach (var zone in zones)
        {
            if (zone.destinationName != destination) continue;
            var zsr = zone.GetComponent<SpriteRenderer>();
            if (zsr != null) return zsr.color;
        }
        return new Color(1f, 0.9f, 0.1f);
    }

    static Sprite BuildArrowSprite()
    {
        const int S = 32;
        var px = new Color[S * S];

        // Clean isoceles triangle — tip at top, wide base
        // y increases upward in Unity texture coords
        float cx      = S * 0.5f;
        float tipY    = S * 0.90f;   // apex
        float baseY   = S * 0.10f;   // base line
        float baseHalf = S * 0.40f;  // half-width at base
        float height  = tipY - baseY;

        Color fill   = Color.white;
        Color border = new Color(0f, 0f, 0f, 0.55f);

        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float fy = y - baseY;
                if (fy < 0f || y > tipY) { px[y * S + x] = Color.clear; continue; }

                float t         = fy / height;               // 0=base → 1=tip
                float halfWidth = baseHalf * (1f - t);
                float dist      = Mathf.Abs(x - cx);

                if (dist > halfWidth) { px[y * S + x] = Color.clear; continue; }

                bool edge = dist >= halfWidth - 1.8f || fy <= 1.8f;
                px[y * S + x] = edge ? border : fill;
            }

        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(px);
        tex.Apply();
        // 48 PPU → 32/48 ≈ 0.67 world units, pivot at vertical center of shape
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 48f);
    }
}
