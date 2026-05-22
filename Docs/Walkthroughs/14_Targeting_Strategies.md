# Episode 14: Targeting Strategies

## What You're Building

Replace hardcoded "closest" targeting in `TowerDetection` with swappable `ITargetingStrategy` implementations. Four BTD6-style modes: First (most progress), Last (least progress), Strong (highest HP), Close (nearest distance). Towers can switch targeting at runtime.

## The Problem

`TowerDetection` hardcodes closest-distance sorting:

```csharp
foreach (Collider hit in hits)
{
    float dist = Vector3.Distance(transform.position, target.Position);
    if (dist < closestDist) { ... }
}
```

Adding "First" or "Strong" means editing TowerDetection for each new sort. Same Open/Closed violation we solved in Episodes 06/07 with movement and health strategies. Same fix: extract to strategy interface.

## ITargetable.cs (updated — add PathProgress and CurrentHealth)

These properties are only needed for targeting. They belong here, not earlier.

```csharp
using UnityEngine;

namespace Interfaces
{
    public interface ITargetable
    {
        Vector3 Position { get; }
        bool IsAlive { get; }
        float PathProgress { get; }
        float CurrentHealth { get; }
    }
}
```

**Why not add these in Episode 02?** Because they weren't needed until now. Adding them early would be a forward reference — the reader wouldn't understand why `PathProgress` exists on `ITargetable` until 11 episodes later. Adding them here is motivated: "First targeting needs to know progress, Strong targeting needs to know health."

## EnemyController.cs (add PathProgress and CurrentHealth)

```csharp
// ITargetable
public Vector3 Position => transform.position;
public bool IsAlive => Health != null && Health.IsAlive;
public float PathProgress => Path != null
    ? (float)CurrentWayPointIndex / Path.WaypointCount
    : 0f;
public float CurrentHealth => Health.CurrentHealth;
```

Two new one-line properties. `PathProgress` is a ratio: current waypoint / total waypoints. `CurrentHealth` delegates to the health strategy.

## ITargetingStrategy.cs

```csharp
using System.Collections.Generic;
using Interfaces;
using UnityEngine;

namespace Interfaces
{
    public interface ITargetingStrategy
    {
        ITargetable GetTarget(IEnumerable<ITargetable> targets, Vector3 towerPosition);
    }
}
```

## Four targeting strategies

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
            return targets
                .Where(t => t.IsAlive)
                .OrderByDescending(t => t.PathProgress)
                .FirstOrDefault();
        }
    }

    public class LastTargeting : ITargetingStrategy
    {
        public ITargetable GetTarget(IEnumerable<ITargetable> targets, Vector3 towerPosition)
        {
            return targets
                .Where(t => t.IsAlive)
                .OrderBy(t => t.PathProgress)
                .FirstOrDefault();
        }
    }

    public class StrongTargeting : ITargetingStrategy
    {
        public ITargetable GetTarget(IEnumerable<ITargetable> targets, Vector3 towerPosition)
        {
            return targets
                .Where(t => t.IsAlive)
                .OrderByDescending(t => t.CurrentHealth)
                .FirstOrDefault();
        }
    }

    public class CloseTargeting : ITargetingStrategy
    {
        public ITargetable GetTarget(IEnumerable<ITargetable> targets, Vector3 towerPosition)
        {
            return targets
                .Where(t => t.IsAlive)
                .OrderBy(t => Vector3.Distance(t.Position, towerPosition))
                .FirstOrDefault();
        }
    }
}
```

All four are **stateless** — no fields, no mutation. Safe to share singleton instances.

- "First": highest `PathProgress` — closest to end of path
- "Last": lowest `PathProgress` — just spawned
- "Strong": highest `CurrentHealth`
- "Close": smallest distance to tower

## TargetingProvider.cs

```csharp
using Interfaces;
using Strategies.Targeting;

namespace Systems.Parsing
{
    public enum TargetPriority
    {
        First,
        Last,
        Strong,
        Close
    }

    public static class TargetingProvider
    {
        public static readonly ITargetingStrategy First = new FirstTargeting();
        public static readonly ITargetingStrategy Last = new LastTargeting();
        public static readonly ITargetingStrategy Strong = new StrongTargeting();
        public static readonly ITargetingStrategy Close = new CloseTargeting();

        public static ITargetingStrategy GetStrategy(TargetPriority priority)
        {
            return priority switch
            {
                TargetPriority.First => First,
                TargetPriority.Last => Last,
                TargetPriority.Strong => Strong,
                TargetPriority.Close => Close,
                _ => First
            };
        }
    }
}
```

Since strategies are stateless, one instance each. No allocation, no factory overhead.

## TowerDetection.cs (refactored)

```csharp
using System.Collections.Generic;
using Interfaces;
using Systems.Parsing;
using UnityEngine;

namespace Towers
{
    public class TowerDetection : MonoBehaviour
    {
        [SerializeField] private float detectionRadius = 5f;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private TargetPriority targetPriority = TargetPriority.Close;

        private ITargetingStrategy _targetingStrategy;
        private List<ITargetable> _targetsInRange;
        private Collider[] _hitBuffer;

        public ITargetable CurrentTarget { get; private set; }
        public bool HasTarget => CurrentTarget != null && CurrentTarget.IsAlive;

        private void Awake()
        {
            _targetsInRange = new List<ITargetable>();
            _hitBuffer = new Collider[32];
            SetTargeting(targetPriority);
        }

        private void Update()
        {
            ScanForTargets();
        }

        private void ScanForTargets()
        {
            _targetsInRange.Clear();

            int count = Physics.OverlapSphereNonAlloc(
                transform.position, detectionRadius, _hitBuffer, enemyLayer);

            for (int i = 0; i < count; i++)
            {
                if (_hitBuffer[i].TryGetComponent<ITargetable>(out var target) && target.IsAlive)
                    _targetsInRange.Add(target);
            }

            SelectTarget();
        }

        public void SetTargeting(ITargetingStrategy strategy)
        {
            _targetingStrategy = strategy;
        }

        public void SetTargeting(TargetPriority priority)
        {
            _targetingStrategy = TargetingProvider.GetStrategy(priority);
        }

        private void SelectTarget()
        {
            CurrentTarget = _targetsInRange.Count > 0
                ? _targetingStrategy.GetTarget(_targetsInRange, transform.position)
                : null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            if (CurrentTarget != null && CurrentTarget.IsAlive)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, CurrentTarget.Position);
            }
        }
    }
}
```

**What changed from Episode 03:**

| Before | After |
|--------|-------|
| `OverlapSphere` (alloc) | `OverlapSphereNonAlloc` with 32-buffer |
| Inline closest-distance sort | `_targetingStrategy.GetTarget()` |
| No targeting mode | `TargetPriority` enum + `SetTargeting()` |
| Hardcoded "closest" | Configurable First/Last/Strong/Close |

`SetTargeting(TargetPriority)` is the runtime API — a UI button or key press can call it to switch targeting mid-game.

## Test Plan

1. **First**: 3 enemies at different progress → tower targets the one closest to end
2. **Last**: same 3 → tower targets the one furthest from end
3. **Strong**: armoured (150 HP) + basic (100 HP) → tower targets armoured
4. **Close**: enemies at varying distances → tower targets nearest
5. **Runtime switch**: Start with Close, call `SetTargeting(TargetPriority.First)` → target changes

## Debugging

| Symptom | Cause | Fix |
|---------|-------|-----|
| Always targets closest | `targetPriority` not changed from default | Set in Inspector or call SetTargeting |
| Target switches every frame | Multiple enemies at same priority | Normal for ties — add secondary sort if needed |
| LINQ allocations | `OrderBy` allocates | Acceptable for TD (small counts). Profile first. |
| `TryGetComponent<ITargetable>` fails | EnemyController not on same GO as Collider | Move both to root |
| `PathProgress` always 0 | `Path.WaypointCount` not set | Verify EnemyPath has waypoints |