using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState { StartScreen, CarSelect, Playing, GameOver }
public enum GameMode  { Heart, Rush, Endless, HeartExtreme, RushExtreme }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public UIManager uiManager;
    public PlayerController player;

    [Header("Car Sprites")]
    public Sprite[] normalCarSprites;
    public Sprite[] bossCarSprites;

    [Header("Car System")]
    public CarData[] carCatalog;
    public CarData selectedCar;
    public static event System.Action OnCarChanged;

    public GameState State       { get; private set; } = GameState.StartScreen;
    public GameMode  CurrentMode { get; private set; } = GameMode.Heart;
    public int   Score           { get; private set; }
    public int   PassScore       { get; private set; }
    public float TimeRemaining   { get; private set; }
    public int   Lives           { get; private set; }
    public int   MaxLives        { get; private set; }
    public string CurrentDestination { get; private set; }
    public bool   IsRushOrder        => isRushOrder;
    public int CurrentLevel => CurrentMode == GameMode.Endless ? endlessTier + 1 : currentLevel + 1;

    public bool IsHeartMode => CurrentMode == GameMode.Heart || CurrentMode == GameMode.HeartExtreme;
    public bool IsRushMode  => CurrentMode == GameMode.Rush   || CurrentMode == GameMode.RushExtreme;
    public bool IsExtreme   => CurrentMode == GameMode.HeartExtreme || CurrentMode == GameMode.RushExtreme;
    public float NPCSpeedMultiplier => IsExtreme ? 1.5f : 1f;

    public int Coins => carSystem.Coins;

    static readonly string[] Destinations = { "house_a", "house_b", "shop", "cafe" };

    // Delegated systems
    CarSystem carSystem;

    // ── Forwarding methods for compatibility ────────────────────────────────

    public static int GetUnlockedLevel(GameMode mode) => LevelData.GetUnlockedLevel(mode);
    public static int GetBestScore(GameMode mode) => LevelData.GetBestScore(mode);
    public static int GetBestEndlessTier() => LevelData.GetBestEndlessTier();
    public static int GetEndlessTier10Count() => LevelData.GetEndlessTier10Count();

    public bool IsCarUnlocked(CarData car) => carSystem.IsCarUnlocked(car);
    public bool CanUnlockCar(CarData car) => carSystem.CanUnlockCar(car);
    public string GetUnlockRequirementText(CarData car) => carSystem.GetUnlockRequirementText(car);

    public void AddCoins(int amount) => carSystem.AddCoins(amount);

    public bool TryUnlockCar(CarData car) => carSystem.TryUnlockCar(car);

    public void SelectCar(CarData car)
    {
        selectedCar = carSystem.SelectCar(car, selectedCar, () => OnCarChanged?.Invoke());
    }

    public void UnlockAllCars() => carSystem.UnlockAllCars();

    public void ResetAllCars()
    {
        var defaultCar = carSystem.ResetAllCars();
        if (defaultCar != null) SelectCar(defaultCar);
    }

    public void LoadSelectedCar()
    {
        selectedCar = carSystem.LoadSelectedCar();
    }

    public void ShowCarSelect()
    {
        State = GameState.CarSelect;
        uiManager.ShowCarSelectScreen();
    }

    public bool TimerFrozen;

    int currentLevel;
    int deliveryStreak;
    int deliveryCount;
    bool isRushOrder;
    bool lowTimePlayed;
    bool isPaused;
    bool waitingForRushAdvance;
    DeliveryZone[] deliveryZones;
    ZoneHighlight[] zoneHighlights;
    NPCCar[] npcPool;
    NPCCar[] bossPool;

    int endlessTier;
    int endlessTierProgress;
    float endlessDeliveryBonus;

    Camera mainCamera;
    float lastHitTime = -10f;
    float HitImmunityDuration => selectedCar != null ? selectedCar.durabilitySeconds : 2.0f;
    const float RushWrongPenalty = 2f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Application.targetFrameRate = -1;
        carSystem = new CarSystem(carCatalog);
    }

    void Start()
    {
        FirestoreLeaderboard.Prefetch();
        CacheZonesAndNPCs();
        currentLevel = 0;
        LoadSelectedCar();
        State = GameState.StartScreen;
        uiManager.ShowStartScreen();
    }

    void CacheZonesAndNPCs()
    {
        deliveryZones = Object.FindObjectsByType<DeliveryZone>(FindObjectsSortMode.None);
        zoneHighlights = new ZoneHighlight[deliveryZones.Length];
        for (int i = 0; i < deliveryZones.Length; i++)
            zoneHighlights[i] = deliveryZones[i].GetComponent<ZoneHighlight>();

        var all = Object.FindObjectsByType<NPCCar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var normals = new List<NPCCar>();
        var bosses  = new List<NPCCar>();
        foreach (var n in all)
            (n.isBoss ? bosses : normals).Add(n);
        npcPool  = normals.ToArray();
        bossPool = bosses.ToArray();
        foreach (var n in all) n.gameObject.SetActive(false);
    }

    void Update()
    {
        HandlePauseInput();

        if (State != GameState.Playing || isPaused) return;

        if (!TimerFrozen)
            TimeRemaining -= Time.deltaTime;
        uiManager.UpdateTimer(TimeRemaining);

        if (!lowTimePlayed && TimeRemaining <= 10f)
        {
            lowTimePlayed = true;
            AudioManager.Play(a => a.PlayTimerWarn());
        }

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            EndGame();
        }
    }

    void HandlePauseInput()
    {
        bool escPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool startPressed = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;

        if (escPressed || startPressed)
        {
            if (isPaused) ResumeGame();
            else if (State == GameState.Playing) PauseGame();
        }
    }

    // ── Game start ──────────────────────────────────────────────────────────

    public void StartWithMode(GameMode mode)
    {
        CurrentMode = mode;
        if (mode == GameMode.Endless)
        {
            currentLevel = 0;
            StartGame();
        }
        else
            uiManager.ShowLevelSelectScreen(mode);
    }

    public GameMode BaseMode => CurrentMode == GameMode.HeartExtreme ? GameMode.Heart
                              : CurrentMode == GameMode.RushExtreme  ? GameMode.Rush
                              : CurrentMode;

    public void StartGame()
    {
        StopAllCoroutines();
        Score = 0;
        deliveryStreak = 0;
        deliveryCount = 0;
        isRushOrder = false;
        lowTimePlayed = false;
        isPaused = false;
        TimerFrozen = false;
        lastHitTime = -10f;

        var level = LevelData.Levels[currentLevel];
        int carHearts = (selectedCar != null) ? selectedCar.bonusHearts : 0;
        MaxLives = IsHeartMode ? level.lives + carHearts : 0;
        Lives = MaxLives;
        Time.timeScale = 1f;
        State = GameState.Playing;
        if (PowerUpManager.Instance != null) PowerUpManager.Instance.OnGameStateChanged(GameState.Playing);

        if (CurrentMode == GameMode.Endless)
        {
            endlessTier = 0;
            endlessTierProgress = 0;
            endlessDeliveryBonus = 4f;
            TimeRemaining = LevelData.Levels[0].time;
            PassScore = 0;
        }
        else
        {
            TimeRemaining = level.time;
            PassScore = level.scoreNeeded;
        }

        player.ResetPlayer();
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            var follow = mainCamera.GetComponent<CameraFollow>();
            if (follow != null) follow.SnapToTarget();
        }

        ActivateNPCsForLevel(CurrentMode == GameMode.Endless ? 0 : currentLevel);
        UpdateNPCSprites();
        GenerateNewOrder();
        uiManager.ShowGameplayUI();
        uiManager.ShowStreak(0);
        uiManager.UpdateScore(Score, PassScore);
        if (IsHeartMode) uiManager.UpdateLives(Lives);
        UpdateLevelDisplay();
        AudioManager.Play(a => a.PlayBGM());
        if (currentLevel == 0)
        {
            if (PlayerPrefs.GetInt("TutorialDone", 0) == 0)
                uiManager.StartInteractiveTutorial();
            else
                uiManager.ShowTutorialHints();
        }
    }

    public void NextLevel()
    {
        RecordLeaderboardEntry();
        currentLevel++;
        StartGame();
    }

    public void StartAtLevel(int levelIndex)
    {
        currentLevel = levelIndex;
        StartGame();
    }

    // ── Pause ───────────────────────────────────────────────────────────────

    public void PauseGame()
    {
        if (State != GameState.Playing || isPaused) return;
        isPaused = true;
        Time.timeScale = 0f;
        AudioManager.Play(a => a.PauseBGM());
        uiManager.ShowPauseScreen();
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;
        AudioManager.Play(a => a.ResumeBGM());
        uiManager.HidePauseScreen();
    }

    // ── End / delivery ──────────────────────────────────────────────────────

    void EndGame()
    {
        State = GameState.GameOver;
        isPaused = false;
        TimerFrozen = false;
        Time.timeScale = 1f;
        CurrentDestination = "";
        UpdateZoneHighlights();
        DeactivateAllNPCs();
        if (PowerUpManager.Instance != null) PowerUpManager.Instance.OnGameStateChanged(GameState.GameOver);
        AudioManager.Play(a => { a.StopBGM(); a.PlayGameOver(); });

        RecordLeaderboardEntry();

        if (CurrentMode == GameMode.Endless)
        {
            LevelData.SaveBestScore(GameMode.Endless, Score);
            LevelData.SaveBestEndlessTier(endlessTier + 1);
            FirestoreUserProfile.QueueSave(LevelData.CloudFields(GameMode.Endless));
            uiManager.ShowEndlessSummary(Score, endlessTier + 1, deliveryCount,
                LevelData.GetBestScore(GameMode.Endless), LevelData.GetBestEndlessTier());
            return;
        }

        int needed = LevelData.Levels[currentLevel].scoreNeeded;
        bool passed = Score >= needed;
        bool isLast = currentLevel == LevelData.TotalLevels - 1;

        if (passed)
            LevelData.SaveUnlockedLevel(CurrentMode, currentLevel + 1);

        LevelData.SaveBestScore(CurrentMode, Score);
        FirestoreUserProfile.QueueSave(LevelData.CloudFields(CurrentMode));

        if (!passed)
            uiManager.ShowLevelFail(Score, needed, CurrentLevel, LevelData.GetBestScore(CurrentMode));
        else if (!isLast)
            uiManager.ShowLevelComplete(Score, CurrentLevel + 1, LevelData.GetBestScore(CurrentMode));
        else
            uiManager.ShowVictory(Score, LevelData.GetBestScore(CurrentMode));
    }

    void RecordLeaderboardEntry()
    {
        var entry = new LeaderboardEntry
        {
            playerName = PlayerPrefs.GetString("PlayerName", "Player"),
            score = Score,
            mode = CurrentMode.ToString(),
            level = CurrentMode == GameMode.Endless ? endlessTier + 1 : currentLevel + 1,
            deliveries = deliveryCount,
            carId = selectedCar != null ? selectedCar.carId : "",
            date = System.DateTime.Now.ToString("yyyy-MM-dd")
        };
        FirestoreLeaderboard.UploadIfQualified(entry);
    }

    public void GenerateNewOrder()
    {
        isRushOrder = (deliveryCount % 4 == 0);
        string next;
        do { next = Destinations[Random.Range(0, Destinations.Length)]; }
        while (next == CurrentDestination && Destinations.Length > 1);
        CurrentDestination = next;
        uiManager.UpdateOrder(CurrentDestination, isRushOrder);
        UpdateZoneHighlights();
    }

    void UpdateZoneHighlights()
    {
        for (int i = 0; i < deliveryZones.Length; i++)
        {
            if (zoneHighlights[i] == null) continue;
            bool isTarget = deliveryZones[i].destinationName == CurrentDestination;
            zoneHighlights[i].SetHighlighted(isTarget);
            if (isTarget) zoneHighlights[i].TriggerFlash();
        }
    }

    public void HitByNPC()
    {
        if (State != GameState.Playing) return;
        if (Time.time - lastHitTime < HitImmunityDuration) return;
        lastHitTime = Time.time;

        if (IsHeartMode)
        {
            Lives--;
            uiManager.UpdateLives(Lives);
            uiManager.ShowFeedback(LocalizationManager.L("feedback_crash_life", "CRASH! -1 ♥"), false);
            if (Lives <= 0)
            {
                TimeRemaining = 0f;
                EndGame();
                return;
            }
        }
        else
        {
            TimeRemaining = Mathf.Max(0f, TimeRemaining - 4f);
            uiManager.ShowFeedback(LocalizationManager.L("feedback_crash", "CRASH! -4s"), false);
        }

        uiManager.ShowCrashFlash();
        AudioManager.Play(a => a.PlayCrash());
        ShakeCamera(0.35f, 0.20f);
    }

    public bool TryDeliver(string destination)
    {
        if (State != GameState.Playing || TimeRemaining <= 0f) return false;
        return destination == CurrentDestination ? HandleCorrectDelivery() : HandleWrongDelivery();
    }

    bool HandleCorrectDelivery()
    {
        int baseCoins = isRushOrder ? 20 : 10;
        deliveryStreak++;
        int streakBonus = deliveryStreak >= 5 ? 5 : deliveryStreak >= 3 ? 2 : 0;
        int totalCoins = baseCoins + streakBonus;
        Score += totalCoins;
        carSystem.AddCoins(totalCoins);

        float timeBonus = CurrentMode == GameMode.Endless ? endlessDeliveryBonus : 4f;
        TimeRemaining += timeBonus;
        uiManager.UpdateScore(Score, PassScore);
        uiManager.UpdateCoinDisplay(Coins);

        string bonusStr = timeBonus % 1f == 0f ? $"{timeBonus:0}s" : $"{timeBonus:0.0}s";
        string feedbackKey = isRushOrder ? "feedback_rush" : "feedback_delivered";
        string fallback = isRushOrder ? "RUSH! +{0} +{1}" : "Delivered! +{0} +{1}";
        uiManager.ShowFeedback(LocalizationManager.LFmt(feedbackKey, fallback, totalCoins, bonusStr), true);

        if (streakBonus > 0)
            uiManager.ShowFeedback(LocalizationManager.LFmt("streak_bonus", "+{0} streak bonus!", streakBonus), true);

        uiManager.ShowStreak(deliveryStreak);
        AudioManager.Play(a => a.PlayDelivered());
        uiManager.ShowScorePopup(totalCoins);

        deliveryCount++;

        if ((IsRushMode || IsHeartMode) && Score >= LevelData.Levels[currentLevel].scoreNeeded)
        {
            if (currentLevel == LevelData.TotalLevels - 1) { EndGame(); return true; }
            State = GameState.GameOver;
            if (PowerUpManager.Instance != null) PowerUpManager.Instance.OnGameStateChanged(GameState.GameOver);
            StartCoroutine(RushLevelClearRoutine());
            return true;
        }

        if (CurrentMode == GameMode.Endless)
        {
            endlessTierProgress += totalCoins;
            int tierTarget = 40 + endlessTier * 25 + (endlessTier / 3) * 20;
            if (endlessTierProgress >= tierTarget)
            {
                endlessTierProgress -= tierTarget;
                EndlessTierUp();
            }
        }

        GenerateNewOrder();
        player.TriggerSpeedBoost();
        return true;
    }

    bool HandleWrongDelivery()
    {
        deliveryStreak = 0;
        string destName = LocalizationManager.LDest(CurrentDestination);

        if (CurrentMode == GameMode.Endless && endlessTier >= 2)
        {
            float penalty = Mathf.Min(2f + (endlessTier - 2) * 0.2f, 5f);
            TimeRemaining = Mathf.Max(0f, TimeRemaining - penalty);
            uiManager.ShowFeedback(LocalizationManager.LFmt("feedback_wrong_time", "Wrong! -{0}s", penalty.ToString("0.#")), false);
        }
        else if (IsRushMode)
        {
            TimeRemaining = Mathf.Max(0f, TimeRemaining - RushWrongPenalty);
            uiManager.ShowFeedback(LocalizationManager.LFmt("feedback_wrong_rush", "Wrong! Go to {0} (-{1}s)", destName, RushWrongPenalty), false);
        }
        else
        {
            uiManager.ShowFeedback(LocalizationManager.LFmt("feedback_wrong", "Wrong! Go to {0}", destName), false);
        }

        uiManager.ShowStreak(0);
        AudioManager.Play(a => a.PlayWrong());
        return false;
    }

    // ── Rush level clear ────────────────────────────────────────────────────

    IEnumerator RushLevelClearRoutine()
    {
        Time.timeScale = 0f;
        waitingForRushAdvance = true;
        uiManager.ShowLevelComplete(Score, CurrentLevel + 1, LevelData.GetBestScore(CurrentMode), showCountdown: true);
        for (int i = 5; i > 0 && waitingForRushAdvance; i--)
        {
            uiManager.UpdateRushCountdown(i);
            yield return new WaitForSecondsRealtime(1f);
        }
        waitingForRushAdvance = false;
        Time.timeScale = 1f;
        uiManager.HideRushCountdown();
        RecordLeaderboardEntry();
        LevelData.SaveBestScore(CurrentMode, Score);
        LevelData.SaveUnlockedLevel(CurrentMode, currentLevel + 1);
        FirestoreUserProfile.QueueSave(LevelData.CloudFields(CurrentMode));
        currentLevel++;
        UpdateNPCSprites();
        StartGame();
    }

    // ── Endless tier-up ─────────────────────────────────────────────────────

    void EndlessTierUp()
    {
        endlessTier++;
        if (endlessTier + 1 == 10) LevelData.IncrementEndlessTier10();

        float tierBonus = endlessTier <= 10 ? 20f : endlessTier <= 20 ? 15f : 10f;
        TimeRemaining += tierBonus;

        endlessDeliveryBonus = Mathf.Max(1.5f, 4f - Mathf.Max(0, endlessTier - 5) * 0.18f);

        float speedInc = endlessTier <= 10 ? 0.3f : 0.5f;
        float speedCap = 9f + Mathf.Max(0, endlessTier - 9) * 0.5f;

        // Bump already-active NPCs and remember the new "current tier" speed
        // so newly activated NPCs can match it (instead of carrying stale values).
        float sampleSpeed = -1f;
        foreach (var npc in npcPool)
            if (npc.gameObject.activeSelf)
            {
                npc.speed = Mathf.Min(npc.speed + speedInc, speedCap);
                if (sampleSpeed < 0f) sampleSpeed = npc.speed;
            }

        if (bossPool != null)
            foreach (var npc in bossPool)
                if (npc.gameObject.activeSelf)
                    npc.speed = Mathf.Min(npc.speed + speedInc * 1.2f, speedCap + 1f);

        int nextCount = Mathf.Min(endlessTier + 1, npcPool.Length);
        Vector2 playerPos2 = player != null ? (Vector2)player.transform.position : Vector2.zero;
        for (int i = 0; i < nextCount; i++)
            if (!npcPool[i].gameObject.activeSelf)
            {
                // Set the newly activated NPC to current tier speed, not stale cached value
                npcPool[i].speed = sampleSpeed > 0f
                    ? sampleSpeed
                    : Mathf.Min(npcPool[i].baseSpeed * NPCSpeedMultiplier + endlessTier * speedInc, speedCap);
                npcPool[i].gameObject.SetActive(true);
                npcPool[i].RandomizePositionAwayFrom(playerPos2, 5f);
            }

        if (bossPool != null)
        {
            int bossCount = endlessTier >= 17 ? 3 : endlessTier >= 11 ? 2 : endlessTier >= 4 ? 1 : 0;
            bossCount = Mathf.Min(bossCount, bossPool.Length);
            for (int i = 0; i < bossPool.Length; i++)
            {
                bool shouldBeActive = i < bossCount;
                if (shouldBeActive && !bossPool[i].gameObject.activeSelf)
                {
                    bossPool[i].gameObject.SetActive(true);
                    bossPool[i].RandomizePositionAwayFrom(playerPos2, 6f);
                    uiManager.ShowFeedback(LocalizationManager.L("feedback_boss", "BOSS INCOMING!"), false);
                }
                else if (!shouldBeActive)
                    bossPool[i].gameObject.SetActive(false);
            }
        }

        UpdateNPCSprites();
        UpdateLevelDisplay();
        ShakeCamera(0.20f, 0.18f);
        AudioManager.Play(a => a.PlayTierUp());
        string bonusStr = tierBonus % 1f == 0f ? $"{tierBonus:0}s" : $"{tierBonus:0.#}s";
        uiManager.ShowFeedback(LocalizationManager.LFmt("feedback_tier", "TIER {0}! +{1}", endlessTier + 1, bonusStr), true);
    }

    public void OnNextLevelButton()
    {
        if (waitingForRushAdvance)
            waitingForRushAdvance = false;
        else
            NextLevel();
    }

    public void ReturnToMainMenu()
    {
        StopAllCoroutines();
        waitingForRushAdvance = false;
        isPaused = false;
        TimerFrozen = false;
        State = GameState.StartScreen;
        Time.timeScale = 1f;
        CurrentDestination = "";
        UpdateZoneHighlights();
        DeactivateAllNPCs();
        if (PowerUpManager.Instance != null) PowerUpManager.Instance.OnGameStateChanged(GameState.StartScreen);
        AudioManager.Play(a => a.StopBGM());
        uiManager.ShowModeSelectScreen();
    }

    // ── NPC helpers ─────────────────────────────────────────────────────────

    void UpdateNPCSprites()
    {
        if (normalCarSprites != null && normalCarSprites.Length > 0 && npcPool != null)
        {
            int level = CurrentMode == GameMode.Endless ? endlessTier : currentLevel;
            int variantCount = Mathf.Clamp(level + 1, 1, 4);
            variantCount = Mathf.Min(variantCount, normalCarSprites.Length);
            for (int i = 0; i < npcPool.Length; i++)
            {
                Sprite spr = normalCarSprites[i % variantCount];
                if (spr != null) npcPool[i].SetSprite(spr);
            }
        }
        AssignBossSpritesByActivation();
    }

    void AssignBossSpritesByActivation()
    {
        if (bossCarSprites == null || bossCarSprites.Length == 0 || bossPool == null) return;
        int spriteIndex = 0;
        for (int i = 0; i < bossPool.Length; i++)
        {
            if (bossPool[i].gameObject.activeSelf && spriteIndex < bossCarSprites.Length)
            {
                if (bossCarSprites[spriteIndex] != null)
                    bossPool[i].SetSprite(bossCarSprites[spriteIndex]);
                spriteIndex++;
            }
        }
    }

    void ActivateNPCsForLevel(int level)
    {
        if (npcPool == null) return;
        Vector2 playerPos = player != null ? (Vector2)player.transform.position : Vector2.zero;

        int normalCount = Mathf.Min(level + 1, npcPool.Length);
        for (int i = 0; i < npcPool.Length; i++)
        {
            bool active = i < normalCount;
            // Always reset speed to base — inactive NPCs must not carry over
            // accumulated speed from a previous session (endless tier-ups).
            npcPool[i].speed = npcPool[i].baseSpeed * NPCSpeedMultiplier;
            if (level >= 10)
                npcPool[i].speed = Mathf.Min(npcPool[i].baseSpeed * NPCSpeedMultiplier * (1f + (level - 10) * 0.035f), 8f);

            npcPool[i].gameObject.SetActive(active);
            if (active)
                npcPool[i].RandomizePositionAwayFrom(playerPos, 5f);
        }

        if (bossPool == null) return;

        int bossCount = level >= 17 ? 3 : level >= 11 ? 2 : level >= 4 ? 1 : 0;
        bossCount = Mathf.Min(bossCount, bossPool.Length);

        bool bossJustActivated = false;
        for (int i = 0; i < bossPool.Length; i++)
        {
            bool active = i < bossCount;
            bool wasInactive = !bossPool[i].gameObject.activeSelf;
            // Reset speed regardless of activation so stale endless values don't carry over
            bossPool[i].speed = bossPool[i].baseSpeed * NPCSpeedMultiplier;
            bossPool[i].gameObject.SetActive(active);
            if (active)
            {
                bossPool[i].RandomizePositionAwayFrom(playerPos, 6f);
                if (wasInactive && !bossJustActivated)
                {
                    bossJustActivated = true;
                    uiManager.ShowFeedback(LocalizationManager.L("feedback_boss", "BOSS INCOMING!"), false);
                }
            }
        }
    }

    void DeactivateAllNPCs()
    {
        if (npcPool != null)  foreach (var npc in npcPool)  npc.gameObject.SetActive(false);
        if (bossPool != null) foreach (var npc in bossPool) npc.gameObject.SetActive(false);
    }

    void ShakeCamera(float duration, float magnitude)
    {
        if (mainCamera == null) return;
        var follow = mainCamera.GetComponent<CameraFollow>();
        if (follow != null) follow.Shake(duration, magnitude);
    }

    public float GetLevelTime(int levelIndex) => LevelData.GetTime(levelIndex);
    public int GetLevelScoreTarget(int levelIndex) => LevelData.GetScoreTarget(levelIndex);

    public void UpdateLevelDisplay()
    {
        if (CurrentMode == GameMode.Endless)
        {
            string tierLabel = LocalizationManager.L("hud_tier", "Tier");
            uiManager.UpdateLevelText($"{tierLabel} {endlessTier + 1}");
        }
        else
            uiManager.UpdateLevel(CurrentLevel, LevelData.TotalLevels);
    }

    // Keep static ref for UIManager level select compatibility
    public static readonly LevelData.Level[] Levels = LevelData.Levels;
}
