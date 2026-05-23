# Episode 03: Tower Detection

## What You're Building

A tower that detects enemies within a radius and keeps a reference to the nearest one. Uses `ITargetable` from Episode 02 — the tower never knows about `EnemyController`. Debug gizmos visualize detection range and current target.

## TowerDetection.cs

```csharp
using Interfaces;
using UnityEngine;

namespace Towers
{
    public class TowerDetection : MonoBehaviour
    {
        [SerializeField] private float detectionRange = 5f;
        [SerializeField] private LayerMask enemyLayer;
        
        public ITargetable CurrentTarget {get; private set;}
        public bool HasTarget => CurrentTarget is { IsAlive: true };

        private void ScanForTargets()
        {
            CurrentTarget = null;
            
            var hits = Physics.OverlapSphere(transform.position, detectionRange, enemyLayer);
            var closestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent(out ITargetable target)) continue;
                var distance = Vector3.Distance(transform.position, hit.transform.position);
                if (!(distance < closestDistance)) continue;
                closestDistance = distance;
                CurrentTarget = target;
            }
        }
        
        private void Update() => ScanForTargets();

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            if (!HasTarget) return;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, CurrentTarget.Position);
        }
    }
}
```

**How it works:**
- `OverlapSphere` finds all Colliders on the `enemyLayer` within `detectionRadius`
- Iterates hits, looks for `ITargetable` via `TryGetComponent`, filters alive ones
- Picks the closest by `Vector3.Distance`
- `HasTarget` checks `IsAlive` — if the current target died between frames, this returns false
- Gizmos draw a sphere for range and a line to current target (visible in Scene view, not Game)

**Why `ITargetable` and not `EnemyController`?** `TryGetComponent<ITargetable>` works with any class that implements the interface. If you later add a different enemy type or a destructible prop, the tower detects it automatically. No changes to TowerDetection.

**Why overwrite CurrentTarget every frame?** Simple and reliable. The alternative — caching and only re-scanning when the target dies or leaves range — saves a tiny amount of work but adds complexity. For a TD game with <50 enemies in range, re-scanning is fine. When this becomes a problem, Episode 13 adds proper targeting strategies.

## Unity Editor Setup

### 1. Create an Enemy Layer

1. Edit > Project Settings > Tags and Layers
2. Add a layer named "Enemy" (e.g., layer 8)
3. Set your Enemy prefab's layer to "Enemy"

### 2. Create the Tower

1. Create a GameObject at a position near the path (where you want a tower)
2. Add `TowerDetection` component
3. Set `detection radius = 5`
4. Set `enemy layer` to the "Enemy" layer
5. Make sure enemy has a Collider on the same GameObject as the `EnemyController`

### 3. Test

1. Press Play — enemy walks along path
2. Switch to Scene view
3. When enemy enters tower radius: cyan sphere appears, red line connects tower to enemy
4. When enemy leaves radius or dies: red line disappears
5. Verify `HasTarget` in Inspector: toggles true/false as enemy enters/leaves range

## Debugging

| Symptom | Cause | Fix |
|---------|-------|-----|
| Tower never detects | Wrong `enemyLayer` mask | Verify LayerMask matches "Enemy" layer |
| `TryGetComponent<ITargetable>` returns null | Collider and EnemyController on different GameObjects | Move both to the same GameObject, or use GetComponentInParent |
| Gizmo sphere too small/large | `detectionRadius` not set | Adjust in Inspector |
| Red line visible in Game view | Gizmos display is on | Gizmos only show in Scene view by default. Toggle Gizmos button in Game view if needed |
| Tower switches target rapidly | Multiple enemies at same distance | Normal — ties break arbitrarily. Episode 13 adds targeting priority |