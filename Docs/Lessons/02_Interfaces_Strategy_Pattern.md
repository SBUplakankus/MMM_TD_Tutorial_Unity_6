# Episode 02: Interfaces & Strategy Pattern

!!! info "Episode Type: Code Lesson"
    You'll create strategy interfaces and a result struct, then extend `EnemyController` with an existing interface implementation. ~15 min.

---

## Video

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden;">
  <iframe src="https://www.youtube.com/embed/EP02_PLACEHOLDER" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%;" frameborder="0" allowfullscreen></iframe>
</div>

**Duration:** ~15 min

---

## Learning Objectives

By the end of this episode you will be able to:

1. Explain why interfaces matter in game development — contracts without coupling
2. Create `IUpdatable` (for the custom update manager) and `IPoolable` (for object pooling)
3. Walk the existing `IDamageable` and `ITargetable` interfaces and understand their properties
4. Define `IHealthStrategy`, `IMovementStrategy`, and `ITargetingStrategy` interfaces for the Strategy pattern
5. Explain `DamageResult` — why `TakeDamage` returns a struct instead of void + event
6. Argue for "composition over inheritance" using the project's enemy system as evidence

---

## Key Concepts

| Concept | Definition | Why It Matters Here |
|---------|-----------|-------------------|
| Interface | A contract — method signatures with no implementation | Lets `TowerDetection` target *anything* that implements `ITargetable`, not just `EnemyController` |
| Strategy pattern | Behaviour encapsulated in separate objects, selected at runtime | `IHealthStrategy` and `IMovementStrategy` let you compose enemy types without subclassing |
| Composition over inheritance | Build objects from pluggable parts instead of deep hierarchies | No `ArmouredFlyingEnemy` subclass — just `ArmouredHealth` + `FlyingPath` created by `StrategyFactory` |
| Interface vs abstract SO | Interfaces define contracts; SOs hold data. Mixing both causes shared-state bugs | `CurrentHealth` on an abstract SO was shared across all enemies using that SO |

!!! tip "Deep dive on interfaces"
    Read [Interfaces](../Concepts/Interfaces.md) for the full rationale behind every interface in this project.

---

## Code Roadmap

| File | Action | Episode |
|------|--------|---------|
| `Interfaces/IUpdatable.cs` | **Create** | This episode |
| `Interfaces/IPoolable.cs` | **Create** | This episode |
| `Interfaces/IHealthStrategy.cs` | **Create** | This episode |
| `Interfaces/IMovementStrategy.cs` | **Create** | This episode |
| `Interfaces/ITargetingStrategy.cs` | **Create** | This episode |
| `Enemies/Controllers/EnemyController.cs` | **Add `ITargetable` implementation** | This episode |

---

## Architecture Context

Interfaces are the glue between decoupled systems:

```
┌──────────────────┐     ITargetable     ┌──────────────────┐
│  TowerDetection  │────────────────────▶│ EnemyController  │
└──────────────────┘                     └──────────────────┘

┌──────────────────┐     IDamageable     ┌──────────────────┐
│  ProjectileBase  │────────────────────▶│ EnemyController   │
└──────────────────┘                     └──────────────────┘

┌──────────────────┐     IUpdatable      ┌──────────────────┐
│  UpdateManager   │────────────────────▶│ Any Tick()-able   │
└──────────────────┘                     └──────────────────┘

┌──────────────────┐     IPoolable       ┌──────────────────┐
│ ObjectPoolManager│────────────────────▶│ ProjectileBase   │
│                  │                     │ (and any pooled) │
└──────────────────┘                     └──────────────────┘

┌──────────────────┐     IHealthStrategy  ┌──────────────────┐
│ EnemyController  │────────────────────▶│ NormalHealth     │
│                  │                     │ ArmouredHealth   │
│                  │                     │ ShieldHealth     │
│                  │                     │ RegenHealth      │
└──────────────────┘                     └──────────────────┘

┌──────────────────┐   IMovementStrategy  ┌──────────────────┐
│ EnemyController  │────────────────────▶│ GroundedPath     │
│                  │                     │ FlyingPath       │
└──────────────────┘                     └──────────────────┘
```

Each arrow means "depends on the **interface**, not the concrete class." `TowerDetection` never imports `EnemyController` — it only knows about `ITargetable`. `EnemyController` never imports `NormalHealth` — it only knows about `IHealthStrategy`.

---

## Step-by-Step Implementation

### Step 1: Introduction to interfaces in game dev

Interfaces define **what** an object can do without specifying **how**. In a tower defence game this is critical:

- A tower doesn't care *what kind* of enemy it's targeting — it just needs `ITargetable.Position` and `ITargetable.IsAlive`
- A projectile doesn't care *how* damage is calculated — it just calls `IDamageable.TakeDamage()`
- The update manager doesn't care *what* system is ticking — it just calls `IUpdatable.Tick()`

This means you can add new enemy types, new damageable objects (destructible scenery?), or new tickable systems **without changing any existing code**.

!!! warning "Interface vs abstract class"
    An abstract class provides shared implementation. An interface provides only the contract. Use interfaces when multiple *unrelated* classes need the same capability. Use abstract classes when related classes share code. See [Abstraction](../Concepts/Abstraction.md).

---

### Step 2: Walk existing `IDamageable` and `ITargetable`

**`IDamageable.cs`** — the simplest contract in the project:

```csharp
namespace Interfaces
{
    public interface IDamageable
    {
        public void TakeDamage(float damage);
    }
}
```

Any class that can take damage implements this. `EnemyController` does. Later, destructible scenery or shields could too — the projectile code wouldn't change.

**`ITargetable.cs`** — what a tower needs to aim:

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

- `Position` — where the enemy is right now. Used by `TowerDetection` for range checks and `ProjectileBase` for homing.
- `IsAlive` — towers and projectiles check this to drop dead targets.

---

### Step 3: Create `IUpdatable` interface

The [Update Manager](../Concepts/Update_Manager.md) needs a way to call `Tick()` on any registered system without knowing its concrete type. That's `IUpdatable`.

Create `Assets/Scripts/Interfaces/IUpdatable.cs`:

```csharp
namespace Interfaces
{
    public interface IUpdatable
    {
        public void Tick();
    }
}
```

!!! info "Why `Tick()` instead of `Update()`?"
    Unity's `Update()` is called on every `MonoBehaviour` that defines it — even disabled ones in some edge cases. Our `UpdateManager` calls `Tick()` only on registered `IUpdatable` objects, at priorities we control. This reduces CPU overhead. See [Update Manager](../Concepts/Update_Manager.md).

---

### Step 4: Create `IPoolable` interface

Object pooling (Episode 04) reuses objects instead of destroying them. When an object returns to the pool, it must **reset its state** so the next user gets a clean object. `IPoolable` defines that contract.

Create `Assets/Scripts/Interfaces/IPoolable.cs`:

```csharp
namespace Interfaces
{
    public interface IPoolable
    {
        public void Reset();
    }
}
```

!!! warning "Forgot to call `Reset()`?"
    If you return a pooled object without calling `Reset()`, the next enemy spawned from the pool might still have the previous enemy's health, waypoint index, and target. Debugging this is painful — the enemy appears fully functional but is in an invalid state. We'll cover this in depth in [Episode 04](04_Object_Pooling.md).

---

### Step 5: Create `IHealthStrategy` and `DamageResult`

The Strategy pattern for health is expressed through a **pure interface**, not an abstract ScriptableObject. Here's why:

!!! danger "The shared-state bug with abstract SOs"
    When `HealthStrategy` was an abstract `ScriptableObject`, its `CurrentHealth` field was **part of the SO asset itself**. If five enemies referenced the same `NormalHealth` SO, they all **shared one `CurrentHealth` value**. Enemy B taking damage reduced Enemy A's health. This is a fundamental problem: SOs are assets, not instances.

    The fix: separate **data** (config SOs) from **behaviour** (plain C# classes). `HealthConfig` SOs hold `startHealth`, `armourPercent`, etc. — values that *should* be shared. `IHealthStrategy` implementations hold `CurrentHealth` — state that must be *per-instance*.

Create `Assets/Scripts/Interfaces/IHealthStrategy.cs`:

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

    // TODO: Define the DamageResult readonly struct
    // Fields: Died (bool), DamageDealt (float)
    // Static helpers: Alive(float damageDealt), Dead(float damageDealt)
    public readonly struct DamageResult
    {
        // TODO: Define Died, DamageDealt fields
        // TODO: Define Alive() and Dead() factory methods
    }
}
```

**Why `DamageResult` instead of void + `OnDeath` event?**

| Approach | Problem |
|----------|---------|
| `void TakeDamage()` + `OnDeath` event | `EnemyController` must subscribe/unsubscribe. Event firing order is unpredictable. If multiple systems subscribe, who handles the death? |
| `DamageResult TakeDamage()` | The **caller** gets the result immediately. `EnemyController.TakeDamage()` checks `result.Died` and calls `Die()` — no subscription, no ordering ambiguity. |

The return-value approach is simpler, deterministic, and avoids the lifecycle complexity of events on pooled objects.

---

### Step 6: Create `IMovementStrategy`

The same interface-first principle applies to movement. `MovementStrategy` as an abstract SO had the same shared-state problem — `CurrentWayPointIndex` on the SO was shared between enemies.

Create `Assets/Scripts/Interfaces/IMovementStrategy.cs`:

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

- `Initialize(enemy)` — stores the enemy reference, reads its `Path`, sets waypoint index
- `Tick(enemy)` — moves the enemy toward the next waypoint each frame
- `OnMovementCompleted` — fires when the enemy reaches the last waypoint

Note: movement strategies take `EnemyController` as a parameter because they need access to `transform.position`, `Path`, and `CurrentWayPointIndex`. Health strategies don't — they manage their own state internally.

---

### Step 7: Create `ITargetingStrategy`

Targeting strategies decide **which** valid enemy to shoot. They're stateless — no per-tower data — so they can be shared instances.

Create `Assets/Scripts/Interfaces/ITargetingStrategy.cs`:

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

We'll implement concrete targeting classes (`FirstTargeting`, `LastTargeting`, `StrongTargeting`, `CloseTargeting`) in [Episode 05](05_Tower_Detection_Targeting.md).

---

### Step 8: Add `ITargetable` to `EnemyController`

`EnemyController` already implements `ITargetable` in the class declaration, but let's verify the properties are wired correctly:

```csharp
public class EnemyController : MonoBehaviour, IDamageable, ITargetable
{
    public IHealthStrategy Health { get; private set; }
    public IMovementStrategy Movement { get; private set; }

    public Vector3 Position => transform.position;
    public bool IsAlive => Health != null && Health.IsAlive;
```

- `Position` returns the transform's world position — exactly what `TowerDetection` needs for aiming
- `IsAlive` delegates to `Health.IsAlive` — the interface property, not a field on the controller. This means any health strategy (Normal, Armoured, Shield, Regen) is handled uniformly.

!!! tip "Why check `Health != null`?"
    When an enemy is spawned from the object pool, `Initialize()` hasn't been called yet. During that one frame, `Health` is null. The null check prevents `NullReferenceException` and ensures `TowerDetection` skips uninitialised enemies.

---

### Step 9: Composition over inheritance

The **inheritance approach** would look like:

```
Enemy
├── NormalEnemy
├── ArmouredEnemy
├── FlyingEnemy
└── ArmouredFlyingEnemy    ← combinatorial explosion
```

Every new trait doubles the subclass count. Adding a "Regen" trait? Now you need `NormalRegenEnemy`, `ArmouredRegenEnemy`, `FlyingRegenEnemy`, `ArmouredFlyingRegenEnemy`…

The **composition approach** used in this project:

```
EnemyController  ← one class, no subclasses
├── IHealthStrategy (created by StrategyFactory from HealthConfig)
│   ├── NormalHealth
│   ├── ArmouredHealth
│   ├── ShieldHealth
│   └── RegenHealth
└── IMovementStrategy (created by StrategyFactory from MovementConfig)
    ├── GroundedPath
    └── FlyingPath
```

Adding a "Regen + Flying" enemy? Create a `HealthConfig` with `type=Regen` and a `MovementConfig` with `type=Flying`. `StrategyFactory` builds the right classes. **Zero code changes.** The combinatorial explosion lives in config SOs, not in your class hierarchy.

---

## Episode Recap

- Created `IUpdatable` — contract for the custom update manager's `Tick()` calls
- Created `IPoolable` — contract for resetting pooled objects via `Reset()`
- Walked existing `IDamageable` and `ITargetable` interface contracts
- Created `IHealthStrategy` + `DamageResult` — health behaviour contract with deterministic death reporting
- Created `IMovementStrategy` — movement behaviour contract with `OnMovementCompleted` event
- Created `ITargetingStrategy` — targeting behaviour contract (stateless, shared instances)
- Understood why interfaces replace abstract SOs: shared-state bug when `CurrentHealth` lived on SO assets
- Verified `EnemyController` implements `ITargetable` with `Position` and `IsAlive`
- Saw why composition beats inheritance: new enemy types = new config SOs + factory cases, not new code

---

## Challenge

Design an `IDestroyable` interface that returns a **reward value** when the object is destroyed:

```csharp
namespace Interfaces
{
    public interface IDestroyable
    {
        // TODO: What members would this need?
    }
}
```

Consider:

1. Should it have a `Destroy()` method, or just a `RewardValue` property that other systems read?
2. How would `EnemyController` implement it? (Hint: it already has `GoldGiven`)
3. Would `ProjectileBase` need `IDestroyable`? Why or why not?
4. How does this differ from `IDamageable` — does an object need both?

Think about what systems would consume this interface and what events they'd raise.