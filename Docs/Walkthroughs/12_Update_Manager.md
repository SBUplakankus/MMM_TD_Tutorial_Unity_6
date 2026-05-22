# Episode 12: Update Manager

## What You're Building

Replace per-MonoBehaviour `Update()` calls with a single managed update loop. `EnemyController` and `ProjectileBase` stop using Unity's native `Update()` and instead implement `IUpdatable`, registered with `UpdateManager`. One native-to-managed transition per frame instead of N.

## The Problem

Open the Profiler during a wave with 30+ enemies and 10+ projectiles. Each `Update()` call is a separate native-to-managed transition. The overhead isn't in the logic — it's in the call dispatch. Unity calls `Update()` on every active MonoBehaviour through native code, crossing the managed boundary each time.

With 50 registered MonoBehaviours, that's 50 native-to-managed transitions per frame. A managed update loop reduces this to 1.

## IUpdatable.cs

```csharp
namespace Core
{
    public interface IUpdatable
    {
        void ManagedUpdate(float deltaTime);
    }
}
```

One method. No `Update()` replacement on the MonoBehaviour — `ManagedUpdate` is called by the manager, not by Unity.

## UpdateManager.cs

```csharp
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Systems.Managers
{
    public class UpdateManager : MonoBehaviour
    {
        private readonly List<IUpdatable> _updatables = new();
        private readonly List<IUpdatable> _pendingAdd = new();
        private readonly List<IUpdatable> _pendingRemove = new();
        private bool _isIterating;

        public void Register(IUpdatable updatable)
        {
            if (_isIterating)
            {
                _pendingAdd.Add(updatable);
                return;
            }

            _updatables.Add(updatable);
        }

        public void Unregister(IUpdatable updatable)
        {
            if (_isIterating)
            {
                _pendingRemove.Add(updatable);
                return;
            }

            _updatables.Remove(updatable);
        }

        private void Update()
        {
            _isIterating = true;

            float dt = Time.deltaTime;
            for (int i = 0; i < _updatables.Count; i++)
            {
                _updatables[i].ManagedUpdate(dt);
            }

            _isIterating = false;

            ProcessPending();
        }

        private void ProcessPending()
        {
            for (int i = 0; i < _pendingAdd.Count; i++)
            {
                _updatables.Add(_pendingAdd[i]);
            }
            _pendingAdd.Clear();

            for (int i = 0; i < _pendingRemove.Count; i++)
            {
                _updatables.Remove(_pendingRemove[i]);
            }
            _pendingRemove.Clear();
        }
    }
}
```

**Why pending lists?** During iteration, an `IUpdatable` might call `Register` or `Unregister` (e.g., an enemy dies and unregisters itself). Modifying the list while iterating throws `InvalidOperationException`. The pending lists buffer modifications until the loop finishes.

**Why `Time.deltaTime` passed as parameter?** Two reasons:
1. `Time.deltaTime` is a property fetch. Cached once, used N times.
2. Testability. You can call `ManagedUpdate(0.016f)` in a unit test without Unity's time system.

**Why `_isIterating` flag?** Prevents modification during iteration. The alternative — copying the list each frame — allocates. The flag is zero-alloc.

## Refactor EnemyController.cs

```csharp
using Core;
using Systems.Events;
using Systems.Managers;
using UnityEngine;

namespace Systems.Game
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable, IPoolable, IUpdatable
    {
        [SerializeField] private EnemyData data;

        public IMovementStrategy Movement { get; private set; }
        public IHealthStrategy Health { get; private set; }
        public EnemyPath Path { get; private set; }
        public int CurrentWayPointIndex { get; set; }
        public float PathProgress { get; private set; }
        public float CurrentHealth => Health.CurrentHealth;

        private CombatEvents _combatEvents;
        private ObjectPoolManager _poolManager;
        private UpdateManager _updateManager;
        private EnemyHealthBar _healthBar;

        private void OnEnable()
        {
            _combatEvents = Services.Get<CombatEvents>();
            _poolManager = Services.Get<ObjectPoolManager>();
            _updateManager = Services.Get<UpdateManager>();
        }

        public void Initialize(EnemyPath path, EnemyData enemyData)
        {
            Path = path;
            data = enemyData;
            CurrentWayPointIndex = 0;
            PathProgress = 0f;

            Movement = StrategyFactory.CreateMovement(data.movementConfig);
            Movement.Initialize(this);

            Health = StrategyFactory.CreateHealth(data.healthConfig);
            Health.Initialize(data.healthConfig, this);

            _healthBar = GetComponentInChildren<EnemyHealthBar>();

            _updateManager.Register(this);
        }

        public void ManagedUpdate(float deltaTime)
        {
            Movement.Tick(this);
            Health.Tick(deltaTime);
            UpdateHealthBar();
            UpdatePathProgress();
        }

        private void UpdateHealthBar()
        {
            if (_healthBar != null)
            {
                _healthBar.SetValue(Health.CurrentHealth, Health.MaxHealth);
            }
        }

        private void UpdatePathProgress()
        {
            if (Path != null && Path.WaypointCount > 0)
            {
                PathProgress = (float)CurrentWayPointIndex / Path.WaypointCount;
            }
        }

        public DamageResult TakeDamage(float amount)
        {
            DamageResult result = Health.TakeDamage(amount);

            if (!result.IsAlive)
            {
                Die();
            }

            return result;
        }

        private void Die()
        {
            _combatEvents.EnemyDeath.Raise(data.goldGiven);
            _poolManager.Return("enemy", gameObject);
        }

        private void OnReachedEnd()
        {
            _combatEvents.EnemyReachedEnd.Raise(data.damage);
            _poolManager.Return("enemy", gameObject);
        }

        public void Reset()
        {
            CurrentWayPointIndex = 0;
            PathProgress = 0f;

            if (Movement != null) Movement.Initialize(this);
            if (Health != null) Health.Initialize(data.healthConfig, this);
        }

        public Vector3 Position => transform.position;
        public bool IsAlive => Health.IsAlive;
    }
}
```

**What changed from Episode 10:**

| Before | After |
|--------|-------|
| `void Update()` | `void ManagedUpdate(float deltaTime)` |
| Unity calls Update automatically | UpdateManager calls ManagedUpdate |
| `Time.deltaTime` fetched per call | Passed as parameter |
| No registration | `_updateManager.Register(this)` in Initialize |
| No unregistration | `_updateManager.Unregister(this)` in Reset |

**Why not unregister in `Reset()`?** `Reset()` is called when the object returns to the pool. The object is inactive — the UpdateManager won't call it. But if `Reset()` also unregistered, `Initialize()` would need to re-register. Registering in `Initialize` and unregistering when the object is destroyed (not pooled) is safer. For simplicity, we keep registration in `Initialize` only — the manager skips inactive objects naturally since `enabled = false` on pooled objects prevents `Update()` from running on the manager itself.

Actually — the UpdateManager's `Update()` calls `ManagedUpdate` on all registered objects regardless of `enabled`. We need to handle this:

**Better approach**: Unregister in `Reset()`, re-register in `Initialize()`:

```csharp
public void Initialize(EnemyPath path, EnemyData enemyData)
{
    // ... existing setup ...
    _updateManager.Register(this);
}

public void Reset()
{
    _updateManager.Unregister(this);
    // ... existing reset ...
}
```

This guarantees inactive pooled objects are never in the update list.

## Refactor ProjectileBase.cs

```csharp
using Core;
using Systems.Events;
using Systems.Managers;
using UnityEngine;

namespace Systems.Game
{
    [RequireComponent(typeof(Rigidbody))]
    public class ProjectileBase : MonoBehaviour, IPoolable, IUpdatable
    {
        [SerializeField] private float speed = 20f;
        [SerializeField] private float maxLifetime = 5f;
        [SerializeField] private string poolKey = "arrow";

        private ITargetable _target;
        private float _lifetime;
        private CombatEvents _combatEvents;
        private ObjectPoolManager _poolManager;
        private UpdateManager _updateManager;

        private void OnEnable()
        {
            _combatEvents = Services.Get<CombatEvents>();
            _poolManager = Services.Get<ObjectPoolManager>();
            _updateManager = Services.Get<UpdateManager>();
        }

        public void Launch(ITargetable target)
        {
            _target = target;
            _lifetime = 0f;
            _updateManager.Register(this);
        }

        public void ManagedUpdate(float deltaTime)
        {
            if (_target == null || !_target.IsAlive)
            {
                ReturnToPool();
                return;
            }

            MoveTowardsTarget(deltaTime);

            _lifetime += deltaTime;
            if (_lifetime >= maxLifetime)
            {
                ReturnToPool();
            }
        }

        private void MoveTowardsTarget(float deltaTime)
        {
            Vector3 direction = (_target.Position - transform.position).normalized;
            transform.position += direction * speed * deltaTime;
            transform.rotation = Quaternion.LookRotation(direction);

            if (Vector3.Distance(transform.position, _target.Position) < 0.5f)
            {
                HitTarget();
            }
        }

        private void HitTarget()
        {
            if (_target is IDamageable damageable)
            {
                damageable.TakeDamage(1f);
            }

            ReturnToPool();
        }

        private void ReturnToPool()
        {
            _updateManager.Unregister(this);
            _poolManager.Return(poolKey, gameObject);
        }

        public void Reset()
        {
            _target = null;
            _lifetime = 0f;
            _updateManager.Unregister(this);
        }
    }
}
```

**What changed from Episode 8:**

| Before | After |
|--------|-------|
| `void Update()` | `void ManagedUpdate(float deltaTime)` |
| `Time.deltaTime` | `deltaTime` parameter |
| No registration | Register in `Launch()`, unregister in `ReturnToPool()` and `Reset()` |

## Register UpdateManager in GameBootstrapper

```csharp
using Systems.Events;
using Systems.Managers;
using UnityEngine;

namespace Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private ObjectPoolManager poolManager;
        [SerializeField] private UpdateManager updateManager;

        private void Awake()
        {
            Services.Register(poolManager);
            Services.Register(updateManager);
            Services.Register(new PlayerStats(startingGold: 100, startingLives: 20));
            Services.Register(new CombatEvents());
            Services.Register(new EconomyEvents());
        }

        private void OnDestroy()
        {
            Services.Clear();
        }
    }
}
```

UpdateManager is serializable — it's a MonoBehaviour on a GameObject. It must be registered before any `IUpdatable` objects try to call `Services.Get<UpdateManager>()`.

## Registration Order

| Order | Service | Why This Order |
|-------|---------|---------------|
| 1 | ObjectPoolManager | Spawning depends on it |
| 2 | UpdateManager | IUpdatable registration depends on it |
| 3 | PlayerStats | Economy depends on it |
| 4-5 | CombatEvents, EconomyEvents | Event subscriptions depend on them |

## IHealthStrategy.Tick — Accept DeltaTime

The UpdateManager passes `deltaTime` to `ManagedUpdate`. `EnemyController.ManagedUpdate` forwards it to `Health.Tick(deltaTime)`. This means `IHealthStrategy.Tick` needs to accept a `float` parameter:

```csharp
public interface IHealthStrategy
{
    void Initialize(HealthConfig config, EnemyController enemy);
    DamageResult TakeDamage(float amount);
    void Tick(float deltaTime);
    bool IsAlive { get; }
    float CurrentHealth { get; }
    float MaxHealth { get; }
}
```

**RegenHealth** uses `deltaTime` for per-frame regeneration:

```csharp
public void Tick(float deltaTime)
{
    if (!IsAlive) return;

    float regenAmount = _regenRate * deltaTime;
    _currentHealth = Mathf.Min(_currentHealth + regenAmount, _startHealth);
}
```

**NormalHealth** and **ArmouredHealth** ignore `deltaTime` — their `Tick` is a no-op. But the parameter is there for types that need it. This is why the interface includes it.

Wait — `IHealthStrategy.Tick(float deltaTime)` hasn't been introduced yet. It's introduced in Episode 13 (now Advanced Health, formerly Episode 12). In the current episode flow:

- Episode 7 (Health Strategies) introduced `IHealthStrategy` with a parameterless `Tick()`
- Episode 13 (Advanced Health) adds RegenHealth which needs `deltaTime`

**Decision**: Add the `float deltaTime` parameter to `IHealthStrategy.Tick` HERE (Episode 12) since the UpdateManager is what makes it relevant. NormalHealth and ArmouredHealth accept the parameter but ignore it. This is a small interface change that prepares for Episode 13.

### Updated NormalHealth.Tick

```csharp
public void Tick(float deltaTime) { }
```

### Updated ArmouredHealth.Tick

```csharp
public void Tick(float deltaTime) { }
```

Both are still no-ops, but now accept the parameter. RegenHealth (Episode 13) uses it.

## Testing

1. Add an empty GameObject named "UpdateManager" with the `UpdateManager` component
2. Add it to the GameBootstrapper serialized field
3. Play — enemies move, towers fire, projectiles hit. Same observable behavior.
4. Open Profiler — compare Update overhead before and after. The number of native `Update()` dispatches drops to 1 (the UpdateManager itself).
5. Verify inactive pooled objects are not in the update list — check `_updatables.Count` in the Inspector during runtime

## Common Issues

| Problem | Cause | Fix |
|---------|-------|-----|
| `NullReferenceException` on `_updateManager` | UpdateManager not in scene or not registered | Add UpdateManager GameObject, add to GameBootstrapper |
| Enemies don't move after pooling | `Reset()` doesn't unregister, `Initialize()` doesn't re-register | Unregister in `Reset()`, register in `Initialize()` |
| `InvalidOperationException` during iteration | Register/Unregister called during ManagedUpdate | Already handled by pending lists |
| Regeneration doesn't work yet | NormalHealth/ArmouredHealth ignore deltaTime | Correct — RegenHealth (Episode 13) uses deltaTime |
| Projectile keeps moving after pool return | Unregister happens after Return to pool | Call unregister before Return in `ReturnToPool()` and `Reset()` |

## What Changed From Episode 10

| Component | Change |
|-----------|--------|
| `IUpdatable` | New interface with `ManagedUpdate(float)` |
| `UpdateManager` | New — manages IUpdatable list with pending buffers |
| `EnemyController` | `Update()` → `ManagedUpdate()`, added `IUpdatable`, register/unregister |
| `ProjectileBase` | `Update()` → `ManagedUpdate()`, added `IUpdatable`, register/unregister |
| `IHealthStrategy.Tick` | Signature: `Tick()` → `Tick(float deltaTime)` |
| `NormalHealth.Tick` | Signature: `Tick()` → `Tick(float deltaTime)` — still no-op |
| `ArmouredHealth.Tick` | Signature: `Tick()` → `Tick(float deltaTime)` — still no-op |
| `GameBootstrapper` | Added UpdateManager registration (order: 2) |

## Key Insights

**Managed update is not premature optimization.** With 5 enemies, the overhead is irrelevant. With 50, it's measurable. With 200, it's significant. This episode follows the same "patterns from pain" principle — the viewer opens the Profiler, sees the dispatch overhead, then learns the fix.

**The pending list pattern appears again.** EventChannels handle mid-raise modifications with backward iteration. UpdateManager handles mid-iteration modifications with pending add/remove lists. Same problem (concurrent modification), different solution. The viewer sees two approaches and understands the trade-off.

**Interface evolution continues.** `IHealthStrategy.Tick` gains a parameter here because the UpdateManager makes `deltaTime` available at the call site. The interface grows as needed — no forward reference, no unused parameter from Episode 7.