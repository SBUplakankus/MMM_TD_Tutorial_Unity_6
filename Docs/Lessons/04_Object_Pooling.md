# Episode 04: Object Pooling

!!! info "Episode Type: Code Lesson"
    You'll understand and implement the `ObjectPoolManager` — the system that eliminates GC spikes by reusing objects. ~20 min.

---

## Video

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden;">
  <iframe src="https://www.youtube.com/embed/EP04_PLACEHOLDER" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%;" frameborder="0" allowfullscreen></iframe>
</div>

**Duration:** ~20 min

---

## Learning Objectives

By the end of this episode you will be able to:

1. Explain why `Instantiate`/`Destroy` causes GC allocation spikes and how pooling avoids them
2. Implement `ObjectPoolManager` — singleton, pre-warming, named pool dictionary, `Get`/`Return`/`ReturnDelayed`
3. Understand `PoolConfig` — key, prefab, default size, max size
4. Wire `IPoolable.Reset()` into the return path so pooled objects start clean
5. Know when **not** to pool — rare objects, complex state, boss entities
6. Connect `EnemyController.Die()` to pool return instead of `Destroy`

---

## Key Concepts

| Concept | Summary | Learn More |
|---------|---------|------------|
| Unity `ObjectPool<T>` | Built-in generic pool with create/get/release/destroy callbacks | [Object Pooling](../Concepts/Object_Pooling.md) |
| Pre-warming | Creating pool objects at startup so runtime never allocates | [Object Pooling](../Concepts/Object_Pooling.md) |
| `IPoolable.Reset()` | Clears stale state when an object returns to the pool | [Interfaces](../Concepts/Interfaces.md) |
| GC spike avoidance | `Instantiate`/`Destroy` allocates heap memory that must be collected later | [Object Pooling](../Concepts/Object_Pooling.md) |
| Named pools | Dictionary lookup by string key so multiple prefabs share one manager | — |

---

## Code Roadmap

| File | Action | Notes |
|------|--------|-------|
| `Interfaces/IPoolable.cs` | **Already created** (Episode 02) | `Reset()` contract |
| `Systems/Managers/ObjectPoolManager.cs` | **Implement** | Singleton + named pools + pre-warm |
| `Enemies/Controllers/EnemyController.cs` | **Modify** `Die()` | Return to pool instead of Destroy |

---

## Architecture Context

`ObjectPoolManager` sits between **all** spawner/firing systems and their instantiated objects:

```
┌──────────────┐   Get(key, pos, rot)   ┌──────────────────┐
│ EnemySpawner │──────────────────────▶│                  │
└──────────────┘                       │                  │
                                       │ ObjectPoolManager│
┌──────────────┐   Get(key, pos, rot)   │  ┌────────┐     │
│ TowerFiring  │──────────────────────▶│  │ Pool A │     │
└──────────────┘                       │  │ Pool B │     │
                                       │  │ Pool C │     │
┌──────────────┐   Return(key, obj)    │  └────────┘     │
│ EnemyCtrl    │──────────────────────▶│                  │
│ .Die()       │                       │  IPoolable      │
└──────────────┘                       │  .Reset() called │
                                       │  on return       │
┌──────────────┐   ReturnDelayed()      └──────────────────┘
│ ProjectileBase│─────────────────────▶
│  (after VFX)  │
└──────────────┘
```

Every system that needs a runtime object goes through `ObjectPoolManager.Instance`. No one calls `Instantiate` or `Destroy` directly.

---

## Step-by-Step Implementation

### Step 1: The problem — `Instantiate`/`Destroy` creates GC spikes

Every time Unity instantiates a `GameObject`, it allocates memory on the managed heap. When you `Destroy` it, that memory becomes garbage. The **Garbage Collector** (GC) runs periodically to reclaim it.

In a tower defence game with dozens of enemies and hundreds of projectiles:

- **Before pooling:** Every projectile fire = `Instantiate`. Every enemy death = `Destroy`. The GC must collect all these allocations, causing **frame hitches** — visible stutters.
- **After pooling:** Objects are created once (pre-warmed), then activated/deactivated. No allocations at runtime. No GC spikes.

!!! tip "See it for yourself"
    Open the Unity Profiler, run the game without pooling, and watch the **GC Alloc** column spike every time enemies spawn or projectiles fire. Then implement pooling and compare — the spikes disappear.

---

### Step 2: The solution — reuse objects by enabling/disabling

Instead of `Instantiate` + `Destroy`:

```
Without pooling:
  Instantiate(prefab) → use → Destroy(gameObject) → GC collects

With pooling:
  Get(key) → SetActive(true) → use → SetActive(false) → Return(key)
       ▲                                          │
       └────────────── object is reused ◀──────────┘
```

The `ObjectPoolManager` maintains a `Dictionary<string, ObjectPool<GameObject>>`. Each pool is keyed by a string (e.g., `"enemy"`, `"arrow"`, `"bomb"`, `"hitEffect"`). Calling `Get()` fetches an inactive object, activates it, and returns it. Calling `Return()` deactivates it and puts it back.

---

### Step 3: Walk the `IPoolable` interface

Created in Episode 02, `IPoolable` defines the reset contract:

```csharp
namespace Interfaces
{
    public interface IPoolable
    {
        public void Reset();
    }
}
```

When `ObjectPoolManager.Return()` is called, it checks if the `GameObject` has any `IPoolable` components and calls `Reset()` on each. This ensures:

- `ProjectileBase.Reset()` clears `Target`, `HasTarget`, position, rotation
- `EnemyController` (once it implements `IPoolable`) would reset `CurrentWayPointIndex`, health, movement state
- Any pooled object starts with a **clean slate** for its next use

---

### Step 4: Walk the `ObjectPoolManager` skeleton

The full skeleton is in `Assets/Scripts/Managers/ObjectPoolManager.cs`:

```csharp
public class ObjectPoolManager : MonoBehaviour
{
    [SerializeField] private PoolConfig[] poolConfigs;

    private Dictionary<string, ObjectPool<GameObject>> _pools;
    private Dictionary<string, Transform> _poolContainers;

    public static ObjectPoolManager Instance { get; private set; }

    // --- Lifecycle ---
    private void Awake()
    {
        // TODO: Singleton setup — assign Instance, handle duplicate
        // TODO: Initialize _pools and _poolContainers dictionaries
        // TODO: Call PreWarmPools()
    }

    // --- Public API ---
    public GameObject Get(string key, Vector3 position, Quaternion rotation)
    {
        // TODO: Get pool from _pools by key
        // TODO: Fetch object from pool
        // TODO: Set position and rotation
        // TODO: SetActive(true)
        // TODO: Return the GameObject
    }

    public void Return(string key, GameObject obj)
    {
        // TODO: Get pool from _pools by key
        // TODO: Call IPoolable.Reset() if the object implements IPoolable
        // TODO: SetActive(false)
        // TODO: Return to pool
    }

    public void ReturnDelayed(string key, GameObject obj, float delay)
    {
        // TODO: Start coroutine to return after delay
    }
}
```

And the `PoolConfig` data class:

```csharp
[System.Serializable]
public class PoolConfig
{
    public string key;
    public GameObject prefab;
    public int defaultSize = 10;
    public int maxSize = 50;
}
```

**Key design decisions:**

- **Singleton** — global access via `ObjectPoolManager.Instance`. Every system (`EnemySpawner`, `TowerFiring`, `AudioController`) calls `Instance.Get()` / `Instance.Return()`.
- **Named pools** — `Dictionary<string, ObjectPool<GameObject>>` lets one manager handle enemies, projectiles, VFX, and audio sources.
- **`PoolConfig[]`** — configured in the Inspector. Each entry defines a pool's key, prefab, initial size, and max size.
- **Pre-warming** — `PreWarmPools()` creates all objects at startup during the loading screen, never during gameplay.

---

### Step 5: Create pool configs in the Inspector

On the `ObjectPoolManager` GameObject in the scene, configure the `poolConfigs` array:

| Key | Prefab | Default Size | Max Size |
|-----|--------|-------------|----------|
| `"enemy"` | Enemy prefab | 20 | 100 |
| `"arrow"` | Arrow projectile prefab | 30 | 200 |
| `"bomb"` | Bomb projectile prefab | 10 | 50 |
| `"hitEffect"` | Hit VFX prefab | 15 | 50 |
| `"audioSource"` | AudioPoolHandler prefab | 10 | 30 |

!!! warning "Max size matters"
    If the pool exceeds `maxSize`, Unity's `ObjectPool<T>` will **destroy** the overflow objects instead of returning them to the pool — exactly the allocation you're trying to avoid. Set `maxSize` high enough for your worst-case scenario (e.g., 200 arrows during a late-game rush).

---

### Step 6: Wire `EnemyController.Die()` to use pool Return

Currently `Die()` is a stub:

```csharp
public void Die()
{
    // TODO: Raise OnEnemyDeath event with GoldGiven value
    // TODO: Return to pool via ObjectPoolManager instead of Destroy
}
```

The implementation will replace `Destroy(gameObject)` with:

```csharp
public void Die()
{
    // TODO: Raise OnEnemyDeath event with GoldGiven value
    ObjectPoolManager.Instance.Return("enemy", gameObject);
}
```

This deactivates the enemy, calls `IPoolable.Reset()` if applicable, and puts it back in the `"enemy"` pool for the next spawn.

!!! info "EnemyController doesn't implement IPoolable yet"
    In a future episode you'll add `IPoolable` to `EnemyController` so its `Reset()` method clears `CurrentWayPointIndex`, re-initializes health, etc. For now, the `Return()` call still works — it just won't reset enemy state until `IPoolable` is implemented.

---

### Step 7: When NOT to pool

Object pooling isn't always the right choice:

| Scenario | Why Not Pool? | What To Do Instead |
|----------|--------------|-------------------|
| **Rarely created objects** | A boss that spawns once per 10 waves — pooling overhead exceeds benefit | Use `Instantiate`/`Destroy` normally |
| **Complex state** | Objects with many interdependent components where `Reset()` is error-prone | Keep using `Instantiate`/`Destroy` |
| **Unique entities** | The player character, game manager — there's only one, ever | Singleton or scene-loaded, never pooled |
| **Varied prefabs** | 50 different VFX prefabs with distinct behaviours — a pool per prefab is wasteful | Pool the common ones, instantiate the rare ones |

The rule of thumb: **pool what you spawn frequently** (projectiles, enemies, hit effects, audio sources). Don't pool what you spawn rarely or once.

---

## Episode Recap

- `Instantiate`/`Destroy` allocates heap memory → GC must collect → frame hitches
- Object pooling reuses objects by activating/deactivating them instead of creating/destroying
- `ObjectPoolManager` is a singleton with named pools (`Dictionary<string, ObjectPool<GameObject>>`)
- `PoolConfig` defines each pool's key, prefab, default size, and max size
- `PreWarmPools()` creates all objects at startup — zero runtime allocation
- `Return()` calls `IPoolable.Reset()` to clear stale state before the object is reused
- `ReturnDelayed()` handles VFX/audio that needs time to finish before returning
- Pool frequently-spawned objects (enemies, projectiles, effects, audio); don't pool rare or unique objects
- `EnemyController.Die()` will call `ObjectPoolManager.Instance.Return("enemy", gameObject)` instead of `Destroy`

---

## Challenge

What happens if you forget to call `IPoolable.Reset()` on a pooled enemy?

Describe the specific bug you'd see in each of these scenarios:

1. **Health not reset** — An enemy with 200 HP is killed when it has 30 HP remaining. It returns to the pool. Next time it's spawned, what HP does it start with? What does the health bar show?

2. **Waypoint index not reset** — An enemy reaches waypoint 8 out of 12 before dying. It returns to the pool without resetting `CurrentWayPointIndex`. Next spawn, where does it start moving from?

3. **Event listeners not reset** — `HealthStrategy.OnDeath` still has subscribers from the previous enemy's death. When this recycled enemy takes damage and reaches 0 HP, what happens?

4. **Visual state not reset** — The enemy was playing a "taking damage" flash animation when it died. It's returned to the pool with the flash still active. When a player sees this enemy spawn, what do they notice?

Think about how `IPoolable.Reset()` prevents each of these. For a deeper look at the pooling pattern, see [Object Pooling](../Concepts/Object_Pooling.md).