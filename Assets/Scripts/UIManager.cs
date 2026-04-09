using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public partial class UIManager : MonoBehaviour
{
    [Header("Gameplay HUD")]
    public TextMeshProUGUI timerText;
    public UnityEngine.UI.Image crashFlashImage;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI orderText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI livesText;

    [Header("Screens")]
    public GameObject startScreen;
    public GameObject modeSelectScreen;
    public GameObject gameOverScreen;
    public GameObject levelCompleteScreen;
    public GameObject victoryScreen;
    public GameObject gameplayUI;
    public GameObject endlessSummaryScreen;
    public GameObject levelSelectScreen;

    [Header("Level Fail")]
    public TextMeshProUGUI failTitleText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI neededScoreText;

    [Header("Level Complete")]
    public TextMeshProUGUI levelCompleteScoreText;
    public TextMeshProUGUI levelCompleteNextText;
    public TextMeshProUGUI levelCompleteCountdownText;

    [Header("Victory")]
    public TextMeshProUGUI victoryScoreText;

    [Header("Streak")]
    public TextMeshProUGUI streakText;

    [Header("Pause")]
    public GameObject pauseScreen;
    public Button resumeButton;
    public Button restartButton;
    public Button selectModeFromPauseButton;

    [Header("Buttons")]
    public Button startButton;
    public Button retryButton;
    public Button nextLevelButton;
    public Button playAgainButton;

    [Header("Mode Select Buttons")]
    public Button rushModeButton;
    public Button heartModeButton;
    public Button endlessModeButton;
    public Button heartExtremeModeButton;
    public Button rushExtremeModeButton;

    [Header("Endless Summary")]
    public TextMeshProUGUI endlessTierText;
    public TextMeshProUGUI endlessScoreText;
    public TextMeshProUGUI endlessDeliveriesText;
    public Button endlessSelectModeButton;
    public Button endlessRetryButton;

    [Header("Power-up HUD")]
    public GameObject powerUpHudPanel;

    [Header("Settings Screen")]
    public GameObject settingsScreen;
    public UnityEngine.UI.Slider volumeSlider;
    public TextMeshProUGUI languageButtonText;
    public Button languageButton;
    public Button settingsButton;
    public Button startSettingsButton;
    public Button hudSettingsButton;
    public Button closeSettingsButton;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;
    public TextMeshProUGUI bgmLabelText;
    public TextMeshProUGUI sfxLabelText;
    public Button fullscreenToggleButton;
    public TextMeshProUGUI fullscreenLabelText;
    public Button modeBackToStartButton;
    public Slider zoneLabelOpacitySlider;
    public TextMeshProUGUI zoneLabelOpacityLabel;
    public Button googleLinkButton;
    public TextMeshProUGUI googleLinkStatusText;

    [Header("Car Select")]
    public GameObject carSelectScreen;
    public TextMeshProUGUI carSelectTitleText;
    public TextMeshProUGUI coinBalanceText;
    public TextMeshProUGUI hudCoinText;
    public Button carSelectBackButton;
    public Button startCarSelectButton;
    public Transform carSelectContent;

    [Header("Leaderboard")]
    public GameObject leaderboardScreen;
    public TextMeshProUGUI leaderboardTitleText;
    public Transform leaderboardContent;
    public Button leaderboardBackButton;
    public Button[] leaderboardModeTabs;
    public Button startLeaderboardButton;
    public Button modeSelectLeaderboardButton;
    public Button gameOverLeaderboardButton;
    public Button levelCompleteLeaderboardButton;
    public Button victoryLeaderboardButton;
    public Button endlessLeaderboardButton;

    [Header("Login Screen")]
    public GameObject loginScreen;
    public TextMeshProUGUI loginTitleText;
    public TextMeshProUGUI loginSubtitleText;
    public TMP_InputField playerNameInput;
    public Button loginConfirmButton;
    public Button googleSignInButton;
    public Button guestSignInButton;
    public TextMeshProUGUI authStatusText;
    public GameObject loginAuthPanel;
    public GameObject loginNamePanel;
    public Button loginBackButton;

    [Header("Localizable Static Texts")]
    public TextMeshProUGUI startTitleText;
    public TextMeshProUGUI settingsTitleText;
    public TextMeshProUGUI volumeLabelText;
    public TextMeshProUGUI modeSelectTitleText;
    public TextMeshProUGUI rushModeDescText;
    public TextMeshProUGUI heartModeDescText;
    public TextMeshProUGUI endlessModeDescText;
    public TextMeshProUGUI heartExtremeDescText;
    public TextMeshProUGUI rushExtremeDescText;
    public TextMeshProUGUI standardHeaderText;
    public TextMeshProUGUI extremeHeaderText;
    public TextMeshProUGUI pauseTitleText;

    [Header("Screen Titles")]
    public TextMeshProUGUI startSubtitleText;
    public TextMeshProUGUI levelCompleteTitleText;
    public TextMeshProUGUI victoryTitleText;
    public TextMeshProUGUI victorySubtitleText;
    public TextMeshProUGUI endlessTitleText;
    public TextMeshProUGUI pauseHintText;
    public TextMeshProUGUI levelSelectTitleText;

    int  lastDisplayedSeconds = -1;
    bool lastCarryState       = false;
    Coroutine feedbackCoroutine;
    Button[] levelButtons;

    // ── Helpers ─────────────────────────────────────────────────────────────

    void AddClickSound(Button btn)
    {
        if (btn != null)
            btn.onClick.AddListener(() => AudioManager.Play(a => a.PlayButtonClick()));
    }

    void Bind(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
        AddClickSound(btn);
    }

    void GM(System.Action<GameManager> action)
    {
        if (GameManager.Instance != null) action(GameManager.Instance);
    }

    // ── Lifecycle ───────────────────────────────────────────────────────────

    void Start()
    {
        BindCoreButtons();
        BindModeButtons();
        BindLeaderboardButtons();
        BindAuthButtons();
        BindSettingsButtons();
        BindVolumeSliders();

        if (levelSelectScreen == null) AutoFindLevelSelectScreen();

        BuildLevelSelectIfNeeded();
        BuildPowerUpHudIfNeeded();
        WireSettingsListeners();
        RefreshLocalization();
    }

    void BindCoreButtons()
    {
        Bind(startButton,               () => ShowModeSelectScreen());
        Bind(retryButton,               () => GM(g => g.StartGame()));
        Bind(resumeButton,              () => GM(g => g.ResumeGame()));
        Bind(restartButton,             () => GM(g => g.StartGame()));
        Bind(nextLevelButton,           () => GM(g => g.OnNextLevelButton()));
        Bind(selectModeFromPauseButton, () => GM(g => g.ReturnToMainMenu()));
        Bind(playAgainButton,           () => ShowModeSelectScreen());
        Bind(modeBackToStartButton,     () => ShowStartScreen());
        Bind(carSelectBackButton,       () => ShowStartScreen());
        Bind(startCarSelectButton,      () => GM(g => g.ShowCarSelect()));
    }

    void BindModeButtons()
    {
        Bind(rushModeButton,            () => GM(g => g.StartWithMode(GameMode.Rush)));
        Bind(heartModeButton,          () => GM(g => g.StartWithMode(GameMode.Heart)));
        Bind(endlessModeButton,         () => GM(g => g.StartWithMode(GameMode.Endless)));
        Bind(heartExtremeModeButton,    () => GM(g => g.StartWithMode(GameMode.HeartExtreme)));
        Bind(rushExtremeModeButton,     () => GM(g => g.StartWithMode(GameMode.RushExtreme)));
        Bind(endlessSelectModeButton,   () => ShowModeSelectScreen());
        Bind(endlessRetryButton,        () => GM(g => g.StartWithMode(GameMode.Endless)));
    }

    void BindLeaderboardButtons()
    {
        Bind(startLeaderboardButton,         () => ShowLeaderboardScreen());
        Bind(modeSelectLeaderboardButton,    () => ShowLeaderboardScreen());
        Bind(gameOverLeaderboardButton,      () => ShowLeaderboardScreen());
        Bind(levelCompleteLeaderboardButton, () => ShowLeaderboardScreen());
        Bind(victoryLeaderboardButton,       () => ShowLeaderboardScreen());
        Bind(endlessLeaderboardButton,       () => ShowLeaderboardScreen(GameMode.Endless));
        Bind(leaderboardBackButton,          () => ShowStartScreen());
        if (leaderboardModeTabs != null)
        {
            GameMode[] modes = { GameMode.Heart, GameMode.Rush, GameMode.Endless, GameMode.HeartExtreme, GameMode.RushExtreme };
            for (int i = 0; i < leaderboardModeTabs.Length && i < modes.Length; i++)
            {
                int idx = i;
                Bind(leaderboardModeTabs[idx], () => { leaderboardCurrentMode = modes[idx]; PopulateLeaderboard(modes[idx]); UpdateLeaderboardTabHighlight(); });
            }
        }
    }

    void BindAuthButtons()
    {
        if (playerNameInput != null)
        {
            playerNameInput.text = LeaderboardManager.GetPlayerName();
            playerNameInput.onEndEdit.AddListener(name => {
                if (!string.IsNullOrEmpty(name)) LeaderboardManager.SetPlayerName(name);
            });
        }
        Bind(loginConfirmButton, () => ConfirmLogin());
        Bind(googleSignInButton, () => GoogleSignIn());
        Bind(guestSignInButton, () => GuestSignIn());
        Bind(loginBackButton, () => ShowLoginAuthPhase());
    }

    void BindSettingsButtons()
    {
        Bind(settingsButton,            () => ShowSettingsScreen());
        Bind(startSettingsButton,       () => ShowSettingsScreen());
        Bind(hudSettingsButton,         () => GM(g => g.PauseGame()));
        Bind(closeSettingsButton,       () => HideSettingsScreen());
        Bind(googleLinkButton,          () => LinkGoogleAccount());
        Bind(languageButton,            () => { if (LocalizationManager.Instance != null) LocalizationManager.Instance.ToggleLanguage(); UpdateLanguageButtonText(); });
    }

    void BindVolumeSliders()
    {
        if (volumeSlider != null)
        {
            if (AudioManager.Instance != null) volumeSlider.value = AudioManager.Instance.MasterVolume;
            volumeSlider.onValueChanged.AddListener(v => AudioManager.Play(a => a.SetMasterVolume(v)));
        }
        if (bgmVolumeSlider != null)
        {
            if (AudioManager.Instance != null) bgmVolumeSlider.value = AudioManager.Instance.BGMVolume;
            bgmVolumeSlider.onValueChanged.AddListener(v => AudioManager.Play(a => a.SetBGMVolume(v)));
        }
        if (sfxVolumeSlider != null)
        {
            if (AudioManager.Instance != null) sfxVolumeSlider.value = AudioManager.Instance.SFXVolume;
            sfxVolumeSlider.onValueChanged.AddListener(v => AudioManager.Play(a => a.SetSFXVolume(v)));
        }
    }

    void AutoFindLevelSelectScreen()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            if (canvases.Length > 0) canvas = canvases[0];
        }
        if (canvas != null)
        {
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                if (t.gameObject.name == "LevelSelectScreen")
                { levelSelectScreen = t.gameObject; break; }
        }
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshLocalization;
        PowerUpManager.OnPowerUpActivated   += HandlePowerUpActivated;
        PowerUpManager.OnPowerUpDeactivated += HandlePowerUpDeactivated;
        AuthManager.OnAuthSessionExpired    += HandleSessionExpired;
        AuthManager.OnAuthStateChanged      += HandleAuthReturningSession;
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshLocalization;
        PowerUpManager.OnPowerUpActivated   -= HandlePowerUpActivated;
        PowerUpManager.OnPowerUpDeactivated -= HandlePowerUpDeactivated;
        AuthManager.OnAuthSessionExpired    -= HandleSessionExpired;
        AuthManager.OnAuthStateChanged      -= HandleAuthReturningSession;
    }

    // ── Screen management ───────────────────────────────────────────────────

    public void ShowStartScreen()
    {
        string savedName = LeaderboardManager.GetPlayerName();
        if (savedName == "Player" || string.IsNullOrEmpty(savedName))
        {
            ShowLoginScreen();
            return;
        }
        SetAllScreens(start: true);
    }

    public void ShowModeSelectScreen()
    {
        SetAllScreens(modeSelect: true);
    }

    public void ShowGameplayUI()
    {
        SetAllScreens(gameplay: true);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        if (streakText != null) streakText.gameObject.SetActive(false);
        if (livesText != null) livesText.gameObject.SetActive(false);
        ClearPowerUpTimers();
        UpdateCoinDisplay(GameManager.Instance != null ? GameManager.Instance.Coins : 0);
    }

    public void ShowLevelFail(int score, int needed, int level, int bestScore)
    {
        SetAllScreens(gameOver: true);
        if (failTitleText != null)   failTitleText.text   = LocalizationManager.LFmt("fail_title",  "LEVEL {0} FAILED", level);
        if (finalScoreText != null)  finalScoreText.text  = LocalizationManager.LFmt("fail_score",  "Score: {0}",        score);
        if (neededScoreText != null) neededScoreText.text = LocalizationManager.LFmt("fail_needed", "Need {0} to pass",  needed);
        var bestTxt = FindOrCreateBestText(gameOverScreen, "BestScoreText");
        if (bestTxt != null)
            bestTxt.text = LocalizationManager.LFmt("best_score", "Best: {0}", bestScore);
    }

    public void ShowLevelComplete(int score, int nextLevel, int bestScore = 0, bool showCountdown = false)
    {
        SetAllScreens(levelComplete: true);
        if (levelCompleteScoreText != null) levelCompleteScoreText.text = LocalizationManager.LFmt("lc_score", "Score: {0}", score);
        if (levelCompleteNextText != null)  levelCompleteNextText.text  = LocalizationManager.LFmt("lc_next",  "Next: Level {0}", nextLevel);
        if (levelCompleteCountdownText != null)
        {
            levelCompleteCountdownText.gameObject.SetActive(showCountdown);
            if (showCountdown) levelCompleteCountdownText.text = LocalizationManager.LFmt("rush_countdown", "Auto-advancing in {0}...", 5);
        }
        var bestTxt = FindOrCreateBestText(levelCompleteScreen, "BestScoreText");
        if (bestTxt != null && bestScore > 0)
        {
            bestTxt.text = LocalizationManager.LFmt("best_score", "Best: {0}", bestScore);
            bestTxt.gameObject.SetActive(true);
        }
    }

    public void ShowVictory(int score, int bestScore)
    {
        SetAllScreens(victory: true);
        if (victoryScoreText != null) victoryScoreText.text = LocalizationManager.LFmt("vic_score", "Final Score: {0}", score);
        var bestTxt = FindOrCreateBestText(victoryScreen, "BestScoreText");
        if (bestTxt != null)
        {
            bool isNew = score >= bestScore;
            bestTxt.text = isNew
                ? LocalizationManager.L("new_record", "NEW RECORD!")
                : LocalizationManager.LFmt("best_score", "Best: {0}", bestScore);
            bestTxt.color = isNew ? new Color(1f, 0.85f, 0.1f) : new Color(0.8f, 0.8f, 0.3f);
        }
    }

    public void ShowEndlessSummary(int score, int tier, int deliveries, int bestScore = 0, int bestTier = 0)
    {
        SetAllScreens(endlessSummary: true);
        if (endlessScoreText != null)      endlessScoreText.text      = LocalizationManager.LFmt("endless_score",      "Score: {0}",         score);
        if (endlessTierText != null)       endlessTierText.text       = LocalizationManager.LFmt("endless_tiers",      "Tiers Reached: {0}", tier);
        if (endlessDeliveriesText != null) endlessDeliveriesText.text = LocalizationManager.LFmt("endless_deliveries", "Deliveries: {0}",    deliveries);

        bool isNewScore = score >= bestScore && bestScore > 0;
        bool isNewTier  = tier >= bestTier && bestTier > 0;
        Color goldColor   = new Color(1f, 0.85f, 0.1f);
        Color normalColor = new Color(0.8f, 0.8f, 0.3f);

        SetupEndlessBestText("BestScoreText", new Vector2(0.10f, 0.34f), new Vector2(0.90f, 0.42f),
            bestScore > 0, LocalizationManager.LFmt("endless_best_score", "Best Score: {0}", bestScore),
            isNewScore ? goldColor : normalColor);

        SetupEndlessBestText("BestTierText", new Vector2(0.10f, 0.27f), new Vector2(0.90f, 0.35f),
            bestTier > 0, LocalizationManager.LFmt("endless_best_tier", "Best Tier: {0}", bestTier),
            isNewTier ? goldColor : normalColor);

        var newRecTxt = FindOrCreateBestText(endlessSummaryScreen, "NewRecordText");
        if (newRecTxt != null)
        {
            var rt = (RectTransform)newRecTxt.transform;
            rt.anchorMin = new Vector2(0.10f, 0.20f);
            rt.anchorMax = new Vector2(0.90f, 0.28f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            bool hasRecord = isNewScore || isNewTier;
            newRecTxt.gameObject.SetActive(hasRecord);
            if (hasRecord)
            {
                newRecTxt.text = LocalizationManager.L("new_record", "NEW RECORD!");
                newRecTxt.color = goldColor;
                newRecTxt.fontSize = 36;
                newRecTxt.fontStyle = FontStyles.Bold;
            }
        }
    }

    void SetupEndlessBestText(string name, Vector2 anchorMin, Vector2 anchorMax, bool show, string text, Color color)
    {
        var txt = FindOrCreateBestText(endlessSummaryScreen, name);
        if (txt == null) return;
        var rt = (RectTransform)txt.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        txt.gameObject.SetActive(show);
        if (show) { txt.text = text; txt.color = color; }
    }

    public void ShowPauseScreen()
    {
        if (pauseScreen != null) pauseScreen.SetActive(true);
    }

    public void HidePauseScreen()
    {
        if (pauseScreen != null) pauseScreen.SetActive(false);
    }

    void SetAllScreens(bool login = false, bool start = false, bool modeSelect = false, bool gameplay = false,
                       bool gameOver = false, bool levelComplete = false, bool victory = false,
                       bool endlessSummary = false, bool levelSelect = false, bool carSelect = false,
                       bool leaderboard = false)
    {
        if (loginScreen != null)          loginScreen.SetActive(login);
        if (startScreen != null)          startScreen.SetActive(start);
        if (modeSelectScreen != null)     modeSelectScreen.SetActive(modeSelect);
        if (gameplayUI != null)           gameplayUI.SetActive(gameplay);
        if (gameOverScreen != null)       gameOverScreen.SetActive(gameOver);
        if (levelCompleteScreen != null)  levelCompleteScreen.SetActive(levelComplete);
        if (victoryScreen != null)        victoryScreen.SetActive(victory);
        if (endlessSummaryScreen != null) endlessSummaryScreen.SetActive(endlessSummary);
        if (levelSelectScreen != null)    levelSelectScreen.SetActive(levelSelect);
        if (carSelectScreen != null)      carSelectScreen.SetActive(carSelect);
        if (leaderboardScreen != null)    leaderboardScreen.SetActive(leaderboard);
        if (pauseScreen != null)          pauseScreen.SetActive(false);
        if (settingsScreen != null)       settingsScreen.SetActive(false);
    }

    // ── HUD updates ─────────────────────────────────────────────────────────

    public void UpdateTimer(float time)
    {
        if (timerText == null) return;
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, time));
        if (seconds != lastDisplayedSeconds)
        {
            timerText.text = $"{LocalizationManager.L("hud_time", "Time")}: {seconds}s";
            lastDisplayedSeconds = seconds;
        }
        timerText.color = time < 10f ? Color.red : Color.white;
    }

    public void UpdateScore(int score, int passScore = 0)
    {
        if (scoreText == null) return;
        scoreText.text = passScore > 0
            ? $"{score}/{passScore}{LocalizationManager.L("hud_pts", "pts")}"
            : $"{LocalizationManager.L("hud_score", "Score")}: {score}";
        scoreText.color = (passScore > 0 && score >= passScore)
            ? new Color(0.22f, 1f, 0.48f)
            : new Color(1f, 0.82f, 0.15f);
    }

    public void UpdateLives(int lives)
    {
        if (livesText == null) return;
        int max = GameManager.Instance != null ? GameManager.Instance.MaxLives : lives;
        livesText.text = $"<color=#FF4444>\u2665</color> {lives}/{max}";
        livesText.gameObject.SetActive(true);
    }

    public void UpdateLevel(int level, int total)
    {
        if (levelText != null)
            levelText.text = $"{LocalizationManager.L("hud_level", "Level")} {level}/{total}";
    }

    public void UpdateLevelText(string text)
    {
        if (levelText != null) levelText.text = text;
    }

    public void UpdateOrder(string destination, bool isRush = false)
    {
        if (orderText == null) return;
        string destName  = LocalizationManager.LDest(destination);
        string deliverTo = LocalizationManager.L("hud_deliver_to", "Deliver to:");
        string rushLabel = LocalizationManager.L("hud_rush_label", "\u26A1 RUSH!");
        orderText.text = isRush
            ? $"{rushLabel}\n{deliverTo}\n<b>{destName}</b>"
            : $"{deliverTo}\n<b>{destName}</b>";
    }

    public void UpdateCarryStatus(bool carrying)
    {
        lastCarryState = carrying;
    }

    public void ShowFeedback(string message, bool positive)
    {
        if (feedbackText == null) return;
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = StartCoroutine(FeedbackRoutine(message, positive));
    }

    public void ShowStreak(int count)
    {
        if (streakText == null) return;
        bool show = count >= 2;
        streakText.gameObject.SetActive(show);
        if (show)
        {
            streakText.text = LocalizationManager.LFmt("streak_label", "x{0} STREAK!", count);
            streakText.color = count >= 5 ? new Color(1f, 0.6f, 0f) : new Color(1f, 0.95f, 0.2f);
        }
    }

    public void ShowScorePopup(int points)
    {
        if (scoreText == null) return;
        StartCoroutine(ScorePopupRoutine(points));
    }

    public void UpdateRushCountdown(int seconds)
    {
        if (levelCompleteCountdownText != null)
            levelCompleteCountdownText.text = LocalizationManager.LFmt("rush_countdown", "Auto-advancing in {0}...", seconds);
    }

    public void HideRushCountdown()
    {
        if (levelCompleteCountdownText != null)
            levelCompleteCountdownText.gameObject.SetActive(false);
    }

    // ── Coroutines ──────────────────────────────────────────────────────────

    IEnumerator ScorePopupRoutine(int points)
    {
        var go = new GameObject("ScorePopup", typeof(RectTransform));
        go.transform.SetParent(scoreText.transform.parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text         = "+" + points;
        tmp.fontSize     = 36;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.color        = new Color(1f, 0.9f, 0.2f, 1f);
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        var rect = (RectTransform)go.transform;
        rect.sizeDelta        = new Vector2(160f, 56f);
        rect.anchoredPosition = scoreText.rectTransform.anchoredPosition + new Vector2(80f, 0f);

        float duration = 0.8f;
        float t = 0f;
        Vector2 startPos = rect.anchoredPosition;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / duration;
            rect.anchoredPosition = startPos + new Vector2(0f, 55f * progress);
            tmp.color = new Color(1f, 0.9f, 0.2f, 1f - progress);
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator FeedbackRoutine(string message, bool positive)
    {
        Color base_ = positive ? new Color(0.2f, 1f, 0.2f) : new Color(1f, 0.35f, 0.35f);
        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);

        for (float t = 0f; t < 0.12f; t += Time.unscaledDeltaTime)
        {
            feedbackText.color = new Color(base_.r, base_.g, base_.b, t / 0.12f);
            yield return null;
        }
        feedbackText.color = base_;

        yield return new WaitForSecondsRealtime(0.65f);

        for (float t = 0f; t < 0.30f; t += Time.unscaledDeltaTime)
        {
            feedbackText.color = new Color(base_.r, base_.g, base_.b, 1f - t / 0.30f);
            yield return null;
        }
        feedbackText.gameObject.SetActive(false);
        feedbackCoroutine = null;
    }

    Coroutine crashFlashCoroutine;

    public void ShowCrashFlash()
    {
        if (crashFlashCoroutine != null) StopCoroutine(crashFlashCoroutine);
        crashFlashCoroutine = StartCoroutine(CrashFlashRoutine());
    }

    IEnumerator CrashFlashRoutine()
    {
        if (crashFlashImage == null) yield break;
        crashFlashImage.gameObject.SetActive(true);
        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0.55f, 0f, t / 0.45f);
            crashFlashImage.color = new Color(1f, 0.08f, 0.08f, alpha);
            yield return null;
        }
        crashFlashImage.gameObject.SetActive(false);
    }

    // ── Level Select ────────────────────────────────────────────────────────

    public void ShowLevelSelectScreen(GameMode mode)
    {
        BuildLevelSelectIfNeeded();
        SetAllScreens(levelSelect: true);
        RefreshLevelButtons(mode);
    }

    void RefreshLevelButtons(GameMode mode)
    {
        if (levelButtons == null) return;
        int unlocked = LevelData.GetUnlockedLevel(mode);
        string timeFmt  = LocalizationManager.L("level_select_time_fmt",  "{0}s");
        string scoreFmt = LocalizationManager.L("level_select_score_fmt", "{0}pts");
        bool showLives = mode == GameMode.Heart || mode == GameMode.HeartExtreme;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            bool isUnlocked = i <= unlocked;
            levelButtons[i].interactable = isUnlocked;
            var lockOverlay = levelButtons[i].transform.Find("Lock");
            if (lockOverlay != null) lockOverlay.gameObject.SetActive(!isUnlocked);

            var subT = levelButtons[i].transform.Find("SubInfo");
            if (subT == null) continue;
            var subTMP = subT.GetComponent<TextMeshProUGUI>();
            if (subTMP == null) continue;

            var level = LevelData.Levels[i];
            string info = string.Format(timeFmt, (int)level.time) + "  " + string.Format(scoreFmt, level.scoreNeeded);
            if (showLives) info += "  \u2665" + level.lives;
            subTMP.text = info;
        }
    }

    void BuildLevelSelectIfNeeded()
    {
        if (levelSelectScreen == null) return;
        if (levelButtons != null && levelButtons.Length > 0) return;

        var allBtns = levelSelectScreen.GetComponentsInChildren<Button>(true);
        var list = new System.Collections.Generic.List<Button>();
        foreach (var b in allBtns)
            if (b.gameObject.name.StartsWith("Btn")) list.Add(b);
        levelButtons = list.ToArray();

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelIdx = i;
            levelButtons[i].onClick.RemoveAllListeners();
            levelButtons[i].onClick.AddListener(() => GameManager.Instance.StartAtLevel(levelIdx));
            AddClickSound(levelButtons[i]);
        }

        var backT = levelSelectScreen.transform.Find("BackBtn");
        if (backT != null)
        {
            var lsBackBtn = backT.GetComponent<Button>();
            if (lsBackBtn != null)
            {
                lsBackBtn.onClick.RemoveAllListeners();
                lsBackBtn.onClick.AddListener(ShowModeSelectScreen);
                AddClickSound(lsBackBtn);
            }
        }
    }

    // ── Localization ────────────────────────────────────────────────────────

    public void RefreshLocalization()
    {
        UpdateLanguageButtonText();
        if (GameManager.Instance != null && levelText != null)
            GameManager.Instance.UpdateLevelDisplay();

        SetText(startTitleText,          "start_title");
        SetText(startSubtitleText,       "start_subtitle");
        SetText(settingsTitleText,       "settings_title");
        SetText(volumeLabelText,         "settings_volume");
        SetText(modeSelectTitleText,     "select_mode_title");
        SetText(rushModeDescText,        "mode_rush_desc");
        SetText(heartModeDescText,      "mode_heart_desc");
        SetText(endlessModeDescText,     "mode_endless_desc");
        SetText(heartExtremeDescText,    "mode_heart_extreme_desc");
        SetText(rushExtremeDescText,     "mode_rush_extreme_desc");
        SetText(standardHeaderText,     "mode_section_standard");
        SetText(extremeHeaderText,      "mode_section_extreme");
        SetText(pauseTitleText,          "pause_title");
        SetText(pauseHintText,           "pause_hint");
        SetText(levelCompleteTitleText,  "lc_title");
        SetText(victoryTitleText,        "vic_title");
        SetText(victorySubtitleText,     "vic_subtitle");
        SetText(endlessTitleText,        "endless_title");
        SetText(levelSelectTitleText,    "level_select_title");
        SetText(carSelectTitleText,      "car_select_title");
        SetText(bgmLabelText,           "settings_bgm");
        SetText(sfxLabelText,           "settings_sfx");
        SetText(zoneLabelOpacityLabel,  "settings_zone_labels");
        UpdateFullscreenButtonLabel();

        SetText(loginTitleText,         "login_title");
        SetText(loginSubtitleText,      "login_subtitle");

        RefreshButtonLocalization();
        RefreshLevelSelectLocalization();

        if (GameManager.Instance != null &&
            GameManager.Instance.State == GameState.Playing &&
            !string.IsNullOrEmpty(GameManager.Instance.CurrentDestination))
            UpdateOrder(GameManager.Instance.CurrentDestination, GameManager.Instance.IsRushOrder);
    }

    void RefreshButtonLocalization()
    {
        SetButtonLabel(loginConfirmButton,       "btn_confirm");
        SetButtonLabel(googleSignInButton,       "auth_google");
        SetButtonLabel(guestSignInButton,        "auth_guest");
        SetButtonLabel(loginBackButton,          "btn_back");
        SetButtonLabel(startButton,              "btn_start");
        SetButtonLabel(retryButton,              "btn_retry");
        SetButtonLabel(nextLevelButton,          "btn_go_next");
        SetButtonLabel(playAgainButton,          "btn_play_again");
        SetButtonLabel(resumeButton,             "btn_resume");
        SetButtonLabel(restartButton,            "btn_restart");
        SetButtonLabel(selectModeFromPauseButton,"btn_select_mode");
        SetButtonLabel(settingsButton,           "btn_settings");
        SetButtonLabel(startSettingsButton,      "btn_settings");
        SetButtonLabel(closeSettingsButton,      "btn_close");
        UpdateGoogleLinkStatus();
        SetButtonLabel(rushModeButton,           "mode_rush");
        SetButtonLabel(heartModeButton,         "mode_heart");
        SetButtonLabel(endlessModeButton,        "mode_endless");
        SetButtonLabel(heartExtremeModeButton,   "mode_heart_extreme");
        SetButtonLabel(rushExtremeModeButton,    "mode_rush_extreme");
        SetButtonLabel(endlessRetryButton,       "btn_retry");
        SetButtonLabel(endlessSelectModeButton,  "btn_select_mode");
        SetButtonLabel(modeBackToStartButton,    "btn_back");
        SetButtonLabel(carSelectBackButton,      "btn_back");
        SetButtonLabel(startCarSelectButton,     "car_select_title");
        SetText(leaderboardTitleText,            "lb_title");
        SetButtonLabel(leaderboardBackButton,    "btn_back");
        SetButtonLabel(startLeaderboardButton,         "btn_leaderboard");
        SetButtonLabel(modeSelectLeaderboardButton,    "btn_leaderboard");
        SetButtonLabel(gameOverLeaderboardButton,      "btn_leaderboard");
        SetButtonLabel(levelCompleteLeaderboardButton, "btn_leaderboard");
        SetButtonLabel(victoryLeaderboardButton,       "btn_leaderboard");
        SetButtonLabel(endlessLeaderboardButton,       "btn_leaderboard");
        if (leaderboardModeTabs != null)
        {
            string[] tabKeys = { "lb_mode_heart", "lb_mode_rush", "lb_mode_endless", "lb_mode_heart_extreme", "lb_mode_rush_extreme" };
            for (int i = 0; i < leaderboardModeTabs.Length && i < tabKeys.Length; i++)
                SetButtonLabel(leaderboardModeTabs[i], tabKeys[i]);
        }
    }

    void RefreshLevelSelectLocalization()
    {
        if (levelButtons != null)
        {
            string lockedStr = LocalizationManager.L("btn_locked", "LOCKED");
            foreach (var b in levelButtons)
            {
                if (b == null) continue;
                var lockT = b.transform.Find("Lock/Text");
                if (lockT != null) { var tmp = lockT.GetComponent<TextMeshProUGUI>(); if (tmp != null) tmp.text = lockedStr; }
            }
            if (levelSelectScreen != null)
            {
                SetChildText(levelSelectScreen, "BackBtn/Text", LocalizationManager.L("btn_back", "BACK"));
                SetChildText(levelSelectScreen, "Title",        LocalizationManager.L("level_select_title", "SELECT LEVEL"));
            }
        }
    }

    // ── Utility helpers ─────────────────────────────────────────────────────

    static void SetChildText(GameObject parent, string path, string text)
    {
        var child = parent.transform.Find(path);
        if (child == null) return;
        var tmp = child.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
    }

    void SetText(TextMeshProUGUI t, string key)
    {
        if (t == null) return;
        if (LocalizationManager.Instance == null) return;
        string v = LocalizationManager.Instance.Get(key);
        if (!string.IsNullOrEmpty(v)) t.text = v;
    }

    void SetButtonLabel(Button b, string key)
    {
        if (b == null) return;
        var t = b.GetComponentInChildren<TextMeshProUGUI>();
        if (t == null) return;
        if (LocalizationManager.Instance == null) return;
        string v = LocalizationManager.Instance.Get(key);
        if (!string.IsNullOrEmpty(v)) t.text = v;
    }

    TextMeshProUGUI FindOrCreateBestText(GameObject screen, string name)
    {
        if (screen == null) return null;
        var existing = screen.transform.Find(name);
        if (existing != null) return existing.GetComponent<TextMeshProUGUI>();
        return null;
    }

    static void AnchorFull(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void AnchorRect(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
