# Episode 07: Projectiles

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden; max-width: 100%; margin: 1.5rem 0;">
  <iframe src="https://www.youtube.com/embed/PLACEHOLDER_EP07" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; border:0;" title="Episode 07" allowfullscreen></iframe>
</div>

## Learning Objectives

- Understand why "damage on overlap" is insufficient for a tower defence game
- Create a `ProjectileBase` with shared behaviour and `ArrowProjectile` as a concrete type
- Implement `TowerFiring` with a cooldown system
- Connect the full pipeline: tower detects target → fires projectile → projectile hits enemy → enemy takes damage

## Key Concepts

- [Strategy Pattern](../Concepts/Strategy_Pattern.md)
- [Object Pooling](../Concepts/Object_Pooling.md) *(preview — we use Instantiate for now)*

## What We're Starting With

- Towers detect targets via `ITargetingStrategy` (Episode 06)
- `EnemyController` implements `IDamageable` and `ITargetable`
- Towers can *find* the right enemy but can't *shoot* at it yet

---

## The Naive Version

The quickest way to make a tower "shoot": when an enemy enters range, reduce its health directly.

```csharp
namespace Towers
{
    public class TowerController : MonoBehaviour
    {
        // TODO: TowerDetection _detection;

        // TODO: void Update()
        // {
        //     var target = _detection.FindTarget();
        //     if (target != null)
        //         (target as IDamageable).TakeDamage(25f);
        // }
    }
}
```

**The problems:**

- **No visual feedback** — enemies just lose health, no arrow flies through the air
- **No projectile types** — can't have arrows, bombs, splash, homing missiles
- **Can't miss** — damage is instant, no travel time, no dodge-able projectiles
- **No cooldown** — deals damage every frame while target is in range

We need projectiles: objects that travel from tower to target, deal damage on hit, and can be customised per tower type.

---

## The Refactor

We introduce a projectile hierarchy and a tower firing system:

| Component | Responsibility |
|-----------|---------------|
| `ProjectileBase` | Shared projectile logic: movement, hit detection, despawn |
| `ArrowProjectile` | Straight-line projectile, deals damage on hit |
| `TowerFiring` | Cooldown system, spawns projectiles at targets |

> **Note on pooling:** We use `Instantiate` / `Destroy` this episode. Object pooling comes in a later episode when we have performance concerns. See [Object Pooling](../Concepts/Object_Pooling.md) for the concept.

---

## Code Roadmap

| File | Purpose |
|------|---------|
| `Projectiles/ProjectileBase.cs` | Base class — move toward target, detect hit, apply damage |
| `Projectiles/ArrowProjectile.cs` | Straight-line movement, single-target damage |
| `Towers/TowerFiring.cs` | Cooldown timer, spawns projectiles |
| `Towers/TowerController.cs` | Orchestrates detection + firing |

### ProjectileBase.cs — skeleton

```csharp
namespace Projectiles
{
    public abstract class ProjectileBase : MonoBehaviour
    {
        // TODO: protected float _speed;
        // TODO: protected float _damage;
        // TODO: protected ITargetable _target;

        // TODO: public virtual void Initialise(float speed, float damage, ITargetable target)
        // TODO: abstract void Move(float deltaTime);
        // TODO: void Update() — Move, then check if reached target
        // TODO: protected void HitTarget() — call (target as IDamageable).TakeDamage(_damage), then despawn
    }
}
```

### ArrowProjectile.cs — skeleton

```csharp
namespace Projectiles
{
    public class ArrowProjectile : ProjectileBase
    {
        // TODO: override Move — move toward _target.Position at _speed
        // TODO: override hit detection — check distance to target
    }
}
```

### TowerFiring.cs — skeleton

```csharp
namespace Towers
{
    public class TowerFiring : MonoBehaviour
    {
        // TODO: float _cooldown;        // seconds between shots
        // TODO: float _cooldownTimer;
        // TODO: GameObject _projectilePrefab;
        // TODO: float _projectileSpeed;
        // TODO: float _damage;
        // TODO: TowerDetection _detection;

        // TODO: void Update()
        //   — decrement _cooldownTimer
        //   — if timer <= 0 and target found, fire
        // TODO: void Fire(ITargetable target)
        //   — Instantiate projectile
        //   — call Initialise on ProjectileBase component
        //   — reset _cooldownTimer
    }
}
```

---

## Step-by-Step Implementation

### 1 — Create ProjectileBase

Create `Assets/Scripts/Projectiles/ProjectileBase.cs`:

```csharp
namespace Projectiles
{
    public abstract class ProjectileBase : MonoBehaviour
    {
        protected float _speed;
        protected float _damage;
        protected ITargetable _target;

        public virtual void Initialise(float speed, float damage, ITargetable target)
        {
            // TODO: store speed, damage, target
        }

        protected abstract void Move(float deltaTime);

        private void Update()
        {
            // TODO: if target is null or not alive, despawn and return
            // TODO: Move(Time.deltaTime)
            // TODO: check if reached target (distance < threshold)
            //   — if yes: HitTarget()
        }

        protected void HitTarget()
        {
            // TODO: if target is IDamageable, call TakeDamage(_damage)
            // TODO: Destroy(gameObject)
        }
    }
}
```

> `ProjectileBase` is `abstract` because different projectiles move differently. Arrows fly straight. Bombs arc. Homing missiles curve. The `Move` method is the extension point.

### 2 — Create ArrowProjectile

Create `Assets/Scripts/Projectiles/ArrowProjectile.cs`:

```csharp
namespace Projectiles
{
    public class ArrowProjectile : ProjectileBase
    {
        protected override void Move(float deltaTime)
        {
            // TODO: if _target is null, return
            // TODO: Vector3 direction = (_target.Position - transform.position).normalized
            // TODO: transform.position += direction * _speed * deltaTime
            // TODO: transform.rotation = Quaternion.LookRotation(direction)  // face movement
        }
    }
}
```

> Arrow projectiles fly in a straight line toward the target's current position. If the target moves, the arrow adjusts direction each frame — this creates natural "homing" for fast projectiles.

### 3 — Create TowerFiring

Create `Assets/Scripts/Towers/TowerFiring.cs`:

```csharp
namespace Towers
{
    public class TowerFiring : MonoBehaviour
    {
        [SerializeField] private float _cooldown = 1f;
        [SerializeField] private float _projectileSpeed = 10f;
        [SerializeField] private float _damage = 25f;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private TowerDetection _detection;

        private float _cooldownTimer;

        private void Update()
        {
            // TODO: _cooldownTimer -= Time.deltaTime;
            // TODO: if _cooldownTimer > 0, return

            // TODO: var target = _detection.FindTarget();
            // TODO: if target == null, return

            // TODO: Fire(target);
        }

        private void Fire(ITargetable target)
        {
            // TODO: GameObject projectile = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
            // TODO: var base = projectile.GetComponent<ProjectileBase>();
            // TODO: base.Initialise(_projectileSpeed, _damage, target);
            // TODO: _cooldownTimer = _cooldown;
        }
    }
}
```

### 4 — Update TowerController to orchestrate

```csharp
namespace Towers
{
    public class TowerController : MonoBehaviour
    {
        // TODO: TowerDetection _detection;   // finds targets
        // TODO: TowerFiring _firing;          // shoots at targets
        // Both run independently via their own Update methods
    }
}
```

> `TowerDetection` populates `_inRange` via triggers. `TowerFiring` asks `TowerDetection` for a target and fires. They don't need `TowerController` to mediate — but we keep `TowerController` for future coordination (upgrades, selling, etc.).

### 5 — Create the Arrow prefab

1. Create a small cylinder or capsule GameObject — this is the arrow mesh
2. Add the `ArrowProjectile` component
3. Save as a prefab: `Assets/Prefabs/Projectiles/ArrowProjectile.prefab`
4. On the tower, add `TowerFiring` and drag the arrow prefab into `_projectilePrefab`

### 6 — Set up tower detection triggers

On the tower, add a `SphereCollider` set to `isTrigger = true` with radius matching `_range`. `TowerDetection` uses `OnTriggerEnter` / `OnTriggerExit` to populate `_inRange`.

```csharp
// Inside TowerDetection
private void OnTriggerEnter(Collider other)
{
    // TODO: if other.TryGetComponent(out ITargetable target)
    // TODO:     _inRange.Add(target);
}

private void OnTriggerExit(Collider other)
{
    // TODO: if other.TryGetComponent(out ITargetable target)
    // TODO:     _inRange.Remove(target);
}
```

### 7 — Test the full pipeline

1. Press Play
2. An enemy walks the path
3. When it enters the tower's range, the tower should detect it (visualise with `Debug.DrawLine` from Episode 06)
4. The tower fires an arrow at the target
5. The arrow flies toward the enemy
6. On contact, the enemy takes 25 damage
7. After 4 hits (4 x 25 = 100), the enemy dies
8. The arrow destroys itself after hitting

---

## Episode Recap

- **Naive**: tower deals damage directly in `Update` — no visual, no variety, no cooldown
- **Refactor**: `ProjectileBase` + `ArrowProjectile` gives us travel time, visual feedback, and customisable projectiles
- `TowerFiring` adds a cooldown system — towers can only shoot every `_cooldown` seconds
- The full pipeline: `TowerDetection.FindTarget()` → `TowerFiring.Fire()` → `ProjectileBase.Initialise()` → `ArrowProjectile.Move()` → `HitTarget()` → `IDamageable.TakeDamage()`
- **We use `Instantiate`/`Destroy` for now**. Object pooling replaces this in a later episode for performance.

Episode 08 will add wave spawning and more enemy types — but the architecture is in place: strategies, interfaces, and composition keep the system extensible without touching core classes.

## Challenge

Create a `BombProjectile` that deals splash damage — instead of hitting a single target, it deals damage to all `IDamageable` objects within a radius when it reaches the target. Hint: use `Physics.OverlapSphere` at the impact point, then call `TakeDamage` on each `IDamageable` found.

<details>
<summary>Hint</summary>

Override `HitTarget()` in `BombProjectile`. Instead of a single `TakeDamage` call, use `Physics.OverlapSphere(transform.position, _splashRadius)`. For each collider, try `GetComponent<IDamageable>()`. If found, call `TakeDamage(_damage)`. Add a `_splashRadius` field and expose it in `Initialise`.

</details>