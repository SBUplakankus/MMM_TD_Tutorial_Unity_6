# Episode 06: Projectile System & Pooling

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden;">
  <iframe src="https://www.youtube.com/embed/EPISODE_06_PLACEHOLDER" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; border:0;" allowfullscreen></iframe>
</div>

## Learning Objectives

- Integrate the object pool system with projectile spawning and returning
- Design a projectile hierarchy: `ProjectileBase` → `ArrowProjectile` / `BombProjectile`
- Implement `IPoolable` reset logic for safe pool reuse
- Build `TowerFiring` with cooldown-based shooting

## Key Concepts

- **Pool integration for projectiles** — no `Instantiate`/`Destroy`, all spawn/return through `ObjectPoolManager`
- **ProjectileBase (IPoolable)** — base class integrating pool lifecycle with projectile behaviour
- **Arrow vs Bomb** — pierce (passes through enemies) vs AOE (damage at impact point)
- **TowerFiring cooldown** — `fireRate` gates how often a tower can shoot
- **Homing vs position-based launch** — homing tracks `ITargetable`, position-based flies to a `Vector3`

## Code Roadmap

| File | Role |
|------|------|
| `ProjectileBase.cs` | Abstract base — IPoolable, move/launch/hit/reset, pooled lifecycle |
| `ArrowProjectile.cs` | Straight-line pierce projectile, multiple hits before pool return |
| `BombProjectile.cs` | AOE projectile, OverlapSphere damage on position impact |
| `TowerFiring.cs` | Cooldown-gated shooter, fetches from pool and launches |

## Architecture Context

```
TowerController.Update
  → TowerDetection.Scan (finds target)
  → TowerFiring.TryFire (checks cooldown, fetches from pool)
    → ObjectPoolManager.Get (spawns projectile)
    → ProjectileBase.Launch (sets target/position, begins movement)
      → ProjectileBase.Move (per-frame, overridden by subclass)
      → ProjectileBase.OnHit / OnHitPosition (deals damage)
        → ObjectPoolManager.Return (returns to pool)
```

Full tower → projectile → enemy pipeline. The pool sits between spawning and destruction, eliminating garbage collection spikes.

## Step-by-Step Implementation Guide

### Step 1: `ProjectileBase` — The Foundation

```csharp
public abstract class ProjectileBase : MonoBehaviour, IPoolable
{
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected int damage;
    [SerializeField] protected string poolKey;

    protected ITargetable Target;
    protected Vector3 TargetPosition;

    public string PoolKey => poolKey;

    public virtual void Launch(ITargetable target)
    {
        // Homing launch — store target reference
        // TODO: implement
    }

    public virtual void Launch(Vector3 position)
    {
        // Position-based launch — store target position
        // TODO: implement
    }

    protected virtual void Move()
    {
        // Per-frame movement — override in subclass
        // TODO: implement default straight-line movement
    }

    protected virtual void OnHit(ITargetable target)
    {
        // Deal damage via IDamageable, return to pool
        // TODO: implement
    }

    protected virtual void OnHitPosition(Vector3 position)
    {
        // AOE impact — override for area damage
        // TODO: implement
    }

    public virtual void Reset()
    {
        // Clear Target, TargetPosition, reset all state for pool reuse
        // TODO: implement
    }

    private void Update()
    {
        Move();
    }
}
```

- `Launch(ITargetable)` — for homing projectiles (arrows tracking an enemy)
- `Launch(Vector3)` — for position-based projectiles (bombs flying to a ground point)
- `Move()` — virtual so subclasses define their own movement (straight, homing, arc, etc.)
- `OnHit` / `OnHitPosition` — damage application, then pool return
- `Reset()` — part of `IPoolable`, called when the object returns to the pool

!!! tip "IPoolable contract"
    Every pooled object must implement `Reset()` to clear all runtime state. If you forget to clear `Target` or tracking variables, the next pool borrower gets stale data and bugs follow.

### Step 2: `ArrowProjectile` — Piercing Straight-Line

```csharp
public class ArrowProjectile : ProjectileBase
{
    [SerializeField] private int pierceCount;
    private int _pierceRemaining;
    private Vector3 _direction;

    public override void Launch(ITargetable target)
    {
        // Store target for damage, but direction is set at launch (no homing)
        // _direction = (target.Position - transform.position).normalized
        // TODO: implement
    }

    protected override void Move()
    {
        // Straight-line movement along _direction
        // transform.position += _direction * moveSpeed * Time.deltaTime
        // TODO: implement
    }

    protected override void OnHit(ITargetable target)
    {
        // Deal damage, decrement _pierceRemaining
        // Only return to pool when _pierceRemaining <= 0
        // TODO: implement
    }

    public override void Reset()
    {
        base.Reset();
        _pierceRemaining = pierceCount;
        _direction = Vector3.zero;
    }
}
```

Key differences from base:

- `pierceCount` — how many enemies this arrow passes through before expiring
- `_direction` — set once at launch, never updated (no homing)
- `OnHit` decrements `pierceRemaining` instead of immediately returning to pool
- `Reset` must restore `pierceCount` so the arrow is fresh when reused from pool

### Step 3: `BombProjectile` — AOE on Impact

```csharp
public class BombProjectile : BombProjectile
{
    [SerializeField] private float explosionRadius;
    [SerializeField] private LayerMask enemyLayer;

    public override void Launch(Vector3 position)
    {
        // Fly toward target position (ground impact point)
        // TODO: implement
    }

    protected override void Move()
    {
        // Straight-line toward TargetPosition
        // When distance < threshold, call OnHitPosition(TargetPosition)
        // TODO: implement
    }

    protected override void OnHitPosition(Vector3 position)
    {
        // OverlapSphere at position with explosionRadius
        // Filter results by IDamageable
        // Apply damage to all in radius
        // Return to pool
        // TODO: implement
    }

    public override void Reset()
    {
        base.Reset();
        // Clear any bomb-specific state
    }
}
```

Key differences from arrow:

- Flies to a `Vector3` position, not tracking an `ITargetable`
- `OnHitPosition` replaces `OnHit` — applies damage to an area, not a single target
- `explosionRadius` — configurable per bomb type
- Always returns to pool after a single impact (no pierce concept)

!!! warning "OverlapSphere performance"
    `OverlapSphere` in `OnHitPosition` runs once per bomb impact — not per frame. This is fine for occasional explosions. If you have dozens of bombs exploding every frame, consider batching or a spatial partition.

### Step 4: `TowerFiring` — The Shooter

```csharp
public class TowerFiring : MonoBehaviour
{
    [SerializeField] private float fireRate;
    [SerializeField] private string projectilePoolKey;
    [SerializeField] private Transform firePoint;

    private float _cooldownTimer;

    public bool CanFire => _cooldownTimer <= 0f;

    public void TryFire(ITargetable target)
    {
        if (!CanFire) return;

        // Fetch projectile from pool via ObjectPoolManager
        // Set position/rotation to firePoint
        // Call projectile.Launch(target)
        // Reset cooldownTimer to 1f / fireRate
        // TODO: implement
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;
    }
}
```

- `fireRate` — shots per second (e.g., 2.0 = one shot every 0.5s)
- `CanFire` — simple timer gate, clean for the controller to check
- `TryFire` — the single public method; controller calls it with a valid target
- Cooldown decrements each frame regardless of whether a shot was taken

### Step 5: Wire Pool Configs

Add projectile prefabs to `ObjectPoolManager`'s `PoolConfigs`:

1. Create an `ArrowProjectile` prefab with the `ArrowProjectile` component
2. Create a `BombProjectile` prefab with the `BombProjectile` component
3. Add both to the pool manager's config list with matching `poolKey` strings
4. Set initial pool sizes (e.g., 20 arrows, 10 bombs — adjust based on gameplay)

The pool keys in `PoolConfig` must exactly match the `poolKey` field on each `ProjectileBase` subclass.

### Step 6: Test the Full Flow

Walk through the complete pipeline each frame:

1. `TowerController.Update` — tower is active
2. `detection.ScanForTargets()` — finds valid enemy
3. `firing.TryFire(target)` — cooldown is clear
4. `ObjectPoolManager.Get(poolKey)` — fetches arrow from pool
5. `projectile.Launch(target)` — arrow starts moving
6. `projectile.Move()` — per-frame straight-line travel
7. `projectile.OnHit(target)` — damage dealt, pierce decremented
8. `ObjectPoolManager.Return(projectile)` — arrow back in pool (if pierce exhausted)

## Episode Recap

- `ProjectileBase` integrates `IPoolable` with projectile lifecycle (launch, move, hit, reset)
- `ArrowProjectile` pierces through multiple enemies before returning to pool
- `BombProjectile` deals AOE damage at an impact position using `OverlapSphere`
- `TowerFiring` enforces cooldowns and fetches projectiles from the pool
- The pool eliminates `Instantiate`/`Destroy` — zero GC allocation during gameplay
- Reset logic is critical — stale pool state causes subtle bugs

## Challenge

Design a **HomingMissile** projectile that tracks its target. Consider:

- What changes to `Move()` would you make? (hint: update `_direction` each frame toward `Target.Position`)
- How would you handle the case where the target **dies mid-flight**?
- Should the missile seek a new target, fly to the last known position, or self-destruct?
- What happens to `pierceCount` — does a homing missile need pierce, or is it single-target?
- How would you cap the homing behavior so missiles can't track forever (e.g., fuel/lifetime)?