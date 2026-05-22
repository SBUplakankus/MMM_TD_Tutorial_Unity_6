# Episode 13: Advanced Health

## What You're Building

Add `ShieldHealth` and `RegenHealth` — two new health strategies that demonstrate the payoff of the Strategy Pattern. **Zero changes to EnemyController.** One new class, one factory case, one config SO each.

## The Payoff

All 14 episodes of architecture led here. The entire point of the Strategy Pattern is: adding new behavior requires no changes to existing code. This episode proves it.

| File | Changed? |
|------|----------|
| EnemyController.cs | No |
| StrategyFactory.cs | Yes — 2 new cases |
| HealthConfig.cs | Yes — 2 new enum values + 2 new fields |
| Everything else | No |

## ShieldHealth.cs

Shield absorbs damage before health. When shield is depleted, remaining damage hits health.

```csharp
using Interfaces;

namespace Strategies.Health
{
    public class ShieldHealth : IHealthStrategy
    {
        private readonly int _startHealth;
        private readonly float _shieldAmount;
        private float _currentHealth;
        private float _currentShield;

        public ShieldHealth(int startHealth, float shieldAmount)
        {
            _startHealth = startHealth;
            _shieldAmount = shieldAmount;
        }

        public void Initialize()
        {
            _currentHealth = _startHealth;
            _currentShield = _shieldAmount;
        }

        public DamageResult TakeDamage(float amount)
        {
            if (_currentShield > 0)
            {
                if (amount <= _currentShield)
                {
                    _currentShield -= amount;
                    return DamageResult.Alive(amount);
                }

                float overflow = amount - _currentShield;
                _currentShield = 0;
                _currentHealth -= overflow;
            }
            else
            {
                _currentHealth -= amount;
            }

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
        public float CurrentShield => _currentShield;
        public float MaxShield => _shieldAmount;
    }
}
```

**Damage trace** (shield=30, health=100, incoming=10):
1. Shield absorbs 10 → shield=20, health=100. Alive(10).
2. Hit again 7 more times → shield depletes, 40 overflow → shield=0, health=60. Alive(10).
3. Continue hitting → health drops, eventually Dead(10).

Shield recharges are out of scope — could be added in `Tick` if needed.

## RegenHealth.cs

Regenerates health over time. Capped at MaxHealth.

```csharp
using Interfaces;

namespace Strategies.Health
{
    public class RegenHealth : IHealthStrategy
    {
        private readonly int _startHealth;
        private readonly float _regenRate;
        private float _currentHealth;

        public RegenHealth(int startHealth, float regenRate)
        {
            _startHealth = startHealth;
            _regenRate = regenRate;
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

        public void Tick(float deltaTime)
        {
            if (!IsAlive) return;
            _currentHealth = Mathf.Min(_currentHealth + _regenRate * deltaTime, _startHealth);
        }

        public bool IsAlive => _currentHealth > 0f;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _startHealth;
    }
}
```

**Regen trace** (health=50, regenRate=5, time=1s):
1. After 1 second: `_currentHealth += 5 * 1 = 55`
2. After 10 seconds at full HP: capped at `_startHealth`, stays at 50

`Tick` is why it exists on the interface. `NormalHealth` and `ArmouredHealth` ignore it. `RegenHealth` uses it.

## HealthType enum (updated)

```csharp
public enum HealthType
{
    Normal,
    Armoured,
    Shield,
    Regen
}
```

## HealthConfig.cs (updated)

```csharp
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "HealthConfig", menuName = "TD/Health Config")]
    public class HealthConfig : ScriptableObject
    {
        [SerializeField] private HealthType type;
        [SerializeField] private int startHealth = 100;
        [SerializeField, Range(0f, 0.99f)] private float armourPercent;
        [SerializeField] private float shieldAmount;
        [SerializeField] private float regenRate;

        public HealthType Type => type;
        public int StartHealth => startHealth;
        public float ArmourPercent => armourPercent;
        public float ShieldAmount => shieldAmount;
        public float RegenRate => regenRate;
    }
}
```

New fields for `ShieldAmount` and `RegenRate`. Unused fields on Normal/Armoured configs are ignored.

## StrategyFactory.cs (updated)

```csharp
public static IHealthStrategy CreateHealth(HealthConfig config)
{
    return config.Type switch
    {
        HealthType.Normal => new NormalHealth(config.StartHealth),
        HealthType.Armoured => new ArmouredHealth(config.StartHealth, config.ArmourPercent),
        HealthType.Shield => new ShieldHealth(config.StartHealth, config.ShieldAmount),
        HealthType.Regen => new RegenHealth(config.StartHealth, config.RegenRate),
        _ => new NormalHealth(config.StartHealth)
    };
}
```

Two new cases. That's the entire change to the factory. `CreateMovement` is unchanged.

## Shield Bar UI

Add a second Slider (blue) to the enemy health bar prefab, layered behind the green health fill:

```
Canvas (world-space)
  +-- Background (red)
  +-- Shield Fill (blue, fill amount = shield/maxShield)
  +-- Health Fill (green, fill amount = health/maxHealth)
```

Update `EnemyHealthBar` to accept the shield slider:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Enemies.Components
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider shieldSlider;
        [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);

        public void SetHealth(float current, float max)
        {
            healthSlider.value = Mathf.Clamp01(current / max);
        }

        public void SetShield(float current, float max)
        {
            if (shieldSlider != null)
            {
                shieldSlider.gameObject.SetActive(max > 0);
                shieldSlider.value = Mathf.Clamp01(current / max);
            }
        }

        public void SetPosition(Vector3 enemyPosition)
        {
            transform.position = enemyPosition + offset;
        }
    }
}
```

Shield slider is hidden (SetActive false) when maxShield is 0 — normal and armoured enemies don't show it.

Update `EnemyController.Update` to also drive the shield bar:

```csharp
if (healthBar != null)
{
    healthBar.SetHealth(Health.CurrentHealth, Health.MaxHealth);
    healthBar.SetPosition(transform.position);

    if (Health is ShieldHealth shield)
        healthBar.SetShield(shield.CurrentShield, shield.MaxShield);
    else
        healthBar.SetShield(0, 0);
}
```

The `is` pattern check is the one place EnemyController knows about a specific health type. This is a pragmatic trade-off — UI display needs to know about shields. You could avoid this with an `IHasShield` interface if it bothers you.

## Unity Editor Setup

### 1. Create HealthConfig SOs

- `HC_Shield`: Type=Shield, Start Health=100, Shield Amount=30
- `HC_Regen`: Type=Regen, Start Health=80, Regen Rate=5

### 2. Create EnemyData SOs

- `ED_Shield`: HC_Shield + MC_Grounded, Gold=25, Damage=1
- `ED_Regen`: HC_Regen + MC_Flying, Gold=20, Damage=1

### 3. Add shield slider to health bar prefab

See Shield Bar UI section above.

## Test Plan

1. Shield enemy: takes 3 hits of 10 → shield depleted, further hits damage health. Blue bar drains first.
2. Regen enemy: health slowly refills when not taking damage. Must out-damage the regen rate to kill.
3. Zero EnemyController changes — verify by diffing the file against Episode 11's version.
4. Compose with movement types: shield+ground, shield+flying, regen+ground, regen+flying all work.

## Debugging

| Symptom | Cause | Fix |
|---------|-------|-----|
| Shield doesn't absorb | ShieldAmount is 0 in config | Set ShieldAmount > 0 in HC_Shield |
| Shield bar doesn't show | shieldSlider not assigned | Assign in Inspector |
| Regen too fast/slow | RegenRate in wrong units | Rate is HP per second. 5 = +5 HP/sec |
| Enemy heals past max | Tick not capping | Verify `Mathf.Min(current + regen * dt, maxHealth)` |
| Factory throws on Shield/Regen | Missing enum cases | Add ShieldHealth and RegenHealth to StrategyFactory |