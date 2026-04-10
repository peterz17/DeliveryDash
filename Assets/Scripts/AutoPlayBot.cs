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
    float _lastUPress = -1f;
    bool showPathLine;
    LineRenderer pathLine;

    Dictionary<string, Vector2> zonePositions;

    // ── Navigation grid (A*) ───────────────────────────────────────────
    const float CELL_SIZE = 0.5f;
    const float MAP_W = 24f;
    const float MAP_H = 16f;
    int gridW, gridH;
    Vector2 gridOrigin;
    bool[,] walkable;
    bool gridReady;

    // ── Current path ───────────────────────────────────────────────────
    List<Vector2> path = new();
    int pathIndex;
    Vector2 lastPathTarget;
    float pathRebuildCooldown;

    // ── Stuck detection + power-up blacklist ──────────────────────────
    Vector2 lastPos;
    float stuckTimer;
    PowerUp currentPowerUpTarget;
    Dictionary<PowerUp, float> powerUpBlacklist = new();

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
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.yKey.wasPressedThisFrame)
            {
                if (Time.unscaledTime - _lastYPress < 0.5f) { Toggle(); _lastYPress = -1f; }
                else _lastYPress = Time.unscaledTime;
            }
            if (kb.uKey.wasPressedThisFrame)
            {
                if (Time.unscaledTime - _lastUPress < 0.5f)
                {
                    showPathLine = !showPathLine;
                    if (!showPathLine) ClearPathLine();
                    _lastUPress = -1f;
                }
                else _lastUPress = Time.unscaledTime;
            }
        }

        if (!IsActive)
        {
            currentDir = Vector2.zero;
            ClearPathLine();
            return;
        }
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            currentDir = Vector2.zero;
            ClearPathLine();
            return;
        }

        if (player == null)
        {
            player = GameManager.Instance.player;
            if (player == null) return;
            CacheZonePositions();
            BuildNavGrid();
        }

        Vector2 pos = (Vector2)player.transform.position;
        Vector2 finalTarget = GetTargetPosition();

        // Detour to a nearby reachable power-up
        PowerUp chosenPowerUp = FindNearbyPowerUp(pos, finalTarget);
        currentPowerUpTarget = chosenPowerUp;
        if (chosenPowerUp != null)
            finalTarget = (Vector2)chosenPowerUp.transform.position;

        // Rebuild path if target changed significantly or cooldown elapsed
        pathRebuildCooldown -= Time.deltaTime;
        if ((finalTarget - lastPathTarget).sqrMagnitude > 0.5f || pathRebuildCooldown <= 0f || path.Count == 0)
        {
            RebuildPath(pos, finalTarget);
            lastPathTarget = finalTarget;
            pathRebuildCooldown = 0.5f;
        }

        Vector2 steerTarget = GetCurrentWaypoint(pos, finalTarget);
        Vector2 toTarget = steerTarget - pos;

        if (toTarget.magnitude < 0.15f)
        {
            currentDir = Vector2.zero;
            UpdateStuckTracking(pos);
            return;
        }

        UpdatePathLine(pos);

        Vector2 desiredDir = toTarget.normalized;
        Vector2 avoidedDir = ApplyNPCAvoidance(pos, desiredDir);

        // Turn-rate limit: rotate current heading toward desired instead of
        // snapping. Makes the bot feel like a human steering a wheel.
        if (currentDir.sqrMagnitude < 0.001f)
        {
            currentDir = avoidedDir;
        }
        else
        {
            float currentAngle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;
            float desiredAngle = Mathf.Atan2(avoidedDir.y, avoidedDir.x) * Mathf.Rad2Deg;
            const float turnSpeedDeg = 540f; // degrees per second
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, desiredAngle, turnSpeedDeg * Time.deltaTime);
            float r = newAngle * Mathf.Deg2Rad;
            currentDir = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        }

        UpdateStuckTracking(pos);
    }

    // ── Path following ─────────────────────────────────────────────────
    // Look-ahead steering: advance pathIndex to the farthest waypoint that
    // still has a clear line of sight from the player, then aim a bit past it.
    // This produces smooth, human-feeling curves instead of snapping from
    // one grid cell to the next.
    Vector2 GetCurrentWaypoint(Vector2 pos, Vector2 fallback)
    {
        if (path.Count == 0) return fallback;

        // Drop waypoints behind us
        while (pathIndex < path.Count - 1 && (path[pathIndex] - pos).magnitude < 0.6f)
            pathIndex++;

        // Advance further as long as the next waypoint is still directly visible
        int look = pathIndex;
        while (look < path.Count - 1 && HasLineOfSight(pos, path[look + 1]))
            look++;

        pathIndex = look;
        return path[Mathf.Min(pathIndex, path.Count - 1)];
    }

    bool HasLineOfSight(Vector2 a, Vector2 b)
    {
        // Use grid walkability instead of physics so we respect the same
        // inflated obstacle margin A* used.
        if (!gridReady) return true;
        Vector2 dir = b - a;
        float dist = dir.magnitude;
        if (dist < 0.01f) return true;
        int steps = Mathf.CeilToInt(dist / (CELL_SIZE * 0.5f));
        Vector2 step = dir / steps;
        Vector2 p = a;
        for (int i = 0; i <= steps; i++)
        {
            var cell = WorldToCell(p);
            if (!InGrid(cell) || !walkable[cell.x, cell.y]) return false;
            p += step;
        }
        return true;
    }

    void RebuildPath(Vector2 from, Vector2 to)
    {
        path.Clear();
        pathIndex = 0;
        if (!gridReady)
        {
            path.Add(to);
            return;
        }

        var found = AStar(from, to);
        if (found != null && found.Count > 0)
        {
            path = SmoothPath(from, found);
        }
        else
        {
            path.Add(to);
        }
    }

    // String-pulling: collapse consecutive waypoints that share a direct
    // line of sight. Turns a stair-step grid path into smooth segments.
    List<Vector2> SmoothPath(Vector2 start, List<Vector2> raw)
    {
        var smoothed = new List<Vector2>();
        Vector2 anchor = start;
        int i = 0;
        while (i < raw.Count)
        {
            // Find the farthest point still visible from the anchor
            int farthest = i;
            for (int j = i + 1; j < raw.Count; j++)
            {
                if (HasLineOfSight(anchor, raw[j])) farthest = j;
                else break;
            }
            smoothed.Add(raw[farthest]);
            anchor = raw[farthest];
            i = farthest + 1;
        }
        return smoothed;
    }

    // ── A* implementation ──────────────────────────────────────────────
    List<Vector2> AStar(Vector2 fromWorld, Vector2 toWorld)
    {
        if (!gridReady) return null;

        Vector2Int start = WorldToCell(fromWorld);
        Vector2Int goal = WorldToCell(toWorld);
        start = NearestWalkable(start);
        goal = NearestWalkable(goal);
        if (start.x < 0 || goal.x < 0) return null;

        int total = gridW * gridH;
        var gScore = new float[total];
        var fScore = new float[total];
        var cameFrom = new int[total];
        var closed = new bool[total];
        for (int i = 0; i < total; i++) { gScore[i] = float.MaxValue; fScore[i] = float.MaxValue; cameFrom[i] = -1; }

        int startIdx = start.y * gridW + start.x;
        int goalIdx = goal.y * gridW + goal.x;
        gScore[startIdx] = 0f;
        fScore[startIdx] = Heuristic(start, goal);

        var open = new List<int> { startIdx };

        int guard = 0;
        while (open.Count > 0 && guard++ < 5000)
        {
            // Pick lowest f in open
            int bestIdx = 0;
            float bestF = fScore[open[0]];
            for (int i = 1; i < open.Count; i++)
            {
                float f = fScore[open[i]];
                if (f < bestF) { bestF = f; bestIdx = i; }
            }
            int current = open[bestIdx];
            if (current == goalIdx)
                return ReconstructPath(cameFrom, current);

            open.RemoveAt(bestIdx);
            closed[current] = true;

            int cx = current % gridW;
            int cy = current / gridW;

            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = cx + dx, ny = cy + dy;
                if (nx < 0 || nx >= gridW || ny < 0 || ny >= gridH) continue;
                if (!walkable[nx, ny]) continue;

                // Disallow diagonal through corners
                if (dx != 0 && dy != 0)
                {
                    if (!walkable[cx + dx, cy] || !walkable[cx, cy + dy]) continue;
                }

                int nIdx = ny * gridW + nx;
                if (closed[nIdx]) continue;

                float step = (dx != 0 && dy != 0) ? 1.41421f : 1f;
                float tentative = gScore[current] + step;
                if (tentative >= gScore[nIdx]) continue;

                cameFrom[nIdx] = current;
                gScore[nIdx] = tentative;
                fScore[nIdx] = tentative + Heuristic(new Vector2Int(nx, ny), goal);
                if (!open.Contains(nIdx)) open.Add(nIdx);
            }
        }

        return null;
    }

    float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) + (1.41421f - 2f) * Mathf.Min(dx, dy);
    }

    List<Vector2> ReconstructPath(int[] cameFrom, int current)
    {
        var result = new List<Vector2>();
        while (current != -1)
        {
            int cx = current % gridW;
            int cy = current / gridW;
            result.Add(CellToWorld(cx, cy));
            current = cameFrom[current];
        }
        result.Reverse();
        // Smooth: drop first cell if it's behind us (we're already there)
        if (result.Count > 1) result.RemoveAt(0);
        return result;
    }

    Vector2Int NearestWalkable(Vector2Int c)
    {
        if (InGrid(c) && walkable[c.x, c.y]) return c;
        for (int r = 1; r < 10; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                var p = new Vector2Int(c.x + dx, c.y + dy);
                if (InGrid(p) && walkable[p.x, p.y]) return p;
            }
        }
        return new Vector2Int(-1, -1);
    }

    bool InGrid(Vector2Int c) => c.x >= 0 && c.x < gridW && c.y >= 0 && c.y < gridH;

    Vector2Int WorldToCell(Vector2 world)
    {
        int x = Mathf.FloorToInt((world.x - gridOrigin.x) / CELL_SIZE);
        int y = Mathf.FloorToInt((world.y - gridOrigin.y) / CELL_SIZE);
        return new Vector2Int(x, y);
    }

    Vector2 CellToWorld(int x, int y)
    {
        return new Vector2(gridOrigin.x + (x + 0.5f) * CELL_SIZE, gridOrigin.y + (y + 0.5f) * CELL_SIZE);
    }

    // ── Grid construction ──────────────────────────────────────────────
    void BuildNavGrid()
    {
        gridW = Mathf.CeilToInt(MAP_W / CELL_SIZE);
        gridH = Mathf.CeilToInt(MAP_H / CELL_SIZE);
        gridOrigin = new Vector2(-MAP_W * 0.5f, -MAP_H * 0.5f);
        walkable = new bool[gridW, gridH];

        // Inflate check box to player half-size + safety margin so the bot
        // stays far enough from obstacles to never scrape them.
        Vector2 checkSize = new Vector2(CELL_SIZE + 0.35f, CELL_SIZE + 0.35f);
        Collider2D[] buf = new Collider2D[8];

        for (int x = 0; x < gridW; x++)
        for (int y = 0; y < gridH; y++)
        {
            Vector2 center = CellToWorld(x, y);
            int n = Physics2D.OverlapBoxNonAlloc(center, checkSize, 0f, buf);
            bool blocked = false;
            for (int i = 0; i < n; i++)
            {
                var c = buf[i];
                if (c == null) continue;
                if (c.isTrigger) continue;
                if (player != null && (c.transform == player.transform || c.transform.IsChildOf(player.transform))) continue;
                blocked = true;
                break;
            }
            walkable[x, y] = !blocked;
        }

        gridReady = true;
        int free = 0; foreach (var w in walkable) if (w) free++;
        Debug.Log($"[Bot] Nav grid built: {gridW}x{gridH}, {free}/{gridW * gridH} walkable cells");
    }

    // ── Stuck detection ────────────────────────────────────────────────
    void UpdateStuckTracking(Vector2 pos)
    {
        if (powerUpBlacklist.Count > 0)
        {
            var expired = new List<PowerUp>();
            foreach (var kv in powerUpBlacklist)
                if (Time.time > kv.Value || kv.Key == null) expired.Add(kv.Key);
            foreach (var k in expired) powerUpBlacklist.Remove(k);
        }

        if (currentDir.sqrMagnitude > 0.01f)
        {
            float moved = (pos - lastPos).magnitude;
            if (moved < 0.02f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > 0.6f)
                {
                    if (currentPowerUpTarget != null)
                    {
                        powerUpBlacklist[currentPowerUpTarget] = Time.time + 5f;
                        Debug.Log("[Bot] Stuck — blacklisting current power-up for 5s");
                        currentPowerUpTarget = null;
                    }
                    // Force immediate repath
                    pathRebuildCooldown = 0f;
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        lastPos = pos;
    }

    // ── Power-up selection ─────────────────────────────────────────────
    PowerUp FindNearbyPowerUp(Vector2 pos, Vector2 mainTarget)
    {
        float maxDetour = 4f;
        var powerUps = Object.FindObjectsByType<PowerUp>(FindObjectsSortMode.None);
        float bestScore = float.MaxValue;
        PowerUp best = null;

        foreach (var pu in powerUps)
        {
            if (pu == null || !pu.gameObject.activeInHierarchy) continue;
            if (powerUpBlacklist.ContainsKey(pu)) continue;

            Vector2 puPos = (Vector2)pu.transform.position;
            float distToMe = (puPos - pos).magnitude;
            float distToTarget = (mainTarget - puPos).magnitude;
            float directDist = (mainTarget - pos).magnitude;

            float detourCost = distToMe + distToTarget - directDist;
            if (detourCost > maxDetour) continue;
            if (distToMe > 6f) continue;

            // Ensure the power-up is on a walkable cell (otherwise A* would
            // route to the nearest walkable, which is misleading)
            if (gridReady)
            {
                var cell = WorldToCell(puPos);
                if (!InGrid(cell) || !walkable[cell.x, cell.y]) continue;
            }

            float score = distToMe + detourCost * 2f;
            if (score < bestScore)
            {
                bestScore = score;
                best = pu;
            }
        }

        return best;
    }

    // ── NPC avoidance (unchanged) ──────────────────────────────────────
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

            float dot = Vector2.Dot(desiredDir, -diff.normalized);
            if (dot < 0.1f) continue;

            float proximity = 1f - dist / outerRadius;
            float strength = proximity * proximity * dot;

            Vector2 perp = Vector2.Perpendicular(desiredDir);
            float side = Vector2.Dot(perp, diff) > 0 ? 1f : -1f;
            avoidForce += perp * side * strength;
        }

        Vector2 result = desiredDir + avoidForce * 1.2f;
        return result.sqrMagnitude > 0.001f ? result.normalized : desiredDir;
    }

    public Vector2 GetDirection() => currentDir;

    void EnsurePathLine()
    {
        if (pathLine != null) return;
        var go = new GameObject("AutoPlayBot_PathLine");
        go.transform.SetParent(transform, false);
        pathLine = go.AddComponent<LineRenderer>();
        pathLine.material = new Material(Shader.Find("Sprites/Default"));
        pathLine.startColor = new Color(0.2f, 1f, 0.4f, 0.9f);
        pathLine.endColor = new Color(0.2f, 1f, 0.4f, 0.9f);
        pathLine.startWidth = 0.1f;
        pathLine.endWidth = 0.1f;
        pathLine.numCapVertices = 4;
        pathLine.numCornerVertices = 4;
        pathLine.sortingOrder = 100;
        pathLine.useWorldSpace = true;
    }

    void UpdatePathLine(Vector2 pos)
    {
        if (!showPathLine) { ClearPathLine(); return; }
        EnsurePathLine();
        if (path.Count == 0) { pathLine.positionCount = 0; return; }

        int remaining = path.Count - pathIndex;
        pathLine.positionCount = remaining + 1;
        pathLine.SetPosition(0, new Vector3(pos.x, pos.y, -0.1f));
        for (int i = 0; i < remaining; i++)
        {
            Vector2 p = path[pathIndex + i];
            pathLine.SetPosition(i + 1, new Vector3(p.x, p.y, -0.1f));
        }
    }

    void ClearPathLine()
    {
        if (pathLine != null) pathLine.positionCount = 0;
    }

    void CacheZonePositions()
    {
        zonePositions = new Dictionary<string, Vector2>();

        var pickup = Object.FindObjectsByType<PickupZone>(FindObjectsSortMode.None);
        if (pickup.Length > 0)
            zonePositions["pickup"] = (Vector2)pickup[0].transform.position;
        else
            zonePositions["pickup"] = Vector2.zero;

        var deliveries = Object.FindObjectsByType<DeliveryZone>(FindObjectsSortMode.None);
        foreach (var dz in deliveries)
        {
            zonePositions[dz.destinationName] = (Vector2)dz.transform.position;
            string normalized = dz.destinationName.ToLower().Replace(" ", "_");
            if (!zonePositions.ContainsKey(normalized))
                zonePositions[normalized] = (Vector2)dz.transform.position;
        }
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
        path.Clear();
        gridReady = false;
        Debug.Log("[AutoPlay] A* bot activated");
    }

    public void Deactivate()
    {
        IsActive = false;
        currentDir = Vector2.zero;
        ClearPathLine();
        Debug.Log("[AutoPlay] Bot deactivated");
    }

    public void Toggle()
    {
        if (IsActive) Deactivate();
        else Activate();
    }

    // Debug: draw path + grid in scene view
    void OnDrawGizmos()
    {
        if (!IsActive || !gridReady) return;

        Gizmos.color = new Color(1, 0, 0, 0.3f);
        for (int x = 0; x < gridW; x++)
        for (int y = 0; y < gridH; y++)
            if (!walkable[x, y])
                Gizmos.DrawCube(CellToWorld(x, y), new Vector3(CELL_SIZE, CELL_SIZE, 0.1f));

        if (path.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < path.Count - 1; i++)
                Gizmos.DrawLine(path[i], path[i + 1]);
            for (int i = pathIndex; i < path.Count; i++)
                Gizmos.DrawSphere(path[i], 0.1f);
        }
    }
}
