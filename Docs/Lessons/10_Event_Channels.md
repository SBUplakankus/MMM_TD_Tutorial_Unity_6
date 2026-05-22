# Episode 10 — Event Channels

<!-- Video placeholder -->
<iframe width="560" height="315" src="https://www.youtube.com/embed/PLACEHOLDER_EP10" frameborder="0"></iframe>

---

## Learning Objectives

- Identify tight coupling where systems directly call methods on other systems
- Implement `EventChannel` (non-generic) and `EventChannel<T>` (generic) base classes
- Create typed event registries (`CombatEvents`, `WaveEvents`, `EconomyEvents`, `GameEvents`)
- Subscribe, raise, and unsubscribe from event channels correctly
- Refactor `EnemyController` to raise events instead of calling `PlayerStats` directly
- Refactor `PlayerStats` to subscribe to events instead of being called directly

## Key Concepts

- [Observer Pattern](../Concepts/Observer_Pattern.md)
- [Interfaces](../Concepts/Interfaces.md)

---

## What We're Starting With

Our game uses the Service Locator from Episode 09. But combat code is still tightly coupled — `EnemyController.Die()` reaches through Services to call `PlayerStats.AddGold()` and `PlayerStats.AddKill()`. `WaveManager` directly checks enemy counts on `ObjectPoolManager`. Systems reach into each other's internals instead of broadcasting intent.

---

## The Naive Version

```csharp
// EnemyController.cs — the problem
public void Die()
{
    // TODO: Direct dependency on PlayerStats — EnemyController
    //       shouldn't need to know about gold and kills.
    //       Every new system that cares about enemy death requires
    //       editing EnemyController.
    Services.Get<PlayerStats>().AddGold(enemyData.goldReward);
    Services.Get<PlayerStats>().AddKill();
    Services.Get<ObjectPoolManager>().ReturnEnemy(gameObject);
}

// WaveManager.cs — the problem
private void CheckWaveComplete()
{
    // TODO: Direct dependency on ObjectPoolManager internals
    //       WaveManager is reaching into the pool to count active enemies
    //       instead of being notified that an enemy died.
    int active = Services.Get<ObjectPoolManager>().ActiveEnemyCount;
    if (active == 0) { /* start next wave */ }
}
```

Each new feature (audio on kill, UI update on gold change, achievement tracking) requires modifying `EnemyController` or `WaveManager`. The dependency graph grows in all directions.

---

## The Refactor

We introduce `EventChannel` and `EventChannel<T>` — lightweight pub/sub channels. Systems raise events; other systems subscribe. The raiser doesn't know or care who's listening.

### Architecture Context

```
┌───────────────┐         ┌──────────────────────────────────┐
│ EventChannel   │         │         Registries                │
│ (void events)  │         │                                  │
└───────────────┘         │  CombatEvents                    │
                           │    .EnemyDied (EventChannel<EnemyData>)
┌───────────────┐         │    .EnemyReachedEnd (EventChannel<EnemyData>)
│ EventChannel<T>│         │    .TowerFired (EventChannel<TowerData>)
│ (typed events) │         │                                  │
└───────────────┘         │  WaveEvents                      │
                           │    .WaveStarted (EventChannel<int>)
┌───────────────┐         │    .WaveCompleted (EventChannel<int>)
│  Services      │         │                                  │
│  (holds refs)  │─────────│  EconomyEvents                   │
└───────────────┘         │    .GoldChanged (EventChannel<int>)
                           │    .LivesChanged (EventChannel<int>)
                           │                                  │
                           │  GameEvents                      │
                           │    .GamePaused (EventChannel)    │
                           │    .GameOver (EventChannel)      │
                           └──────────────────────────────────┘

  EnemyController.Die()              PlayerStats
       │                                  │
       │ raises                            │ subscribes
       ▼                                  ▼
  CombatEvents.EnemyDied    ──►   AddGold(data.goldReward)
                                      AddKill()
```

### Code Roadmap

| File | Purpose |
|------|---------|
| `Events/EventChannel.cs` | Non-generic event channel (void events) |
| `Events/EventChannelT.cs` | Generic event channel `EventChannel<T>` |
| `Events/Registries/CombatEvents.cs` | Combat-related event channel instances |
| `Events/Registries/WaveEvents.cs` | Wave-related event channel instances |
| `Events/Registries/EconomyEvents.cs` | Economy-related event channel instances |
| `Events/Registries/GameEvents.cs` | General game event channel instances |
| `Enemies/Controllers/EnemyController.cs` | Raise events instead of calling PlayerStats |
| `Systems/Game/PlayerStats.cs` | Subscribe to events instead of being called |
| `Core/GameBootstrapper.cs` | Register event registries in Services |

---

## Step-by-Step Implementation

### Step 1 — Create EventChannel (non-generic)

Create `Events/EventChannel.cs`:

```csharp
using System;

namespace Events
{
    public class EventChannel
    {
        // TODO: Private event Action _onEvent

        // TODO: public void Subscribe(Action handler)
        //       Add handler to _onEvent delegate

        // TODO: public void Unsubscribe(Action handler)
        //       Remove handler from _onEvent delegate

        // TODO: public void Raise()
        //       Invoke _onEvent if not null
    }
}
```

Use this for events that carry no data — e.g. `GamePaused`, `GameOver`.

### Step 2 — Create EventChannel<T> (generic)

Create `Events/EventChannelT.cs`:

```csharp
using System;

namespace Events
{
    public class EventChannel<T>
    {
        // TODO: Private event Action<T> _onEvent

        // TODO: public void Subscribe(Action<T> handler)
        //       Add handler to _onEvent

        // TODO: public void Unsubscribe(Action<T> handler)
        //       Remove handler from _onEvent

        // TODO: public void Raise(T data)
        //       Invoke _onEvent with data parameter
    }
}
```

Use this for events that carry data — e.g. `EnemyDied` passes `EnemyData`, `GoldChanged` passes the new gold amount.

### Step 3 — Create event registries

Create `Events/Registries/CombatEvents.cs`:

```csharp
using Data;

namespace Events.Registries
{
    public class CombatEvents
    {
        // TODO: public EventChannel<EnemyData> EnemyDied = new();
        // TODO: public EventChannel<EnemyData> EnemyReachedEnd = new();
        // TODO: public EventChannel<TowerData> TowerFired = new();
    }
}
```

Create `Events/Registries/WaveEvents.cs`:

```csharp
namespace Events.Registries
{
    public class WaveEvents
    {
        // TODO: public EventChannel<int> WaveStarted = new();
        // TODO: public EventChannel<int> WaveCompleted = new();
    }
}
```

Create `Events/Registries/EconomyEvents.cs`:

```csharp
namespace Events.Registries
{
    public class EconomyEvents
    {
        // TODO: public EventChannel<int> GoldChanged = new();
        // TODO: public EventChannel<int> LivesChanged = new();
    }
}
```

Create `Events/Registries/GameEvents.cs`:

```csharp
namespace Events.Registries
{
    public class GameEvents
    {
        // TODO: public EventChannel GamePaused = new();
        // TODO: public EventChannel GameOver = new();
        // TODO: public EventChannel GameRestart = new();
    }
}
```

### Step 4 — Register event registries in GameBootstrapper

Update `Core/GameBootstrapper.cs`:

```csharp
using Events.Registries;

namespace Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        // Existing fields...

        // TODO: Create and register event registries
        //       var combatEvents = new CombatEvents();
        //       var waveEvents = new WaveEvents();
        //       var economyEvents = new EconomyEvents();
        //       var gameEvents = new GameEvents();
        //
        //       Services.Register(combatEvents);
        //       Services.Register(waveEvents);
        //       Services.Register(economyEvents);
        //       Services.Register(gameEvents);
    }
}
```

### Step 5 — Refactor EnemyController to raise events

Update `Enemies/Controllers/EnemyController.cs`:

```csharp
using Events.Registries;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IPoolable
    {
        // TODO: In Die()
        //       REMOVE direct PlayerStats calls:
        //         Services.Get<PlayerStats>().AddGold(data.goldReward);
        //         Services.Get<PlayerStats>().AddKill();
        //
        //       REPLACE with event raise:
        //         Services.Get<CombatEvents>().EnemyDied.Raise(data);
        //
        //       Keep the pool return:
        //         Services.Get<ObjectPoolManager>().ReturnEnemy(gameObject);

        // TODO: In ReachEnd() (when enemy reaches the end of the path)
        //       REMOVE direct PlayerStats.TakeDamage call
        //       REPLACE with:
        //         Services.Get<CombatEvents>().EnemyReachedEnd.Raise(data);
        //         Services.Get<ObjectPoolManager>().ReturnEnemy(gameObject);
    }
}
```

`EnemyController` no longer knows about `PlayerStats`, gold, kills, or lives. It just says "I died" or "I reached the end" — and whoever cares, listens.

### Step 6 — Refactor PlayerStats to subscribe

Update `Systems/Game/PlayerStats.cs`:

```csharp
using Events.Registries;

namespace Systems.Game
{
    public class PlayerStats
    {
        private CombatEvents _combatEvents;
        private EconomyEvents _economyEvents;

        // TODO: Constructor — subscribe to events
        //       public PlayerStats(int startingGold, int startingLives,
        //           CombatEvents combatEvents, EconomyEvents economyEvents)
        //       {
        //           _combatEvents = combatEvents;
        //           _economyEvents = economyEvents;
        //           _combatEvents.EnemyDied.Subscribe(OnEnemyDied);
        //           _combatEvents.EnemyReachedEnd.Subscribe(OnEnemyReachedEnd);
        //       }

        // TODO: private void OnEnemyDied(EnemyData data)
        //       {
        //           AddGold(data.goldReward);
        //           AddKill();
        //       }

        // TODO: private void OnEnemyReachedEnd(EnemyData data)
        //       {
        //           TakeDamage(1);
        //       }

        // TODO: private void AddGold(int amount)
        //       {
        //           _gold += amount;
        //           _economyEvents.GoldChanged.Raise(_gold);
        //       }
        //       (Similarly for TakeDamage → LivesChanged)

        // TODO: public void Dispose() or Cleanup()
        //       Unsubscribe from all events to prevent leaks:
        //           _combatEvents.EnemyDied.Unsubscribe(OnEnemyDied);
        //           _combatEvents.EnemyReachedEnd.Unsubscribe(OnEnemyReachedEnd);
    }
}
```

**Critical**: Always unsubscribe in cleanup. Failing to unsubscribe causes "ghost listeners" — destroyed objects still receiving events, which is the #1 source of bugs in event-driven code.

### Step 7 — Refactor WaveManager to use events

Update `Systems/Game/WaveManager.cs`:

```csharp
using Events.Registries;

namespace Systems.Game
{
    public class WaveManager : MonoBehaviour
    {
        // TODO: In Awake() or Start()
        //       Subscribe to CombatEvents.EnemyDied and CombatEvents.EnemyReachedEnd
        //       Track kill count vs. expected count per wave

        // TODO: REMOVE direct ObjectPoolManager.ActiveEnemyCount checks
        //       REPLACE with event-driven tracking:
        //       When enemiesKilledThisWave == totalEnemiesInWave
        //           → Services.Get<WaveEvents>().WaveCompleted.Raise(currentWave);

        // TODO: When starting a wave:
        //       Services.Get<WaveEvents>().WaveStarted.Raise(currentWave);
    }
}
```

---

## Episode Recap

- **Naive**: Direct method calls between systems → `EnemyController` depends on `PlayerStats`, `WaveManager` depends on `ObjectPoolManager` internals
- **Refactor**: `EventChannel` / `EventChannel<T>` decouple raisers from listeners; registries group related events; `GameBootstrapper` wires everything
- `EnemyController` only raises events — it doesn't know who receives them
- `PlayerStats` subscribes to events it cares about — no direct dependency on `EnemyController`
- **Always unsubscribe** to prevent ghost listeners and memory leaks

---

## Challenge

1. Add an `AchievementSystem` class that subscribes to `CombatEvents.EnemyDied` and tracks kill streaks. Notice that you didn't need to modify `EnemyController` at all — that's the power of decoupling.

2. What happens if you forget to unsubscribe `PlayerStats` from `CombatEvents` in a scene reload? Write a test or add a debug log that catches this bug.