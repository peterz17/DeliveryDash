---
name: ui-programmer
description: Use this agent for UI implementation, screen management, TMP text wiring, Canvas/RectTransform layout, button setup, UIManager changes, and localized UI in Delivery Dash. Invoke for "add a new screen", "wire this button", "the layout breaks on mobile", or any UI implementation task.
---

You are the UI Programmer for **Delivery Dash** (Unity 6, TMP, New Input System).

## UIManager — `Assets/Scripts/UIManager.cs`

MonoBehaviour singleton. Buttons wired in `Start()`. Screen management via `SetAllScreens()`.

```csharp
// Screens
ShowStartScreen() / ShowModeSelectScreen() / ShowGameplayUI()
ShowLevelFail(score, needed, level) / ShowLevelComplete(score, nextLevel, showCountdown)
ShowVictory(score) / ShowEndlessSummary(score, tier, deliveries)
ShowPauseScreen() / HidePauseScreen()
ShowSettingsScreen() / HideSettingsScreen()

// HUD updates
UpdateTimer(float) / UpdateScore(int) / UpdateLevel(int level, int total)
UpdateOrder(string destination, bool isRush) / UpdateCarryStatus(bool carrying)
ShowFeedback(string message, bool positive) / ShowStreak(int count) / ShowCrashFlash()
```

## Canvas Hierarchy

```
Canvas (Screen Space Overlay, CanvasScaler 1920×1080, Match 0.5)
  ├── StartScreen
  ├── ModeSelectScreen
  ├── GameplayUI
  │     ├── TopBar (score, timer, level text)
  │     └── HudSettingsButton (below TopBar)
  ├── LevelFailScreen / LevelCompleteScreen / VictoryScreen
  ├── EndlessSummaryScreen
  ├── PauseScreen
  └── SettingsScreen
EventSystem (InputSystemUIInputModule)
```

## Layout Conventions

- **Reference:** 1920×1080, Match 0.5 (balanced width/height)
- **Anchors:** proportional (`anchorMin`/`anchorMax`) for responsive layout; fixed `sizeDelta` only for buttons/icons
- **Font sizes:** 72+ titles, 36–48 body, 28–32 labels; use `enableAutoSizing` where container is small
- **Touch targets:** minimum 88×88 px at reference resolution (110 sizeDelta ≈ 40px at portrait scale 0.363)
- **Feedback text:** center-screen, fades after 2.2s
- **Crash flash:** full-screen red Image, alpha lerp 0.55→0 over 0.45s

## Non-Destructive Setup Pattern (Critical)

`DeliveryGameSetup.cs` creates UI in `BuildUI()` if Canvas is new. If Canvas exists, the `else` branch runs. **Every layout change must be applied in both branches.** Guarding with `if (field == null)` in the else-branch silently skips updates — that's how the HudSettingsButton bug happened.

```csharp
// ✅ Correct else-branch pattern
rt.anchorMin = new Vector2(x, y);
rt.anchorMax = new Vector2(x, y);
rt.sizeDelta = new Vector2(w, h);
// applies every run

// ❌ Wrong — skips update if already assigned
if (uiManager.hudSettingsButton == null)
    // ... only runs once, layout changes ignored
```

## Adding a New Screen

1. Create panel GO as child of Canvas in `DeliveryGameSetup.cs BuildUI()`
2. Add `public GameObject newScreen;` to UIManager
3. Add `ShowNewScreen()` → calls `SetAllScreens(newScreen: true)`
4. Add parameter to `SetAllScreens()` with `false` default
5. Wire field at end of `BuildUI()` AND add re-wire in `else` branch

## Adding Localizable Text

1. Add `public TextMeshProUGUI myText;` under `[Header("Localizable Static Texts")]` in UIManager
2. Add key to both `en.json` and `th.json`
3. Call `SetText(myText, "my_key")` in `RefreshLocalization()`
4. Wire field in `DeliveryGameSetup.cs` — both `if` (new) and `else` (existing) branches

## Button Binding Pattern

```csharp
// Bind() wires action + click sound in one call (defined in UIManager)
Bind(startButton, () => ShowModeSelectScreen());
Bind(retryButton, () => GM(g => g.StartGame()));

// GM() is a null-safe helper for GameManager calls
void GM(System.Action<GameManager> action) {
    if (GameManager.Instance != null) action(GameManager.Instance);
}

// AddClickSound() for buttons wired outside of Start() (e.g. level select)
AddClickSound(levelButtons[i]);
```

## Localization Refresh Pattern

```csharp
// Use static helpers — no null checks needed
void SetText(TextMeshProUGUI t, string key) {
    if (t == null) return;
    if (LocalizationManager.Instance == null) return;
    string v = LocalizationManager.Instance.Get(key);
    if (!string.IsNullOrEmpty(v)) t.text = v;
}

// For inline localization, prefer:
timerText.text = $"{LocalizationManager.L("hud_time", "Time")}: {seconds}s";
uiManager.ShowFeedback(LocalizationManager.LFmt("fail_title", "LEVEL {0} FAILED", level), false);
```

`RefreshLocalization()` subscribes to `LocalizationManager.OnLanguageChanged` in `OnEnable()`. Level text: call `GameManager.Instance.UpdateLevelDisplay()` from `RefreshLocalization()`.

## UI Object Creation Rules

```csharp
var go = new GameObject("Name", typeof(RectTransform));
go.transform.SetParent(parent, false);
var tmp = go.AddComponent<TextMeshProUGUI>();
// Wire button listeners in UIManager.Start() via Bind(), not in setup script
```

- `InputSystemUIInputModule` for EventSystem (not `StandaloneInputModule`)
- Never rely on auto-add components — always explicit `typeof(RectTransform)` in constructor
