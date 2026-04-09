using System.Collections.Generic;
using UnityEngine;

public class OffScreenIndicator : MonoBehaviour
{
    SpriteRenderer sr;
    Camera          cam;
    Dictionary<string, DeliveryZone> zoneMap;

    const float EdgeMargin = 0.65f;

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
        cam = Camera.main;
        CacheZones();
    }

    void CacheZones()
    {
        zoneMap = new Dictionary<string, DeliveryZone>();
        foreach (var zone in Object.FindObjectsByType<DeliveryZone>(FindObjectsSortMode.None))
            zoneMap[zone.destinationName] = zone;
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

        if (!zoneMap.TryGetValue(gm.CurrentDestination, out var zone))
        {
            sr.enabled = false;
            return;
        }

        Vector3 targetPos = zone.transform.position;

        Vector3 vp = cam.WorldToViewportPoint(targetPos);
        bool onScreen = vp.z > 0f && vp.x >= 0.05f && vp.x <= 0.95f
                                   && vp.y >= 0.05f && vp.y <= 0.95f;
        if (onScreen) { sr.enabled = false; return; }

        Vector3 camPos = cam.transform.position;
        float halfH    = cam.orthographicSize  - EdgeMargin;
        float halfW    = halfH * cam.aspect     - EdgeMargin;

        Vector2 dir = new Vector2(targetPos.x - camPos.x, targetPos.y - camPos.y);
        if (dir == Vector2.zero) { sr.enabled = false; return; }

        float tx = dir.x != 0f ? (dir.x > 0f ? halfW : -halfW) / dir.x : float.MaxValue;
        float ty = dir.y != 0f ? (dir.y > 0f ? halfH : -halfH) / dir.y : float.MaxValue;
        float t  = Mathf.Min(tx, ty);

        Vector3 edgePos = new Vector3(camPos.x + dir.x * t, camPos.y + dir.y * t, 0f);

        float topBarWorldY = camPos.y + (0.875f * 2f - 1f) * cam.orthographicSize;
        float maxWorldY    = topBarWorldY - EdgeMargin;
        if (edgePos.y > maxWorldY) edgePos.y = maxWorldY;

        transform.position = edgePos;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        float pulse = Mathf.Sin(Time.time * 5f) * 0.5f + 0.5f;
        float alpha = 0.72f + 0.28f * pulse;
        float scale = 0.68f + 0.08f * pulse;
        sr.color = new Color(1f, 1f, 0.6f, alpha);
        transform.localScale = Vector3.one * scale;
        sr.enabled = true;
    }

    static Sprite BuildArrowSprite()
    {
        const int S = 32;
        var px = new Color[S * S];

        float cx      = S * 0.5f;
        float tipY    = S * 0.90f;
        float baseY   = S * 0.10f;
        float baseHalf = S * 0.40f;
        float height  = tipY - baseY;

        Color fill   = Color.white;
        Color border = new Color(0f, 0f, 0f, 0.55f);

        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float fy = y - baseY;
                if (fy < 0f || y > tipY) { px[y * S + x] = Color.clear; continue; }

                float tt        = fy / height;
                float halfWidth = baseHalf * (1f - tt);
                float dist      = Mathf.Abs(x - cx);

                if (dist > halfWidth) { px[y * S + x] = Color.clear; continue; }

                bool edge = dist >= halfWidth - 1.8f || fy <= 1.8f;
                px[y * S + x] = edge ? border : fill;
            }

        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 48f);
    }
}
