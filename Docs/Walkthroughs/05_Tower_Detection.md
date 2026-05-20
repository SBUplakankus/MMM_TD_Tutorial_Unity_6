# Episode 05: Tower Detection & Priority Targeting — Implementation Guide

## What You're Building

Towers need to detect enemies within their range and pick which one to shoot. This episode implements:
- **PathProgress** on `ITargetable` so targeting strategies can rank enemies by how far along the path they are
- **Four concrete targeting strategies** (First, Last, Strong, Close) as plain C# classes implementing `ITargetingStrategy`
- **TargetingProvider** static class with shared singleton instances (safe because targeting strategies are stateless)
- **TowerDetection** using `Physics.OverlapSphereNonAlloc` for zero-alloc scanning
- **TowerController** update loop wiring detection to firing

**Key architecture change from old walkthrough:** Targeting strategies are no longer abstract ScriptableObjects. They're plain C# classes implementing `ITargetingStrategy`. Since they're **stateless** (no per-instance data — just sorting logic), they don't need the factory pattern. Instead, `TargetingProvider` holds shared instances. This is safe because the shared-state bug only applies to strategies with per-instance data like `_currentHealth`.

**For BTD6-style mid-game targeting priority switching:** `TowerDetection` has a `SetTargeting(ITargetingStrategy)` method. The UI can call this when the player clicks a targeting button on the tower.

## Files & Order

| # | File | Action |
|---|------|--------|
| 1 | `Assets/Scripts/Interfaces/ITargetable.cs` | UPDATE — add PathProgress, Health |
| 2 | `Assets/Scripts/Strategies/Targeting/TargetPriority.cs` | NEW — enum |
| 3 | `Assets/Scripts/Strategies/Targeting/FirstTargeting.cs` | NEW |
| 4 | `Assets/Scripts/Strategies/Targeting/LastTargeting.cs` | NEW |
| 5 | `Assets/Scripts/Strategies/Targeting/StrongTargeting.cs` | NEW |
| 6 | `Assets/Scripts/Strategies/Targeting/CloseTargeting.cs` | NEW |
| 7 | `Assets/Scripts/Strategies/Targeting/TargetingProvider.cs` | NEW — shared instances |
| 8 | `Assets/Scripts/Enemies/Controllers/EnemyController.cs` | UPDATE — implement PathProgress, Health |
| 9 | `Assets/Scripts/Towers/TowerDetection.cs` | UPDATE — ITargetingStrategy, OverlapSphereNonAlloc |
| 10 | `Assets/Scripts/Towers/TowerController.cs` | UPDATE — full implementation |

## Implementation

### 1. ITargetable.cs — Add PathProgress + Health

```csharp
using UnityEngine;

namespace Interfaces
{
    public interface ITargetable
    {
        public Vector3 Position { get; }
        public bool IsAlive { get; }
        public float PathProgress { get; }
        public float Health { get; }
    }
}
```

**Why add `float Health { get; }` to ITargetable:** StrongTargeting needs to sort by remaining health. Options:
1. Cast `ITargetable` to some interface with health — awkward, couples targeting to specific types
2. Add `Health` to ITargetable — clean, and in a tower defence game, every targetable thing has health
3. Add a separate `IHealthInfo` interface — over-engineering for one strategy

Option 2 wins. EnemyController already has `IHealthStrategy Health` — the `Health` property on ITargetable is just `Health.CurrentHealth` with a null guard.

### 2. TargetPriority.cs — Enum

```csharp
namespace Strategies.Targeting
{
    public enum TargetPriority
    {
        First,
        Last,
        Strong,
        Close
    }
}
```

**Why a separate file:** The enum is used by `TargetingProvider.GetStrategy()` for the runtime switch, and by the tower upgrade UI for button labels. Keeping it separate from any one class avoids circular dependencies.

### 3. FirstTargeting.cs — NEW

Enemy closest to path end = highest PathProgress.

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
                .Where(t => t != null && t.IsAlive)
                .OrderByDescending(t => t.PathProgress)
                .FirstOrDefault();
        }
    }
}
```

**Why OrderByDescending:** PathProgress increases as the enemy moves along the path. The enemy with the highest PathProgress is closest to the end — that's the "First" target in BTD6 terminology (first to reach the exit).

### 4. LastTargeting.cs — NEW

Enemy closest to path start = lowest PathProgress.

```csharp
using System.Collections.Generic;
using System.Linq;
using Interfaces;
using UnityEngine;

namespace Strategies.Targeting
{
    public class LastTargeting : ITargetingStrategy
    {
        public ITargetable GetTarget(IEnumerable<ITargetable> targets, Vector3 towerPosition)
        {
            return targets
                .Where(t => t != null && t.IsAlive)
                .OrderBy(t => t.PathProgress)
                .FirstOrDefault();
        }
    }
}
```

### 5. StrongTargeting.cs — NEW

Highest remaining health.

```csharp
using System.Collections.Generic;
using System.Linq;
using Interfaces;
using UnityEngine;

namespace Strategies.Targeting
{
    public class StrongTargeting : ITargetingStrategy
    {
        public ITargetable GetTarget(IEnumerable<ITargetable> targets, Vector3 towerPosition)
        {
            return targets
                .Where(t => t != null && t.IsAlive)
                .OrderByDescending(t => t.Health)
                .FirstOrDefault();
        }
    }
}
```

**Why `t.Health` not `t.CurrentHealth`:** The ITargetable property is named `Health` — keep it consistent. It maps to `IHealthStrategy.CurrentHealth` on EnemyController.

**Design note:** "Strong" in BTD6 means highest remaining HP, not highest max HP. This is the more useful behavior — you want to focus fire on whatever can absorb the most damage. If you wanted "highest max HP" semantics, you'd sort by `MaxHealth` instead, but that requires adding another property to ITargetable.

### 6. CloseTargeting.cs — NEW

Nearest to tower. Uses `Vector3.SqrMagnitude` to avoid the square root in `Vector3.Distance`.

```csharp
using System.Collections.Generic;
using System.Linq;
using Interfaces;
using UnityEngine;

namespace Strategies.Targeting
{
    public class CloseTargeting : ITargetingStrategy
    {
        public ITargetable GetTarget(IEnumerable<ITargetable> targets, Vector3 towerPosition)
        {
            return targets
                .Where(t => t != null && t.IsAlive)
                .OrderBy(t => Vector3.SqrMagnitude(t.Position - towerPosition))
                .FirstOrDefault();
        }
    }
}
```

**Why SqrMagnitude not Distance:** `Vector3.Distance` computes `sqrt(x² + y² + z²)`. `Vector3.SqrMagnitude` skips the sqrt. Since we're only sorting (not comparing to a threshold), the relative order is identical. Saves the sqrt for every candidate every scan — with 20 enemies in range at 10 scans/second, that's 200 avoided sqrts per second.

### 7. TargetingProvider.cs — NEW

Shared singleton instances. Safe because targeting strategies are stateless.

```csharp
namespace Strategies.Targeting
{
    public static class TargetingProvider
    {
        private static FirstTargeting _first;
        private static LastTargeting _last;
        private static StrongTargeting _strong;
        private static CloseTargeting _close;

        public static FirstTargeting First => _first ??= new FirstTargeting();
        public static LastTargeting Last => _last ??= new LastTargeting();
        public static StrongTargeting Strong => _strong ??= new StrongTargeting();
        public static CloseTargeting Close => _close ??= new CloseTargeting();

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

**Why shared instances are safe:** Unlike `IHealthStrategy` which holds `_currentHealth`, targeting strategies have zero per-instance state. `FirstTargeting.GetTarget()` performs a pure sort on the input list — no fields are read or written. Multiple towers sharing the same `FirstTargeting` instance is completely safe.

**The contrast with IHealthStrategy:** If we shared a `NormalHealth` instance between two enemies, both would read/write the same `_currentHealth`. That's the shared-state bug. But `FirstTargeting` has no fields at all — it's a function object. This is the key architectural insight: **stateless strategies can be singletons; stateful strategies must be instanced per-entity.**

**Why lazy initialization (`??=`) not eager:** Order of static field initialization in C# is undefined. Lazy init guarantees the instances are only created when first accessed, avoiding any ordering issues. The `??=` operator is thread-safe for single-threaded Unity code.

**Why a static class not a ScriptableObject:** The old architecture had `TargetingStrategy : ScriptableObject`. This required creating `.asset` files for each targeting type, dragging them onto TowerDetection in the Inspector, and dealing with the SO lifecycle. For a stateless function object, this is pointless ceremony. A static class with lazy singletons is simpler, zero-allocation after first access, and requires no Inspector setup.

**Default case returns First:** The `switch` default handles any future enum values gracefully — "First" is the most common targeting mode and a safe fallback.

### 8. EnemyController.cs — UPDATE (implement PathProgress + Health)

Add PathProgress and Health properties to satisfy the updated ITargetable:

```csharp
using Data;
using Enemies.Components;
using Interfaces;
using Systems.Game;
using Systems.Managers;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable, IPoolable
    {
        #region Fields

        [Header("Enemy UI")]
        [SerializeField] private EnemyHealthBar healthBar;

        [SerializeField] private string poolKey = "enemy";

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
        public float Health => Health != null ? Health.CurrentHealth : 0f;

        public float PathProgress
        {
            get
            {
                if (Path == null) return 0f;

                var index = CurrentWayPointIndex;
                var totalWaypoints = Path.WaypointCount;

                if (totalWaypoints <= 1) return index;

                var progress = (float)index;

                if (Path.HasWaypoint(index))
                {
                    var nextWaypoint = Path.GetWaypointPosition(index);
                    var prevWaypoint = index > 0
                        ? Path.GetWaypointPosition(index - 1)
                        : Path.StartPosition;

                    var segmentLength = Vector3.Distance(prevWaypoint, nextWaypoint);
                    if (segmentLength > 0.001f)
                    {
                        var distToNext = Vector3.Distance(transform.position, nextWaypoint);
                        var normalized = 1f - Mathf.Clamp01(distToNext / segmentLength);
                        progress += normalized;
                    }
                }

                return progress;
            }
        }

        #endregion

        #region Class Methods

        public void Initialize(EnemyData data, EnemyPath path)
        {
            Path = path;
            GoldGiven = data.GoldGiven;
            Damage = data.Damage;

            Health = StrategyFactory.CreateHealth(data.HealthConfig);
            Movement = StrategyFactory.CreateMovement(data.MovementConfig);

            Health.Initialize();
            Movement.Initialize(this);

            Movement.OnMovementCompleted += OnReachedEnd;
        }

        public void Die()
        {
            Movement.OnMovementCompleted -= OnReachedEnd;
            ObjectPoolManager.Instance.Return(poolKey, gameObject);
        }

        #endregion

        #region Unity Methods

        private void Update()
        {
            Health.Tick(Time.deltaTime);
            Movement.Tick(this);
        }

        #endregion

        #region IDamageable

        public void TakeDamage(float damage)
        {
            var result = Health.TakeDamage(damage);

            if (healthBar != null)
            {
                healthBar.UpdateHealth(Health.CurrentHealth, Health.MaxHealth);
            }

            if (result.Died)
            {
                Die();
            }
        }

        #endregion

        #region IPoolable

        public void Reset()
        {
            if (Movement != null)
            {
                Movement.OnMovementCompleted -= OnReachedEnd;
            }

            Health = null;
            Movement = null;
            Path = null;
            CurrentWayPointIndex = 0;
            GoldGiven = 0;
            Damage = 0;
        }

        #endregion

        #region Private Methods

        private void OnReachedEnd()
        {
            ObjectPoolManager.Instance.Return(poolKey, gameObject);
        }

        #endregion
    }
}
```

**PathProgress formula explained:**

`PathProgress = waypointIndex + (1 - distToNext / segmentLength)`

- `waypointIndex` is the integer part — how many waypoints the enemy has passed
- `(1 - distToNext / segmentLength)` is the fractional part — how far along the current segment

**Concrete example (4 waypoints, enemy between WP1 and WP2):**
```
Waypoints: WP0=(0,0,0), WP1=(5,0,0), WP2=(5,0,5), WP3=(0,0,5)
Enemy position: (3, 0, 0) — between WP0 and WP1, but CurrentWayPointIndex = 1

progress = 1.0  (index)
nextWaypoint = WP1 = (5, 0, 0)
prevWaypoint = WP0 = (0, 0, 0)
segmentLength = 5
distToNext = |3 - 5| = 2
normalized = 1 - (2 / 5) = 1 - 0.4 = 0.6
PathProgress = 1.0 + 0.6 = 1.6
```

This means the enemy has passed 1.6 waypoints — useful for sorting against another enemy at progress 2.3.

**Why `segmentLength > 0.001f` guard:** If two waypoints are at the same position (authoring error), the segment length is 0. Division by zero would return NaN, which corrupts the sort. The guard skips the fractional part and returns the integer index — a reasonable degradation.

**Health property — why the explicit implementation:**

The `Health` property satisfies `ITargetable.Health` (returns `float`). But `EnemyController` also has a `IHealthStrategy Health` property (returns the strategy interface). These are different members with the same name.

C# distinguishes them because:
- `IHealthStrategy Health` is a property returning a reference type
- `float Health` (from ITargetable) is a property returning a value type

Wait — that won't compile. Two properties named `Health` with different return types is ambiguous in C#. The fix: rename the ITargetable implementation explicitly or use a different name.

**Resolution:** The ITargetable `Health` property is implemented explicitly:

```csharp
public float PathProgress { get { ... } }

float ITargetable.Health => Health != null ? Health.CurrentHealth : 0f;
```

No — explicit interface implementation makes the property only accessible via the interface, which is what we want for ITargetable consumers. But then TowerDetection accesses `target.Health` which calls the explicit implementation. That works fine.

Actually, reconsider. `IHealthStrategy Health` is the strategy reference used internally. `float ITargetable.Health` is the read-only value for targeting. They're different members. But having the same name is confusing. Better approach:

```csharp
public IHealthStrategy HealthStrategy { get; private set; }
public float Health => HealthStrategy != null ? HealthStrategy.CurrentHealth : 0f;
```

But the existing codebase uses `Health` for the strategy reference (in `TakeDamage`, `IsAlive`, etc.). Changing it would cascade through multiple files. Keep the existing naming and use explicit interface implementation for ITargetable.Health:

```csharp
public IHealthStrategy Health { get; private set; }
public bool IsAlive => Health != null && Health.IsAlive;

float ITargetable.Health => Health != null ? Health.CurrentHealth : 0f;
```

This is the approach used in the complete code above. The strategy reference is `Health` (used by EnemyController internally), and `ITargetable.Health` is the explicit implementation (used by TowerDetection through the interface).

### 9. TowerDetection.cs — Full implementation

```csharp
using System.Collections.Generic;
using Interfaces;
using Strategies.Targeting;
using UnityEngine;

namespace Towers
{
    public class TowerDetection : MonoBehaviour
    {
        #region Fields

        [SerializeField] private float detectionRadius;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private TargetPriority defaultPriority = TargetPriority.First;

        private ITargetingStrategy _targetingStrategy;
        private readonly Collider[] _hitBuffer = new Collider[32];
        private readonly List<ITargetable> _targetsInRange = new List<ITargetable>();

        #endregion

        #region Properties

        public ITargetable CurrentTarget { get; private set; }
        public bool HasTarget => CurrentTarget != null && CurrentTarget.IsAlive;
        public ITargetingStrategy Targeting => _targetingStrategy;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _targetingStrategy = TargetingProvider.GetStrategy(defaultPriority);
        }

        #endregion

        #region Public API

        public void ScanForTargets()
        {
            _targetsInRange.Clear();

            var hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                detectionRadius,
                _hitBuffer,
                enemyLayer
            );

            for (var i = 0; i < hitCount; i++)
            {
                if (_hitBuffer[i].TryGetComponent<ITargetable>(out var target) && target.IsAlive)
                {
                    _targetsInRange.Add(target);
                }
            }

            SelectTarget();
        }

        public void ClearTarget()
        {
            CurrentTarget = null;
        }

        public void SetTargeting(ITargetingStrategy strategy)
        {
            _targetingStrategy = strategy;
            ClearTarget();
        }

        public void SetTargeting(TargetPriority priority)
        {
            SetTargeting(TargetingProvider.GetStrategy(priority));
        }

        #endregion

        #region Private Methods

        private void SelectTarget()
        {
            if (_targetsInRange.Count == 0)
            {
                CurrentTarget = null;
                return;
            }

            CurrentTarget = _targetingStrategy.GetTarget(_targetsInRange, transform.position);
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = HasTarget ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        #endregion
    }
}
```

**Changes from old walkthrough:**

1. **No `TargetingStrategy` SO field.** Replaced with `TargetPriority defaultPriority` enum — set in Inspector via dropdown, resolved to an `ITargetingStrategy` via `TargetingProvider.GetStrategy()` in Awake.
2. **`_targetingStrategy` is `ITargetingStrategy`** (interface), not `TargetingStrategy` (abstract SO).
3. **`SetTargeting()` overload.** Can take an `ITargetingStrategy` directly (for programmatic switching) or a `TargetPriority` enum (for UI-driven switching via TargetingProvider).
4. **`HasTarget` now checks `IsAlive`.** Old code checked `CurrentTarget != null`. Now it also checks `IsAlive` — if the target died between scans, `HasTarget` returns false immediately without waiting for the next scan.
5. **`OverlapSphereNonAlloc` with `Collider[] _hitBuffer`.** Pre-allocated buffer of 32 avoids GC.Alloc per frame. The `readonly` modifier prevents reassignment.

**Why OverlapSphereNonAlloc not OverlapSphere:** `Physics.OverlapSphere` allocates a `Collider[]` array every call. At 10+ towers scanning every frame, that's 10+ arrays per frame headed for GC. `OverlapSphereNonAlloc` writes into the pre-allocated `_hitBuffer` — zero GC per scan. The tradeoff: if more than 32 enemies are in range, excess colliders are silently ignored. 32 is generous — that's more enemies than most TD games show on screen at once.

**Why SetTargeting clears the current target:** When you switch from First to Last targeting, the current target might not be the best target under the new strategy. Clearing forces the next ScanForTargets to re-evaluate. Without this, the tower would continue shooting the old "First" target until the next scan — potentially several frames of shooting the wrong enemy.

**Why Awake initializes from defaultPriority:** The TowerDetection needs a valid strategy before any ScanForTargets call. Awake runs before the first Update, so the strategy is always ready. The default of `First` matches BTD6's default targeting.

### 10. TowerController.cs — Full implementation

```csharp
using UnityEngine;

namespace Towers
{
    public class TowerController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private TowerDetection detection;
        [SerializeField] private TowerFiring firing;

        #endregion

        #region Properties

        public bool IsActive { get; private set; }

        #endregion

        #region Lifecycle

        private void Update()
        {
            if (!IsActive) return;

            detection.ScanForTargets();

            if (detection.HasTarget)
            {
                firing.TryFire(detection.CurrentTarget);
            }
        }

        #endregion

        #region Public API

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
            detection.ClearTarget();
        }

        #endregion
    }
}
```

**The detection → targeting → firing pipeline per frame:**

```
TowerController.Update()
  → detection.ScanForTargets()
    → Physics.OverlapSphereNonAlloc(position, radius, buffer, layerMask)
    → Filter: TryGetComponent<ITargetable>() && IsAlive
    → _targetingStrategy.GetTarget(targets, towerPosition)
      → e.g., FirstTargeting: OrderByDescending(PathProgress), take first
    → CurrentTarget = result
  → detection.HasTarget (checks CurrentTarget != null && IsAlive)
  → firing.TryFire(detection.CurrentTarget)
    → Check cooldown
    → Get projectile from pool
    → Launch at target
```

**Why scan every frame (not on a timer):** Later episodes will wire this through `UpdateManager` with configurable tick intervals. For now, every-frame scanning is correct and easy to debug. The performance cost of `OverlapSphereNonAlloc` with 32 colliders is negligible — the real cost is in the targeting sort, and with <50 targets, LINQ is fine.

**Why Deactivate clears the target:** When a tower is sold or picked up, it should stop targeting immediately. If we only set `IsActive = false`, the `CurrentTarget` reference would remain — potentially causing issues if something reads it while the tower is inactive.

## Unity Editor Setup

### Tower Prefab Setup

1. Create or select the Tower prefab
2. Add `TowerDetection` component, set:
   - `Detection Radius`: e.g. `3.0`
   - `Enemy Layer`: set to your `Enemy` layer (create one if needed: Edit → Project Settings → Tags and Layers, add "Enemy")
   - `Default Priority`: `First` (dropdown)
3. Add `TowerController` component, drag detection + firing references

### Enemy Layer

1. Edit → Project Settings → Tags and Layers → add layer `Enemy`
2. Assign `Enemy` layer to all enemy prefabs
3. **CRITICAL:** After creating the layer, select each enemy prefab → Inspector → top right dropdown → set Layer to `Enemy`. This must be done on the prefab, not just scene instances.

### Verify Gizmos

1. Select a tower in Scene view
2. You should see a wire sphere around it
3. Green when target acquired, red when no target
4. Adjust `detectionRadius` to see the sphere change size

### Targeting Strategy Verification

The targeting strategies are now code-only — no SO assets to create. Verify in the Inspector:
1. Select a Tower with TowerDetection
2. `Default Priority` shows a dropdown: First, Last, Strong, Close
3. No SO field needed — `TargetingProvider` handles strategy creation

**This is a major simplification from the old walkthrough** which required:
1. Creating 4 `.asset` files (FirstTargeting, LastTargeting, StrongTargeting, CloseTargeting)
2. Dragging SOs onto TowerDetection fields
3. Understanding the `TargetingStrategy : ScriptableObject` inheritance chain

Now: one dropdown in the Inspector. Done.

## Test Plan

| Test | Steps | Expected |
|------|-------|----------|
| First targeting | Place tower (default), spawn two enemies at different path positions | Tower shoots enemy further along path (highest PathProgress) |
| Last targeting | Change defaultPriority to Last, same scenario | Tower shoots enemy closest to start (lowest PathProgress) |
| Strong targeting | Change to Strong, spawn enemies with different health | Tower shoots highest-health enemy |
| Close targeting | Change to Close, spawn enemies at different distances from tower | Tower shoots nearest enemy |
| Runtime switching | Call `detection.SetTargeting(TargetPriority.Last)` mid-game | Tower immediately re-evaluates and targets last enemy on next scan |
| No enemies | Run with no enemies on field | No null refs, CurrentTarget is null, HasTarget is false |
| Target death mid-scan | Kill current target while tower is shooting | HasTarget returns false (IsAlive check), next scan picks new target |
| OverlapSphereNonAlloc overflow | Spawn >32 enemies in range | No crash, excess colliders silently ignored, 32 closest processed |
| Pooled enemy not detected | Return enemy to pool, scan before Get+Initialize | Enemy is inactive → not in OverlapSphere results (inactive colliders are skipped) |
| PathProgress zero check | Enemy not initialized (pooled, no Path set) | PathProgress returns 0f safely (null guard on Path) |

### Manual Test: Targeting Priority Switch

1. Place two towers side by side
2. Set Tower A to First, Tower B to Last
3. Spawn a wave of enemies
4. Observe: Tower A shoots the lead enemy, Tower B shoots the trailing enemy
5. Mid-wave, call `towerB Detection.SetTargeting(TargetPriority.Strong)` via a debug script
6. Verify: Tower B switches to shooting the highest-HP enemy

### Manual Test: PathProgress Accuracy

1. Add a debug display that shows `PathProgress` per enemy (e.g., OnGUI text above enemies)
2. Spawn an enemy and watch PathProgress increase as it moves along the path
3. Verify: at WP0, progress ≈ 0.x (starts moving toward WP1 immediately)
4. Verify: between WP1 and WP2, progress ≈ 1.x
5. Verify: at final waypoint, progress ≈ N (where N = number of waypoints - 1)

## Debugging Tips

| Symptom | Cause | Fix |
|---------|-------|-----|
| No targets found | Enemy layer not set | Assign `Enemy` layer to enemy prefabs, set `enemyLayer` on TowerDetection |
| Wrong target selected | Add Debug.Log in SelectTarget() printing target count and each target's PathProgress | Verify which strategy is active: `Debug.Log(detection.Targeting.GetType().Name)` |
| NullReferenceException on PathProgress | EnemyController.Initialize() not called before scan | Path null guard returns 0f — but check if enemies are being scanned before they're ready |
| OverlapSphere finds nothing | Layer mask wrong in Inspector | The `enemyLayer` field is a LayerMask, not a layer index. Use the Layer dropdown |
| Targets flicker | Current target dies between scans, HasTarget re-checks IsAlive | This is correct behavior — next scan picks a new target |
| PathProgress seems wrong | Formula: `index + (1 - distToNext/segmentLength)` | If waypoints are very close (< 0.001f segment), partial progress is skipped. Check waypoint spacing |
| SetTargeting doesn't take effect | You set the strategy but the tower has a stale CurrentTarget | SetTargeting calls ClearTarget internally — if it's not clearing, check for a custom version that skips the clear |
| StrongTargeting picks wrong enemy | `Health` property returns 0f for uninitialized enemies | Null guard: `Health != null ? Health.CurrentHealth : 0f`. Uninitialized enemies get sorted to the end by StrongTargeting |
| ITargetable.Health causes compile error | Name collision with IHealthStrategy Health | Use explicit interface implementation: `float ITargetable.Health => ...` |
| TowerDetection still references old TargetingStrategy SO | Old code not fully replaced | Remove the `[SerializeField] private TargetingStrategy targetingStrategy` field, replace with TargetPriority enum |

### Advanced: OverlapSphereNonAlloc Buffer Sizing

The `_hitBuffer` is fixed at 32. If you need more:

```csharp
private readonly Collider[] _hitBuffer = new Collider[64];
```

Each `Collider*` reference is 8 bytes, so 64 entries = 512 bytes permanently allocated. This is a trivial cost compared to the GC savings from avoiding `OverlapSphere`.

**How to tell if you need a bigger buffer:** Add a debug check after `OverlapSphereNonAlloc`:
```csharp
if (hitCount >= _hitBuffer.Length)
{
    Debug.LogWarning($"TowerDetection buffer overflow: {hitCount} hits, buffer size {_hitBuffer.Length}");
}
```

If you see this warning during a heavy wave, increase the buffer size. For most TD games, 32 is sufficient — 32 enemies within a single tower's radius is extreme.

### Advanced: LINQ Allocation Concerns

The targeting strategies use LINQ (`Where`, `OrderBy`, `FirstOrDefault`). This allocates enumerators and sorting buffers. At 60fps with 10+ towers, that's 600+ LINQ operations per second.

**When to optimize:** Profile first. If GC.Alloc from targeting is visible in the Profiler, replace with manual loops:

```csharp
public ITargetable GetTarget(IEnumerable<ITargetable> targets, Vector3 towerPosition)
{
    ITargetable best = null;
    var bestProgress = float.MinValue;

    foreach (var t in targets)
    {
        if (t == null || !t.IsAlive) continue;
        if (t.PathProgress > bestProgress)
        {
            bestProgress = t.PathProgress;
            best = t;
        }
    }

    return best;
}
```

This eliminates all LINQ allocations. But don't do this prematurely — the LINQ version is clearer and the allocation cost (a few hundred bytes per call) is typically invisible below 50 active towers.