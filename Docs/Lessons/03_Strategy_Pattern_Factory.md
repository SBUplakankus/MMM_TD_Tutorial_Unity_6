# Episode 03: Strategy Pattern + Factory

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden; max-width: 100%; margin: 1.5rem 0;">
  <iframe src="https://www.youtube.com/embed/PLACEHOLDER_EP03" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; border:0;" title="Episode 03" allowfullscreen></iframe>
</div>

## Learning Objectives

- Recognise the copy-paste explosion problem when adding enemy types
- Extract interchangeable behaviours with the **Strategy Pattern**
- Use a **Factory** to create strategy instances from configuration data
- Separate data from behaviour with **ScriptableObject configs**

## Key Concepts

- [Strategy Pattern](../Concepts/Strategy_Pattern.md)
- [Factory Pattern](../Concepts/Factory_Pattern.md)

## What We're Starting With

- `EnemyController` implements `IDamageable` and `ITargetable`
- Movement is hardcoded in `EnemyController`
- Health is a `_currentHealth` field directly in the controller
- We want *different* enemies: grounded, flying, fast, etc.

---

## The Naive Version

We want a flying enemy. The "obvious" solution: copy `EnemyController` into `GroundedEnemyController` and `FlyingEnemyController`.

```csharp
namespace Enemies.Controllers
{
    public class GroundedEnemyController : MonoBehaviour, IDamageable, ITargetable
    {
        // TODO: identical to EnemyController — moves along ground waypoints
    }
}

public class FlyingEnemyController : MonoBehaviour, IDamageable, ITargetable
{
    // TODO: same as Grounded BUT with a height offset
    // TODO: same TakeDamage logic — copy-pasted
}
```

**The problem:**

- Adding an *armoured* enemy means *another* controller with yet another copy of `TakeDamage`
- Fixing a bug in movement? Hunt down every controller
- Health logic duplicated across every controller — classic copy-paste explosion

---

## The Refactor

Instead of copy-pasting controllers, we extract two **strategies**:

| Strategy | What it swaps | Interface |
|----------|--------------|-----------|
| Movement | How the enemy moves along the path | `IMovementStrategy` |
| Health | How the enemy takes damage | `IHealthStrategy` *(preview — full implementation in Ep 04)* |

We also introduce:

- **`DamageResult`** — a struct returned by `IHealthStrategy.TakeDamage` so callers know what happened
- **`StrategyFactory`** — creates strategy instances from config data
- **`HealthConfig`** / **`MovementConfig`** — ScriptableObjects holding pure data (no behaviour)

### Strategy interfaces (skeleton)

```csharp
namespace Interfaces
{
    public interface IMovementStrategy
    {
        // TODO: void Initialise(Transform enemy, Transform[] waypoints);
        // TODO: void Tick(float deltaTime);
        // TODO: float PathProgress { get; }
    }
}
```

```csharp
namespace Interfaces
{
    public interface IHealthStrategy
    {
        // TODO: void Initialise(HealthConfig config);
        // TODO: DamageResult TakeDamage(float amount);
        // TODO: bool IsAlive { get; }
        // TODO: float CurrentHealth { get; }
        // TODO: float MaxHealth { get; }
    }
}
```

### DamageResult struct (skeleton)

```csharp
namespace Data
{
    public struct DamageResult
    {
        // TODO: float DamageTaken;
        // TODO: float RemainingHealth;
        // TODO: bool WasKilled;
    }
}
```

### Config ScriptableObjects (skeleton)

```csharp
namespace Data
{
    [CreateAssetMenu(menuName = "TD/HealthConfig")]
    public class HealthConfig : ScriptableObject
    {
        // TODO: float MaxHealth;
    }
}
```

```csharp
namespace Data
{
    [CreateAssetMenu(menuName = "TD/MovementConfig")]
    public class MovementConfig : ScriptableObject
    {
        // TODO: float Speed;
        // TODO: MovementType Type; // Grounded, Flying, etc.
    }
}
```

### StrategyFactory (skeleton)

```csharp
namespace Systems.Parsing
{
    public static class StrategyFactory
    {
        // TODO: static IHealthStrategy CreateHealth(HealthConfig config)
        // TODO: static IMovementStrategy CreateMovement(MovementConfig config)
    }
}
```

---

## Code Roadmap

| File | Purpose |
|------|---------|
| `Interfaces/IMovementStrategy.cs` | Movement behaviour contract |
| `Interfaces/IHealthStrategy.cs` | Health behaviour contract *(preview)* |
| `Data/DamageResult.cs` | Struct returned by TakeDamage |
| `Data/HealthConfig.cs` | SO — health data (max health, armour, etc.) |
| `Data/MovementConfig.cs` | SO — movement data (speed, type) |
| `Systems/Parsing/StrategyFactory.cs` | Creates strategy instances from configs |
| `Enemies/Controllers/EnemyController.cs` | Refactored — delegates to strategies |

### EnemyController.cs — skeleton after refactor

```csharp
namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable
    {
        // TODO: IMovementStrategy _movement;
        // TODO: IHealthStrategy _health;

        // TODO: Start — create strategies via StrategyFactory, call Initialise
        // TODO: Update — _movement.Tick(Time.deltaTime)

        // --- IDamageable ---
        // TODO: public void TakeDamage(float amount) → _health.TakeDamage(amount)

        // --- ITargetable ---
        // TODO: Position, IsAlive, PathProgress delegate to strategies
    }
}
```

---

## Step-by-Step Implementation

### 1 — Create DamageResult

Create `Assets/Scripts/Data/DamageResult.cs`:

```csharp
namespace Data
{
    public struct DamageResult
    {
        // TODO: public float DamageTaken;
        // TODO: public float RemainingHealth;
        // TODO: public bool WasKilled;

        // TODO: static DamageResult Create(float damage, float remaining, bool killed)
    }
}
```

> **Why a struct?** `DamageResult` is a small data packet that's created and discarded every frame during combat. Structs avoid GC allocation — important when dozens of enemies take damage simultaneously.

### 2 — Create IMovementStrategy

Create `Assets/Scripts/Interfaces/IMovementStrategy.cs`:

```csharp
namespace Interfaces
{
    public interface IMovementStrategy
    {
        // TODO: void Initialise(Transform enemy, Transform[] waypoints);
        // TODO: void Tick(float deltaTime);
        // TODO: float PathProgress { get; }
    }
}
```

### 3 — Create IHealthStrategy (preview)

Create `Assets/Scripts/Interfaces/IHealthStrategy.cs`:

```csharp
namespace Interfaces
{
    public interface IHealthStrategy
    {
        // TODO: void Initialise(HealthConfig config);
        // TODO: DamageResult TakeDamage(float amount);
        // TODO: bool IsAlive { get; }
        // TODO: float CurrentHealth { get; }
        // TODO: float MaxHealth { get; }
    }
}
```

> We won't implement a concrete health strategy until Episode 04. For now, the interface exists so `EnemyController` has a slot for it.

### 4 — Create config ScriptableObjects

Create `Assets/Scripts/Data/HealthConfig.cs`:

```csharp
namespace Data
{
    [CreateAssetMenu(menuName = "TD/HealthConfig")]
    public class HealthConfig : ScriptableObject
    {
        // TODO: public float MaxHealth = 100f;
    }
}
```

Create `Assets/Scripts/Data/MovementConfig.cs`:

```csharp
namespace Data
{
    public enum MovementType { Grounded, Flying }

    [CreateAssetMenu(menuName = "TD/MovementConfig")]
    public class MovementConfig : ScriptableObject
    {
        // TODO: public float Speed = 3f;
        // TODO: public MovementType Type = MovementType.Grounded;
    }
}
```

### 5 — Create StrategyFactory

Create `Assets/Scripts/Systems/Parsing/StrategyFactory.cs`:

```csharp
namespace Systems.Parsing
{
    public static class StrategyFactory
    {
        // TODO: public static IHealthStrategy CreateHealth(HealthConfig config)
        //       — switch on config type, return new NormalHealth(config)

        // TODO: public static IMovementStrategy CreateMovement(MovementConfig config)
        //       — switch on config.Type, return new GroundedPath or FlyingPath
    }
}
```

> The factory doesn't need `StrategyFactory` yet — right now `GroundedPath` copies what `EnemyController` did. We'll add `FlyingPath` in Episode 05.

### 6 — Create GroundedPath (minimal)

Create `Assets/Scripts/Strategies/Movement/GroundedPath.cs`:

```csharp
namespace Strategies.Movement
{
    public class GroundedPath : IMovementStrategy
    {
        // TODO: Transform _enemy;
        // TODO: Transform[] _waypoints;
        // TODO: int _waypointIndex;
        // TODO: float _speed;
        // TODO: float _pathProgress;

        // TODO: Initialise — store enemy transform, waypoints, speed from config
        // TODO: Tick — move toward current waypoint, advance, update _pathProgress
        // TODO: PathProgress => _pathProgress;
    }
}
```

> This is the same movement logic that was in `EnemyController`, just moved into a strategy class. The controller will delegate to it.

### 7 — Refactor EnemyController to use strategies

```csharp
namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable
    {
        [SerializeField] private EnemyPath _path;
        [SerializeField] private HealthConfig _healthConfig;
        [SerializeField] private MovementConfig _movementConfig;

        private IMovementStrategy _movement;
        private IHealthStrategy _health;

        private void Start()
        {
            // TODO: _movement = StrategyFactory.CreateMovement(_movementConfig);
            // TODO: _movement.Initialise(transform, _path.Waypoints);

            // TODO: _health = StrategyFactory.CreateHealth(_healthConfig);
            // TODO: _health.Initialise(_healthConfig);
        }

        private void Update()
        {
            // TODO: _movement.Tick(Time.deltaTime);
        }

        // --- IDamageable ---
        public void TakeDamage(float amount)
        {
            // TODO: _health.TakeDamage(amount);
        }

        // --- ITargetable ---
        public Vector3 Position => transform.position;
        public bool IsAlive => _health.IsAlive;
        public float PathProgress => _movement.PathProgress;
    }
}
```

### 8 — Create config assets in Unity

1. Right-click in the Project window → **Create → TD → HealthConfig** — name it `NormalHealth`, set `MaxHealth = 100`
2. Right-click → **Create → TD → MovementConfig** — name it `GroundedFast`, set `Speed = 3`, `Type = Grounded`
3. On your Enemy prefab, drag both configs into the `EnemyController` slots

---

## Episode Recap

- **Naive**: copy-paste `EnemyController` for each enemy type — explosion of near-identical classes
- **Refactor**: extract `IMovementStrategy` and `IHealthStrategy` — `EnemyController` delegates behaviour instead of owning it
- `StrategyFactory` decides *which* strategy to create from config data
- `DamageResult` gives callers feedback about what happened
- Config data (ScriptableObjects) is separate from behaviour (strategy classes)
- **Adding a new enemy type = new config + new strategy class, not a new controller**

Movement strategies are working, but `EnemyController` still has no real health system — `IHealthStrategy` is just a slot. Episode 04 implements `NormalHealth` and the full damage pipeline.

## Challenge

Create a `SlowMovementConfig` ScriptableObject asset with `Speed = 1.5f`. You should be able to create a slow enemy by assigning this config on the prefab — zero code changes. Verify that the enemy moves slower in Play mode.

<details>
<summary>Hint</summary>

Just duplicate the `GroundedFast` config asset, rename it `GroundedSlow`, and change the `Speed` value. Swap it on the prefab — same `GroundedPath` strategy, different speed. That's the power of data-driven design.

</details>