# Appendix: Performance & Polish — Implementation Guide

## What You're Building

A tiered UpdateManager that groups IUpdatables by priority and ticks them at different intervals — high-priority every frame, medium every 0.15s, low every 0.4s. Also a GameConstants static class for magic numbers that keep escaping into inspector fields. And PrimeTween integration for juice: health bar scale punches on damage, button hover feedback.

---

## Files & Order

1. `Assets/Scripts/Systems/Managers/UpdateManager.cs` — FULL implementation
2. `Assets/Scripts/Systems/Game/GameConstants.cs` — NEW file, FULL implementation

---

## Implementation

### 1. UpdateManager.cs — full implementation

```csharp
using System.Collections.Generic;
using Interfaces;
using UnityEngine;

namespace Systems.Managers
{
    public enum UpdatePriority
    {
        High,
        Medium,
        Low
    }

    public class UpdateManager : MonoBehaviour
    {
        #region Fields

        private readonly List<IUpdatable> _high = new();
        private readonly List<IUpdatable> _medium = new();
        private readonly List<IUpdatable> _low = new();

        private float _mediumInterval = 0.15f;
        private float _lowInterval = 0.4f;

        private float _mediumTimer;
        private float _lowTimer;

        #endregion

        #region Properties

        public static UpdateManager Instance { get; private set; }

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _mediumTimer = _mediumInterval;
            _lowTimer = _lowInterval;
        }

        private void Update()
        {
            TickList(_high);

            _mediumTimer -= Time.deltaTime;
            if (_mediumTimer <= 0f)
            {
                TickList(_medium);
                _mediumTimer = _mediumInterval;
            }

            _lowTimer -= Time.deltaTime;
            if (_lowTimer <= 0f)
            {
                TickList(_low);
                _lowTimer = _lowInterval;
            }
        }

        #endregion

        #region Public API

        public void Register(IUpdatable updatable, UpdatePriority priority)
        {
            var list = GetList(priority);
            if (!list.Contains(updatable))
                list.Add(updatable);
        }

        public void Unregister(IUpdatable updatable, UpdatePriority priority)
        {
            GetList(priority).Remove(updatable);
        }

        #endregion

        #region Private Methods

        private List<IUpdatable> GetList(UpdatePriority priority)
        {
            return priority switch
            {
                UpdatePriority.High => _high,
                UpdatePriority.Medium => _medium,
                UpdatePriority.Low => _low,
                _ => _low
            };
        }

        private static void TickList(List<IUpdatable> list)
        {
            for (int i = 0; i < list.Count; i++)
                list[i].Tick();
        }

        #endregion
    }
}
```

**Design decisions:**

- **Three separate List fields** instead of a Dictionary. Fewer allocations, no boxing on enum keys, direct field access in the hot path. The Dictionary approach in the original stub was over-engineered for 3 lists.

- **Timer counts down** from the interval. Initialize timers to their intervals in Awake so the first tick fires at the correct time (not 0.15s + whatever-Update-delay).

- **Contains check in Register** prevents double-registration. If the same object registers twice, it won't get Tick called twice per interval.

- **IUpdatable.Tick() takes no deltaTime parameter.** If an IUpdatable needs deltaTime, it reads `Time.deltaTime` itself. This matches the existing `IUpdatable` interface. If you want to pass deltaTime later, change the interface — but that means updating every implementation.

- **No collection-safe iteration issues.** `TickList` uses an indexed for-loop, not foreach. If an IUpdatable unregisters itself during its own Tick, the index shift could skip the next item. For this project it's fine — unregistering during Tick is rare. If it becomes a problem, iterate backwards.

---

### 2. GameConstants.cs — new file

```csharp
namespace Systems.Game
{
    public static class GameConstants
    {
        #region Gameplay

        public const int StartingCash = 100;
        public const int StartingHealth = 20;
        public const float WaveInterval = 5f;

        #endregion

        #region UIToolkit Styles

        public static class UIToolkitStyles
        {
            public const string Container = "container";
            public const string PanelBody = "panel-body";
            public const string PanelHeader = "panel-header";
            public const string ButtonPrimary = "btn-primary";
            public const string ButtonSecondary = "btn-secondary";
            public const string ButtonIcon = "btn-icon";
            public const string LabelTitle = "lbl-title";
            public const string LabelBody = "lbl-body";
            public const string HealthBar = "health-bar";
            public const string HealthBarFill = "health-bar__fill";
            public const string GoldDisplay = "gold-display";
            public const string WaveDisplay = "wave-display";
            public const string TowerNode = "tower-node";
            public const string ShopItem = "shop-item";
            public const string Tooltip = "tooltip";
        }

        #endregion

        #region Pooling

        public const string PoolAudioSource = "audioSource";
        public const string PoolProjectile = "projectile";
        public const string PoolEnemy = "enemy";
        public const string PoolVFX = "vfx";

        #endregion
    }
}
```

**Usage:** Replace hardcoded strings like `"audioSource"` with `GameConstants.PoolAudioSource`. Replace magic numbers like `100` for starting cash with `GameConstants.StartingCash`. USS class names live in `GameConstants.UIToolkitStyles` so you get compile-time checking instead of typo-prone string literals scattered across C# files.

---

## PrimeTween Usage

### Install

1. Open Package Manager → Add package from git URL:
   `https://github.com/KyryloKuzyk/PrimeTween.git`
2. Wait for install, verify no console errors.
3. PrimeTween works out of the box — no setup required. It auto-initializes on first use.

### Verify it's working

Create a test script, attach to any GameObject:

```csharp
using PrimeTween;
using UnityEngine;

public class PrimeTweenTest : MonoBehaviour
{
    private void Start()
    {
        Tween.PositionY(transform, 2f, duration: 1f);
    }
}
```

If the object moves up 2 units over 1 second, PrimeTween is installed and working. Delete the test script after.

### Health bar scale punch on damage

When an enemy takes damage, punch its health bar scale to create a "hit" reaction. Add this to `EnemyHealthBar`:

```csharp
using PrimeTween;
using UnityEngine;

namespace Enemies.Components
{
    public class EnemyHealthBar : MonoBehaviour
    {
        #region Fields

        [SerializeField] private Transform barFill;
        [SerializeField] private float punchScale = 1.3f;
        [SerializeField] private float punchDuration = 0.15f;

        private Tween _punchTween;

        #endregion

        #region Public API

        public void OnDamageTaken()
        {
            _punchTween.Stop();
            _punchTween = Tween.Scale(barFill, Vector3.one * punchScale, punchDuration,
                Ease.OutBack, 2, CycleMode.Rewind);
        }

        #endregion
    }
}
```

**Wire it:** In `EnemyController.TakeDamage`, after `Health.TakeDamage(this, damage)`, call `healthBar.OnDamageTaken()`.

**How it works:** `Tween.Scale` with `2` cycles and `CycleMode.Rewind` means: scale up to 1.3x (cycle 1), then scale back to 1.0x (cycle 2, rewind). The `Ease.OutBack` gives a slight overshoot for juice. Stopping the previous tween prevents stacking if damage comes in rapid succession.

### Button scale on hover (UIToolkit)

PrimeTween works with UI Toolkit VisualElements via the `PrimeTween.UnityUI` namespace extension:

```csharp
using PrimeTween;
using UnityEngine.UIElements;

public class HoverButton : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private string buttonId = "my-button";

    private VisualElement _button;

    private void OnEnable()
    {
        _button = document.rootVisualElement.Q<VisualElement>(buttonId);
        _button.RegisterCallback<PointerEnterEvent>(OnHover);
        _button.RegisterCallback<PointerLeaveEvent>(OnHoverEnd);
    }

    private void OnHover(PointerEnterEvent evt)
    {
        Tween.Scale(_button, 1.05f, 0.12f, Ease.OutQuad);
    }

    private void OnHoverEnd(PointerLeaveEvent evt)
    {
        Tween.Scale(_button, 1f, 0.12f, Ease.OutQuad);
    }
}
```

**Why PrimeTween over DOTween:** Zero-alloc, struct-based tweens. No coroutines. No GC pressure from lambda captures. Plays well with DOTS/Burst if you go that route later. The API is simpler and harder to misuse (no leaked tweens from forgetting to recycle).

### Important PrimeTween notes

- **Auto-cleanup:** PrimeTween completes and auto-disposes tweens when they finish. You don't need to manually kill them unless you're interrupting (like the health bar punch stopping the previous tween).
- **Domain reload:** In Edit mode, PrimeTween resets on domain reload. No stale tween state surviving play-mode exits.
- **Sequence support:** If you want a sequence (scale up → wait → fade out), use `Sequence.Create()` — same zero-alloc pattern.
- **Duration 0:** `Tween.Scale(element, 1f, 0f)` instantly sets the value. Useful for initialization without a separate code path.

---

## Unity Editor Setup

### UpdateManager

1. Create empty GameObject named `UpdateManager` in the scene
2. Add `UpdateManager` component
3. Ensure it's active before any IUpdatable tries to register
4. Script execution order (optional): set UpdateManager to run before default time if registration happens in Awake of other managers

### GameConstants

No setup needed — it's a static class. Just reference it from code.

### Refactoring existing strings

Search the codebase for hardcoded pool keys and replace:

| Before | After |
|---|---|
| `"audioSource"` in ObjectPoolManager.Get/Return | `GameConstants.PoolAudioSource` |
| `"projectile"` | `GameConstants.PoolProjectile` |
| `"enemy"` | `GameConstants.PoolEnemy` |

### Refactoring existing magic numbers

Search for `100` used as starting cash → replace with `GameConstants.StartingCash`. Search for `20` used as health → `GameConstants.StartingHealth`. Be careful not to blindly replace — only the ones that are gameplay constants, not unrelated numbers.

---

## Test Plan

| # | Test | Expected Result |
|---|---|---|
| 1 | Spawn 1 enemy with IUpdatable, register at High | Tick() called every frame |
| 2 | Register same IUpdatable at Medium | Tick() called every 0.15s |
| 3 | Register/Unregister at runtime | No NRE, list removes correctly, no double-tick |
| 4 | Register duplicate at same priority | Contains check prevents double-add |
| 5 | Unregister non-existent item | Remove returns false, no crash |
| 6 | Scene with 50 enemies registered | High list ticks 50 items per frame, Medium/Low at intervals |
| 7 | GameConstants.StartingCash accessed from PlayerStats | Returns 100 |
| 8 | GameConstants.PoolAudioSource used in AudioController | Same string "audioSource", compiles and works |
| 9 | PrimeTween test script runs | Object moves upward 2 units in 1s |
| 10 | Health bar punch on damage | Bar scales up to 1.3x then snaps back |
| 11 | Rapid damage (10 hits in 0.5s) | Punch doesn't stack, each hit restarts the tween |
| 12 | UpdateManager duplicate in scene | Second instance destroyed, no duplicate ticking |

---

## Debugging Tips

**UpdateManager.Instance is null:**
- The UpdateManager must be in the scene and Awake must have run. If another manager's Awake tries to register an IUpdatable before UpdateManager.Awake, Instance is null. Fix script execution order: Edit → Project Settings → Script Execution Order → add UpdateManager at -100.

**IUpdatable not getting Tick called:**
- Confirm Register was called with the correct priority.
- Add `Debug.Log($"Registered {updatable} at {priority}")` in Register and confirm it fires.
- If you registered at Low but expected per-frame ticks, wrong priority.

**Tween does nothing:**
- PrimeTween must be installed. Check Package Manager for errors.
- `duration: 0` is an instant set, not a no-op. If you see no animation, check duration > 0.
- If tweening a UI Toolkit element, confirm the VisualElement exists and is visible.

**Health bar punch looks weird:**
- `punchScale = 1.3f` might be too much or too little. Adjust per enemy size.
- If the bar fill Transform has a non-uniform scale already, the punch multiplies from current. Call `Tween.Scale` to a world-space target or reset to Vector3.one first.

**Buttons not responding to hover:**
- Confirm `PointerEnterEvent` / `PointerLeaveEvent` fire — add a Debug.Log in the callback.
- On mobile, pointer events behave differently. Add `PointerDownEvent` as a fallback if targeting touch.

**Performance regression after adding UpdateManager:**
- Profile with the Profiler. The only per-frame work is iterating the High list and calling Tick. If High list is large (100+), that's 100 virtual method calls per frame — still negligible. The real cost is what each Tick does internally.
- Medium/Low tier savings: if 50 enemies move to Low-tier ticking, you save ~150ms/s at 60fps (from 3000 calls to ~150 calls per second). Measure before and after.

---

# From Service Locator to Dependency Injection

---

## Why Service Locator Is Limited

The `Services.Get<T>()` pattern works, but it has tradeoffs that grow with your project:

**Hidden dependencies.** A class that calls `Services.Get<ObjectPoolManager>()` inside its methods hides what it needs. You can't tell from the constructor or API surface that `EnemySpawner` depends on `ObjectPoolManager` — you have to read every method body.

**Any class can request any service at any time.** There's no compile-time constraint. A UI class could suddenly pull in `ObjectPoolManager` without anyone noticing until the coupling is deep.

**Hard to test in isolation.** Unit testing a class that calls `Services.Get<T>()` means you need to register real or mock services before each test. The global mutable state makes tests interfere with each other.

```csharp
// Hidden dependency — nothing in the API says this class needs ObjectPoolManager
private IEnumerator SpawnEnemyRoutine(WaveEntry entry, EnemyPath path)
{
    var enemy = Services.Get<ObjectPoolManager>().Get("enemy", path.StartPosition, Quaternion.identity);
    // ...
}
```

---

## The Upgrade Path: Manual DI via Initialize() Injection

Instead of pulling dependencies from a global service locator, **push them in** through an explicit `Initialize()` method. The GameBootstrapper becomes a pure composition root — the one place where all systems are wired together.

### Before — Service Locator pulls

```csharp
public class EnemySpawner : MonoBehaviour
{
    private IEnumerator SpawnEnemyRoutine(WaveEntry entry, EnemyPath path)
    {
        var pool = Services.Get<ObjectPoolManager>();
        var enemy = pool.Get("enemy", path.StartPosition, Quaternion.identity);
        var controller = enemy.GetComponent<EnemyController>();
        controller.Initialize(enemyDataLookup[entry.EnemyId], path);
        _remainingInBatch--;
    }
}
```

### After — Initialize() injection pushes

```csharp
public class EnemySpawner : MonoBehaviour
{
    private ObjectPoolManager _pool;

    public void Initialize(ObjectPoolManager pool)
    {
        _pool = pool;
    }

    private IEnumerator SpawnEnemyRoutine(WaveEntry entry, EnemyPath path)
    {
        var enemy = _pool.Get("enemy", path.StartPosition, Quaternion.identity);
        var controller = enemy.GetComponent<EnemyController>();
        controller.Initialize(enemyDataLookup[entry.EnemyId], path);
        _remainingInBatch--;
    }
}
```

Now the dependency is visible in the `Initialize` signature. The class doesn't reach into a global registry — it receives what it needs from the outside.

### GameBootstrapper as composition root

```csharp
public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private ObjectPoolManager objectPoolManager;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private AudioController audioController;
    [SerializeField] private UpdateManager updateManager;

    private void Awake()
    {
        enemySpawner.Initialize(objectPoolManager);
        audioController.Initialize(objectPoolManager);
        // ...
    }

    private void OnDestroy()
    {
        Services.Get<CombatEvents>().Clear();
        Services.Get<GameEvents>().Clear();
        Services.Get<WaveEvents>().Clear();
        Services.Get<EconomyEvents>().Clear();
        Services.Clear();
    }
}
```

Every dependency is wired in one place. If you want to swap `ObjectPoolManager` for a test double, you pass it in `Initialize()` — no service registry to mock.

---

## VContainer — Production DI for Unity

Manual `Initialize()` injection works well for small projects. As dependency graphs grow, wiring everything by hand in `GameBootstrapper.Awake()` becomes repetitive and error-prone.

**VContainer** is a production-ready DI container built for Unity that automates the wiring:

- Resolves dependencies through constructor or `[Inject]` method injection
- Supports scoped lifetimes (per-container, transient, singleton)
- Integrates with Unity's lifecycle (MonoBehaviour injection, `VContainerSettings`)
- Zero reflection in resolve path after startup — fast enough for gameplay code

Documentation: [https://vcontainer.hadashikick.jp/](https://vcontainer.hadashikick.jp/)

With VContainer, your `EnemySpawner` declares its dependencies in the constructor, and the container resolves them automatically:

```csharp
public class EnemySpawner : MonoBehaviour
{
    private readonly ObjectPoolManager _pool;

    // VContainer injects this automatically
    [Inject]
    public void Construct(ObjectPoolManager pool)
    {
        _pool = pool;
    }
}
```

No `Services.Get<T>()` calls. No manual `Initialize()` wiring in the bootstrapper. The container reads the `[Inject]` attribute and provides the registered `ObjectPoolManager`.

---

## When to Upgrade

| Project Size | Recommended Approach |
|---|---|
| Small solo project, < 20 systems | Service locator is fine. The indirection cost of DI isn't worth it. |
| Medium project, 20-50 systems | Manual `Initialize()` injection. Dependencies become visible without adding a framework. |
| Large team project, 50+ systems, complex dependency graph | VContainer or another DI container. Automated wiring saves time and prevents mistakes. |

The service locator in this tutorial is intentionally kept simple. It teaches the concept of shared services without the overhead of a DI framework. When your project outgrows it, the `Initialize()` pattern is the same concept — just with explicit parameters instead of global lookups. VContainer automates that pattern at scale.