using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState { StartScreen, CarSelect, Playing, GameOver }
public enum GameMode  { Normal, Rush, Endless, HeartExtreme, RushExtreme }

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
    public GameMode  CurrentMode { get; private set; } = GameMode.Normal;
    public int   Score           { get; private set; }
    public int   PassScore       { get; private set; }
    public float TimeRemaining   { get; private set; }
    public int   Lives           { get; private set; }
    public int   MaxLives        { get; private set; }
    public string CurrentDestination { get; private set; }
    public bool   IsRushOrder        => isRushOrder;
    public int CurrentLevel => CurrentMode == GameMode.Endless ? endlessTier + 1 : currentLevel + 1;

    public bool IsHeartMode => CurrentMode == GameMode.Normal || CurrentMode == GameMode.HeartExtreme;
    public bool IsRushMode  => CurrentMode == GameMode.Rush   || CurrentMode == GameMode.RushExtreme;
    public bool IsExtreme   => CurrentMode == GameMode.HeartExtreme || CurrentMode == GameMode.RushExtreme;
    public float NPCSpeedMultiplier => IsExtreme ? 1.5f : 1f;

    static readonly string[] Destinations = { "House A", "House B", "Shop", "Cafe" };

    public static readonly (float time, int scoreNeeded, int lives)[] Levels =
    {
        (30f,   50, 3),   // 1
        (30f,   70, 3),   // 2
        (35f,   90, 3),   // 3
        (35f,  110, 3),   // 4
        (40f,  130, 4),   // 5
        (40f,  155, 4),   // 6
        (45f,  180, 4),   // 7
        (45f,  210, 4),   // 8
        (50f,  240, 5),   // 9
        (50f,  275, 5),   // 10
        (55f,  290, 6),   // 11
        (57f,  330, 6),   // 12
        (59f,  370, 6),   // 13
        (61f,  415, 6),   // 14
        (63f,  465, 7),   // 15
        (65f,  520, 7),   // 16
        (67f,  580, 7),   // 17
        (69f,  645, 7),   // 18
        (71f,  715, 8),   // 19
        (73f,  805, 8),   // 20
        (75f,  900, 8),   // 21
        (77f, 1000, 8),   // 22
        (79f, 1105, 9),   // 23
        (81f, 1215, 9),   // 24
        (83f, 1295, 9),   // 25
        (85f, 1400, 9),   // 26
        (87f, 1510, 10),  // 27
        (89f, 1625, 10),  // 28
        (91f, 1745, 10),  // 29
        (93f, 1870, 11),  // 30
    };
    const int TotalLevels = 30;

    static string UnlockKey(GameMode mode) => mode switch
    {
        GameMode.Rush         => "Unlocked_Rush",
        GameMode.HeartExtreme => "Unlocked_HeartExtreme",
        GameMode.RushExtreme  => "Unlocked_RushExtreme",
        _                     => "Unlocked_Normal"
    };

    public static int GetUnlockedLevel(GameMode mode)
    {
        return PlayerPrefs.GetInt(UnlockKey(mode), 0);
    }

    public static int GetBestScore(GameMode mode)
        => PlayerPrefs.GetInt("BestScore_" + mode.ToString(), 0);

    static void SaveBestScore(GameMode mode, int score)
    {
        string key = "BestScore_" + mode.ToString();
        if (score > PlayerPrefs.GetInt(key, 0))
        {
            PlayerPrefs.SetInt(key, score);
            PlayerPrefs.Save();
        }
    }

    public static int GetBestEndlessTier()
        => PlayerPrefs.GetInt("BestTier_Endless", 0);

    static void SaveBestEndlessTier(int tier)
    {
        if (tier > PlayerPrefs.GetInt("BestTier_Endless", 0))
        {
            PlayerPrefs.SetInt("BestTier_Endless", tier);
            PlayerPrefs.Save();
        }
    }

    static void SaveUnlockedLevel(GameMode mode, int levelIndex)
    {
        string key = UnlockKey(mode);
        if (levelIndex > PlayerPrefs.GetInt(key, 0))
        {
            PlayerPrefs.SetInt(key, levelIndex);
            PlayerPrefs.Save();
        }
    }

    // ── Coin system ──────────────────────────────────────────────────────────
    public int Coins => PlayerPrefs.GetInt("Coins", 0);

    public void AddCoins(int amount)
    {
        PlayerPrefs.SetInt("Coins", Coins + amount);
        PlayerPrefs.Save();
    }

    public bool IsCarUnlocked(CarData car)
    {
        if (car == null) return false;
        if (car.unlockCost == 0 && car.requiredLevel == 0 && car.requiredEndlessTier10 == 0) return true;
        return PlayerPrefs.GetInt("CarUnlocked_" + car.carId, 0) == 1;
    }

    public bool CanUnlockCar(CarData car)
    {
        if (car == null) return false;
        if (Coins < car.unlockCost) return false;
        if (car.requiredLevel > 0)
        {
            if (GetUnlockedLevel(GameMode.Normal) < car.requiredLevel) return false;
            if (GetUnlockedLevel(GameMode.Rush) < car.requiredLevel) return false;
        }
        if (car.requiredEndlessTier10 > 0)
        {
            if (GetEndlessTier10Count() < car.requiredEndlessTier10) return false;
        }
        return true;
    }

    public string GetUnlockRequirementText(CarData car)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (car.requiredLevel > 0)
        {
            bool heartOk = GetUnlockedLevel(GameMode.Normal) >= car.requiredLevel;
            bool rushOk = GetUnlockedLevel(GameMode.Rush) >= car.requiredLevel;
            string heartIcon = heartOk ? "\u2713" : "\u2717";
            string rushIcon = rushOk ? "\u2713" : "\u2717";
            parts.Add($"{heartIcon} Heart Lv.{car.requiredLevel}");
            parts.Add($"{rushIcon} Rush Lv.{car.requiredLevel}");
        }
        if (car.requiredEndlessTier10 > 0)
        {
            int count = GetEndlessTier10Count();
            string icon = count >= car.requiredEndlessTier10 ? "\u2713" : "\u2717";
            parts.Add($"{icon} Endless T10 x{count}/{car.requiredEndlessTier10}");
        }
        return string.Join("\n", parts);
    }

    public static int GetEndlessTier10Count()
        => PlayerPrefs.GetInt("EndlessTier10Count", 0);

    static void IncrementEndlessTier10()
    {
        PlayerPrefs.SetInt("EndlessTier10Count", GetEndlessTier10Count() + 1);
        PlayerPrefs.Save();
    }

    public bool TryUnlockCar(CarData car)
    {
        if (car == null || IsCarUnlocked(car)) return false;
        if (!CanUnlockCar(car)) return false;
        AddCoins(-car.unlockCost);
        PlayerPrefs.SetInt("CarUnlocked_" + car.carId, 1);
        PlayerPrefs.Save();
        return true;
    }

    public void SelectCar(CarData car)
    {
        if (car == null) return;
        selectedCar = car;
        PlayerPrefs.SetString("SelectedCar", car.carId);
        PlayerPrefs.Save();
        OnCarChanged?.Invoke();
    }

    public void UnlockAllCars()
    {
        if (carCatalog == null) return;
        foreach (var car in carCatalog)
            if (car != null) PlayerPrefs.SetInt("CarUnlocked_" + car.carId, 1);
        PlayerPrefs.Save();
    }

    public void ResetAllCars()
    {
        if (carCatalog == null) return;
        foreach (var car in carCatalog)
        {
            if (car == null) continue;
            if (car.unlockCost == 0 && car.requiredLevel == 0 && car.requiredEndlessTier10 == 0) continue;
            PlayerPrefs.DeleteKey("CarUnlocked_" + car.carId);
        }
        PlayerPrefs.SetInt("Coins", 0);
        PlayerPrefs.SetInt("EndlessTier10Count", 0);
        if (carCatalog.Length > 0 && carCatalog[0] != null)
            SelectCar(carCatalog[0]);
        PlayerPrefs.Save();
    }

    void LoadSelectedCar()
    {
        string savedId = PlayerPrefs.GetString("SelectedCar", "");
        if (carCatalog != null)
        {
            foreach (var car in carCatalog)
                if (car != null && car.carId == savedId)
                { selectedCar = car; return; }
            if (carCatalog.Length > 0 && carCatalog[0] != null)
                selectedCar = carCatalog[0];
        }
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

    // Endless mode state
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
    }

    void Start()
    {
        deliveryZones = Object.FindObjectsByType<DeliveryZone>(FindObjectsSortMode.None);
        zoneHighlights = new ZoneHighlight[deliveryZones.Length];
        for (int i = 0; i < deliveryZones.Length; i++)
            zoneHighlights[i] = deliveryZones[i].GetComponent<ZoneHighlight>();

        // Split NPC pool into normal and boss subsets
        var all = Object.FindObjectsByType<NPCCar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var normals = new List<NPCCar>();
        var bosses  = new List<NPCCar>();
        foreach (var n in all)
            (n.isBoss ? bosses : normals).Add(n);
        npcPool  = normals.ToArray();
        bossPool = bosses.ToArray();
        foreach (var n in all) n.gameObject.SetActive(false);

        currentLevel = 0;
        LoadSelectedCar();
        State = GameState.StartScreen;
        uiManager.ShowStartScreen();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else if (State == GameState.Playing) PauseGame();
        }

        var gp = Gamepad.current;
        if (gp != null && gp.startButton.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else if (State == GameState.Playing) PauseGame();
        }

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

    // ── Game start entry points ───────────────────────────────────────────────

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

    public GameMode BaseMode => CurrentMode == GameMode.HeartExtreme ? GameMode.Normal
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
        int carHearts = (selectedCar != null) ? selectedCar.bonusHearts : 0;
        MaxLives = IsHeartMode ? Levels[currentLevel].lives + carHearts : 0;
        Lives = MaxLives;
        Time.timeScale = 1f;
        State = GameState.Playing;
        if (PowerUpManager.Instance != null) PowerUpManager.Instance.OnGameStateChanged(GameState.Playing);

        if (CurrentMode == GameMode.Endless)
        {
            endlessTier = 0;
            endlessTierProgress = 0;
            endlessDeliveryBonus = 4f;
            TimeRemaining = Levels[0].time;
            PassScore = 0;
        }
        else
        {
            float t = Levels[currentLevel].time;
            TimeRemaining = t;
            PassScore = Levels[currentLevel].scoreNeeded;
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
        currentLevel++;
        StartGame();
    }

    public void StartAtLevel(int levelIndex)
    {
        currentLevel = levelIndex;
        StartGame();
    }

    // ── Pause ─────────────────────────────────────────────────────────────────

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

    // ── End / delivery ────────────────────────────────────────────────────────

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

        if (CurrentMode == GameMode.Endless)
        {
            SaveBestScore(GameMode.Endless, Score);
            SaveBestEndlessTier(endlessTier + 1);
            uiManager.ShowEndlessSummary(Score, endlessTier + 1, deliveryCount,
                GetBestScore(GameMode.Endless), GetBestEndlessTier());
            return;
        }

        int needed = Levels[currentLevel].scoreNeeded;
        bool passed = Score >= needed;
        bool isLast = currentLevel == TotalLevels - 1;

        if (passed && CurrentMode != GameMode.Endless)
            SaveUnlockedLevel(CurrentMode, currentLevel + 1);

        if (!passed)
        {
            SaveBestScore(CurrentMode, Score);
            uiManager.ShowLevelFail(Score, needed, CurrentLevel, GetBestScore(CurrentMode));
        }
        else if (!isLast)
        {
            SaveBestScore(CurrentMode, Score);
            uiManager.ShowLevelComplete(Score, CurrentLevel + 1, GetBestScore(CurrentMode));
        }
        else
        {
            SaveBestScore(CurrentMode, Score);
            uiManager.ShowVictory(Score, GetBestScore(CurrentMode));
        }
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

        if (mainCamera != null)
        {
            var follow = mainCamera.GetComponent<CameraFollow>();
            if (follow != null) follow.Shake(0.35f, 0.20f);
        }
    }

    public bool TryDeliver(string destination)
    {
        if (State != GameState.Playing || TimeRemaining <= 0f) return false;

        if (destination != CurrentDestination)
            return HandleWrongDelivery();

        return HandleCorrectDelivery();
    }

    bool HandleCorrectDelivery()
    {
        int baseCoins = isRushOrder ? 20 : 10;
        deliveryStreak++;
        int streakBonus = deliveryStreak >= 5 ? 5 : deliveryStreak >= 3 ? 2 : 0;
        int totalCoins = baseCoins + streakBonus;
        Score += totalCoins;
        AddCoins(totalCoins);

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

        if ((IsRushMode || IsHeartMode) && Score >= Levels[currentLevel].scoreNeeded)
        {
            if (currentLevel == TotalLevels - 1) { EndGame(); return true; }
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

        deliveryCount++;
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

    // ── Rush level clear ──────────────────────────────────────────────────────

    IEnumerator RushLevelClearRoutine()
    {
        Time.timeScale = 0f;
        waitingForRushAdvance = true;
        uiManager.ShowLevelComplete(Score, CurrentLevel + 1, GetBestScore(CurrentMode), showCountdown: true);
        for (int i = 5; i > 0 && waitingForRushAdvance; i--)
        {
            uiManager.UpdateRushCountdown(i);
            yield return new WaitForSecondsRealtime(1f);
        }
        waitingForRushAdvance = false;
        Time.timeScale = 1f;
        uiManager.HideRushCountdown();
        SaveBestScore(CurrentMode, Score);
        SaveUnlockedLevel(CurrentMode, currentLevel + 1);
        currentLevel++;
        UpdateNPCSprites();
        StartGame();
    }

    // ── Endless tier-up ───────────────────────────────────────────────────────

    void EndlessTierUp()
    {
        endlessTier++;
        if (endlessTier + 1 == 10) IncrementEndlessTier10();

        float tierBonus = endlessTier <= 10 ? 20f : endlessTier <= 20 ? 15f : 10f;
        TimeRemaining += tierBonus;

        endlessDeliveryBonus = Mathf.Max(1.5f, 4f - Mathf.Max(0, endlessTier - 5) * 0.18f);

        float speedInc = endlessTier <= 10 ? 0.3f : 0.5f;
        float speedCap = 9f + Mathf.Max(0, endlessTier - 9) * 0.5f;

        foreach (var npc in npcPool)
            if (npc.gameObject.activeSelf)
                npc.speed = Mathf.Min(npc.speed + speedInc, speedCap);

        // Boss NPCs speed up slightly faster
        if (bossPool != null)
            foreach (var npc in bossPool)
                if (npc.gameObject.activeSelf)
                    npc.speed = Mathf.Min(npc.speed + speedInc * 1.2f, speedCap + 1f);

        // Activate normal NPCs
        int nextCount = Mathf.Min(endlessTier + 1, npcPool.Length);
        Vector2 playerPos2 = player != null ? (Vector2)player.transform.position : Vector2.zero;
        for (int i = 0; i < nextCount; i++)
            if (!npcPool[i].gameObject.activeSelf)
            {
                npcPool[i].gameObject.SetActive(true);
                npcPool[i].RandomizePositionAwayFrom(playerPos2, 5f);
            }

        // Activate boss NPCs at tier milestones (mirrors level 5/12/18 thresholds)
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

    // ── NPC helpers ───────────────────────────────────────────────────────────

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

        // Normal NPCs: level+1 active, scaled speed for levels 11+
        int normalCount = Mathf.Min(level + 1, npcPool.Length);
        for (int i = 0; i < npcPool.Length; i++)
        {
            bool active = i < normalCount;
            npcPool[i].gameObject.SetActive(active);
            if (active)
            {
                npcPool[i].speed = npcPool[i].baseSpeed * NPCSpeedMultiplier;
                if (level >= 10)
                    npcPool[i].speed = Mathf.Min(npcPool[i].baseSpeed * NPCSpeedMultiplier * (1f + (level - 10) * 0.035f), 8f);
                npcPool[i].RandomizePositionAwayFrom(playerPos, 5f);
            }
        }

        if (bossPool == null) return;

        // Boss NPCs: appear level 5+, max 2 at level 12+, max 3 at level 18+
        int bossCount = 0;
        if (level >= 4)  bossCount = 1;   // level 5
        if (level >= 11) bossCount = 2;   // level 12
        if (level >= 17) bossCount = 3;   // level 18
        bossCount = Mathf.Min(bossCount, bossPool.Length);

        bool bossJustActivated = false;
        for (int i = 0; i < bossPool.Length; i++)
        {
            bool active = i < bossCount;
            bool wasInactive = !bossPool[i].gameObject.activeSelf;
            bossPool[i].gameObject.SetActive(active);
            if (active)
            {
                bossPool[i].speed = bossPool[i].baseSpeed * NPCSpeedMultiplier;
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

    public float GetLevelTime(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= Levels.Length) return 0f;
        return Levels[levelIndex].time;
    }

    public int GetLevelScoreTarget(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= Levels.Length) return 0;
        return Levels[levelIndex].scoreNeeded;
    }

    public void UpdateLevelDisplay()
    {
        if (CurrentMode == GameMode.Endless)
        {
            string tierLabel = LocalizationManager.L("hud_tier", "Tier");
            uiManager.UpdateLevelText($"{tierLabel} {endlessTier + 1}");
        }
        else
            uiManager.UpdateLevel(CurrentLevel, TotalLevels);
    }
}
