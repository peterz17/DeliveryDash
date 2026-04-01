using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.IO;

/// <summary>
/// Run via: Delivery Dash ▶ Setup Scene
/// Builds the entire Delivery Dash scene from scratch.
/// </summary>
public static class DeliveryGameSetup
{
    // ── layout constants ────────────────────────────────────────────────────
    const float MapW = 22f, MapH = 15f;          // ground size (units)
    const float ZoneSize = 2.8f;                   // delivery zone square (40% larger)
    const float PickupSize = 2.8f;                 // pickup zone square (match delivery zones)
    const float PlayerSize = 2.7f;

    static readonly Vector2[] DeliveryPositions =
    {
        new Vector2(-8f,  5.5f),   // House A
        new Vector2( 8f,  5.5f),   // House B
        new Vector2(-8f, -5.5f),   // Shop
        new Vector2( 8f, -5.5f),   // Cafe
    };
    static readonly string[] DeliveryNames     = { "House A", "House B", "Shop", "Cafe" };
    static readonly string[] DeliveryLabelKeys = { "dest_house_a", "dest_house_b", "dest_shop", "dest_cafe" };
    static readonly Color[] ZoneColors =
    {
        new Color(1f, 0.55f, 0.1f),  // orange  – House A
        new Color(1f, 0.4f,  0.7f),  // pink    – House B
        new Color(0.6f, 0.2f, 1f),   // purple  – Shop
        new Color(0f,  0.8f, 0.8f),  // teal    – Cafe
    };

    // ── find a root GameObject by exact name ─────────────────────────────────
    static GameObject FindRoot(string name)
    {
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }

    // ── entry point (non-destructive — only creates what is missing) ──────────
    // Manual changes you made in the Editor are preserved.
    // Run this menu item to create or update scene objects non-destructively.
    [MenuItem("Delivery Dash/Setup Scene")]
    public static void SetupScene()
    {
        ImportTMPEssentials();
        EnsureThaiFont();

        // ── Fix PPU for real sprites so world-size matches old pixel-art ───────
        // Icons: 512px / 512 PPU = 1.0 unit (GO scale 0.7 → 0.7 units, same as before)
        ConfigureSpriteImport("Assets/Sprites/Icons/icon_shield.png",  512f, FilterMode.Bilinear);
        ConfigureSpriteImport("Assets/Sprites/Icons/icon_rocket.png",  512f, FilterMode.Bilinear);
        ConfigureSpriteImport("Assets/Sprites/Icons/icon_clock.png",   512f, FilterMode.Bilinear);
        ConfigureSpriteImport("Assets/Sprites/Icons/icon_setting.png", 512f, FilterMode.Bilinear);
        // Cars: 256px / 800 PPU = 0.32 units (matches old 32px/100PPU car)
        ConfigureSpriteImport("Assets/Sprites/Cars/car_player_1.png", 800f, FilterMode.Bilinear);
        ConfigureSpriteImport("Assets/Sprites/Cars/car_normal_1.png", 800f, FilterMode.Bilinear);
        ConfigureSpriteImport("Assets/Sprites/Cars/car_normal_2.png", 800f, FilterMode.Bilinear);
        ConfigureSpriteImport("Assets/Sprites/Cars/car_normal_3.png", 800f, FilterMode.Bilinear);
        ConfigureSpriteImport("Assets/Sprites/Cars/car_normal_4.png", 800f, FilterMode.Bilinear);
        ConfigureSpriteImport("Assets/Sprites/Cars/car_boss_1.png",   800f, FilterMode.Bilinear);
        ConfigureSpriteImport("Assets/Sprites/Cars/car_boss_2.png",   800f, FilterMode.Bilinear);
        ConfigureSpriteImport("Assets/Sprites/Cars/car_boss_3.png",   800f, FilterMode.Bilinear);

        // ImportBakerySprites(); // disabled — use Unity auto-slice for UI_game.png
        Sprite whiteSpr    = GetOrCreateWhiteSprite();
        Sprite carSpr      = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cars/car_player_1.png");
        if (carSpr == null) carSpr = BuildCarSprite();
        Sprite npcCarSpr   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cars/car_normal_1.png");
        if (npcCarSpr == null) npcCarSpr = BuildNPCCarSprite();
        Sprite bossCarSpr1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cars/car_boss_1.png");
        Sprite bossCarSpr2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cars/car_boss_2.png");
        Sprite bossCarSpr3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cars/car_boss_3.png");
        if (bossCarSpr1 == null) bossCarSpr1 = BuildBossCarSprite();
        if (bossCarSpr2 == null) bossCarSpr2 = bossCarSpr1;
        if (bossCarSpr3 == null) bossCarSpr3 = bossCarSpr1;
        // PPU=512: 512px sprite → 1.0 unit natural → × localScale(2,2) = 2×2 world units (matches old placeholder)
        ConfigureSpriteImport("Assets/Sprites/Building/building_house.png", 512f, FilterMode.Bilinear);
        ConfigureSpriteImport("Assets/Sprites/Building/building_cafe.png",  512f, FilterMode.Bilinear);
        ConfigureSpriteImport("Assets/Sprites/Building/building_shop.png",  512f, FilterMode.Bilinear);
        Sprite buildingHouseSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Building/building_house.png");
        Sprite buildingShopSpr  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Building/building_shop.png");
        Sprite buildingCafeSpr  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Building/building_cafe.png");
        Sprite buildingFallback = BuildBuildingSprite();
        // Index order: 0=House A, 1=House B, 2=Shop, 3=Cafe
        Sprite[] zoneBuildingSprites =
        {
            buildingHouseSpr != null ? buildingHouseSpr : buildingFallback,
            buildingHouseSpr != null ? buildingHouseSpr : buildingFallback,
            buildingShopSpr  != null ? buildingShopSpr  : buildingFallback,
            buildingCafeSpr  != null ? buildingCafeSpr  : buildingFallback,
        };
        Sprite buildingSpr = buildingFallback;
        ConfigureSpriteImport("Assets/Sprites/Building/building_office.png", 1536f, FilterMode.Bilinear);
        ConfigureSpriteImport("Assets/Sprites/Building/building_normal.png", 1365f, FilterMode.Bilinear);
        Sprite packageSpr  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Building/building_office.png");
        if (packageSpr == null) packageSpr = BuildPackageSprite();
        Sprite obstacleSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Building/building_normal.png");
        if (obstacleSpr == null) obstacleSpr = BuildObstacleSprite();

        // ── Environment ──────────────────────────────────────────────────────
        // Environment is managed manually by the user (tilemap or custom objects).
        // Setup Scene only creates an empty root if missing.
        if (FindRoot("--- Environment ---") == null)
            new GameObject("--- Environment ---");

        // ── Obstacles ────────────────────────────────────────────────────────
        if (FindRoot("--- Obstacles ---") == null)
        {
            var obstGroup = new GameObject("--- Obstacles ---");
            Transform obst = obstGroup.transform;
            CreateObstacle("Block NW",  new Vector3(-3f,    3.75f, 0), new Vector2(2f, 2f), obstacleSpr, obst);
            CreateObstacle("Block NE",  new Vector3( 3f,    3.75f, 0), new Vector2(2f, 2f), obstacleSpr, obst);
            CreateObstacle("Block SW",  new Vector3(-3f,   -3.25f, 0), new Vector2(2f, 2f), obstacleSpr, obst);
            CreateObstacle("Block SE",  new Vector3( 3f,   -3.2f,  0), new Vector2(2f, 2f), obstacleSpr, obst);

            const float WT = 1f;
            CreateWall("WallTop",   new Vector3(0,           MapH * 0.5f + WT * 0.5f, 0), new Vector2(MapW + WT * 2, WT), obst);
            CreateWall("WallBot",   new Vector3(0,          -MapH * 0.5f - WT * 0.5f, 0), new Vector2(MapW + WT * 2, WT), obst);
            CreateWall("WallLeft",  new Vector3(-MapW * 0.5f - WT * 0.5f, 0,          0), new Vector2(WT, MapH + WT * 2), obst);
            CreateWall("WallRight", new Vector3( MapW * 0.5f + WT * 0.5f, 0,          0), new Vector2(WT, MapH + WT * 2), obst);
        }

        // ── Decorations ──────────────────────────────────────────────────────
        // Decorations managed manually by user.
        if (FindRoot("--- Decorations ---") == null)
            new GameObject("--- Decorations ---");

        // ── Zones ────────────────────────────────────────────────────────────
        if (FindRoot("--- Zones ---") == null)
        {
            var zoneGroup = new GameObject("--- Zones ---");
            Transform zones = zoneGroup.transform;

            var pickup = CreateZoneObject("Pickup Zone", Vector3.zero,
                new Vector2(PickupSize, PickupSize), Color.white, packageSpr, parent: zones);
            AddWorldLabel(pickup, "PICKUP", Color.white, "dest_pickup");
            pickup.AddComponent<PickupZone>();

            for (int i = 0; i < 4; i++)
            {
                var zone = CreateZoneObject(DeliveryNames[i],
                    (Vector3)(Vector2)DeliveryPositions[i],
                    new Vector2(ZoneSize, ZoneSize), Color.white, zoneBuildingSprites[i], parent: zones);

                AddWorldLabel(zone, DeliveryNames[i], Color.white, DeliveryLabelKeys[i]);
                var dz = zone.AddComponent<DeliveryZone>();
                dz.destinationName = DeliveryNames[i];
                zone.AddComponent<ZoneHighlight>();
            }
        }
        else
        {
            // Update sprites and scale on existing zones
            var existingZones = FindRoot("--- Zones ---");
            for (int i = 0; i < DeliveryNames.Length; i++)
            {
                var zoneT = existingZones.transform.Find(DeliveryNames[i]);
                if (zoneT == null) continue;
                var sr = zoneT.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = zoneBuildingSprites[i];
                    sr.color  = Color.white;
                }
                // Keep world size at ZoneSize×ZoneSize (2×2) to match old placeholder
                zoneT.localScale = new Vector3(ZoneSize, ZoneSize, 1f);
                // Remove Border child if it exists
                var border = zoneT.Find("Border");
                if (border != null) Object.DestroyImmediate(border.gameObject);
                // Ensure ZoneLabelLocalizer is on the Label child
                var labelT = zoneT.Find("Label");
                if (labelT != null)
                {
                    var localizer = labelT.GetComponent<ZoneLabelLocalizer>();
                    if (localizer == null) localizer = labelT.gameObject.AddComponent<ZoneLabelLocalizer>();
                    localizer.localizationKey = DeliveryLabelKeys[i];
                }
            }
            // Pickup zone label
            var pickupT = existingZones.transform.Find("Pickup Zone");
            if (pickupT != null)
            {
                var labelT = pickupT.Find("Label");
                if (labelT != null)
                {
                    var localizer = labelT.GetComponent<ZoneLabelLocalizer>();
                    if (localizer == null) localizer = labelT.gameObject.AddComponent<ZoneLabelLocalizer>();
                    localizer.localizationKey = "dest_pickup";
                }
            }
        }

        // ── Player ───────────────────────────────────────────────────────────
        PlayerController playerCtrl;
        var existingPlayer = FindRoot("Player");
        if (existingPlayer == null)
        {
            var playerGO = new GameObject("Player");
            playerGO.tag = "Player";
            playerGO.transform.position = new Vector3(0f, -1.5f, 0f);

            var playerSprGO = new GameObject("Sprite");
            playerSprGO.transform.SetParent(playerGO.transform, false);
            playerSprGO.transform.localScale = new Vector3(PlayerSize, PlayerSize, 1f);
            var playerSr = playerSprGO.AddComponent<SpriteRenderer>();
            playerSr.sprite = carSpr;
            playerSr.color = Color.white;
            playerSr.sortingOrder = 10;

            var rb = playerGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var playerCol = playerGO.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.4f, 0.8f);

            playerCtrl = playerGO.AddComponent<PlayerController>();
        }
        else
        {
            existingPlayer.tag = "Player";
            playerCtrl = existingPlayer.GetComponent<PlayerController>();
            var spriteGO = existingPlayer.transform.Find("Sprite");
            if (spriteGO != null)
            {
                spriteGO.transform.localScale = new Vector3(PlayerSize, PlayerSize, 1f);
                var sr = spriteGO.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    if (carSpr != null) sr.sprite = carSpr;
                    sr.color = Color.white;
                    sr.sortingOrder = 10;
                }
            }
            var playerRb = existingPlayer.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.bodyType = RigidbodyType2D.Dynamic;
                playerRb.gravityScale = 0f;
                playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
                playerRb.interpolation = RigidbodyInterpolation2D.Interpolate;
                playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
            var playerCol2 = existingPlayer.GetComponent<BoxCollider2D>();
            if (playerCol2 != null) playerCol2.size = new Vector2(0.4f, 0.8f);
        }

        // ── Camera ───────────────────────────────────────────────────────────
        if (FindRoot("Main Camera") == null)
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.10f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGO.AddComponent<AudioListener>();

            var follow = camGO.AddComponent<CameraFollow>();
            if (playerCtrl != null) follow.target = playerCtrl.transform;
            follow.smoothSpeed = 6f;
            follow.bounds = new Vector2(MapW * 0.5f - 4f, MapH * 0.5f - 3f);
        }

        // ── UI ───────────────────────────────────────────────────────────────
        UIManager uiManager;
        VirtualJoystick joystick;
        if (FindRoot("Canvas") == null)
        {
            BuildUI(out uiManager, out joystick);
        }
        else
        {
            uiManager = Object.FindAnyObjectByType<UIManager>();
            joystick  = Object.FindAnyObjectByType<VirtualJoystick>();

            // Re-wire any UIManager fields that were added after the Canvas was first built
            if (uiManager != null)
            {
                var canvasRoot = FindRoot("Canvas");
                Transform canvasT = canvasRoot != null ? canvasRoot.transform : null;
                T FindTMP<T>(string path) where T : Component
                {
                    if (canvasT == null) return null;
                    var t = canvasT.Find(path);
                    return t != null ? t.GetComponent<T>() : null;
                }

                if (uiManager.startSubtitleText      == null) uiManager.startSubtitleText      = FindTMP<TextMeshProUGUI>("StartScreen/SubtitleText");
                if (uiManager.levelCompleteTitleText == null) uiManager.levelCompleteTitleText = FindTMP<TextMeshProUGUI>("LevelCompleteScreen/LCTitle");
                if (uiManager.victoryTitleText       == null) uiManager.victoryTitleText       = FindTMP<TextMeshProUGUI>("VictoryScreen/VicTitle");
                if (uiManager.victorySubtitleText    == null) uiManager.victorySubtitleText    = FindTMP<TextMeshProUGUI>("VictoryScreen/VicSubtitle");
                if (uiManager.endlessTitleText       == null) uiManager.endlessTitleText       = FindTMP<TextMeshProUGUI>("EndlessSummaryScreen/EndlessTitle");
                if (uiManager.pauseHintText          == null) uiManager.pauseHintText          = FindTMP<TextMeshProUGUI>("PauseScreen/PauseHint");
                if (uiManager.pauseTitleText         == null) uiManager.pauseTitleText         = FindTMP<TextMeshProUGUI>("PauseScreen/PauseTitle");
                if (uiManager.modeSelectTitleText    == null) uiManager.modeSelectTitleText    = FindTMP<TextMeshProUGUI>("ModeSelectScreen/ModeContent/ModeTitle");
                if (uiManager.rushModeDescText       == null) uiManager.rushModeDescText       = FindTMP<TextMeshProUGUI>("ModeSelectScreen/ModeContent/RushDesc");
                if (uiManager.normalModeDescText     == null) uiManager.normalModeDescText     = FindTMP<TextMeshProUGUI>("ModeSelectScreen/ModeContent/NormalDesc");
                if (uiManager.endlessModeDescText    == null) uiManager.endlessModeDescText    = FindTMP<TextMeshProUGUI>("ModeSelectScreen/ModeContent/EndlessDesc");
                if (uiManager.standardHeaderText     == null) uiManager.standardHeaderText     = FindTMP<TextMeshProUGUI>("ModeSelectScreen/ModeContent/StandardHeader");
                if (uiManager.extremeHeaderText      == null) uiManager.extremeHeaderText      = FindTMP<TextMeshProUGUI>("ModeSelectScreen/ModeContent/ExtremeHeader");
                if (uiManager.heartExtremeDescText   == null) uiManager.heartExtremeDescText   = FindTMP<TextMeshProUGUI>("ModeSelectScreen/ModeContent/HeartExtremeDesc");
                if (uiManager.rushExtremeDescText    == null) uiManager.rushExtremeDescText    = FindTMP<TextMeshProUGUI>("ModeSelectScreen/ModeContent/RushExtremeDesc");

                // Re-wire ModeSelectScreen reference and mode buttons
                if (uiManager.modeSelectScreen == null)
                {
                    if (canvasT != null)
                    {
                        var modeSelGO = canvasT.Find("ModeSelectScreen");
                        if (modeSelGO != null) uiManager.modeSelectScreen = modeSelGO.gameObject;
                    }
                }
                if (uiManager.normalModeButton       == null) uiManager.normalModeButton       = FindTMP<Button>("ModeSelectScreen/ModeContent/NormalModeButton");
                if (uiManager.rushModeButton          == null) uiManager.rushModeButton          = FindTMP<Button>("ModeSelectScreen/ModeContent/RushModeButton");
                if (uiManager.endlessModeButton       == null) uiManager.endlessModeButton       = FindTMP<Button>("ModeSelectScreen/ModeContent/EndlessModeButton");
                if (uiManager.heartExtremeModeButton  == null) uiManager.heartExtremeModeButton  = FindTMP<Button>("ModeSelectScreen/ModeContent/HeartExtremeModeButton");
                if (uiManager.rushExtremeModeButton   == null) uiManager.rushExtremeModeButton   = FindTMP<Button>("ModeSelectScreen/ModeContent/RushExtremeModeButton");
                if (uiManager.settingsTitleText      == null) uiManager.settingsTitleText      = FindTMP<TextMeshProUGUI>("SettingsScreen/SettingsPanel/SettingsTitle");
                if (uiManager.volumeLabelText        == null) uiManager.volumeLabelText        = FindTMP<TextMeshProUGUI>("SettingsScreen/SettingsPanel/VolumeLabel");
                if (uiManager.startTitleText         == null) uiManager.startTitleText         = FindTMP<TextMeshProUGUI>("StartScreen/TitleText");
            }

            // Always update HUD element positions (re-applies anchors on every Setup run)
            if (uiManager != null)
            {
                var canvasGO2 = FindRoot("Canvas");
                Transform canvasT2 = canvasGO2 != null ? canvasGO2.transform : null;
                var hudT2 = canvasT2 != null ? canvasT2.Find("GameplayUI") : null;
                var hudGO2 = hudT2 != null ? hudT2.gameObject : null;
                // Re-wire all screen and button references if missing
                if (canvasT2 != null)
                {
                    void RewireGO(ref GameObject field, string path)
                    {
                        if (field != null) return;
                        var t = canvasT2.Find(path);
                        if (t != null) field = t.gameObject;
                    }
                    T RewireComp<T>(string path) where T : Component
                    {
                        var t = canvasT2.Find(path);
                        return t != null ? t.GetComponent<T>() : null;
                    }

                    // Screens
                    RewireGO(ref uiManager.gameplayUI,           "GameplayUI");
                    RewireGO(ref uiManager.startScreen,          "StartScreen");
                    RewireGO(ref uiManager.modeSelectScreen,     "ModeSelectScreen");
                    RewireGO(ref uiManager.gameOverScreen,       "GameOverScreen");
                    RewireGO(ref uiManager.levelCompleteScreen,  "LevelCompleteScreen");
                    RewireGO(ref uiManager.victoryScreen,        "VictoryScreen");
                    RewireGO(ref uiManager.endlessSummaryScreen, "EndlessSummaryScreen");
                    RewireGO(ref uiManager.levelSelectScreen,    "LevelSelectScreen");
                    RewireGO(ref uiManager.pauseScreen,          "PauseScreen");
                    RewireGO(ref uiManager.settingsScreen,       "SettingsScreen");

                    // Buttons
                    if (uiManager.startButton == null)            uiManager.startButton            = RewireComp<Button>("StartScreen/StartButton");
                    if (uiManager.retryButton == null)            uiManager.retryButton            = RewireComp<Button>("GameOverScreen/RetryButton");
                    if (uiManager.nextLevelButton == null)        uiManager.nextLevelButton        = RewireComp<Button>("LevelCompleteScreen/NextLevelButton");
                    if (uiManager.playAgainButton == null)        uiManager.playAgainButton        = RewireComp<Button>("VictoryScreen/PlayAgainButton");
                    if (uiManager.resumeButton == null)           uiManager.resumeButton           = RewireComp<Button>("PauseScreen/ResumeButton");
                    if (uiManager.restartButton == null)          uiManager.restartButton          = RewireComp<Button>("PauseScreen/RestartButton");
                    if (uiManager.selectModeFromPauseButton == null) uiManager.selectModeFromPauseButton = RewireComp<Button>("PauseScreen/SelectModeButton");
                    if (uiManager.settingsButton == null)         uiManager.settingsButton         = RewireComp<Button>("PauseScreen/SettingsButton");
                    if (uiManager.startSettingsButton == null)    uiManager.startSettingsButton    = RewireComp<Button>("StartScreen/SettingsButton");
                    if (uiManager.closeSettingsButton == null)    uiManager.closeSettingsButton    = RewireComp<Button>("SettingsScreen/SettingsPanel/CloseSettingsButton");
                    if (uiManager.languageButton == null)         uiManager.languageButton         = RewireComp<Button>("SettingsScreen/SettingsPanel/LanguageButton");
                    if (uiManager.endlessRetryButton == null)     uiManager.endlessRetryButton     = RewireComp<Button>("EndlessSummaryScreen/EndlessRetryButton");
                    if (uiManager.endlessSelectModeButton == null) uiManager.endlessSelectModeButton = RewireComp<Button>("EndlessSummaryScreen/EndlessSelectModeButton");
                    if (uiManager.modeBackToStartButton == null)
                    {
                        uiManager.modeBackToStartButton = RewireComp<Button>("ModeSelectScreen/BackToStartButton");
                        if (uiManager.modeBackToStartButton == null)
                        {
                            var modeSelT2 = canvasT2.Find("ModeSelectScreen");
                            if (modeSelT2 != null)
                            {
                                var backBtn2 = MakeButton(modeSelT2.gameObject, "BackToStartButton", "BACK",
                                    new Vector2(0.30f, 0.02f), new Vector2(0.70f, 0.08f), Vector2.zero, Vector2.zero);
                                ApplyButtonColor(backBtn2, new Color(0.22f, 0.25f, 0.35f), new Color(0.32f, 0.38f, 0.52f), new Color(0.14f, 0.16f, 0.24f));
                                uiManager.modeBackToStartButton = backBtn2;
                            }
                        }
                    }
                    if (uiManager.rushModeButton == null)         uiManager.rushModeButton         = RewireComp<Button>("ModeSelectScreen/ModeContent/RushModeButton");
                    if (uiManager.normalModeButton == null)       uiManager.normalModeButton       = RewireComp<Button>("ModeSelectScreen/ModeContent/NormalModeButton");
                    if (uiManager.endlessModeButton == null)      uiManager.endlessModeButton      = RewireComp<Button>("ModeSelectScreen/ModeContent/EndlessModeButton");
                    if (uiManager.heartExtremeModeButton == null) uiManager.heartExtremeModeButton = RewireComp<Button>("ModeSelectScreen/ModeContent/HeartExtremeModeButton");
                    if (uiManager.rushExtremeModeButton == null)  uiManager.rushExtremeModeButton  = RewireComp<Button>("ModeSelectScreen/ModeContent/RushExtremeModeButton");

                    // Text refs
                    if (uiManager.failTitleText == null)          uiManager.failTitleText          = RewireComp<TextMeshProUGUI>("GameOverScreen/FailTitle");
                    if (uiManager.finalScoreText == null)         uiManager.finalScoreText         = RewireComp<TextMeshProUGUI>("GameOverScreen/FinalScoreText");
                    if (uiManager.neededScoreText == null)        uiManager.neededScoreText        = RewireComp<TextMeshProUGUI>("GameOverScreen/NeededScoreText");
                    if (uiManager.levelCompleteScoreText == null) uiManager.levelCompleteScoreText = RewireComp<TextMeshProUGUI>("LevelCompleteScreen/LCScoreText");
                    if (uiManager.levelCompleteNextText == null)  uiManager.levelCompleteNextText  = RewireComp<TextMeshProUGUI>("LevelCompleteScreen/LCNextText");
                    if (uiManager.victoryScoreText == null)       uiManager.victoryScoreText       = RewireComp<TextMeshProUGUI>("VictoryScreen/VicScoreText");
                    if (uiManager.endlessTierText == null)        uiManager.endlessTierText        = RewireComp<TextMeshProUGUI>("EndlessSummaryScreen/EndlessTierText");
                    if (uiManager.endlessScoreText == null)       uiManager.endlessScoreText       = RewireComp<TextMeshProUGUI>("EndlessSummaryScreen/EndlessScoreText");
                    if (uiManager.endlessDeliveriesText == null)  uiManager.endlessDeliveriesText  = RewireComp<TextMeshProUGUI>("EndlessSummaryScreen/EndlessDelivText");
                    if (uiManager.languageButtonText == null)     uiManager.languageButtonText     = RewireComp<TextMeshProUGUI>("SettingsScreen/SettingsPanel/LanguageButton/Text");

                    EditorUtility.SetDirty(uiManager);
                }

                if (hudGO2 != null)
                {
                    // Re-wire critical HUD references
                    if (uiManager.feedbackText == null)
                    {
                        var fbT = hudGO2.transform.Find("FeedbackText");
                        if (fbT != null) uiManager.feedbackText = fbT.GetComponent<TextMeshProUGUI>();
                    }
                    if (uiManager.timerText == null)
                    {
                        var tt = hudGO2.transform.Find("TimerText");
                        if (tt != null) uiManager.timerText = tt.GetComponent<TextMeshProUGUI>();
                    }
                    if (uiManager.scoreText == null)
                    {
                        var st = hudGO2.transform.Find("ScoreText");
                        if (st != null) uiManager.scoreText = st.GetComponent<TextMeshProUGUI>();
                    }
                    if (uiManager.streakText == null)
                    {
                        var skt = hudGO2.transform.Find("StreakText");
                        if (skt != null) uiManager.streakText = skt.GetComponent<TextMeshProUGUI>();
                    }
                    if (uiManager.orderText == null)
                    {
                        var ot = hudGO2.transform.Find("OrderText");
                        if (ot != null) uiManager.orderText = ot.GetComponent<TextMeshProUGUI>();
                    }
                    if (uiManager.levelText == null)
                    {
                        var lt = hudGO2.transform.Find("LevelText");
                        if (lt != null) uiManager.levelText = lt.GetComponent<TextMeshProUGUI>();
                    }

                    // Re-apply TimerText anchors
                    var timerT2 = hudGO2.transform.Find("TimerText");
                    if (timerT2 != null)
                    {
                        var rt = timerT2.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0.01f, 0.915f); rt.anchorMax = new Vector2(0.19f, 1.0f);
                        rt.offsetMin = new Vector2(-10, -8); rt.offsetMax = Vector2.zero;
                        var tmp = timerT2.GetComponent<TextMeshProUGUI>();
                        if (tmp != null) tmp.fontSize = 46;
                    }
                    // Re-apply LevelText anchors + auto-size
                    var levelT2 = hudGO2.transform.Find("LevelText");
                    if (levelT2 != null)
                    {
                        var rt = levelT2.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0.01f, 0.875f); rt.anchorMax = new Vector2(0.22f, 0.915f);
                        rt.offsetMin = new Vector2(-10, -2); rt.offsetMax = Vector2.zero;
                        var tmp = levelT2.GetComponent<TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            tmp.enableAutoSizing = true;
                            tmp.fontSizeMin = 14;
                            tmp.fontSizeMax = 28;
                        }
                    }
                    // Re-apply or create LivesText
                    var livesT2 = hudGO2.transform.Find("LivesText");
                    if (livesT2 != null)
                    {
                        var rt = livesT2.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0.19f, 0.915f); rt.anchorMax = new Vector2(0.42f, 1.0f);
                        rt.offsetMin = new Vector2(-4, -8); rt.offsetMax = Vector2.zero;
                        var tmp = livesT2.GetComponent<TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            tmp.fontStyle = FontStyles.Bold;
                            tmp.richText = true;
                        }
                        if (uiManager.livesText == null)
                            uiManager.livesText = livesT2.GetComponent<TextMeshProUGUI>();
                    }
                    else
                    {
                        var livesTxt2 = MakeTMP(hudGO2, "LivesText", "", 36, TextAlignmentOptions.MidlineLeft,
                            new Vector2(0.19f, 0.915f), new Vector2(0.42f, 1.0f), new Vector2(-4, -8), Vector2.zero);
                        livesTxt2.fontStyle = FontStyles.Bold;
                        livesTxt2.color = Color.white;
                        livesTxt2.richText = true;
                        livesTxt2.gameObject.SetActive(false);
                        uiManager.livesText = livesTxt2;
                    }
                    // Re-apply ScoreText anchors (full right area — button is below topbar)
                    var scoreT2 = hudGO2.transform.Find("ScoreText");
                    if (scoreT2 != null)
                    {
                        var rt = scoreT2.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0.76f, 0.905f); rt.anchorMax = new Vector2(0.99f, 1.0f);
                        rt.offsetMin = new Vector2(-10, -8); rt.offsetMax = Vector2.zero;
                    }
                    // Create or update HudSettingsButton
                    var existingBtnT = hudGO2.transform.Find("HudSettingsButton");
                    if (existingBtnT != null)
                    {
                        // Update position of existing button
                        var rt = existingBtnT.GetComponent<RectTransform>();
                        rt.anchorMin        = new Vector2(1.0f, 0.875f);
                        rt.anchorMax        = new Vector2(1.0f, 0.875f);
                        rt.pivot            = new Vector2(1.0f, 1.0f);
                        rt.sizeDelta        = new Vector2(110f, 110f);
                        rt.anchoredPosition = new Vector2(0f, 0f);
                        if (uiManager.hudSettingsButton == null)
                            uiManager.hudSettingsButton = existingBtnT.GetComponent<Button>();
                    }
                    else
                    {
                        // Create button from scratch
                        Sprite settingIconSpr2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Icons/icon_setting.png");
                        var hudSettingsBtnGO2 = new GameObject("HudSettingsButton", typeof(RectTransform));
                        hudSettingsBtnGO2.transform.SetParent(hudGO2.transform, false);
                        var rt2 = hudSettingsBtnGO2.GetComponent<RectTransform>();
                        rt2.anchorMin        = new Vector2(1.0f, 0.875f);
                        rt2.anchorMax        = new Vector2(1.0f, 0.875f);
                        rt2.pivot            = new Vector2(1.0f, 1.0f);
                        rt2.sizeDelta        = new Vector2(110f, 110f);
                        rt2.anchoredPosition = new Vector2(0f, 0f);
                        var bg2 = hudSettingsBtnGO2.AddComponent<Image>();
                        bg2.color = new Color(0.04f, 0.05f, 0.12f, 0.75f);
                        var btn2 = hudSettingsBtnGO2.AddComponent<Button>();
                        btn2.targetGraphic = bg2;
                        var cols2 = btn2.colors;
                        cols2.normalColor      = new Color(0.04f, 0.05f, 0.12f, 0.75f);
                        cols2.highlightedColor = new Color(0.15f, 0.20f, 0.38f, 0.90f);
                        cols2.pressedColor     = new Color(0.02f, 0.03f, 0.08f, 0.90f);
                        btn2.colors = cols2;
                        var iconChild2 = new GameObject("Icon", typeof(RectTransform));
                        iconChild2.transform.SetParent(hudSettingsBtnGO2.transform, false);
                        var irt2 = iconChild2.GetComponent<RectTransform>();
                        irt2.anchorMin = Vector2.zero;
                        irt2.anchorMax = Vector2.one;
                        irt2.offsetMin = new Vector2(12f, 12f);
                        irt2.offsetMax = new Vector2(-12f, -12f);
                        var iImg2 = iconChild2.AddComponent<Image>();
                        if (settingIconSpr2 != null) iImg2.sprite = settingIconSpr2;
                        iImg2.color = new Color(0.75f, 0.85f, 1f, 0.95f);
                        uiManager.hudSettingsButton = btn2;
                    }
                }
            }
        }

        // Always re-apply EndlessSummary proportional anchors
        if (uiManager != null)
        {
            var canvasES = FindRoot("Canvas");
            var endlessSumT = canvasES != null ? canvasES.transform.Find("EndlessSummaryScreen") : null;
            if (endlessSumT != null)
            {
                void ApplyESAnchors(string childName, Vector2 aMin, Vector2 aMax)
                {
                    var t = endlessSumT.Find(childName);
                    if (t == null) return;
                    var rt = t.GetComponent<RectTransform>();
                    rt.anchorMin = aMin; rt.anchorMax = aMax;
                    rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                }
                ApplyESAnchors("EndlessTitle",            new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.92f));
                ApplyESAnchors("EndlessTierText",         new Vector2(0.10f, 0.64f), new Vector2(0.90f, 0.75f));
                ApplyESAnchors("EndlessScoreText",        new Vector2(0.10f, 0.52f), new Vector2(0.90f, 0.63f));
                ApplyESAnchors("EndlessDelivText",        new Vector2(0.10f, 0.42f), new Vector2(0.90f, 0.52f));
                ApplyESAnchors("EndlessRetryButton",      new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.18f));
                ApplyESAnchors("EndlessSelectModeButton", new Vector2(0.52f, 0.08f), new Vector2(0.92f, 0.18f));

                if (uiManager.endlessSummaryScreen == null)
                    uiManager.endlessSummaryScreen = endlessSumT.gameObject;
            }
        }

        // Always re-apply ModeSelect layout (else-branch: Canvas exists)
        if (uiManager != null)
        {
            var canvasGO3 = FindRoot("Canvas");
            var modeSelT = canvasGO3 != null ? canvasGO3.transform.Find("ModeSelectScreen") : null;
            var modeContentT = modeSelT != null ? modeSelT.Find("ModeContent") : null;
            if (modeSelT != null && modeContentT != null)
            {
                // Helper to update RectTransform anchors for a child inside ModeContent
                void ApplyAnchors(string childName, Vector2 aMin, Vector2 aMax)
                {
                    var t = modeContentT.Find(childName);
                    if (t == null) return;
                    var rt = t.GetComponent<RectTransform>();
                    rt.anchorMin = aMin; rt.anchorMax = aMax;
                    rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                }

                void ApplyBtnLayout(string childName, float anchorY)
                {
                    var t = modeContentT.Find(childName);
                    if (t == null) return;
                    var rt = t.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, anchorY);
                    rt.anchorMax = new Vector2(0.5f, anchorY);
                    rt.sizeDelta = new Vector2(340, 62);
                    rt.anchoredPosition = Vector2.zero;
                }

                void ApplyDescLayout(string childName, float anchorY)
                {
                    var t = modeContentT.Find(childName);
                    if (t == null) return;
                    var rt = t.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.10f, anchorY);
                    rt.anchorMax = new Vector2(0.90f, anchorY);
                    rt.sizeDelta = new Vector2(0, 18);
                    rt.anchoredPosition = new Vector2(0, -42);
                    var tmp = t.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.fontSize = 18;
                }

                // Title
                ApplyAnchors("ModeTitle", new Vector2(0.08f, 0.90f), new Vector2(0.92f, 0.98f));

                // Section headers
                ApplyAnchors("StandardHeader", new Vector2(0.30f, 0.835f), new Vector2(0.70f, 0.865f));
                ApplyAnchors("ExtremeHeader", new Vector2(0.30f, 0.535f), new Vector2(0.70f, 0.565f));

                // Separators
                ApplyAnchors("SepTop", new Vector2(0.20f, 0.860f), new Vector2(0.80f, 0.862f));
                ApplyAnchors("SepMid", new Vector2(0.20f, 0.565f), new Vector2(0.80f, 0.567f));
                ApplyAnchors("SepBot", new Vector2(0.20f, 0.265f), new Vector2(0.80f, 0.267f));

                // Buttons
                ApplyBtnLayout("NormalModeButton", 0.77f);
                ApplyBtnLayout("RushModeButton", 0.65f);
                ApplyBtnLayout("HeartExtremeModeButton", 0.47f);
                ApplyBtnLayout("RushExtremeModeButton", 0.35f);
                ApplyBtnLayout("EndlessModeButton", 0.19f);

                // Descriptions
                ApplyDescLayout("NormalDesc", 0.77f);
                ApplyDescLayout("RushDesc", 0.65f);
                ApplyDescLayout("HeartExtremeDesc", 0.47f);
                ApplyDescLayout("RushExtremeDesc", 0.35f);
                ApplyDescLayout("EndlessDesc", 0.19f);
            }
            else if (canvasGO3 != null && (modeSelT == null || modeContentT == null))
            {
                // UI structure broken — destroy Canvas+UIManager+AudioManager, then recreate everything
                Object.DestroyImmediate(canvasGO3);
                var oldUIM = FindRoot("UIManager");
                if (oldUIM != null) Object.DestroyImmediate(oldUIM);
                var oldAM = FindRoot("AudioManager");
                if (oldAM != null) Object.DestroyImmediate(oldAM);
                // Now fall through — Canvas/UIManager/AudioManager are gone, creation branch will run
            }
        }

        // ── AudioManager ─────────────────────────────────────────────────────
        if (FindRoot("AudioManager") == null)
            new GameObject("AudioManager").AddComponent<AudioManager>();

        // ── LocalizationManager ──────────────────────────────────────────────
        if (FindRoot("LocalizationManager") == null)
            new GameObject("LocalizationManager").AddComponent<LocalizationManager>();

        // ── FontLocalizationManager ───────────────────────────────────────────
        if (FindRoot("FontLocalizationManager") == null)
        {
            var flmGO = new GameObject("FontLocalizationManager");
            var flm   = flmGO.AddComponent<FontLocalizationManager>();

            string[] liberGuids = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset");
            TMP_FontAsset liberFont = liberGuids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(liberGuids[0]))
                : null;
            if (liberFont != null)
                flm.fontEntries.Add(new LanguageFontEntry { language = SupportedLanguage.English, primaryFont = liberFont });

            var thaiAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ThaiFont.asset");
            if (thaiAsset != null)
                flm.fontEntries.Add(new LanguageFontEntry { language = SupportedLanguage.Thai, primaryFont = thaiAsset });
        }

        // ── NPC Cars ─────────────────────────────────────────────────────────
        if (FindRoot("--- NPC Cars ---") == null)
        {
            var npcGroup = new GameObject("--- NPC Cars ---");
            CreateNPC("NPC_H0", new Vector3( 0f,  0.30f, 0), npcCarSpr, true,  -9f, 9f, 1.2f, npcGroup.transform);
            CreateNPC("NPC_H1", new Vector3( 0f, -0.30f, 0), npcCarSpr, true,  -9f, 9f, 1.4f, npcGroup.transform);
            CreateNPC("NPC_H2", new Vector3( 0f,  0.55f, 0), npcCarSpr, true,  -9f, 9f, 1.6f, npcGroup.transform);
            CreateNPC("NPC_H3", new Vector3( 0f, -0.55f, 0), npcCarSpr, true,  -9f, 9f, 1.8f, npcGroup.transform);
            CreateNPC("NPC_H4", new Vector3( 0f,  0.10f, 0), npcCarSpr, true,  -9f, 9f, 2.0f, npcGroup.transform);
            CreateNPC("NPC_V0", new Vector3( 0.30f, 0f, 0), npcCarSpr, false, -6f, 6f, 1.3f, npcGroup.transform);
            CreateNPC("NPC_V1", new Vector3(-0.30f, 0f, 0), npcCarSpr, false, -6f, 6f, 1.5f, npcGroup.transform);
            CreateNPC("NPC_V2", new Vector3( 0.55f, 0f, 0), npcCarSpr, false, -6f, 6f, 1.7f, npcGroup.transform);
            CreateNPC("NPC_V3", new Vector3(-0.55f, 0f, 0), npcCarSpr, false, -6f, 6f, 1.9f, npcGroup.transform);
            CreateNPC("NPC_V4", new Vector3( 0.10f, 0f, 0), npcCarSpr, false, -6f, 6f, 2.1f, npcGroup.transform);
            CreateBossNPC("NPC_BOSS0", new Vector3( 0f,   0.45f, 0), bossCarSpr1, true,  -9f, 9f, 2.2f, npcGroup.transform);
            CreateBossNPC("NPC_BOSS1", new Vector3( 0.45f, 0f,   0), bossCarSpr2, false, -6f, 6f, 2.2f, npcGroup.transform);
            CreateBossNPC("NPC_BOSS2", new Vector3( 0f,  -0.45f, 0), bossCarSpr3, true,  -9f, 9f, 2.4f, npcGroup.transform);
        }
        else
        {
            // Authoritative NPC data — must match the creation block above
            var npcData = new Dictionary<string, (float speed, float rangeMin, float rangeMax, bool moveH)>
            {
                { "NPC_H0",    (1.2f, -9f, 9f, true)  },
                { "NPC_H1",    (1.4f, -9f, 9f, true)  },
                { "NPC_H2",    (1.6f, -9f, 9f, true)  },
                { "NPC_H3",    (1.8f, -9f, 9f, true)  },
                { "NPC_H4",    (2.0f, -9f, 9f, true)  },
                { "NPC_V0",    (1.3f, -6f, 6f, false) },
                { "NPC_V1",    (1.5f, -6f, 6f, false) },
                { "NPC_V2",    (1.7f, -6f, 6f, false) },
                { "NPC_V3",    (1.9f, -6f, 6f, false) },
                { "NPC_V4",    (2.1f, -6f, 6f, false) },
                { "NPC_BOSS0", (2.2f, -9f, 9f, true)  },
                { "NPC_BOSS1", (2.2f, -6f, 6f, false) },
                { "NPC_BOSS2", (2.4f, -9f, 9f, true)  },
            };

            // Update all properties of existing NPCs (scales, sprites, speeds, ranges)
            var existingNpcGroup = FindRoot("--- NPC Cars ---");
            foreach (Transform npc in existingNpcGroup.transform)
            {
                bool isBoss = npc.name.StartsWith("NPC_BOSS");
                float targetScale = isBoss ? 2.835f : 2.295f;
                Sprite targetSprite = npcCarSpr;
                if (isBoss)
                {
                    if (npc.name.EndsWith("0")) targetSprite = bossCarSpr1;
                    else if (npc.name.EndsWith("1")) targetSprite = bossCarSpr2;
                    else targetSprite = bossCarSpr3;
                }
                var sprT = npc.Find("Sprite");
                if (sprT != null)
                {
                    sprT.localScale = new Vector3(targetScale, targetScale, 1f);
                    var sr = sprT.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        if (targetSprite != null) sr.sprite = targetSprite;
                        sr.color = Color.white;
                    }
                }
                // Ensure Rigidbody2D is Kinematic with correct settings
                var rb = npc.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.gravityScale = 0f;
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                }
                var col2 = npc.GetComponent<BoxCollider2D>();
                if (col2 != null) col2.size = new Vector2(0.4f, 0.8f);

                // Update speed, baseSpeed, range, and axis on the NPCCar component
                var npcCar = npc.GetComponent<NPCCar>();
                if (npcCar != null && npcData.TryGetValue(npc.name, out var data))
                {
                    npcCar.speed = data.speed;
                    npcCar.baseSpeed = data.speed;
                    npcCar.rangeMin = data.rangeMin;
                    npcCar.rangeMax = data.rangeMax;
                    npcCar.moveHorizontal = data.moveH;
                    npcCar.isBoss = isBoss;
                    EditorUtility.SetDirty(npcCar);
                }
            }
        }

        // ── GameManager ──────────────────────────────────────────────────────
        GameObject gmGO;
        GameManager gm;
        var existingGM = FindRoot("GameManager");
        if (existingGM == null)
        {
            gmGO = new GameObject("GameManager");
            gm   = gmGO.AddComponent<GameManager>();
        }
        else
        {
            gmGO = existingGM;
            gm   = existingGM.GetComponent<GameManager>();
        }

        // Always re-wire serialized references (survives script updates)
        if (gm != null)
        {
            if (uiManager  != null) gm.uiManager = uiManager;
            if (playerCtrl != null) gm.player    = playerCtrl;

            gm.normalCarSprites = new Sprite[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cars/car_normal_1.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cars/car_normal_2.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cars/car_normal_3.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cars/car_normal_4.png"),
            };
            gm.bossCarSprites = new Sprite[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cars/car_boss_1.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cars/car_boss_2.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cars/car_boss_3.png"),
            };
        }
        if (playerCtrl != null && joystick != null)
            playerCtrl.joystick = joystick;
        if (playerCtrl != null)
        {
            var boxSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Box/box.png");
            if (boxSpr != null) playerCtrl.packageSprite = boxSpr;
        }

        // ── PowerUpManager ───────────────────────────────────────────────────
        PowerUpManager pm;
        if (FindRoot("PowerUpManager") == null)
        {
            var pmGO = new GameObject("PowerUpManager");
            pm = pmGO.AddComponent<PowerUpManager>();
        }
        else
        {
            pm = Object.FindAnyObjectByType<PowerUpManager>();
        }
        if (pm != null)
        {
            pm.powerUpIcons = new Sprite[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Icons/icon_shield.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Icons/icon_rocket.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Icons/icon_clock.png"),
            };
        }

        // ── EventSystem ──────────────────────────────────────────────────────
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        // ── Mark scene dirty & save ──────────────────────────────────────────
        EditorUtility.SetDirty(gmGO);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        Debug.Log("[Delivery Dash] Scene setup complete! Press Play to test.");
    }

    // Full Rebuild removed — use Setup Scene only (non-destructive).

    // ── UI builder ───────────────────────────────────────────────────────────
    static void BuildUI(out UIManager uiManager, out VirtualJoystick joystick)
    {
        Sprite settingIconSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Icons/icon_setting.png");

        // Root canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920, 1080);
        scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight   = 0.5f;   // blend width + height — correct at any aspect ratio
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Gameplay HUD ──────────────────────────────────────────────────────
        var hudGO = new GameObject("GameplayUI", typeof(RectTransform));
        hudGO.transform.SetParent(canvasGO.transform, false);
        StretchFull(hudGO);

        // ── Gameplay HUD — top bar (dark bg) + carry badge + feedback/streak ──────
        // Layout: [Timer | bold] [Destination — center, dominant] [Score — right, yellow]
        //         [Carry badge — bottom-left pill]

        // Dark top-bar background (full width, top 12.5% of screen)
        var topBarGO = new GameObject("TopBar", typeof(RectTransform));
        topBarGO.transform.SetParent(hudGO.transform, false);
        var topBarRT = topBarGO.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(0f, 0.875f);
        topBarRT.anchorMax = Vector2.one;
        topBarRT.offsetMin = topBarRT.offsetMax = Vector2.zero;
        topBarGO.AddComponent<UnityEngine.UI.Image>().color = new Color(0.04f, 0.05f, 0.12f, 0.88f);

        // Timer — top-left, large bold (top 65% of topbar)
        var timerTxt = MakeTMP(hudGO, "TimerText", "32s", 46, TextAlignmentOptions.MidlineLeft,
            new Vector2(0.01f, 0.915f), new Vector2(0.19f, 1.0f), new Vector2(-10, -8), Vector2.zero);
        timerTxt.fontStyle = FontStyles.Bold;
        timerTxt.color = new Color(0f, 0.85f, 1f);

        // Lives — hearts display, right of timer
        var livesTxt = MakeTMP(hudGO, "LivesText", "", 36, TextAlignmentOptions.MidlineLeft,
            new Vector2(0.19f, 0.915f), new Vector2(0.42f, 1.0f), new Vector2(-4, -8), Vector2.zero);
        livesTxt.fontStyle = FontStyles.Bold;
        livesTxt.color = Color.white;
        livesTxt.richText = true;
        livesTxt.gameObject.SetActive(false);

        // Level — bottom 35% of topbar, auto-size so it fills its container at any resolution
        var levelTxt = MakeTMP(hudGO, "LevelText", "Level 1/30", 28, TextAlignmentOptions.MidlineLeft,
            new Vector2(0.01f, 0.875f), new Vector2(0.22f, 0.915f), new Vector2(-10, -2), Vector2.zero);
        levelTxt.color = new Color(0.60f, 0.68f, 0.82f);
        levelTxt.enableAutoSizing = true;
        levelTxt.fontSizeMin = 14;
        levelTxt.fontSizeMax = 28;

        // Destination — center, dominant
        var orderTxt = MakeTMP(hudGO, "OrderText", "Deliver to:\n—", 40, TextAlignmentOptions.Center,
            new Vector2(0.19f, 0.875f), new Vector2(0.78f, 1.0f), new Vector2(-10, -8), Vector2.zero);
        orderTxt.fontStyle = FontStyles.Bold;

        // Score — top-right, yellow bold (full right area — button is below, not beside)
        var scoreTxt = MakeTMP(hudGO, "ScoreText", "Score: 0", 32, TextAlignmentOptions.MidlineRight,
            new Vector2(0.76f, 0.905f), new Vector2(0.99f, 1.0f), new Vector2(-10, -8), Vector2.zero);
        scoreTxt.fontStyle = FontStyles.Bold;
        scoreTxt.color = new Color(1f, 0.82f, 0.15f);

        // Settings icon button — small square BELOW the TopBar, anchored bottom-right
        // pivot (1,1) = top-right corner sits at anchor point → button hangs downward
        // sizeDelta 110 canvas-units ≈ 40px at portrait 424×645 (good tap target)
        var hudSettingsBtnGO = new GameObject("HudSettingsButton", typeof(RectTransform));
        hudSettingsBtnGO.transform.SetParent(hudGO.transform, false);
        var hudSettingsBtnRT = hudSettingsBtnGO.GetComponent<RectTransform>();
        hudSettingsBtnRT.anchorMin        = new Vector2(1.0f, 0.875f);
        hudSettingsBtnRT.anchorMax        = new Vector2(1.0f, 0.875f);
        hudSettingsBtnRT.pivot            = new Vector2(1.0f, 1.0f);
        hudSettingsBtnRT.sizeDelta        = new Vector2(110f, 110f);
        hudSettingsBtnRT.anchoredPosition = new Vector2(0f, 0f);
        var hudSettingsBtnBg = hudSettingsBtnGO.AddComponent<Image>();
        hudSettingsBtnBg.color = new Color(0.04f, 0.05f, 0.12f, 0.75f);
        var hudSettingsBtn = hudSettingsBtnGO.AddComponent<Button>();
        hudSettingsBtn.targetGraphic = hudSettingsBtnBg;
        var hudSettingsCols = hudSettingsBtn.colors;
        hudSettingsCols.normalColor      = new Color(0.04f, 0.05f, 0.12f, 0.75f);
        hudSettingsCols.highlightedColor = new Color(0.15f, 0.20f, 0.38f, 0.90f);
        hudSettingsCols.pressedColor     = new Color(0.02f, 0.03f, 0.08f, 0.90f);
        hudSettingsBtn.colors = hudSettingsCols;
        // Icon image
        var settingsIconChildGO = new GameObject("Icon", typeof(RectTransform));
        settingsIconChildGO.transform.SetParent(hudSettingsBtnGO.transform, false);
        var settingsIconRT = settingsIconChildGO.GetComponent<RectTransform>();
        settingsIconRT.anchorMin = Vector2.zero;
        settingsIconRT.anchorMax = Vector2.one;
        settingsIconRT.offsetMin = new Vector2(12f, 12f);
        settingsIconRT.offsetMax = new Vector2(-12f, -12f);
        var settingsIconImg = settingsIconChildGO.AddComponent<Image>();
        if (settingIconSpr != null) settingsIconImg.sprite = settingIconSpr;
        settingsIconImg.color = new Color(0.75f, 0.85f, 1f, 0.95f);

        // Clean up removed UI elements
        var oldCarryBadge = hudGO.transform.Find("CarryBadge");
        if (oldCarryBadge != null) Object.DestroyImmediate(oldCarryBadge.gameObject);
        var oldCarryText = hudGO.transform.Find("CarryText");
        if (oldCarryText != null) Object.DestroyImmediate(oldCarryText.gameObject);

        // Feedback — center screen, bold
        var feedTxt = MakeTMP(hudGO, "FeedbackText", "", 38, TextAlignmentOptions.Center,
            new Vector2(0.15f, 0.52f), new Vector2(0.85f, 0.62f), new Vector2(-20, -8), Vector2.zero);
        feedTxt.fontStyle = FontStyles.Bold;
        feedTxt.gameObject.SetActive(false);

        // Streak — center, above joystick
        var streakTxt = MakeTMP(hudGO, "StreakText", "", 40, TextAlignmentOptions.Center,
            new Vector2(0.3f, 0.19f), new Vector2(0.7f, 0.27f), new Vector2(-20, -8), Vector2.zero);
        streakTxt.fontStyle = FontStyles.Bold;
        streakTxt.gameObject.SetActive(false);

        // Crash flash overlay — full-screen red, fades out on hit
        var crashFlashGO = new GameObject("CrashFlash", typeof(RectTransform));
        crashFlashGO.transform.SetParent(hudGO.transform, false);
        var crashFlashImg = crashFlashGO.AddComponent<UnityEngine.UI.Image>();
        crashFlashImg.color = new Color(1f, 0.08f, 0.08f, 0f);
        StretchFull(crashFlashGO);
        crashFlashGO.SetActive(false);

        // ── Start Screen ──────────────────────────────────────────────────────
        // Overlay screens: horizontal-stretch anchors on all text containers so content
        // fills available width at any aspect ratio. Vertical position stays fixed from centre.
        var startGO = MakeFullScreenPanel(canvasGO, "StartScreen", new Color(0.05f, 0.06f, 0.13f, 0.93f));

        var startTitleTxt = MakeTMP(startGO, "TitleText", "DELIVERY DASH", 76, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0, 100), new Vector2(0, 190));
        startTitleTxt.fontStyle = FontStyles.Bold;
        startTitleTxt.color = new Color(0f, 0.85f, 1f);

        var startSubtitleTxt = MakeTMP(startGO, "SubtitleText",
            "Pick up packages & deliver to the right location!\nWatch out for traffic  |  Rush orders = double points  |  10 Levels",
            26, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0, 76), new Vector2(0, 95));
        startSubtitleTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);

        var reqTxt = MakeTMP(startGO, "LevelReqs",
            "Lv1: 30s→50pts  Lv2: 30s→70pts  Lv3: 35s→90pts  Lv4: 35s→110pts  Lv5: 40s→130pts\n" +
            "Lv6: 40s→155pts  Lv7: 45s→180pts  Lv8: 45s→210pts  Lv9: 50s→240pts  Lv10: 50s→275pts",
            18, TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.5f), new Vector2(0, 88), new Vector2(0, 5));
        reqTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);

        var startBtn = MakeButton(startGO, "StartButton", "START GAME",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(280, 72), new Vector2(0, -90));
        ApplyButtonColor(startBtn, new Color(0f, 0.75f, 0.85f), new Color(0f, 0.90f, 1f), new Color(0f, 0.55f, 0.65f));

        var startSettingsBtn = MakeButton(startGO, "StartSettingsButton", "\u2699 Settings",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180, 52), new Vector2(0, -172));
        ApplyButtonColor(startSettingsBtn, new Color(0.22f, 0.25f, 0.35f), new Color(0.32f, 0.38f, 0.52f), new Color(0.14f, 0.16f, 0.24f));

        // ── Level Fail Screen ─────────────────────────────────────────────────
        var gameOverGO = MakeFullScreenPanel(canvasGO, "GameOverScreen", new Color(0.08f, 0.05f, 0.12f, 0.93f));
        gameOverGO.SetActive(false);

        var failTitleTxt = MakeTMP(gameOverGO, "FailTitle", "LEVEL 1 FAILED", 60, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
        failTitleTxt.color = new Color(0.85f, 0.30f, 0.15f);

        var finalScoreTxt = MakeTMP(gameOverGO, "FinalScoreText", "Score: 0", 48, TextAlignmentOptions.Center,
            new Vector2(0.10f, 0.55f), new Vector2(0.90f, 0.68f), Vector2.zero, Vector2.zero);
        finalScoreTxt.color = Color.white;

        var neededTxt = MakeTMP(gameOverGO, "NeededScoreText", "Need 50 to pass", 32, TextAlignmentOptions.Center,
            new Vector2(0.10f, 0.42f), new Vector2(0.90f, 0.54f), Vector2.zero, Vector2.zero);
        neededTxt.color = new Color(0.85f, 0.60f, 0.30f);

        var retryBtn = MakeButton(gameOverGO, "RetryButton", "TRY AGAIN",
            new Vector2(0.25f, 0.10f), new Vector2(0.75f, 0.20f), Vector2.zero, Vector2.zero);
        ApplyButtonColor(retryBtn, new Color(0.85f, 0.22f, 0.15f), new Color(1f, 0.35f, 0.25f), new Color(0.65f, 0.12f, 0.08f));

        // ── Level Complete Screen ─────────────────────────────────────────────
        var levelCompleteGO = MakeFullScreenPanel(canvasGO, "LevelCompleteScreen", new Color(0.05f, 0.08f, 0.12f, 0.93f));
        levelCompleteGO.SetActive(false);

        var lcTitleTxt = MakeTMP(levelCompleteGO, "LCTitle", "LEVEL COMPLETE!", 60, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
        lcTitleTxt.color = new Color(0.85f, 0.65f, 0.20f);

        var lcScoreTxt = MakeTMP(levelCompleteGO, "LCScoreText", "Score: 0", 48, TextAlignmentOptions.Center,
            new Vector2(0.10f, 0.55f), new Vector2(0.90f, 0.68f), Vector2.zero, Vector2.zero);
        lcScoreTxt.color = Color.white;

        var lcNextTxt = MakeTMP(levelCompleteGO, "LCNextText", "Next: Level 2", 32, TextAlignmentOptions.Center,
            new Vector2(0.10f, 0.42f), new Vector2(0.90f, 0.54f), Vector2.zero, Vector2.zero);
        lcNextTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);

        var lcCountdownTxt = MakeTMP(levelCompleteGO, "LCCountdownText", "Auto-advancing in 5...", 22, TextAlignmentOptions.Center,
            new Vector2(0.10f, 0.32f), new Vector2(0.90f, 0.42f), Vector2.zero, Vector2.zero);
        lcCountdownTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);
        lcCountdownTxt.gameObject.SetActive(false);

        var nextLvlBtn = MakeButton(levelCompleteGO, "NextLevelButton", "NEXT LEVEL",
            new Vector2(0.25f, 0.10f), new Vector2(0.75f, 0.20f), Vector2.zero, Vector2.zero);
        ApplyButtonColor(nextLvlBtn, new Color(0.12f, 0.65f, 0.32f), new Color(0.18f, 0.80f, 0.42f), new Color(0.08f, 0.48f, 0.22f));

        // ── Victory Screen ────────────────────────────────────────────────────
        var victoryGO = MakeFullScreenPanel(canvasGO, "VictoryScreen", new Color(0.05f, 0.06f, 0.13f, 0.93f));
        victoryGO.SetActive(false);

        var vicTitleTxt = MakeTMP(victoryGO, "VicTitle", "CONGRATULATIONS!", 64, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.75f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
        vicTitleTxt.color = new Color(0.85f, 0.65f, 0.20f);

        var vicSubtitleTxt = MakeTMP(victoryGO, "VicSubtitle", "You cleared all 30 levels!", 36, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.74f), Vector2.zero, Vector2.zero);
        vicSubtitleTxt.color = Color.white;

        var vicScoreTxt = MakeTMP(victoryGO, "VicScoreText", "Final Score: 0", 48, TextAlignmentOptions.Center,
            new Vector2(0.10f, 0.48f), new Vector2(0.90f, 0.60f), Vector2.zero, Vector2.zero);
        vicScoreTxt.color = new Color(1f, 0.82f, 0.15f);

        var playAgainBtn = MakeButton(victoryGO, "PlayAgainButton", "PLAY AGAIN",
            new Vector2(0.25f, 0.10f), new Vector2(0.75f, 0.20f), Vector2.zero, Vector2.zero);
        ApplyButtonColor(playAgainBtn, new Color(0f, 0.75f, 0.85f), new Color(0f, 0.90f, 1f), new Color(0f, 0.55f, 0.65f));

        // ── Pause Screen ──────────────────────────────────────────────────────
        var pauseGO = MakeFullScreenPanel(canvasGO, "PauseScreen", new Color(0.05f, 0.06f, 0.13f, 0.85f));
        pauseGO.SetActive(false);

        var pauseTitleTxt = MakeTMP(pauseGO, "PauseTitle", "PAUSED", 76, TextAlignmentOptions.Center,
            new Vector2(0.15f, 0.5f), new Vector2(0.85f, 0.5f), new Vector2(0, 100), new Vector2(0, 130));
        pauseTitleTxt.color = Color.white;

        var pauseHintTxt = MakeTMP(pauseGO, "PauseHint", "ESC to resume", 24, TextAlignmentOptions.Center,
            new Vector2(0.15f, 0.5f), new Vector2(0.85f, 0.5f), new Vector2(0, 40), new Vector2(0, 60));
        pauseHintTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);

        var resumeBtn  = MakeButton(pauseGO, "ResumeButton",  "RESUME",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220, 64), new Vector2(0, -20));
        ApplyButtonColor(resumeBtn, new Color(0.12f, 0.65f, 0.32f), new Color(0.18f, 0.80f, 0.42f), new Color(0.08f, 0.48f, 0.22f));

        var restartBtn = MakeButton(pauseGO, "RestartButton", "RETRY LEVEL",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220, 64), new Vector2(0, -95));
        ApplyButtonColor(restartBtn, new Color(0.85f, 0.22f, 0.15f), new Color(1f, 0.35f, 0.25f), new Color(0.65f, 0.12f, 0.08f));

        var settingsBtn = MakeButton(pauseGO, "SettingsButton", "Settings",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220, 64), new Vector2(0, -170));
        ApplyButtonColor(settingsBtn, new Color(0.22f, 0.25f, 0.35f), new Color(0.32f, 0.38f, 0.52f), new Color(0.14f, 0.16f, 0.24f));

        var selectModeBtn = MakeButton(pauseGO, "SelectModeButton", "SELECT MODE",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220, 64), new Vector2(0, -245));
        ApplyButtonColor(selectModeBtn, new Color(0f, 0.75f, 0.85f), new Color(0f, 0.90f, 1f), new Color(0f, 0.55f, 0.65f));

        // ── Settings Screen ───────────────────────────────────────────────────
        var settingsGO = new GameObject("SettingsScreen", typeof(RectTransform));
        settingsGO.transform.SetParent(canvasGO.transform, false);
        var settingsImg = settingsGO.AddComponent<Image>();
        settingsImg.color = new Color(0.05f, 0.06f, 0.10f, 0.75f);
        StretchFull(settingsGO);

        var settingsPanelGO = new GameObject("SettingsPanel", typeof(RectTransform));
        settingsPanelGO.transform.SetParent(settingsGO.transform, false);
        var settingsPanelImg = settingsPanelGO.AddComponent<Image>();
        settingsPanelImg.color = new Color(0.12f, 0.14f, 0.22f, 0.95f);
        var settingsPanelRT = settingsPanelGO.GetComponent<RectTransform>();
        settingsPanelRT.anchorMin = new Vector2(0.10f, 0.5f);
        settingsPanelRT.anchorMax = new Vector2(0.90f, 0.5f);
        settingsPanelRT.pivot     = new Vector2(0.5f, 0.5f);
        settingsPanelRT.sizeDelta = new Vector2(0, 350);
        settingsPanelRT.anchoredPosition = Vector2.zero;

        // Panel height to fit all controls
        settingsPanelRT.sizeDelta = new Vector2(0, 820);

        var settingsTitleTxt = MakeTMP(settingsPanelGO, "SettingsTitle", "Settings", 52, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0, 60), new Vector2(0, 360));
        settingsTitleTxt.color = Color.white;

        var volumeLabelTxt = MakeTMP(settingsPanelGO, "VolumeLabel", "Volume", 30, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0, 44), new Vector2(0, 300));
        volumeLabelTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);

        var sliderGO = new GameObject("VolumeSlider", typeof(RectTransform));
        sliderGO.transform.SetParent(settingsPanelGO.transform, false);
        var sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.anchorMin       = new Vector2(0.1f, 0.5f);
        sliderRT.anchorMax       = new Vector2(0.9f, 0.5f);
        sliderRT.pivot           = new Vector2(0.5f, 0.5f);
        sliderRT.sizeDelta       = new Vector2(0, 40);
        sliderRT.anchoredPosition = new Vector2(0, 260);

        var sliderBgGO = new GameObject("Background", typeof(RectTransform));
        sliderBgGO.transform.SetParent(sliderGO.transform, false);
        var sliderBgImg = sliderBgGO.AddComponent<Image>();
        sliderBgImg.color = new Color(0.25f, 0.28f, 0.35f);
        var sliderBgRT = sliderBgGO.GetComponent<RectTransform>();
        sliderBgRT.anchorMin = new Vector2(0, 0.25f);
        sliderBgRT.anchorMax = new Vector2(1, 0.75f);
        sliderBgRT.offsetMin = sliderBgRT.offsetMax = Vector2.zero;

        var sliderFillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        sliderFillAreaGO.transform.SetParent(sliderGO.transform, false);
        var sliderFillAreaRT = sliderFillAreaGO.GetComponent<RectTransform>();
        sliderFillAreaRT.anchorMin = new Vector2(0, 0.25f);
        sliderFillAreaRT.anchorMax = new Vector2(1, 0.75f);
        sliderFillAreaRT.offsetMin = new Vector2(5, 0);
        sliderFillAreaRT.offsetMax = new Vector2(-15, 0);

        var sliderFillGO = new GameObject("Fill", typeof(RectTransform));
        sliderFillGO.transform.SetParent(sliderFillAreaGO.transform, false);
        var sliderFillImg = sliderFillGO.AddComponent<Image>();
        sliderFillImg.color = new Color(0f, 0.75f, 0.85f);
        var sliderFillRT = sliderFillGO.GetComponent<RectTransform>();
        sliderFillRT.anchorMin = Vector2.zero;
        sliderFillRT.anchorMax = new Vector2(0, 1);
        sliderFillRT.offsetMin = sliderFillRT.offsetMax = Vector2.zero;

        var sliderHandleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
        sliderHandleAreaGO.transform.SetParent(sliderGO.transform, false);
        var sliderHandleAreaRT = sliderHandleAreaGO.GetComponent<RectTransform>();
        sliderHandleAreaRT.anchorMin = new Vector2(0, 0);
        sliderHandleAreaRT.anchorMax = new Vector2(1, 1);
        sliderHandleAreaRT.offsetMin = new Vector2(10, 0);
        sliderHandleAreaRT.offsetMax = new Vector2(-10, 0);

        var sliderHandleGO = new GameObject("Handle", typeof(RectTransform));
        sliderHandleGO.transform.SetParent(sliderHandleAreaGO.transform, false);
        var sliderHandleImg = sliderHandleGO.AddComponent<Image>();
        sliderHandleImg.color = Color.white;
        var sliderHandleRT = sliderHandleGO.GetComponent<RectTransform>();
        sliderHandleRT.anchorMin = new Vector2(0, 0);
        sliderHandleRT.anchorMax = new Vector2(0, 1);
        sliderHandleRT.sizeDelta = new Vector2(20, 0);

        var volumeSlider = sliderGO.AddComponent<Slider>();
        volumeSlider.fillRect      = sliderFillRT;
        volumeSlider.handleRect    = sliderHandleRT;
        volumeSlider.direction     = Slider.Direction.LeftToRight;
        volumeSlider.minValue      = 0f;
        volumeSlider.maxValue      = 1f;
        volumeSlider.value         = 1f;

        // BGM label + slider
        var bgmLabelTxt = MakeTMP(settingsPanelGO, "BGMLabel", "Music", 28, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0, 30), new Vector2(0, 200));
        bgmLabelTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);
        var bgmSliderGO = BuildSettingsSliderInSetup(settingsPanelGO, "BGMSlider", 170);

        // SFX label + slider
        var sfxLabelTxt = MakeTMP(settingsPanelGO, "SFXLabel", "Effects", 28, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0, 30), new Vector2(0, 110));
        sfxLabelTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);
        var sfxSliderGO = BuildSettingsSliderInSetup(settingsPanelGO, "SFXSlider", 80);

        // Zone Labels label + slider
        var zoneLabelLabelTxt = MakeTMP(settingsPanelGO, "ZoneLabelLabel", "Zone Labels", 28, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0, 30), new Vector2(0, 20));
        zoneLabelLabelTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);
        var zoneLabelSliderGO = BuildSettingsSliderInSetup(settingsPanelGO, "ZoneLabelSlider", -10);

        // Fullscreen toggle button
        var fsGO = new GameObject("FullscreenButton", typeof(RectTransform));
        fsGO.transform.SetParent(settingsPanelGO.transform, false);
        var fsRT = fsGO.GetComponent<RectTransform>();
        fsRT.anchorMin = new Vector2(0.15f, 0.5f);
        fsRT.anchorMax = new Vector2(0.85f, 0.5f);
        fsRT.pivot = new Vector2(0.5f, 0.5f);
        fsRT.sizeDelta = new Vector2(0, 40);
        fsRT.anchoredPosition = new Vector2(0, -70);
        var fsImg = fsGO.AddComponent<Image>();
        fsImg.color = new Color(0.22f, 0.25f, 0.35f, 0.9f);
        var fsBtn = fsGO.AddComponent<Button>();
        fsBtn.targetGraphic = fsImg;
        var fsCols = fsBtn.colors;
        fsCols.normalColor      = new Color(0.22f, 0.25f, 0.35f, 0.9f);
        fsCols.highlightedColor = new Color(0.32f, 0.38f, 0.52f, 1f);
        fsCols.pressedColor     = new Color(0.14f, 0.16f, 0.24f, 1f);
        fsBtn.colors = fsCols;
        var fsTxtGO = new GameObject("Label", typeof(RectTransform));
        fsTxtGO.transform.SetParent(fsGO.transform, false);
        var fsTxtRT = fsTxtGO.GetComponent<RectTransform>();
        fsTxtRT.anchorMin = Vector2.zero; fsTxtRT.anchorMax = Vector2.one;
        fsTxtRT.offsetMin = Vector2.zero; fsTxtRT.offsetMax = Vector2.zero;
        var fsTMP = fsTxtGO.AddComponent<TextMeshProUGUI>();
        fsTMP.fontSize = 24;
        fsTMP.alignment = TextAlignmentOptions.Center;
        fsTMP.color = Color.white;

        // Language button
        var langBtnGO = MakeButton(settingsPanelGO, "LanguageButton", "Language: EN",
            new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.5f), new Vector2(0, 50), new Vector2(0, -140));
        ApplyButtonColor(langBtnGO, new Color(0f, 0.75f, 0.85f), new Color(0f, 0.90f, 1f), new Color(0f, 0.55f, 0.65f));
        var langBtnTxtTMP = langBtnGO.GetComponentInChildren<TextMeshProUGUI>();

        // Close button
        var closeSettingsBtnGO = MakeButton(settingsPanelGO, "CloseSettingsButton", "Close",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180, 56), new Vector2(0, -210));
        ApplyButtonColor(closeSettingsBtnGO, new Color(0.22f, 0.25f, 0.35f), new Color(0.32f, 0.38f, 0.52f), new Color(0.14f, 0.16f, 0.24f));

        settingsGO.SetActive(false);

        // ── Mode Select Screen ────────────────────────────────────────────────
        var modeSelectGO = MakeFullScreenPanel(canvasGO, "ModeSelectScreen", new Color(0.05f, 0.06f, 0.13f, 0.93f));
        modeSelectGO.SetActive(false);

        // ── Content container centered vertically ──
        var contentGO = new GameObject("ModeContent", typeof(RectTransform));
        contentGO.transform.SetParent(modeSelectGO.transform, false);
        var contentRt = contentGO.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 0.5f);
        contentRt.anchorMax = new Vector2(1f, 0.5f);
        contentRt.pivot = new Vector2(0.5f, 0.5f);
        contentRt.sizeDelta = new Vector2(0, 1050);
        contentRt.anchoredPosition = Vector2.zero;

        // Title
        var modeSelectTitleTxt = MakeTMP(contentGO, "ModeTitle", "SELECT MODE", 64, TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.5f), new Vector2(0, 48), new Vector2(0, 470));
        modeSelectTitleTxt.color = Color.white;

        // ── Standard section ──
        var standardHeaderTxt = MakeTMP(contentGO, "StandardHeader", "STANDARD", 26, TextAlignmentOptions.Center,
            new Vector2(0.25f, 0.5f), new Vector2(0.75f, 0.5f), new Vector2(0, 28), new Vector2(0, 370));
        standardHeaderTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);
        standardHeaderTxt.fontStyle = FontStyles.Bold;

        var sepTop = new GameObject("SepTop", typeof(RectTransform));
        sepTop.transform.SetParent(contentGO.transform, false);
        var sepTopImg = sepTop.AddComponent<Image>();
        sepTopImg.color = new Color(0.45f, 0.55f, 0.75f, 0.6f);
        var sepTopRt = sepTop.GetComponent<RectTransform>();
        sepTopRt.anchorMin = new Vector2(0.15f, 0.5f); sepTopRt.anchorMax = new Vector2(0.85f, 0.5f);
        sepTopRt.pivot = new Vector2(0.5f, 0.5f);
        sepTopRt.sizeDelta = new Vector2(0, 2); sepTopRt.anchoredPosition = new Vector2(0, 398);

        // Heart Mode
        var normalBtn = MakeButton(contentGO, "NormalModeButton", "\u2665  HEART MODE",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(440, 68), new Vector2(0, 300));
        ApplyButtonColor(normalBtn, new Color(0f, 0.75f, 0.85f), new Color(0f, 0.90f, 1f), new Color(0f, 0.55f, 0.65f));
        var normalDescTxt = MakeTMP(contentGO, "NormalDesc", "More hearts as you progress!", 18, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0, 20), new Vector2(0, 256));
        normalDescTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);

        // Rush Mode
        var rushBtn = MakeButton(contentGO, "RushModeButton", "\u26a1  RUSH MODE",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(440, 68), new Vector2(0, 195));
        ApplyButtonColor(rushBtn, new Color(0f, 0.75f, 0.85f), new Color(0f, 0.90f, 1f), new Color(0f, 0.55f, 0.65f));
        var rushDescTxt = MakeTMP(contentGO, "RushDesc", "Hit target to advance! Crash = lose time", 18, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0, 20), new Vector2(0, 151));
        rushDescTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);

        // ── Extreme section ──
        var sepMid = new GameObject("SepMid", typeof(RectTransform));
        sepMid.transform.SetParent(contentGO.transform, false);
        var sepMidImg = sepMid.AddComponent<Image>();
        sepMidImg.color = new Color(0.45f, 0.55f, 0.75f, 0.6f);
        var sepMidRt = sepMid.GetComponent<RectTransform>();
        sepMidRt.anchorMin = new Vector2(0.15f, 0.5f); sepMidRt.anchorMax = new Vector2(0.85f, 0.5f);
        sepMidRt.pivot = new Vector2(0.5f, 0.5f);
        sepMidRt.sizeDelta = new Vector2(0, 2); sepMidRt.anchoredPosition = new Vector2(0, 100);

        var extremeHeaderTxt = MakeTMP(contentGO, "ExtremeHeader", "EXTREME", 26, TextAlignmentOptions.Center,
            new Vector2(0.25f, 0.5f), new Vector2(0.75f, 0.5f), new Vector2(0, 28), new Vector2(0, 68));
        extremeHeaderTxt.color = new Color(0.85f, 0.45f, 0.15f);
        extremeHeaderTxt.fontStyle = FontStyles.Bold;

        // Heart Extreme
        var heartExtremeBtn = MakeButton(contentGO, "HeartExtremeModeButton", "\u2665  HEART EXTREME",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(440, 68), new Vector2(0, -5));
        ApplyButtonColor(heartExtremeBtn, new Color(0.75f, 0.35f, 0.12f), new Color(0.90f, 0.45f, 0.18f), new Color(0.55f, 0.25f, 0.08f));
        var heartExtremeDescTxt = MakeTMP(contentGO, "HeartExtremeDesc", "Heart Mode + 50% faster traffic!", 18, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0, 20), new Vector2(0, -49));
        heartExtremeDescTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);

        // Rush Extreme
        var rushExtremeBtn = MakeButton(contentGO, "RushExtremeModeButton", "\u26a1  RUSH EXTREME",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(440, 68), new Vector2(0, -110));
        ApplyButtonColor(rushExtremeBtn, new Color(0.75f, 0.35f, 0.12f), new Color(0.90f, 0.45f, 0.18f), new Color(0.55f, 0.25f, 0.08f));
        var rushExtremeDescTxt = MakeTMP(contentGO, "RushExtremeDesc", "Rush Mode + 50% faster traffic!", 18, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0, 20), new Vector2(0, -154));
        rushExtremeDescTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);

        // ── Endless section ──
        var sepBot = new GameObject("SepBot", typeof(RectTransform));
        sepBot.transform.SetParent(contentGO.transform, false);
        var sepBotImg = sepBot.AddComponent<Image>();
        sepBotImg.color = new Color(0.45f, 0.55f, 0.75f, 0.6f);
        var sepBotRt = sepBot.GetComponent<RectTransform>();
        sepBotRt.anchorMin = new Vector2(0.15f, 0.5f); sepBotRt.anchorMax = new Vector2(0.85f, 0.5f);
        sepBotRt.pivot = new Vector2(0.5f, 0.5f);
        sepBotRt.sizeDelta = new Vector2(0, 2); sepBotRt.anchoredPosition = new Vector2(0, -205);

        var endlessBtn = MakeButton(contentGO, "EndlessModeButton", "\u221e  ENDLESS",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(440, 68), new Vector2(0, -265));
        ApplyButtonColor(endlessBtn, new Color(0f, 0.75f, 0.85f), new Color(0f, 0.90f, 1f), new Color(0f, 0.55f, 0.65f));
        var endlessDescTxt = MakeTMP(contentGO, "EndlessDesc", "Survive as long as you can!", 18, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0, 20), new Vector2(0, -309));
        endlessDescTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);

        // Back to Start button (outside ModeContent, pinned to bottom of ModeSelectScreen)
        var modeBackBtn = MakeButton(modeSelectGO, "BackToStartButton", "BACK",
            new Vector2(0.30f, 0.02f), new Vector2(0.70f, 0.08f), Vector2.zero, Vector2.zero);
        ApplyButtonColor(modeBackBtn, new Color(0.22f, 0.25f, 0.35f), new Color(0.32f, 0.38f, 0.52f), new Color(0.14f, 0.16f, 0.24f));

        // ── Endless Summary Screen ────────────────────────────────────────────
        var endlessSummaryGO = MakeFullScreenPanel(canvasGO, "EndlessSummaryScreen", new Color(0.05f, 0.06f, 0.13f, 0.94f));
        endlessSummaryGO.SetActive(false);

        var endlessTitleTxt = MakeTMP(endlessSummaryGO, "EndlessTitle", "RUN COMPLETE!", 68, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
        endlessTitleTxt.color = new Color(0.85f, 0.65f, 0.20f);

        var endlessTierTxt = MakeTMP(endlessSummaryGO, "EndlessTierText", "Tiers Reached: 0", 44, TextAlignmentOptions.Center,
            new Vector2(0.10f, 0.64f), new Vector2(0.90f, 0.75f), Vector2.zero, Vector2.zero);
        endlessTierTxt.color = new Color(0.85f, 0.65f, 0.20f);

        var endlessScTxt = MakeTMP(endlessSummaryGO, "EndlessScoreText", "Score: 0", 44, TextAlignmentOptions.Center,
            new Vector2(0.10f, 0.52f), new Vector2(0.90f, 0.63f), Vector2.zero, Vector2.zero);
        endlessScTxt.color = Color.white;

        var endlessDelivTxt = MakeTMP(endlessSummaryGO, "EndlessDelivText", "Deliveries: 0", 34, TextAlignmentOptions.Center,
            new Vector2(0.10f, 0.42f), new Vector2(0.90f, 0.52f), Vector2.zero, Vector2.zero);
        endlessDelivTxt.color = new Color(0.75f, 0.85f, 1f, 0.85f);

        var endlessRetryBtn = MakeButton(endlessSummaryGO, "EndlessRetryButton", "RETRY ENDLESS",
            new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.18f), Vector2.zero, Vector2.zero);
        ApplyButtonColor(endlessRetryBtn, new Color(0.85f, 0.22f, 0.15f), new Color(1f, 0.35f, 0.25f), new Color(0.65f, 0.12f, 0.08f));

        var endlessSelectBtn = MakeButton(endlessSummaryGO, "EndlessSelectModeButton", "SELECT MODE",
            new Vector2(0.52f, 0.08f), new Vector2(0.92f, 0.18f), Vector2.zero, Vector2.zero);
        ApplyButtonColor(endlessSelectBtn, new Color(0f, 0.75f, 0.85f), new Color(0f, 0.90f, 1f), new Color(0f, 0.55f, 0.65f));

        // ── Virtual Joystick — floating, responsive layout ─────────────────────
        Sprite circleSpr     = BuildCircleSprite();
        Sprite ringSprite    = BuildJoystickRingSprite();

        // Zone: transparent hit area — VirtualJoystick sets its anchors at runtime
        var zoneGO = new GameObject("JoystickZone", typeof(RectTransform));
        zoneGO.transform.SetParent(hudGO.transform, false);
        var zoneImg = zoneGO.AddComponent<Image>();
        zoneImg.color = Color.clear;           // invisible but catches raycasts
        zoneImg.raycastTarget = true;
        var zoneRT = zoneGO.GetComponent<RectTransform>();
        // Default anchors — VirtualJoystick.Start() will override based on orientation
        zoneRT.anchorMin = Vector2.zero;
        zoneRT.anchorMax = new Vector2(0.38f, 0.54f);
        zoneRT.pivot     = new Vector2(0.5f, 0.5f);
        zoneRT.offsetMin = zoneRT.offsetMax = Vector2.zero;
        joystick = zoneGO.AddComponent<VirtualJoystick>();

        // BG circle: floating visual — appears at touch point, hidden while idle
        var bgGO = new GameObject("JoystickBG", typeof(RectTransform));
        bgGO.transform.SetParent(zoneGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = ringSprite;
        bgImg.color  = new Color(0.04f, 0.06f, 0.14f, 0.72f);
        bgImg.raycastTarget = false;
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = bgRT.anchorMax = new Vector2(0.5f, 0.5f);
        bgRT.pivot     = new Vector2(0.5f, 0.5f);
        bgRT.sizeDelta = new Vector2(210f, 210f);   // runtime adjusts
        bgRT.anchoredPosition = Vector2.zero;
        joystick.bgCircle = bgRT;

        // Handle: thumb nub inside bgCircle
        var handleGO = new GameObject("Handle", typeof(RectTransform));
        handleGO.transform.SetParent(bgGO.transform, false);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.sprite = circleSpr;
        handleImg.color  = new Color(0.85f, 0.92f, 1.00f, 0.90f);
        handleImg.raycastTarget = false;
        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = handleRT.anchorMax = new Vector2(0.5f, 0.5f);
        handleRT.pivot     = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(84f, 84f);   // runtime adjusts
        handleRT.anchoredPosition = Vector2.zero;
        joystick.handle = handleRT;

        // ── Attach UIManager ──────────────────────────────────────────────────
        var uiManagerGO = new GameObject("UIManager");
        uiManager = uiManagerGO.AddComponent<UIManager>();

        uiManager.timerText              = timerTxt;
        uiManager.crashFlashImage        = crashFlashImg;
        uiManager.scoreText              = scoreTxt;
        uiManager.levelText              = levelTxt;
        uiManager.orderText              = orderTxt;
        uiManager.feedbackText           = feedTxt;
        uiManager.livesText              = livesTxt;
        uiManager.startScreen            = startGO;
        uiManager.gameOverScreen         = gameOverGO;
        uiManager.levelCompleteScreen    = levelCompleteGO;
        uiManager.victoryScreen          = victoryGO;
        uiManager.gameplayUI             = hudGO;
        uiManager.failTitleText          = failTitleTxt;
        uiManager.finalScoreText         = finalScoreTxt;
        uiManager.neededScoreText        = neededTxt;
        uiManager.levelCompleteScoreText = lcScoreTxt;
        uiManager.levelCompleteNextText  = lcNextTxt;
        uiManager.levelCompleteCountdownText = lcCountdownTxt;
        uiManager.victoryScoreText       = vicScoreTxt;
        uiManager.pauseScreen            = pauseGO;
        uiManager.selectModeFromPauseButton = selectModeBtn;
        uiManager.startButton            = startBtn;
        uiManager.retryButton            = retryBtn;
        uiManager.nextLevelButton        = nextLvlBtn;
        uiManager.playAgainButton        = playAgainBtn;
        uiManager.resumeButton           = resumeBtn;
        uiManager.restartButton          = restartBtn;
        uiManager.streakText             = streakTxt;
        uiManager.modeSelectScreen       = modeSelectGO;
        uiManager.modeBackToStartButton  = modeBackBtn;
        uiManager.rushModeButton         = rushBtn;
        uiManager.normalModeButton       = normalBtn;
        uiManager.endlessModeButton      = endlessBtn;
        uiManager.heartExtremeModeButton = heartExtremeBtn;
        uiManager.rushExtremeModeButton  = rushExtremeBtn;
        uiManager.endlessSummaryScreen   = endlessSummaryGO;
        uiManager.endlessTierText        = endlessTierTxt;
        uiManager.endlessScoreText       = endlessScTxt;
        uiManager.endlessDeliveriesText  = endlessDelivTxt;
        uiManager.endlessRetryButton     = endlessRetryBtn;
        uiManager.endlessSelectModeButton = endlessSelectBtn;
        uiManager.settingsScreen         = settingsGO;
        uiManager.volumeSlider           = volumeSlider;
        uiManager.bgmVolumeSlider        = bgmSliderGO.GetComponent<Slider>();
        uiManager.sfxVolumeSlider        = sfxSliderGO.GetComponent<Slider>();
        uiManager.zoneLabelOpacitySlider = zoneLabelSliderGO.GetComponent<Slider>();
        uiManager.bgmLabelText           = bgmLabelTxt;
        uiManager.sfxLabelText           = sfxLabelTxt;
        uiManager.zoneLabelOpacityLabel  = zoneLabelLabelTxt;
        uiManager.fullscreenToggleButton = fsBtn;
        uiManager.fullscreenLabelText    = fsTMP;
        uiManager.languageButtonText     = langBtnTxtTMP;
        uiManager.languageButton         = langBtnGO;
        uiManager.settingsButton         = settingsBtn;
        uiManager.startSettingsButton    = startSettingsBtn;
        uiManager.hudSettingsButton      = hudSettingsBtn;
        uiManager.closeSettingsButton    = closeSettingsBtnGO;

        // Localizable static texts
        uiManager.startTitleText       = startTitleTxt;
        uiManager.settingsTitleText    = settingsTitleTxt;
        uiManager.volumeLabelText      = volumeLabelTxt;
        uiManager.modeSelectTitleText  = modeSelectTitleTxt;
        uiManager.rushModeDescText     = rushDescTxt;
        uiManager.normalModeDescText   = normalDescTxt;
        uiManager.endlessModeDescText  = endlessDescTxt;
        uiManager.heartExtremeDescText = heartExtremeDescTxt;
        uiManager.rushExtremeDescText  = rushExtremeDescTxt;
        uiManager.standardHeaderText   = standardHeaderTxt;
        uiManager.extremeHeaderText    = extremeHeaderTxt;
        uiManager.pauseTitleText       = pauseTitleTxt;

        // Screen title texts (new fields)
        uiManager.startSubtitleText      = startSubtitleTxt;
        uiManager.levelCompleteTitleText = lcTitleTxt;
        uiManager.victoryTitleText       = vicTitleTxt;
        uiManager.victorySubtitleText    = vicSubtitleTxt;
        uiManager.endlessTitleText       = endlessTitleTxt;
        uiManager.pauseHintText          = pauseHintTxt;
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    static void ApplyButtonColor(Button btn, Color normal, Color highlighted, Color pressed)
    {
        btn.GetComponent<UnityEngine.UI.Image>().color = normal;
        var colors = btn.colors;
        colors.normalColor      = normal;
        colors.highlightedColor = highlighted;
        colors.pressedColor     = pressed;
        btn.colors = colors;
    }

    static GameObject CreateSprite(string name, Vector3 pos, Vector2 size, Color color,
                                   Sprite sprite, int order = 0, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        if (parent != null) go.transform.SetParent(parent, true);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color  = color;
        sr.sortingOrder = order;
        return go;
    }

    static void CreateObstacle(string name, Vector3 pos, Vector2 size, Sprite sprite, Transform parent = null)
    {
        var go = CreateSprite(name, pos, size, Color.white, sprite, order: 1, parent: parent);
        var sr = go.GetComponent<SpriteRenderer>();
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = false;
        // Match collider to actual sprite bounds (local space) so it fits the visual exactly
        col.size = sr.sprite != null ? (Vector2)sr.sprite.bounds.size : Vector2.one;
    }

    static void CreateWall(string name, Vector3 pos, Vector2 size, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        if (parent != null) go.transform.SetParent(parent, true);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.isTrigger = false;
    }

    static void CreateNPC(string name, Vector3 pos, Sprite sprite, bool moveHorizontal,
                          float rangeMin, float rangeMax, float speed, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        if (parent != null) go.transform.SetParent(parent, true);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.bodyType = RigidbodyType2D.Kinematic;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.4f, 0.8f);

        var sprGO = new GameObject("Sprite");
        sprGO.transform.SetParent(go.transform, false);
        sprGO.transform.localScale = new Vector3(2.295f, 2.295f, 1f);
        var sr = sprGO.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingOrder = 5;

        var npc = go.AddComponent<NPCCar>();
        npc.speed = speed;
        npc.rangeMin = rangeMin;
        npc.rangeMax = rangeMax;
        npc.moveHorizontal = moveHorizontal;
    }

    static void CreateBossNPC(string name, Vector3 pos, Sprite sprite, bool moveHorizontal,
                               float rangeMin, float rangeMax, float speed, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        if (parent != null) go.transform.SetParent(parent, true);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.bodyType = RigidbodyType2D.Kinematic;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.4f, 0.8f);

        var sprGO = new GameObject("Sprite");
        sprGO.transform.SetParent(go.transform, false);
        sprGO.transform.localScale = new Vector3(2.835f, 2.835f, 1f);
        var sr = sprGO.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingOrder = 6;

        var npc = go.AddComponent<NPCCar>();
        npc.speed = speed;
        npc.rangeMin = rangeMin;
        npc.rangeMax = rangeMax;
        npc.moveHorizontal = moveHorizontal;
        npc.isBoss = true;
    }

    static GameObject CreateZoneObject(string name, Vector3 pos, Vector2 size,
                                       Color color, Sprite sprite, Transform parent = null)
    {
        var go = CreateSprite(name, pos, size, color, sprite, order: 0, parent: parent);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one * 0.95f;
        return go;
    }

    // Uses legacy TextMesh — no TMP resources required, renders in world space directly.
    static void AddWorldLabel(GameObject parent, string text, Color textColor, string localizationKey = null)
    {
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(parent.transform, false);
        labelGO.transform.localPosition = new Vector3(0f, 0.62f, -0.05f);
        labelGO.transform.localScale    = Vector3.one * 0.39f;

        var tm = labelGO.AddComponent<TextMesh>();
        tm.text          = text;
        tm.fontSize      = 48;
        tm.color         = textColor;
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
        tm.characterSize = 0.12f;
        tm.fontStyle     = FontStyle.Bold;

        // Ensure mesh renders on top of sprites
        var mr = labelGO.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 25;

        if (!string.IsNullOrEmpty(localizationKey))
        {
            var localizer = labelGO.AddComponent<ZoneLabelLocalizer>();
            localizer.localizationKey = localizationKey;
        }
    }

    static Sprite GetOrCreateWhiteSprite()
    {
        const string spritePath = "Assets/Sprites/WhiteSquare.png";

        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");

        // 32×32 white texture
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        File.WriteAllBytes(Application.dataPath + "/Sprites/WhiteSquare.png", bytes);
        AssetDatabase.ImportAsset(spritePath);

        var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType     = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode      = FilterMode.Point;
            importer.mipmapEnabled   = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }

    // ── UI factory helpers ───────────────────────────────────────────────────

    static void StretchFull(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI MakeTMP(GameObject parent, string name, string text, float size,
        TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 sizeDelta, Vector2 anchoredPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text           = text;
        tmp.fontSize       = size;
        tmp.fontSizeMin    = Mathf.Max(8f, size * 0.4f);
        tmp.fontSizeMax    = size;
        tmp.enableAutoSizing = true;   // shrinks to fit container — stays sharp at any scale
        tmp.alignment      = align;
        tmp.color          = Color.white;
        tmp.overflowMode   = TextOverflowModes.Overflow;  // never clip labels

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        // Pivot: for a stretched axis use 0.5 (centre); for a pinned axis match the anchor corner.
        bool xs = !Mathf.Approximately(anchorMin.x, anchorMax.x);
        bool ys = !Mathf.Approximately(anchorMin.y, anchorMax.y);
        rt.pivot            = new Vector2(xs ? 0.5f : anchorMin.x, ys ? 0.5f : anchorMin.y);
        rt.sizeDelta        = sizeDelta;
        rt.anchoredPosition = anchoredPos;
        return tmp;
    }

    static GameObject BuildSettingsSliderInSetup(GameObject panel, string name, float y)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(panel.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.5f);
        rt.anchorMax = new Vector2(0.9f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0, 40);
        rt.anchoredPosition = new Vector2(0, y);

        var bgGO = new GameObject("Background", typeof(RectTransform));
        bgGO.transform.SetParent(go.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.25f, 0.28f, 0.35f);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.25f); bgRT.anchorMax = new Vector2(1, 0.75f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(go.transform, false);
        var fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0.25f); fillAreaRT.anchorMax = new Vector2(1, 0.75f);
        fillAreaRT.offsetMin = new Vector2(5, 0); fillAreaRT.offsetMax = new Vector2(-15, 0);

        var fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0f, 0.75f, 0.85f);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = new Vector2(0, 1);
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        var handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaGO.transform.SetParent(go.transform, false);
        var handleAreaRT = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero; handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(10, 0); handleAreaRT.offsetMax = new Vector2(-10, 0);

        var handleGO = new GameObject("Handle", typeof(RectTransform));
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;
        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = Vector2.zero; handleRT.anchorMax = new Vector2(0, 1);
        handleRT.sizeDelta = new Vector2(20, 0);

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
        return go;
    }

    static Button MakeButton(GameObject parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 1f);

        var btn = go.AddComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor      = new Color(0.2f, 0.6f, 1f);
        cb.highlightedColor = new Color(0.3f, 0.75f, 1f);
        cb.pressedColor     = new Color(0.1f, 0.45f, 0.85f);
        btn.colors = cb;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = sizeDelta;
        rt.anchoredPosition = anchoredPos;

        // Label text — stretches to fill button, auto-sizes for clean rendering at any resolution
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text             = label;
        tmp.fontSize         = 28;
        tmp.fontSizeMin      = 10;
        tmp.fontSizeMax      = 28;
        tmp.enableAutoSizing = true;
        tmp.fontStyle        = FontStyles.Bold;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.color            = Color.white;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        var trt = tmp.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8, 4);
        trt.offsetMax = new Vector2(-8, -4);

        return btn;
    }

    static GameObject MakeFullScreenPanel(GameObject canvasParent, string name, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(canvasParent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        StretchFull(go);
        return go;
    }

    // ── pixel art sprite factory ──────────────────────────────────────────────

    static void FillRect(Color[] px, int w, int x0, int y0, int x1, int y1, Color c)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (x >= 0 && x < w && y >= 0 && y < w)
                    px[y * w + x] = c;
    }

    static Sprite SaveSprite(string assetPath, Color[] pixels, int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();
        byte[] bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");

        File.WriteAllBytes(Application.dataPath + "/Sprites/" + Path.GetFileName(assetPath), bytes);
        AssetDatabase.ImportAsset(assetPath);

        var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType      = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode       = FilterMode.Point;
            imp.mipmapEnabled    = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static Sprite GetOrBuildSprite(string path, System.Action<Color[], int> draw)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;
        const int N = 32;
        var px = new Color[N * N]; // transparent by default
        draw(px, N);
        return SaveSprite(path, px, N);
    }

    // Solid white circle — used for joystick handle
    static Sprite BuildCircleSprite() => GetOrBuildSprite("Assets/Sprites/spr_circle.png", (px, S) =>
    {
        float cx = S * 0.5f, cy = S * 0.5f, r = S * 0.48f;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x - cx + 0.5f, dy = y - cy + 0.5f;
                if (dx * dx + dy * dy < r * r)
                    px[y * S + x] = Color.white;
            }
    });

    // Ring sprite for joystick background: filled circle with brighter outer rim
    static Sprite BuildJoystickRingSprite() => GetOrBuildSprite("Assets/Sprites/spr_joy_ring.png", (px, S) =>
    {
        float cx = S * 0.5f, cy = S * 0.5f;
        float rOuter = S * 0.48f;
        float rInner = S * 0.30f;   // softer fill inside, solid ring outside
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x - cx + 0.5f, dy = y - cy + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > rOuter) continue;
                // Rim: full white; interior: slightly transparent white for depth
                float alpha = d > rInner ? 1.0f : 0.55f;
                px[y * S + x] = new Color(1f, 1f, 1f, alpha);
            }
    });

    // Top-down car: white body (tinted blue externally), dark wheels/windows, yellow headlights, red tail lights
    // ── Player car: sporty top-down view, white body tinted by SpriteRenderer ──
    static Sprite BuildCarSprite()
    {
        const int S = 32;
        var px = new Color[S * S];

        Color body    = Color.white;
        Color dark    = new Color(0.10f, 0.10f, 0.12f);
        Color hood    = new Color(0.87f, 0.87f, 0.90f);
        Color roof    = new Color(0.42f, 0.62f, 0.93f);
        Color roofHi  = new Color(0.74f, 0.87f, 1.00f);
        Color yel     = new Color(1f, 0.95f, 0.22f);
        Color red     = new Color(0.95f, 0.12f, 0.08f);
        Color mirror  = new Color(0.62f, 0.62f, 0.65f);
        Color seam    = new Color(0.72f, 0.72f, 0.76f);
        Color exhaust = new Color(0.28f, 0.28f, 0.30f);

        FillRect(px, S, 5,  2, 26, 29, body);         // body
        FillRect(px, S, 5, 22, 26, 29, hood);          // hood (front section)
        FillRect(px, S, 5, 21, 26, 22, seam);          // hood/cabin seam line
        FillRect(px, S, 1,  5,  5, 26, dark);          // left wheel arch
        FillRect(px, S, 26, 5, 30, 26, dark);          // right wheel arch
        FillRect(px, S, 7, 23, 14, 28, dark);          // front windshield L
        FillRect(px, S, 17,23, 24, 28, dark);          // front windshield R
        FillRect(px, S, 15,23, 16, 28, seam);          // A-pillar
        FillRect(px, S, 7,  4, 14,  9, dark);          // rear windshield L
        FillRect(px, S, 17, 4, 24,  9, dark);          // rear windshield R
        FillRect(px, S, 15, 4, 16,  9, seam);          // rear pillar
        FillRect(px, S, 10, 9, 21, 21, roof);          // roof glass
        FillRect(px, S, 11,17, 15, 20, roofHi);        // glass glare highlight
        FillRect(px, S, 15, 9, 16, 21, seam);          // door seam
        FillRect(px, S, 6, 28, 10, 30, yel);           // headlight L
        FillRect(px, S, 21,28, 25, 30, yel);           // headlight R
        FillRect(px, S, 6, 26,  9, 27, new Color(1f, 1f, 0.65f)); // DRL L
        FillRect(px, S, 22,26, 25, 27, new Color(1f, 1f, 0.65f)); // DRL R
        FillRect(px, S, 6,  1, 10,  3, red);           // tail light L
        FillRect(px, S, 21, 1, 25,  3, red);           // tail light R
        FillRect(px, S, 2, 14,  4, 17, mirror);        // left mirror
        FillRect(px, S, 27,14, 29, 17, mirror);        // right mirror
        FillRect(px, S, 5,  2,  8,  5, exhaust);       // exhaust L
        FillRect(px, S, 23, 2, 26,  5, exhaust);       // exhaust R

        return SaveSprite("Assets/Sprites/spr_car.png", px, S);
    }

    // ── NPC car: blockier van shape, distinct from player ─────────────────────
    static Sprite BuildNPCCarSprite() => GetOrBuildSprite("Assets/Sprites/spr_npc_car.png", (px, S) =>
    {
        Color body   = Color.white;
        Color dark   = new Color(0.10f, 0.10f, 0.12f);
        Color cabin  = new Color(0.22f, 0.22f, 0.25f);   // flat dark roof (no glass)
        Color stripe = new Color(1f, 0.85f, 0.10f);       // yellow stripe
        Color yel    = new Color(1f, 0.95f, 0.22f);
        Color red    = new Color(0.95f, 0.12f, 0.08f);

        FillRect(px, S, 4,  2, 27, 29, body);   // wider body
        FillRect(px, S, 0,  4,  4, 27, dark);   // left wheel arch (wider)
        FillRect(px, S, 27, 4, 31, 27, dark);   // right wheel arch
        FillRect(px, S, 7, 23, 24, 28, dark);   // front bumper area
        FillRect(px, S, 7,  3, 24,  8, dark);   // rear bumper area
        FillRect(px, S, 9,  9, 22, 21, cabin);  // flat dark cab roof
        FillRect(px, S, 14, 2, 17, 29, stripe); // center yellow stripe
        FillRect(px, S, 6, 28, 12, 30, yel);    // headlight L (wider)
        FillRect(px, S, 19,28, 25, 30, yel);    // headlight R
        FillRect(px, S, 6,  1, 12,  3, red);    // tail light L
        FillRect(px, S, 19, 1, 25,  3, red);    // tail light R
    });

    // ── Boss car: aggressive low-profile racer ─────────────────────────────────
    static Sprite BuildBossCarSprite() => GetOrBuildSprite("Assets/Sprites/spr_boss_car.png", (px, S) =>
    {
        Color body    = Color.white;
        Color dark    = new Color(0.08f, 0.08f, 0.10f);
        Color cockpit = new Color(0.15f, 0.15f, 0.18f);  // tiny deep cockpit
        Color grille  = new Color(0.12f, 0.12f, 0.14f);
        Color exhaust = new Color(0.20f, 0.18f, 0.18f);
        Color yel     = new Color(1f, 0.95f, 0.20f);
        Color red     = new Color(0.95f, 0.10f, 0.06f);
        Color accent  = new Color(0.85f, 0.05f, 0.05f);  // red accent stripe

        FillRect(px, S, 3,  1, 28, 30, body);    // wide low body
        FillRect(px, S, 0,  3,  3, 28, dark);    // left wheel arch
        FillRect(px, S, 28, 3, 31, 28, dark);    // right wheel arch
        FillRect(px, S, 3,  0, 28,  2, dark);    // rear spoiler bar
        FillRect(px, S, 3, 29, 28, 31, grille);  // front grille
        // Grille slats
        FillRect(px, S, 6, 29,  9, 30, dark);
        FillRect(px, S, 11,29, 14, 30, dark);
        FillRect(px, S, 17,29, 20, 30, dark);
        FillRect(px, S, 22,29, 25, 30, dark);
        // Cockpit (small, central)
        FillRect(px, S, 11,11, 20, 20, cockpit);
        // Red accent racing stripe
        FillRect(px, S, 14, 1, 17, 30, accent);
        // Dual exhaust pipes
        FillRect(px, S, 3,  1,  7,  4, exhaust);
        FillRect(px, S, 24, 1, 28,  4, exhaust);
        // Headlights (aggressive narrow)
        FillRect(px, S, 5, 28, 11, 30, yel);
        FillRect(px, S, 20,28, 26, 30, yel);
        // Tail lights
        FillRect(px, S, 4,  1, 10,  3, red);
        FillRect(px, S, 21, 1, 27,  3, red);
        // Side vents (visual detail)
        FillRect(px, S, 3,  8,  5, 12, dark);
        FillRect(px, S, 26, 8, 28, 12, dark);
        FillRect(px, S, 3, 18,  5, 22, dark);
        FillRect(px, S, 26,18, 28, 22, dark);
    });

    // Top-down building: zone-colored walls, detailed windows, door step
    static Sprite BuildBuildingSprite() => GetOrBuildSprite("Assets/Sprites/spr_building.png", (px, S) =>
    {
        Color shadow = new Color(0.30f, 0.30f, 0.30f);
        Color wall   = new Color(0.50f, 0.50f, 0.50f);
        Color roof   = new Color(0.95f, 0.95f, 0.95f);
        Color ridge  = new Color(0.72f, 0.72f, 0.72f);
        Color window = new Color(0.14f, 0.14f, 0.20f);
        Color winLit = new Color(0.85f, 0.80f, 0.42f);  // warm light inside window
        Color door   = new Color(0.24f, 0.14f, 0.06f);
        Color step   = new Color(0.68f, 0.68f, 0.68f);
        Color chimney = new Color(0.35f, 0.22f, 0.14f);

        FillRect(px, S, 0, 0, 31, 31, shadow);      // outer shadow border
        FillRect(px, S, 1, 1, 30, 30, wall);        // wall (receives zone tint)
        FillRect(px, S, 3, 3, 28, 28, roof);        // roof surface
        FillRect(px, S, 3, 15, 28, 16, ridge);      // horizontal ridge
        FillRect(px, S, 15, 3, 16, 28, ridge);      // vertical ridge
        // Windows — 6x6 with inner lit pixel
        FillRect(px, S, 5, 19, 11, 25, window);     // TL window
        FillRect(px, S, 6, 20,  9, 24, winLit);
        FillRect(px, S, 20,19, 26, 25, window);     // TR window
        FillRect(px, S, 21,20, 24, 24, winLit);
        FillRect(px, S, 5,  6, 11, 12, window);     // BL window
        FillRect(px, S, 6,  7,  9, 11, winLit);
        FillRect(px, S, 20, 6, 26, 12, window);     // BR window
        FillRect(px, S, 21, 7, 24, 11, winLit);
        // Door + step
        FillRect(px, S, 13, 3, 18,  8, door);
        FillRect(px, S, 12, 3, 19,  4, step);       // door step
        // Chimney
        FillRect(px, S, 22,24, 26, 28, chimney);
    });

    // Package box: cardboard brown, tape cross, red bow
    static Sprite BuildPackageSprite() => GetOrBuildSprite("Assets/Sprites/spr_package.png", (px, S) =>
    {
        Color box  = new Color(0.72f, 0.52f, 0.22f);
        Color edge = new Color(0.38f, 0.24f, 0.06f);
        Color tape = new Color(0.88f, 0.72f, 0.30f);
        Color bow  = new Color(1f, 0.28f, 0.18f);

        FillRect(px, S, 0, 0, 31, 31, box);
        FillRect(px, S, 0, 0,  31, 1,  edge);       // bottom edge
        FillRect(px, S, 0, 30, 31, 31, edge);       // top edge
        FillRect(px, S, 0, 0,  1,  31, edge);       // left edge
        FillRect(px, S, 30, 0, 31, 31, edge);       // right edge
        FillRect(px, S, 0, 14, 31, 17, tape);       // tape horizontal
        FillRect(px, S, 14, 0, 17, 31, tape);       // tape vertical
        FillRect(px, S, 12, 12, 19, 19, bow);       // bow/label center
    });

    // Top-down tree: circular canopy with trunk centre
    static Sprite BuildTreeSprite() => GetOrBuildSprite("Assets/Sprites/spr_tree.png", (px, S) =>
    {
        Color dark  = new Color(0.08f, 0.26f, 0.06f);
        Color mid   = new Color(0.20f, 0.50f, 0.12f);
        Color light = new Color(0.36f, 0.70f, 0.20f);
        Color trunk = new Color(0.32f, 0.20f, 0.07f);
        float cx = S * 0.5f, cy = S * 0.5f, r = S * 0.46f;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x - cx + 0.5f, dy = y - cy + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if      (d < r * 0.22f) px[y * S + x] = trunk;
                else if (d < r * 0.55f) px[y * S + x] = light;
                else if (d < r * 0.82f) px[y * S + x] = mid;
                else if (d < r)         px[y * S + x] = dark;
            }
    });

    // Obstacle building: multi-story office block with rooftop details
    static Sprite BuildObstacleSprite() => GetOrBuildSprite("Assets/Sprites/spr_obstacle.png", (px, S) =>
    {
        Color edge    = new Color(0.18f, 0.16f, 0.15f);
        Color wall    = new Color(0.40f, 0.36f, 0.33f);
        Color face    = new Color(0.56f, 0.52f, 0.48f);
        Color parapet = new Color(0.30f, 0.27f, 0.25f);
        Color window  = new Color(0.12f, 0.12f, 0.16f);
        Color winLit  = new Color(0.75f, 0.82f, 0.48f);  // green-yellow office light
        Color ac      = new Color(0.28f, 0.32f, 0.35f);  // AC unit
        Color tower   = new Color(0.22f, 0.20f, 0.18f);  // water tower
        Color antenna = new Color(0.20f, 0.20f, 0.22f);

        FillRect(px, S, 0, 0, 31, 31, edge);       // shadow border
        FillRect(px, S, 1, 1, 30, 30, wall);       // building mass
        FillRect(px, S, 2, 2, 29, 29, face);       // rooftop face
        FillRect(px, S, 2, 2, 29,  3, parapet);    // parapet S
        FillRect(px, S, 2,28, 29, 29, parapet);    // parapet N
        FillRect(px, S, 2, 2,  3, 29, parapet);    // parapet W
        FillRect(px, S, 28,2, 29, 29, parapet);    // parapet E
        // Floor band lines (suggest multiple floors)
        FillRect(px, S, 3, 10, 28, 11, parapet);
        FillRect(px, S, 3, 19, 28, 20, parapet);
        // Windows — 3 columns × 2 rows per floor band
        FillRect(px, S, 4,  21, 9,  27, window); FillRect(px, S, 5, 22, 8, 26, winLit);
        FillRect(px, S, 13, 21,18,  27, window); FillRect(px, S,14, 22,17, 26, winLit);
        FillRect(px, S, 22, 21,27,  27, window); FillRect(px, S,23, 22,26, 26, winLit);
        FillRect(px, S, 4,  12, 9,  18, window); FillRect(px, S, 5, 13, 8, 17, winLit);
        FillRect(px, S, 13, 12,18,  18, window); FillRect(px, S,14, 13,17, 17, winLit);
        FillRect(px, S, 22, 12,27,  18, window); FillRect(px, S,23, 13,26, 17, winLit);
        // AC units (rooftop)
        FillRect(px, S, 11, 23, 16, 27, ac);
        FillRect(px, S, 18, 23, 23, 26, ac);
        // Water tower (NW corner)
        FillRect(px, S, 4, 23,  8, 27, tower);
        FillRect(px, S, 5, 27,  7, 28, tower);   // legs
        // Antenna
        FillRect(px, S, 15,27, 16, 29, antenna);
    });

    // ── Thai font helper ─────────────────────────────────────────────────────

    static void EnsureThaiFont()
    {
        const string thaiAssetPath = "Assets/Fonts/ThaiFont.asset";
        const string ttfDstPath    = "Assets/Fonts/ThaiSrc.ttf";

        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(thaiAssetPath);
        if (existing != null) { AddThaiAsFallback(existing); return; }

        if (!AssetDatabase.IsValidFolder("Assets/Fonts"))
            AssetDatabase.CreateFolder("Assets", "Fonts");

        // 1. User-supplied NotoSansThai in Assets/Fonts/ (preferred)
        // 2. macOS system Thonburi (fallback)
        string srcPath = null;
        const string notoInProject = "Assets/Fonts/NotoSansThai-Regular.ttf";
        if (File.Exists(Application.dataPath + "/Fonts/NotoSansThai-Regular.ttf"))
        {
            // Already in project — import it directly as the source TTF
            AssetDatabase.ImportAsset(notoInProject, ImportAssetOptions.ForceSynchronousImport);
            var notoFont = AssetDatabase.LoadAssetAtPath<Font>(notoInProject);
            if (notoFont != null)
            {
                BuildAndSaveThaiAsset(notoFont, thaiAssetPath);
                return;
            }
        }

        foreach (var candidate in new[] {
            "/System/Library/Fonts/Supplemental/Thonburi.ttf",
            "/System/Library/Fonts/Thonburi.ttf"
        })
            if (File.Exists(candidate)) { srcPath = candidate; break; }

        if (srcPath == null)
        {
            Debug.LogWarning("[Delivery Dash] Thai font not found.\n" +
                "Place NotoSansThai-Regular.ttf in Assets/Fonts/ and re-run Setup Scene.");
            return;
        }

        File.Copy(srcPath, Application.dataPath + "/Fonts/ThaiSrc.ttf", true);
        AssetDatabase.ImportAsset(ttfDstPath, ImportAssetOptions.ForceSynchronousImport);

        var font = AssetDatabase.LoadAssetAtPath<Font>(ttfDstPath);
        if (font == null) { Debug.LogWarning("[Delivery Dash] Failed to load ThaiSrc.ttf after import."); return; }

        BuildAndSaveThaiAsset(font, thaiAssetPath);
    }

    static void BuildAndSaveThaiAsset(Font font, string thaiAssetPath)
    {
        var tmpFont = TMP_FontAsset.CreateFontAsset(font);
        tmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        tmpFont.name = "ThaiFont";

        // Pre-populate entire Thai Unicode block (U+0E00–U+0E7F = 128 code points)
        var thaiRange = new uint[128];
        for (int i = 0; i < 128; i++) thaiRange[i] = (uint)(0x0E00 + i);
        tmpFont.TryAddCharacters(thaiRange);

        // Save font asset with its sub-assets (material + atlas texture)
        AssetDatabase.CreateAsset(tmpFont, thaiAssetPath);
        if (tmpFont.material != null)
        {
            tmpFont.material.hideFlags = HideFlags.None;
            AssetDatabase.AddObjectToAsset(tmpFont.material, tmpFont);
        }
        if (tmpFont.atlasTextures != null)
            foreach (var tex in tmpFont.atlasTextures)
                if (tex != null) { tex.hideFlags = HideFlags.None; AssetDatabase.AddObjectToAsset(tex, tmpFont); }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AddThaiAsFallback(tmpFont);
        Debug.Log($"[Delivery Dash] ThaiFont.asset created from '{font.name}' with Thai block pre-populated.");
    }

    static void AddThaiAsFallback(TMP_FontAsset thaiFont)
    {
        string[] guids = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset");
        foreach (var guid in guids)
        {
            string path  = AssetDatabase.GUIDToAssetPath(guid);
            var    asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (asset == null) continue;
            if (asset.fallbackFontAssetTable == null)
                asset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            if (!asset.fallbackFontAssetTable.Contains(thaiFont))
            {
                asset.fallbackFontAssetTable.Add(thaiFont);
                EditorUtility.SetDirty(asset);
            }
        }
        AssetDatabase.SaveAssets();
    }

    static void ConfigureSpriteImport(string path, float ppu, FilterMode filter = FilterMode.Bilinear)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = filter;
        imp.mipmapEnabled = false;
        imp.SaveAndReimport();
    }

    // ── Bakery UI spritesheet import & slicing ──────────────────────────────
    const string BakerySheetPath = "Assets/Sprites/UI/UI_game.png";
    const int SheetH = 288; // spritesheet height in pixels

    static void ImportBakerySprites()
    {
        var imp = AssetImporter.GetAtPath(BakerySheetPath) as TextureImporter;
        if (imp == null) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = 16;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;

        // y_unity = SheetH - y_topLeft - height
        var metas = new List<SpriteMetaData>();
        void Add(string n, int x, int y, int w, int h, Vector4 border = default)
        {
            metas.Add(new SpriteMetaData
            {
                name = n,
                rect = new Rect(x, SheetH - y - h, w, h),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = border
            });
        }

        var panelBorder = new Vector4(14, 8, 8, 12); // left, bottom, right, top
        var btnBorder   = new Vector4(6, 6, 6, 6);

        // Large panels
        Add("Panel_Large_Left",  2, 2, 110, 110, panelBorder);
        Add("Panel_Large_Right", 118, 2, 110, 110, panelBorder);
        Add("Panel_Vertical",    234, 2, 80, 130, panelBorder);

        // Buttons
        Add("Btn_Start_Large", 2, 118, 55, 24, btnBorder);
        Add("Btn_Start_Small", 62, 118, 45, 22, btnBorder);
        Add("Btn_Save_Large",  2, 146, 55, 24, btnBorder);
        Add("Btn_Save_Small",  62, 146, 45, 22, btnBorder);
        Add("Btn_Exit_Large",  2, 174, 55, 24, btnBorder);
        Add("Btn_Exit_Small",  62, 174, 45, 22, btnBorder);

        // Small tile panels
        Add("Panel_Small_A", 118, 118, 30, 30, btnBorder);
        Add("Panel_Small_B", 152, 118, 30, 30, btnBorder);

        // Icon grid (20x20 each)
        int iconY0 = 118, iconX0 = 186, iconS = 20, iconGap = 4;
        string[,] iconNames =
        {
            { "Icon_Play", "Icon_Pause", "Icon_Forward", "Icon_Rewind" },
            { "Icon_ArrowUp", "Icon_ArrowDown", "Icon_ArrowLeft", "Icon_ArrowRight" },
            { "Icon_Gear", "Icon_Question", "Icon_Star", "Icon_Dollar" },
            { "Icon_Check", "Icon_X", "Icon_Home", "Icon_Heart" }
        };
        for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
                Add(iconNames[row, col],
                    iconX0 + col * (iconS + iconGap),
                    iconY0 + row * (iconS + iconGap),
                    iconS, iconS);

        // Decorations
        Add("Deco_Croissant",   180, 210, 60, 50);
        Add("Deco_RollingPin",  300, 90, 55, 18);

        // Small decorative elements
        Add("Deco_Dot1", 2, 210, 16, 16);
        Add("Deco_Dot2", 22, 210, 16, 16);
        Add("Deco_Star1", 42, 210, 16, 16);
        Add("Deco_Coin1", 62, 210, 16, 16);

        imp.spritesheet = metas.ToArray();
        imp.SaveAndReimport();
    }

    static Sprite LoadBakerySprite(string name)
    {
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(BakerySheetPath);
        foreach (var asset in allAssets)
        {
            var spr = asset as Sprite;
            if (spr != null && spr.name == name) return spr;
        }
        return null;
    }

    // Bakery warm color palette
    static readonly Color BakeryDarkBrown    = new Color(0.45f, 0.28f, 0.12f);
    static readonly Color BakeryTan          = new Color(0.55f, 0.40f, 0.25f);
    static readonly Color BakeryCream        = new Color(0.95f, 0.90f, 0.80f);
    static readonly Color BakeryBtnNormal    = new Color(0.55f, 0.35f, 0.15f);
    static readonly Color BakeryBtnHighlight = new Color(0.68f, 0.45f, 0.20f);
    static readonly Color BakeryBtnPressed   = new Color(0.40f, 0.25f, 0.10f);
    static readonly Color BakeryWarmOverlay  = new Color(0.12f, 0.08f, 0.04f, 0.93f);
    static readonly Color BakeryPanelWhite   = new Color(1f, 1f, 1f, 1f);

    static void ApplyBakeryButtonColor(Button btn)
    {
        ApplyButtonColor(btn, BakeryBtnNormal, BakeryBtnHighlight, BakeryBtnPressed);
    }

    static void ApplyBakeryButtonSprite(Button btn, Sprite spr)
    {
        if (btn == null || spr == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = spr;
            img.type = Image.Type.Sliced;
        }
    }

    static void ApplyBakeryButtonText(Button btn, Color textColor)
    {
        if (btn == null) return;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.color = textColor;
    }

    static void ImportTMPEssentials()
    {
        // Check if TMP settings asset already exists (resources already imported)
        if (AssetDatabase.FindAssets("TMP Settings").Length > 0) return;

        // Try to import from com.unity.ugui package (Unity 6)
        string[] candidates =
        {
            "Packages/com.unity.ugui/PackageResources/TMP Essential Resources.unitypackage",
            "Packages/com.unity.textmeshpro/PackageResources/TMP Essential Resources.unitypackage",
        };
        foreach (string path in candidates)
        {
            if (System.IO.File.Exists(path))
            {
                AssetDatabase.ImportPackage(path, false);
                Debug.Log("[Delivery Dash] TMP Essential Resources imported.");
                return;
            }
        }
        Debug.LogWarning("[Delivery Dash] TMP Essential Resources not found automatically.\n" +
            "Please go to Window > TextMeshPro > Import TMP Essential Resources.");
    }
}
