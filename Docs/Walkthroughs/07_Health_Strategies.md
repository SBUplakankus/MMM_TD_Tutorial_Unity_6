# Episode 07: Health Strategies

## What You're Building

Extract health logic from `EnemyController` into `IHealthStrategy` implementations. `NormalHealth` does simple subtraction. `ArmouredHealth` absorbs damage into a separate armour pool first — overflow bleeds through to health. A `DamageResult` struct tells the caller what happened. `EnemyController` becomes a thin orchestrator that delegates to both movement and health strategies.

## The Problem

You want an armoured enemy. Right now health is hardcoded inline in `EnemyController.TakeDamage`:

```csharp
public void TakeDamage(float damage)
{
    _currentHealth -= damage;
    healthBar.Show();
    healthBar.UpdateValue(Mathf.Clamp01(_currentHealth / _startHealth));

    if (_currentHealth > 0) return;
    _currentHealth = 0;
    Die();
}
```

To add armour you'd add branches inside `TakeDamage`. Then shields. Then regen. The method grows with every health type and the caller can never tell how much damage was actually dealt. The Strategy Pattern moves each health behaviour into its own class — `EnemyController.TakeDamage` becomes two lines.

## DamageResult + IHealthStrategy.cs

```csharp
namespace Structs
{
    public readonly struct DamageResult
    {
        public bool Died { get; }
        public float DamageDealt { get; }

        private DamageResult(bool died, float damageDealt)
        {
            Died = died;
            DamageDealt = damageDealt;
        }

        public static DamageResult Alive(float damageDealt) => new(false, damageDealt);
        public static DamageResult Dead(float damageDealt) => new(true, damageDealt);
    }
}

namespace Interfaces
{
    public interface IHealthStrategy
    {
        void Init();
        DamageResult TakeDamage(float amount);
        void Tick(float deltaTime);
        bool IsAlive { get; }
        float CurrentHealth { get; }
        float MaxHealth { get; }
    }
}
```

**Why `DamageResult` instead of `bool`?** The caller needs to know two things: did the enemy die, and how much damage was actually dealt. With `ArmouredHealth` the dealt amount can differ from the incoming amount when armour absorbs some of it. A bool only answers the first question. `DamageResult` answers both in one zero-allocation stack value.

**Why `Tick` on the interface?** `NormalHealth` ignores it. Future health strategies — regen, shield recharge — need a per-frame update. Putting it on the interface now means the interface never needs to change when those types are added.

**Why `readonly record struct`?** Immutable, stack-allocated, and the positional syntax replaces explicit field declarations and constructor boilerplate. Factory methods (`Alive()`, `Dead()`) keep call sites readable.

## HealthConfig.cs

```csharp
namespace Enums
{
    public enum HealthType
    {
        Normal,
        Armoured
    }
}

using Enums;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "HealthConfig", menuName = "TD/Health Config")]
    public class HealthConfig : ScriptableObject
    {
        public HealthType type;
        public int startHealth = 100;
        [Range(0,0.99f)] public float armourStrength;
    }
}
```

`armourPoints` is ignored by `NormalHealth` — unused fields on a ScriptableObject cost nothing.

## NormalHealth.cs

```csharp
using Interfaces;

namespace Strategies.Health
{
    public class NormalHealth : IHealthStrategy
    {
        private readonly int _startHealth;
        private float _currentHealth;

        public NormalHealth(int startHealth) => _startHealth = startHealth;

        public void Init() => _currentHealth = _startHealth;

        public DamageResult TakeDamage(float amount)
        {
            _currentHealth -= amount;

            if (!(_currentHealth <= 0f)) return DamageResult.Alive(amount);

            _currentHealth = 0f;
            return DamageResult.Dead(amount);

        }

        public void Tick(float deltaTime) { }

        public bool IsAlive => _currentHealth > 0f;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _startHealth;
    }
}
```

Same logic that was inline in `EnemyController.TakeDamage` — extracted unchanged.

## ArmouredHealth.cs

Armour absorbs damage first. Any overflow goes to health. Once the armour pool hits zero it stays there.

```csharp
using Interfaces;
using Structs;
using UnityEngine;

namespace Strategies.Health
{
    public class ArmouredHealth : IHealthStrategy
    {
        private readonly int _startHealth;
        private readonly float _armourStrength;
        private float _currentHealth;

        public ArmouredHealth(int startHealth, float armourStrength)
        {
            _startHealth = startHealth;
            _armourStrength = armourStrength;
        }

        public void Init()
        {
            _currentHealth = _startHealth;
        }

        public DamageResult TakeDamage(float amount)
        {
            var damage = amount - (amount * _armourStrength);
            _currentHealth -= damage;

            if (_currentHealth > 0f) return DamageResult.Alive(damage);
            
            _currentHealth = 0f;
            return DamageResult.Dead(damage);

        }

        public void Tick(float deltaTime) { }

        public bool IsAlive => _currentHealth > 0f;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _startHealth;
    }
}
```

**Damage trace** (armour = 10, health = 100, incoming = 15):

```
armourDamage = min(15, 10) = 10  → armour hits 0
healthDamage = 15 - 10     = 5   → health becomes 95
```

**Damage trace** (armour = 0, health = 95, incoming = 15):

```
armourDamage = min(15, 0) = 0    → armour stays 0
healthDamage = 15 - 0     = 15   → health becomes 80
```

Once armour is depleted, subsequent hits go entirely to health.

## EnemyData.cs (updated)

`startHealth` float is replaced by a `HealthConfig` reference. Enemy data is now fully composed from config assets.

```csharp
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "TD/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        public MovementConfig movementConfig;
        public HealthConfig healthConfig;
        public int goldGiven = 10;
        public int livesTaken = 1;
    }
}
```

## StrategyFactory.cs (updated)

Both strategies exist now. Add `CreateHealth` alongside the existing `CreateMovement`.

```csharp
using Data;
using Interfaces;
using Strategies.Health;
using Strategies.Movement;

namespace Systems.Parsing
{
    public static class StrategyFactory
    {
        public static IMovementStrategy CreateMovement(MovementConfig config)
        {
            return config.Type switch
            {
                MovementType.Grounded => new GroundedPath(config.MoveSpeed),
                MovementType.Flying   => new FlyingPath(config.MoveSpeed, config.FlyingHeight),
                _                    => new GroundedPath(config.MoveSpeed)
            };
        }

        public static IHealthStrategy CreateHealth(HealthConfig config)
        {
            return config.Type switch
            {
                HealthType.Normal   => new NormalHealth(config.StartHealth),
                HealthType.Armoured => new ArmouredHealth(config.StartHealth, config.ArmourPoints),
                _                  => new NormalHealth(config.StartHealth)
            };
        }
    }
}
```

## EnemyController.cs (thin orchestrator)

`EnemyData` and `StrategyFactory` both exist now. The controller can wire health up the same way it wired movement in Episode 06.

**What changes from Episode 06:**

| Removed                            | Added                                                           |
| ---------------------------------- | --------------------------------------------------------------- |
| `float _currentHealth`             | `IHealthStrategy _health`                                       |
| `float _startHealth`               | `_health.IsAlive`, `_health.CurrentHealth`, `_health.MaxHealth` |
| Inline subtraction in `TakeDamage` | `_health.TakeDamage(damage)` returns `DamageResult`             |
| `if (_currentHealth <= 0)`         | `if (result.Died)`                                              |

```csharp
using Data;
using Enemies.Components;
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
        public int CurrentWaypointIndex { get; set; }
        public Vector3 Position => transform.position;
        public bool IsAlive => _health != null && _health.IsAlive;

        private IHealthStrategy _health;
        private IMovementStrategy _movement;
        private PlayerStats _playerStats;
        private int _goldGiven;
        private int _livesTaken;

        public void Initialize(EnemyData data, EnemyPath path, PlayerStats playerStats)
        {
            Path = path;
            _playerStats = playerStats;
            _goldGiven = data.GoldGiven;
            _livesTaken = data.LivesTaken;

            _health = StrategyFactory.CreateHealth(data.HealthConfig);
            _health.Init();

            _movement = StrategyFactory.CreateMovement(data.MovementConfig);
            _movement.Init(this);

            healthBar.Hide();
        }

        private void Update()
        {
            if (!IsAlive) return;

            _health.Tick(Time.deltaTime);

            if (_movement.Tick(this))
            {
                HandleEndReached();
                return;
            }

            healthBar.UpdateValue(Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth));
        }

        public void TakeDamage(float damage)
        {
            var result = _health.TakeDamage(damage);
            healthBar.Show();

            if (result.Died)
                Die();
        }

        private void Die()
        {
            _playerStats.AddGold(_goldGiven);
            Destroy(gameObject);
        }

        private void HandleEndReached()
        {
            _playerStats.RemoveLives(_livesTaken);
            Destroy(gameObject);
        }
    }
}
```

`TakeDamage` is now three lines. `Update` contains no health or movement logic — only delegation and reactions. Adding `RegenHealth` or `ShieldHealth` in a future episode requires zero changes here.

## Unity Editor Setup

### 1. Create HealthConfig assets

1. Right-click → Create > TD > Health Config
2. `HC_Normal`: Type = Normal, Start Health = 100, Armour Points = 0
3. `HC_Armoured`: Type = Armoured, Start Health = 100, Armour Points = 50

### 2. Update EnemyData assets

| Asset               | Health Config | Movement Config | Gold | Lives Taken |
| ------------------- | ------------- | --------------- | ---- | ----------- |
| `ED_Basic`          | HC_Normal     | MC_Grounded     | 10   | 1           |
| `ED_Armoured`       | HC_Armoured   | MC_Grounded     | 20   | 1           |
| `ED_Flying`         | HC_Normal     | MC_Flying       | 15   | 1           |
| `ED_FlyingArmoured` | HC_Armoured   | MC_Flying       | 25   | 2           |

### 3. Test all four types

1. **Basic** — 10 hits of 10 damage to kill (100 HP)
2. **Armoured** — first 5 hits absorbed by armour, next 10 hits kill (50 armour + 100 HP)
3. **Flying** — same as Basic but hovers
4. **FlyingArmoured** — same as Armoured but hovers

## Debugging

| Symptom                                | Cause                              | Fix                                                |
| -------------------------------------- | ---------------------------------- | -------------------------------------------------- |
| NullRef on `_health.TakeDamage`        | `Initialize` not called            | Spawner must call `Initialize` after `Instantiate` |
| Armoured takes full damage immediately | `ArmourPoints` is 0                | Set Armour Points on HC_Armoured                   |
| Health bar not updating                | `healthBar.UpdateValue` not called | Confirm Update runs and `_health` is not null      |
| All enemies same health                | Same HealthConfig assigned         | Create separate config assets per type             |
| Enemy never dies                       | `DamageResult.Died` never true     | Confirm `_currentHealth <= 0` check in strategy    |