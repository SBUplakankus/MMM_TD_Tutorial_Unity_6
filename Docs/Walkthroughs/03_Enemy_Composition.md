# Episode 3: Enemy Type Composition — Implementation Guide

## What You're Building

Two config ScriptableObjects (`HealthConfig`, `MovementConfig`), a static `StrategyFactory` that creates strategy instances from config enums, and the first two concrete strategies for each interface (`NormalHealth`, `ArmouredHealth`, `GroundedPath`, `FlyingPath`). You'll also update `EnemyData` to hold config references instead of strategy references, then create four enemy type compositions in the Unity Editor. This is the episode where the strategy pattern actually produces enemy types.

## Files & Order

1. `Assets/Scripts/Data/HealthConfig.cs` — **implement**
2. `Assets/Scripts/Data/MovementConfig.cs` — **implement**
3. `Assets/Scripts/Strategies/Health/NormalHealth.cs` — **implement**
4. `Assets/Scripts/Strategies/Health/ArmouredHealth.cs` — **implement**
5. `Assets/Scripts/Strategies/Movement/GroundedPath.cs` — **implement**
6. `Assets/Scripts/Strategies/Movement/FlyingPath.cs` — **implement**
7. `Assets/Scripts/Systems/Parsing/StrategyFactory.cs` — **implement**
8. `Assets/Scripts/Data/EnemyData.cs` — already updated (just verify)
9. `Assets/Scripts/Enemies/Controllers/EnemyController.cs` — already updated (just verify)

## Implementation

### File: `Assets/Scripts/Data/HealthConfig.cs`

A pure-data SO. No behavior, no logic. Just values that `StrategyFactory` reads to create the right `IHealthStrategy` instance.

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
        [Header("Health Type")]
        [SerializeField] private HealthType type;

        [Header("Base Stats")]
        [SerializeField] private int startHealth = 100;

        [Header("Armoured")]
        [Range(0f, 0.99f)]
        [SerializeField] private float armourPercent;

        [Header("Shield")]
        [SerializeField] private int shieldPoints;

        [Header("Regen")]
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

**Field-by-field:**

| Field | Used By | When |
|-------|---------|------|
| `type` | `StrategyFactory.CreateHealth()` | Determines which concrete class to instantiate |
| `startHealth` | All health strategies | Constructor parameter — passed to `new NormalHealth(startHealth)` etc. |
| `armourPercent` | `ArmouredHealth` only | `0.3` means 30% damage reduction. Other types ignore this field. |
| `shieldPoints` | `ShieldHealth` only | Absorbed before main health. Other types ignore. |
| `regenRate` | `RegenHealth` only | HP per second regenerated after `regenDelay`. Other types ignore. |
| `regenDelay` | `RegenHealth` only | Seconds after last damage before regen starts. Other types ignore. |

**Why all fields live in one SO instead of separate config types:**

Each health type only reads 2-3 fields. The rest show in the Inspector but are ignored. The alternative — four separate config classes (`NormalHealthConfig`, `ArmouredHealthConfig`, etc.) — means four `CreateAssetMenu` entries and the factory needs four different types. One config SO with an enum is simpler. Unused fields default to zero, which is harmless.

**The `[Range(0f, 0.99f)]` on armourPercent:**

`armourPercent = 1.0` would mean 100% damage reduction — an immortal enemy. The range attribute prevents this in the Inspector. The slider clamps at 0.99 (99%), which gives 100x effective HP — strong but still killable.

---

### File: `Assets/Scripts/Data/MovementConfig.cs`

Same pattern as `HealthConfig` — pure data, no behavior.

```csharp
using UnityEngine;

namespace Data
{
    public enum MovementType
    {
        Grounded,
        Flying
    }

    [CreateAssetMenu(fileName = "MovementConfig", menuName = "Scriptable Objects/Config/Movement")]
    public class MovementConfig : ScriptableObject
    {
        [Header("Movement Type")]
        [SerializeField] private MovementType type;

        [Header("Base Stats")]
        [SerializeField] private float moveSpeed = 3f;

        [Header("Flying")]
        [Range(0f, 5f)]
        [SerializeField] private float flyingHeight;

        public MovementType Type => type;
        public float MoveSpeed => moveSpeed;
        public float FlyingHeight => flyingHeight;
    }
}
```

**Field-by-field:**

| Field | Used By | When |
|-------|---------|------|
| `type` | `StrategyFactory.CreateMovement()` | Determines `GroundedPath` vs `FlyingPath` |
| `moveSpeed` | All movement strategies | Units per second along waypoint path |
| `flyingHeight` | `FlyingPath` only | Y offset above waypoints. GroundedPath ignores this. |

**The `[Range(0f, 5f)]` on flyingHeight:**

Zero means the flying enemy is on the ground (same as grounded — useful for debugging). Five units is high enough to be visually distinct from ground-level enemies without breaking camera frustum or detection range.

---

### File: `Assets/Scripts/Strategies/Health/NormalHealth.cs`

Plain C# class. No `MonoBehaviour`, no `ScriptableObject`. Created by `StrategyFactory`, one instance per enemy.

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

**Constructor pattern:** `StrategyFactory` calls `new NormalHealth(config.StartHealth)`. The constructor stores the config value; `Initialize()` applies it to `_currentHealth`. This two-step pattern matches the interface contract — every `IHealthStrategy` has `Initialize()` called after construction.

**TakeDamage trace (startHealth = 100):**
```
Hit 1: TakeDamage(25)
  → _currentHealth = 100 - 25 = 75
  → 75 > 0 → return DamageResult.Alive(25)

Hit 2: TakeDamage(25)
  → _currentHealth = 75 - 25 = 50
  → 50 > 0 → return DamageResult.Alive(25)

Hit 3: TakeDamage(25)
  → _currentHealth = 50 - 25 = 25
  → 25 > 0 → return DamageResult.Alive(25)

Hit 4: TakeDamage(25)
  → _currentHealth = 25 - 25 = 0
  → 0 <= 0 → _currentHealth = 0 → return DamageResult.Dead(25)
```

Four hits of 25 damage kills a 100hp NormalHealth enemy.

**`_currentHealth = 0f` clamp on death:**

Without this clamp, overkill damage leaves `_currentHealth` negative. `IsAlive` still returns false (negative is not > 0), but health bar UI would show a negative fill ratio which could break a `Mathf.Clamp01` somewhere. Clamping to 0 is a safety net.

**Tick is empty:**

NormalHealth has no per-frame logic. The method must exist to satisfy the interface, but it's a no-op. The `RegenHealth` implementation (later episode) is the only health strategy that uses `Tick`.

---

### File: `Assets/Scripts/Strategies/Health/ArmouredHealth.cs`

Same structure as NormalHealth, but damage is reduced by `armourPercent` before being applied.

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

**TakeDamage trace (startHealth = 200, armourPercent = 0.3):**

The formula: `reducedDamage = amount * (1f - armourPercent)` = `amount * 0.7f`

```
Hit 1: TakeDamage(10)
  → reducedDamage = 10 * 0.7 = 7
  → _currentHealth = 200 - 7 = 193
  → 193 > 0 → return DamageResult.Alive(7)

Hit 2: TakeDamage(10)
  → reducedDamage = 7
  → _currentHealth = 193 - 7 = 186
  → alive

... after 28 hits of 10 damage each:
  → Total applied: 28 * 7 = 196
  → _currentHealth = 200 - 196 = 4

Hit 29: TakeDamage(10)
  → reducedDamage = 7
  → _currentHealth = 4 - 7 = -3
  → -3 <= 0 → _currentHealth = 0 → return DamageResult.Dead(7)
```

**Effective HP:** `startHealth / (1 - armourPercent)` = `200 / 0.7` ≈ **286 effective HP**. That's 1.43x the base HP — a 200HP armoured enemy acts like a 286HP normal enemy against the same damage.

**armourPercent values and their impact:**

| armourPercent | Damage Multiplier | Effective HP (base 200) |
|---------------|-------------------|------------------------|
| 0.0 | 1.0x | 200 |
| 0.2 | 0.8x | 250 |
| 0.3 | 0.7x | ~286 |
| 0.5 | 0.5x | 400 |
| 0.75 | 0.25x | 800 |
| 0.99 | 0.01x | 20,000 |

**Key difference from NormalHealth.TakeDamage:** `DamageResult.DamageDealt` returns the *reduced* damage (7), not the raw damage (10). This lets any future UI or logging system show "7 damage absorbed" vs "10 damage attempted."

---

### File: `Assets/Scripts/Strategies/Movement/GroundedPath.cs`

Plain C# class implementing `IMovementStrategy`. Moves an enemy along waypoint positions at a fixed speed.

```csharp
using System;
using Enemies.Controllers;
using Interfaces;
using UnityEngine;

namespace Strategies.Movement
{
    public class GroundedPath : IMovementStrategy
    {
        private readonly float _moveSpeed;
        private EnemyPath _path;

        public event Action OnMovementCompleted;

        public GroundedPath(float moveSpeed)
        {
            _moveSpeed = moveSpeed;
        }

        public void Initialize(EnemyController enemy)
        {
            _path = enemy.Path;
            OnMovementCompleted = null;
            enemy.CurrentWayPointIndex = 0;
            enemy.transform.position = _path.StartPosition;
        }

        public void Tick(EnemyController enemy)
        {
            int index = enemy.CurrentWayPointIndex;

            if (!_path.HasWaypoint(index))
            {
                OnMovementCompleted?.Invoke();
                return;
            }

            Vector3 target = _path.GetWaypointPosition(index);

            if (_path.IsAtWaypoint(index, enemy.transform.position))
            {
                enemy.CurrentWayPointIndex = index + 1;
                return;
            }

            enemy.transform.position = Vector3.MoveTowards(
                enemy.transform.position, target, _moveSpeed * Time.deltaTime);
        }
    }
}
```

**Constructor:** `StrategyFactory` calls `new GroundedPath(config.MoveSpeed)`. Only the speed value is stored — the path reference comes later in `Initialize`.

**Initialize trace (moveSpeed = 5):**
```
Initialize(enemy):
  _path = enemy.Path                // store reference to the waypoint path
  OnMovementCompleted = null        // clear subscribers (pool reuse safety)
  enemy.CurrentWayPointIndex = 0    // start at first waypoint
  enemy.transform.position = _path.StartPosition   // snap to WP0
```

**Tick trace — concrete numbers (moveSpeed = 5, waypoints at (0,0,0) and (5,0,0)):**

Frame 1 (enemy at (0,0,0)):
```
Tick(enemy):
  index = 0
  HasWaypoint(0) → true
  target = GetWaypointPosition(0) = (0, 0, 0)
  IsAtWaypoint(0, (0,0,0)) → sqrMagnitude = 0 ≤ 0.01 → TRUE
  CurrentWayPointIndex = 1
  return                                    ← snaps to next waypoint
```

Frame 2 (enemy still at (0,0,0), index now 1):
```
Tick(enemy):
  index = 1
  HasWaypoint(1) → true
  target = (5, 0, 0)
  IsAtWaypoint(1, (0,0,0)) → (5)² = 25 > 0.01 → FALSE
  MoveTowards((0,0,0), (5,0,0), 5 * 0.016) = (0.08, 0, 0)
  enemy.transform.position = (0.08, 0, 0)
```

**Time to traverse 5 units at speed 5:** `5/5 = 1 second`.

**OnMovementCompleted = null in Initialize:**

This line is critical for object pooling. When an enemy is returned to the pool and later reused, the `EnemyController` subscribes to `OnMovementCompleted` again in `Initialize`. Without clearing the event first, the second subscription *adds* a second handler — the callback fires twice. Setting it to `null` before the controller re-subscribes prevents this. The controller always does `Movement.OnMovementCompleted += OnReachedEnd` *after* `Initialize` runs.

---

### File: `Assets/Scripts/Strategies/Movement/FlyingPath.cs`

Same movement logic as `GroundedPath`, but the Y position is offset by `flyingHeight` at all times.

```csharp
using System;
using Enemies.Controllers;
using Interfaces;
using UnityEngine;

namespace Strategies.Movement
{
    public class FlyingPath : IMovementStrategy
    {
        private readonly float _moveSpeed;
        private readonly float _flyingHeight;
        private EnemyPath _path;

        public event Action OnMovementCompleted;

        public FlyingPath(float moveSpeed, float flyingHeight)
        {
            _moveSpeed = moveSpeed;
            _flyingHeight = flyingHeight;
        }

        public void Initialize(EnemyController enemy)
        {
            _path = enemy.Path;
            OnMovementCompleted = null;
            enemy.CurrentWayPointIndex = 0;
            Vector3 startPos = _path.StartPosition;
            startPos.y += _flyingHeight;
            enemy.transform.position = startPos;
        }

        public void Tick(EnemyController enemy)
        {
            int index = enemy.CurrentWayPointIndex;

            if (!_path.HasWaypoint(index))
            {
                OnMovementCompleted?.Invoke();
                return;
            }

            Vector3 target = _path.GetWaypointPosition(index);
            target.y += _flyingHeight;

            if (_path.IsAtWaypoint(target, enemy.transform.position))
            {
                enemy.CurrentWayPointIndex = index + 1;
                return;
            }

            enemy.transform.position = Vector3.MoveTowards(
                enemy.transform.position, target, _moveSpeed * Time.deltaTime);
        }
    }
}
```

**Key difference from GroundedPath — the `target.y += _flyingHeight` offset:**

GroundedPath: `target = _path.GetWaypointPosition(index)` — moves toward the raw waypoint position.

FlyingPath: `target = _path.GetWaypointPosition(index); target.y += _flyingHeight;` — moves toward the waypoint position plus Y offset.

**Why `IsAtWaypoint` uses the 2-parameter overload (`Vector3, Vector3`) instead of the index-based overload:**

`_path.IsAtWaypoint(index, position)` compares `position` against `waypoints[index].position` — the *raw* waypoint Y. A flying enemy at `(5, 2, 0)` trying to reach waypoint `(5, 0, 0)` would have a Y difference of 2, giving `sqrMagnitude` of 4 — far above the threshold of 0.01. The enemy would never reach the waypoint.

`_path.IsAtWaypoint(target, position)` compares `position` against the *Y-offset target* `(5, 2, 0)`. The `sqrMagnitude` is 0 (or tiny) — the enemy is at the waypoint.

**Initialize trace (moveSpeed = 4, flyingHeight = 2, WP0 at (0,0,0)):**
```
Initialize(enemy):
  _path = enemy.Path
  OnMovementCompleted = null
  CurrentWayPointIndex = 0
  startPos = _path.StartPosition = (0, 0, 0)
  startPos.y += 2 → (0, 2, 0)
  enemy.transform.position = (0, 2, 0)        ← spawns above ground
```

**Tick trace (enemy at (0, 2, 0), WP1 at (5, 0, 0), deltaTime ≈ 0.016):**
```
Tick(enemy):
  index = 1
  HasWaypoint(1) → true
  target = GetWaypointPosition(1) = (5, 0, 0)
  target.y += 2 → (5, 2, 0)                  ← Y offset applied
  IsAtWaypoint((5,2,0), (0,2,0)) → sqrMagnitude = 25 > 0.01 → FALSE
  MoveTowards((0,2,0), (5,2,0), 4 * 0.016) = (0.064, 2, 0)
  enemy.transform.position = (0.064, 2, 0)   ← stays at Y=2
```

**Time to traverse 5 units at speed 4:** `5/4 = 1.25 seconds`. Faster than GroundedPath at speed 5 (1 second) only if speed values differ. Design intent: flying enemies are slightly slower (speed 4) but take a shorter visual path because they ignore ground obstacles.

---

### File: `Assets/Scripts/Systems/Parsing/StrategyFactory.cs`

Static factory that creates the right `IHealthStrategy` or `IMovementStrategy` from a config SO's enum value.

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
                HealthType.Armoured => new ArmouredHealth(
                    config.StartHealth, config.ArmourPercent),
                HealthType.Shield => new ShieldHealth(
                    config.StartHealth, config.ShieldPoints),
                HealthType.Regen => new RegenHealth(
                    config.StartHealth, config.RegenRate, config.RegenDelay),
                _ => new NormalHealth(config.StartHealth)
            };
        }

        public static IMovementStrategy CreateMovement(MovementConfig config)
        {
            return config.Type switch
            {
                MovementType.Grounded => new GroundedPath(config.MoveSpeed),
                MovementType.Flying => new FlyingPath(
                    config.MoveSpeed, config.FlyingHeight),
                _ => new GroundedPath(config.MoveSpeed)
            };
        }
    }
}
```

**Pattern: enum switch → constructor call:**

Each case in the switch reads the relevant fields from the config SO and passes them as constructor parameters to the concrete class. The factory owns the mapping from "which type" to "which class."

**Why the `_ =>` default case returns NormalHealth/GroundedPath:**

If someone adds a new `HealthType` enum value but forgets to add it to the factory switch, the default case prevents a crash. The enemy silently gets NormalHealth behavior instead of throwing. This is a "fail safe" default — NormalHealth is the simplest strategy, no armor, no special mechanics.

**Why this class is `static`:**

The factory has no state. It doesn't need instantiation. It's a pure function: config in, strategy out. Making it `static` communicates this intent and prevents accidental stateful usage.

**Why it's in the `Systems.Parsing` namespace:**

This project groups infrastructure/systems code under `Systems.Parsing` (which also houses `CsvWaveParser`). The factory is a "parser" in the sense that it converts config data into runtime objects. If you prefer `Systems.Factory` or `Data`, that works too — the namespace doesn't affect behavior.

---

### File: `Assets/Scripts/Data/EnemyData.cs`

**Already updated.** Verify it holds `HealthConfig` and `MovementConfig` references instead of old strategy SO references:

```csharp
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/Data/Enemy")]
    public class EnemyData : ScriptableObject
    {
        #region Fields

        [Header("Enemy Config")]
        [SerializeField] private HealthConfig healthConfig;
        [SerializeField] private MovementConfig movementConfig;

        [Header("Enemy Stats")]
        [SerializeField] private int goldGiven;
        [SerializeField] private int damage;

        #endregion

        #region Properties

        public HealthConfig HealthConfig => healthConfig;
        public MovementConfig MovementConfig => movementConfig;
        public int GoldGiven => goldGiven;
        public int Damage => damage;

        #endregion
    }
}
```

**What changed:**

| Old | New |
|-----|-----|
| `HealthStrategy health` | `HealthConfig healthConfig` |
| `MovementStrategy movement` | `MovementConfig movementConfig` |

The old fields held references to strategy SOs with both data *and* behavior. The new fields hold references to config SOs with data only. `EnemyController.Initialize` passes these to `StrategyFactory`, which creates behavior instances.

---

### The Shared-State Bug — Eliminated

The architecture change from abstract SOs to interfaces + factory was driven by one bug. Understanding it completely is worth the time.

**Old architecture — the problem:**

```
NormalHealth.asset (SO instance, exists on disk)
├── startHealth = 100      ← serialized, never changes at runtime
├── CurrentHealth = ???    ← runtime field, shared by all references

EnemyController A:
  Health → NormalHealth.asset    ← same SO object

EnemyController B:
  Health → NormalHealth.asset    ← same SO object
```

When `A.Initialize()` runs: `Health.CurrentHealth = 100`
When `B.Initialize()` runs: `Health.CurrentHealth = 100` ← **overwrites A's value**
When `A.TakeDamage(25)` runs: `Health.CurrentHealth = 75` ← **B now also sees 75**

All enemies sharing the same SO asset share the same `CurrentHealth`. Damaging one damages all. Killing one kills all.

**Old workaround: clone the SO.**

```csharp
Health = Instantiate(data.Health);   // runtime clone
Movement = Instantiate(data.Movement);
```

Problems with cloning:
- Allocates a full `UnityEngine.Object` on the heap per enemy
- Unity tracks the clone internally, adding overhead
- If you forget to clone, the bug appears silently
- Cloned SOs can't be serialized back — they're anonymous runtime objects

**New architecture — the fix:**

```
HealthConfig.asset (SO instance, exists on disk)
├── type = Normal
├── startHealth = 100     ← serialized, read-only at runtime

StrategyFactory.CreateHealth(HealthConfig):
  → config.Type == Normal → new NormalHealth(config.StartHealth)
  → returns: NormalHealth instance (plain C# object on heap)

EnemyController A:
  Health → NormalHealth object A    ← own instance, own _currentHealth

EnemyController B:
  Health → NormalHealth object B    ← own instance, own _currentHealth
```

`NormalHealth object A._currentHealth = 100` and `NormalHealth object B._currentHealth = 100` are separate fields on separate objects. Damaging A has zero effect on B.

**The factory creates one instance per enemy.** No cloning, no shared state, no `Instantiate()` needed. The only shared reference is `HealthConfig.asset`, which is read-only at runtime — all enemies can safely share it because nobody modifies it.

---

### Initialize() Call Chain — Full Trace

Here's the exact sequence when `enemy.Initialize(data, path)` is called with `BasicEnemy` data:

```
EnemyController.Initialize(data, path)
│
├── Path = path
│   // Stores reference to the EnemyPath component (waypoints, start/end positions)
│
├── Health = StrategyFactory.CreateHealth(data.HealthConfig)
│   ├── data.HealthConfig.Type == Normal
│   └── return new NormalHealth(100)
│   // Health is now a NormalHealth instance with _startHealth = 100
│
├── Movement = StrategyFactory.CreateMovement(data.MovementConfig)
│   ├── data.MovementConfig.Type == Grounded
│   └── return new GroundedPath(5)
│   // Movement is now a GroundedPath instance with _moveSpeed = 5
│
├── GoldGiven = data.GoldGiven    // 10
├── Damage = data.Damage          // 1
│
├── Health.Initialize()
│   └── _currentHealth = _startHealth = 100
│
├── Movement.Initialize(this)
│   ├── _path = enemy.Path        // stores path reference
│   ├── OnMovementCompleted = null // clears previous subscribers
│   ├── enemy.CurrentWayPointIndex = 0
│   └── enemy.transform.position = _path.StartPosition   // snap to WP0
│
└── Movement.OnMovementCompleted += OnReachedEnd
    // Controller subscribes to the "reached end" event
```

**After Initialize completes:**
- Enemy is at WP0, facing WP1
- `_currentHealth = 100`
- `_moveSpeed = 5`, waypoint index = 0
- Controller is subscribed to movement completion event
- Next frame: `Update()` calls `Health.Tick(dt)` (no-op) and `Movement.Tick(this)` (starts moving)

---

## Unity Editor Setup

### Create HealthConfig SOs

**NormalHealthConfig:**
1. In Project window, navigate to `Assets/Data/Enemies/`
2. Right-click → **Create → Scriptable Objects → Config → Health**
3. Name: `NormalHealthConfig`
4. Inspector values:
   - **Type:** `Normal`
   - **Start Health:** `100`
   - All other fields: leave at defaults (0)

**ArmouredHealthConfig:**
1. Right-click in `Assets/Data/Enemies/` → **Create → Scriptable Objects → Config → Health**
2. Name: `ArmouredHealthConfig`
3. Inspector values:
   - **Type:** `Armoured`
   - **Start Health:** `200`
   - **Armour Percent:** `0.3`
   - All other fields: leave at defaults

### Create MovementConfig SOs

**GroundedConfig:**
1. Right-click in `Assets/Data/Enemies/` → **Create → Scriptable Objects → Config → Movement**
2. Name: `GroundedConfig`
3. Inspector values:
   - **Type:** `Grounded`
   - **Move Speed:** `5`
   - **Flying Height:** `0`

**FlyingConfig:**
1. Right-click in `Assets/Data/Enemies/` → **Create → Scriptable Objects → Config → Movement**
2. Name: `FlyingConfig`
3. Inspector values:
   - **Type:** `Flying`
   - **Move Speed:** `4`
   - **Flying Height:** `2`

### Create EnemyData SOs

All use Right-click → **Create → Scriptable Objects → Data → Enemy**.

**Basic:**
- **Health Config:** `NormalHealthConfig`
- **Movement Config:** `GroundedConfig`
- **Gold Given:** `10`
- **Damage:** `1`

**Armoured:**
- **Health Config:** `ArmouredHealthConfig`
- **Movement Config:** `GroundedConfig`
- **Gold Given:** `25`
- **Damage:** `2`

**Flying:**
- **Health Config:** `NormalHealthConfig`
- **Movement Config:** `FlyingConfig`
- **Gold Given:** `15`
- **Damage:** `1`

**FlyingArmoured:**
- **Health Config:** `ArmouredHealthConfig`
- **Movement Config:** `FlyingConfig`
- **Gold Given:** `40`
- **Damage:** `3`

### What Each EnemyData Produces

| EnemyData | Health | Movement | Effective HP | Speed | Gold | Threat |
|-----------|--------|----------|-------------|-------|------|--------|
| Basic | 100 (Normal) | 5 (Grounded) | 100 | 5 u/s | 10 | Low |
| Armoured | 200 (30% armor) | 5 (Grounded) | ~286 | 5 u/s | 25 | Medium-High |
| Flying | 100 (Normal) | 4 (Flying, height 2) | 100 | 4 u/s | 15 | Medium |
| FlyingArmoured | 200 (30% armor) | 4 (Flying, height 2) | ~286 | 4 u/s | 40 | High |

### Asset Folder Structure

```
Assets/Data/Enemies/
├── NormalHealthConfig.asset
├── ArmouredHealthConfig.asset
├── GroundedConfig.asset
├── FlyingConfig.asset
├── BasicEnemy.asset
├── ArmouredEnemy.asset
├── FlyingEnemy.asset
└── FlyingArmouredEnemy.asset
```

---

## Test Plan

### Test 1: Basic Enemy Spawns and Moves
1. Assign `BasicEnemy` EnemyData to your test spawner
2. Play, spawn the enemy
3. Verify: enemy appears at WP0, moves along waypoints at speed 5, fires `OnMovementCompleted` at the end

### Test 2: Switch to Flying
1. Change test data to `FlyingEnemy`
2. Play, spawn
3. Verify: enemy spawns at `(WP0.x, 2, WP0.z)` — Y offset of 2
4. Verify: moves at speed 4 (slightly slower than Basic)
5. Verify: stays at Y + 2 throughout the path

### Test 3: Armoured Damage Reduction
1. Change test data to `ArmouredEnemy`
2. Play, spawn
3. Call `TakeDamage(10)`:
   - `reducedDamage = 10 * (1 - 0.3) = 7`
   - `CurrentHealth = 200 - 7 = 193`
4. Verify health is **193**, not 190 (which would be unarmored reduction)

### Test 4: FlyingArmoured
1. Change test data to `FlyingArmouredEnemy`
2. Play, spawn
3. Verify: enemy flies at Y=2 with 200HP and 30% armor
4. Call `TakeDamage(10)`: expected health = 193 (same armor calculation as Armoured)
5. Verify: moves at speed 4 (same as Flying)

### Test 5: Shared-State Bug Is Gone
1. Spawn two `BasicEnemy` enemies simultaneously
2. Damage enemy A by 25
3. Check enemy B's health — should still be 100 (unchanged)
4. Damage enemy A by 75 more (total 100)
5. Verify: enemy A is dead, enemy B is still at 100 HP

This test confirms the factory pattern eliminated the shared-state bug. Each enemy has its own `NormalHealth` instance with its own `_currentHealth`.

### Test 6: DamageResult Death Flow
1. Spawn a `BasicEnemy`
2. Call `TakeDamage(50)` — check that `IsAlive` is still true
3. Call `TakeDamage(50)` — check that `Die()` is called (enemy should start cleanup)
4. Verify: no exceptions, no double-death calls

---

## Debugging Tips

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| `StrategyFactory` compile error — type not found | Missing `using Strategies.Health;` or `using Strategies.Movement;` | Add the using directives to `StrategyFactory.cs` |
| EnemyData fields show "None" in picker | Config SOs not created yet | Create `HealthConfig` and `MovementConfig` SOs first |
| Enemy spawns with 0 HP | `HealthConfig.StartHealth` is 0 in the SO | Check the config SO's `Start Health` field in Inspector |
| Armoured enemy takes full damage | `HealthConfig.ArmourPercent` is 0 (default) | Open the ArmouredHealthConfig SO, set armourPercent to 0.3 |
| Flying enemy moves at Y=0 | `MovementConfig` Type is Grounded, not Flying | Check the `Type` enum on the MovementConfig SO |
| Flying enemy never reaches waypoints | `IsAtWaypoint` using wrong overload | Verify `FlyingPath.Tick` uses `IsAtWaypoint(target, position)` not `IsAtWaypoint(index, position)` |
| `OnMovementCompleted` fires twice | Event not cleared in Initialize | `FlyingPath.Initialize`/`GroundedPath.Initialize` must set `OnMovementCompleted = null` before controller subscribes |
| NullReferenceException in CreateHealth | `HealthConfig` field on EnemyData is null | Assign a HealthConfig SO to the EnemyData |
| Two enemies share health values | Old SO strategy references instead of factory | Verify `EnemyController.Initialize` calls `StrategyFactory.CreateHealth`, not `data.Health` |
| Enemy doesn't move after spawn | `MoveSpeed` is 0 in MovementConfig | Check the `Move Speed` field in the MovementConfig SO |
| Enemy spawns at (0,0,0) not WP0 | EnemyPath waypoints array is empty | Populate waypoints on the EnemyPath component in the scene |