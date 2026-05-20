# Episode 08: Event System & Service Locator

!!! info "Episode Type: Code Lesson"
    You'll implement a pure C# event system with registries and a service locator — replacing SO event channels with a cleaner architecture. ~25 min.

---

## Video

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden;">
  <iframe src="https://www.youtube.com/embed/EP08_PLACEHOLDER" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%;" frameborder="0" allowfullscreen></iframe>
</div>

---

## Learning Objectives

By the end of this episode you will be able to:

1. Explain why direct references and singletons create tight coupling and how events solve it
2. Implement pure C# event channels: `EventChannel` (void) and `EventChannel<T>` (typed payload)
3. Organize events into **registries** — non-static classes that group related event channels
4. Implement a **service locator** (`Services`) that provides access to registries and other systems
5. Build a **composition root** (`GameBootstrapper`) that wires everything together at startup
6. Manage subscription lifecycle correctly to prevent memory leaks and stale callbacks

---

## Key Concepts

| Concept | Summary | Learn More |
|---------|---------|------------|
| Observer Pattern | Publishers raise events; subscribers respond — neither knows about the other | [Observer Pattern](../Concepts/Observer_Pattern.md) |
| EventChannel | Pure C# class with `Raise`, `Subscribe`, `Unsubscribe`, `Clear` — no SO, no asset | — |
| EventChannel\<T\> | Typed event channel that carries a payload (e.g. gold amount, position) | — |
| Event Registries | Non-static classes grouping related events (`CombatEvents`, `WaveEvents`, etc.) | — |
| Service Locator | `Services.Get<T>()` — central access to registries and systems | — |
| Composition Root | `GameBootstrapper` registers everything in `Awake()`, cleans up in `OnDestroy()` | — |

---

## Code Roadmap

| File | Action | Notes |
|------|--------|-------|
| `Events/EventChannel.cs` | **Create** | Void event channel — `Raise()` with no payload |
| `Events/EventChannelT.cs` | **Create** | Generic event channel — `Raise(T)` with typed payload |
| `Events/Registries/CombatEvents.cs` | **Create** | `EnemyDeath`, `EnemyReachedEnd` |
| `Events/Registries/WaveEvents.cs` | **Create** | `WaveStarted`, `WaveCompleted`, `AllWavesCompleted` |
| `Events/Registries/EconomyEvents.cs` | **Create** | `GoldChanged`, `LivesChanged` |
| `Events/Registries/GameEvents.cs` | **Create** | `TowerPlaced`, `GamePaused` |
| `Core/Services.cs` | **Create** | Service locator — `Register<T>`, `Get<T>` |
| `Core/GameBootstrapper.cs` | **Create** | Composition root MonoBehaviour |
| `Systems/Game/PlayerStats.cs` | **Create** | Plain C# class — subscribes to events, raises economy events |

---

## Architecture Context

```
┌─────────────────┐     Services.Get<CombatEvents>()     ┌──────────────┐
│  EnemyController │────────────────────────────────────→│ CombatEvents │
│  .Die()          │     .EnemyDeath.Raise(goldReward)   │              │
└─────────────────┘                                       └──────┬───────┘
                                                                 │
                                ┌────────────────────────────────┘
                                │
                                ▼
                         ┌──────────────┐     Services.Get<EconomyEvents>()
                         │ PlayerStats  │─────────────────────────────→ .GoldChanged.Raise(newTotal)
                         │ (subscriber) │
                         └──────────────┘
                                │
                                ▼
                         ┌──────────────┐
                         │ UI (subscriber)│  ← updates gold display
                         └──────────────┘

Composition Root:
┌──────────────────────────────────────────────────┐
│ GameBootstrapper (MonoBehaviour)                  │
│   Awake():                                        │
│     Services.Register(new CombatEvents());        │
│     Services.Register(new WaveEvents());          │
│     Services.Register(new EconomyEvents());       │
│     Services.Register(new GameEvents());          │
│     Services.Register(new PlayerStats());         │
│   OnDestroy():                                    │
│     Services.Get<CombatEvents>().EnemyDeath.Clear();│
│     // ... Clear all event channels                │
└──────────────────────────────────────────────────┘
```

Every system access events through `Services.Get<T>()`. Publishers raise events; subscribers listen. No direct references. No SO assets. The `GameBootstrapper` is the only place that knows about everything.

---

## Step-by-Step Implementation Guide

### Step 1: The Problem — Direct References and Singletons

Without events, systems reach for each other directly:

```csharp
void Die()
{
    FindObjectOfType<PlayerStats>().AddGold(goldReward);
    FindObjectOfType<AudioController>().PlayDeathSound();
    FindObjectOfType<UIManager>().UpdateGoldDisplay();
}
```

Problems:

- `Die()` must know about `PlayerStats`, `AudioController`, `UIManager`
- Adding a new system (e.g., particle effects on death) requires editing `Die()`
- `FindObjectOfType` is slow and fragile
- Singletons (`.Instance`) are only slightly better — still coupled

!!! tip "Why not ScriptableObject event channels?"
    SO event channels work, but they have downsides: asset creation overhead, generic SO limitations in Unity, hidden state in the Inspector, and no compile-time safety that an event channel is properly registered. Pure C# event channels + registries give you the same decoupling with less ceremony and full IDE support.

### Step 2: `EventChannel` — Void Event Channel

```csharp
public class EventChannel
{
    private Action _onEventRaised;

    public void Raise()
    {
        // TODO: Invoke _onEventRaised
        // TODO: Wrap in try/catch to prevent one failed subscriber from blocking others
    }

    public void Subscribe(Action listener)
    {
        // TODO: Add listener to _onEventRaised
    }

    public void Unsubscribe(Action listener)
    {
        // TODO: Remove listener from _onEventRaised
    }

    public void Clear()
    {
        // TODO: Set _onEventRaised to null — use this on scene transition
    }
}
```

- Pure C# — no `MonoBehaviour`, no `ScriptableObject`, no Unity asset
- `Raise()` invokes every subscriber in order
- `Clear()` removes all subscribers — call this when leaving a scene to prevent stale callbacks
- No payload — for simple signals like "enemy died", "wave started"

### Step 3: `EventChannel<T>` — Typed Event Channel

```csharp
public class EventChannel<T>
{
    private Action<T> _onEventRaised;

    public void Raise(T data)
    {
        // TODO: Invoke _onEventRaised with data
        // TODO: Wrap in try/catch to prevent one failed subscriber from blocking others
    }

    public void Subscribe(Action<T> listener)
    {
        // TODO: Add listener to _onEventRaised
    }

    public void Unsubscribe(Action<T> listener)
    {
        // TODO: Remove listener from _onEventRaised
    }

    public void Clear()
    {
        // TODO: Set _onEventRaised to null
    }
}
```

Same API as `EventChannel`, but carries typed data:

| Example Channel | Type `T` | Purpose |
|-----------------|----------|---------|
| `EnemyDeath` | `int` | Gold reward from enemy death |
| `GoldChanged` | `int` | New gold total after change |
| `LivesChanged` | `int` | New lives total after change |

!!! tip "Why not use C# `event` directly?"
    You *could* use `public event Action OnEnemyDeath` on each class. The channel pattern gives you: (1) a named, discoverable object you can pass around, (2) `Clear()` for scene transitions, (3) a place to add logging/debugging later, and (4) consistent API across void and typed channels.

### Step 4: Event Registries — Grouping Related Channels

Registries are plain C# classes that hold `EventChannel` instances, organized by domain:

```csharp
public class CombatEvents
{
    public readonly EventChannel<int> EnemyDeath = new();
    public readonly EventChannel EnemyReachedEnd = new();
}
```

```csharp
public class WaveEvents
{
    public readonly EventChannel<int> WaveStarted = new();   // wave index
    public readonly EventChannel WaveCompleted = new();
    public readonly EventChannel AllWavesCompleted = new();
}
```

```csharp
public class EconomyEvents
{
    public readonly EventChannel<int> GoldChanged = new();    // new total
    public readonly EventChannel<int> LivesChanged = new();   // new total
}
```

```csharp
public class GameEvents
{
    public readonly EventChannel TowerPlaced = new();
    public readonly EventChannel GamePaused = new();
}
```

**Why registries instead of static classes?**

- Static classes are globally mutable — any code can reassign fields, no registration step needed, no cleanup possible
- Registry instances are registered in `Services` — discoverable, replaceable, clearable
- `Services.Get<CombatEvents>()` is explicit — you know where the events come from
- Testing: swap a registry with a mock without touching static state

### Step 5: `Services` — Service Locator

```csharp
public static class Services
{
    private static readonly Dictionary<Type, object> _services = new();

    public static void Register<T>(T service)
    {
        // TODO: Add service to _services dictionary with key typeof(T)
        // TODO: If key already exists, overwrite (last registration wins)
    }

    public static T Get<T>()
    {
        // TODO: Retrieve service from _services by type
        // TODO: Throw if not found — service not registered is a programming error
    }

    public static void Reset()
    {
        // TODO: Clear _services dictionary — call on scene unload
    }
}
```

- Static access — `Services.Get<CombatEvents>()` works from anywhere
- Type-keyed — each type has exactly one registration
- `Reset()` clears everything — called by `GameBootstrapper` on destroy
- Missing service = exception, not silent failure

!!! warning "Service Locator trade-offs"
    A service locator is a **controlled global** — more disciplined than singletons (one registration point) but still globally accessible. Use it for infrastructure (events, pools, configs). Don't use it as a shortcut to avoid passing references for gameplay objects.

### Step 6: `GameBootstrapper` — Composition Root

The bootstrapper is the **only** place that knows about all systems. Registration order matters:

```csharp
public class GameBootstrapper : MonoBehaviour
{
    private void Awake()
    {
        // Register event registries FIRST — other systems may subscribe during their own registration
        // TODO: Services.Register(new CombatEvents());
        // TODO: Services.Register(new WaveEvents());
        // TODO: Services.Register(new EconomyEvents());
        // TODO: Services.Register(new GameEvents());

        // Register systems — these subscribe to events in their constructors
        // TODO: Services.Register(new PlayerStats(startingGold, startingLives));
    }

    private void OnDestroy()
    {
        // Clear all event channels — prevents stale callbacks across scenes
        // TODO: Services.Get<CombatEvents>().EnemyDeath.Clear();
        // TODO: Services.Get<CombatEvents>().EnemyReachedEnd.Clear();
        // TODO: Services.Get<WaveEvents>().WaveStarted.Clear();
        // TODO: Services.Get<WaveEvents>().WaveCompleted.Clear();
        // TODO: Services.Get<WaveEvents>().AllWavesCompleted.Clear();
        // TODO: Services.Get<EconomyEvents>().GoldChanged.Clear();
        // TODO: Services.Get<EconomyEvents>().LivesChanged.Clear();
        // TODO: Services.Get<GameEvents>().TowerPlaced.Clear();
        // TODO: Services.Get<GameEvents>().GamePaused.Clear();

        // Reset service locator
        // TODO: Services.Reset();
    }
}
```

**Registration order matters because:**

- Event registries must exist before systems that subscribe during construction
- If `PlayerStats` subscribes to `CombatEvents.EnemyDeath` in its constructor, `CombatEvents` must be registered first
- `OnDestroy` clears all channels *before* resetting the locator — ensures no subscriber can access a cleared registry through `Services`

### Step 7: Wire `EnemyController.Die()` — Raising Events

```csharp
public class EnemyController : MonoBehaviour, IPoolable
{
    [SerializeField] private int goldReward;

    public void Die()
    {
        // TODO: Get CombatEvents from Services
        // TODO: Raise EnemyDeath with goldReward
        // TODO: Return to pool via ObjectPoolManager
    }

    public void OnEnemyReachedEnd()
    {
        // TODO: Get CombatEvents from Services
        // TODO: Raise EnemyReachedEnd (void — no payload)
        // TODO: Return to pool
    }
}
```

- No serialized event channel references — `Services.Get<CombatEvents>()` replaces them
- `goldReward` stays as a serialized field — it's gameplay data, not infrastructure
- Any number of systems can respond to `EnemyDeath` without `EnemyController` knowing about them

### Step 8: Wire `PlayerStats` — Plain C# Subscriber

`PlayerStats` is a **plain C# class** — not a `MonoBehaviour`. It subscribes to combat events and raises economy events:

```csharp
public class PlayerStats
{
    private int _gold;
    private int _lives;

    public PlayerStats(int startingGold, int startingLives)
    {
        _gold = startingGold;
        _lives = startingLives;

        // TODO: Subscribe to CombatEvents.EnemyDeath with HandleEnemyDeath
        // TODO: Subscribe to CombatEvents.EnemyReachedEnd with HandleEnemyReachedEnd
    }

    private void HandleEnemyDeath(int goldReward)
    {
        // TODO: _gold += goldReward
        // TODO: Raise EconomyEvents.GoldChanged with new _gold value
    }

    private void HandleEnemyReachedEnd()
    {
        // TODO: _lives -= 1
        // TODO: Raise EconomyEvents.LivesChanged with new _lives value
    }

    public void Cleanup()
    {
        // TODO: Unsubscribe from CombatEvents.EnemyDeath
        // TODO: Unsubscribe from CombatEvents.EnemyReachedEnd
    }
}
```

- Plain C# — no `MonoBehaviour`, no `GameObject`, testable in isolation
- Subscribes in constructor, unsubscribes in `Cleanup()`
- `GameBootstrapper` calls `Cleanup()` before clearing event channels in `OnDestroy`

### Step 9: Subscription Lifecycle — Subscribe/Unsubscribe Patterns

Different lifecycles need different patterns:

**MonoBehaviour (pooled objects, UI):**
```csharp
private void OnEnable()
{
    // TODO: Services.Get<CombatEvents>().EnemyDeath.Subscribe(OnEnemyDeath);
}

private void OnDisable()
{
    // TODO: Services.Get<CombatEvents>().EnemyDeath.Unsubscribe(OnEnemyDeath);
}
```

- `OnEnable` / `OnDisable` pair is safe for pooled objects that activate/deactivate repeatedly
- Automatically handles scene unloads and object destruction

**Plain C# classes (PlayerStats, etc.):**
```csharp
public PlayerStats(/* ... */)
{
    // Subscribe in constructor
}

public void Cleanup()
{
    // Unsubscribe in Cleanup — called by GameBootstrapper
}
```

!!! warning "Event Leaks"
    Forgetting to unsubscribe causes:
    - **Null reference errors** when a destroyed object's callback is invoked
    - **Duplicate calls** if an object subscribes multiple times without unsubscribing
    - **Subtle bugs** that only appear after object pooling cycles

    Always pair subscribe with unsubscribe. For `MonoBehaviour`s, use `OnEnable`/`OnDisable`. For plain C# classes, provide a `Cleanup()` method.

### Step 10: `Clear()` on Scene Transition

When transitioning between scenes (e.g., main menu → game → game over), stale event subscriptions cause bugs:

- A subscriber from the old scene still exists in the channel's delegate list
- When the event fires, it calls a method on a destroyed object — null reference
- Or worse: a pooled object subscribes in `OnEnable`, gets recycled, and the old subscription fires alongside the new one

**Solution:** `GameBootstrapper.OnDestroy()` calls `Clear()` on every channel, then `Services.Reset()`.

```
Scene unloading:
  1. GameBootstrapper.OnDestroy() fires
  2. PlayerStats.Cleanup() — unsubscribes from combat events
  3. Each EventChannel.Clear() — removes all remaining subscribers
  4. Services.Reset() — clears the service dictionary
  5. New scene loads with fresh GameBootstrapper
```

The order matters: `Cleanup()` first (graceful unsubscribe), then `Clear()` (safety net for anything missed), then `Reset()`.

---

## Episode Recap

- Direct references and singletons create tight coupling — events decouple publishers from subscribers
- `EventChannel` carries no data — use for simple signals (enemy reached end, wave completed, game paused)
- `EventChannel<T>` carries typed data — use for value propagation (gold reward, new gold total, lives remaining)
- Event registries (`CombatEvents`, `WaveEvents`, `EconomyEvents`, `GameEvents`) group related channels — non-static, registered in `Services`
- `Services.Get<T>()` provides central access — no SO assets, no serialized references for infrastructure
- `GameBootstrapper` is the composition root — registration order matters, cleanup order matters
- `PlayerStats` is a plain C# class that subscribes in its constructor and unsubscribes in `Cleanup()`
- Always pair subscribe with unsubscribe — `OnEnable`/`OnDisable` for `MonoBehaviour`, `Cleanup()` for plain C#
- `Clear()` on every channel during scene transitions prevents stale callbacks

---

## Challenge

Add a `DamageEvents` registry with a `TypedEventChannel<DamageReport>` where:

```csharp
public struct DamageReport
{
    public Vector3 Position;
    public int Amount;
    public bool WasKilled;
}
```

Consider:

- What systems would subscribe? (damage numbers, screen shake, achievement tracker, status effects)
- Why is a `struct` better than passing the `EnemyController` itself? (think: what happens when the enemy is pooled immediately after `Die()`?)
- How would `PlayerStats` use `WasKilled` to differentiate between "enemy died" and "enemy took damage but survived"?
- Where would you register `DamageEvents` relative to `CombatEvents` in `GameBootstrapper`?