using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
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
    public Button normalModeButton;
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

    [Header("Localizable Static Texts")]
    public TextMeshProUGUI startTitleText;
    public TextMeshProUGUI settingsTitleText;
    public TextMeshProUGUI volumeLabelText;
    public TextMeshProUGUI modeSelectTitleText;
    public TextMeshProUGUI rushModeDescText;
    public TextMeshProUGUI normalModeDescText;
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

    readonly Dictionary<string, Coroutine> powerUpTimerCoroutines = new Dictionary<string, Coroutine>();
    readonly Dictionary<string, TextMeshProUGUI> powerUpSlotTexts  = new Dictionary<string, TextMeshProUGUI>();

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

    void Start()
    {
        Bind(startButton,               () => ShowModeSelectScreen());
        Bind(retryButton,               () => GM(g => g.StartGame()));
        Bind(resumeButton,              () => GM(g => g.ResumeGame()));
        Bind(restartButton,             () => GM(g => g.StartGame()));
        Bind(nextLevelButton,           () => GM(g => g.OnNextLevelButton()));
        Bind(selectModeFromPauseButton, () => GM(g => g.ReturnToMainMenu()));
        Bind(playAgainButton,           () => ShowModeSelectScreen());
        Bind(rushModeButton,            () => GM(g => g.StartWithMode(GameMode.Rush)));
        Bind(normalModeButton,          () => GM(g => g.StartWithMode(GameMode.Normal)));
        Bind(endlessModeButton,         () => GM(g => g.StartWithMode(GameMode.Endless)));
        Bind(heartExtremeModeButton,    () => GM(g => g.StartWithMode(GameMode.HeartExtreme)));
        Bind(rushExtremeModeButton,     () => GM(g => g.StartWithMode(GameMode.RushExtreme)));
        Bind(endlessSelectModeButton,   () => ShowModeSelectScreen());
        Bind(endlessRetryButton,        () => GM(g => g.StartWithMode(GameMode.Endless)));
        Bind(modeBackToStartButton,     () => ShowStartScreen());
        Bind(settingsButton,            () => ShowSettingsScreen());
        Bind(startSettingsButton,       () => ShowSettingsScreen());
        Bind(hudSettingsButton,         () => GM(g => g.PauseGame()));
        Bind(closeSettingsButton,       () => HideSettingsScreen());
        Bind(languageButton,            () => { if (LocalizationManager.Instance != null) LocalizationManager.Instance.ToggleLanguage(); UpdateLanguageButtonText(); });
        if (volumeSlider != null)
        {
            if (AudioManager.Instance != null) volumeSlider.value = AudioManager.Instance.MasterVolume;
            volumeSlider.onValueChanged.AddListener(v => AudioManager.Play(a => a.SetMasterVolume(v)));
        }

        // Auto-find LevelSelectScreen if reference is missing
        if (levelSelectScreen == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length > 0
                ? Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0] : null;
            if (canvas != null)
            {
                foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                    if (t.gameObject.name == "LevelSelectScreen")
                    { levelSelectScreen = t.gameObject; break; }
            }
        }

        BuildLevelSelectIfNeeded();
        BuildPowerUpHudIfNeeded();
        WireSettingsListeners();

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
        RefreshLocalization();
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshLocalization;
        PowerUpManager.OnPowerUpActivated   += HandlePowerUpActivated;
        PowerUpManager.OnPowerUpDeactivated += HandlePowerUpDeactivated;
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshLocalization;
        PowerUpManager.OnPowerUpActivated   -= HandlePowerUpActivated;
        PowerUpManager.OnPowerUpDeactivated -= HandlePowerUpDeactivated;
    }

    public void ShowStartScreen()
    {
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
    }

    public void ClearPowerUpTimers()
    {
        foreach (var kv in powerUpTimerCoroutines)
            if (kv.Value != null) StopCoroutine(kv.Value);
        powerUpTimerCoroutines.Clear();
        foreach (var kv in powerUpSlotTexts)
            if (kv.Value != null) kv.Value.gameObject.SetActive(false);
        if (powerUpHudPanel != null) powerUpHudPanel.SetActive(false);
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

    public void ShowSettingsScreen()
    {
        if (settingsScreen != null) settingsScreen.SetActive(true);
    }

    public void HideSettingsScreen()
    {
        if (settingsScreen != null) settingsScreen.SetActive(false);
    }

    void UpdateLanguageButtonText()
    {
        if (languageButtonText == null) return;
        bool isEng = LocalizationManager.Instance == null || LocalizationManager.Instance.CurrentLanguage == LocalizationManager.Language.English;
        languageButtonText.text = isEng
            ? LocalizationManager.L("lang_current_en", "Language: EN")
            : LocalizationManager.L("lang_current_th", "ภาษา: TH");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

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

    public void RefreshLocalization()
    {
        // HUD
        UpdateLanguageButtonText();
        if (GameManager.Instance != null && levelText != null)
            GameManager.Instance.UpdateLevelDisplay();

        // Static screen texts
        SetText(startTitleText,          "start_title");
        SetText(startSubtitleText,       "start_subtitle");
        SetText(settingsTitleText,       "settings_title");
        SetText(volumeLabelText,         "settings_volume");
        SetText(modeSelectTitleText,     "select_mode_title");
        SetText(rushModeDescText,        "mode_rush_desc");
        SetText(normalModeDescText,      "mode_normal_desc");
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
        SetText(bgmLabelText,           "settings_bgm");
        SetText(sfxLabelText,           "settings_sfx");
        SetText(zoneLabelOpacityLabel,  "settings_zone_labels");
        UpdateFullscreenButtonLabel();

        // Button labels
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
        SetButtonLabel(rushModeButton,           "mode_rush");
        SetButtonLabel(normalModeButton,         "mode_normal");
        SetButtonLabel(endlessModeButton,        "mode_endless");
        SetButtonLabel(heartExtremeModeButton,   "mode_heart_extreme");
        SetButtonLabel(rushExtremeModeButton,    "mode_rush_extreme");
        SetButtonLabel(endlessRetryButton,       "btn_retry");
        SetButtonLabel(endlessSelectModeButton,  "btn_select_mode");
        SetButtonLabel(modeBackToStartButton,    "btn_back");

        // Refresh level-select lock and back button labels
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

        // Refresh order if playing
        if (GameManager.Instance != null &&
            GameManager.Instance.State == GameState.Playing &&
            !string.IsNullOrEmpty(GameManager.Instance.CurrentDestination))
            UpdateOrder(GameManager.Instance.CurrentDestination, GameManager.Instance.IsRushOrder);
    }

    public void ShowPauseScreen()
    {
        if (pauseScreen != null) pauseScreen.SetActive(true);
    }

    public void HidePauseScreen()
    {
        if (pauseScreen != null) pauseScreen.SetActive(false);
    }

    void SetAllScreens(bool start = false, bool modeSelect = false, bool gameplay = false,
                       bool gameOver = false, bool levelComplete = false, bool victory = false,
                       bool endlessSummary = false, bool levelSelect = false)
    {
        if (startScreen != null)          startScreen.SetActive(start);
        if (modeSelectScreen != null)     modeSelectScreen.SetActive(modeSelect);
        if (gameplayUI != null)           gameplayUI.SetActive(gameplay);
        if (gameOverScreen != null)       gameOverScreen.SetActive(gameOver);
        if (levelCompleteScreen != null)  levelCompleteScreen.SetActive(levelComplete);
        if (victoryScreen != null)        victoryScreen.SetActive(victory);
        if (endlessSummaryScreen != null) endlessSummaryScreen.SetActive(endlessSummary);
        if (levelSelectScreen != null)    levelSelectScreen.SetActive(levelSelect);
        if (pauseScreen != null)          pauseScreen.SetActive(false);
        if (settingsScreen != null)       settingsScreen.SetActive(false);
    }

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

        // Fade in 0.12s
        for (float t = 0f; t < 0.12f; t += Time.unscaledDeltaTime)
        {
            feedbackText.color = new Color(base_.r, base_.g, base_.b, t / 0.12f);
            yield return null;
        }
        feedbackText.color = base_;

        yield return new WaitForSecondsRealtime(0.65f);

        // Fade out 0.30s
        for (float t = 0f; t < 0.30f; t += Time.unscaledDeltaTime)
        {
            feedbackText.color = new Color(base_.r, base_.g, base_.b, 1f - t / 0.30f);
            yield return null;
        }
        feedbackText.gameObject.SetActive(false);
        feedbackCoroutine = null;
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

    // ── Level Select ──────────────────────────────────────────────────────────

    public void ShowLevelSelectScreen(GameMode mode)
    {
        BuildLevelSelectIfNeeded();
        SetAllScreens(levelSelect: true);
        RefreshLevelButtons(mode);
    }

    void RefreshLevelButtons(GameMode mode)
    {
        if (levelButtons == null) return;
        int unlocked = GameManager.GetUnlockedLevel(mode);
        string timeFmt  = LocalizationManager.L("level_select_time_fmt",  "{0}s");
        string scoreFmt = LocalizationManager.L("level_select_score_fmt", "{0}pts");
        bool showLives = mode == GameMode.Normal || mode == GameMode.HeartExtreme;

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

            var level = GameManager.Levels[i];
            string info = string.Format(timeFmt, (int)level.time) + "  " + string.Format(scoreFmt, level.scoreNeeded);
            if (showLives) info += "  \u2665" + level.lives;
            subTMP.text = info;
        }
    }

    void BuildLevelSelectIfNeeded()
    {
        if (levelSelectScreen == null) return;
        if (levelButtons != null && levelButtons.Length > 0) return;

        // Load buttons from scene (only Btn* names, exclude BackBtn)
        var allBtns = levelSelectScreen.GetComponentsInChildren<Button>(true);
        var list = new System.Collections.Generic.List<Button>();
        foreach (var b in allBtns)
            if (b.gameObject.name.StartsWith("Btn")) list.Add(b);
        levelButtons = list.ToArray();

        // Wire onClick listeners
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelIdx = i;
            levelButtons[i].onClick.RemoveAllListeners();
            levelButtons[i].onClick.AddListener(() => GameManager.Instance.StartAtLevel(levelIdx));
            AddClickSound(levelButtons[i]);
        }

        // Wire back button
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

    public void ShowTutorialHints()
    {
        ShowFeedback(LocalizationManager.L("tutorial_pickup", "Pick up packages here!"), true);
        StartCoroutine(DelayedTutorialHint(LocalizationManager.L("tutorial_deliver", "Deliver to the glowing zone!")));
    }

    IEnumerator DelayedTutorialHint(string message)
    {
        yield return new WaitForSecondsRealtime(2f);
        ShowFeedback(message, true);
    }

    // ── Power-up HUD ──────────────────────────────────────────────────────────

    void BuildPowerUpHudIfNeeded()
    {
        if (powerUpHudPanel != null) return;
        if (gameplayUI == null) return;

        var panel = new GameObject("PowerUpHudPanel", typeof(RectTransform));
        panel.transform.SetParent(gameplayUI.transform, false);
        AnchorRect(panel, new Vector2(0f, 0.68f), new Vector2(0.24f, 0.872f));

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);

        var vlg = panel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(8, 4, 4, 4);

        var fitter = panel.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        powerUpHudPanel = panel;
        panel.SetActive(false);
    }

    TextMeshProUGUI GetOrCreatePowerUpSlot(string typeName)
    {
        if (powerUpSlotTexts.ContainsKey(typeName))
            return powerUpSlotTexts[typeName];

        if (powerUpHudPanel == null) BuildPowerUpHudIfNeeded();
        if (powerUpHudPanel == null) return null;

        var slotGO = new GameObject($"Slot_{typeName}", typeof(RectTransform));
        slotGO.transform.SetParent(powerUpHudPanel.transform, false);
        var le = slotGO.AddComponent<UnityEngine.UI.LayoutElement>();
        le.preferredHeight = 36f;
        var tmp = slotGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize    = 28;
        tmp.fontStyle   = FontStyles.Bold;
        tmp.alignment   = TextAlignmentOptions.MidlineLeft;
        tmp.color       = Color.white;
        tmp.raycastTarget = false;
        powerUpSlotTexts[typeName] = tmp;
        return tmp;
    }

    void HandlePowerUpActivated(string typeName, float duration)
    {
        string label = LocalizationManager.L("powerup_timer_" + typeName, typeName);
        if (powerUpTimerCoroutines.ContainsKey(typeName) && powerUpTimerCoroutines[typeName] != null)
            StopCoroutine(powerUpTimerCoroutines[typeName]);
        powerUpTimerCoroutines[typeName] = StartCoroutine(PowerUpTimerRoutine(typeName, label, duration));
    }

    void HandlePowerUpDeactivated(string typeName)
    {
        if (powerUpTimerCoroutines.ContainsKey(typeName) && powerUpTimerCoroutines[typeName] != null)
        {
            StopCoroutine(powerUpTimerCoroutines[typeName]);
            powerUpTimerCoroutines[typeName] = null;
        }
        if (powerUpSlotTexts.ContainsKey(typeName))
            powerUpSlotTexts[typeName].gameObject.SetActive(false);
        RefreshPowerUpHudVisibility();
    }

    IEnumerator PowerUpTimerRoutine(string typeName, string label, float duration)
    {
        var slot = GetOrCreatePowerUpSlot(typeName);
        if (slot == null) yield break;

        if (powerUpHudPanel != null) powerUpHudPanel.SetActive(true);
        slot.gameObject.SetActive(true);

        float remaining = duration;
        while (remaining > 0f)
        {
            slot.text = $"{label}  {remaining:0.0}s";
            float lerp = remaining / duration;
            slot.color = Color.Lerp(new Color(1f, 0.4f, 0.2f), Color.white, lerp);
            yield return null;
            remaining -= Time.deltaTime;
        }

        slot.gameObject.SetActive(false);
        if (powerUpTimerCoroutines.ContainsKey(typeName))
            powerUpTimerCoroutines[typeName] = null;
        RefreshPowerUpHudVisibility();
    }

    void RefreshPowerUpHudVisibility()
    {
        if (powerUpHudPanel == null) return;
        bool anyActive = false;
        foreach (var kv in powerUpSlotTexts)
        {
            if (kv.Value != null && kv.Value.gameObject.activeSelf)
            {
                anyActive = true;
                break;
            }
        }
        powerUpHudPanel.SetActive(anyActive);
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

    // ── Best score helper ──────────────────────────────────────────────────

    TextMeshProUGUI FindOrCreateBestText(GameObject screen, string name)
    {
        if (screen == null) return null;
        var existing = screen.transform.Find(name);
        if (existing != null) return existing.GetComponent<TextMeshProUGUI>();

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(screen.transform, false);
        AnchorRect(go, new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.36f));
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.8f, 0.8f, 0.3f);
        tmp.raycastTarget = false;
        return tmp;
    }

    // ── Settings extras (BGM/SFX sliders + fullscreen) ─────────────────────

    void WireSettingsListeners()
    {
        if (settingsScreen == null) return;

        // Wire zone label opacity slider listener
        if (zoneLabelOpacitySlider != null)
        {
            zoneLabelOpacitySlider.onValueChanged.RemoveAllListeners();
            zoneLabelOpacitySlider.value = PlayerPrefs.GetFloat("ZoneLabelOpacity", 1f);
            zoneLabelOpacitySlider.onValueChanged.AddListener(v =>
            {
                PlayerPrefs.SetFloat("ZoneLabelOpacity", v);
                ZoneLabelLocalizer.SetGlobalOpacity(v);
            });
            ZoneLabelLocalizer.SetGlobalOpacity(zoneLabelOpacitySlider.value);
        }

        // Wire fullscreen button
        if (fullscreenToggleButton != null)
        {
            fullscreenToggleButton.onClick.RemoveAllListeners();
            fullscreenToggleButton.onClick.AddListener(() => { Screen.fullScreen = !Screen.fullScreen; UpdateFullscreenButtonLabel(); });
            AddClickSound(fullscreenToggleButton);
            UpdateFullscreenButtonLabel();
        }
    }

    static void ShiftChild(GameObject parent, string childName, float newY)
    {
        var child = parent.transform.Find(childName);
        if (child == null) return;
        var rt = child.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, newY);
    }

    // Settings slider/label helpers removed — now created by DeliveryGameSetup.cs

    void UpdateFullscreenButtonLabel()
    {
        if (fullscreenLabelText == null) return;
        string label = LocalizationManager.L("settings_fullscreen", "Fullscreen");
        fullscreenLabelText.text = Screen.fullScreen ? "[ON] " + label : "[OFF] " + label;
    }

    // ── Interactive tutorial ────────────────────────────────────────────────

    public void StartInteractiveTutorial()
    {
        StartCoroutine(InteractiveTutorialRoutine());
    }

    IEnumerator InteractiveTutorialRoutine()
    {
        var gm = GameManager.Instance;
        if (gm == null) yield break;

        Canvas canvas = null;
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { canvas = c; break; }
        if (canvas == null) yield break;

        // Build tutorial bar at bottom of screen
        var overlayGO = new GameObject("TutorialOverlay", typeof(RectTransform));
        overlayGO.transform.SetParent(canvas.transform, false);
        AnchorRect(overlayGO, new Vector2(0f, 0f), new Vector2(1f, 0.12f));
        var bg = overlayGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);

        var textGO = new GameObject("InstrText", typeof(RectTransform));
        textGO.transform.SetParent(overlayGO.transform, false);
        AnchorRect(textGO, new Vector2(0.02f, 0f), new Vector2(0.78f, 1f));
        var instrText = textGO.AddComponent<TextMeshProUGUI>();
        instrText.fontSize = 28;
        instrText.alignment = TextAlignmentOptions.MidlineLeft;
        instrText.color = new Color(1f, 1f, 0.6f);
        instrText.margin = new Vector4(12f, 0f, 4f, 0f);
        instrText.raycastTarget = false;

        var skipGO = new GameObject("SkipBtn", typeof(RectTransform));
        skipGO.transform.SetParent(overlayGO.transform, false);
        AnchorRect(skipGO, new Vector2(0.80f, 0.15f), new Vector2(0.98f, 0.85f));
        var skipImg = skipGO.AddComponent<Image>();
        skipImg.color = new Color(0.35f, 0.35f, 0.40f, 1f);
        var skipBtn = skipGO.AddComponent<Button>();
        skipBtn.targetGraphic = skipImg;
        var skipTxtGO = new GameObject("Text", typeof(RectTransform));
        skipTxtGO.transform.SetParent(skipGO.transform, false);
        AnchorFull(skipTxtGO);
        var skipTmp = skipTxtGO.AddComponent<TextMeshProUGUI>();
        skipTmp.text = LocalizationManager.L("btn_skip", "SKIP");
        skipTmp.fontSize = 22;
        skipTmp.fontStyle = FontStyles.Bold;
        skipTmp.alignment = TextAlignmentOptions.Center;
        skipTmp.color = Color.white;
        skipTmp.raycastTarget = false;

        bool skipped = false;
        skipBtn.onClick.AddListener(() => { skipped = true; });

        instrText.text = LocalizationManager.L("tutorial_pickup", "Pick up packages here!");
        overlayGO.SetActive(true);
        yield return new WaitUntil(() => skipped || gm.player == null || gm.player.HasPackage);

        if (!skipped)
        {
            instrText.text = LocalizationManager.L("tutorial_deliver", "Deliver to the glowing zone!");
            yield return new WaitUntil(() => skipped || gm.player == null || !gm.player.HasPackage);

            if (!skipped)
            {
                instrText.text = LocalizationManager.L("tutorial_done", "Great job! You're ready!");
                yield return new WaitForSecondsRealtime(1.8f);
            }
        }

        PlayerPrefs.SetInt("TutorialDone", 1);
        PlayerPrefs.Save();
        overlayGO.SetActive(false);
        Destroy(overlayGO);
    }
}
