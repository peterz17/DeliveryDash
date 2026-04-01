using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    public static event System.Action<string, float> OnPowerUpActivated;
    public static event System.Action<string> OnPowerUpDeactivated;
    public static void RaisePowerUpActivated(string powerUpId, float duration)
    {
        OnPowerUpActivated?.Invoke(powerUpId, duration);
    }

    public static void RaisePowerUpDeactivated(string powerUpId)
    {
        OnPowerUpDeactivated?.Invoke(powerUpId);
    }
    public bool IsFrozen { get; private set; }

    public Sprite[] powerUpIcons;

    readonly List<GameObject> activeItems = new List<GameObject>();
    PlayerController player;
    Coroutine spawnCoroutine;
    Coroutine clockCoroutine;

    static readonly Color[] TypeColors =
    {
        new Color(0f,  0.8f, 1f),   // Shield — cyan
        new Color(1f,  0.5f, 0f),   // Rocket — orange
        new Color(0.7f, 1f,  0f),   // Clock  — yellow-green
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        var players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players.Length > 0) player = players[0];
    }

    void Start()
    {
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(7f, 10f));

            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                continue;

            CleanDestroyedItems();
            if (activeItems.Count >= 2) continue;

            SpawnRandom();
        }
    }

    Vector2 GetSpawnPosition()
    {
        var filter = new ContactFilter2D { useTriggers = false };
        var hits = new Collider2D[1];

        for (int attempts = 0; attempts < 30; attempts++)
        {
            Vector2 pos = new Vector2(Random.Range(-7f, 7f), Random.Range(-4f, 4f));
            if (pos.magnitude < 2.5f) continue;
            if (Physics2D.OverlapCircle(pos, 0.5f, filter, hits) == 0)
                return pos;
        }

        return new Vector2(0f, 2.5f);
    }

    void SpawnRandom()
    {
        PowerUpType type = (PowerUpType)Random.Range(0, 3);

        Vector2 offset = GetSpawnPosition();
        Vector3 pos = new Vector3(offset.x, offset.y, 0f);

        var go = new GameObject($"PowerUp_{type}");
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        Sprite iconSpr = null;
        if (powerUpIcons != null && (int)type < powerUpIcons.Length)
            iconSpr = powerUpIcons[(int)type];
        sr.sprite = iconSpr != null ? iconSpr : CreateIconSprite(type);
        sr.color = Color.white;
        sr.sortingOrder = 10;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        var pu = go.AddComponent<PowerUp>();
        pu.Type = type;

        activeItems.Add(go);
    }

    // ── icon sprite factory ───────────────────────────────────────────────────

    static Sprite CreateIconSprite(PowerUpType type)
    {
        const int S = 32;
        var px = new Color[S * S];

        // Colored disc background
        FillDisc(px, S, 15, 15, 14, TypeColors[(int)type]);
        // Thin dark border ring
        DrawRing(px, S, 15, 15, 14, 13, new Color(0f, 0f, 0f, 0.35f));

        switch (type)
        {
            case PowerUpType.Shield: DrawShieldIcon(px, S); break;
            case PowerUpType.Rocket: DrawRocketIcon(px, S); break;
            case PowerUpType.Clock: DrawClockIcon(px, S); break;
        }

        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), Vector2.one * 0.5f, S);
    }

    // ── pixel helpers (y = 0 at bottom of texture) ───────────────────────────

    static void FillDisc(Color[] px, int S, int cx, int cy, int r, Color c)
    {
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                    px[y * S + x] = c;
    }

    static void DrawRing(Color[] px, int S, int cx, int cy, int outerR, int innerR, Color c)
    {
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (d2 <= outerR * outerR && d2 >= innerR * innerR)
                    px[y * S + x] = c;
            }
    }

    static void FillRect(Color[] px, int S, int x, int y, int w, int h, Color c)
    {
        for (int py = y; py < y + h; py++)
            for (int qx = x; qx < x + w; qx++)
                if (qx >= 0 && qx < S && py >= 0 && py < S)
                    px[py * S + qx] = c;
    }

    // ── icon shapes ───────────────────────────────────────────────────────────

    // Shield: rectangle body with tapered point at bottom
    static void DrawShieldIcon(Color[] px, int S)
    {
        Color w = Color.white;
        FillRect(px, S, 8, 10, 16, 16, w);   // main body  x=8-23  y=10-25
        FillRect(px, S, 9, 9, 14, 1, w);   // taper row
        FillRect(px, S, 10, 8, 12, 1, w);
        FillRect(px, S, 11, 7, 10, 1, w);
        FillRect(px, S, 12, 6, 8, 1, w);
        FillRect(px, S, 13, 5, 6, 1, w);
        FillRect(px, S, 14, 4, 4, 1, w);
        FillRect(px, S, 15, 3, 2, 1, w);   // tip
    }

    // Rocket: nose cone + body + fins + yellow flame
    static void DrawRocketIcon(Color[] px, int S)
    {
        Color w = Color.white;
        Color flame = new Color(1f, 0.9f, 0.3f);
        // Nose cone (triangle pointing up)
        FillRect(px, S, 15, 26, 2, 1, w);
        FillRect(px, S, 14, 25, 4, 1, w);
        FillRect(px, S, 13, 24, 6, 1, w);
        FillRect(px, S, 12, 23, 8, 1, w);
        // Body
        FillRect(px, S, 12, 11, 8, 12, w);
        // Left fin
        FillRect(px, S, 9, 10, 3, 1, w);
        FillRect(px, S, 8, 9, 4, 1, w);
        FillRect(px, S, 9, 8, 3, 1, w);
        // Right fin
        FillRect(px, S, 20, 10, 3, 1, w);
        FillRect(px, S, 20, 9, 4, 1, w);
        FillRect(px, S, 20, 8, 3, 1, w);
        // Flame (yellow, below body)
        FillRect(px, S, 13, 6, 6, 5, flame);
        FillRect(px, S, 14, 5, 4, 1, flame);
        FillRect(px, S, 15, 4, 2, 1, flame);
    }

    // Clock: ring face + minute hand up + hour hand right + center hub
    static void DrawClockIcon(Color[] px, int S)
    {
        Color w = Color.white;
        DrawRing(px, S, 15, 15, 12, 9, w);       // clock face ring
        FillRect(px, S, 14, 15, 3, 8, w);        // minute hand (pointing up)
        FillRect(px, S, 15, 14, 6, 3, w);        // hour hand (pointing right)
        FillDisc(px, S, 15, 15, 2, w);            // center hub
    }

    void CleanDestroyedItems()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            if (activeItems[i] == null)
                activeItems.RemoveAt(i);
        }
    }

    void DestroyAllItems()
    {
        CleanDestroyedItems();
        foreach (var item in activeItems)
        {
            if (item != null)
                Destroy(item);
        }
        activeItems.Clear();
    }

    public void OnGameStateChanged(GameState state)
    {
        if (state != GameState.Playing)
        {
            DestroyAllItems();

            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            if (clockCoroutine != null)
            {
                StopCoroutine(clockCoroutine);
                clockCoroutine = null;
            }

            IsFrozen = false;
            if (GameManager.Instance != null)
                GameManager.Instance.TimerFrozen = false;
        }
        else
        {
            // Clear leftover items from previous level before restarting spawn
            DestroyAllItems();
            if (clockCoroutine != null)
            {
                StopCoroutine(clockCoroutine);
                clockCoroutine = null;
            }
            IsFrozen = false;
            if (GameManager.Instance != null)
                GameManager.Instance.TimerFrozen = false;

            if (spawnCoroutine == null)
                spawnCoroutine = StartCoroutine(SpawnLoop());
        }
    }

    public void StartClockEffect()
    {
        if (clockCoroutine != null)
            StopCoroutine(clockCoroutine);
        clockCoroutine = StartCoroutine(ClockRoutine(4f));
    }

    IEnumerator ClockRoutine(float duration)
    {
        IsFrozen = true;
        if (GameManager.Instance != null)
            GameManager.Instance.TimerFrozen = true;
        OnPowerUpActivated?.Invoke("clock", duration);
        try
        {
            yield return new WaitForSeconds(duration);
        }
        finally
        {
            IsFrozen = false;
            if (GameManager.Instance != null)
                GameManager.Instance.TimerFrozen = false;
            OnPowerUpDeactivated?.Invoke("clock");
            clockCoroutine = null;
        }
    }
}
