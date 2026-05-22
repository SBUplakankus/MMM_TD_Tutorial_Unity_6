# Episode 14 — UI Toolkit HUD

<!-- Video placeholder -->
<iframe width="560" height="315" src="https://www.youtube.com/embed/PLACEHOLDER_EP14" frameborder="0"></iframe>

---

## Learning Objectives

- Plan a data-driven HUD that reacts to game events via event channels
- Identify the UI elements needed for a tower defence HUD
- Map event channels to UI updates (gold, lives, wave number, etc.)
- Outline the UXML structure and USS styling for a HUD
- Design the UISetup workflow: bind event subscriptions, register visual elements, wire updates

## Key Concepts

- [Observer Pattern](../Concepts/Observer_Pattern.md) — event channels driving UI updates
- [Service Locator](../Concepts/Service_Locator.md) — accessing services from UI code

---

## What We're Starting With

Our game has a fully working combat loop, wave system, audio, strategies, and event channels. But there's **no UI**. The player can't see their gold, lives, the current wave number, or available towers. They have no way to pause. The game needs a heads-up display.

This episode is a **doc outline only**. You'll fill in the implementation yourself — the structure, event subscriptions, and visual design are described here, but the actual UXML, USS, and C# code are yours to write.

---

## HUD Elements Required

| Element | Data Source | Event Channel | Update Trigger |
|---------|------------|---------------|-----------------|
| Gold counter | `PlayerStats.Gold` | `EconomyEvents.GoldChanged` | Every gold change |
| Lives counter | `PlayerStats.Lives` | `EconomyEvents.LivesChanged` | Every life lost |
| Wave indicator | `WaveManager.CurrentWave` | `WaveEvents.WaveStarted` | Each new wave |
| Wave complete banner | — | `WaveEvents.WaveCompleted` | Wave cleared |
| Tower selection panel | Tower data SOs | — (static config) | On click |
| Pause button | — | `GameEvents.GamePaused` | Player action |
| Game Over screen | — | `GameEvents.GameOver` | All lives lost |
| Restart button | — | `GameEvents.GameRestart` | Player action |

---

## UXML Structure Outline

The HUD uses Unity UI Toolkit (UXML + USS). Plan your layout hierarchically:

```
HUD (full-screen overlay, position: absolute)
├── TopBar (horizontal strip)
│   ├── GoldDisplay
│   │   ├── GoldIcon (Image)
│   │   └── GoldLabel (Label)
│   ├── WaveDisplay
│   │   ├── WaveLabel (Label: "Wave 3")
│   │   └── WaveCompleteLabel (Label, hidden by default)
│   └── LivesDisplay
│       ├── LivesIcon (Image)
│       └── LivesLabel (Label)
├── TowerPanel (side panel, vertical)
│   ├── TowerButton_Arrow (Button)
│   ├── TowerButton_Bomb (Button)
│   └── TowerButton_Special (Button)
├── PauseButton (top-right corner, Button)
├── PauseOverlay (full-screen, hidden by default)
│   └── ResumeButton (Button)
└── GameOverOverlay (full-screen, hidden by default)
    ├── GameOverLabel (Label: "Game Over")
    └── RestartButton (Button)
```

Your job: create the `.uxml` file that defines this structure, and the `.uss` file that styles it. Position the top bar at the top, tower panel on the left or bottom, pause button in a corner.

---

## Event Subscriptions

The HUD controller subscribes to event channels in its `OnEnable` and unsubscribes in `OnDisable`:

| Event Channel | Method | UI Update |
|---------------|--------|-----------|
| `EconomyEvents.GoldChanged` | `OnGoldChanged(int newGold)` | Update `GoldLabel.text` |
| `EconomyEvents.LivesChanged` | `OnLivesChanged(int newLives)` | Update `LivesLabel.text` |
| `WaveEvents.WaveStarted` | `OnWaveStarted(int waveNumber)` | Update `WaveLabel.text`, hide wave-complete banner |
| `WaveEvents.WaveCompleted` | `OnWaveCompleted(int waveNumber)` | Show wave-complete banner, auto-hide after 2 seconds |
| `GameEvents.GamePaused` | `OnGamePaused()` | Show pause overlay, set `Time.timeScale = 0` |
| `GameEvents.GameOver` | `OnGameOver()` | Show game-over overlay |
| `GameEvents.GameRestart` | `OnGameRestart()` | Hide game-over overlay, reset `Time.timeScale = 1` |

**Pattern for each**: subscribe in `OnEnable`, unsubscribe in `OnDisable`, update a single visual element in the handler.

---

## UISetup Steps

Work through these steps in order. Each step depends on the previous one.

### Step 1 — Create the HUD UXML and USS files

1. In the Unity Editor, right-click in the Project window → Create → UI Toolkit → Panel Settings Asset
2. Create a new `.uxml` file for the HUD layout following the structure above
3. Create a new `.uss` file for HUD styles (colors, fonts, spacing, borders)
4. Create a `UIDocument` in the scene and assign the Panel Settings and UXML

### Step 2 — Create the HUDController C# script

1. Create `UI/HUDController.cs` as a `MonoBehaviour`
2. In `OnEnable`, get references to event registries via `Services.Get<T>()`
3. Subscribe to all event channels listed above
4. In `OnDisable`, unsubscribe from all channels

### Step 3 — Wire visual element references

1. Use `UIDocument.rootVisualElement` to query elements by name:
   - `root.Q<Label>("GoldLabel")`
   - `root.Q<Label>("LivesLabel")`
   - `root.Q<Label>("WaveLabel")`
   - etc.
2. Store these references in private fields of `HUDController`
3. Initialise display values from `Services.Get<PlayerStats>()` current state

### Step 4 — Implement event handlers

For each event subscription, write the handler that updates the corresponding visual element:

- Gold changes → update gold label text
- Lives changes → update lives label text (and flash red if lives decreased)
- Wave starts → update wave label, hide any banners from previous wave
- Wave completes → show wave-complete banner, hide after 2-second coroutine

### Step 5 — Implement pause and game-over overlays

- Pause button click → raise `GameEvents.GamePaused`
- `GamePaused` handler → show pause overlay, set `Time.timeScale = 0`
- Resume button click → set `Time.timeScale = 1`, hide pause overlay
- `GameOver` handler → show game-over overlay with final score
- Restart button click → raise `GameEvents.GameRestart`, reload scene

### Step 6 — Implement tower selection buttons

- Each tower button stores the `TowerData` ScriptableObject reference
- On click, set the "selected tower" state that `TowerPlacement` will read
- Visually highlight the selected button (add/remove USS class)

### Step 7 — Polish and iterate

- Add number-formatting for gold (e.g. "1,250" with comma separators)
- Animate label changes (scale bounce on gold change, colour flash on lives lost)
- Ensure all overlays block raycasts to prevent interaction with the game world behind them
- Test with `Time.timeScale = 0` — ensure pause overlay is responsive

---

## Architecture Context

```
┌────────────────────────────────────────────────────────────┐
│                      HUDController                         │
│                                                            │
│  OnEnable()                                                │
│    ├─ Services.Get<EconomyEvents>().GoldChanged.Subscribe   │
│    ├─ Services.Get<EconomyEvents>().LivesChanged.Subscribe  │
│    ├─ Services.Get<WaveEvents>().WaveStarted.Subscribe      │
│    ├─ Services.Get<WaveEvents>().WaveCompleted.Subscribe    │
│    ├─ Services.Get<GameEvents>().GamePaused.Subscribe       │
│    └─ Services.Get<GameEvents>().GameOver.Subscribe         │
│                                                            │
│  Visual Elements:                                          │
│    GoldLabel ◄── EconomyEvents.GoldChanged                 │
│    LivesLabel ◄── EconomyEvents.LivesChanged               │
│    WaveLabel ◄── WaveEvents.WaveStarted                     │
│    WaveBanner ◄── WaveEvents.WaveCompleted                 │
│    PauseOverlay ◄── GameEvents.GamePaused                  │
│    GameOverOverlay ◄── GameEvents.GameOver                   │
└────────────────────────────────────────────────────────────┘
```

The HUD is **entirely read-only**. It observes events and updates labels. It never directly modifies `PlayerStats` or `WaveManager` — it only raises events (pause, restart) that other systems handle.

---

## Episode Recap

- This episode was an **outline only** — you fill in the implementation
- HUD elements map 1:1 to event channels from Episodes 10 and 11
- Subscribe in `OnEnable`, unsubscribe in `OnDisable`
- Use `UIDocument.rootVisualElement.Q<T>("name")` to get visual element references
- The HUD is **read-only** — it observes and displays, never directly modifies game state
- Pause and game-over overlays manipulate `Time.timeScale` and block raycasts

---

## Challenge

1. Add a **tower range preview** that shows the range circle when a tower button is selected and the mouse hovers over a valid placement position. This requires responding to mouse events in UI Toolkit and communicating with the tower placement system — via an event channel, of course.

2. Add **animated number transitions** — when gold changes from 100 to 250, the label should count up over 0.3 seconds rather than snapping. Use `schedule.Execute()` in UI Toolkit to create a smooth counter animation.

3. Accessibility: add a **high-contrast mode** USS theme that can be toggled from a settings menu. Use USS custom properties (variables) for colors so switching themes only requires swapping the variable sheet.