# Episode 02: Interfaces

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden; max-width: 100%; margin: 1.5rem 0;">
  <iframe src="https://www.youtube.com/embed/PLACEHOLDER_EP02" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; border:0;" title="Episode 02" allowfullscreen></iframe>
</div>

## Learning Objectives

- Recognise when a class is doing too many things (the "fat controller" smell)
- Extract responsibilities into interfaces
- Implement `IDamageable` and `ITargetable` so other systems can interact with enemies *without knowing about EnemyController*

## Key Concepts

- [Interfaces](../Concepts/Interfaces.md)

## What We're Starting With

- A working `EnemyController` from Episode 01 that moves along waypoints
- The enemy can walk the path — but has no health, no damage, no way for towers to target it

---

## The Naive Version

We need enemies to take damage. The obvious approach: just bolt health and damage straight into `EnemyController`.

```csharp
namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour
    {
        // movement fields from Episode 01 ...

        [SerializeField] private float _maxHealth = 100f;
        private float _currentHealth;

        private void Start()
        {
            // ... waypoint caching ...
            _currentHealth = _maxHealth;
        }

        public void TakeDamage(float amount)
        {
            // TODO: subtract amount from _currentHealth
            // TODO: if _currentHealth <= 0, destroy enemy
        }
    }
}
```

**Why is this a problem?**

- Towers need a reference to `EnemyController` just to deal damage — they're coupled to the *entire* enemy class
- If we add a destructible wall later, we'd need a whole new `DestructibleController` with its own `TakeDamage` — towers can't treat both uniformly
- `TakeDamage` and `Position`/`IsAlive` are *concepts*, not `EnemyController` details

---

## The Refactor

We extract two interfaces:

| Interface | Responsibility |
|-----------|---------------|
| `IDamageable` | Anything that can receive damage |
| `ITargetable` | Anything that can be targeted by towers |

### Interface definitions (skeleton)

```csharp
namespace Interfaces
{
    public interface IDamageable
    {
        // TODO: void TakeDamage(float amount);
    }
}
```

```csharp
namespace Interfaces
{
    public interface ITargetable
    {
        // TODO: Vector3 Position { get; }
        // TODO: bool IsAlive { get; }
        // TODO: float PathProgress { get; }
    }
}
```

> **Why `ITargetable` too?** Towers need to *find* and *sort* targets. They don't care about damage — they care about position and progress. Splitting keeps each interface focused on one job.

---

## Code Roadmap

| File | Purpose |
|------|---------|
| `Interfaces/IDamageable.cs` | Contract for receiving damage |
| `Interfaces/ITargetable.cs` | Contract for targeting (position, alive, progress) |
| `Enemies/Controllers/EnemyController.cs` | Implements both interfaces, delegates health logic |

### EnemyController.cs — skeleton after refactor

```csharp
namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable
    {
        // TODO: movement fields from Episode 01
        // TODO: _maxHealth, _currentHealth fields

        // --- IDamageable ---
        // TODO: public void TakeDamage(float amount)

        // --- ITargetable ---
        // TODO: public Vector3 Position => transform.position;
        // TODO: public bool IsAlive => _currentHealth > 0;
        // TODO: public float PathProgress { get; private set; }
    }
}
```

---

## Step-by-Step Implementation

### 1 — Create IDamageable

Create `Assets/Scripts/Interfaces/IDamageable.cs`:

```csharp
namespace Interfaces
{
    public interface IDamageable
    {
        // TODO: void TakeDamage(float amount);
    }
}
```

### 2 — Create ITargetable

Create `Assets/Scripts/Interfaces/ITargetable.cs`:

```csharp
namespace Interfaces
{
    public interface ITargetable
    {
        // TODO: Vector3 Position { get; }
        // TODO: bool IsAlive { get; }
        // TODO: float PathProgress { get; }
    }
}
```

### 3 — Add health fields to EnemyController

```csharp
namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable
    {
        // movement fields ...

        [SerializeField] private float _maxHealth = 100f;
        private float _currentHealth;

        private void Start()
        {
            // ... waypoint caching ...
            // TODO: _currentHealth = _maxHealth;
        }
    }
}
```

### 4 — Implement IDamageable

```csharp
// Inside EnemyController

public void TakeDamage(float amount)
{
    // TODO: _currentHealth -= amount;
    // TODO: if _currentHealth <= 0, handle death (destroy or disable)
}
```

### 5 — Implement ITargetable

```csharp
// Inside EnemyController

public Vector3 Position => transform.position;

public bool IsAlive => _currentHealth > 0;

public float PathProgress { get; private set; }
```

> **PathProgress** tracks how far along the path the enemy is (0 at start, 1 at end). You computed this in the Episode 01 challenge. If you skipped it, add it now — towers will need it.

### 6 — Test: click to damage

Add a temporary test in `Update` so you can verify the system:

```csharp
private void Update()
{
    // ... movement code ...

    // TEMP TEST: press Space to deal 25 damage
    // TODO: if Input.GetKeyDown(KeyCode.Space), call TakeDamage(25f)
}
```

Press Play, spawn an enemy, press Space — the enemy should die after 4 hits (4 x 25 = 100).

**Remove the test input before moving on.**

---

## Episode Recap

- **Naive**: `TakeDamage` and health baked right into `EnemyController` — towers would need to reference the concrete class
- **Refactor**: `IDamageable` and `ITargetable` let towers work with *any* damageable, targetable thing — not just enemies
- Interfaces decouple *what* something does from *how* it does it
- `ITargetable` uses `PathProgress` — this is the hook that will power targeting strategies in Episode 06

Right now, health is still a hard-coded field inside `EnemyController`. Episode 03 introduces the **Strategy Pattern** and **Factory** so we can swap health and movement behaviours without touching the controller.

## Challenge

What if a tower also needed to be damageable? Sketch what `TowerController : MonoBehaviour, IDamageable` might look like. You don't need to implement it — just list what fields and methods it would need.

<details>
<summary>Hint</summary>

A tower's `TakeDamage` would need its own `_maxHealth` and `_currentHealth`. The difference: towers don't move, so they might implement `IDamageable` but *not* `ITargetable`. Or they might implement `ITargetable` with `PathProgress = 0` since they never move. The point is: both can be damaged through the same interface.

</details>