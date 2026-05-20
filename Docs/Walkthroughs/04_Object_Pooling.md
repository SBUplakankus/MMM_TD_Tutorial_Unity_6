# Episode 4: Object Pooling — Implementation Guide

## What You're Building
A complete ObjectPoolManager using Unity's built-in `UnityEngine.Pool.ObjectPool<T>`. Pre-warms pools at Awake, provides Get/Return/ReturnDelayed API, resets pooled objects via IPoolable. Then update EnemyController to implement IPoolable — clearing strategy instances and unsubscribing from events before returning to pool.

**Critical architecture note — service locator, NOT singleton:**
- ObjectPoolManager NO LONGER has a singleton (`Instance` property removed)
- Instead: registers itself in `Services` during `Awake()`: `Services.Register<ObjectPoolManager>(this);`
- All access via `Services.Get<ObjectPoolManager>()` — not `ObjectPoolManager.Instance`
- No `DontDestroyOnLoad` — the bootstrapper owns the lifecycle

**Critical architecture note — strategy reset:**
With the interface-based strategy pattern, `IHealthStrategy` and `IMovementStrategy` instances hold per-instance state (`_currentHealth` in NormalHealth, waypoint index/timer in GroundedPath, etc.). If `IPoolable.Reset()` doesn't clear these references, a pooled enemy reused from the pool will have a stale Health strategy with a previous enemy's `_currentHealth`. This is even more critical than the old SO architecture because the old code could paper over this with `Instantiate(strategySO)` — now the strategy instances are plain C# objects that only get replaced when `Initialize()` is called again.

## Files & Order
1. `Assets/Scripts/Systems/Managers/ObjectPoolManager.cs` — implement all TODOs, register in Services
2. `Assets/Scripts/Enemies/Controllers/EnemyController.cs` — implement IPoolable, update Die(), update TakeDamage

## Implementation

### File: `Assets/Scripts/Systems/Managers/ObjectPoolManager.cs`

The skeleton already exists with TODOs. Here's the complete implementation with service locator registration:

```csharp
using System.Collections;
using System.Collections.Generic;
using Core;
using Interfaces;
using UnityEngine;
using UnityEngine.Pool;

namespace Systems.Managers
{
    public class ObjectPoolManager : MonoBehaviour
    {
        #region Fields

        [SerializeField] private PoolConfig[] poolConfigs;

        private Dictionary<string, ObjectPool<GameObject>> _pools;
        private Dictionary<string, Transform> _poolContainers;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _pools = new Dictionary<string, ObjectPool<GameObject>>();
            _poolContainers = new Dictionary<string, Transform>();

            Services.Register<ObjectPoolManager>(this);

            PreWarmPools();
        }

        #endregion

        #region Public API

        public GameObject Get(string key, Vector3 position, Quaternion rotation)
        {
            var pool = _pools[key];
            var obj = pool.Get();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }

        public void Return(string key, GameObject obj)
        {
            var pool = _pools[key];
            pool.Release(obj);
        }

        public void ReturnDelayed(string key, GameObject obj, float delay)
        {
            StartCoroutine(ReturnDelayedRoutine(key, obj, delay));
        }

        #endregion

        #region Private Methods

        private void PreWarmPools()
        {
            var keys = new HashSet<string>();

            foreach (var config in poolConfigs)
            {
                if (string.IsNullOrEmpty(config.key))
                {
                    Debug.LogError($"PoolConfig has empty key for prefab {config.prefab.name}");
                    continue;
                }

                if (!keys.Add(config.key))
                {
                    Debug.LogError($"Duplicate pool key: {config.key}");
                    continue;
                }

                var container = new GameObject($"Pool_{config.key}").transform;
                container.SetParent(transform);
                _poolContainers[config.key] = container;

                var pool = CreatePool(config, container);
                _pools[config.key] = pool;

                var preWarmObjects = new List<GameObject>(config.defaultSize);
                for (var i = 0; i < config.defaultSize; i++)
                {
                    var obj = pool.Get();
                    preWarmObjects.Add(obj);
                }

                foreach (var obj in preWarmObjects)
                {
                    pool.Release(obj);
                }
            }
        }

        private ObjectPool<GameObject> CreatePool(PoolConfig config, Transform parent)
        {
            return new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(config.prefab, parent);
                    return obj;
                },
                actionOnGet: obj =>
                {
                    obj.SetActive(true);
                },
                actionOnRelease: obj =>
                {
                    obj.SetActive(false);
                    var poolables = obj.GetComponentsInChildren<IPoolable>();
                    foreach (var poolable in poolables)
                    {
                        poolable.Reset();
                    }
                },
                actionOnDestroy: obj =>
                {
                    Destroy(obj);
                },
                defaultCapacity: config.defaultSize,
                maxSize: config.maxSize
            );
        }

        private IEnumerator ReturnDelayedRoutine(string key, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null && obj.activeInHierarchy)
            {
                Return(key, obj);
            }
        }

        #endregion
    }

    [System.Serializable]
    public class PoolConfig
    {
        public string key;
        public GameObject prefab;
        public int defaultSize = 10;
        public int maxSize = 50;
    }
}
```

**Key changes from the old singleton version:**

| Change | Old (Singleton) | New (Service Locator) |
|--------|-----------------|----------------------|
| Access pattern | `ObjectPoolManager.Instance` | `Services.Get<ObjectPoolManager>()` |
| Registration | `Instance = this` in Awake | `Services.Register<ObjectPoolManager>(this)` in Awake |
| Singleton check | `if (Instance != null && Instance != this) Destroy(gameObject)` | Removed — GameBootstrapper owns lifecycle |
| DontDestroyOnLoad | Optional, some versions used it | Removed — not needed for single-scene TD |
| Null check | `if (Instance == null)` | `Services.Get<T>()` throws `KeyNotFoundException` if not registered |

**Why the singleton check was removed:** The old singleton pattern guarded against duplicate ObjectPoolManagers by destroying the duplicate at runtime. With GameBootstrapper owning the scene, you simply don't put two ObjectPoolManagers in the scene. If you do, `Services.Register` will overwrite the first registration — which is a bug you'll catch immediately (the second registration wins, the first pool's references are lost). This fails fast rather than silently.

**Why `Services.Register<ObjectPoolManager>(this)` is in ObjectPoolManager.Awake(), not GameBootstrapper.Awake():** MonoBehaviour services self-register. This is the pattern: GameBootstrapper holds the serialized reference to ObjectPoolManager (so Unity can wire it in the Inspector), but ObjectPoolManager registers *itself* in Services during its own Awake. This way ObjectPoolManager doesn't need a public method that GameBootstrapper must remember to call — registration is automatic, and Awake() order is guaranteed before any OnEnable/Start.

**Everything else is the same logic:** PreWarmPools, Get, Return, ReturnDelayed, CreatePool, ReturnDelayedRoutine — identical to the singleton version. Only the access pattern changed.

**Notes on design decisions (unchanged from previous version):**

**Why `Dictionary<string, ObjectPool<GameObject>>`:** String keys are the simplest approach. The alternative would be `Dictionary<GameObject, ObjectPool<GameObject>>` (prefab as key) or enum keys. String keys keep the Inspector simple and make the API readable: `Get("enemy", pos, rot)`.

**PreWarmPools allocates a List:** The `preWarmObjects` list prevents Get/Release from interfering. If we did `pool.Get(); pool.Release(obj);` in a loop, the same object might be returned by Get and then double-released. By collecting all objects first, then releasing them all, we avoid this.

**Pool key validation:** Added `HashSet<string>` to catch duplicate keys and empty keys at startup instead of having silent overwrites in the dictionary.

**actionOnRelease calls IPoolable.Reset():** When an object returns to the pool, we immediately reset all IPoolable components. This is the simplest approach — the object is clean before it even sits in the pool.

**ReturnDelayed checks `activeInHierarchy`:** If the object was already returned manually before the delay elapsed, we skip the double-return.

**Gotcha: Get() does NOT call IPoolable.Reset():** Reset happens in `actionOnRelease` (when the object enters the pool). So by the time `Get()` returns the object, it's already been reset. If you need to set specific data AFTER getting (like calling `enemy.Initialize()`), you do that outside the pool.

**Gotcha: createFunc sets parent to container:** Instantiated objects are parented under `Pool_enemy`, `Pool_arrow`, etc. When objects are active (Get'd), they stay under the container.

### File: `Assets/Scripts/Enemies/Controllers/EnemyController.cs`

EnemyController now implements IPoolable. This is **critical** because the interface-based strategies hold per-instance state. `Reset()` must clear strategy references and unsubscribe from events.

```csharp
using Core;
using Data;
using Enemies.Components;
using Events.Registries;
using Interfaces;
using Systems.Game;
using Systems.Managers;
using Systems.Parsing;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable, IPoolable
    {
        #region Fields

        [Header("Enemy UI")]
        [SerializeField] private EnemyHealthBar healthBar;

        [SerializeField] private string poolKey = "enemy";

        #endregion

        #region Properties

        public EnemyPath Path { get; private set; }
        public int CurrentWayPointIndex { get; set; }
        public IHealthStrategy Health { get; private set; }
        public IMovementStrategy Movement { get; private set; }
        public int GoldGiven { get; private set; }
        public int Damage { get; private set; }

        public Vector3 Position => transform.position;
        public bool IsAlive => Health != null && Health.IsAlive;

        #endregion

        #region Class Methods

        public void Initialize(EnemyData data, EnemyPath path)
        {
            Path = path;
            GoldGiven = data.GoldGiven;
            Damage = data.Damage;

            Health = StrategyFactory.CreateHealth(data.HealthConfig);
            Movement = StrategyFactory.CreateMovement(data.MovementConfig);

            Health.Initialize();
            Movement.Initialize(this);

            Movement.OnMovementCompleted += OnReachedEnd;
        }

        public void Die()
        {
            Services.Get<CombatEvents>().EnemyDeath.Raise(GoldGiven);

            Movement.OnMovementCompleted -= OnReachedEnd;

            Services.Get<ObjectPoolManager>().Return(poolKey, gameObject);
        }

        #endregion

        #region Unity Methods

        private void Update()
        {
            Health.Tick(Time.deltaTime);
            Movement.Tick(this);
        }

        #endregion

        #region IDamageable

        public void TakeDamage(float damage)
        {
            var result = Health.TakeDamage(damage);

            if (healthBar != null)
            {
                healthBar.UpdateHealth(Health.CurrentHealth, Health.MaxHealth);
            }

            if (result.Died)
            {
                Die();
            }
        }

        #endregion

        #region IPoolable

        public void Reset()
        {
            if (Movement != null)
            {
                Movement.OnMovementCompleted -= OnReachedEnd;
            }

            Health = null;
            Movement = null;
            Path = null;
            CurrentWayPointIndex = 0;
            GoldGiven = 0;
            Damage = 0;

            if (healthBar != null)
            {
                healthBar.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Private Methods

        private void OnReachedEnd()
        {
            Services.Get<CombatEvents>().EnemyReachedEnd.Raise(Damage);
            Services.Get<ObjectPoolManager>().Return(poolKey, gameObject);
        }

        #endregion
    }
}
```

**Key changes from the old singleton version:**

| Change | Old (Singleton) | New (Service Locator) |
|--------|-----------------|----------------------|
| `Die()` access | `ObjectPoolManager.Instance.Return(...)` | `Services.Get<ObjectPoolManager>().Return(...)` |
| `Die()` events | No event raised (old version didn't have registries) | `Services.Get<CombatEvents>().EnemyDeath.Raise(GoldGiven)` |
| `OnReachedEnd()` access | `ObjectPoolManager.Instance.Return(...)` | `Services.Get<ObjectPoolManager>().Return(...)` |
| `OnReachedEnd()` events | No event raised | `Services.Get<CombatEvents>().EnemyReachedEnd.Raise(Damage)` |
| `IPoolable.Reset()` | Same logic | Added healthBar deactivation |

**Changes from the previous walkthrough version:**

1. **Services.Get<T>() instead of Instance.** Every access to ObjectPoolManager and event registries goes through the service locator. No more static singleton references.
2. **Die() raises CombatEvents.** Before returning to pool, Die() raises `EnemyDeath` with the gold reward. This is how PlayerStats (and later AudioController) learn about the kill.
3. **OnReachedEnd() raises CombatEvents.** Before returning to pool, raises `EnemyReachedEnd` with the damage value. PlayerStats subscribes to this to subtract lives.
4. **Reset() clears strategy instances.** Sets `Health = null` and `Movement = null` — the old strategy instances (NormalHealth, GroundedPath, etc.) have no remaining references and become eligible for GC. Next `Get()` + `Initialize()` creates fresh instances via StrategyFactory.
5. **Reset() deactivates healthBar.** If the health bar child is still active when the enemy returns to pool, it renders at (0,0,0) with stale data.

**Why Reset() must clear strategy references:**

The interface-based architecture means each enemy owns a *unique* strategy instance (NormalHealth, ArmouredHealth, GroundedPath, etc.). These are plain C# objects created by `StrategyFactory` — not shared ScriptableObjects. When an enemy is returned to the pool:

1. `Die()` unsubscribes from `Movement.OnMovementCompleted`
2. `ObjectPoolManager.Return()` → `actionOnRelease` → `SetActive(false)` → `IPoolable.Reset()`
3. `Reset()` sets `Health = null`, `Movement = null`, `Path = null`, index = 0, gold = 0, damage = 0
4. The old strategy instances (NormalHealth, etc.) have no remaining references → eligible for GC
5. Next `Get()` + `Initialize()` creates *fresh* strategy instances via StrategyFactory

**Important: if Reset() doesn't clear the IHealthStrategy instance, pooled enemies retain old health state.** This is the single most common pooling bug with interface strategies. A pooled enemy that gets `Get()`'d but never `Initialize()`'d would retain a stale Health strategy with the previous enemy's `_currentHealth`. Code that reads `IsAlive` between pool Get and Initialize (e.g., TowerDetection scanning during that window) would get a phantom result from the dead enemy's Health strategy.

**Why Health.Tick(Time.deltaTime) is in Update():** IHealthStrategy has a `Tick(float deltaTime)` method for RegenHealth. NormalHealth and ArmouredHealth have empty Tick implementations, but calling it every frame is harmless and avoids a per-type check.

**Calling order for pooled enemy death:**

```
EnemyController.Die()
  → Services.Get<CombatEvents>().EnemyDeath.Raise(GoldGiven)  // 1. Notify listeners
  → Movement.OnMovementCompleted -= OnReachedEnd              // 2. Unsubscribe
  → Services.Get<ObjectPoolManager>().Return(poolKey, gameObject) // 3. Return to pool
      → pool.Release(obj)
          → actionOnRelease(obj)
              → obj.SetActive(false)                           // 4. Deactivate
              → IPoolable.Reset()                              // 5. Clear all state
                  → Movement.OnMovementCompleted -= OnReachedEnd // 6. Double-guard unsubscribe
                  → Health = null, Movement = null, etc.        // 7. Clear strategies
```

The `OnMovementCompleted` unsubscribe happens twice (step 2 and step 6). This is intentional — Die() must unsubscribe BEFORE the pool Release because the Release triggers Reset() asynchronously from Die()'s perspective. The double-guard is harmless (`-= ` on an already-removed handler is a no-op).

## How Other Systems Access the Pool

For reference, here's how other systems use pool access after the service locator migration:

**EnemySpawner uses `Services.Get<ObjectPoolManager>().Get(...)`:**
```csharp
// In EnemySpawner.SpawnEnemyRoutine()
var obj = Services.Get<ObjectPoolManager>().Get("enemy", path.StartPosition, Quaternion.identity);
var enemy = obj.GetComponent<EnemyController>();
enemy.Initialize(enemyDataLookup[entry.EnemyId], path);
```

**ProjectileBase.ReturnToPool() uses `Services.Get<ObjectPoolManager>()`:**
```csharp
protected void ReturnToPool()
{
    Services.Get<ObjectPoolManager>().Return(poolKey, gameObject);
}
```

**AudioController uses `Services.Get<ObjectPoolManager>()` for audio source pooling:**
```csharp
// In AudioController.OnEnemyDeath()
var audioSource = Services.Get<ObjectPoolManager>().Get("audioSource", transform.position, Quaternion.identity);
```

## Unity Editor Setup

### Create ObjectPoolManager in Scene

1. In the Hierarchy, under `Managers/`, create empty GameObject named `ObjectPoolManager`
2. Add `ObjectPoolManager` component
3. Configure PoolConfig array:

| Key | Prefab | Default Size | Max Size |
|-----|--------|-------------|----------|
| `enemy` | Enemy prefab | 20 | 100 |
| `arrow` | ArrowProjectile prefab | 30 | 200 |
| `bomb` | BombProjectile prefab | 10 | 50 |
| `hitEffect` | Hit VFX prefab | 15 | 50 |
| `audioSource` | AudioSource prefab | 10 | 30 |

**Creating the pool config entries:**
1. Select the ObjectPoolManager GameObject
2. In the Inspector, find the Pool Configs array
3. Set Size to `5`
4. Fill each element:
   - Element 0: key = `enemy`, prefab = (drag Enemy prefab), defaultSize = `20`, maxSize = `100`
   - Element 1: key = `arrow`, prefab = (drag ArrowProjectile prefab), defaultSize = `30`, maxSize = `200`
   - Element 2: key = `bomb`, prefab = (drag BombProjectile prefab), defaultSize = `10`, maxSize = `50`
   - Element 3: key = `hitEffect`, prefab = (drag VFX prefab if you have one), defaultSize = `15`, maxSize = `50`
   - Element 4: key = `audioSource`, prefab = (drag AudioSource prefab), defaultSize = `10`, maxSize = `30`

**AudioSource prefab setup:**
1. Create empty GameObject named `PooledAudioSource`
2. Add `AudioSource` component
3. Add `AudioPoolHandler` component
4. Drag to `Assets/Prefabs/` to make a prefab
5. Delete from scene
6. Assign to the `audioSource` pool config

### Wire ObjectPoolManager into GameBootstrapper

1. Select the `GameBootstrapper` GameObject
2. Drag `Managers/ObjectPoolManager` into the `objectPoolManager` serialized field
3. This ensures GameBootstrapper has the reference — ObjectPoolManager self-registers in its own Awake()
4. **Verification:** ObjectPoolManager must appear BEFORE GameBootstrapper in Script Execution Order if GameBootstrapper.Awake() needs `Services.Get<ObjectPoolManager>()`. In practice, the bootstrapper only registers other services in Awake(), so default order is fine.

### Verify Hierarchy After Play

After playing, you should see under `Managers/ObjectPoolManager/`:
```
ObjectPoolManager
├── Pool_enemy         (contains 20 inactive enemy clones)
├── Pool_arrow         (contains 30 inactive arrow clones)
├── Pool_bomb          (contains 10 inactive bomb clones)
├── Pool_hitEffect     (contains 15 inactive VFX clones)
└── Pool_audioSource   (contains 10 inactive audio clones)
```

Each container holds `defaultSize` inactive children. When you `Get()` an object, it becomes active and stays under the container. When you `Return()`, it deactivates in place.

## Test Plan

### Test 1: Pre-warm Creates Pools
1. Play
2. Check Hierarchy — 5 container GameObjects appear under ObjectPoolManager
3. Expand `Pool_enemy` — should contain 20 inactive GameObjects named `Enemy (Clone)`
4. Stop — containers and clones are cleaned up

### Test 2: Service Registration Works
1. Play
2. Open Console
3. Verify no `KeyNotFoundException` — ObjectPoolManager.Awake() registered via `Services.Register<ObjectPoolManager>(this)`
4. In a test script: `Debug.Log(Services.Get<ObjectPoolManager>().name);` — should print `ObjectPoolManager`
5. Verify that `ObjectPoolManager.Instance` does NOT exist (it's been removed)

### Test 3: Enemy Returns to Pool Instead of Destroying
1. Play
2. Spawn an enemy (via test script or manual Instantiate)
3. Call `enemy.Initialize(data, path)` — this creates fresh strategy instances via StrategyFactory
4. Kill the enemy (call TakeDamage until Health.TakeDamage returns Died)
5. Verify: enemy GameObject is STILL in Hierarchy but inactive
6. Verify: enemy is under `Pool_enemy` container
7. Verify: no new clones were instantiated (pool reused the deactivated one)

### Test 4: Enemy Reuse Works Correctly (Strategy Reset Verification)
1. After Test 3, spawn another enemy from pool
2. `var enemy = Services.Get<ObjectPoolManager>().Get("enemy", path.StartPosition, Quaternion.identity);`
3. Get EnemyController component, call `Initialize(data, path)` — creates fresh strategy instances
4. Verify: enemy appears at WP0, moves correctly, has full health
5. **Critical check:** Verify the reused enemy's health is at `startHealth` (e.g., 100), NOT at the previous enemy's death value (0). This proves Reset() + Initialize() correctly replaces strategy instances.

### Test 5: CombatEvents Fire on Death
1. Play
2. Subscribe to `Services.Get<CombatEvents>().EnemyDeath` with a debug log
3. Spawn an enemy, kill it
4. Verify the event fires with the correct gold reward
5. Verify the enemy is returned to pool (not Destroy'd)

### Test 6: ReturnDelayed Works
1. Create a test that calls `Services.Get<ObjectPoolManager>().ReturnDelayed("enemy", enemyGo, 2f)`
2. Verify: enemy stays active for 2 seconds
3. Verify: after 2 seconds, enemy deactivates
4. This simulates how projectile VFX will work (play animation, then auto-return)

### Test 7: Pool Max Size Behavior
1. Set `enemy` max size to `3` temporarily
2. Try to Get 5 enemies simultaneously
3. First 3: instantiated and returned from pool
4. 4th and 5th: new instances created by createFunc
5. Return all 5: first 3 go to pool, 4th and 5th hit maxSize → `actionOnDestroy` called → Destroyed
6. Check Profiler for GC.Alloc from Destroy calls — this tells you max is too low

### Test 8: Event Subscription Leak Test
1. Spawn two enemies in sequence (spawn first, kill it, spawn second from the same pool slot)
2. Add a debug log in `OnReachedEnd()` that prints the enemy's instance ID
3. Let the second enemy reach the end of the path
4. Verify: `OnReachedEnd` fires only ONCE for the second enemy
5. If it fires twice, `Reset()` didn't unsubscribe from the old Movement strategy — the old event handler leaked.

### Test 9: Pool Key Mismatch
1. Try `Services.Get<ObjectPoolManager>().Get("enemmy", pos, rot)` (typo)
2. Expected: `KeyNotFoundException`
3. This proves the key must exactly match the PoolConfig

### Test 10: Services.Get Throws When Not Registered
1. Stop the scene (which calls `Services.Clear()` in GameBootstrapper.OnDestroy)
2. Try `Services.Get<ObjectPoolManager>()` from a script that outlives the scene
3. Expected: `KeyNotFoundException` — proves cleanup works

## Debugging Tips

| Symptom | Cause | Fix |
|---------|-------|-----|
| `KeyNotFoundException` from `Services.Get<ObjectPoolManager>()` | ObjectPoolManager not registered — forgot `Services.Register` in Awake, or GameBootstrapper not in scene | Ensure ObjectPoolManager.Awake() calls `Services.Register<ObjectPoolManager>(this)`. Ensure GameBootstrapper is in the scene. |
| `KeyNotFoundException` from `Services.Get<CombatEvents>()` | Event registry not registered in GameBootstrapper | Add `Services.Register<CombatEvents>(new CombatEvents())` to GameBootstrapper.Awake() |
| Events not firing after scene reload | Forgot to Clear() registries in OnDestroy, or forgot to re-subscribe in OnEnable | GameBootstrapper.OnDestroy() must call `Clear()` on all registries then `Services.Clear()`. Subscribers re-subscribe in OnEnable. |
| Pooled enemy has old health value | `Reset()` not clearing IHealthStrategy instance | Ensure `Reset()` sets `Health = null`. Without this, `IsAlive` reads from the stale strategy. |
| Pooled enemy has old movement state | `Reset()` not clearing IMovementStrategy instance | Ensure `Reset()` sets `Movement = null`. Waypoint index must reset to 0. |
| KeyNotFoundException on Get/Return | Pool key typo | Keys must exactly match PoolConfig.key (case-sensitive) |
| Objects not reset when reused | Missing IPoolable.Reset() implementation | Ensure EnemyController implements IPoolable with Reset() |
| Double-release crash | Object returned to pool twice | Check that Die() isn't called twice. Add guard: `if (!gameObject.activeInHierarchy) return;` |
| Destroy called during gameplay | maxSize too low for your wave size | Increase maxSize in PoolConfig, or check how many objects are active simultaneously |
| GC.Alloc spikes from Destroy | Pool overflow (maxSize too low) | Open Profiler → Memory, filter for GC.Alloc, check if Destroy is being called during waves |
| Enemy keeps moving after Return | Update() runs on inactive objects — NO, Unity doesn't call Update on inactive GameObjects | If this happens, something else is calling Tick. Check UpdateManager if you've wired enemies to it |
| ReturnDelayed doesn't execute | Scene loaded or coroutine cancelled | Coroutines stop when the MonoBehaviour is disabled/destroyed. Ensure ObjectPoolManager stays active. |
| Enemy appears at wrong position after Get | Position set in Get() but then Initialize() overrides it | Set position AFTER Get, Initialize sets position to WP0 — this is correct behavior |
| Pool containers missing in Hierarchy | PreWarmPools() didn't run | Check that ObjectPoolManager.Awake() is called (is the component enabled? Is the GameObject active?) |
| Pre-warmed objects visible briefly on Play | createFunc instantiates active objects | Objects are created active, then immediately Released (deactivated). You may see 1 frame of flashing — cosmetic only |
| OnMovementCompletion fires twice | Event not unsubscribed in Reset() or Die() | Both Die() and Reset() must unsubscribe. Die() goes first, Reset() is the safety net |
| IsAlive true for pooled zombie enemy | Health strategy not null in Reset() | Reset() must set Health = null. IsAlive checks `Health != null && Health.IsAlive` — null makes it safely false |
| CombatEvents.EnemyDeath not firing | Die() not raising the event, or CombatEvents not registered | Check Die() calls `Services.Get<CombatEvents>().EnemyDeath.Raise()`. Check GameBootstrapper registers CombatEvents. |
| `ObjectPoolManager.Instance` compile error | Old singleton code still referenced | Replace all `ObjectPoolManager.Instance` with `Services.Get<ObjectPoolManager>()` |

### Advanced: Creating Objects Inactive

If you see the 1-frame flash during pre-warm, modify `createFunc`:

```csharp
createFunc: () =>
{
    var obj = Instantiate(config.prefab, parent);
    obj.SetActive(false);
    return obj;
},
```

This creates objects already inactive. Unity's ObjectPool will call `actionOnGet` (SetActive(true)) when you Get them, and `actionOnRelease` (SetActive(false)) when you Return them. Pre-warm will Get them (activating), then Release them (deactivating). You'll still see a brief flash during pre-warm, but it's contained to Awake.

The real fix for the flash is to pre-warm BEFORE the first frame renders, which Awake already does. The flash happens within a single frame's Awake, so it won't be visible unless you have a very expensive OnEnable on the prefab.

### Advanced: Why IPoolable.Reset() Is More Critical With Interface Strategies

In the old ScriptableObject architecture, strategies were shared SO references. A bug in Reset() might mean two enemies share the same `_currentHealth` field — bad, but bounded. The damage was limited by the SO's lifecycle.

With interface-based strategies, each enemy owns its own `NormalHealth` instance. If Reset() doesn't clear `Health = null`, the pooled enemy retains a fully-functional strategy object from its previous life. Code that reads `IsAlive` between pool Get and Initialize will get the *old* enemy's health status. An enemy that died with 0 HP could appear "dead" before Initialize, or worse — if the old enemy had positive HP due to regen or shield, a "phantom alive" enemy could be targeted by towers while sitting inactive in the pool.

**The fix is always the same:** `Reset()` clears all per-instance state to null/zero, and `Initialize()` is *always* called after `Get()`. Between these two contracts, no stale state can leak.

### Advanced: Migration Checklist — Singleton to Service Locator

If you have existing code that still uses the old singleton pattern, here's the full migration:

| File | Old Code | New Code |
|------|----------|----------|
| ObjectPoolManager.cs | `public static ObjectPoolManager Instance { get; private set; }` | Removed — no singleton |
| ObjectPoolManager.cs | `Instance = this;` in Awake | `Services.Register<ObjectPoolManager>(this);` in Awake |
| ObjectPoolManager.cs | `if (Instance != null && Instance != this) Destroy(gameObject);` | Removed |
| EnemyController.cs | `ObjectPoolManager.Instance.Return(poolKey, gameObject)` | `Services.Get<ObjectPoolManager>().Return(poolKey, gameObject)` |
| EnemySpawner.cs | `ObjectPoolManager.Instance.Get("enemy", ...)` | `Services.Get<ObjectPoolManager>().Get("enemy", ...)` |
| ProjectileBase.cs | `ObjectPoolManager.Instance.Return(poolKey, gameObject)` | `Services.Get<ObjectPoolManager>().Return(poolKey, gameObject)` |
| AudioController.cs | `ObjectPoolManager.Instance.Get("audioSource", ...)` | `Services.Get<ObjectPoolManager>().Get("audioSource", ...)` |
| Any file | `using Systems.Managers;` for pool access | `using Core;` + `using Systems.Managers;` |