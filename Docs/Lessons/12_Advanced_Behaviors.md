# Episode 12 — Advanced Behaviors

<!-- Video placeholder -->
<iframe width="560" height="315" src="https://www.youtube.com/embed/PLACEHOLDER_EP12" frameborder="0"></iframe>

---

## Learning Objectives

- Recognise the fragility of modifying existing strategy classes to add new behaviours
- Implement `ShieldHealth` as a new `IHealthStrategy` with shield-then-health overflow
- Implement `RegenHealth` as a new `IHealthStrategy` with time-since-damage regeneration delay
- Extend `StrategyFactory` with new cases without touching existing strategies
- Extend `HealthConfig` with new fields for shield and regen parameters
- Verify that `IHealthStrategy`, `EnemyController`, `TowerDetection`, and `ProjectileBase` require **zero changes**

## Key Concepts

- [Strategy Pattern](../Concepts/Strategy_Pattern.md)
- [Interfaces](../Concepts/Interfaces.md)
- [Factory Pattern](../Concepts/Factory_Pattern.md)

---

## What We're Starting With

We have `NormalHealth` and `ArmouredHealth` strategies working. Enemies pick a strategy at runtime via `StrategyFactory.Create()`. The strategy pattern is in place — but now we want shield enemies and regenerating enemies. The naive temptation is to crack open existing files and add conditional logic.

---

## The Naive Version

```csharp
// The WRONG way — modifying NormalHealth.cs
public class NormalHealth : IHealthStrategy
{
    // TODO: DON'T DO THIS — the problem
    //       Adding shield logic directly into NormalHealth:
    //
    // if (hasShield) {
    //     shieldPoints -= damage;
    //     if (shieldPoints <= 0) {
    //         overflowDamage = -shieldPoints;
    //         shieldPoints = 0;
    //         health -= overflowDamage;
    //     }
    // } else {
    //     health -= damage;
    // }

    // Adding regen logic in the same file:
    // if (canRegen) {
    //     timeSinceLastDamage += Time.deltaTime;
    //     if (timeSinceLastDamage >= regenDelay) {
    //         health = Mathf.Min(health + regenRate * Time.deltaTime, maxHealth);
    //     }
    // }

    // Problem: every new behaviour means more conditionals in existing code.
    //         NormalHealth is no longer "normal" — it's a mess.
    //         Any change to shield logic risks breaking normal enemies.
}
```

This violates **Open/Closed Principle**: we're modifying existing, working code to add new behaviour. The whole point of the strategy pattern is to add strategies *without* touching existing ones.

---

## The Refactor

We add **two new strategy classes** — `ShieldHealth` and `RegenHealth` — and one factory case each. Zero changes to `IHealthStrategy`, `EnemyController`, `TowerDetection`, `ProjectileBase`, `NormalHealth`, or `ArmouredHealth`.

### Architecture Context

```
              ┌──────────────────────┐
              │    IHealthStrategy    │
              │  TakeDamage(dmg)     │
              │  Tick(dt)            │
              │  IsDead              │
              └──────────┬───────────┘
                         │
          ┌──────────────┼──────────────┬──────────────┐
          │              │              │              │
   ┌──────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐
   │NormalH.  │  │ArmouredH.  │  │ShieldH.    │  │RegenH.     │
   │Health    │  │Half dmg   │  │Shield first │  │Regens after│
   │          │  │            │  │overflow to  │  │delay period│
   │          │  │            │  │health       │  │            │
   └──────────┘  └────────────┘  └────────────┘  └────────────┘
                                          │              │
                                   ┌──────┴──────┐  ┌──┴───┐
                                   │shieldPoints  │  │regenRate│
                                   │in HealthConfig│  │regenDelay│
                                   └─────────────┘  │in HealthConfig
                                                    └──────┘

              ┌──────────────────────┐
              │   StrategyFactory    │
              │  Create(config)      │
              │  "Normal" → NormalH. │
              │  "Armoured" → Arm.   │
              │  "Shield" → ShieldH. │  ← NEW
              │  "Regen" → RegenH.   │  ← NEW
              └──────────────────────┘
```

### Code Roadmap

| File | Purpose |
|------|---------|
| `Data/HealthConfig.cs` | Add `shieldPoints`, `regenRate`, `regenDelay` fields |
| `Enemies/Strategies/ShieldHealth.cs` | New strategy — shield absorbs damage first |
| `Enemies/Strategies/RegenHealth.cs` | New strategy — regenerates health after delay |
| `Systems/Parsing/StrategyFactory.cs` | Add `Shield` and `Regen` cases |

**Files that require NO changes**: `IHealthStrategy.cs`, `EnemyController.cs`, `TowerDetection.cs`, `ProjectileBase.cs`, `NormalHealth.cs`, `ArmouredHealth.cs`

---

## Step-by-Step Implementation

### Step 1 — Extend HealthConfig

Update `Data/HealthConfig.cs`:

```csharp
namespace Data
{
    [System.Serializable]
    public class HealthConfig
    {
        // ... existing fields: maxHealth, strategyName, etc.

        // TODO: Add shield-specific fields
        //       public float shieldPoints = 0f;
        //       (ShieldHealth reads this; other strategies ignore it)

        // TODO: Add regen-specific fields
        //       public float regenRate = 0f;      // HP per second
        //       public float regenDelay = 3f;     // seconds after last damage before regen starts
        //       (RegenHealth reads these; other strategies ignore them)
    }
}
```

`HealthConfig` is a **dumb data container** — it holds every possible parameter every strategy *might* need. Strategies only read the fields they care about. This keeps configs flexible without inheritance hierarchies.

### Step 2 — Create ShieldHealth strategy

Create `Enemies/Strategies/ShieldHealth.cs`:

```csharp
using Data;
using Interfaces;

namespace Enemies.Strategies
{
    public class ShieldHealth : IHealthStrategy
    {
        private float _health;
        private float _shieldPoints;
        private float _maxHealth;

        // TODO: Constructor — ShieldHealth(HealthConfig config)
        //       _maxHealth = config.maxHealth
        //       _health = _maxHealth
        //       _shieldPoints = config.shieldPoints

        // TODO: public DamageResult TakeDamage(float damage)
        //       1. If _shieldPoints > 0:
        //          a. Apply damage to shield first
        //          b. If shield breaks (shieldPoints go below 0):
        //             - overflowDamage = -shieldPoints
        //             - shieldPoints = 0
        //             - health -= overflowDamage
        //          c. Else: shield absorbed all damage, health unchanged
        //       2. Else: apply damage directly to health (like NormalHealth)
        //       3. Return DamageResult with remaining health and whether dead

        // TODO: public void Tick(float deltaTime)
        //       ShieldHealth doesn't tick — shields don't regenerate on their own
        //       (If you want regenerating shields, that's a different strategy composition)

        // TODO: public bool IsDead => _health <= 0;
    }
}
```

**Key design decision**: shield absorbs damage first, and any overflow hits health. This makes shield enemies resilient to small hits but vulnerable to burst damage — interesting gameplay without complex rules.

### Step 3 — Create RegenHealth strategy

Create `Enemies/Strategies/RegenHealth.cs`:

```csharp
using Data;
using Interfaces;
using UnityEngine;

namespace Enemies.Strategies
{
    public class RegenHealth : IHealthStrategy
    {
        private float _health;
        private float _maxHealth;
        private float _regenRate;
        private float _regenDelay;
        private float _timeSinceLastDamage;

        // TODO: Constructor — RegenHealth(HealthConfig config)
        //       _maxHealth = config.maxHealth
        //       _health = _maxHealth
        //       _regenRate = config.regenRate
        //       _regenDelay = config.regenDelay
        //       _timeSinceLastDamage = float.MaxValue (so regen starts immediately)

        // TODO: public DamageResult TakeDamage(float damage)
        //       _health -= damage
        //       _timeSinceLastDamage = 0f   ← RESET the regen timer!
        //       Return DamageResult

        // TODO: public void Tick(float deltaTime)
        //       _timeSinceLastDamage += deltaTime
        //       If _timeSinceLastDamage >= _regenDelay AND _health < _maxHealth:
        //           _health = Mathf.Min(_health + _regenRate * deltaTime, _maxHealth)

        // TODO: public bool IsDead => _health <= 0;
    }
}
```

**Key design decision**: regen doesn't start immediately after taking damage — there's a delay. This means fast-firing towers can out-DPS the regen, but slow towers let the enemy recover. The `_timeSinceLastDamage` resets on every hit, so rapid fire keeps regen suppressed. This creates a meaningful tactical choice for the player.

### Step 4 — Extend StrategyFactory

Update `Systems/Parsing/StrategyFactory.cs`:

```csharp
using Enemies.Strategies;

namespace Systems.Parsing
{
    public static class StrategyFactory
    {
        // TODO: Add new cases to the Create method:
        //
        // public static IHealthStrategy Create(HealthConfig config)
        // {
        //     return config.strategyName switch
        //     {
        //         "Normal" => new NormalHealth(config),
        //         "Armoured" => new ArmouredHealth(config),
        //         "Shield" => new ShieldHealth(config),    // NEW
        //         "Regen" => new RegenHealth(config),      // NEW
        //         _ => Debug.LogError($"Unknown strategy: {config.strategyName}"),
        //                new NormalHealth(config)
        //     };
        // }
    }
}
```

That's it. One `switch` case per new strategy. The factory is the **only place** that knows about concrete strategy types.

### Step 5 — Update wave CSV and EnemyData

In `wave_data.csv`, add new enemy types:

```csv
4,Normal,5,1.0
4,Shield,3,2.0
5,Regen,4,0.8
5,Armoured,2,1.5
5,Shield,2,2.0
```

Create new `EnemyData` ScriptableObjects for Shield and Regen enemies with appropriate `HealthConfig` values:

- **Shield enemy**: `strategyName = "Shield"`, `shieldPoints = 50`, `maxHealth = 80`
- **Regen enemy**: `strategyName = "Regen"`, `regenRate = 5`, `regenDelay = 2`, `maxHealth = 60`

Register these in `EnemyDataRegistry`.

### Step 6 — Verify zero changes to existing code

Open each of these files and confirm they were **not modified**:

- `Interfaces/IHealthStrategy.cs` — no changes
- `Enemies/Controllers/EnemyController.cs` — no changes
- `Towers/TowerDetection.cs` — no changes
- `Projectiles/ProjectileBase.cs` — no changes
- `Enemies/Strategies/NormalHealth.cs` — no changes
- `Enemies/Strategies/ArmouredHealth.cs` — no changes

This is the strategy pattern paying off. New behaviour = new class, not invasive surgery on existing code.

---

## Episode Recap

- **Naive**: Crack open `NormalHealth.cs`, add `if (hasShield)` and `if (canRegen)` conditionals → existing strategies break, code becomes a mess
- **Refactor**: `ShieldHealth` and `RegenHealth` are new `IHealthStrategy` implementations — zero changes to existing strategies, `EnemyController`, `TowerDetection`, or `ProjectileBase`
- `StrategyFactory` gains two new `switch` cases — that's the only existing file that changes
- `HealthConfig` grows with `shieldPoints`, `regenRate`, `regenDelay` — strategies only read what they need
- **The pattern scales**: want a "ShieldRegen" enemy? You could compose strategies or create a `ShieldRegenHealth` class — still no changes to existing code

---

## Challenge

1. Create a `ShieldRegenHealth` strategy that combines shield and regen (shield must be fully depleted before regen activates). How much code does it share with `ShieldHealth` and `RegenHealth`? Consider whether composition might be better than inheritance here.

2. Add a `VampiricHealth` strategy: every time this enemy damages a tower (if you have tower health), it heals for 10% of the damage dealt. What new event channel would you need? What new field in `HealthConfig`? Hint: this requires the event system from Episode 10.