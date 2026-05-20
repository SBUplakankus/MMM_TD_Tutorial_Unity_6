# Episode 03: Enemy Type Composition

!!! info "Episode Type: Code Lesson + Inspector Wiring"
    You'll understand `EnemyController` composition, then create config ScriptableObject assets in the Unity Editor. ~20 min.

---

## Video

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden;">
  <iframe src="https://www.youtube.com/embed/EP03_PLACEHOLDER" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%;" frameborder="0" allowfullscreen></iframe>
</div>

**Duration:** ~20 min

---

## Learning Objectives

By the end of this episode you will be able to:

1. Explain how `EnemyData` composes a `HealthConfig` and `MovementConfig` into a single enemy definition
2. Create `HealthConfig` and `MovementConfig` ScriptableObject assets in the Unity Editor
3. Wire four enemy types in the Inspector using config SOs — no strategy SO instances
4. Trace `EnemyController.Initialize()` through `StrategyFactory` to see how config → concrete strategy
5. Compare `NormalHealth.TakeDamage` vs `ArmouredHealth.TakeDamage` — straight subtraction vs percentage reduction
6. Compare `GroundedPath.Tick` vs `FlyingPath.Tick` — same waypoint system with Y offset

---

## Key Concepts

| Concept | How It's Applied | Learn More |
|---------|-----------------|------------|
| Config SOs | `HealthConfig` / `MovementConfig` hold **data only** (no behaviour) | [Scriptable Objects](../Concepts/Scriptable_Objects.md) |
| Strategy factory | `StrategyFactory` reads config type enum → `new ConcreteStrategy(config values)` | [Abstraction](../Concepts/Abstraction.md) |
| Interfaces for behaviour | `IHealthStrategy` / `IMovementStrategy` — plain C# classes, not SOs | [Interfaces](../Concepts/Interfaces.md) |
| Open/Closed Principle | Add new enemy types by creating config SOs + factory case — existing code unchanged | [Interfaces](../Concepts/Interfaces.md) |

---

## Code Roadmap

| File | Action | Notes |
|------|--------|-------|
| `Data/HealthConfig.cs` | **Create** in code + Inspector | Pure data SO — `HealthType` enum + numeric fields |
| `Data/MovementConfig.cs` | **Create** in code + Inspector | Pure data SO — `MovementType` enum + numeric fields |
| `Data/EnemyData.cs` | **Understand** (already implemented) | Now holds `HealthConfig` + `MovementConfig` refs |
| `Systems/Parsing/StrategyFactory.cs` | **Create** in code | Static factory — `CreateHealth` / `CreateMovement` from config |
| `Strategies/Health/NormalHealth.cs` | **Create** in code | Plain class, `IHealthStrategy`, constructor `(startHealth)` |
| `Strategies/Health/ArmouredHealth.cs` | **Create** in code | Plain class, `IHealthStrategy`, constructor `(startHealth, armourPercent)` |
| `Strategies/Movement/GroundedPath.cs` | **Create** in code | Plain class, `IMovementStrategy`, constructor `(moveSpeed)` |
| `Strategies/Movement/FlyingPath.cs` | **Create** in code | Plain class, `IMovementStrategy`, constructor `(moveSpeed, flyingHeight)` |
| `Enemies/Controllers/EnemyController.cs` | **Understand** (already implemented) | Creates strategies via `StrategyFactory` in `Initialize()` |
| Config SO assets | **Create** in Inspector | `HealthConfig` and `MovementConfig` instances |
| Enemy data SO assets | **Create** in Inspector | 4 enemy type SOs referencing config SOs |

---

## Architecture Context

```
┌──────────────────────────────────────────────────────────┐
│                      EnemyData SO                          │
│  ┌──────────────────┐         ┌──────────────────────┐     │
│  │  HealthConfig    │         │  MovementConfig       │     │
│  │  (data SO ref)   │         │  (data SO ref)        │     │
│  │  type: Normal    │         │  type: Grounded       │     │
│  │  startHealth: 100│         │  moveSpeed: 3.0       │     │
│  └────────┬─────────┘         └──────────┬───────────┘     │
└───────────┼───────────────────────────────┼────────────────┘
            │                               │
            ▼                               ▼
  ┌───────────────────┐           ┌───────────────────┐
  │  StrategyFactory   │           │  StrategyFactory   │
  │  .CreateHealth()   │           │  .CreateMovement() │
  └────────┬──────────┘           └────────┬──────────┘
           │                               │
           ▼                               ▼
  ┌──────────────────┐           ┌──────────────────┐
  │ NormalHealth     │           │ GroundedPath     │
  │ (plain C# class) │           │ (plain C# class) │
  └──────────────────┘           └──────────────────┘
           │                               │
           └───────────────┬───────────────┘
                           ▼
                ┌──────────────────┐
                │ EnemyController  │  ← receives IHealthStrategy + IMovementStrategy
                └──────────────────┘
```

The `EnemyData` SO holds **config references**, not strategy references. At runtime, `StrategyFactory` translates config data + type enum into concrete strategy instances. Each enemy gets its own `NormalHealth` or `ArmouredHealth` object — no shared state.

---

## Step-by-Step Implementation

### Step 1: Understand the config→factory→strategy pipeline

The old architecture wired strategy SOs directly:

```
EnemyData → [NormalHealth SO asset] → shared CurrentHealth ← BUG
```

The new architecture separates data from behaviour:

```
EnemyData → HealthConfig (data: type=Normal, startHealth=100)
         → StrategyFactory.CreateHealth(config) → new NormalHealth(100)  ← per-instance state
```

**Data lives in SOs.** Behaviour lives in plain C# classes. `StrategyFactory` bridges them.

---

### Step 2: Create `HealthConfig` SOs

`HealthConfig` is a pure data SO — it knows *what kind* of health (via `HealthType` enum) and *what values*, but contains zero behaviour logic.

Create `Assets/Scripts/Data/HealthConfig.cs`:

```csharp
using UnityEngine;

namespace Data
{
    public enum HealthType
    {
        // TODO: Normal, Armoured, Shield, Regen
    }

    [CreateAssetMenu(fileName = "HealthConfig", menuName = "Scriptable Objects/Config/Health")]
    public class HealthConfig : ScriptableObject
    {
        // TODO: type (HealthType enum)
        // TODO: startHealth (int)
        // TODO: armourPercent (float, Range 0-0.99, Armoured only)
        // TODO: shieldPoints (int, Shield only)
        // TODO: regenRate (float, Regen only)
        // TODO: regenDelay (float, Regen only)
        // TODO: Public properties for each field
    }
}
```

In the Unity Project window, right-click and create:

1. **Right-click** → `Create > Scriptable Objects > Config > Health`
   - Name it `NormalHealthConfig`
   - Set `Type` = Normal
   - Set `Start Health` = 100

2. **Right-click** → `Create > Scriptable Objects > Config > Health`
   - Name it `ArmouredHealthConfig`
   - Set `Type` = Armoured
   - Set `Start Health` = 200
   - Set `Armour Percent` = 0.3 (30% damage reduction)

!!! tip "All health data in one SO type"
    One `HealthConfig` class holds fields for *all* health types. The `HealthType` enum tells `StrategyFactory` which strategy to create; unused fields (like `armourPercent` on a Normal config) are simply ignored by the factory. This keeps the SO inspector simple — you don't need a separate SO class per health type.

---

### Step 3: Create `MovementConfig` SOs

The same pattern: `MovementType` enum drives `StrategyFactory.CreateMovement()`.

Create `Assets/Scripts/Data/MovementConfig.cs`:

```csharp
using UnityEngine;

namespace Data
{
    public enum MovementType
    {
        // TODO: Grounded, Flying
    }

    [CreateAssetMenu(fileName = "MovementConfig", menuName = "Scriptable Objects/Config/Movement")]
    public class MovementConfig : ScriptableObject
    {
        // TODO: type (MovementType enum)
        // TODO: moveSpeed (float)
        // TODO: flyingHeight (float, Range 0-5, Flying only)
        // TODO: Public properties for each field
    }
}
```

In the Unity Project window:

1. **Right-click** → `Create > Scriptable Objects > Config > Movement`
   - Name it `GroundedMovementConfig`
   - Set `Type` = Grounded
   - Set `Move Speed` = 5.0

2. **Right-click** → `Create > Scriptable Objects > Config > Movement`
   - Name it `FlyingMovementConfig`
   - Set `Type` = Flying
   - Set `Move Speed` = 4.0
   - Set `Flying Height` = 2.0

---

### Step 4: Create `EnemyData` SOs referencing config SOs

`EnemyData` now holds config references instead of strategy references:

```csharp
[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/Data/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Enemy Config")]
    [SerializeField] private HealthConfig healthConfig;
    [SerializeField] private MovementConfig movementConfig;

    [Header("Enemy Stats")]
    [SerializeField] private int goldGiven;
    [SerializeField] private int damage;

    public HealthConfig HealthConfig => healthConfig;
    public MovementConfig MovementConfig => movementConfig;
    public int GoldGiven => goldGiven;
    public int Damage => damage;
}
```

Create four `EnemyData` SOs — **Right-click** → `Create > Scriptable Objects > Data > Enemy`:

| SO Name | Health Config | Movement Config | Gold Given | Damage |
|---------|---------------|-----------------|------------|--------|
| **Basic** | NormalHealthConfig (100 HP) | GroundedMovementConfig (5.0 speed) | 10 | 1 |
| **Armoured** | ArmouredHealthConfig (200 HP, 30% armour) | GroundedMovementConfig (5.0 speed) | 25 | 2 |
| **Flying** | NormalHealthConfig (100 HP) | FlyingMovementConfig (4.0 speed, height 2.0) | 15 | 1 |
| **FlyingArmoured** | ArmouredHealthConfig (200 HP, 30% armour) | FlyingMovementConfig (4.0 speed, height 2.0) | 40 | 3 |

!!! info "The key insight"
    You're wiring **data**, not behaviour. The `HealthConfig.type` enum tells `StrategyFactory` what to build. Changing "Normal" to "Armoured" in the config is all it takes to swap health behaviour — no strategy SO creation, no code changes.

---

### Step 5: Create `StrategyFactory`

The factory is the bridge between config data and concrete strategy classes:

Create `Assets/Scripts/Systems/Parsing/StrategyFactory.cs`:

```csharp
using Data;
using Interfaces;
using Strategies.Health;
using Strategies.Movement;

namespace Systems.Parsing
{
    public static class StrategyFactory
    {
        // TODO: CreateHealth(HealthConfig config) — switch on config.Type
        //   case Normal:    return new NormalHealth(config.StartHealth)
        //   case Armoured:  return new ArmouredHealth(config.StartHealth, config.ArmourPercent)
        //   case Shield:    return new ShieldHealth(config.StartHealth, config.ShieldPoints)
        //   case Regen:     return new RegenHealth(config.StartHealth, config.RegenRate, config.RegenDelay)

        // TODO: CreateMovement(MovementConfig config) — switch on config.Type
        //   case Grounded:  return new GroundedPath(config.MoveSpeed)
        //   case Flying:    return new FlyingPath(config.MoveSpeed, config.FlyingHeight)
    }
}
```

Each `case` maps a config enum value to a `new` concrete strategy, passing config values as constructor arguments. The return type is the interface — callers never see the concrete class.

---

### Step 6: How `EnemyController.Initialize()` wires everything

When an enemy spawns from the pool and `Initialize()` is called:

```csharp
public void Initialize(EnemyData data, EnemyPath path)
{
    // TODO: Store Path reference
    // TODO: Health = StrategyFactory.CreateHealth(data.HealthConfig)
    // TODO: Movement = StrategyFactory.CreateMovement(data.MovementConfig)
    // TODO: Store GoldGiven and Damage from data
    // TODO: Call Health.Initialize()
    // TODO: Call Movement.Initialize(this)
    // TODO: Subscribe to Movement.OnMovementCompleted → OnReachedEnd
}
```

The flow:

1. `StrategyFactory.CreateHealth(data.HealthConfig)` reads the config type enum and constructs the right class with config values — each enemy gets **its own instance**
2. `StrategyFactory.CreateMovement(data.MovementConfig)` does the same for movement
3. `Health.Initialize()` sets `CurrentHealth = startHealth` — inside the strategy instance, not on a shared SO
4. `Movement.Initialize(this)` stores the enemy reference for waypoint access

This is **delegation through interfaces** — `EnemyController` doesn't implement health or movement logic. It holds interface references and delegates.

---

### Step 7: `NormalHealth.TakeDamage` vs `ArmouredHealth.TakeDamage`

Both are plain C# classes implementing `IHealthStrategy`. They take config values via constructor, not `[SerializeField]`.

**NormalHealth** — straight subtraction:

```csharp
public class NormalHealth : IHealthStrategy
{
    // TODO: Constructor(int startHealth)
    // TODO: _startHealth, _currentHealth fields

    public void Initialize()
    {
        // TODO: _currentHealth = _startHealth
    }

    public DamageResult TakeDamage(float amount)
    {
        // TODO: _currentHealth -= amount
        // TODO: Return DamageResult.Dead(amount) or DamageResult.Alive(amount)
    }

    public void Tick(float deltaTime) { }
}
```

10 damage → 100 HP becomes 90 HP. Returns `DamageResult.Alive(10)`.

**ArmouredHealth** — percentage reduction before subtraction:

```csharp
public class ArmouredHealth : IHealthStrategy
{
    // TODO: Constructor(int startHealth, float armourPercent)
    // TODO: _startHealth, _armourPercent, _currentHealth fields

    public DamageResult TakeDamage(float amount)
    {
        // TODO: var reducedDamage = amount * (1f - _armourPercent)
        // TODO: _currentHealth -= reducedDamage
        // TODO: Return DamageResult based on _currentHealth
    }

    public void Tick(float deltaTime) { }
}
```

With `_armourPercent = 0.3`, taking 10 damage: `10 * 0.7 = 7` → 200 HP becomes 193 HP. The armour absorbs 30% of every hit.

!!! warning "Armour is not additive defence"
    `ArmouredHealth` reduces a *percentage* of incoming damage — it doesn't add a flat HP buffer. Compare this with `ShieldHealth`, which absorbs a *flat amount* first, then passes remaining damage to health. Two very different damage models from the same `IHealthStrategy` interface.

---

### Step 8: `GroundedPath.Tick` vs `FlyingPath.Tick`

Both are plain C# classes implementing `IMovementStrategy`.

**GroundedPath** — move along ground-level waypoints:

```csharp
public class GroundedPath : IMovementStrategy
{
    // TODO: Constructor(float moveSpeed)
    // TODO: _moveSpeed field

    public void Initialize(EnemyController enemy) { /* TODO */ }

    public void Tick(EnemyController enemy)
    {
        // TODO: Move toward current waypoint at _moveSpeed
        // TODO: If at waypoint, increment CurrentWayPointIndex
        // TODO: If no more waypoints, fire OnMovementCompleted
    }
}
```

**FlyingPath** — same waypoints with Y offset:

```csharp
public class FlyingPath : IMovementStrategy
{
    // TODO: Constructor(float moveSpeed, float flyingHeight)
    // TODO: _moveSpeed, _flyingHeight fields

    public void Tick(EnemyController enemy)
    {
        // TODO: Same as GroundedPath but target.y += _flyingHeight
        // TODO: Uses IsAtWaypoint(targetPosition, enemyPosition) overload
    }
}
```

The only difference: `target.y += _flyingHeight`. Flying enemies follow the **same waypoint path** as grounded enemies — they're just at a different height.

---

## Episode Recap

- `EnemyData` is the composition hub: `HealthConfig` ref + `MovementConfig` ref + stats
- Created config SOs: `NormalHealthConfig`, `ArmouredHealthConfig`, `GroundedMovementConfig`, `FlyingMovementConfig`
- Created 4 EnemyData SOs combining those configs (Basic, Armoured, Flying, FlyingArmoured)
- `StrategyFactory` translates config type enum + values → concrete strategy instances
- `EnemyController.Initialize()` creates strategies via `StrategyFactory` — each enemy gets its own instances
- `NormalHealth.TakeDamage` = straight subtraction; `ArmouredHealth.TakeDamage` = percentage reduction
- `GroundedPath.Tick` = ground-level waypoints; `FlyingPath.Tick` = same waypoints + Y offset
- **The key insight:** adding a new enemy type = creating a new `HealthConfig`/`MovementConfig` SO. If the type enum already exists, zero code changes.

---

## Challenge

Create a **"Heavy Flying"** enemy type using `ArmouredHealthConfig` + `FlyingMovementConfig`.

Answer these questions:

1. What `Start Health` and `Armour Percent` would make this enemy feel like a boss-tier threat? (Consider: it already has the speed advantage of flight.)
2. How much `GoldGiven` makes the reward feel proportional to the difficulty?
3. What `Move Speed` would make it challenging but not unfair? (Should it be slower than a normal Flying enemy to offset its tankiness?)
4. If you wanted `ShieldHealth` instead of `ArmouredHealth`, you'd need a `HealthConfig` with `type=Shield`. Would `StrategyFactory` already handle it, or would you need to add a `case` first?

Create the config SOs in the Inspector and test it by spawning it alongside the other enemy types.