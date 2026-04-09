using System.Collections.Generic;
using UnityEngine;

public class DestinationArrow : MonoBehaviour
{
    Dictionary<string, DeliveryZone> zoneMap;
    SpriteRenderer    sr;
    Vector3           baseScale;

    const float PulseSpeed = 3f;
    const float PulseMin   = 0.75f;
    const float PulseMax   = 1.10f;

    void Awake()
    {
        sr        = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    void Start()
    {
        CacheZones();
    }

    void CacheZones()
    {
        zoneMap = new Dictionary<string, DeliveryZone>();
        foreach (var zone in Object.FindObjectsByType<DeliveryZone>(FindObjectsSortMode.None))
            zoneMap[zone.destinationName] = zone;
    }

    void Update()
    {
        bool playing = GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;
        if (sr != null) sr.enabled = playing;
        if (!playing) return;

        transform.position = transform.parent.position + new Vector3(0f, 0.55f, -0.1f);

        string dest = GameManager.Instance.CurrentDestination;
        if (string.IsNullOrEmpty(dest)) return;

        if (!zoneMap.TryGetValue(dest, out var zone)) return;

        Vector3 dir = zone.transform.position - transform.parent.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (sr != null)
        {
            var zoneSr = zone.GetComponent<SpriteRenderer>();
            Color zoneColor = zoneSr != null ? zoneSr.color : Color.white;

            float pulse = PulseMin + (PulseMax - PulseMin) * (Mathf.Sin(Time.time * PulseSpeed) * 0.5f + 0.5f);
            sr.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, pulse);
            transform.localScale = baseScale * (0.95f + 0.10f * (Mathf.Sin(Time.time * PulseSpeed) * 0.5f + 0.5f));
        }
    }
}
