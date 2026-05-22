# Episode 07: Health Strategies

## What You're Building

Extract health logic from `EnemyController` into `IHealthStrategy` implementations. `NormalHealth` does simple subtraction. `ArmouredHealth` reduces incoming damage. A `DamageResult` struct tells the caller what happened. Four composed enemy types work. `EnemyController` becomes a thin orchestrator that delegates to both movement and health strategies.

## The Problem

You want an armoured enemy that takes 30% less damage. Right now:

```csharp
public void TakeDamage(float damage)
{
    _currentHealth -= damage;
    if (_currentHealth <= 0) Die();
}
```

To add armour, you'd add `if (isArmoured) damage *= 0.7f;` inside `TakeDamage`. Then shields. Then regen. `TakeDamage` grows with every health type. And the caller can't tell if the enemy died — `void TakeDamage` returns nothing.

## IHealthStrategy.cs + DamageResult.cs

```csharp
using System;

namespace Interfaces
{
    public interface IHealthStrategy
    {
        void Initialize();
        DamageResult TakeDamage(float amount);
        void Tick(float deltaTime);
        bool IsAlive { get; }
        float CurrentHealth { get; }
        float MaxHealth { get; }
    }

    public readonly record struct DamageResult(bool Died, float DamageDealt)
    {
        public static DamageResult Alive(float damageDealt) => new(false, damageDealt);
        public static DamageResult Dead(float damageDealt) => new(true, damageDealt);
    }
}
```

**Why DamageResult instead of bool?** A `bool` only says "died or not." But callers also need to know *how much damage was actually dealt* — for damage numbers, for gold-on-kill tracking, for armour calculations. The struct carries both in one zero-alloc stack value.

**Why readonly record struct?** Record structs are immutable and stack-allocated like readonly structs, plus auto-generated value-based `Equals`, `GetHashCode`, and `ToString`. The positional syntax `(bool Died, float DamageDealt)` replaces explicit field declarations and constructor. Factory methods (`Alive()`, `Dead()`) make call sites readable: `return DamageResult.Dead(17.5f)`. Use `readonly record struct` for simple data carriers where value equality and debug string output are useful.

**Why Tick on the interface?** `NormalHealth` ignores it. But `RegenHealth` (Episode 12) needs a per-frame tick to restore health. Putting it on the interface now means we don't have to change the interface later.

## NormalHealth.cs

```csharp
using Interfaces;

namespace Strategies.Health
{
    public class NormalHealth : IHealthStrategy
    {
        private readonly int _startHealth;
        private float _currentHealth;

        public NormalHealth(int startHealth)
        {
            _startHealth = startHealth;
        }

        public void Initialize()
        {
            _currentHealth = _startHealth;
        }

        public DamageResult TakeDamage(float amount)
        {
            _currentHealth -= amount;

            if (_currentHealth <= 0f)
            {
                _currentHealth = 0f;
                return DamageResult.Dead(amount);
            }

            return DamageResult.Alive(amount);
        }

        public void Tick(float deltaTime) { }

        public bool IsAlive => _currentHealth > 0f;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _startHealth;
    }
}
```

Same logic as what was inline in `EnemyController.TakeDamage`. Extracted unchanged.

## ArmouredHealth.cs

```csharp
using Interfaces;

namespace Strategies.Health
{
    public class ArmouredHealth : IHealthStrategy
    {
        private readonly int _startHealth;
        private readonly float _armourPercent;
        private float _currentHealth;

        public ArmouredHealth(int startHealth, float armourPercent)
        {
            _startHealth = startHealth;
            _armourPercent = armourPercent;
        }

        public void Initialize()
        {
            _currentHealth = _startHealth;
        }

        public DamageResult TakeDamage(float amount)
        {
            float reducedDamage = amount * (1f - _armourPercent);
            _currentHealth -= reducedDamage;

            if (_currentHealth <= 0f)
            {
                _currentHealth = 0f;
                return DamageResult.Dead(reducedDamage);
            }

            return DamageResult.Alive(reducedDamage);
        }

        public void Tick(float deltaTime) { }

        public bool IsAlive => _currentHealth > 0f;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _startHealth;
    }
}
```

**Damage trace** (armourPercent = 0.3, incoming = 10):
1. `reducedDamage = 10 * 0.7 = 7`
2. `_currentHealth -= 7`
3. `DamageResult.Alive(7)` — caller knows 7 was dealt, not 10

A 100 HP armoured enemy (30% armour) takes ~14 hits of 10 to kill vs 10 hits for normal.

## HealthType enum + HealthConfig.cs

```csharp
using UnityEngine;

namespace Data
{
    public enum HealthType
    {
        Normal,
        Armoured
    }

    [CreateAssetMenu(fileName = "HealthConfig", menuName = "TD/Health Config")]
    public class HealthConfig : ScriptableObject
    {
        [SerializeField] private HealthType type;
        [SerializeField] private int startHealth = 100;
        [SerializeField, Range(0f, 0.99f)] private float armourPercent;

        public HealthType Type => type;
        public int StartHealth => startHealth;
        public float ArmourPercent => armourPercent;
    }
}
```

`Shield` and `Regen` types added in Episode 12. The `armourPercent` field is ignored for Normal configs — unused fields on a ScriptableObject cost nothing.

## StrategyFactory.cs (updated)

```csharp
using Data;
using Interfaces;
using Strategies.Health;
using Strategies.Movement;

namespace Systems.Parsing
{
    public static class StrategyFactory
    {
        public static IHealthStrategy CreateHealth(HealthConfig config)
        {
            return config.Type switch
            {
                HealthType.Normal => new NormalHealth(config.StartHealth),
                HealthType.Armoured => new ArmouredHealth(config.StartHealth, config.ArmourPercent),
                _ => new NormalHealth(config.StartHealth)
            };
        }

        public static IMovementStrategy CreateMovement(MovementConfig config)
        {
            return config.Type switch
            {
                MovementType.Grounded => new GroundedPath(config.MoveSpeed),
                MovementType.Flying => new FlyingPath(config.MoveSpeed, config.FlyingHeight),
                _ => new GroundedPath(config.MoveSpeed)
            };
        }
    }
}
```

Added `CreateHealth`. The movement method is unchanged from Episode 06.

## EnemyData.cs (updated)

```csharp
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "TD/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private HealthConfig healthConfig;
        [SerializeField] private MovementConfig movementConfig;
        [SerializeField] private int goldGiven = 10;
        [SerializeField] private int damage = 1;

        public HealthConfig HealthConfig => healthConfig;
        public MovementConfig MovementConfig => movementConfig;
        public int GoldGiven => goldGiven;
        public int Damage => damage;
    }
}
```

`startHealth` float replaced by `HealthConfig` reference. Enemy data is now fully composed from config ScriptableObjects.

## EnemyController.cs (thin orchestrator)

```csharp
using Data;
using Interfaces;
using Systems.Game;
using Systems.Parsing;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable
    {
        [SerializeField] private EnemyHealthBar healthBar;

        public EnemyPath Path { get; private set; }
        public int CurrentWayPointIndex { get; set; }
        public IHealthStrategy Health { get; private set; }
        public IMovementStrategy Movement { get; private set; }
        private int _goldGiven;
        private int _damage;

        // ITargetable
        public Vector3 Position => transform.position;
        public bool IsAlive => Health != null && Health.IsAlive;

        public void Initialize(EnemyData data, EnemyPath path)
        {
            Path = path;
            Health = StrategyFactory.CreateHealth(data.HealthConfig);
            Movement = StrategyFactory.CreateMovement(data.MovementConfig);
            _goldGiven = data.GoldGiven;
            _damage = data.Damage;

            Health.Initialize();
            Movement.Initialize(this);
            Movement.OnMovementCompleted += OnReachedEnd;
        }

        private void Update()
        {
            if (!IsAlive) return;

            Health.Tick(Time.deltaTime);
            Movement.Tick(this);

            if (healthBar != null)
                healthBar.SetHealth(Health.CurrentHealth, Health.MaxHealth);
            if (healthBar != null)
                healthBar.SetPosition(transform.position);
        }

        public void TakeDamage(float damage)
        {
            DamageResult result = Health.TakeDamage(damage);
            if (result.Died)
                Die();
        }

        private void Die()
        {
            PlayerStats.Instance.AddGold(_goldGiven);
            Destroy(gameObject);
        }

        private void OnReachedEnd()
        {
            Movement.OnMovementCompleted -= OnReachedEnd;
            PlayerStats.Instance.SubtractLives(_damage);
            Destroy(gameObject);
        }
    }
}
```

**What changed from Episode 06:**

| Before | After |
|--------|-------|
| `float _currentHealth` | `IHealthStrategy Health` property |
| `float _startHealth` | Gone — inside Health strategy |
| `IsAlive => _currentHealth > 0` | `IsAlive => Health.IsAlive` |
| `_currentHealth -= damage` inline | `Health.TakeDamage(damage)` returns DamageResult |
| `if (_currentHealth <= 0) Die()` | `if (result.Died) Die()` |
| No Health.Tick | `Health.Tick(Time.deltaTime)` in Update |

**EnemyController now delegates to both `Health` and `Movement`.** It contains zero type-specific logic. Adding a new health type (Shield, Regen) requires zero changes to this file.

## Unity Editor Setup

### 1. Create HealthConfig SOs

1. Right-click → Create > TD > Health Config
2. Name `HC_Normal`: Type=Normal, Start Health=100, Armour=0
3. Name `HC_Armoured`: Type=Armoured, Start Health=150, Armour=0.3

### 2. Update EnemyData SOs

| Name | Health Config | Movement Config | Gold | Damage |
|------|--------------|----------------|------|--------|
| `ED_Basic` | HC_Normal | MC_Grounded | 10 | 1 |
| `ED_Armoured` | HC_Armoured | MC_Grounded | 20 | 1 |
| `ED_Flying` | HC_Normal | MC_Flying | 15 | 1 |
| `ED_FlyingArmoured` | HC_Armoured | MC_Flying | 25 | 2 |

### 3. Test all four types

1. Basic: 10 hits of 10 damage to kill (100 HP / 10 per hit)
2. Armoured: ~14 hits of 10 damage (100 effective HP after 30% reduction... 150 / 7 ≈ 21... wait, 150 HP with 30% armour: each hit of 10 becomes 7, so 150/7 ≈ 22 hits)
3. Flying: same as Basic HP but hovers above ground
4. FlyingArmoured: same as Armoured HP but hovers

## Debugging

| Symptom | Cause | Fix |
|---------|-------|-----|
| Armoured takes full damage | ArmourPercent is 0 in config | Set ArmourPercent=0.3 in HC_Armoured |
| Armoured takes zero damage | ArmourPercent is 1.0 | Keep in range [0, 0.99] |
| Health bar shows wrong values | Not reading from strategy | Use `Health.CurrentHealth / Health.MaxHealth` |
| NullRef on Health | HealthConfig not assigned on EnemyData | Assign HC_Normal or HC_Armoured |
| All enemies same health | Same HealthConfig for all | Create separate config SOs per type |