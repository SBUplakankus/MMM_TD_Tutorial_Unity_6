# Episode 06: Projectile System & Pooling — Implementation Guide

## What You're Building

Projectiles are the damage delivery mechanism. This episode implements:
- **ProjectileBase** — pooled MonoBehaviour base class with homing and position-based movement, distance-check hit detection
- **ArrowProjectile** — straight-line (no homing), pierce support, only returns to pool when pierce exhausted
- **BombProjectile** — straight-line toward target position, AOE damage via OverlapSphere on arrival
- **TowerFiring** — cooldown-based launcher that fetches projectiles from ObjectPoolManager

Key design decisions:
- Distance check (`sqrMagnitude`) for hit detection instead of OnTriggerEnter — more reliable with pooled/deactivated objects
- Arrow locks direction at launch (no homing), Bomb locks target position at launch
- Base class `Move()` handles homing; Arrow and Bomb override with straight-line logic
- `hitRadius` serialized field on base controls "close enough" threshold

## Files & Order

| # | File | Action |
|---|------|--------|
| 1 | `Assets/Scripts/Projectiles/ProjectileBase.cs` | UPDATE — full implementation |
| 2 | `Assets/Scripts/Projectiles/ArrowProjectile.cs` | UPDATE — full implementation |
| 3 | `Assets/Scripts/Projectiles/BombProjectile.cs` | UPDATE — full implementation |
| 4 | `Assets/Scripts/Towers/TowerFiring.cs` | UPDATE — full implementation |

## Implementation

### 1. ProjectileBase.cs

```csharp
using Interfaces;
using Systems.Managers;
using UnityEngine;

namespace Projectiles
{
    public class ProjectileBase : MonoBehaviour, IPoolable
    {
        #region Fields

        [SerializeField] protected float moveSpeed = 10f;
        [SerializeField] protected float damage = 10f;
        [SerializeField] protected string poolKey = "arrow";
        [SerializeField] protected float hitRadius = 0.5f;

        protected ITargetable Target;
        protected Vector3 TargetPosition;
        protected bool HasTarget;
        protected Vector3 Direction;
        protected bool DirectionLocked;

        #endregion

        #region Lifecycle

        private void Update()
        {
            if (HasTarget && Target != null && !Target.IsAlive)
            {
                HasTarget = false;
            }

            Move();
        }

        #endregion

        #region Public API

        public virtual void Launch(ITargetable target)
        {
            Target = target;
            HasTarget = true;
            TargetPosition = target.Position;
            Direction = (TargetPosition - transform.position).normalized;
            DirectionLocked = false;
        }

        public virtual void Launch(Vector3 position)
        {
            Target = null;
            HasTarget = false;
            TargetPosition = position;
            Direction = (TargetPosition - transform.position).normalized;
            DirectionLocked = true;
        }

        #endregion

        #region Protected Methods

        protected virtual void Move()
        {
            var moveTarget = HasTarget && Target != null && Target.IsAlive
                ? Target.Position
                : TargetPosition;

            Direction = (moveTarget - transform.position).normalized;
            transform.position += Direction * (moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(Direction);

            var sqrDist = (transform.position - moveTarget).sqrMagnitude;
            var hitThreshold = hitRadius * hitRadius;

            if (sqrDist <= hitThreshold)
            {
                if (HasTarget && Target != null && Target.IsAlive)
                {
                    OnHit(Target);
                }
                else
                {
                    OnHitPosition(transform.position);
                }
            }
        }

        protected virtual void OnHit(ITargetable target)
        {
            if (target is IDamageable damageable)
            {
                damageable.TakeDamage(damage);
            }

            ReturnToPool();
        }

        protected virtual void OnHitPosition(Vector3 position)
        {
            ReturnToPool();
        }

        protected void ReturnToPool()
        {
            ObjectPoolManager.Instance.Return(poolKey, gameObject);
        }

        #endregion

        #region IPoolable

        public virtual void Reset()
        {
            Target = null;
            HasTarget = false;
            TargetPosition = Vector3.zero;
            Direction = Vector3.zero;
            DirectionLocked = false;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        #endregion
    }
}
```

### 2. ArrowProjectile.cs

Arrows fly straight — direction locked at launch, no homing. Pierce means the arrow can hit multiple enemies before returning to pool.

```csharp
using Interfaces;
using Systems.Managers;
using UnityEngine;

namespace Projectiles
{
    public class ArrowProjectile : ProjectileBase
    {
        #region Fields

        [SerializeField] private int pierceCount = 1;
        private int _pierceRemaining;
        private readonly Collider[] _hitBuffer = new Collider[8];
        [SerializeField] private float hitCheckRadius = 0.5f;
        [SerializeField] private LayerMask enemyLayer;

        #endregion

        #region Public API

        public override void Launch(ITargetable target)
        {
            base.Launch(target);
            _pierceRemaining = pierceCount;
            DirectionLocked = true;
        }

        #endregion

        #region Protected Methods

        protected override void Move()
        {
            transform.position += Direction * (moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(Direction);

            CheckForHits();

            if (Vector3.SqrMagnitude(transform.position - TargetPosition) > 900f)
            {
                ReturnToPool();
            }
        }

        private void CheckForHits()
        {
            if (_pierceRemaining <= 0) return;

            var hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                hitCheckRadius,
                _hitBuffer,
                enemyLayer
            );

            for (var i = 0; i < hitCount; i++)
            {
                if (_hitBuffer[i].TryGetComponent<ITargetable>(out var target) && target.IsAlive)
                {
                    OnHit(target);
                    break;
                }
            }
        }

        protected override void OnHit(ITargetable target)
        {
            if (target is IDamageable damageable)
            {
                damageable.TakeDamage(damage);
            }

            _pierceRemaining--;

            if (_pierceRemaining <= 0)
            {
                ReturnToPool();
            }
        }

        #endregion

        #region IPoolable

        public override void Reset()
        {
            base.Reset();
            _pierceRemaining = pierceCount;
        }

        #endregion
    }
}
```

### 3. BombProjectile.cs

Bombs fly toward a fixed position (no homing). On arrival, they deal AOE damage to all IDamageable within `explosionRadius`.

```csharp
using Interfaces;
using Systems.Managers;
using UnityEngine;

namespace Projectiles
{
    public class BombProjectile : ProjectileBase
    {
        #region Fields

        [SerializeField] private float explosionRadius = 2f;
        [SerializeField] private LayerMask enemyLayer;
        private readonly Collider[] _explosionBuffer = new Collider[32];

        #endregion

        #region Public API

        public override void Launch(ITargetable target)
        {
            base.Launch(target);
            TargetPosition = target.Position;
            Direction = (TargetPosition - transform.position).normalized;
            DirectionLocked = true;
        }

        #endregion

        #region Protected Methods

        protected override void Move()
        {
            transform.position += Direction * (moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(Direction);

            var sqrDist = (transform.position - TargetPosition).sqrMagnitude;
            if (sqrDist <= hitRadius * hitRadius)
            {
                OnHitPosition(transform.position);
            }
        }

        protected override void OnHitPosition(Vector3 position)
        {
            var hitCount = Physics.OverlapSphereNonAlloc(
                position,
                explosionRadius,
                _explosionBuffer,
                enemyLayer
            );

            for (var i = 0; i < hitCount; i++)
            {
                if (_explosionBuffer[i].TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }

            ReturnToPool();
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        #endregion
    }
}
```

### 4. TowerFiring.cs

```csharp
using Interfaces;
using Systems.Managers;
using UnityEngine;

namespace Towers
{
    public class TowerFiring : MonoBehaviour
    {
        #region Fields

        [SerializeField] private float fireRate = 1f;
        [SerializeField] private string projectilePoolKey = "arrow";
        [SerializeField] private Transform firePoint;

        private float _cooldownTimer;

        #endregion

        #region Properties

        public bool CanFire => _cooldownTimer <= 0f;

        #endregion

        #region Lifecycle

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }

            if (_cooldownTimer < 0f)
            {
                _cooldownTimer = 0f;
            }
        }

        #endregion

        #region Public API

        public void TryFire(ITargetable target)
        {
            if (!CanFire) return;

            var projectileObj = ObjectPoolManager.Instance.Get(
                projectilePoolKey,
                firePoint != null ? firePoint.position : transform.position,
                firePoint != null ? firePoint.rotation : transform.rotation
            );

            if (projectileObj == null) return;

            var projectile = projectileObj.GetComponent<ProjectileBase>();
            if (projectile == null) return;

            projectile.Launch(target);
            _cooldownTimer = 1f / fireRate;
        }

        #endregion
    }
}
```

## Unity Editor Setup

1. **Projectile prefabs**:
   - Create Arrow prefab: add `ArrowProjectile` component, set:
     - `Move Speed`: 15
     - `Damage`: 10
     - `Pool Key`: "arrow"
     - `Hit Radius`: 0.5
     - `Pierce Count`: 1 (increase for multi-shot towers)
     - `Hit Check Radius`: 0.5
     - `Enemy Layer`: same enemy layer from Ep05
   - Create Bomb prefab: add `BombProjectile` component, set:
     - `Move Speed`: 8
     - `Damage`: 20
     - `Pool Key`: "bomb"
     - `Hit Radius`: 0.5
     - `Explosion Radius`: 2.0
     - `Enemy Layer`: same enemy layer

2. **ObjectPoolManager config**: Add pool entries:
   - key: "arrow", prefab: Arrow prefab, defaultSize: 20, maxSize: 100
   - key: "bomb", prefab: Bomb prefab, defaultSize: 10, maxSize: 50

3. **Tower prefab**:
   - Add `TowerFiring` component, set:
     - `Fire Rate`: 1.0 (1 shot per second)
     - `Projectile Pool Key`: "arrow"
     - `Fire Point`: create empty child GameObject at barrel/tip, assign here
   - Wire `TowerFiring` reference into `TowerController`

4. **Layer consistency**: All enemy prefabs and projectile enemy-layer masks must use the same "Enemy" layer.

## Test Plan

| Test | Steps | Expected |
|------|-------|----------|
| Arrow basic fire | Place tower, spawn enemy in range | Arrow flies toward enemy, deals damage, returns to pool |
| Arrow no homing | Spawn enemy, let arrow launch, move enemy sideways | Arrow continues in locked direction, misses |
| Arrow pierce | Set pierce to 3, spawn enemies in a line | Arrow hits first, continues, hits next, returns after 3 |
| Bomb AOE | Use bomb projectile, spawn cluster of enemies | All enemies in explosion radius take damage |
| Bomb position lock | Target moves after bomb launch | Bomb lands at original position, not chasing target |
| Fire rate cooldown | Watch tower with fireRate=2 | Fires exactly every 0.5s |
| Pool recycling | Fire many arrows rapidly | No new GameObjects created after pool is warm, objects recycled |
| Target dies mid-flight | Kill target while arrow is flying | Arrow doesn't crash, HasTarget becomes false, flies to last known position |
| Off-screen cleanup | Arrow misses and flies far away | SqrMagnitude > 900 check returns it to pool |

## Debugging Tips

- **Arrow not hitting**: Increase `hitCheckRadius` — the OverlapSphereNonAlloc check needs to be big enough to overlap the enemy collider. If enemies are small, 0.5 might be too tight.
- **Bomb not dealing AOE damage**: Verify `enemyLayer` on BombProjectile matches the enemy prefab layer. OverlapSphereNonAlloc uses the layer mask.
- **Projectile spawner returns null**: ObjectPoolManager likely hasn't been initialized or the pool key doesn't exist. Check pool configs match `projectilePoolKey`.
- **Tons of GC allocs from OverlapSphere**: We used `NonAlloc` variants with pre-allocated buffers — if you still see allocs, check that nothing is calling the non-NonAlloc `Physics.OverlapSphere`.
- **Arrow stuck never returning**: The `sqrMagnitude > 900` failsafe (30 unit distance) is a safety net. If your play area is large, increase this value. The real fix is ensuring arrows eventually hit something or fly off.
- **FirePoint null**: If you don't set a firePoint child on the tower, `TryFire` falls back to tower position/rotation. This is fine for testing but arrows will spawn at the tower base.
- **pierce reset on pool return**: `ArrowProjectile.Reset()` is called by `ObjectPoolManager.Return()` via `IPoolable.Reset()`. Verify the pool manager calls Reset on release.
- **Bomb hits target directly instead of position**: BombProjectile overrides `Launch(ITargetable)` to snapshot `target.Position` into `TargetPosition`, then ignores the target for tracking. The base `Move()` is overridden — bomb uses its own straight-line logic.