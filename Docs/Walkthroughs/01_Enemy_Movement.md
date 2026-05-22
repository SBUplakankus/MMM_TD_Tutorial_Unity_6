# Episode 01: Enemy Movement

## What You're Building

An enemy that walks along a path of waypoints from start to end. This is the foundation everything else builds on.

## Starting Point

You have a Unity project with a 3D map and some enemy models. No scripts exist yet. You'll create two:

1. **EnemyPath** — a MonoBehaviour on a path GameObject that stores waypoints and answers questions about them
2. **EnemyController** — a MonoBehaviour on the enemy that moves along the path

## EnemyPath.cs

Create an empty GameObject in your scene called "EnemyPath". Add this script to it:

```csharp
using UnityEngine;

namespace Systems.Game
{
    public class EnemyPath : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;

        private const float WaypointReachedDistance = 0.1f;

        public int WaypointCount => waypoints.Length;
        public Vector3 StartPosition => waypoints[0].position;
        public Vector3 EndPosition => waypoints[^1].position;

        public Vector3 GetWaypointPosition(int index)
        {
            return index >= 0 && index < waypoints.Length
                ? waypoints[index].position
                : EndPosition;
        }

        public bool HasWaypoint(int index)
        {
            return index >= 0 && index < waypoints.Length;
        }

        public bool IsAtWaypoint(int index, Vector3 currentPosition)
        {
            if (index < 0 || index >= waypoints.Length) return false;

            float sqrDist = (currentPosition - waypoints[index].position).sqrMagnitude;
            return sqrDist <= WaypointReachedDistance * WaypointReachedDistance;
        }
    }
}
```

**How it works:**
- `waypoints` is a Transform array you populate in the Inspector with Empty GameObjects placed along the map path
- `GetWaypointPosition(index)` returns the world position of waypoint N. Clamps to the last waypoint if index is out of range
- `HasWaypoint(index)` tells you if index is valid — used to know when the enemy has reached the end
- `IsAtWaypoint(index, position)` uses sqrMagnitude (avoids the square root of Vector3.Distance) to check if position is within 0.1 units of the waypoint

**Why sqrMagnitude instead of Distance?** `Vector3.Distance(a, b)` computes `Mathf.Sqrt((a-b).sqrMagnitude)`. The square root is expensive and unnecessary when you're just comparing against a threshold. Compare squared distance against squared threshold — same result, no sqrt.

## EnemyController.cs

Create a GameObject for your enemy (or use a provided model). Add this script:

```csharp
using Systems.Game;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private EnemyPath path;

        private int _currentWaypointIndex;

        private void Start()
        {
            _currentWaypointIndex = 0;
            transform.position = path.StartPosition;
        }

        private void Update()
        {
            if (path == null) return;

            if (!path.HasWaypoint(_currentWaypointIndex))
            {
                Destroy(gameObject);
                return;
            }

            Vector3 target = path.GetWaypointPosition(_currentWaypointIndex);
            transform.position = Vector3.MoveTowards(
                transform.position, target, moveSpeed * Time.deltaTime);

            if (path.IsAtWaypoint(_currentWaypointIndex, transform.position))
            {
                _currentWaypointIndex++;
            }
        }
    }
}
```

**How it works:**
- `Start()` snaps the enemy to the first waypoint position
- `Update()` moves toward the current waypoint using `Vector3.MoveTowards` (constant speed, no acceleration)
- When `IsAtWaypoint` returns true, advance `_currentWaypointIndex` to the next waypoint
- When `HasWaypoint` returns false, the enemy has reached the end — `Destroy(gameObject)`

**Why MoveTowards not Lerp?** `Vector3.Lerp(a, b, t)` interpolates by a percentage — the enemy slows down as it approaches the target. `MoveTowards(a, b, maxDistance)` moves at constant speed. Tower defense enemies should move at constant speed.

## Unity Editor Setup

### 1. Create the EnemyPath

1. Create Empty GameObject named "EnemyPath"
2. Add `EnemyPath` component
3. Create several Empty GameObjects as children — position them along the map path from start to end
4. In the Inspector on EnemyPath, set the `waypoints` array size to match your waypoint count
5. Drag each waypoint Transform into the array in order

### 2. Create the Enemy

1. Create a GameObject with a Collider and the enemy model/mesh
2. Add `EnemyController` component
3. Drag the "EnemyPath" object into the `path` field
4. Set `move speed = 5`

### 3. Test

1. Press Play
2. Enemy appears at the first waypoint
3. Enemy walks toward each waypoint at constant speed
4. Enemy reaches the end and is destroyed

## Debugging

| Symptom | Cause | Fix |
|---------|-------|-----|
| Enemy doesn't move | `path` field is null | Drag EnemyPath into the Inspector field |
| Enemy spawns at (0,0,0) | `Start` not called or path has no waypoints | Verify waypoints array is populated |
| Enemy skips waypoints | WaypointReachedDistance too large | Default 0.1 is fine for most scales. If your map is tiny, reduce it. |
| Enemy walks past waypoints | moveSpeed too high for frame rate | Reduce speed or reduce WaypointReachedDistance — at 60fps and 5 units/sec, 0.1 threshold works fine |
| Enemy destroyed immediately | First waypoint index is out of range | Waypoints array is empty — populate it in Inspector |