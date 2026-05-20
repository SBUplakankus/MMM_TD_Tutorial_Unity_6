# Episode 09: Advanced Behaviors — Implementation Guide

## What You're Building

Shield enemies absorb damage through a shield buffer before health is touched. Regen enemies heal over time after a damage-free cooldown window.

The architecture is already in place. `IHealthStrategy` defines the contract. `EnemyController` already calls `Health.Tick(Time.deltaTime)` in `Update` and `Health.TakeDamage(damage)` returns a `DamageResult`. All you're doing is writing two new classes that implement the same interface, adding their enum cases to `StrategyFactory`, and confirming the `HealthConfig` SO has the right fields.

No base class changes. No `Instantiate()` shared-SO bug. No virtual method dispatch. Just plain C# classes behind an interface.

**The key lesson:** Adding ShieldHealth and RegenHealth required ZERO changes to `IHealthStrategy`, `EnemyController`, `TowerDetection`, `ProjectileBase`, or any other system. Only three things changed: new class file, factory switch case, config SO field. That's the power of the interface + factory pattern.

---

## Files & Order

| # | File | Action |
|---|------|--------|
| 1 | `Assets/Scripts/Data/HealthConfig.cs` | UPDATE — confirm Shield/Regen fields exist |
| 2 | `Assets/Scripts/Strategies/Health/ShieldHealth.cs` | NEW — full implementation |
| 3 | `Assets/Scripts/Strategies/Health/RegenHealth.cs` | NEW — full implementation |
| 4 | `Assets/Scripts/Systems/Parsing/StrategyFactory.cs` | UPDATE — add Shield/Regen cases |

---

## Implementation

### 1. HealthConfig.cs — confirm fields

The config SO should already have `shieldPoints`, `regenRate`, and `regenDelay` from the enum being defined with Shield and Regen values. Confirm the file matches:

```csharp
using UnityEngine;

namespace Data
{
    public enum HealthType
    {
        Normal,
        Armoured,
        Shield,
        Regen
    }

    [CreateAssetMenu(fileName = "HealthConfig", menuName = "Scriptable Objects/Config/Health")]
    public class HealthConfig : ScriptableObject
    {
        [Tooltip("Which IHealthStrategy to create")]
        [SerializeField] private HealthType type;
        [SerializeField] private int startHealth = 100;
        [Range(0f, 0.99f)]
        [SerializeField] private float armourPercent;
        [SerializeField] private int shieldPoints;
        [SerializeField] private float regenRate;
        [SerializeField] private float regenDelay;

        public HealthType Type => type;
        public int StartHealth => startHealth;
        public float ArmourPercent => armourPercent;
        public int ShieldPoints => shieldPoints;
        public float RegenRate => regenRate;
        public float RegenDelay => regenDelay;
    }
}
```

**No logic, no strategy references.** This is pure data. The `type` enum drives `StrategyFactory.CreateHealth()` — the factory reads `config.Type` and constructs the right class. The Shield/Regen fields are unused when `type` is Normal or Armoured, and vice versa. The Inspector shows all fields regardless — you can hide them with a custom editor later if the clutter bothers you, but it's not required.

---

### 2. ShieldHealth.cs — full implementation

Shield absorbs incoming damage before it touches health. If damage exceeds remaining shield, the overflow applies to health.

```csharp
using Interfaces;

namespace Strategies.Health
{
    public class ShieldHealth : IHealthStrategy
    {
        private readonly int _startHealth;
        private readonly int _shieldPoints;

        private float _currentHealth;
        private float _currentShield;

        public bool IsAlive => _currentHealth > 0f;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _startHealth;

        public ShieldHealth(int startHealth, int shieldPoints)
        {
            _startHealth = startHealth;
            _shieldPoints = shieldPoints;
        }

        public void Initialize()
        {
            _currentHealth = _startHealth;
            _currentShield = _shieldPoints;
        }

        public DamageResult TakeDamage(float amount)
        {
            if (_currentShield > 0f)
            {
                if (_currentShield >= amount)
                {
                    _currentShield -= amount;
                    return DamageResult.Alive(amount);
                }

                float overflow = amount - _currentShield;
                float shieldAbsorbed = _currentShield;
                _currentShield = 0f;
                _currentHealth -= overflow;

                if (_currentHealth <= 0f)
                    return DamageResult.Dead(shieldAbsorbed + _currentHealth + overflow);

                return DamageResult.Alive(amount);
            }

            _currentHealth -= amount;

            if (_currentHealth <= 0f)
                return DamageResult.Dead(_currentHealth + amount);

            return DamageResult.Alive(amount);
        }

        public void Tick(float deltaTime) { }
    }
}
```

**Logic walkthrough — the three branches:**

1. **Shield up, can absorb full hit** (`_currentShield >= amount`): reduce shield, return `Alive`. Health untouched. No death check needed.

2. **Shield up, can't absorb full hit** (`_currentShield < amount`): calculate overflow, zero the shield, apply overflow to health. Check if health dropped to 0 or below — if so, return `Dead`. The `DamageDealt` in the Dead result accounts for the actual damage that killed the enemy (shield absorbed portion + health overflow that brought it to ≤ 0).

3. **Shield down** (`_currentShield <= 0`): damage goes straight to health, same as NormalHealth. Check death.

**Gotcha — why return Alive when shield absorbs everything:**

`EnemyController.TakeDamage` checks `result.Died` to decide whether to call `Die()`. When the shield absorbs the full hit, the enemy is still alive — we must return `Alive()` so `Die()` is NOT called. The old ScriptableObject version had a bug where `CheckForDeath()` was skipped on full shield absorb, which was correct behavior but happened by accident (early return before the death check line). Now it's explicit in the return value.

**Gotcha — DamageResult.Dead parameter when dying with shield overflow:**

When the enemy dies from overflow damage, the `DamageDealt` in `DamageResult.Dead(...)` should represent actual damage dealt to the enemy, not the raw incoming amount. Calculate it as: shield absorbed + overflow that actually reduced health (clamped so it doesn't go negative). This keeps damage-tracking accurate if you ever display damage numbers or calculate kill credit.

---

### 3. RegenHealth.cs — full implementation

After `regenDelay` seconds without taking damage, health regenerates at `regenRate` per second. `TakeDamage` resets the timer.

```csharp
using Interfaces;

namespace Strategies.Health
{
    public class RegenHealth : IHealthStrategy
    {
        private readonly int _startHealth;
        private readonly float _regenRate;
        private readonly float _regenDelay;

        private float _currentHealth;
        private float _timeSinceLastDamage;

        public bool IsAlive => _currentHealth > 0f;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _startHealth;

        public RegenHealth(int startHealth, float regenRate, float regenDelay)
        {
            _startHealth = startHealth;
            _regenRate = regenRate;
            _regenDelay = regenDelay;
        }

        public void Initialize()
        {
            _currentHealth = _startHealth;
            _timeSinceLastDamage = 0f;
        }

        public DamageResult TakeDamage(float amount)
        {
            _currentHealth -= amount;
            _timeSinceLastDamage = 0f;

            if (_currentHealth <= 0f)
                return DamageResult.Dead(_currentHealth + amount);

            return DamageResult.Alive(amount);
        }

        public void Tick(float deltaTime)
        {
            if (_currentHealth <= 0f) return;

            _timeSinceLastDamage += deltaTime;

            if (_timeSinceLastDamage < _regenDelay) return;

            _currentHealth += _regenRate * deltaTime;

            if (_currentHealth > _startHealth)
                _currentHealth = _startHealth;
        }
    }
}
```

**Logic walkthrough — Tick:**

1. Dead enemies can't regen. Early return if `_currentHealth <= 0f`.
2. Accumulate `_timeSinceLastDamage` every frame.
3. Only when the timer exceeds `_regenDelay` does healing begin.
4. Healing is `_regenRate * deltaTime` per frame, clamped to `_startHealth` (no overheal).
5. `TakeDamage` resets `_timeSinceLastDamage` to 0, restarting the delay window.

**Why `_timeSinceLastDamage` starts at 0 on Initialize:**

A freshly spawned regen enemy should not immediately start healing — it's at full health anyway. But if you spawn a regen enemy with partial health (e.g., from a wave config), the delay window should start from spawn. Setting the timer to 0 means the enemy must survive `_regenDelay` seconds without being hit before regen kicks in. This is intentional: regen rewards the player for focusing fire on regen enemies.

**Why no MathF.Min for the clamp:**

`if (_currentHealth > _startHealth) _currentHealth = _startHealth;` is a simple branch. `Mathf.Min` would also work but adds a method call for a trivial comparison. Either is fine — use whichever reads clearer to you.

---

### 4. StrategyFactory.cs — add Shield and Regen cases

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
                HealthType.Shield => new ShieldHealth(config.StartHealth, config.ShieldPoints),
                HealthType.Regen => new RegenHealth(config.StartHealth, config.RegenRate, config.RegenDelay),
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

**What changed:** two new cases in the `CreateHealth` switch. That's it. The factory reads the config enum, calls the right constructor with the right fields, and returns an `IHealthStrategy`. `EnemyController` never knows which concrete type it got — it just calls `Initialize()`, `TakeDamage()`, and `Tick()` through the interface.

**The default case:** falls back to `NormalHealth`. This shouldn't happen in production, but it prevents a crash if someone adds a new `HealthType` enum value without updating the factory. You could also throw an `ArgumentOutOfRangeException` here — either approach is valid.

---

### What did NOT change

This is the most important part of the episode:

| System | Changed? | Why |
|--------|----------|-----|
| `IHealthStrategy` | No | Shield and Regen implement the same interface |
| `EnemyController` | No | Already calls `Health.Tick()` and `Health.TakeDamage()` |
| `TowerDetection` | No | Targets `ITargetable`, doesn't know about health types |
| `ProjectileBase` | No | Calls `IDamageable.TakeDamage()`, same as before |
| `NormalHealth` | No | Still works, Tick is empty |
| `ArmouredHealth` | No | Still works, Tick is empty |
| `DamageResult` | No | Already supports Alive/Dead returns |

Only three things changed: `ShieldHealth.cs` (new file), `RegenHealth.cs` (new file), `StrategyFactory.cs` (two switch cases). The `HealthConfig` SO already had the fields.

This is the open/closed principle in action. The system is open for extension (new health types) but closed for modification (existing code untouched).

---

## Unity Editor Setup

### Create HealthConfig SOs

1. In `Assets/Data/Enemies/`, right-click → **Create → Scriptable Objects → Config → Health**
2. Create **ShieldHealthConfig**:
   - **Type:** `Shield`
   - **Start Health:** `100`
   - **Armour Percent:** `0` (unused)
   - **Shield Points:** `50`
   - **Regen Rate:** `0` (unused)
   - **Regen Delay:** `0` (unused)
3. Create **RegenHealthConfig**:
   - **Type:** `Regen`
   - **Start Health:** `150`
   - **Armour Percent:** `0` (unused)
   - **Shield Points:** `0` (unused)
   - **Regen Rate:** `10`
   - **Regen Delay:** `3`

### Create EnemyData compositions

| EnemyData Name | HealthConfig | MovementConfig | Gold | Damage |
|---|---|---|---|---|
| ShieldEnemy | ShieldHealthConfig | GroundedConfig | 15 | 2 |
| RegenEnemy | RegenHealthConfig | GroundedConfig | 20 | 1 |
| ShieldFlyer | ShieldHealthConfig | FlyingConfig | 25 | 2 |
| RegenFlyer | RegenHealthConfig | FlyingConfig | 30 | 2 |

**Important:** Each EnemyData holds ONE HealthConfig. You cannot stack Shield + Regen on the same enemy through data alone. If you need an enemy with both behaviors, that's a future `CompositeHealth` — a single `IHealthStrategy` implementation that delegates to inner strategies. Out of scope for this episode.

### Verification

- Select ShieldHealthConfig → confirm Type = Shield, Shield Points visible
- Select RegenHealthConfig → confirm Type = Regen, Regen Rate / Regen Delay visible
- Play mode: spawn shield enemy, deal 30 damage → shield absorbs, health stays at 100. Deal 60 damage → shield breaks (0), health drops to 90 (10 overflow).
- Play mode: spawn regen enemy, deal 40 damage (150 HP → 110), wait 3+ seconds → health slowly climbs at 10 HP/s.

---

## Test Plan

| # | Test | Expected Result |
|---|---|---|
| 1 | Shield enemy (100HP, 50 shield), deal 30 damage | Shield = 20, Health = 100, IsAlive = true |
| 2 | Shield enemy (100HP, 50 shield), deal 60 damage | Shield = 0, Health = 90, DamageResult.Died = false |
| 3 | Shield enemy, shield at 0, deal 30 damage | Health = 60, no shield interaction |
| 4 | Shield enemy, deal 150 damage (50 shield + 100 HP) | Health ≤ 0, DamageResult.Died = true, Die() called |
| 5 | Regen enemy (150HP), deal 40 damage | Health = 110, timer resets to 0 |
| 6 | Regen enemy, wait 4 seconds after damage (3s delay) | Health starts climbing at 10 HP/s |
| 7 | Regen enemy, damage during regen | Timer resets to 0, regen stops, 3s delay restarts |
| 8 | Regen enemy, heal until max | Health clamps at 150 (MaxHealth), no overheal |
| 9 | NormalHealth enemy still works | Tick() is empty, no errors in console |
| 10 | ArmouredHealth enemy still works | Tick() is empty, no errors in console |
| 11 | Existing enemies in waves still spawn and move | No regression from factory changes |
| 12 | StrategyFactory fallback: invalid enum value | Returns NormalHealth, no crash |

---

## Debugging Tips

**Shield not absorbing damage:**
- Check `ShieldPoints` in the HealthConfig SO — confirm it's not 0.
- Add `Debug.Log($"Shield: {_currentShield}, Dmg: {amount}");` at the top of `TakeDamage`.
- Confirm the factory switch case for `HealthType.Shield` actually runs — log `config.Type` before the switch.

**Regen not healing:**
- Confirm `EnemyController.Update()` calls `Health.Tick(Time.deltaTime)`. If it doesn't, RegenHealth.Tick never runs.
- Confirm `regenDelay` and `regenRate` are both > 0 in the HealthConfig SO.
- `_timeSinceLastDamage` resets on every `TakeDamage` call — if something accidentally calls `TakeDamage(0)`, regen will never start. Log calls to TakeDamage to check.
- Is the enemy alive? Dead enemies skip regen (`_currentHealth <= 0f` early return).

**Enemy dies immediately on spawn:**
- Check `StartHealth` is > 0 in the HealthConfig SO. If it defaults to 0, the enemy is dead on `Initialize()`.
- ShieldHealth: if both `ShieldPoints` and `StartHealth` are 0, enemy is dead on init.

**NullReferenceException on Health.Tick:**
- `Health` property is null if `EnemyController.Initialize()` hasn't been called. Confirm the spawner calls `Initialize(data, path)` before the enemy's first `Update`.
- With the factory pattern, `Health` is set inside `Initialize()` — it can't be set from the Inspector. If someone accidentally removes the `StrategyFactory.CreateHealth()` call, Health stays null.

**Wrong health type spawning:**
- The factory reads `config.Type` from the enum. If the enum value doesn't match the intended class, the wrong strategy is created.
- Example: HealthConfig SO has Type = Normal but you expected ShieldHealth. Verify the Type dropdown in the Inspector.

**DamageResult.Died is never true:**
- Check that ShieldHealth returns `DamageResult.Dead(...)` when health drops to ≤ 0, not `Alive()`. The return in the shield-overflow branch must check `_currentHealth <= 0f` after applying overflow.