# Episode 01: Enemy Movement

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden; max-width: 100%; margin: 1.5rem 0;">
  <iframe src="https://www.youtube.com/embed/PLACEHOLDER_EP01" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; border:0;" title="Episode 01" allowfullscreen></iframe>
</div>

## Learning Objectives

- Set up a basic enemy that follows a waypoint path
- Understand the game loop for enemy movement each frame
- Use `EnemyPath` to provide waypoint positions

## Key Concepts

- [Interfaces](../Concepts/Interfaces.md) *(preview — we won't use them yet)*

## What We're Starting With

- A 3D map with a visible road
- An `EnemyPath` component already placed in the scene that holds waypoint `Transform` references
- No enemy, no movement, no game loop

---

## Code Roadmap

| File | Purpose |
|------|---------|
| `Enemies/Controllers/EnemyController.cs` | Holds movement logic — moves enemy along path |
| `Systems/Game/EnemyPath.cs` | Already exists — provides waypoint array |

### EnemyController.cs — skeleton

```csharp
namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour
    {
        // TODO: reference to EnemyPath for waypoints
        // TODO: movement speed field
        // TODO: current waypoint index

        // TODO: Start — cache waypoints from EnemyPath
        // TODO: Update — move toward current waypoint
        // TODO: advance to next waypoint when close enough
    }
}
```

---

## Step-by-Step Implementation

### 1 — Create the EnemyController script

Create `Assets/Scripts/Enemies/Controllers/EnemyController.cs`.

```csharp
namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour
    {
        // TODO: declare _path (EnemyPath reference)
        // TODO: declare _speed (float, e.g. 3f)
        // TODO: declare _waypoints (Transform[])
        // TODO: declare _waypointIndex (int, start at 0)
    }
}
```

### 2 — Cache waypoints on Start

```csharp
namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour
    {
        // ... fields from step 1

        private void Start()
        {
            // TODO: get waypoints from _path.Waypoints into _waypoints
            // TODO: snap enemy to first waypoint position
        }
    }
}
```

> **Why `Start` and not the constructor?** Unity components only exist after `Awake`/`Start`. We reference a scene object, so we cache in `Start`.

### 3 — Move toward the current waypoint

```csharp
namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour
    {
        // ... fields and Start from steps 1–2

        private void Update()
        {
            // TODO: if no waypoints, return
            // TODO: if past last waypoint, return (or destroy)

            // TODO: Vector3 target = current waypoint position
            // TODO: Vector3 direction = (target - transform.position).normalized
            // TODO: transform.position += direction * _speed * Time.deltaTime
        }
    }
}
```

### 4 — Advance to the next waypoint

```csharp
namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour
    {
        // ...

        private void Update()
        {
            // ... movement from step 3

            // TODO: check distance to current waypoint
            // TODO: if within threshold (e.g. 0.1f), increment _waypointIndex
            // TODO: if _waypointIndex >= _waypoints.Length, destroy or despawn
        }
    }
}
```

### 5 — Wire it up in the Inspector

1. Create a cube in the scene, name it "Enemy"
2. Add the `EnemyController` component
3. Drag the `EnemyPath` object from the scene into the `_path` field
4. Set `_speed` to `3`
5. Press Play — the enemy should walk from waypoint to waypoint

---

## Episode Recap

- `EnemyController` moves an enemy along waypoints with hardcoded logic
- Movement is frame-rate independent thanks to `Time.deltaTime`
- Waypoint advancement uses a distance threshold
- **No interfaces, no strategies** — just direct, simple code

The movement works, but we've painted ourselves into a corner: health, damage, and targeting logic will all end up crammed into this one class. Episode 02 introduces **interfaces** to start splitting responsibilities.

## Challenge

Add a `_pathProgress` field that increases as the enemy moves (0 at start, 1 at the end of the path). You'll need this value later for targeting.

<details>
<summary>Hint</summary>

Track the total distance of all waypoint segments, then compute how far along the enemy has traveled as a fraction of that total.

</details>