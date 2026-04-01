using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    public bool HasPackage { get; private set; }

    public VirtualJoystick joystick;
    public Sprite packageSprite;

    Rigidbody2D rb;
    Transform spriteChild;
    SpriteRenderer auraRenderer;
    SpriteRenderer packageIconRenderer;
    Vector2 moveInput;
    Vector3 startPosition;
    float boostTimer;
    const float BoostSpeed = 7.5f;
    const float BoostDuration = 0.4f;

    public bool HasShield { get; private set; }
    float shieldTimer;
    float rocketTimer;
    const float RocketMultiplier = 2f;
    const float ShieldDuration = 3f;
    const float RocketDuration = 2f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        spriteChild = transform.Find("Sprite");

        var auraGO = new GameObject("Aura");
        auraGO.transform.SetParent(transform, false);
        auraRenderer = auraGO.AddComponent<SpriteRenderer>();
        auraRenderer.sprite = SpriteFactory.AuraDisc();
        auraRenderer.sortingOrder = 9;   // behind the car sprite (order 10)
        auraRenderer.color = Color.clear;

        var indicatorGO = new GameObject("OffScreenIndicator");
        indicatorGO.AddComponent<OffScreenIndicator>();

        var pkgGO = new GameObject("PackageIcon");
        pkgGO.transform.SetParent(spriteChild != null ? spriteChild : transform, false);
        pkgGO.transform.localPosition = new Vector3(0f, -0.12f, -0.1f);
        pkgGO.transform.localScale    = new Vector3(0.22f, 0.22f, 1f);
        packageIconRenderer = pkgGO.AddComponent<SpriteRenderer>();
        packageIconRenderer.sprite       = packageSprite != null ? packageSprite : CreatePackageSprite();
        packageIconRenderer.sortingOrder = 11;  // above car sprite
        packageIconRenderer.enabled      = false;
    }

    static Sprite CreatePackageSprite()
    {
        const int S = 32;
        var px = new Color[S * S];
        Color box  = new Color(0.95f, 0.75f, 0.20f);  // warm yellow
        Color band = new Color(1.00f, 0.30f, 0.20f);  // red ribbon
        Color bow  = new Color(1.00f, 0.20f, 0.15f);  // bow

        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                bool border = (x == 0 || x == S - 1 || y == 0 || y == S - 1);
                bool ribbon = (x >= S / 2 - 2 && x <= S / 2 + 1) || (y >= S / 2 - 2 && y <= S / 2 + 1);
                bool bowLeft  = (y > S * 3 / 4) && (x > S / 4 && x < S / 2 - 2) && (Mathf.Abs(x - S / 2 + 1) + Mathf.Abs(y - S + 4) < 8);
                bool bowRight = (y > S * 3 / 4) && (x >= S / 2 + 2 && x < S * 3 / 4) && (Mathf.Abs(x - S / 2 + 1) + Mathf.Abs(y - S + 4) < 8);
                Color c = border ? new Color(0.6f, 0.4f, 0.1f) :
                          bowLeft || bowRight ? bow :
                          ribbon ? band : box;
                px[y * S + x] = c;
            }
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), Vector2.one * 0.5f, 32f);
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            moveInput = Vector2.zero;
            return;
        }

        bool wasShield = shieldTimer > 0f;
        bool wasRocket = rocketTimer > 0f;

        shieldTimer -= Time.deltaTime;
        if (shieldTimer < 0f) shieldTimer = 0f;
        HasShield = shieldTimer > 0f;

        rocketTimer -= Time.deltaTime;
        if (rocketTimer < 0f) rocketTimer = 0f;

        // if (wasShield && shieldTimer == 0f) PowerUpManager.OnPowerUpDeactivated?.Invoke("shield");
        if (wasShield && shieldTimer == 0f)
        {
            PowerUpManager.RaisePowerUpDeactivated("shield");
        }
        if (wasRocket && rocketTimer == 0f)
        {
            PowerUpManager.RaisePowerUpDeactivated("rocket");
        }

        UpdateAura();

        var kb = Keyboard.current;
        float x = 0f, y = 0f;
        if (kb != null)
        {
            x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
              - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
              - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
        }

        var gp = Gamepad.current;
        if (gp != null)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            x += stick.x;
            y += stick.y;
            if (gp.dpad.right.isPressed) x += 1f;
            if (gp.dpad.left.isPressed)  x -= 1f;
            if (gp.dpad.up.isPressed)    y += 1f;
            if (gp.dpad.down.isPressed)  y -= 1f;
        }

        Vector2 joyDir = (joystick != null) ? joystick.Direction : Vector2.zero;
        moveInput = Vector2.ClampMagnitude(new Vector2(x, y) + joyDir, 1f);

        if (moveInput.sqrMagnitude > 0.01f && spriteChild != null)
        {
            float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg - 90f;
            spriteChild.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    void FixedUpdate()
    {
        boostTimer -= Time.fixedDeltaTime;
        float speed = boostTimer > 0f ? BoostSpeed : moveSpeed;
        if (rocketTimer > 0f) speed *= RocketMultiplier;
        rb.linearVelocity = moveInput * speed;
    }

    void UpdateAura()
    {
        if (auraRenderer == null) return;

        bool isShield = shieldTimer > 0f;
        bool isRocket = rocketTimer > 0f;
        bool isClock = PowerUpManager.Instance != null && PowerUpManager.Instance.IsFrozen;

        if (!isShield && !isRocket && !isClock)
        {
            auraRenderer.color = Color.clear;
            return;
        }

        Color baseColor;
        float pulseFreq;
        float activeTimer;

        if (isShield)
        {
            baseColor = new Color(0f, 0.8f, 1f);   // cyan
            pulseFreq = 3f;                          // slow breath
            activeTimer = shieldTimer;
        }
        else if (isRocket)
        {
            baseColor = new Color(1f, 0.5f, 0f);   // orange
            pulseFreq = 9f;                          // fast strobe
            activeTimer = rocketTimer;
        }
        else
        {
            baseColor = new Color(0.6f, 1f, 0.2f); // yellow-green
            pulseFreq = 0f;                          // steady
            activeTimer = 2f;
        }

        float pulse = pulseFreq > 0f
            ? 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * pulseFreq))
            : 0.85f;

        float fade = activeTimer < 0.5f ? activeTimer / 0.5f : 1f;
        float scale = 2.2f + 0.15f * pulse;

        auraRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        auraRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.55f * pulse * fade);
    }

    public void TriggerSpeedBoost()
    {
        boostTimer = BoostDuration;
    }

    void ShakeCamera(float duration, float magnitude)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow != null) follow.Shake(duration, magnitude);
    }

    public void PickupPowerUp(PowerUpType type)
    {
        ShakeCamera(0.08f, 0.06f);
        AudioManager.Play(a => a.PlayPowerUpPickup(type));

        switch (type)
        {
            case PowerUpType.Shield:
                shieldTimer = ShieldDuration;
                HasShield = true;
                PowerUpManager.RaisePowerUpActivated("shield", ShieldDuration);
                break;
            case PowerUpType.Rocket:
                rocketTimer = RocketDuration;
                PowerUpManager.RaisePowerUpActivated("rocket", RocketDuration);
                break;
            case PowerUpType.Clock:
                if (PowerUpManager.Instance != null) PowerUpManager.Instance.StartClockEffect();
                break;
        }

        string feedbackKey = type switch
        {
            PowerUpType.Shield => "feedback_shield",
            PowerUpType.Rocket => "feedback_rocket",
            PowerUpType.Clock  => "feedback_clock",
            _                  => null
        };
        if (feedbackKey != null && GameManager.Instance != null)
            GameManager.Instance.uiManager.ShowFeedback(LocalizationManager.L(feedbackKey), true);
    }

    public void PickupPackage()
    {
        HasPackage = true;
        if (packageIconRenderer != null) packageIconRenderer.enabled = true;
        GameManager.Instance.uiManager.UpdateCarryStatus(true);
    }

    public void DropPackage()
    {
        HasPackage = false;
        if (packageIconRenderer != null) packageIconRenderer.enabled = false;
        if (GameManager.Instance != null)
            GameManager.Instance.uiManager.UpdateCarryStatus(false);
    }

    public void ResetPlayer()
    {
        transform.position = startPosition;
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        boostTimer = 0f;
        shieldTimer = 0f;
        rocketTimer = 0f;
        HasShield = false;
        if (auraRenderer != null) auraRenderer.color = Color.clear;
        if (joystick != null) joystick.Reset();
        HasPackage = false;
        if (packageIconRenderer != null) packageIconRenderer.enabled = false;
        if (GameManager.Instance != null)
            GameManager.Instance.uiManager.UpdateCarryStatus(false);
    }
}
