# Episode 09: Advanced Behaviors

> Duration: ~15-20 min

## Tutorial Video

<iframe width="560" height="315" src="https://www.youtube.com/embed/PLACEHOLDER_EP09" title="Episode 09: Advanced Behaviors" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

## Learning Objectives

- Implement **ShieldHealth** and **RegenHealth** as plain C# classes implementing `IHealthStrategy`
- Add new health types to `HealthConfig` enum and `StrategyFactory` switch — that's the ONLY code change needed
- Understand how the [Strategy pattern](../Concepts/Abstraction.md) scales: new behaviors require zero changes to consumers
- Combine Health and Movement strategies into flexible enemy compositions using config SOs

## Key Concepts

- **Strategy pattern extensibility** — the core lesson of this episode. We prove the pattern works by adding two new strategies without touching `EnemyController`, `TowerDetection`, `TowerFiring`, or `ProjectileBase`.
- **Plain classes, not ScriptableObjects** — `ShieldHealth` and `RegenHealth` take config values via constructor, created by `StrategyFactory`. No SO asset creation for strategies.
- **ShieldHealth** — absorbs damage with a shield buffer before health depletes (inspired by BTD6's Shielded/Lead properties)
- **RegenHealth** — heals over time when not recently damaged (inspired by BTD6's Regrow property)
- **DamageResult** — `TakeDamage` returns a struct, so callers check `Died` deterministically
- [Abstraction](../Concepts/Abstraction.md) — each health strategy is a self-contained abstraction that `EnemyController` delegates to

## Code Roadmap

### Files You'll Create

| File | Purpose |
|------|---------|
| `Strategies/Health/ShieldHealth.cs` | `IHealthStrategy` — shield buffer absorbs damage first |
| `Strategies/Health/RegenHealth.cs` | `IHealthStrategy` — regenerates health after a delay |

### Files You'll Modify

| File | Change |
|------|--------|
| `Data/HealthConfig.cs` | Already has `Shield` and `Regen` enum values + fields |
| `Systems/Parsing/StrategyFactory.cs` | Add `case Shield` and `case Regen` to `CreateHealth` |

### Files That Need ZERO Changes

| File | Why |
|------|-----|
| `Interfaces/IHealthStrategy.cs` | Interface unchanged — new classes implement it |
| `Enemies/Controllers/EnemyController.cs` | Still calls `Health.TakeDamage()`, checks `DamageResult.Died` |
| `TowerDetection.cs` | Still finds `ITargetable` enemies |
| `ProjectileBase.cs` | Still delivers damage amounts via `IDamageable` |

### Prerequisites

- Episode 03 complete — `StrategyFactory`, `HealthConfig`, `EnemyController.Initialize()` pipeline must be working

## Architecture Context

### Strategy Composition Matrix

The power of the Strategy pattern is **composition**. Any Health strategy can pair with any Movement strategy:

| | **GroundedPath** | **FlyingPath** | *(future strategies)* |
|---|---|---|---|
| **NormalHealth** | Basic enemy | Basic flyer | — |
| **ArmouredHealth** | Tank enemy | Armoured flyer | — |
| **ShieldHealth** | Shielded ground | Shielded flyer | — |
| **RegenHealth** | Regen ground | Regen flyer | — |
| *(future)* | — | — | — |

Each cell is a valid combination — just create a `HealthConfig` SO with the right `HealthType` enum and pair it with a `MovementConfig` SO. No new classes for the combination, no `if` chains.

```text
┌─────────────────────────────────────────────┐
│              EnemyController                 │
│  ┌───────────────┐    ┌──────────────────┐    │
│  │IHealthStrategy│    │IMovementStrategy│    │
│  │  (interface)  │    │   (interface)    │    │
│  └──────┬────────┘    └────────┬─────────┘    │
│         │                      │              │
│    ┌────┴─────┐          ┌─────┴──────┐       │
│    │ Normal   │          │ Grounded   │       │
│    │ Armoured │          │  Flying    │       │
│    │ Shield   │  ← NEW   │            │       │
│    │ Regen    │  ← NEW   │            │       │
│    └──────────┘          └────────────┘       │
└─────────────────────────────────────────────┘
         ▲
         │ Created by StrategyFactory from HealthConfig
```

## Step-by-Step Implementation Guide

### Step 1: Recap — Where We Are

We have two working health strategies:

- **NormalHealth** — applies damage directly to `_currentHealth`, returns `DamageResult`
- **ArmouredHealth** — reduces damage by a percentage before applying, returns `DamageResult`

Both are plain C# classes implementing `IHealthStrategy`, constructed by `StrategyFactory` with values from `HealthConfig`. `EnemyController` calls `Health.TakeDamage()` and checks `result.Died` — it doesn't care which implementation runs. Time for more.

### Step 2: ShieldHealth — Shield Buffer

**Concept:** A shield absorbs damage before it reaches health. Think of it as an extra HP pool that depletes first.

```csharp
using Interfaces;

namespace Strategies.Health
{
    public class ShieldHealth : IHealthStrategy
    {
        // TODO: Constructor(int startHealth, int shieldPoints)
        // TODO: _startHealth, _shieldPoints, _currentHealth, _currentShield fields

        public void Initialize()
        {
            // TODO: _currentHealth = _startHealth
            // TODO: _currentShield = _shieldPoints
        }

        public DamageResult TakeDamage(float amount)
        {
            // TODO: Shield absorbs damage first
            // If _currentShield >= amount: reduce shield, return Alive
            // If _currentShield < amount: overflow hits _currentHealth
            //   reduce _currentHealth by (amount - _currentShield), set _currentShield = 0
            //   check if dead, return appropriate DamageResult
        }

        public void Tick(float deltaTime) { }

        public bool IsAlive => /* TODO: _currentHealth > 0 */;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _startHealth;
    }
}
```

**How it works:**

1. `Initialize` sets `_currentShield = _shieldPoints` alongside `_currentHealth = _startHealth`
2. `TakeDamage` checks `_currentShield` first — if shield is up, damage reduces shield
3. Overflow damage (shield depleted mid-hit) carries over to `_currentHealth`
4. Returns `DamageResult.Dead(damage)` when health drops to 0, `DamageResult.Alive(damage)` otherwise

**Example flow:**

- Enemy has `shieldPoints = 50`, `startHealth = 100`
- Takes 30 damage → shield drops to 20, health stays 100 → `Alive(30)`
- Takes 40 damage → shield absorbs 20, overflow 20 hits health → health drops to 80 → `Alive(40)`

### Step 3: RegenHealth — Healing Over Time

**Concept:** After a delay with no damage, the enemy regenerates health each tick. Inspired by BTD6's Regrow bloons.

```csharp
using Interfaces;

namespace Strategies.Health
{
    public class RegenHealth : IHealthStrategy
    {
        // TODO: Constructor(int startHealth, float regenRate, float regenDelay)
        // TODO: _startHealth, _regenRate, _regenDelay, _currentHealth, _timeSinceLastDamage

        public void Initialize()
        {
            // TODO: _currentHealth = _startHealth
            // TODO: _timeSinceLastDamage = 0
        }

        public DamageResult TakeDamage(float amount)
        {
            // TODO: _currentHealth -= amount
            // TODO: _timeSinceLastDamage = 0 (reset regen timer)
            // TODO: Return DamageResult based on _currentHealth
        }

        public void Tick(float deltaTime)
        {
            // TODO: _timeSinceLastDamage += deltaTime
            // TODO: If _timeSinceLastDamage < _regenDelay, return
            // TODO: Heal _currentHealth by _regenRate * deltaTime
            // TODO: Clamp to _startHealth (never exceed max)
        }

        public bool IsAlive => /* TODO: _currentHealth > 0 */;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _startHealth;
    }
}
```

!!! tip "Regen uses Tick() — that's why IHealthStrategy has Tick()"
    `IHealthStrategy.Tick(float deltaTime)` exists for exactly this strategy. `NormalHealth`, `ArmouredHealth`, and `ShieldHealth` have empty `Tick()` implementations. `RegenHealth` is the first strategy that *needs* per-frame logic. `EnemyController.Update()` already calls `Health.Tick(Time.deltaTime)` — no changes needed there.

**Regen timing:**

- Enemy takes damage at `t=0` → `_timeSinceLastDamage = 0`
- At `t=regenDelay`, regen kicks in
- If damaged again, timer resets to 0

### Step 4: Update StrategyFactory — The ONLY Code Change

Adding `ShieldHealth` and `RegenHealth` requires exactly **one code change**: two new `case` statements in `StrategyFactory.CreateHealth()`.

```csharp
public static IHealthStrategy CreateHealth(HealthConfig config)
{
    return config.Type switch
    {
        HealthType.Normal   => new NormalHealth(config.StartHealth),
        HealthType.Armoured => new ArmouredHealth(config.StartHealth, config.ArmourPercent),
        // TODO: Add these two cases:
        HealthType.Shield   => new ShieldHealth(config.StartHealth, config.ShieldPoints),
        HealthType.Regen    => new RegenHealth(config.StartHealth, config.RegenRate, config.RegenDelay),
        _ => throw new System.ArgumentException($"Unknown health type: {config.Type}")
    };
}
```

That's it. No changes to `IHealthStrategy`, `EnemyController`, `TowerDetection`, `ProjectileBase`, or any other file.

### Step 5: Create HealthConfig SOs — No Strategy SO Assets

The `HealthConfig` SO already has `Shield` and `Regen` in its `HealthType` enum with the relevant fields. Just create config SOs:

1. **ShieldHealthConfig** — `Create > Scriptable Objects > Config > Health`
   - Set `Type` = Shield
   - Set `Start Health` = 100
   - Set `Shield Points` = 50

2. **RegenHealthConfig** — `Create > Scriptable Objects > Config > Health`
   - Set `Type` = Regen
   - Set `Start Health` = 80
   - Set `Regen Rate` = 5
   - Set `Regen Delay` = 3

No strategy SO creation. No new SO classes. The config enum drives `StrategyFactory`, which builds the right concrete class.

### Step 6: Combine with Movement Strategies

Pair configs to create enemy archetypes:

| Enemy | HealthConfig Type | MovementConfig Type | Feel |
|-------|-------------------|---------------------|------|
| Shield Drone | Shield (100 HP, 50 shield) | Flying (4 speed, height 2) | Shielded flyer — must strip shield before it reaches base |
| Regen Zombie | Regen (80 HP, 5 HP/s, 3s delay) | Grounded (5 speed) | Relentless ground unit — kills must be fast or it heals back |
| Shield Tank | Shield (150 HP, 80 shield) | Grounded (3 speed) | Double-layer defence on the ground |

No code changes. Just config SO wiring.

### Step 7: The Key Lesson — Zero Consumer Changes

Adding `ShieldHealth` and `RegenHealth` required **no changes** to:

- `IHealthStrategy` — interface unchanged, new classes implement it
- `EnemyController` — still calls `Health.TakeDamage()`, checks `DamageResult.Died`, calls `Health.Tick()`
- `TowerDetection` — still finds `ITargetable` enemies
- `TowerFiring` — still calls `TakeDamage` via the interface
- `ProjectileBase` — still delivers damage amounts

The **only** code change was two `case` statements in `StrategyFactory.CreateHealth()`. The Strategy pattern scaled exactly as promised.

### Step 8: Future Strategies Brainstorm

The same pattern can support:

| Strategy | Concept | Config Fields Needed |
|----------|---------|---------------------|
| **CamoHealth** | Invisible to non-detecting towers | `isCamo` (bool) — towers need detection check |
| **SprintMovement** | Periodic speed boost | `sprintMultiplier`, `sprintDuration`, `sprintCooldown` |
| **SplitHealth** | Splits into smaller enemies on death | `splitCount` (int) — needs `DamageResult.Died` hook |

Each requires:

1. A new `IHealthStrategy` or `IMovementStrategy` class
2. A new enum value in `HealthType` or `MovementType`
3. A new `case` in `StrategyFactory`
4. A `HealthConfig` or `MovementConfig` SO with the right type selected

Zero changes to consumers.

## Episode Recap

- Implemented **ShieldHealth** — shield buffer absorbs damage before health depletes
- Implemented **RegenHealth** — heals over time after a damage cooldown period, uses `Tick(deltaTime)`
- Added exactly **two `case` statements** to `StrategyFactory.CreateHealth()` — the only code change
- Created `HealthConfig` SOs with `type=Shield` and `type=Regen` — no strategy SO assets needed
- Combined strategies freely — any HealthConfig + any MovementConfig via SO wiring
- **Proved the pattern scales** — two new strategies, zero changes to consumer code
- Brainstormed future strategies (Camo, Sprint, Split) that follow the same pattern

## Challenge

Design a **CamoMovement** strategy where camo enemies are invisible to towers that lack detection capability.

1. What interface change would `ITargetable` need so towers can check if an enemy is detectable?
2. Would you add `IsCamo` directly to `ITargetable`, or create a separate `IDetectable` interface?
3. How would `TowerDetection` filter targets — skip camo enemies, or let the tower decide?
4. Where does the camo flag live — `HealthConfig`, `MovementConfig`, or a new `EnemyConfig`?

!!! tip "Think about ISP"
    The Interface Segregation Principle suggests that not every enemy needs detectability. A separate `IDetectable` interface keeps `ITargetable` clean — but adds a type-check at the detection site. Consider the tradeoffs.