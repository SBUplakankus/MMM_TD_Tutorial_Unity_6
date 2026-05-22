# Episode 04: Tower Firing

## What You're Building

The tower fires a projectile at the detected enemy. The projectile moves toward the target, hits it, and deals damage via `IDamageable`. **This is the first episode where the full combat loop works** — enemy walks, tower shoots, enemy takes damage and dies.

## ProjectileBase.cs

```csharp
using Interfaces;
using UnityEngine;

namespace Projectiles
{
    public class ProjectileBase : MonoBehaviour
    {
        [SerializeField] protected float moveSpeed = 15f;
        [SerializeField] protected float damage = 10f;
        [SerializeField] protected float maxLifetime = 5f;

        protected ITargetable Target;

        public virtual void Launch(ITargetable target)
        {
            Target = target;
            Destroy(gameObject, maxLifetime);
        }

        private void Update()
        {
            if (Target == null || !Target.IsAlive)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 direction = (Target.Position - transform.position).normalized;
            transform.position += direction * (moveSpeed * Time.deltaTime);

            float distance = Vector3.Distance(transform.position, Target.Position);
            if (distance <= 0.2f)
            {
                OnHit(Target);
            }
        }

        protected virtual void OnHit(ITargetable target)
        {
            if (target is IDamageable damageable)
                damageable.TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}
```

**How it works:**
- `Launch(ITargetable)` stores the target. The projectile homes toward the target each frame.
- If target is null or dead, projectile self-destructs — no point chasing nothing.
- `direction * moveSpeed * Time.deltaTime` — constant-speed homing movement.
- When distance is below 0.2f, call `OnHit` — which deals damage via `IDamageable` and destroys the projectile.
- `Destroy(gameObject, maxLifetime)` safety net — if the projectile never hits anything (target moves erratically), it cleans up after 5 seconds.
- Uses Instantiate/Destroy — naive version. Episode 8 replaces with object pooling.

**Why homing instead of ballistic?** Homing is simpler and more reliable for a first pass. Ballistic projectiles (arc, leading the target) are harder to code and miss often. We can add ballistic variants later as subclasses.

**Why `target is IDamageable damageable`?** `ITargetable` says where something is. `IDamageable` says it can take damage. A projectile that hits a targetable thing needs to check: "can this thing actually be damaged?" The `is` pattern checks and casts in one line.

## TowerFiring.cs

```csharp
using Towers;
using UnityEngine;

namespace Towers
{
    public class TowerFiring : MonoBehaviour
    {
        [SerializeField] private TowerDetection detection;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 1f;

        private float _fireCooldown;

        private void Update()
        {
            _fireCooldown -= Time.deltaTime;

            if (detection.HasTarget && _fireCooldown <= 0f)
            {
                Fire();
                _fireCooldown = 1f / fireRate;
            }
        }

        private void Fire()
        {
            GameObject projectileObj = Instantiate(
                projectilePrefab, firePoint.position, firePoint.rotation);

            ProjectileBase projectile = projectileObj.GetComponent<ProjectileBase>();
            projectile.Launch(detection.CurrentTarget);
        }
    }
}
```

**How it works:**
- References `TowerDetection` (from Episode 03) — asks "do you have a target?"
- `_fireCooldown` counts down. When 0 and a target exists, fires.
- `1f / fireRate` converts "shots per second" to "seconds between shots" (fireRate=1 → 1 second cooldown).
- `Instantiate` creates the projectile, `Launch` gives it the target.
- Uses `Instantiate` — naive version. Pooling in Episode 8.

## Unity Editor Setup

### 1. Create the Projectile

1. Create a small Sphere (scale 0.2, 0.2, 0.2) — this is your projectile visual
2. Add `ProjectileBase` component
3. Set `move speed = 15`, `damage = 10`, `max lifetime = 5`
4. Create a prefab from this (drag to Project window)

### 2. Update the Tower

1. On your tower GameObject, add `TowerFiring` component
2. Drag the `TowerDetection` component into the `detection` field
3. Set `projectile prefab` to your projectile prefab
4. Create an empty child GameObject named "FirePoint" at the muzzle position
5. Drag FirePoint into the `fire point` field
6. Set `fire rate = 1` (one shot per second)

### 3. Test the Combat Loop

1. Press Play
2. Enemy walks along path
3. When enemy enters tower range:
   - Tower fires a projectile (one per second)
   - Projectile flies toward enemy
   - On hit: enemy takes 10 damage, health bar decreases
4. After 10 hits (10 × 10 = 100 HP): enemy dies, is destroyed

## Debugging

| Symptom | Cause | Fix |
|---------|-------|-----|
| Tower never fires | `detection` field not assigned | Drag TowerDetection into Inspector |
| Projectile spawns at wrong spot | `firePoint` not set | Create FirePoint child and assign |
| Projectile flies sideways | FirePoint rotation is wrong | Orient FirePoint to face the path |
| Projectile misses | 0.2f hit distance too small for projectile speed | Increase hit distance or reduce projectile speed |
| Multiple projectiles per frame | `fireRate = 0` causes division by zero | Set fireRate to at least 0.1 |
| Projectile flies through enemy | Speed too high for frame rate | At 60fps and 15 units/sec, projectile moves 0.25 units per frame. Hit distance of 0.2f is fine at this speed. If you increase speed, increase hit distance. |
| Enemy doesn't take damage | Enemy Collider is a trigger | Projectile uses distance check, not collision. But if you want collision-based, replace the distance check with `OnTriggerEnter`. |