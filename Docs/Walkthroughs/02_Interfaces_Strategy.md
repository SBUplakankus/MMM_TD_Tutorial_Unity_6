# Episode 2: Interfaces & Strategy Pattern — Implementation Guide

## What You're Building

Three new interfaces (`IHealthStrategy`, `IMovementStrategy`, `ITargetingStrategy`) plus a `DamageResult` struct. These replace the old abstract ScriptableObject strategy base classes. You're also walking the existing interfaces (`IDamageable`, `ITargetable`, `ISelectable`, `IUpdatable`, `IPoolable`) to understand how EnemyController fits into the broader interface ecosystem, and you'll see why the architecture switched from abstract SOs to plain C# interfaces.

## Files & Order

1. `Assets/Scripts/Interfaces/IDamageable.cs` — already exists, walk
2. `Assets/Scripts/Interfaces/ITargetable.cs` — already exists, walk
3. `Assets/Scripts/Interfaces/ISelectable.cs` — already exists, walk
4. `Assets/Scripts/Interfaces/IUpdatable.cs` — already exists, walk
5. `Assets/Scripts/Interfaces/IPoolable.cs` — already exists, walk
6. `Assets/Scripts/Interfaces/IHealthStrategy.cs` — **implement**
7. `Assets/Scripts/Interfaces/IMovementStrategy.cs` — **implement**
8. `Assets/Scripts/Interfaces/ITargetingStrategy.cs` — **implement**
9. `Assets/Scripts/Enemies/Controllers/EnemyController.cs` — update to use IHealthStrategy/IMovementStrategy

## Implementation

### File: `Assets/Scripts/Interfaces/IDamageable.cs`

**Already complete.** Walk only.

```csharp
namespace Interfaces
{
    public interface IDamageable
    {
        public void TakeDamage(float damage);
    }
}
```

**Where it's used:** ProjectileBase calls `target.TakeDamage(damage)` when a projectile hits. The target is typed as `IDamageable`, so projectile code never touches `EnemyController` directly. Any future destructible object can implement `IDamageable` and projectiles will damage it the same way.

---

### File: `Assets/Scripts/Interfaces/ITargetable.cs`

**Already complete.** Walk only.

```csharp
using UnityEngine;

namespace Interfaces
{
    public interface ITargetable
    {
        public Vector3 Position { get; }
        public bool IsAlive { get; }
    }
}
```

**Where it's used:** `TowerDetection` builds a `List<ITargetable>` from physics overlap results. `ITargetingStrategy.GetTarget()` receives that list and returns the single best `ITargetable`. Towers never reference `EnemyController` — they work against this interface.

**Why two properties:**
- `Position` — towers need the target's world position for aim direction, projectile launch angle, and range checks.
- `IsAlive` — towers must skip dead enemies. Without this, a tower would lock onto a corpse that hasn't been removed from the detection list yet (pool return happens one frame late).

**EnemyController implements ITargetable via:**
```csharp
public Vector3 Position => transform.position;
public bool IsAlive => Health != null && Health.IsAlive;
```

The null check on `Health` guards the window between pool activation and `Initialize()`. Between `ObjectPoolManager.Get()` and `enemy.Initialize(data, path)`, the `Health` property is null. If `TowerDetection` runs an `OverlapSphere` during that window and tries to read `IsAlive`, the null check saves you from a `NullReferenceException`.

---

### File: `Assets/Scripts/Interfaces/ISelectable.cs`

**Already complete.** Walk only.

```csharp
namespace Interfaces
{
    public interface ISelectable
    {
        public void OnSelected();
        public void OnDeselected();
    }
}
```

**Where it's used:** Tower range indicators, enemy info panels, and the shop system. When the player clicks a tower or enemy, the selection system calls `OnSelected()`. Clicking elsewhere calls `OnDeselected()`. This decouples input handling from whatever visual/UI response the selected object needs.

---

### File: `Assets/Scripts/Interfaces/IUpdatable.cs`

**Already complete.** Walk only.

```csharp
namespace Interfaces
{
    public interface IUpdatable
    {
        public void Tick();
    }
}
```

**Where it's used:** `UpdateManager` manages tick-based updates with priority levels and interval throttling. Components implement `IUpdatable` and register with the manager instead of running their own `Update()`. This centralizes frame control and avoids the overhead of hundreds of `MonoBehaviour.Update` calls.

---

### File: `Assets/Scripts/Interfaces/IPoolable.cs`

**Already complete.** Walk only.

```csharp
namespace Interfaces
{
    public interface IPoolable
    {
        public void Reset();
    }
}
```

**Where it's used:** When an object is returned to the pool, `ObjectPoolManager` calls `IPoolable.Reset()`. ProjectileBase implements it — when reused, it clears its target reference and resets pierce counters. Any pooled object needs to clean up its runtime state before its next use.

---

### File: `Assets/Scripts/Interfaces/IHealthStrategy.cs`

**Implement now.** This replaces the old `HealthStrategy` abstract ScriptableObject.

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

    public readonly struct DamageResult
    {
        public readonly bool Died;
        public readonly float DamageDealt;

        private DamageResult(bool died, float damageDealt)
        {
            Died = died;
            DamageDealt = damageDealt;
        }

        public static DamageResult Alive(float damageDealt) =>
            new DamageResult(false, damageDealt);

        public static DamageResult Dead(float damageDealt) =>
            new DamageResult(true, damageDealt);
    }
}
```

**Method-by-method breakdown:**

| Member | Purpose |
|--------|---------|
| `Initialize()` | Sets `_currentHealth = _startHealth`. Called once when the enemy spawns. |
| `TakeDamage(float)` | Applies damage logic (flat, armoured, shield, etc.). Returns `DamageResult` so the controller can check `Died`. |
| `Tick(float deltaTime)` | Per-frame logic — only `RegenHealth` uses it. All other implementations leave it empty. |
| `IsAlive` | `CurrentHealth > 0f`. Controllers check this before applying damage or targeting. |
| `CurrentHealth` | Read-only access to live health value. Used for health bar UI. |
| `MaxHealth` | Returns starting health. Used for health bar fill calculation `(CurrentHealth / MaxHealth)`. |

**The DamageResult struct — why it exists:**

Old architecture: `HealthStrategy.TakeDamage` modified `CurrentHealth` and called `CheckForDeath()`, which invoked `OnDeath` event. The controller subscribed to `Health.OnDeath += Die`.

New architecture: `IHealthStrategy.TakeDamage` returns a `DamageResult`. The controller checks `result.Died` and calls its own `Die()`. No event subscription needed for the death flow.

Why the change:
- **Explicit control flow.** With events, death could fire inside `TakeDamage` mid-method, before the controller finishes its damage response. With `DamageResult`, the controller handles death *after* `TakeDamage` returns — deterministic order.
- **No subscription cleanup.** With events, you must unsubscribe in `Die()` or risk leaks. With `DamageResult`, there's nothing to unsubscribe.
- **Testable.** Unit tests can assert on the return value directly instead of setting up event listeners.

**DamageResult is a `readonly struct`:**
- No heap allocation. Returned on the stack.
- `readonly` means the compiler can optimize copies away.
- Static factory methods (`Alive`, `Dead`) prevent invalid combinations — you can't accidentally construct `DamageResult(false, 0)` when you meant "dead with 0 damage dealt". The static methods make intent clear.

---

### File: `Assets/Scripts/Interfaces/IMovementStrategy.cs`

**Implement now.** This replaces the old `MovementStrategy` abstract ScriptableObject.

```csharp
using System;
using Enemies.Controllers;

namespace Interfaces
{
    public interface IMovementStrategy
    {
        void Initialize(EnemyController enemy);
        void Tick(EnemyController enemy);
        event Action OnMovementCompleted;
    }
}
```

**Method-by-method breakdown:**

| Member | Purpose |
|--------|---------|
| `Initialize(EnemyController enemy)` | Sets start position, waypoint index to 0, stores path reference. Called once when the enemy spawns. |
| `Tick(EnemyController enemy)` | Moves toward current waypoint at the strategy's speed. Called every frame from `EnemyController.Update()`. |
| `OnMovementCompleted` | Fires when enemy reaches the last waypoint. Controller subscribes to handle "enemy reached end of path" logic. |

**Why `Initialize` and `Tick` take `EnemyController` as a parameter:**

Unlike `IHealthStrategy`, movement strategies need to:
- Read `enemy.Path` (the waypoint path reference)
- Read/write `enemy.CurrentWayPointIndex` (which waypoint is next)
- Write `enemy.transform.position` (move the enemy)

These are controller-owned properties. The strategy can't store a `Transform` reference internally because the same enemy's `Transform` is used by other systems. Passing `EnemyController` each frame keeps the strategy stateless with respect to the transform — it moves the enemy in place rather than caching and syncing position.

**Why `OnMovementCompleted` stays as an event (unlike `IHealthStrategy.TakeDamage` which returns `DamageResult`):**

The "enemy reached end of path" notification fires asynchronously — the enemy doesn't die immediately. The controller runs extra logic: damage the player, return the enemy to the pool, update wave state. This is an "announcement" pattern, not a request-response pattern. Events fit naturally here; `DamageResult`-style returns would add no value because the controller doesn't need to make a decision based on the result.

---

### File: `Assets/Scripts/Interfaces/ITargetingStrategy.cs`

**Implement now.** This will be used by towers in Episode 05.

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Interfaces
{
    public interface ITargetingStrategy
    {
        ITargetable GetTarget(IEnumerable<ITargetable> targets, Vector3 towerPosition);
    }
}
```

**Method-by-method breakdown:**

| Member | Purpose |
|--------|---------|
| `GetTarget(IEnumerable<ITargetable>, Vector3)` | Selects the best target from all valid targets in range. Tower position is provided for distance-based strategies. |

**Why `IEnumerable<ITargetable>` instead of `List<ITargetable>`:**
- The caller (`TowerDetection`) builds a `List<ITargetable>` from `OverlapSphere` results. The interface accepts `IEnumerable` so implementations can choose to iterate once (First strategy), sort (Closest strategy), or scan all (Strongest/Weakest strategy) without forcing a specific collection type on the strategy.

**Concrete strategies you'll build later:**
- `FirstTargeting` — picks the `ITargetable` with the highest waypoint index (furthest along the path)
- `ClosestTargeting` — picks the `ITargetable` nearest to `towerPosition`
- `StrongestTargeting` — picks the `ITargetable` with the highest `CurrentHealth`
- `WeakestTargeting` — picks the `ITargetable` with the lowest `CurrentHealth`

---

### Architecture Decision: Why Interfaces Instead of Abstract ScriptableObjects

This is the most important design decision in the project. Understanding *why* will save you from the shared-state bug and explain every subsequent file structure.

#### The Old Architecture (Abstract SO Strategies)

```
EnemyData (SO)
├── Health field → references a HealthStrategy SO asset (e.g., NormalHealth.asset)
└── Movement field → references a MovementStrategy SO asset (e.g., GroundedPath.asset)

EnemyController
├── Health → data.Health (same SO reference as every other enemy using NormalHealth.asset)
└── Movement → data.Movement (same SO reference as every other enemy using GroundedPath.asset)
```

**The shared-state bug:**

ScriptableObjects are asset instances. If 10 Basic enemies all reference `NormalHealth.asset`, they share the SAME object. When `NormalHealth` has a `CurrentHealth` field:

```
Enemy A.Initialize() → NormalHealth.asset.CurrentHealth = 100
Enemy B.Initialize() → NormalHealth.asset.CurrentHealth = 100  ← overwrites A's value!
Enemy A.TakeDamage(25) → NormalHealth.asset.CurrentHealth = 75  ← B also reads 75!
```

Both enemies read and write to the same `CurrentHealth`. Damage one enemy, and *all* enemies sharing that SO take damage.

**The old workaround: clone the SO at runtime.**

```csharp
Health = Instantiate(data.Health);   // clones the SO — each enemy gets a copy
Movement = Instantiate(data.Movement);
```

This works but has problems:
- `Instantiate()` allocates a full SO clone on the heap every time an enemy spawns
- The clone is a `UnityEngine.Object` — Unity tracks it, it adds overhead
- You must remember to clone. If you forget, the bug appears silently
- Cloning doesn't compose well — if a strategy holds references to other SOs, those aren't cloned

#### The New Architecture (Interfaces + Factory)

```
EnemyData (SO)
├── HealthConfig field → references a HealthConfig SO asset (pure data: startHealth=100)
└── MovementConfig field → references a MovementConfig SO asset (pure data: moveSpeed=5)

EnemyController
├── Health → StrategyFactory.CreateHealth(data.HealthConfig) → new NormalHealth(100)
└── Movement → StrategyFactory.CreateMovement(data.MovementConfig) → new GroundedPath(5)
```

**How this eliminates the bug:**

`StrategyFactory.CreateHealth(data.HealthConfig)` calls `new NormalHealth(config.StartHealth)`. This creates a **plain C# class instance** — not a SO clone, not a Unity object. Each enemy's `NormalHealth` is a separate object with its own `_currentHealth` field. No sharing, no cloning, no bug.

**The split: data stays in SOs, behavior lives in interfaces**

| Old | New | Why |
|-----|-----|-----|
| `NormalHealth` SO (data + behavior) | `HealthConfig` SO (data only) + `NormalHealth` class (behavior only) | SOs are for designer-editable config. Classes are for runtime logic. Mixing them caused the shared-state bug. |
| `ArmouredHealth` SO | `HealthConfig` SO with `type=Armoured` + `ArmouredHealth` class | Same config SO can represent any health type via the enum. The factory reads the enum and creates the right class. |
| `GroundedPath` SO | `MovementConfig` SO with `type=Grounded` + `GroundedPath` class | Movement config is just speed and height values. No reason for that to be a SO with behavior. |

**Config SOs are safe to share** because they're read-only at runtime. `HealthConfig.StartHealth` is never modified after creation. It's a constant. Ten enemies can safely reference the same `HealthConfig` asset because the factory reads it once during `CreateHealth()` and never touches it again.

---

### File: `Assets/Scripts/Enemies/Controllers/EnemyController.cs`

**Update** to use `IHealthStrategy` and `IMovementStrategy` via `StrategyFactory`. This file will be fully implemented in Episode 03, but the key changes to understand now:

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
        #region Fields

        [Header("Enemy UI")]
        [SerializeField] private EnemyHealthBar healthBar;

        #endregion

        #region Properties

        public EnemyPath Path { get; private set; }
        public int CurrentWayPointIndex { get; set; }
        public IHealthStrategy Health { get; private set; }
        public IMovementStrategy Movement { get; private set; }
        public int GoldGiven { get; private set; }
        public int Damage { get; private set; }

        public Vector3 Position => transform.position;
        public bool IsAlive => Health != null && Health.IsAlive;

        #endregion

        #region Class Methods

        public void Initialize(EnemyData data, EnemyPath path)
        {
            Path = path;
            Health = StrategyFactory.CreateHealth(data.HealthConfig);
            Movement = StrategyFactory.CreateMovement(data.MovementConfig);
            GoldGiven = data.GoldGiven;
            Damage = data.Damage;
            Health.Initialize();
            Movement.Initialize(this);
            Movement.OnMovementCompleted += OnReachedEnd;
        }

        public void Die()
        {
            Movement.OnMovementCompleted -= OnMovementCompleted;
            // Raise death event, return to pool — Episode 04
        }

        #endregion

        #region Unity Methods

        private void Update()
        {
            if (!IsAlive) return;
            Health.Tick(Time.deltaTime);
            Movement.Tick(this);
        }

        #endregion

        #region IDamageable

        public void TakeDamage(float damage)
        {
            var result = Health.TakeDamage(damage);
            if (result.Died)
                Die();
            // Update health bar: healthBar.UpdateFill(Health.CurrentHealth, Health.MaxHealth);
        }

        #endregion

        #region Private Methods

        private void OnReachedEnd()
        {
            // Enemy reached end — damage player, return to pool — Episode 04
        }

        #endregion
    }
}
```

**Key changes from the old architecture:**

1. **`Health` and `Movement` are now interface-typed** (`IHealthStrategy`, `IMovementStrategy`), not `HealthStrategy`/`MovementStrategy` SO references.

2. **`Initialize` creates strategies via `StrategyFactory`**, not by copying SO references:
   - Old: `Health = data.Health` (shared SO reference)
   - New: `Health = StrategyFactory.CreateHealth(data.HealthConfig)` (new instance per enemy)

3. **`TakeDamage` uses `DamageResult`** instead of relying on `OnDeath` event:
   - Old: `Health.TakeDamage(amount)` modified `CurrentHealth`, fired `OnDeath` internally
   - New: `var result = Health.TakeDamage(amount)` returns struct, controller checks `result.Died`

4. **`Die()` unsubscribes from `Movement.OnMovementCompleted`**. This cleanup was needed in the old architecture too, but the event was on the SO — now it's on the interface, same pattern.

5. **`Update` checks `IsAlive` before ticking.** A dead enemy shouldn't move or regen. This guard prevents the common bug where `Die()` is called but `Update` keeps running for one more frame.

---

## Test Plan

### Test 1: Verify Existing Interfaces Compile
1. Open `IDamageable.cs`, `ITargetable.cs`, `ISelectable.cs`, `IUpdatable.cs`, `IPoolable.cs`
2. Confirm no compile errors — these are unchanged

### Test 2: Verify New Interfaces Compile
1. Implement `IHealthStrategy.cs`, `IMovementStrategy.cs`, `ITargetingStrategy.cs` as shown above
2. Confirm no compile errors — the interfaces reference `EnemyController` (used by `IMovementStrategy`) which already exists

### Test 3: DamageResult Smoke Test
1. Write a temporary test in any MonoBehaviour's `Start`:
```csharp
var alive = DamageResult.Alive(25f);
Debug.Log($"Alive: Died={alive.Died}, DamageDealt={alive.DamageDealt}");

var dead = DamageResult.Dead(25f);
Debug.Log($"Dead: Died={dead.Died}, DamageDealt={dead.DamageDealt}");
```
2. Expected output:
```
Alive: Died=False, DamageDealt=25
Dead: Died=True, DamageDealt=25
```

### Test 4: Verify EnemyController Switch to Interfaces
1. `EnemyController.Health` is now `IHealthStrategy` — confirm the property type compiles
2. `EnemyController.Movement` is now `IMovementStrategy` — confirm the property type compiles
3. `EnemyController.Initialize` calls `StrategyFactory.CreateHealth` — this won't compile yet because `StrategyFactory` isn't implemented. That's Episode 03.

## Debugging Tips

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| `StrategyFactory` does not exist | Episode 03 hasn't been completed | Implement `StrategyFactory` in Episode 03 |
| `DamageResult` type not found | Forgot to define the struct alongside `IHealthStrategy` | DamageResult lives in the same file as IHealthStrategy.cs |
| `EnemyController` doesn't implement `ITargetable.IsAlive` correctly | `IsAlive` returns `CurrentHealth > 0` but `Health` is null before Initialize | Keep the `Health != null` null check in `IsAlive` |
| `OnMovementCompleted` never fires | No one subscribed to it | `EnemyController.Initialize` must subscribe `Movement.OnMovementCompleted += OnReachedEnd` |
| `TakeDamage` returns `DamageResult` but code treats it as `void` | Old pattern: `Health.TakeDamage(amount)` with no return value | Change to `var result = Health.TakeDamage(amount)` and check `result.Died` |
| Multiple enemies share health values | Still using SO references instead of Factory-created instances | Verify `Initialize` calls `StrategyFactory.CreateHealth` (not `data.Health`) |
