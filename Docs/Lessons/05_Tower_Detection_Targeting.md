# Episode 05: Tower Detection & Priority Targeting

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden;">
  <iframe src="https://www.youtube.com/embed/EPISODE_05_PLACEHOLDER" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; border:0;" allowfullscreen></iframe>
</div>

## Learning Objectives

- Understand how towers detect enemies using physics overlaps and interface filtering
- Implement targeting strategies as plain C# classes implementing `ITargetingStrategy`
- Create four targeting modes: First, Last, Strong, and Close
- Add `PathProgress` to `ITargetable` for First/Last targeting
- Wire detection and targeting into the TowerController update loop

## Key Concepts

- **ITargetable filtering** — casting physics results to our interface to validate targets
- **TargetPriority enum** — First, Last, Strong, Close — maps to `ITargetingStrategy` instances
- **BTD6-style targeting** — players can switch targeting mode mid-game
- **ITargetingStrategy interface** — replaces abstract SO; targeting classes are stateless plain C# classes
- **Strategy Pattern** — behaviour encapsulated in objects, interchangeable without changing the host class

## Code Roadmap

| File | Role |
|------|------|
| `Interfaces/ITargetingStrategy.cs` | Interface — `GetTarget` method contract |
| `Interfaces/ITargetable.cs` | Existing — add `PathProgress` property |
| `TowerDetection.cs` | Scans for enemies in range, filters by `ITargetable`, delegates selection |
| `FirstTargeting.cs` | Concrete strategy — enemy closest to path end (highest `PathProgress`) |
| `LastTargeting.cs` | Concrete strategy — enemy closest to path start (lowest `PathProgress`) |
| `StrongTargeting.cs` | Concrete strategy — enemy with lowest `CurrentHealth` |
| `CloseTargeting.cs` | Concrete strategy — enemy closest to tower |
| `TowerController.cs` | Orchestrates detection → targeting → firing pipeline |

## Architecture Context

```
TowerController
  ├── TowerDetection (scans for enemies)
  │     └── ITargetingStrategy (selects best target)
  │           ├── FirstTargeting
  │           ├── LastTargeting
  │           ├── StrongTargeting
  │           └── CloseTargeting
  └── TowerFiring (shoots at selected target)
```

The `TowerController` owns the update loop. Each frame it asks `TowerDetection` to scan, which uses an `ITargetingStrategy` to pick the best valid target, then hands that target to `TowerFiring`.

!!! info "Why plain classes, not ScriptableObjects?"
    Targeting strategies are **stateless** — `FirstTargeting.GetTarget()` doesn't store any per-tower data. The sorting logic depends only on the `targets` and `towerPosition` parameters. Because there's no instance data, all towers can **share** the same `FirstTargeting` instance. ScriptableObjects would add unnecessary asset creation overhead for classes that hold zero serialized state.

## Step-by-Step Implementation Guide

### Step 1: The Detection Problem

Towers need to find valid enemies within their range every frame. We need:

- A fast physics query (`OverlapSphere`) to find colliders in range
- Interface filtering to confirm each result is `ITargetable`
- An alive check — dead or pooled enemies must be skipped
- A way to pick *which* valid target to shoot

Direct `FindObjectOfType` calls every frame would be catastrophic for performance. Physics overlaps constrained to a layer are the standard Unity approach.

### Step 2: Add `PathProgress` to `ITargetable`

For First/Last targeting, we need to know how far along the path each enemy is. Add a `PathProgress` property:

```csharp
using UnityEngine;

namespace Interfaces
{
    public interface ITargetable
    {
        public Vector3 Position { get; }
        public bool IsAlive { get; }

        // TODO: Add PathProgress — float representing how far along the path
        // Higher value = closer to path end. Used by First/Last targeting.
        public float PathProgress { get; }
    }
}
```

`EnemyController` implements this using waypoint index + fraction to next waypoint:

```csharp
// In EnemyController
public float PathProgress => CurrentWayPointIndex +
    // TODO: Calculate fraction toward next waypoint
    // Vector3.Distance(transform.position, Path.GetWaypointPosition(CurrentWayPointIndex)) /
    // Path.GetWaypointDistance(CurrentWayPointIndex)
```

!!! tip "Why not just waypoint index?"
    If two enemies are at waypoint index 3, which is "first"? The one closer to waypoint 4. `PathProgress` adds the fractional distance, giving a continuous progress value for sorting.

### Step 3: Walk the `ITargetingStrategy` Interface

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

No `TargetPriority` enum here — that belongs on `TowerDetection` as a serializable field the player can switch. The interface is agnostic about *why* a target is chosen; it just returns the best one.

### Step 4: Create a `TargetPriority` enum + strategy map

Instead of creating SO assets, we map an enum to strategy instances:

```csharp
public enum TargetPriority { First, Last, Strong, Close }
```

`TowerDetection` holds a `TargetPriority` field and resolves it to an `ITargetingStrategy`:

```csharp
// Option A: Dictionary on TowerDetection
// TODO: Map TargetPriority enum → ITargetingStrategy instance
// _strategyMap = new Dictionary<TargetPriority, ITargetingStrategy>
// {
//     { TargetPriority.First,  new FirstTargeting() },
//     { TargetPriority.Last,   new LastTargeting() },
//     { TargetPriority.Strong, new StrongTargeting() },
//     { TargetPriority.Close,  new CloseTargeting() }
// };

// Option B: Static provider class
// TODO: TargetingProvider.GetStrategy(TargetPriority priority) → ITargetingStrategy
```

Since targeting strategies are stateless, the same instance can be shared across all towers. No asset creation needed.

### Step 5: Implement Concrete Strategies

Each strategy is a plain C# class implementing `ITargetingStrategy`:

**FirstTargeting** — enemy closest to path end:

```csharp
using System.Collections.Generic;
using System.Linq;
using Interfaces;
using UnityEngine;

namespace Strategies.Targeting
{
    public class FirstTargeting : ITargetingStrategy
    {
        public ITargetable GetTarget(IEnumerable<ITargetable> targets, Vector3 towerPosition)
        {
            // TODO: Order by PathProgress descending
            // TODO: Return highest PathProgress (closest to path end)
            // TODO: Return null if targets is empty
        }
    }
}
```

**LastTargeting** — enemy closest to path start:

```csharp
public class LastTargeting : ITargetingStrategy
{
    public ITargetable GetTarget(IEnumerable<ITargetable> targets, Vector3 towerPosition)
    {
        // TODO: Order by PathProgress ascending
        // TODO: Return lowest PathProgress (closest to path start)
    }
}
```

**StrongTargeting** — enemy easiest to kill (lowest health):

```csharp
public class StrongTargeting : ITargetingStrategy
{
    public ITargetable GetTarget(IEnumerable<ITargetable> targets, Vector3 towerPosition)
    {
        // TODO: Order by CurrentHealth ascending (via IHealthStrategy)
        // NOTE: ITargetable doesn't expose CurrentHealth.
        // Option A: Add float Health to ITargetable
        // Option B: Cast to EnemyController (couples — avoid)
        // Option C: Add IHealthInfo interface with CurrentHealth
        // TODO: Choose approach and implement
    }
}
```

!!! warning "ITargetable doesn't expose health"
    `StrongTargeting` needs health information, but `ITargetable` only has `Position`, `IsAlive`, and `PathProgress`. Options:
    - Add `float HealthPercent { get; }` to `ITargetable` — keeps it interface-based but adds a member that only some consumers need
    - Create a separate `IVulnerable` interface with `CurrentHealth` / `MaxHealth` — follows ISP but requires a type check at the call site
    - For now, consider adding `HealthPercent` to `ITargetable` since health is fundamental to targeting decisions in TD games

**CloseTargeting** — enemy physically closest to tower:

```csharp
public class CloseTargeting : ITargetingStrategy
{
    public ITargetable GetTarget(IEnumerable<ITargetable> targets, Vector3 towerPosition)
    {
        // TODO: Order by Vector3.Distance(Position, towerPosition) ascending
        // TODO: Return closest enemy
    }
}
```

All four share the same structure — sort the `targets` collection by a different metric, return the first result.

### Step 6: Walk `TowerDetection`

```csharp
public class TowerDetection : MonoBehaviour
{
    [SerializeField] private float detectionRadius;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private TargetPriority priority;

    // TODO: ITargetingStrategy resolved from priority enum
    // TODO: Cache collider array for OverlapSphereNonAlloc
    // TODO: Track current target for persistence between frames

    public ITargetable ScanForTargets()
    {
        // TODO: OverlapSphere → filter ITargetable → filter IsAlive
        // TODO: Pass valid list to targetingStrategy.GetTarget(validTargets, transform.position)
    }

    // TODO: Method to swap TargetPriority at runtime (BTD6-style)
    public void SetPriority(TargetPriority newPriority)
    {
        // TODO: Update priority field, resolve new ITargetingStrategy
    }

    private void OnDrawGizmosSelected()
    {
        // TODO: Draw wire sphere at detectionRadius for scene debugging
    }
}
```

- `detectionRadius` — tunable per tower type
- `enemyLayer` — limits physics query to enemy layer only
- `priority` — enum the player can switch; resolves to `ITargetingStrategy` instance
- `ScanForTargets` — the single public method the controller calls

### Step 7: Walk `TowerController`

```csharp
public class TowerController : MonoBehaviour
{
    [SerializeField] private TowerDetection detection;
    [SerializeField] private TowerFiring firing;

    public bool IsActive { get; set; }

    private void Update()
    {
        if (!IsActive) return;

        var target = detection.ScanForTargets();
        if (target != null)
        {
            // TODO: Pass target to TowerFiring
        }
    }

    // TODO: Method to swap TargetingStrategy at runtime for BTD6-style switching
}
```

- `IsActive` — tower only scans/fires when active (placed and not selling)
- `Update` loop: scan → if target found → fire
- The controller never knows *how* targeting works — it just gets a target back

### Step 8: BTD6 Comparison

In Bloons TD 6, players tap a tower and cycle its targeting mode (First → Last → Strong → Close). We achieve the same by:

1. `TowerDetection` holds a `TargetPriority` enum
2. The enum resolves to a shared `ITargetingStrategy` instance (no SO creation)
3. `SetPriority(newPriority)` swaps the strategy in one line

No code changes needed in detection or firing — the strategy swap is a single enum change + instance lookup.

!!! info "BTD6 Targeting Modes"
    BTD6 also includes "Elite" targeting on some towers, which is a composite strategy. Our architecture supports this — you could create a `CompositeTargeting` class implementing `ITargetingStrategy` that delegates to sub-strategies with priority rules.

## Episode Recap

- Towers detect enemies via `OverlapSphere` + `ITargetable` filtering
- Added `PathProgress` to `ITargetable` for First/Last targeting
- Targeting strategies are **plain C# classes** implementing `ITargetingStrategy` — stateless, shared instances
- A `TargetPriority` enum maps to strategy instances (no SO asset creation needed)
- Four concrete strategies cover standard TD targeting: First, Last, Strong, Close
- `TowerController` orchestrates the pipeline: detect → select → fire
- BTD6-style mid-game targeting switches are trivial — just change the enum and resolve a new instance

## Challenge

Add a **"Smart"** targeting strategy that prioritizes enemies **closest to the path end** that are **also below 30% health**. Consider:

- Could you compose this from existing `FirstTargeting` and `StrongTargeting` logic?
- Could you build a `CompositeTargeting` class that applies one strategy then filters by a second condition?
- Since `ITargetingStrategy` is a plain interface, can you implement a class that wraps two strategies?
- What's the cleanest way to reuse existing sorting logic without duplicating code?