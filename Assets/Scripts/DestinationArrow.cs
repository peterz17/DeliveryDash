using UnityEngine;

public class DestinationArrow : MonoBehaviour
{
    DeliveryZone[]    zones;
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
        zones = Object.FindObjectsByType<DeliveryZone>(FindObjectsSortMode.None);
    }

    void Update()
    {
        bool playing = GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;
        if (sr != null) sr.enabled = playing;
        if (!playing) return;

        transform.position = transform.parent.position + new Vector3(0f, 0.55f, -0.1f);

        string dest = GameManager.Instance.CurrentDestination;
        if (string.IsNullOrEmpty(dest)) return;

        foreach (var zone in zones)
        {
            if (zone.destinationName != dest) continue;

            // Point toward destination
            Vector3 dir = zone.transform.position - transform.parent.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Match zone color
            if (sr != null)
            {
                var zoneSr = zone.GetComponent<SpriteRenderer>();
                Color zoneColor = zoneSr != null ? zoneSr.color : Color.white;

                // Pulse alpha and scale
                float pulse = PulseMin + (PulseMax - PulseMin) * (Mathf.Sin(Time.time * PulseSpeed) * 0.5f + 0.5f);
                sr.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, pulse);
                transform.localScale = baseScale * (0.95f + 0.10f * (Mathf.Sin(Time.time * PulseSpeed) * 0.5f + 0.5f));
            }
            return;
        }
    }
}
