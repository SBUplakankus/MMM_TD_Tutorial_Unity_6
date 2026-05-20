# Episode 08: Event System & Game Loop — Implementation Guide

## What You're Building

A pure C# event system that decouples game systems without any ScriptableObject dependencies. This episode replaces the old SO event channel architecture entirely. You're implementing:

- **EventChannel / EventChannel\<T\>** — reusable base classes with C# `delegate` backing
- **Event registries** — non-static classes that group related channels (CombatEvents, WaveEvents, EconomyEvents, GameEvents)
- **Services** — static service locator for consistent access to registries and managers
- **GameBootstrapper** — composition root that registers everything in `Awake()`
- **PlayerStats** — plain C# class that subscribes to combat events and raises economy events
- **Updated EnemyController / WaveManager** — raise events via `Services.Get<T>()`

### Why Pure C# Over ScriptableObject Events

| Problem with SO events | How pure C# solves it |
|---|---|
| Missing .asset references in inspector — silent null, event never fires | Channels live in code — they always exist |
| .asset files cause merge conflicts in team projects | No .asset files — code-only |
| Can't create generic SO instances in inspector | `EventChannel<T>` works for any type |
| SOs persist across scene loads — stale subscriptions | `Clear()` on scene teardown is explicit and centralized |
| SOs are Unity-specific — not reusable in other engines/frameworks | Pure C# ports anywhere |

### Architecture Overview

```
GameBootstrapper (Awake)
  ├── new CombatEvents()  → Services.Register<CombatEvents>()
  ├── new WaveEvents()    → Services.Register<WaveEvents>()
  ├── new EconomyEvents() → Services.Register<EconomyEvents>()
  ├── new GameEvents()    → Services.Register<GameEvents>()
  ├── new PlayerStats()   → Services.Register<PlayerStats>()
  ├── ObjectPoolManager   → Services.Register<ObjectPoolManager>() (self-registers in its own Awake)
  └── WaveManager         → Services.Register<WaveManager>() (self-registers in its own Awake)

Any code → Services.Get<CombatEvents>().EnemyDeath.Raise(gold)

GameBootstrapper (OnDestroy)
  ├── combatEvents.Clear()
  ├── waveEvents.Clear()
  ├── economyEvents.Clear()
  ├── gameEvents.Clear()
  └── Services.Clear()
```

**Key rule**: Subscribe in `OnEnable`, unsubscribe in `OnDisable` for MonoBehaviours. For plain C# classes, subscribe in constructor/`Initialize`, unsubscribe in `Cleanup()`.

## Files & Order

| # | File | Action |
|---|------|--------|
| 1 | `Assets/Scripts/Events/EventChannel.cs` | UPDATE — full implementation |
| 2 | `Assets/Scripts/Events/EventChannelT.cs` | UPDATE — full implementation |
| 3 | `Assets/Scripts/Events/Registries/CombatEvents.cs` | UPDATE — full implementation |
| 4 | `Assets/Scripts/Events/Registries/WaveEvents.cs` | UPDATE — full implementation |
| 5 | `Assets/Scripts/Events/Registries/EconomyEvents.cs` | UPDATE — full implementation |
| 6 | `Assets/Scripts/Events/Registries/GameEvents.cs` | UPDATE — full implementation |
| 7 | `Assets/Scripts/Core/Services.cs` | UPDATE — full implementation |
| 8 | `Assets/Scripts/Core/GameBootstrapper.cs` | UPDATE — full implementation |
| 9 | `Assets/Scripts/Systems/Game/PlayerStats.cs` | UPDATE — full implementation |
| 10 | `Assets/Scripts/Enemies/Controllers/EnemyController.cs` | UPDATE — raise events on Die/OnReachedEnd |
| 11 | `Assets/Scripts/Systems/Managers/WaveManager.cs` | UPDATE — raise events via Services |

## Implementation

### 1. EventChannel.cs

The void event channel. No payload — just a signal that something happened.

```csharp
using System;

namespace Events
{
    public class EventChannel
    {
        private event Action Handlers;

        public void Raise() => Handlers?.Invoke();

        public void Subscribe(Action handler) => Handlers += handler;

        public void Unsubscribe(Action handler) => Handlers -= handler;

        public void Clear() => Handlers = null;
    }
}
```

**How it works**: `private event Action Handlers` is a delegate field. `Raise()` invokes all subscribers. `Clear()` sets the delegate to null — this is how we prevent leaked subscriptions on scene teardown.

### 2. EventChannelT.cs

The generic typed event channel. One class, infinite typed channels — `EventChannel<int>`, `EventChannel<float>`, `EventChannel<EnemyData>`, etc.

```csharp
using System;

namespace Events
{
    public class EventChannel<T>
    {
        private event Action<T> Handlers;

        public void Raise(T value) => Handlers?.Invoke(value);

        public void Subscribe(Action<T> handler) => Handlers += handler;

        public void Unsubscribe(Action<T> handler) => Handlers -= handler;

        public void Clear() => Handlers = null;
    }
}
```

**Why this works without ScriptableObject**: With SO events, you can't create `TypedEventChannel<T>` directly in the inspector — Unity can't instantiate generic SOs. You had to write `IntEventChannel : TypedEventChannel<int>` concrete subclasses with `[CreateAssetMenu]`. With pure C#, `EventChannel<int>` is just a `new()` away. No subclassing needed.

### 3. CombatEvents.cs

Combat-related events. Enemy death, enemy reaching the end of the path, enemy taking damage.

```csharp
using Events;

namespace Events.Registries
{
    public class CombatEvents
    {
        public readonly EventChannel<int> EnemyDeath = new();
        public readonly EventChannel<int> EnemyReachedEnd = new();
        public readonly EventChannel EnemyDamaged = new();

        public void Clear()
        {
            EnemyDeath.Clear();
            EnemyReachedEnd.Clear();
            EnemyDamaged.Clear();
        }
    }
}
```

**`readonly` on channels**: The channel instances themselves don't change — only their subscribers do. `readonly` prevents accidental reassignment like `EnemyDeath = new EventChannel<int>()` which would orphan any existing subscribers.

**Channel payloads**:
- `EnemyDeath` — `int` is the gold reward amount (so PlayerStats can add gold directly)
- `EnemyReachedEnd` — `int` is the damage the enemy deals to the player
- `EnemyDamaged` — void, just a signal (UI can listen to show damage numbers)

### 4. WaveEvents.cs

Wave progression events.

```csharp
using Events;

namespace Events.Registries
{
    public class WaveEvents
    {
        public readonly EventChannel<int> WaveStarted = new();
        public readonly EventChannel WaveCompleted = new();
        public readonly EventChannel AllWavesCompleted = new();

        public void Clear()
        {
            WaveStarted.Clear();
            WaveCompleted.Clear();
            AllWavesCompleted.Clear();
        }
    }
}
```

**Channel payloads**:
- `WaveStarted` — `int` is the wave number (UI shows "Wave 3")
- `WaveCompleted` — void, no extra data needed
- `AllWavesCompleted` — void, triggers win state

### 5. EconomyEvents.cs

Gold and lives change events. UI listens to these to update displays.

```csharp
using Events;

namespace Events.Registries
{
    public class EconomyEvents
    {
        public readonly EventChannel<int> GoldChanged = new();
        public readonly EventChannel<int> LivesChanged = new();

        public void Clear()
        {
            GoldChanged.Clear();
            LivesChanged.Clear();
        }
    }
}
```

**Channel payloads**:
- `GoldChanged` — `int` is the new gold total (not the delta — UI needs the absolute value)
- `LivesChanged` — `int` is the new lives total

### 6. GameEvents.cs

General game state events. Tower placement, pause, etc.

```csharp
using Events;

namespace Events.Registries
{
    public class GameEvents
    {
        public readonly EventChannel TowerPlaced = new();
        public readonly EventChannel GamePaused = new();

        public void Clear()
        {
            TowerPlaced.Clear();
            GamePaused.Clear();
        }
    }
}
```

### 7. Services.cs

The service locator. Single static dictionary, typed access. Every manager and registry is registered here in `GameBootstrapper.Awake()`.

```csharp
using System;
using System.Collections.Generic;

namespace Core
{
    public static class Services
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T service) where T : class => _services[typeof(T)] = service;

        public static T Get<T>() where T : class => (T)_services[typeof(T)];

        public static void Clear() => _services.Clear();
    }
}
```

**Why a service locator?** You need a consistent way to access event registries from plain C# classes that can't hold `[SerializeField]` references. `Services.Get<CombatEvents>()` works from anywhere — MonoBehaviour, plain C#, even static contexts.

**Trade-off**: Service locators hide dependencies (unlike constructor injection). But for a game jam / tutorial project, the simplicity wins. You can see every registration in one place — `GameBootstrapper.Awake()`.

**`where T : class`**: Prevents value types from being boxed and stored. All services are reference types.

**`_services[typeof(T)]` on Get**: This throws `KeyNotFoundException` if the service isn't registered. That's intentional — a missing service is a bug, not a recoverable condition. If you see this exception, the service wasn't registered in `GameBootstrapper.Awake()`.

### 8. GameBootstrapper.cs

The composition root. ONE MonoBehaviour that wires everything together. This is the only place that knows about all services.

```csharp
using Core;
using Events.Registries;
using Systems.Game;
using Systems.Managers;
using UnityEngine;

namespace Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        #region Serialized References

        [Header("Managers")]
        [SerializeField] private ObjectPoolManager objectPoolManager;
        [SerializeField] private WaveManager waveManager;

        [Header("Player")]
        [SerializeField] private int startingGold = 100;
        [SerializeField] private int startingLives = 20;

        #endregion

        #region Private Fields

        private CombatEvents _combatEvents;
        private WaveEvents _waveEvents;
        private EconomyEvents _economyEvents;
        private GameEvents _gameEvents;
        private PlayerStats _playerStats;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _combatEvents = new CombatEvents();
            _waveEvents = new WaveEvents();
            _economyEvents = new EconomyEvents();
            _gameEvents = new GameEvents();
            _playerStats = new PlayerStats(startingGold, startingLives);

            Services.Register(_combatEvents);
            Services.Register(_waveEvents);
            Services.Register(_economyEvents);
            Services.Register(_gameEvents);
            Services.Register(_playerStats);

            Services.Register(objectPoolManager);
            Services.Register(waveManager);
        }

        private void OnDestroy()
        {
            _combatEvents.Clear();
            _waveEvents.Clear();
            _economyEvents.Clear();
            _gameEvents.Clear();

            _playerStats.Cleanup();
            Services.Clear();
        }

        #endregion
    }
}
```

**Why store private fields for registries?** We need references to call `Clear()` in `OnDestroy()`. `Services.Get<T>()` would also work, but holding the references is cleaner — no risk of someone clearing the dictionary before we clear the events.

**Registration order matters**: Event registries are registered before managers. This ensures managers can access registries in their own `Awake()` methods (which run after `GameBootstrapper.Awake()` due to Unity's script execution order or because they're on the same GameObject).

**`OnDestroy` sequence**: Clear event registries first (prevents any late-firing events from hitting cleaned-up systems), then clean up PlayerStats, then clear the service dictionary.

**Why `[SerializeField]` for ObjectPoolManager and WaveManager?** These are MonoBehaviours that already exist in the scene. You can't `new` them — they need their serialized references and Unity lifecycle. Drag them from the scene hierarchy.

**Why `[SerializeField]` for startingGold / startingLives?** These are game-design values that should be tweakable in the inspector. PlayerStats is a plain C# class — it can't have `[SerializeField]`. The bootstrapper bridges inspector data to the C# constructor.

### 9. PlayerStats.cs

Plain C# class. NOT a MonoBehaviour. Created by `GameBootstrapper`, registered in Services. Subscribes to combat events, raises economy events.

```csharp
using Events.Registries;

namespace Systems.Game
{
    public class PlayerStats
    {
        #region Properties

        public int Gold { get; private set; }
        public int Lives { get; private set; }

        #endregion

        #region Private Fields

        private readonly CombatEvents _combatEvents;
        private readonly EconomyEvents _economyEvents;

        #endregion

        #region Constructor

        public PlayerStats(int startingGold, int startingLives)
        {
            Gold = startingGold;
            Lives = startingLives;

            _combatEvents = Services.Services.Get<CombatEvents>();
            _economyEvents = Services.Services.Get<EconomyEvents>();

            _combatEvents.EnemyDeath.Subscribe(OnEnemyDeath);
            _combatEvents.EnemyReachedEnd.Subscribe(OnEnemyReachedEnd);
        }

        #endregion

        #region Public API

        public bool TrySpendGold(int amount)
        {
            if (Gold < amount) return false;

            Gold -= amount;
            _economyEvents.GoldChanged.Raise(Gold);
            return true;
        }

        public void Cleanup()
        {
            _combatEvents.EnemyDeath.Unsubscribe(OnEnemyDeath);
            _combatEvents.EnemyReachedEnd.Unsubscribe(OnEnemyReachedEnd);
        }

        #endregion

        #region Private Methods

        private void OnEnemyDeath(int goldReward)
        {
            Gold += goldReward;
            _economyEvents.GoldChanged.Raise(Gold);
        }

        private void OnEnemyReachedEnd(int damage)
        {
            Lives -= damage;
            if (Lives < 0) Lives = 0;
            _economyEvents.LivesChanged.Raise(Lives);
        }

        #endregion
    }
}
```

**Why plain C# instead of MonoBehaviour?** PlayerStats has no transform, no visual representation, no Update loop. It's pure logic — gold and lives. Making it a MonoBehaviour adds overhead (hidden Transform, serialization, Unity lifecycle callbacks) for no benefit. As a plain C# class, it's testable, portable, and can't be accidentally duplicated in the scene.

**Constructor does the work that `OnEnable` would do**: Since PlayerStats isn't a MonoBehaviour, there's no `OnEnable`. The constructor subscribes to events. `Cleanup()` replaces `OnDisable`.

**`Cleanup()` is called from `GameBootstrapper.OnDestroy()`**: This ensures subscriptions are removed before the event registries are cleared. Order in `OnDestroy()` is: `_playerStats.Cleanup()` → `_combatEvents.Clear()` → `Services.Clear()`.

**The `Services.Services.Get` namespace issue**: PlayerStats lives in `Systems.Game`, and `Services` lives in `Core`. The `using Core;` import is needed. If you see a compiler error about `Services` not being found, add `using Core;` to the top of the file. The double `Services.Services.Get<T>()` in the constructor is a typo — it should be `Services.Get<T>()` with `using Core;` at the top. Corrected constructor:

```csharp
using Core;
using Events.Registries;

namespace Systems.Game
{
    public class PlayerStats
    {
        #region Properties

        public int Gold { get; private set; }
        public int Lives { get; private set; }

        #endregion

        #region Private Fields

        private readonly CombatEvents _combatEvents;
        private readonly EconomyEvents _economyEvents;

        #endregion

        #region Constructor

        public PlayerStats(int startingGold, int startingLives)
        {
            Gold = startingGold;
            Lives = startingLives;

            _combatEvents = Services.Get<CombatEvents>();
            _economyEvents = Services.Get<EconomyEvents>();

            _combatEvents.EnemyDeath.Subscribe(OnEnemyDeath);
            _combatEvents.EnemyReachedEnd.Subscribe(OnEnemyReachedEnd);
        }

        #endregion

        #region Public API

        public bool TrySpendGold(int amount)
        {
            if (Gold < amount) return false;

            Gold -= amount;
            _economyEvents.GoldChanged.Raise(Gold);
            return true;
        }

        public void Cleanup()
        {
            _combatEvents.EnemyDeath.Unsubscribe(OnEnemyDeath);
            _combatEvents.EnemyReachedEnd.Unsubscribe(OnEnemyReachedEnd);
        }

        #endregion

        #region Private Methods

        private void OnEnemyDeath(int goldReward)
        {
            Gold += goldReward;
            _economyEvents.GoldChanged.Raise(Gold);
        }

        private void OnEnemyReachedEnd(int damage)
        {
            Lives -= damage;
            if (Lives < 0) Lives = 0;
            _economyEvents.LivesChanged.Raise(Lives);
        }

        #endregion
    }
}
```

**Caching registry references in the constructor**: `_combatEvents` and `_economyEvents` are cached once instead of calling `Services.Get<T>()` every time an event fires. This avoids dictionary lookups on every enemy death and is cleaner to read.

**`TrySpendGold` returns bool**: The shop/tower system checks the return value. If `false`, the purchase is rejected. The `GoldChanged` event only fires on a successful spend.

**Gold/Lives payload convention**: Events carry the new total, not the delta. UI components need to display the absolute value, and having to track deltas would force every listener to maintain its own state.

### 10. EnemyController.cs — Raise events on Die and OnReachedEnd

Update the existing `EnemyController` to raise combat events via the service locator.

```csharp
using Core;
using Data;
using Enemies.Components;
using Events.Registries;
using Interfaces;
using Systems.Game;
using Systems.Managers;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable, IPoolable
    {
        #region Fields

        [Header("Enemy UI")]
        [SerializeField] private EnemyHealthBar healthBar;

        #endregion

        #region Properties

        public EnemyPath Path { get; private set; }
        public int CurrentWayPointIndex { get; set; }
        public IHealthStrategy Health { get; private set; }
        public IMovementStrategy Movement { get; private set; }
        public int GoldGiven { get; private set; }
        public int Damage { get; private set; }

        public Vector3 Position => transform.position;
        public bool IsAlive => Health != null && Health.IsAlive;

        #endregion

        #region Class Methods

        public void Initialize(EnemyData data, EnemyPath path)
        {
            Path = path;
            CurrentWayPointIndex = 0;
            GoldGiven = data.GoldGiven;
            Damage = data.Damage;

            Health = StrategyFactory.CreateHealthStrategy(data.HealthConfig);
            Movement = StrategyFactory.CreateMovementStrategy(data.MovementConfig);

            Health.Initialize(this);
            Movement.Initialize(this);
            Movement.OnMovementCompleted += OnReachedEnd;
        }

        public void Die()
        {
            Services.Get<CombatEvents>().EnemyDeath.Raise(GoldGiven);
            Movement.OnMovementCompleted -= OnReachedEnd;
            Services.Get<ObjectPoolManager>().Return("enemy", gameObject);
        }

        #endregion

        #region Unity Methods

        private void Update()
        {
            Health.Tick(Time.deltaTime);
            Movement.Tick(this);
        }

        #endregion

        #region IDamageable

        public void TakeDamage(float damage)
        {
            Health.TakeDamage(damage);
            if (!IsAlive) Die();
        }

        #endregion

        #region IPoolable

        public void Reset()
        {
            Health = null;
            Movement = null;
            CurrentWayPointIndex = 0;
        }

        #endregion

        #region Private Methods

        private void OnReachedEnd()
        {
            Services.Get<CombatEvents>().EnemyReachedEnd.Raise(Damage);
            Movement.OnMovementCompleted -= OnReachedEnd;
            Services.Get<ObjectPoolManager>().Return("enemy", gameObject);
        }

        #endregion
    }
}
```

**What changed from the stub**:
- `Initialize()` now fully implements strategy creation via `StrategyFactory`, data assignment, and wiring
- `Die()` raises `CombatEvents.EnemyDeath` with `GoldGiven` as payload, then returns to pool
- `OnReachedEnd()` raises `CombatEvents.EnemyReachedEnd` with `Damage` as payload, then returns to pool
- `IPoolable.Reset()` clears runtime state only — no event unsubscription needed here because events are raised, not subscribed to
- No `[SerializeField]` event channels — events are accessed via `Services.Get<T>()`

**Why `Die()` unsubscribes from `Movement.OnMovementCompleted`**: If the enemy is killed by tower damage (not by reaching the end), the movement completion callback would still be subscribed. When the enemy is deactivated and returned to the pool, the next time this GameObject is activated with a different enemy, the old subscription could fire. Always unsubscribe in both `Die()` and `OnReachedEnd()`.

**`IPoolable.Reset()` doesn't touch event subscriptions**: EnemyController doesn't subscribe to any `EventChannel` — it only raises them. So `Reset()` only clears runtime state (health, movement, waypoint index). The event-raising code uses `Services.Get<T>()` which always works.

**Event raise before pool return**: `Die()` raises `EnemyDeath` *before* calling `Return()`. Once the object is returned to the pool, it's deactivated. Any code after `Return()` would execute on an inactive GameObject — avoid that pattern.

### 11. WaveManager.cs — Raise events via Services

Update the existing `WaveManager` to raise wave events.

```csharp
using Core;
using Events.Registries;
using Systems.Managers;
using UnityEngine;

namespace Systems.Game
{
    public class WaveManager : MonoBehaviour
    {
        #region Fields

        [SerializeField] private TextAsset waveCsvFile;
        [SerializeField] private EnemySpawner enemySpawner;

        #endregion

        #region Properties

        public int CurrentWave { get; private set; }
        public int TotalWaves { get; private set; }

        #endregion

        #region Lifecycle

        private void Awake()
        {
            Services.Register(this);
        }

        #endregion

        #region Public API

        public void StartNextWave()
        {
            CurrentWave++;

            if (CurrentWave > TotalWaves)
            {
                Services.Get<WaveEvents>().AllWavesCompleted.Raise();
                return;
            }

            Services.Get<WaveEvents>().WaveStarted.Raise(CurrentWave);
        }

        public void CheckWaveProgress()
        {
            if (enemySpawner.IsComplete && Services.Get<EnemyManager>().ActiveCount == 0)
            {
                Services.Get<WaveEvents>().WaveCompleted.Raise();
            }
        }

        #endregion
    }
}
```

**Self-registration in `Awake()`**: `Services.Register(this)` means `WaveManager` registers itself. This is the pattern for MonoBehaviours that already exist in the scene — `GameBootstrapper` also registers them, but having the manager self-register means it works even if the bootstrapper isn't set up yet. If both register, the second `Register()` overwrites — same reference, no problem.

**`StartNextWave` flow**: Increment → check if past total → raise `AllWavesCompleted` if done, otherwise raise `WaveStarted` with wave number.

**`CheckWaveProgress`**: Called by external code (e.g., when an enemy dies) to check if the wave is over. Raises `WaveCompleted` when the spawner is done and no enemies remain.

## Unity Editor Setup

### Create the GameBootstrapper GameObject

1. Create empty GameObject named `GameBootstrapper` at scene root
2. Add `GameBootstrapper` component
3. Drag `ObjectPoolManager` from the `Managers/` hierarchy into the `objectPoolManager` field
4. Drag `WaveManager` from the `Managers/` hierarchy into the `waveManager` field
5. Set `startingGold = 100`, `startingLives = 20`

### Scene Hierarchy Update

```
Scene Root
├── Managers
│   ├── GameBootstrapper       (GameBootstrapper component)  ← NEW
│   ├── ObjectPoolManager      (ObjectPoolManager component — self-registers)
│   ├── UpdateManager          (UpdateManager component)
│   ├── WaveManager            (WaveManager component — self-registers via Awake)
│   └── AudioController       (AudioController component)
├── Game
│   ├── EnemySpawner           (EnemySpawner component)
│   ├── EnemyPath              (EnemyPath component)
│   └── [PlayerStats REMOVED from here — now plain C# in Services]
└── Camera
```

**Remove PlayerStats MonoBehaviour from the scene**: PlayerStats is no longer a MonoBehaviour. If you had a `PlayerStats` component on a GameObject, remove it. The `GameBootstrapper` creates and manages the `PlayerStats` instance internally.

### Delete Old SO Event Channel Assets

If you have leftover `.asset` files from the old SO event system, delete them:
- `Assets/Data/Events/OnEnemyDeath.asset`
- `Assets/Data/Events/OnEnemyKillReward.asset`
- `Assets/Data/Events/OnGoldChanged.asset`
- etc.

Also delete the old SO event channel scripts if they still exist:
- `Assets/Scripts/Events/EventChannelBase.cs`
- `Assets/Scripts/Events/VoidEventChannel.cs`
- `Assets/Scripts/Events/TypedEventChannel.cs`
- `Assets/Scripts/Events/IntEventChannel.cs`
- `Assets/Scripts/Events/FloatEventChannel.cs`

### Script Execution Order

If you hit the `KeyNotFoundException` from `Services.Get<T>()` during `Awake()`, set `GameBootstrapper` to run first:

1. Edit → Project Settings → Script Execution Order
2. Add `GameBootstrapper` at **-100** (before default time of 0)
3. This ensures all services are registered before any other `Awake()` tries to access them

## Test Plan

| # | Test | Steps | Expected |
|---|------|-------|----------|
| 1 | EventChannel Subscribe/Raise | Subscribe a handler, call `Raise()` | Handler invoked |
| 2 | EventChannel\<int\> payload | Subscribe to `EventChannel<int>`, `Raise(42)` | Handler receives 42 |
| 3 | Unsubscribe safety | Subscribe handler, then unsubscribe, then raise | Handler NOT invoked |
| 4 | EventChannel.Clear | Subscribe 2 handlers, call `Clear()`, then `Raise()` | Neither handler invoked |
| 5 | Services round-trip | `Register<X>(x)`, then `Get<X>()` | Returns same reference |
| 6 | Services missing | `Get<SomeUnregisteredType>()` | `KeyNotFoundException` thrown |
| 7 | GameBootstrapper registers all | Play, check `Services.Get<CombatEvents>()` | Returns non-null CombatEvents |
| 8 | PlayerStats gold on kill | `EnemyDeath.Raise(10)` with startingGold=100 | `Gold == 110`, `GoldChanged` raised with 110 |
| 9 | PlayerStats lives on reach | `EnemyReachedEnd.Raise(1)` with startingLives=20 | `Lives == 19`, `LivesChanged` raised with 19 |
| 10 | TrySpendGold success | Spend 50 with Gold=100 | Returns true, Gold=50, `GoldChanged` raised |
| 11 | TrySpendGold fail | Spend 200 with Gold=100 | Returns false, Gold unchanged, no event raised |
| 12 | Cleanup prevents ghost calls | Call `Cleanup()`, then `EnemyDeath.Raise(10)` | Handler not invoked |
| 13 | Bootstrapper OnDestroy clears | Play, stop, check CombatEvents handlers | All channels cleared (no leaked subscriptions) |
| 14 | Enemy Die raises event | Kill enemy with GoldGiven=10 | `CombatEvents.EnemyDeath` raised with 10 |
| 15 | Enemy OnReachedEnd raises event | Enemy reaches end of path | `CombatEvents.EnemyReachedEnd` raised with Damage |
| 16 | Pool cycle events | Spawn enemy, kill it, respawn, kill again | Events fire on both deaths, no stale state |
| 17 | WaveStarted | Call `StartNextWave()` | `WaveEvents.WaveStarted` raised with wave number |
| 18 | AllWavesCompleted | Call `StartNextWave()` past `TotalWaves` | `WaveEvents.AllWavesCompleted` raised |

### Quick Manual Test Script

Add this to a temporary GameObject for live testing. Delete it after verification.

```csharp
using Core;
using Events.Registries;
using Systems.Game;
using UnityEngine;

namespace Tests
{
    public class EventSystemTest : MonoBehaviour
    {
        private void OnEnable()
        {
            var combat = Services.Get<CombatEvents>();
            var economy = Services.Get<EconomyEvents>();
            var wave = Services.Get<WaveEvents>();

            combat.EnemyDeath.Subscribe(g => Debug.Log($"[Test] EnemyDeath: +{g} gold"));
            combat.EnemyReachedEnd.Subscribe(d => Debug.Log($"[Test] EnemyReachedEnd: -{d} lives"));
            combat.EnemyDamaged.Subscribe(() => Debug.Log("[Test] EnemyDamaged"));
            economy.GoldChanged.Subscribe(g => Debug.Log($"[Test] GoldChanged: {g}"));
            economy.LivesChanged.Subscribe(l => Debug.Log($"[Test] LivesChanged: {l}"));
            wave.WaveStarted.Subscribe(w => Debug.Log($"[Test] WaveStarted: {w}"));
            wave.WaveCompleted.Subscribe(() => Debug.Log("[Test] WaveCompleted"));
            wave.AllWavesCompleted.Subscribe(() => Debug.Log("[Test] AllWavesCompleted"));
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                Services.Get<CombatEvents>().EnemyDeath.Raise(10);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                Services.Get<CombatEvents>().EnemyReachedEnd.Raise(1);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                Services.Get<WaveEvents>().WaveStarted.Raise(1);

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                var stats = Services.Get<PlayerStats>();
                Debug.Log($"[Test] Gold: {stats.Gold}, Lives: {stats.Lives}");
            }
        }

        private void OnDisable()
        {
            var combat = Services.Get<CombatEvents>();
            var economy = Services.Get<EconomyEvents>();
            var wave = Services.Get<WaveEvents>();

            combat.EnemyDeath.Subscribe(g => Debug.Log($"[Test] EnemyDeath: +{g} gold"));
            combat.EnemyReachedEnd.Subscribe(d => Debug.Log($"[Test] EnemyReachedEnd: -{d} lives"));
            combat.EnemyDamaged.Subscribe(() => Debug.Log("[Test] EnemyDamaged"));
            economy.GoldChanged.Subscribe(g => Debug.Log($"[Test] GoldChanged: {g}"));
            economy.LivesChanged.Subscribe(l => Debug.Log($"[Test] LivesChanged: {l}"));
            wave.WaveStarted.Subscribe(w => Debug.Log($"[Test] WaveStarted: {w}"));
            wave.WaveCompleted.Subscribe(() => Debug.Log("[Test] WaveCompleted"));
            wave.AllWavesCompleted.Subscribe(() => Debug.Log("[Test] AllWavesCompleted"));
        }
    }
}
```

**Important**: The `OnDisable` above accidentally re-subscribes instead of unsubscribing. Here's the corrected version — the subscribe calls should use `-=` (Unsubscribe):

```csharp
using Core;
using Events.Registries;
using Systems.Game;
using UnityEngine;

namespace Tests
{
    public class EventSystemTest : MonoBehaviour
    {
        private void OnEnable()
        {
            var combat = Services.Get<CombatEvents>();
            var economy = Services.Get<EconomyEvents>();
            var wave = Services.Get<WaveEvents>();

            combat.EnemyDeath.Subscribe(OnEnemyDeath);
            combat.EnemyReachedEnd.Subscribe(OnEnemyReachedEnd);
            combat.EnemyDamaged.Subscribe(OnEnemyDamaged);
            economy.GoldChanged.Subscribe(OnGoldChanged);
            economy.LivesChanged.Subscribe(OnLivesChanged);
            wave.WaveStarted.Subscribe(OnWaveStarted);
            wave.WaveCompleted.Subscribe(OnWaveCompleted);
            wave.AllWavesCompleted.Subscribe(OnAllWavesCompleted);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                Services.Get<CombatEvents>().EnemyDeath.Raise(10);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                Services.Get<CombatEvents>().EnemyReachedEnd.Raise(1);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                Services.Get<WaveEvents>().WaveStarted.Raise(1);

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                var stats = Services.Get<PlayerStats>();
                Debug.Log($"[Test] Gold: {stats.Gold}, Lives: {stats.Lives}");
            }
        }

        private void OnDisable()
        {
            var combat = Services.Get<CombatEvents>();
            var economy = Services.Get<EconomyEvents>();
            var wave = Services.Get<WaveEvents>();

            combat.EnemyDeath.Unsubscribe(OnEnemyDeath);
            combat.EnemyReachedEnd.Unsubscribe(OnEnemyReachedEnd);
            combat.EnemyDamaged.Unsubscribe(OnEnemyDamaged);
            economy.GoldChanged.Unsubscribe(OnGoldChanged);
            economy.LivesChanged.Unsubscribe(OnLivesChanged);
            wave.WaveStarted.Unsubscribe(OnWaveStarted);
            wave.WaveCompleted.Unsubscribe(OnWaveCompleted);
            wave.AllWavesCompleted.Unsubscribe(OnAllWavesCompleted);
        }

        private void OnEnemyDeath(int gold) => Debug.Log($"[Test] EnemyDeath: +{gold} gold");
        private void OnEnemyReachedEnd(int damage) => Debug.Log($"[Test] EnemyReachedEnd: -{damage} lives");
        private void OnEnemyDamaged() => Debug.Log("[Test] EnemyDamaged");
        private void OnGoldChanged(int gold) => Debug.Log($"[Test] GoldChanged: {gold}");
        private void OnLivesChanged(int lives) => Debug.Log($"[Test] LivesChanged: {lives}");
        private void OnWaveStarted(int wave) => Debug.Log($"[Test] WaveStarted: {wave}");
        private void OnWaveCompleted() => Debug.Log("[Test] WaveCompleted");
        private void OnAllWavesCompleted() => Debug.Log("[Test] AllWavesCompleted");
    }
}
```

**Why named methods instead of lambdas**: You can't unsubscribe a lambda unless you keep a reference to it. Named methods give you stable references for both subscribe and unsubscribe. This is the most common bug with event systems — subscribe with a lambda, can't unsubscribe.

## Debugging Tips

### KeyNotFoundException from Services.Get\<T\>()

**Cause**: The service wasn't registered before code tried to access it. Check `GameBootstrapper.Awake()` — is the `Register()` call there? Is `Awake()` actually running?

**Fix**: Set `GameBootstrapper` to run first in Script Execution Order (-100). If a manager's `Awake()` calls `Services.Get<T>()` before the bootstrapper has registered it, you get this exception.

### Events fire but nothing happens

**Cause**: The listener hasn't subscribed yet, or the subscribe happened after the raise.

**Debug**: Add a `Debug.Log` inside the `Raise()` method temporarily:
```csharp
public void Raise() => Debug.Log($"[EventChannel] Raise — {Handlers?.GetInvocationList().Length ?? 0} subscribers");
```
If you see "0 subscribers", the listener isn't subscribed. Check `OnEnable` timing.

### Double-fire: handler called twice

**Cause**: Handler was subscribed twice. Most common with `OnEnable` — if a GameObject is disabled and re-enabled, `OnEnable` runs again.

**Fix**: Always pair `Subscribe` in `OnEnable` with `Unsubscribe` in `OnDisable`. Never subscribe in `Awake` without a corresponding `OnDestroy` unsubscribe.

### Leaked subscriptions after scene load

**Symptom**: Events from Scene A still fire in Scene B. Handler in Scene A's code runs on a destroyed object.

**Cause**: `EventChannel.Clear()` wasn't called during scene transition.

**Fix**: `GameBootstrapper.OnDestroy()` calls `Clear()` on every registry. If you load a new scene additively, you need to manage Clear() manually.

### EnemyController: events not raising on Die

**Cause**: `Die()` isn't being called, or `Services` isn't initialized yet.

**Debug**: Add a `Debug.Log` at the top of `Die()` to confirm it's called. If it is, check `Services.Get<CombatEvents>()` returns non-null.

### PlayerStats: gold doesn't increase when enemy dies

**Cause**: `PlayerStats` wasn't registered in `Services`, or `EnemyController.Die()` raises the event before `PlayerStats` subscribes.

**Debug**: In `PlayerStats` constructor, add `Debug.Log($"[PlayerStats] Subscribed to CombatEvents. Gold: {Gold}");`. In `EnemyController.Die()`, add `Debug.Log($"[EnemyController] Raising EnemyDeath with {GoldGiven}");`.

### NullReferenceException in PlayerStats

**Cause**: `Services.Get<CombatEvents>()` was called before `GameBootstrapper.Awake()` registered it.

**Fix**: Ensure `GameBootstrapper.Awake()` registers all services before any other code accesses them. Use Script Execution Order if needed.

### Pool cycle: stale subscriptions

**Cause**: If a pooled MonoBehaviour subscribes to events in `OnEnable`, it automatically unsubscribes in `OnDisable` (because pool `Return()` deactivates the GameObject). But if you subscribe in `Awake` or `Start`, deactivation doesn't trigger unsubscribe.

**Fix**: For pooled MonoBehaviours, always use `OnEnable`/`OnDisable` for event subscriptions. `EnemyController` raises events but doesn't subscribe to any, so pool cycles are safe.

### The subscribe-in-lambda trap

```csharp
// WRONG — can't unsubscribe
channel.Subscribe(g => Debug.Log(g));

// RIGHT — named method with stable reference
channel.Subscribe(OnGoldChanged);
// ...
channel.Unsubscribe(OnGoldChanged);
```

Every lambda creates a new delegate instance. Even if the code is identical, `g => Debug.Log(g)` in subscribe and `g => Debug.Log(g)` in unsubscribe are two different objects. `-=` won't find the match.