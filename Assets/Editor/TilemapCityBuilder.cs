using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Builds a tilemap-based city from the Kenney Tiny Town spritesheet.
/// Run via: Delivery Dash ▸ Build Tilemap City
///
/// After building, edit manually: Window ▸ 2D ▸ Tile Palette
/// → Create New Palette → drag tile assets from Assets/Tiles/
/// </summary>
public static class TilemapCityBuilder
{
    // ── Spritesheet ─────────────────────────────────────────────────────────
    const string SheetPath = "Assets/Sprites/Tilemap/Tilemap/tilemap_packed.png";
    const string TileDir   = "Assets/Tiles";
    const int TILE_PX = 16, COLS = 27, ROWS = 18;
    const int TEX_W = 432, TEX_H = 288;
    const int PPU = 16; // 16px tile = 1 world unit
    const int TOTAL = COLS * ROWS; // 486

    // ── Tile index from spritesheet grid (col, row from top-left, 0-based) ──
    static int T(int col, int row) => row * COLS + col;

    // ── Key tile indices ────────────────────────────────────────────────────
    // Spritesheet layout (tilemap_packed.png, 27×18):
    //   Cols  0–5:  Park / playground (green, teal)
    //   Cols  6–9:  Roads, sidewalks, pavement (grays)
    //   Cols 10–13: Red/brick-roof buildings
    //   Cols 14–17: Brown/wood-roof buildings
    //   Cols 18–26: Characters (rows 0-6), trees (rows 7-10), vehicles (rows 13+)
    //
    // If a tile looks wrong, change the col/row here and re-run Build.
    static readonly int ROAD       = T(7, 0);
    static readonly int ROAD_ALT   = T(7, 1);
    static readonly int SIDEWALK   = T(6, 0);
    static readonly int SIDEWALK2  = T(8, 0);
    static readonly int GRASS      = T(1, 1);
    static readonly int GRASS_ALT  = T(2, 1);
    static readonly int CONCRETE   = T(9, 0);
    static readonly int CONCRETE2  = T(9, 1);

    // Park border tiles
    static readonly int PARK_TL = T(0, 0);
    static readonly int PARK_T  = T(1, 0);
    static readonly int PARK_TR = T(2, 0);
    static readonly int PARK_L  = T(0, 1);
    static readonly int PARK_C  = T(1, 1); // same as GRASS
    static readonly int PARK_R  = T(2, 1);
    static readonly int PARK_BL = T(0, 2);
    static readonly int PARK_B  = T(1, 2);
    static readonly int PARK_BR = T(2, 2);

    // Building roof tiles (for decoration on blocks)
    static readonly int ROOF_RED_TL = T(10, 0);
    static readonly int ROOF_RED_TR = T(11, 0);
    static readonly int ROOF_RED_BL = T(10, 1);
    static readonly int ROOF_RED_BR = T(11, 1);

    static readonly int ROOF_BRN_TL = T(14, 0);
    static readonly int ROOF_BRN_TR = T(15, 0);
    static readonly int ROOF_BRN_BL = T(14, 1);
    static readonly int ROOF_BRN_BR = T(15, 1);

    // ── Map extents (tile coordinates) ──────────────────────────────────────
    const int X_MIN = -11, X_MAX = 10; // 22 tiles wide  → 22 world units
    const int Y_MIN = -8,  Y_MAX = 7;  // 16 tiles tall  → 16 world units

    // Road grid: each road is 2 tiles wide
    // Vertical roads centered near x = -6.5, 0, 6.5 (matching game layout)
    // Horizontal roads centered near y = -4.5, 0, 4.5
    static readonly HashSet<int> VRoadX = new() { -7, -6,  -1, 0,  6, 7 };
    static readonly HashSet<int> HRoadY = new() { -5, -4,  -1, 0,  3, 4 };

    // ═════════════════════════════════════════════════════════════════════════
    [MenuItem("Delivery Dash/Build Tilemap City")]
    public static void BuildCity()
    {
        // 1. Import & slice spritesheet into 16×16 sprites
        if (!SliceSpriteSheet()) return;

        // 2. Create a Tile asset for every sprite (486 tiles → Assets/Tiles/)
        var tiles = BuildTileAssets();
        if (tiles == null) return;

        // 3. Replace old sprite-based environment with Grid + Tilemap
        var oldEnv = FindRoot("--- Environment ---");
        if (oldEnv != null && oldEnv.GetComponent<Grid>() == null)
        {
            Undo.RegisterCompleteObjectUndo(oldEnv, "Replace Environment with Tilemap");
            Object.DestroyImmediate(oldEnv);
        }

        var (ground, buildings, detail) = EnsureTilemapLayers();

        // 4. Paint the default city
        PaintCity(ground, buildings, detail, tiles);

        MarkDirty();
        Debug.Log("[Delivery Dash] Tilemap city built!\n" +
                  "To paint manually: Window ▸ 2D ▸ Tile Palette\n" +
                  "→ Create New Palette → drag tile assets from Assets/Tiles/");
    }

    // ── 1. Slice spritesheet ────────────────────────────────────────────────
    static bool SliceSpriteSheet()
    {
        var imp = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
        if (imp == null)
        {
            Debug.LogError($"[TilemapCity] Spritesheet not found: {SheetPath}");
            return false;
        }

        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = PPU;
        imp.filterMode          = FilterMode.Point;
        imp.mipmapEnabled       = false;
        imp.textureCompression  = TextureImporterCompression.Uncompressed;

        var metas = new SpriteMetaData[TOTAL];
        for (int r = 0; r < ROWS; r++)
            for (int c = 0; c < COLS; c++)
            {
                int i = r * COLS + c;
                metas[i] = new SpriteMetaData
                {
                    name      = $"tile_{i:D3}",
                    rect      = new Rect(c * TILE_PX, TEX_H - (r + 1) * TILE_PX, TILE_PX, TILE_PX),
                    alignment = (int)SpriteAlignment.Center,
                    pivot     = new Vector2(0.5f, 0.5f)
                };
            }

        imp.spritesheet = metas;
        imp.SaveAndReimport();
        return true;
    }

    // ── 2. Create tile assets ───────────────────────────────────────────────
    static TileBase[] BuildTileAssets()
    {
        if (!AssetDatabase.IsValidFolder(TileDir))
            AssetDatabase.CreateFolder("Assets", "Tiles");

        var sprites = new Dictionary<string, Sprite>();
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(SheetPath))
            if (a is Sprite s) sprites[s.name] = s;

        if (sprites.Count == 0)
        {
            Debug.LogError("[TilemapCity] No sprites after slicing — check spritesheet import.");
            return null;
        }

        var tiles = new TileBase[TOTAL];
        int created = 0;
        for (int i = 0; i < TOTAL; i++)
        {
            string name = $"tile_{i:D3}";
            string path = $"{TileDir}/{name}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (existing != null)
            {
                if (sprites.TryGetValue(name, out var spr) && existing.sprite != spr)
                {
                    existing.sprite = spr;
                    EditorUtility.SetDirty(existing);
                }
                tiles[i] = existing;
                continue;
            }

            if (!sprites.TryGetValue(name, out var sprite)) continue;

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite      = sprite;
            tile.color        = Color.white;
            tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, path);
            tiles[i] = tile;
            created++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[TilemapCity] {created} new tile assets created, {TOTAL - created} already existed.");
        return tiles;
    }

    // ── 3. Grid + Tilemap layers ────────────────────────────────────────────
    static (Tilemap ground, Tilemap buildings, Tilemap detail) EnsureTilemapLayers()
    {
        var root = FindRoot("--- Environment ---");
        if (root == null) root = new GameObject("--- Environment ---");

        var grid = root.GetComponent<Grid>();
        if (grid == null) grid = root.AddComponent<Grid>();
        grid.cellSize = new Vector3(1, 1, 0);

        var ground    = AddTilemap(root.transform, "Ground",    -20);
        var buildings = AddTilemap(root.transform, "Buildings", -15);
        var detail    = AddTilemap(root.transform, "Detail",    -10);
        return (ground, buildings, detail);
    }

    static Tilemap AddTilemap(Transform parent, string name, int order)
    {
        var t = parent.Find(name);
        if (t == null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            t = go.transform;
        }
        var tm = t.GetComponent<Tilemap>();
        if (tm == null) tm = t.gameObject.AddComponent<Tilemap>();
        var tmr = t.GetComponent<TilemapRenderer>();
        if (tmr == null) tmr = t.gameObject.AddComponent<TilemapRenderer>();
        tmr.sortingOrder = order;
        return tm;
    }

    // ── 4. City painting ────────────────────────────────────────────────────
    static void PaintCity(Tilemap ground, Tilemap buildings, Tilemap detail, TileBase[] t)
    {
        ground.ClearAllTiles();
        buildings.ClearAllTiles();
        detail.ClearAllTiles();

        // --- Ground layer: roads, sidewalks, grass, concrete ---
        for (int x = X_MIN; x <= X_MAX; x++)
            for (int y = Y_MIN; y <= Y_MAX; y++)
                SetSafe(ground, x, y, PickGround(x, y), t);

        // --- Buildings layer: rooftops on some city blocks ---
        PaintBuildings(buildings, t);

        // --- Detail layer: road center-line dashes ---
        PaintRoadMarkings(detail, t);
    }

    static int PickGround(int x, int y)
    {
        bool onVRoad = VRoadX.Contains(x);
        bool onHRoad = HRoadY.Contains(y);

        // Road surface
        if (onVRoad || onHRoad) return ROAD;

        // Sidewalk: immediately adjacent to any road tile
        if (Adj(x, VRoadX) || Adj(y, HRoadY)) return SIDEWALK;

        // Determine which city block this tile belongs to
        int cb = x < -4 ? 0 : x < 1 ? 1 : x < 6 ? 2 : 3;
        int rb = y < -3 ? 0 : y < 1 ? 1 : y < 3 ? 2 : 3;
        bool outerC = cb == 0 || cb == 3;
        bool outerR = rb == 0 || rb == 3;

        // Corner blocks (zone locations) → concrete
        if (outerC && outerR) return CONCRETE;

        // Outer non-corner → grass border
        if (outerC || outerR) return GRASS;

        // Inner blocks: checkerboard parks and plazas
        return (cb + rb) % 2 == 0 ? GRASS : CONCRETE;
    }

    static void PaintBuildings(Tilemap buildings, TileBase[] t)
    {
        // Place 2×2 building rooftops on concrete city blocks
        // Each building is a 2×2 tile cluster
        PlaceBuilding(buildings,  2,  1, ROOF_RED_TL, ROOF_RED_TR, ROOF_RED_BL, ROOF_RED_BR, t);
        PlaceBuilding(buildings, -4, -3, ROOF_RED_TL, ROOF_RED_TR, ROOF_RED_BL, ROOF_RED_BR, t);
        PlaceBuilding(buildings,  3,  1, ROOF_BRN_TL, ROOF_BRN_TR, ROOF_BRN_BL, ROOF_BRN_BR, t);
        PlaceBuilding(buildings, -3, -2, ROOF_BRN_TL, ROOF_BRN_TR, ROOF_BRN_BL, ROOF_BRN_BR, t);

        // Corner blocks: smaller buildings near zone areas
        PlaceBuilding(buildings, -10,  6, ROOF_BRN_TL, ROOF_BRN_TR, ROOF_BRN_BL, ROOF_BRN_BR, t);
        PlaceBuilding(buildings,   9,  6, ROOF_RED_TL, ROOF_RED_TR, ROOF_RED_BL, ROOF_RED_BR, t);
        PlaceBuilding(buildings, -10, -7, ROOF_RED_TL, ROOF_RED_TR, ROOF_RED_BL, ROOF_RED_BR, t);
        PlaceBuilding(buildings,   9, -7, ROOF_BRN_TL, ROOF_BRN_TR, ROOF_BRN_BL, ROOF_BRN_BR, t);
    }

    static void PlaceBuilding(Tilemap tm, int x, int y,
        int tl, int tr, int bl, int br, TileBase[] t)
    {
        SetSafe(tm, x,     y + 1, tl, t);
        SetSafe(tm, x + 1, y + 1, tr, t);
        SetSafe(tm, x,     y,     bl, t);
        SetSafe(tm, x + 1, y,     br, t);
    }

    static void PaintRoadMarkings(Tilemap detail, TileBase[] t)
    {
        // Horizontal road center dashes (on lower tile of each H-road pair)
        int[] hCenters = { -5, -1, 3 };
        foreach (int ry in hCenters)
            for (int x = X_MIN; x <= X_MAX; x += 2)
                if (!VRoadX.Contains(x))
                    SetSafe(detail, x, ry, ROAD_ALT, t);

        // Vertical road center dashes (on left tile of each V-road pair)
        int[] vCenters = { -7, -1, 6 };
        foreach (int rx in vCenters)
            for (int y = Y_MIN; y <= Y_MAX; y += 2)
                if (!HRoadY.Contains(y))
                    SetSafe(detail, rx, y, ROAD_ALT, t);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    static bool Adj(int v, HashSet<int> set) => set.Contains(v - 1) || set.Contains(v + 1);

    static void SetSafe(Tilemap tm, int x, int y, int idx, TileBase[] t)
    {
        if (idx >= 0 && idx < t.Length && t[idx] != null)
            tm.SetTile(new Vector3Int(x, y, 0), t[idx]);
    }

    static GameObject FindRoot(string name)
    {
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }

    static void MarkDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
