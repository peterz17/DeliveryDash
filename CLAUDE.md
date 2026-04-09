# Delivery Dash — Unity Project

## Project
Top-down 2D delivery game. Unity 6 (6000.3.11f1), URP 2D, new Input System.

## Key Files
```
Assets/Scripts/
  GameManager.cs        — singleton, state machine, score, orders, level progression
  PlayerController.cs   — WASD/arrows via Keyboard.current, Rigidbody2D.linearVelocity
  UIManager.cs          — TMP UI, all screens, feedback, localization refresh
  LocalizationManager.cs — loads en.json/th.json from Resources, fires OnLanguageChanged
  ZoneLabelLocalizer.cs — attaches to world-space TextMesh zones, refreshes on language change
  PickupZone.cs         — OnTriggerEnter2D, gives player package
  DeliveryZone.cs       — OnTriggerEnter2D, checks destinationName vs CurrentDestination
  CameraFollow.cs       — LateUpdate lerp + bounds clamp
Assets/Editor/
  DeliveryGameSetup.cs  — MenuItem "Delivery Dash/Setup Scene", non-destructive scene builder
Assets/Resources/Localization/
  en.json / th.json     — all UI strings, loaded via Resources.Load<TextAsset>
```

## Architecture
- `GameManager.Instance` is the single source of truth — State, Score, TimeRemaining, CurrentDestination
- `UIManager` wires button listeners in `Start()` via serialized refs; calls `RefreshLocalization()` on language change
- `LocalizationManager` fires `OnLanguageChanged` static event — all UI subscribes to it
- World-space zone labels use `TextMesh` + `ZoneLabelLocalizer` (subscribes to `OnLanguageChanged`)
- Screen-space UI uses `TextMeshProUGUI` — requires TMP Essential Resources imported
- `DeliveryGameSetup.cs` is **non-destructive**: always updates existing objects in-place

## Unity API Rules (Unity 6)
- Use `rb.linearVelocity` not `rb.velocity` (deprecated)
- Use `Keyboard.current` from `UnityEngine.InputSystem` not `Input.GetAxis`
- Use `InputSystemUIInputModule` not `StandaloneInputModule` for EventSystem
- Use `Object.FindObjectsByType<T>(FindObjectsSortMode.None)` not `FindObjectsOfType`
- Do NOT use `??` with Unity objects — use explicit `if (x == null)` checks
- Create UI GameObjects with `new GameObject(name, typeof(RectTransform))` — do not rely on auto-add

## Localization Rules
- **Every visible string must use a localization key** — no hardcoded Thai or English text in scripts
- Keys live in `en.json` and `th.json` under `Assets/Resources/Localization/`
- Retrieve strings with `LocalizationManager.Instance.Get("key")`
- Dynamic strings use `string.Format(Get("key"), arg0, arg1)` — keys use `{0}`, `{1}` placeholders
- When adding a new UI element, add its key to **both** JSON files immediately
- `ZoneLabelLocalizer` handles world-space TextMesh labels — set `localizationKey` in the component
- `RefreshLocalization()` in UIManager must be called and must cover every static label

## UI Layout Rules (responsive)
- **Never use fixed `sizeDelta` for buttons/panels** unless the element is intentionally a fixed pixel size that scales with canvas
- Use **proportional anchors** (`anchorMin`/`anchorMax`) so elements scale correctly at all resolutions
- Canvas reference resolution: 1920×1080, `matchWidthOrHeight = 0.5`
- At portrait 424×645, canvas scale ≈ 0.36 — so `sizeDelta = 110` ≈ 40px actual
- `DeliveryGameSetup.cs` else-branch **must always re-apply anchors and font sizes** — do not guard with `if (field == null)` for layout properties

## Scene Setup
Run **Delivery Dash → Setup Scene** in Unity menu. Safe to re-run at any time.
- Updates existing objects in-place (non-destructive)
- Safe to re-run at any time — else-branch must reset all values, not just reposition

## Gameplay
State: StartScreen → ModeSelect → Playing → LevelComplete / LevelFail / Victory / EndlessSummary
- Modes: Normal (score target per level), Rush (auto-advance on target), Endless (infinite tiers)
- Pick up at PICKUP zone (center); deliver to correct colored zone
- Wrong delivery: feedback shown, package kept
- Power-ups: Shield, Rocket (speed), Clock (time freeze)

## Coding Conventions
- **Readable over clever** — variable names explain what they are, not how they work
- **Dynamic data, not hardcoded** — use localization keys, level data arrays, and config values; never embed strings or magic numbers inline
- No docstrings or comments unless logic is genuinely non-obvious
- No defensive null checks for internal Unity refs wired in setup
- Keep scripts single-responsibility: GameManager does not touch transforms, UIManager does not compute game logic
- Prefer event-driven patterns (`OnLanguageChanged`, `OnTriggerEnter2D`) over polling in `Update()`

## What NOT to Do
- **No hardcoded display strings** — always go through `LocalizationManager.Get()`
- **No `Update()` polling** for event-driven things — use callbacks, events, triggers
- **No per-frame loops over scene objects** — cache in `Awake`/`Start`, O(n²) kills performance
- **No `while`/`for` loops per-frame** — use Coroutines with `yield` for time-based sequences
- **No infinite Coroutines** without a clear exit condition
- **No layout values that only work at one resolution** — always use proportional anchors
- **No else-branch shortcuts** in `DeliveryGameSetup` — if a property changed, the else branch must also update it
