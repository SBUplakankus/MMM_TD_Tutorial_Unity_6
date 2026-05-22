# Episode 08: Object Pooling

## What You're Building

Replace `Instantiate`/`Destroy` with object pooling. Enemies and projectiles are deactivated, reset, and reactivated — zero allocations during runtime.

## The Problem

Open the Profiler (Window > Analysis > Profiler). Run a wave with 20 enemies and 30+ projectiles. You'll see GC.Alloc spikes every time an enemy spawns or dies, every time a projectile is fired or impacts. These spikes cause frame drops that grow with object count.

`Instantiate` allocates managed + native memory. `Destroy` frees it, triggering garbage collection. The fix: create all objects once at startup (pre-warm), then reuse them by deactivating and reactivating.

## IPoolable.cs

```csharp
namespace Interfaces
{
    public interface IPoolable
    {
        void Reset();
    }
}
```

Called when an object returns to the pool. Clears runtime state so the next reuse starts clean.

## ObjectPoolManager.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using Interfaces;
using UnityEngine;
using UnityEngine.Pool;

namespace Systems.Managers
{
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        [SerializeField] private PoolConfig[] poolConfigs;

        private Dictionary<string, ObjectPool<GameObject>> _pools;
        private Dictionary<string, Transform> _poolContainers;

        private void Awake()
        {
            Instance = this;
            _pools = new Dictionary<string, ObjectPool<GameObject>>();
            _poolContainers = new Dictionary<string, Transform>();
            PreWarmPools();
        }

        public GameObject Get(string key, Vector3 position, Quaternion rotation)
        {
            var pool = GetPool(key);
            GameObject obj = pool.Get();
            obj.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }

        public void Return(string key, GameObject obj)
        {
            IPoolable poolable = obj.GetComponent<IPoolable>();
            poolable?.Reset();
            GetPool(key).Release(obj);
        }

        public void ReturnDelayed(string key, GameObject obj, float delay)
        {
            StartCoroutine(ReturnDelayedRoutine(key, obj, delay));
        }

        private ObjectPool<GameObject> GetPool(string key)
        {
            if (!_pools.TryGetValue(key, out var pool))
                throw new System.ArgumentException($"No pool with key '{key}'.");
            return pool;
        }

        private void PreWarmPools()
        {
            foreach (PoolConfig config in poolConfigs)
            {
                Transform container = new GameObject($"Pool_{config.key}").transform;
                container.SetParent(transform);
                _poolContainers[config.key] = container;

                var pool = new ObjectPool<GameObject>(
                    createFunc: () => CreatePooledObject(config),
                    actionOnGet: obj => obj.SetActive(true),
                    actionOnRelease: obj =>
                    {
                        obj.SetActive(false);
                        obj.transform.SetParent(_poolContainers[config.key]);
                    },
                    actionOnDestroy: obj => Destroy(obj),
                    defaultCapacity: config.defaultSize,
                    maxSize: config.maxSize
                );

                _pools[config.key] = pool;

                GameObject[] preWarm = new GameObject[config.defaultSize];
                for (int i = 0; i < config.defaultSize; i++)
                    preWarm[i] = pool.Get();
                for (int i = 0; i < config.defaultSize; i++)
                    pool.Release(preWarm[i]);
            }
        }

        private GameObject CreatePooledObject(PoolConfig config)
        {
            GameObject obj = Instantiate(config.prefab);
            obj.SetActive(false);
            obj.transform.SetParent(_poolContainers[config.key]);
            return obj;
        }

        private IEnumerator ReturnDelayedRoutine(string key, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Return(key, obj);
        }
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

**Key points:**
- Uses `Instance` singleton — Episode 9 replaces with Services
- `PreWarmPools` runs in `Awake`: creates and immediately releases `defaultSize` objects, forcing all startup allocations
- The 4 `ObjectPool` delegates: create (Instantiate), get (SetActive true), release (SetActive false + reparent), destroy (when pool exceeds maxSize)
- `Return` calls `IPoolable.Reset()` before releasing — ensures clean state

## EnemyController.cs (pool integration)

```csharp
using Data;
using Interfaces;
using Systems.Game;
using Systems.Managers;
using Systems.Parsing;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable, IPoolable
    {
        [SerializeField] private EnemyHealthBar healthBar;

        public EnemyPath Path { get; private set; }
        public int CurrentWayPointIndex { get; set; }
        public IHealthStrategy Health { get; private set; }
        public IMovementStrategy Movement { get; private set; }
        private int _goldGiven;
        private int _damage;

        // ITargetable
        public Vector3 Position => transform.position;
        public bool IsAlive => Health != null && Health.IsAlive;

        public void Initialize(EnemyData data, EnemyPath path)
        {
            Path = path;
            Health = StrategyFactory.CreateHealth(data.HealthConfig);
            Movement = StrategyFactory.CreateMovement(data.MovementConfig);
            _goldGiven = data.GoldGiven;
            _damage = data.Damage;

            Health.Initialize();
            Movement.Initialize(this);
            Movement.OnMovementCompleted += OnReachedEnd;
        }

        private void Update()
        {
            if (!IsAlive) return;

            Health.Tick(Time.deltaTime);
            Movement.Tick(this);

            if (healthBar != null)
            {
                healthBar.SetHealth(Health.CurrentHealth, Health.MaxHealth);
                healthBar.SetPosition(transform.position);
            }
        }

        public void TakeDamage(float damage)
        {
            DamageResult result = Health.TakeDamage(damage);
            if (result.Died)
                Die();
        }

        private void Die()
        {
            PlayerStats.Instance.AddGold(_goldGiven);
            Movement.OnMovementCompleted -= OnReachedEnd;
            ObjectPoolManager.Instance.Return("enemy", gameObject);
        }

        private void OnReachedEnd()
        {
            PlayerStats.Instance.SubtractLives(_damage);
            Movement.OnMovementCompleted -= OnReachedEnd;
            ObjectPoolManager.Instance.Return("enemy", gameObject);
        }

        // IPoolable
        public void Reset()
        {
            CurrentWayPointIndex = 0;
            Health = null;
            Movement = null;
            _goldGiven = 0;
            _damage = 0;
            Path = null;
        }
    }
}
```

**What changed from Episode 07:**

| Before | After |
|--------|-------|
| `Destroy(gameObject)` in Die | `ObjectPoolManager.Instance.Return("enemy", gameObject)` |
| `Destroy(gameObject)` in OnReachedEnd | Same Return call |
| No IPoolable | Implements `IPoolable`, adds `Reset()` |
| No unsubscription in Die | `Movement.OnMovementCompleted -= OnReachedEnd` before Return |

**Why unsubscribe before Return?** When the object is reactivated from the pool, `Initialize` subscribes again. If we don't unsubscribe first, the old subscription leaks and fires twice.

## ProjectileBase.cs (pool integration)

```csharp
using Interfaces;
using Systems.Managers;
using UnityEngine;

namespace Projectiles
{
    public class ProjectileBase : MonoBehaviour, IPoolable
    {
        [SerializeField] protected float moveSpeed = 15f;
        [SerializeField] protected float damage = 10f;
        [SerializeField] protected float maxLifetime = 5f;
        [SerializeField] protected string poolKey;

        protected ITargetable Target;

        public virtual void Launch(ITargetable target)
        {
            Target = target;
        }

        private void Update()
        {
            if (Target == null || !Target.IsAlive)
            {
                ReturnToPool();
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

            ReturnToPool();
        }

        protected void ReturnToPool()
        {
            ObjectPoolManager.Instance.Return(poolKey, gameObject);
        }

        // IPoolable
        public virtual void Reset()
        {
            Target = null;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }
    }
}
```

**What changed from Episode 04:**

| Before | After |
|--------|-------|
| `Destroy(gameObject, maxLifetime)` in Launch | Gone — pool handles lifetime |
| `Destroy(gameObject)` when target lost | `ReturnToPool()` |
| `Destroy(gameObject)` on hit | `ReturnToPool()` |
| No IPoolable | Implements `IPoolable`, adds `Reset()` |
| No poolKey | `poolKey` serialized field set per prefab |

## TowerFiring.cs (pool integration)

```csharp
using Systems.Managers;
using Towers;
using UnityEngine;

namespace Towers
{
    public class TowerFiring : MonoBehaviour
    {
        [SerializeField] private TowerDetection detection;
        [SerializeField] private string projectilePoolKey;
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
            GameObject projectileObj = ObjectPoolManager.Instance.Get(
                projectilePoolKey, firePoint.position, firePoint.rotation);

            projectileObj.GetComponent<ProjectileBase>().Launch(detection.CurrentTarget);
        }
    }
}
```

**What changed from Episode 04:**

| Before | After |
|--------|-------|
| `Instantiate(projectilePrefab, ...)` | `ObjectPoolManager.Instance.Get(poolKey, ...)` |
| `projectilePrefab` reference | `projectilePoolKey` string |

## Unity Editor Setup

### 1. Create ObjectPoolManager

1. Empty GameObject named "ObjectPoolManager"
2. Add `ObjectPoolManager` component
3. PoolConfigs array size = 2

| Key | Prefab | Default Size | Max Size |
|-----|--------|-------------|----------|
| `enemy` | Enemy prefab | 20 | 100 |
| `arrow` | ArrowProjectile prefab | 30 | 100 |

### 2. Set Pool Keys on Prefabs

On ArrowProjectile prefab: `poolKey = "arrow"`
On Enemy prefab: not needed (EnemyController doesn't reference its own pool key)

### 3. Update Spawner

Your spawner now uses the pool:

```csharp
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private EnemyPath path;

    public void SpawnEnemy()
    {
        GameObject enemy = ObjectPoolManager.Instance.Get("enemy", path.StartPosition, Quaternion.identity);
        enemy.GetComponent<EnemyController>().Initialize(enemyData, path);
    }
}
```

### 4. Profile Before/After

1. **Before**: Temporarily revert to Instantiate/Destroy, run 50 enemies, screenshot Profiler
2. **After**: Use pooling, run same 50 enemies, screenshot Profiler
3. Compare GC.Alloc — pooling should show near-zero allocations during runtime

## Debugging

| Symptom | Cause | Fix |
|---------|-------|-----|
| `ArgumentException: No pool with key 'xxx'` | No PoolConfig for that key | Add a PoolConfig entry |
| Enemies spawn but don't move | `Initialize` not called after Get | Spawner must call Initialize |
| Pooled enemies keep old health/direction | Reset not clearing | Ensure IPoolable.Reset clears all fields |
| NullRef on Instance | Manager not in scene | Add ObjectPoolManager GameObject |
| Objects visible at (0,0,0) at startup | Pre-warm cycle visible | Normal — happens in one frame. If not, check actionOnRelease sets active=false |
| Enemy dies but doesn't return to pool | Missing Return call | Check Die/OnReachedEnd call ObjectPoolManager.Instance.Return |