# Episode 09 — Service Locator

<!-- Video placeholder -->
<iframe width="560" height="315" src="https://www.youtube.com/embed/PLACEHOLDER_EP09" frameborder="0" allowfullscreen></iframe>

---

## Learning Objectives

- Identify the problems with multiple singletons scattered across a codebase
- Implement a `Services` static class with `Register`, `Get`, and `Clear` methods
- Create a `GameBootstrapper` as a composition root that wires up all services
- Refactor `ObjectPoolManager` and `UpdateManager` from singletons to services
- Convert `PlayerStats` from a MonoBehaviour to a plain C# class registered in Services

## Key Concepts

- [Service Locator](../Concepts/Service_Locator.md)
- [Interfaces](../Concepts/Interfaces.md)

---

## What We're Starting With

Our game has pooling working, but we access `ObjectPoolManager.Instance` and `UpdateManager.Instance` from everywhere. PlayerStats is a MonoBehaviour attached to a GameObject. Each manager is a singleton — tightly coupled to its own global access pattern.

---

## The Naive Version

```csharp
// Scattered across the codebase — the problem
ObjectPoolManager.Instance.GetEnemy();
ObjectPoolManager.Instance.ReturnEnemy(go);
UpdateManager.Instance.Register(enemy);
PlayerStats.Instance.AddGold(10);
PlayerStats.Instance.TakeDamage(5);
```

Every class that needs a manager reaches through its `.Instance` property. Want to swap `ObjectPoolManager` for a test double? You can't — every call site hard-codes the concrete class. Want to change `PlayerStats` to not be a MonoBehaviour? Every caller breaks.

**Dependency inversion is violated**: high-level gameplay code depends on concrete manager classes instead of abstractions.

---

## The Refactor

We introduce a central `Services` class that acts as a registry, and a `GameBootstrapper` that wires everything up in one place (the composition root).

### Architecture Context

```
┌─────────────────────────────────┐
│        GameBootstrapper         │
│  (Composition Root — Scene GO)  │
│                                 │
│  Awake() {                      │
│    Services.Register(poolMgr);  │
│    Services.Register(updMgr);   │
│    Services.Register(playerStats│
│  }                              │
└─────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────┐
│          Services               │
│  Dictionary<Type, object>       │
│                                │
│  Register<T>(T service)        │
│  Get<T>() → T                  │
│  Clear()                        │
└─────────────────────────────────┘
         │
    Get<T>() resolves
         │
    ┌────┼────┬──────────────┐
    ▼         ▼              ▼
ObjectPool  Update      PlayerStats
Manager     Manager     (plain C#)
```

### Code Roadmap

| File | Purpose |
|------|---------|
| `Core/Services.cs` | Static service locator — Register, Get, Clear |
| `Core/GameBootstrapper.cs` | Composition root — creates and registers all services in Awake |
| `Systems/Managers/ObjectPoolManager.cs` | Remove singleton (Instance), keep pool logic |
| `Systems/Managers/UpdateManager.cs` | Remove singleton (Instance), keep tick logic |
| `Systems/Game/PlayerStats.cs` | Convert from MonoBehaviour to plain C# class |
| *Every file that called .Instance* | Replace with `Services.Get<T>()` |

---

## Step-by-Step Implementation

### Step 1 — Create the Services class

Create `Core/Services.cs`:

```csharp
namespace Core
{
    public static class Services
    {
        // TODO: Private static Dictionary<System.Type, object> _services

        // TODO: public static void Register<T>(T service)
        //       Add service keyed by typeof(T) to the dictionary
        //       Throw if T is already registered

        // TODO: public static T Get<T>()
        //       Retrieve and cast from the dictionary
        //       Throw with clear message if T is not registered

        // TODO: public static void Clear()
        //       Remove all registrations (call on scene unload / cleanup)
    }
}
```

This is a **simple dictionary** — not a full DI container. It's enough to decouple callers from concrete implementations while staying beginner-friendly.

### Step 2 — Create GameBootstrapper

Create `Core/GameBootstrapper.cs`:

```csharp
namespace Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        // TODO: Serialized references to manager GameObjects in the scene
        //       [SerializeField] ObjectPoolManager _poolManager;
        //       [SerializeField] UpdateManager _updateManager;

        // TODO: Awake() — composition root
        //       1. Services.Register<ObjectPoolManager>(_poolManager);
        //       2. Services.Register<UpdateManager>(_updateManager);
        //       3. Create PlayerStats as plain C# and register:
        //          var playerStats = new PlayerStats(startingGold, startingLives);
        //          Services.Register<PlayerStats>(playerStats);
        //       4. Call _poolManager.Initialise() if needed (after registration)

        // TODO: OnDestroy() — cleanup
        //       Services.Clear();
    }
}
```

The bootstrapper is the **single place** where all wiring happens. If you ever need to swap a service for a test double or a mock, you change one line here — not twenty call sites.

### Step 3 — Refactor ObjectPoolManager — remove singleton

Update `Systems/Managers/ObjectPoolManager.cs`:

```csharp
namespace Systems.Managers
{
    public class ObjectPoolManager : MonoBehaviour
    {
        // TODO: REMOVE the static Instance property and Awake singleton setup
        //       The class keeps all its pool logic (GetEnemy, ReturnEnemy, etc.)

        // TODO: Add a public Initialise() method called by GameBootstrapper
        //       Move pre-warm logic here so it runs after Services registration

        // All existing pool methods (GetEnemy, ReturnEnemy, etc.) remain unchanged
        // Only the access pattern changes: Services.Get<ObjectPoolManager>() instead of .Instance
    }
}
```

### Step 4 — Refactor UpdateManager — remove singleton

Update `Systems/Managers/UpdateManager.cs`:

```csharp
namespace Systems.Managers
{
    public class UpdateManager : MonoBehaviour
    {
        // TODO: REMOVE the static Instance property and Awake singleton setup

        // All existing Update/Tick methods remain unchanged
        // Access changes to: Services.Get<UpdateManager>()
    }
}
```

### Step 5 — Convert PlayerStats from MonoBehaviour to plain C#

Update `Systems/Game/PlayerStats.cs`:

```csharp
namespace Systems.Game
{
    // TODO: Remove MonoBehaviour inheritance — this is now a plain C# class
    public class PlayerStats
    {
        // TODO: Keep all existing fields (gold, lives, etc.) as auto-properties
        //       or private fields with public getters

        // TODO: Constructor — PlayerStats(int startingGold, int startingLives)
        //       Initialise all values from parameters (no more serialized fields)

        // TODO: Keep all existing public methods (AddGold, TakeDamage, etc.)
        //       They remain the same — just no more MonoBehaviour lifecycle

        // TODO: Remove any Update() or coroutine logic
        //       If PlayerStats needed frame updates, it should use
        //       Services.Get<UpdateManager>().Register() instead
    }
}
```

Key insight: PlayerStats had no `Update` logic of its own — it was a MonoBehaviour purely so other scripts could reach it via `FindObjectOfType`. Now it's a plain class registered in Services. Cleaner, testable, no GameObject overhead.

### Step 6 — Update every call site

Search the entire codebase for `.Instance` calls and replace them:

```csharp
// BEFORE (everywhere)
ObjectPoolManager.Instance.GetEnemy();
UpdateManager.Instance.Register(this);
PlayerStats.Instance.AddGold(10);

// AFTER (everywhere)
Services.Get<ObjectPoolManager>().GetEnemy();
Services.Get<UpdateManager>().Register(this);
Services.Get<PlayerStats>().AddGold(10);
```

Full list of files to update (search for `.Instance`):

| File | Change |
|------|--------|
| `EnemyController.cs` | `ObjectPoolManager.Instance` → `Services.Get<ObjectPoolManager>()` |
| `ProjectileBase.cs` | same as above |
| `EnemySpawner.cs` | same as above |
| `TowerFiring.cs` | same as above |
| `Any file using UpdateManager` | `UpdateManager.Instance` → `Services.Get<UpdateManager>()` |
| `Any file using PlayerStats` | `PlayerStats.Instance` → `Services.Get<PlayerStats>()` |

---

## Episode Recap

- **Naive**: Multiple singletons (`.Instance`) scattered everywhere → tight coupling, untestable, can't swap implementations
- **Refactor**: `Services` static class centralises service registration and lookup; `GameBootstrapper` is the composition root that wires everything in one place
- `PlayerStats` is now a plain C# class — no MonoBehaviour overhead, easily testable
- `ObjectPoolManager` and `UpdateManager` keep their logic, just lose their singletons
- **Every call site** now goes through `Services.Get<T>()` instead of `.Instance`

---

## Challenge

1. Add a `DebugServices` wrapper that logs every `Register` and `Get` call. How many times is `ObjectPoolManager` retrieved per frame? Is it worth caching the reference in hot paths?

2. Write a unit test for `PlayerStats.AddGold()` — now that it's a plain C# class, you can `new PlayerStats(0, 20)` without a GameObject. Notice how much simpler testing becomes.