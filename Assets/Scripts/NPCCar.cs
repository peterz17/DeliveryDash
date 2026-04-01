using UnityEngine;

public class NPCCar : MonoBehaviour
{
    public float speed = 3f;
    public float baseSpeed;
    public float rangeMin = -7f;
    public float rangeMax =  7f;
    public bool moveHorizontal = true;
    public bool isBoss = false;

    static readonly Collider2D[] _overlapBuf = new Collider2D[16];

    Rigidbody2D rb;
    Transform spriteChild;
    SpriteRenderer bossAura;
    int dir = 1;
    float pauseTimer;
    float nextPauseCountdown;
    const float AvgPauseInterval = 7f;
    const float MaxPauseDuration = 1.0f;
    const float LookAheadDist = 1.5f;
    const float SwerveDist = 2.8f;
    const float PickupSwerveDist = 1.5f;
    const float SwerveSpeedMul = 1.4f;
    const float ForwardDuringLateral = 5.0f;
    const float PickupForbiddenRadius = 2.2f;
    int instanceId;
    int swervePhase;       // 0=none, 1=lateral out, 2=forward, 3=lateral back
    int swerveSign;
    float swerveTraveled;
    float activeSwerveLen; // actual lateral distance for current swerve
    bool avoidingPickupZone;

    void Awake()
    {
        baseSpeed = speed;
        rb = GetComponent<Rigidbody2D>();
        spriteChild = transform.Find("Sprite");
        nextPauseCountdown = AvgPauseInterval * (0.5f + Random.value);

        instanceId = gameObject.GetInstanceID();
        if (isBoss) CreateBossAura();
    }

    void CreateBossAura()
    {
        var go = new GameObject("BossAura");
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * 2.5f;

        bossAura = go.AddComponent<SpriteRenderer>();
        bossAura.sprite = SpriteFactory.AuraDisc();
        bossAura.sortingOrder = 5; // below car sprite (order 6)
        bossAura.color = new Color(1f, 0.2f, 0.15f, 0.45f); // red tint
    }

    void FixedUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            return;

        if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsFrozen)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Occasional pause behavior
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        nextPauseCountdown -= Time.fixedDeltaTime;
        if (nextPauseCountdown <= 0f)
        {
            nextPauseCountdown = AvgPauseInterval * (0.5f + Random.value);
            if (Random.value < 0.35f)
                pauseTimer = Random.Range(0.3f, MaxPauseDuration);
        }

        MoveNormal();
    }

    void Update()
    {
        if (bossAura == null) return;
        float pulse = 0.35f + 0.15f * Mathf.Sin(Time.time * 4f);
        float scale = 2.4f + 0.2f * Mathf.Sin(Time.time * 3f);
        bossAura.color = new Color(1f, 0.2f, 0.15f, pulse);
        bossAura.transform.localScale = Vector3.one * scale;
    }

    void MoveNormal()
    {
        float pos = moveHorizontal ? rb.position.x : rb.position.y;
        if (pos >= rangeMax && dir > 0) dir = -1;
        if (pos <= rangeMin && dir < 0) dir =  1;

        Vector2 forward = (moveHorizontal ? Vector2.right : Vector2.up) * dir;
        Vector2 lateral = moveHorizontal ? Vector2.up : Vector2.right;
        float swerveStep = speed * SwerveSpeedMul * Time.fixedDeltaTime;

        // ── Active swerve ──
        if (swervePhase > 0)
        {
            swerveTraveled += swerveStep;

            if (swervePhase == 1) // lateral out
            {
                rb.MovePosition(rb.position + lateral * swerveSign * swerveStep);
                RotateSprite(lateral * swerveSign);
                if (swerveTraveled >= activeSwerveLen)
                { swerveTraveled = 0f; swervePhase = 2; }
            }
            else if (swervePhase == 2) // forward while offset
            {
                if (IsObstacleAhead())
                { swerveTraveled = 0f; swervePhase = 3; return; }
                rb.MovePosition(rb.position + forward * swerveStep);
                RotateSprite(forward);
                if (swerveTraveled >= ForwardDuringLateral)
                { swerveTraveled = 0f; swervePhase = 3; }
            }
            else // phase 3: lateral back (always complete — return to lane)
            {
                rb.MovePosition(rb.position + lateral * -swerveSign * swerveStep);
                RotateSprite(lateral * -swerveSign);
                if (swerveTraveled >= activeSwerveLen)
                { swerveTraveled = 0f; swervePhase = 0; }
            }
            return;
        }

        // ── Check for obstacle ahead — reverse ──
        if (IsObstacleAhead())
        {
            dir = -dir;
            return;
        }

        // ── Pickup zone avoidance ──
        if (!avoidingPickupZone)
        {
            float crossAxis = moveHorizontal ? rb.position.y : rb.position.x;
            bool headingToCenter = (pos > 0 && dir < 0) || (pos < 0 && dir > 0);
            float distToCenter = Mathf.Abs(pos);

            if (Mathf.Abs(crossAxis) < PickupForbiddenRadius && headingToCenter && distToCenter < PickupForbiddenRadius + 1.5f)
            {
                avoidingPickupZone = true;
                int sign = crossAxis >= 0f ? 1 : -1;
                if (Mathf.Abs(crossAxis) < 0.3f) sign = (instanceId % 2 == 0) ? 1 : -1;
                if (TryStartSwerve(sign, lateral, PickupSwerveDist) || TryStartSwerve(-sign, lateral, PickupSwerveDist))
                    return;
                avoidingPickupZone = false;
            }
        }
        if (avoidingPickupZone && swervePhase == 0)
            avoidingPickupZone = false;

        // ── NPC ahead — try swerve, else slow down ──
        if (IsNPCAhead())
        {
            int sign = Random.value > 0.5f ? 1 : -1;
            if (!TryStartSwerve(sign, lateral) && !TryStartSwerve(-sign, lateral))
                dir = -dir; // both sides blocked, just reverse
            return;
        }

        rb.MovePosition(rb.position + forward * (speed * Time.fixedDeltaTime));
        RotateSprite(forward);
    }

    bool TryStartSwerve(int sign, Vector2 lateral, float dist = SwerveDist)
    {
        Vector2 swerveTarget = rb.position + lateral * sign * dist;
        if (IsObstacleAt(swerveTarget)) return false;
        swerveSign = sign;
        swervePhase = 1;
        swerveTraveled = 0f;
        activeSwerveLen = dist;
        return true;
    }

    bool IsObstacleAhead()
    {
        Vector2 checkPos = rb.position + (moveHorizontal
            ? new Vector2(dir * LookAheadDist, 0f)
            : new Vector2(0f, dir * LookAheadDist));
        return IsObstacleAt(checkPos);
    }

    bool IsObstacleAt(Vector2 pos)
    {
        int count = Physics2D.OverlapCircleNonAlloc(pos, 0.6f, _overlapBuf);
        for (int i = 0; i < count; i++)
        {
            if (_overlapBuf[i].isTrigger) continue;
            if (_overlapBuf[i].GetComponent<NPCCar>() != null) continue;
            if (_overlapBuf[i].GetComponent<PlayerController>() != null) continue;
            return true;
        }
        return false;
    }

    bool IsNPCAhead()
    {
        Vector2 checkPos = rb.position + (moveHorizontal
            ? new Vector2(dir * LookAheadDist, 0f)
            : new Vector2(0f, dir * LookAheadDist));

        int count = Physics2D.OverlapCircleNonAlloc(checkPos, 0.5f, _overlapBuf);
        for (int i = 0; i < count; i++)
        {
            if (_overlapBuf[i].gameObject == gameObject) continue;
            var other = _overlapBuf[i].GetComponent<NPCCar>();
            if (other == null) continue;
            if (other.moveHorizontal != moveHorizontal) continue;
            if (instanceId < other.instanceId) continue;
            return true;
        }
        return false;
    }

    void RotateSprite(Vector2 moveDir)
    {
        if (spriteChild == null) return;
        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg - 90f;
        spriteChild.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void SetSprite(Sprite sprite)
    {
        if (spriteChild == null) return;
        var sr = spriteChild.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = sprite;
    }

    public void RandomizePosition()
    {
        for (int i = 0; i < 20; i++)
        {
            float roadPos = Random.Range(rangeMin, rangeMax);
            Vector2 candidate = moveHorizontal
                ? new Vector2(roadPos, rb.position.y)
                : new Vector2(rb.position.x, roadPos);
            if (candidate.sqrMagnitude >= PickupForbiddenRadius * PickupForbiddenRadius
                && !IsObstacleAt(candidate))
            {
                rb.position = candidate;
                dir = Random.value > 0.5f ? 1 : -1;
                return;
            }
        }
        // fallback: place at edge
        rb.position = moveHorizontal
            ? new Vector2(rangeMax, rb.position.y)
            : new Vector2(rb.position.x, rangeMax);
        dir = -1;
    }

    public void RandomizePositionAwayFrom(Vector2 playerPos, float minDist)
    {
        const float minNpcDist = 2.5f;
        for (int attempt = 0; attempt < 30; attempt++)
        {
            RandomizePosition();
            if (Vector2.Distance(rb.position, playerPos) < minDist) continue;
            if (!IsTooCloseToOtherNPC(minNpcDist)) return;
        }
        // fallback: spread evenly along the lane
        float playerRoadPos = moveHorizontal ? playerPos.x : playerPos.y;
        float safePos = playerRoadPos > 0f ? rangeMin : rangeMax;
        rb.position = moveHorizontal
            ? new Vector2(safePos, rb.position.y)
            : new Vector2(rb.position.x, safePos);
    }

    bool IsTooCloseToOtherNPC(float minDist)
    {
        int count = Physics2D.OverlapCircleNonAlloc(rb.position, minDist, _overlapBuf);
        for (int i = 0; i < count; i++)
        {
            if (_overlapBuf[i].gameObject == gameObject) continue;
            if (_overlapBuf[i].GetComponent<NPCCar>() != null) return true;
        }
        return false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var playerCtrl = other.GetComponent<PlayerController>();
        if (playerCtrl == null) return;
        if (playerCtrl.HasShield) return;
        if (GameManager.Instance == null) return;
        GameManager.Instance.HitByNPC();
    }
}
