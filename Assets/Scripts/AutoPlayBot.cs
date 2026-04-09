using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AutoPlayBot : MonoBehaviour
{
    public static AutoPlayBot Instance { get; private set; }

    public bool IsActive { get; private set; }

    PlayerController player;
    Vector2 currentDir;
    float _lastYPress = -1f;

    // Zone positions — populated from actual scene objects at runtime
    Dictionary<string, Vector2> zonePositions;

    // Waypoint navigation: go via center corridor to avoid buildings
    // Route: current pos → align X to road → align Y to road → target
    List<Vector2> waypoints = new();
    int waypointIndex;
    string lastTarget = "";
    bool lastHadPackage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("AutoPlayBot");
        go.AddComponent<AutoPlayBot>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.yKey.wasPressedThisFrame)
        {
            if (Time.unscaledTime - _lastYPress < 0.5f)
            {
                Toggle();
                _lastYPress = -1f;
            }
            else
                _lastYPress = Time.unscaledTime;
        }

        if (!IsActive)
        {
            currentDir = Vector2.zero;
            return;
        }
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            currentDir = Vector2.zero;
            return;
        }

        if (player == null)
        {
            player = GameManager.Instance.player;
            if (player == null) return;
            CacheZonePositions();
        }

        Vector2 finalTarget = GetTargetPosition();
        Vector2 pos = (Vector2)player.transform.position;

        // Navigate via roads to avoid buildings and wrong zone triggers
        Vector2 steerTarget;

        if (!player.HasPackage)
        {
            // Going to pickup — go directly
            steerTarget = finalTarget;
        }
        else
        {
            // Going to corner zone — use L-shaped path
            // Route: pos → (target.x, pos.y clamped to road) → target
            // But ONLY move along one axis at a time to avoid diagonal into wrong zone

            float dx = Mathf.Abs(pos.x - finalTarget.x);
            float dy = Mathf.Abs(pos.y - finalTarget.y);

            if (dx > 1.5f)
            {
                // First: move horizontally along Y=0 road to align X
                steerTarget = new Vector2(finalTarget.x, 0f);
            }
            else
            {
                // Then: move vertically straight to target
                steerTarget = new Vector2(finalTarget.x, finalTarget.y);
            }
        }

        // Detour to nearby power-up if close to our path
        Vector2 powerUpTarget = FindNearbyPowerUp(pos, steerTarget);
        if (powerUpTarget.sqrMagnitude > 0.001f)
            steerTarget = powerUpTarget;

        Vector2 toTarget = steerTarget - pos;

        if (toTarget.magnitude < 0.15f)
        {
            currentDir = Vector2.zero;
            return;
        }

        Vector2 desiredDir = toTarget.normalized;
        currentDir = ApplyNPCAvoidance(pos, desiredDir);
    }

    Vector2 FindNearbyPowerUp(Vector2 pos, Vector2 mainTarget)
    {
        float maxDetour = 4f; // max distance off-path to grab a power-up
        var powerUps = Object.FindObjectsByType<PowerUp>(FindObjectsSortMode.None);
        float bestScore = float.MaxValue;
        Vector2 bestPos = Vector2.zero;

        foreach (var pu in powerUps)
        {
            Vector2 puPos = (Vector2)pu.transform.position;
            float distToMe = (puPos - pos).magnitude;
            float distToTarget = (mainTarget - puPos).magnitude;
            float directDist = (mainTarget - pos).magnitude;

            // Only detour if power-up is roughly on the way (total detour < direct + maxDetour)
            float detourCost = distToMe + distToTarget - directDist;
            if (detourCost > maxDetour) continue;
            if (distToMe > 6f) continue; // too far away

            // Prefer closer power-ups with less detour
            float score = distToMe + detourCost * 2f;
            if (score < bestScore)
            {
                bestScore = score;
                bestPos = puPos;
            }
        }

        return bestPos;
    }

    Vector2 ApplyNPCAvoidance(Vector2 pos, Vector2 desiredDir)
    {
        Vector2 avoidForce = Vector2.zero;
        var npcs = Object.FindObjectsByType<NPCCar>(FindObjectsSortMode.None);
        float outerRadius = 3.0f;

        foreach (var npc in npcs)
        {
            if (!npc.gameObject.activeSelf) continue;
            Vector2 npcPos = (Vector2)npc.transform.position;
            Vector2 diff = pos - npcPos;
            float dist = diff.magnitude;

            if (dist > outerRadius || dist < 0.01f) continue;

            // Only avoid NPCs that are roughly ahead or to the side
            float dot = Vector2.Dot(desiredDir, -diff.normalized);
            if (dot < 0.1f) continue;

            // Stronger response when closer — quadratic falloff
            float proximity = 1f - dist / outerRadius;
            float strength = proximity * proximity * dot;

            Vector2 perp = Vector2.Perpendicular(desiredDir);
            float side = Vector2.Dot(perp, diff) > 0 ? 1f : -1f;
            avoidForce += perp * side * strength;
        }

        Vector2 result = desiredDir + avoidForce * 1.2f;
        return result.sqrMagnitude > 0.001f ? result.normalized : desiredDir;
    }

    void BuildWaypoints(Vector2 from, Vector2 to)
    {
        waypoints.Clear();
        waypointIndex = 0;

        // Simple corridor navigation:
        // Buildings are at the 4 corners. The roads are along X=0 and Y=0 corridors.
        // Strategy: go to (from.x, 0) → (to.x, 0) → to
        // If already near center, skip intermediate points.

        float corridorY = 0f;
        bool fromNearCenter = Mathf.Abs(from.y) < 2f;
        Vector2 pickupPos = zonePositions != null && zonePositions.ContainsKey("pickup") ? zonePositions["pickup"] : Vector2.zero;
        bool toIsCenter = (to - pickupPos).sqrMagnitude < 1f;

        if (toIsCenter)
        {
            // Going to pickup (0,0) — just go via corridor
            if (!fromNearCenter)
                waypoints.Add(new Vector2(from.x, corridorY));
            waypoints.Add(to);
        }
        else
        {
            // Going to a corner zone — route via corridor
            if (!fromNearCenter)
                waypoints.Add(new Vector2(from.x, corridorY));
            waypoints.Add(new Vector2(to.x, corridorY));
            waypoints.Add(to);
        }
    }

    public Vector2 GetDirection() => currentDir;

    void CacheZonePositions()
    {
        zonePositions = new Dictionary<string, Vector2>();

        // Find pickup zone
        var pickup = Object.FindObjectsByType<PickupZone>(FindObjectsSortMode.None);
        if (pickup.Length > 0)
            zonePositions["pickup"] = (Vector2)pickup[0].transform.position;
        else
            zonePositions["pickup"] = Vector2.zero;

        // Find delivery zones — store both original name and normalized key
        var deliveries = Object.FindObjectsByType<DeliveryZone>(FindObjectsSortMode.None);
        foreach (var dz in deliveries)
        {
            zonePositions[dz.destinationName] = (Vector2)dz.transform.position;
            // Also store normalized key (lowercase, spaces→underscores)
            string normalized = dz.destinationName.ToLower().Replace(" ", "_");
            if (!zonePositions.ContainsKey(normalized))
                zonePositions[normalized] = (Vector2)dz.transform.position;
        }

        Debug.Log($"[Bot] Cached {zonePositions.Count} zones: {string.Join(", ", zonePositions.Keys)}");
        foreach (var kv in zonePositions)
            Debug.Log($"[Bot]   {kv.Key} → {kv.Value}");
    }

    Vector2 GetTargetPosition()
    {
        if (zonePositions == null) return Vector2.zero;

        if (!player.HasPackage)
            return zonePositions.ContainsKey("pickup") ? zonePositions["pickup"] : Vector2.zero;

        string dest = GameManager.Instance.CurrentDestination;
        if (!string.IsNullOrEmpty(dest) && zonePositions.ContainsKey(dest))
            return zonePositions[dest];

        return zonePositions.ContainsKey("pickup") ? zonePositions["pickup"] : Vector2.zero;
    }

    public void Activate()
    {
        IsActive = true;
        player = null;
        currentDir = Vector2.zero;
        waypoints.Clear();
        lastTarget = "";
        Debug.Log("[AutoPlay] Pro bot activated");
    }

    public void Deactivate()
    {
        IsActive = false;
        currentDir = Vector2.zero;
        Debug.Log("[AutoPlay] Bot deactivated");
    }

    public void Toggle()
    {
        if (IsActive) Deactivate();
        else Activate();
    }
}
