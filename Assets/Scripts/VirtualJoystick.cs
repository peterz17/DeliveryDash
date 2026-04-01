using UnityEngine;
using UnityEngine.EventSystems;

// Floating joystick: appears at touch point anywhere within the activation zone.
// Zone covers bottom-center (portrait) or bottom-left (landscape) and auto-resizes
// on orientation change via OnRectTransformDimensionsChange.
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public RectTransform bgCircle;  // visible ring — moved to touch origin on press
    public RectTransform handle;    // thumb nub, child of bgCircle

    public Vector2 Direction { get; private set; }

    RectTransform zoneRect;
    float radius;
    Vector2 touchOrigin;   // touch-down point in zoneRect local space (pivot = center)
    bool isPressed;

    void Awake()
    {
        zoneRect = GetComponent<RectTransform>();
    }

    void Start()
    {
        ApplyLayout();
        if (bgCircle != null) bgCircle.gameObject.SetActive(false);
    }

    // Fires on canvas resize / orientation change
    void OnRectTransformDimensionsChange()
    {
        ApplyLayout();
    }

    void ApplyLayout()
    {
        if (zoneRect == null) return;
        bool portrait = Screen.height > Screen.width;

        // Zone covers the activation region (transparent, full touch area)
        if (portrait)
        {
            // Portrait: wide band at bottom-center so thumb reaches it naturally
            zoneRect.anchorMin = new Vector2(0.04f, 0f);
            zoneRect.anchorMax = new Vector2(0.96f, 0.40f);
        }
        else
        {
            // Landscape: left-side block, thumb rests bottom-left
            zoneRect.anchorMin = new Vector2(0f, 0f);
            zoneRect.anchorMax = new Vector2(0.38f, 0.54f);
        }
        zoneRect.pivot     = new Vector2(0.5f, 0.5f);
        zoneRect.offsetMin = zoneRect.offsetMax = Vector2.zero;

        // Size the bgCircle and handle for each orientation
        if (bgCircle != null)
        {
            float bgSize     = portrait ? 240f : 210f;
            float handleSize = portrait ? 96f  : 84f;
            bgCircle.sizeDelta = new Vector2(bgSize, bgSize);
            if (handle != null)
                handle.sizeDelta = new Vector2(handleSize, handleSize);
            radius = bgSize * 0.5f;
        }
    }

    // ── Touch handlers ────────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData e)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            zoneRect, e.position, e.pressEventCamera, out touchOrigin)) return;

        isPressed = true;

        if (bgCircle != null)
        {
            bgCircle.gameObject.SetActive(true);
            bgCircle.anchoredPosition = touchOrigin;   // float to touch point
        }
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        Direction = Vector2.zero;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!isPressed) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            zoneRect, e.position, e.pressEventCamera, out Vector2 localPt)) return;

        Vector2 delta   = localPt - touchOrigin;
        Vector2 clamped = Vector2.ClampMagnitude(delta, radius);
        if (handle != null) handle.anchoredPosition = clamped;
        Direction = clamped.magnitude / radius < 0.15f ? Vector2.zero : clamped / radius;
    }

    public void OnPointerUp(PointerEventData e)
    {
        Reset();
    }

    public void Reset()
    {
        isPressed = false;
        Direction = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        if (bgCircle != null) bgCircle.gameObject.SetActive(false);
    }
}
