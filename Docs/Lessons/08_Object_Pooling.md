# Episode 08 — Object Pooling

<!-- Video placeholder -->
<iframe width="560" height="315" src="https://www.youtube.com/embed/PLACEHOLDER_EP08" frameborder="0" allowfullscreen></iframe>

---

## Learning Objectives

- Explain why `Instantiate`/`Destroy` cause GC pressure in a spawn-heavy game
- Implement Unity's `ObjectPool<T>` for enemies and projectiles
- Create an `IPoolable` interface with a `Reset()` method
- Pre-warm pools at startup for zero-alloc gameplay
- Compare Profiler screenshots before and after pooling

## Key Concepts

- [Object Pooling](../Concepts/Object_Pooling.md)
- [Interfaces](../Concepts/Interfaces.md)

---

## What We're Starting With

Our combat loop works — towers detect enemies, fire projectiles, enemies take damage and die. But every enemy spawn calls `Instantiate`, and every death calls `Destroy`. With ten enemies on screen and projectiles flying everywhere, that's a lot of allocations and deallocations per second.

---

## The Naive Version

Spawning and killing the "naive" way:

```csharp
// EnemySpawner.cs — the problem
public void SpawnEnemy(EnemyData data)
{
    // TODO: Instantiate creates a new GameObject every call
    //       Destroy deallocates it every time
    //       Profiler shows GC.Alloc spikes each frame
    //       → Replace with pool Get() in the refactor
    GameObject enemy = Instantiate(data.prefab, spawnPoint.position, Quaternion.identity);
}

// EnemyController.cs — the problem
public void Die()
{
    // TODO: Destroy triggers GC pressure
    //       Over hundreds of spawns this tanks frame rate
    //       → Replace with pool Return() in the refactor
    Destroy(gameObject);
}
```

**Open the Profiler** (Window → Analysis → Profiler). Run the game with a wave of enemies. Notice the GC.Alloc spikes every time an enemy spawns or dies. Those spikes grow with enemy count and eventually cause frame drops.

---

## The Refactor

We replace `Instantiate`/`Destroy` with Unity's built-in `ObjectPool<T>` and an `IPoolable` interface that lets pooled objects reset their state.

### Architecture Context

```
┌─────────────────────────────────────────────┐
│           ObjectPoolManager                  │
│  ┌─────────────┐  ┌──────────────────────┐  │
│  │ EnemyPool    │  │ ProjectilePools       │  │
│  │ ObjectPool   │  │ Dictionary<type,pool> │  │
│  └─────────────┘  └──────────────────────┘  │
│         │   Pre-warm on Awake                │
└─────────┼────────────────────────────────────┘
          │
    Get() │  Return()
          ▼
   ┌──────────────┐
   │  IPoolable    │
   │  Reset()      │
   └──────────────┘
      ▲         ▲
      │         │
 EnemyController  ProjectileBase
```

### Code Roadmap

| File | Purpose |
|------|---------|
| `Interfaces/IPoolable.cs` | Interface with `Reset()` for pooled objects |
| `Systems/Managers/ObjectPoolManager.cs` | Manages enemy + projectile pools, pre-warms on startup |
| `Enemies/Controllers/EnemyController.cs` | Implements `IPoolable`, returns to pool on `Die()` |
| `Projectiles/ProjectileBase.cs` | Implements `IPoolable`, returns to pool on impact/expire |

---

## Step-by-Step Implementation

### Step 1 — Create the IPoolable interface

Create `Interfaces/IPoolable.cs`:

```csharp
namespace Interfaces
{
    public interface IPoolable
    {
        // TODO: Define a Reset() method that restores
        //       the object to its fresh-from-pool state.
        //       Called automatically when the object is
        //       returned to the pool.
    }
}
```

When an object leaves the pool, `Reset()` is called so it behaves like a brand-new instance. This replaces any `OnEnable` re-initialisation logic you'd normally write.

### Step 2 — Create ObjectPoolManager (singleton for now)

Create `Systems/Managers/ObjectPoolManager.cs`. This episode uses the `Instance` singleton pattern; Episode 09 will refactor it to use the Service Locator.

```csharp
using UnityEngine.Pool;
using Interfaces;

namespace Systems.Managers
{
    public class ObjectPoolManager : MonoBehaviour
    {
        // TODO: Singleton — temporary until Episode 09
        public static ObjectPoolManager Instance { get; private set; }

        // TODO: Create a private ObjectPool<GameObject> for enemies
        //       Use createFunc, actionOnGet, actionOnRelease, actionOnDestroy
        //       defaultCapacity, maxSize

        // TODO: Create a Dictionary<string, ObjectPool<GameObject>>
        //       for projectile pools (key = projectile type name)

        // TODO: Awake() — set Instance, pre-warm pools

        // TODO: public GameObject GetEnemy() — pull from enemy pool

        // TODO: public void ReturnEnemy(GameObject enemy) — return to pool

        // TODO: public GameObject GetProjectile(string type) — pull from projectile pool

        // TODO: public void ReturnProjectile(GameObject proj, string type) — return to pool

        // TODO: Private helper — createFunc for enemy pool
        //       Instantiate prefab, add IPoolable component if missing

        // TODO: Private helper — actionOnGet
        //       Call IPoolable.Reset() and gameObject.SetActive(true)

        // TODO: Private helper — actionOnRelease
        //       Call gameObject.SetActive(false)

        // TODO: Private helper — actionOnDestroy
        //       Call Destroy(gameObject)
    }
}
```

**Pre-warming**: In `Awake()`, call `pool.Get()` then `pool.Release()` for each object up to `defaultCapacity`. This forces all allocations to happen once at startup — zero allocs during gameplay.

### Step 3 — Update EnemyController to use pooling

Update `Enemies/Controllers/EnemyController.cs`:

```csharp
using Interfaces;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IPoolable
    {
        // TODO: Implement IPoolable.Reset()
        //       — Reset health to max
        //       — Reset path position to start
        //       — Re-enable health bar
        //       — Re-register with UpdateManager if needed

        public void Die()
        {
            // TODO: Return to pool instead of Destroy
            //       ObjectPoolManager.Instance.ReturnEnemy(gameObject);
        }

        // TODO: Remove any OnDestroy or Destroy(gameObject) calls
        //       Replace with return-to-pool logic
    }
}
```

### Step 4 — Update ProjectileBase to use pooling

Update `Projectiles/ProjectileBase.cs`:

```csharp
using Interfaces;

namespace Projectiles
{
    public abstract class ProjectileBase : MonoBehaviour, IPoolable
    {
        // TODO: Add _poolType field (set by spawner, e.g. "Arrow", "Bomb")

        // TODO: Implement IPoolable.Reset()
        //       — Reset speed, direction, target
        //       — Reset travel time / distance counter

        // TODO: In OnHit() or OnExpire()
        //       Replace Destroy(gameObject) with:
        //       ObjectPoolManager.Instance.ReturnProjectile(gameObject, _poolType);
    }
}
```

### Step 5 — Update EnemySpawner to use the pool

Update `Systems/Game/EnemySpawner.cs`:

```csharp
namespace Systems.Game
{
    public class EnemySpawner : MonoBehaviour
    {
        // TODO: Replace Instantiate() with:
        //       GameObject enemy = ObjectPoolManager.Instance.GetEnemy();
        //       enemy.transform.position = spawnPoint.position;
        //       enemy.transform.rotation = Quaternion.identity;

        // TODO: Remove any Instantiate calls for enemies
    }
}
```

---

## Episode Recap

- **Naive**: `Instantiate`/`Destroy` on every spawn and kill → GC.Alloc spikes in the Profiler
- **Refactor**: `ObjectPool<T>` reuses objects; `IPoolable.Reset()` restores state; pre-warming moves all allocations to startup
- `ObjectPoolManager` is a **singleton for now** — Episode 09 wraps it into the Service Locator
- Open the Profiler again: GC.Alloc spikes during gameplay should be gone

---

## Challenge

1. Add pooling for status-effect visuals (e.g. the shield bubble or regen sparkle). Create a new pool in `ObjectPoolManager`, make the VFX prefab implement `IPoolable`, and measure the difference in the Profiler.

2. Stress test: spawn 100 enemies simultaneously with and without pooling. Record the frame time difference and write a short note in your project wiki.