# Episode 06: Movement Strategies

## What You're Building

Extract movement logic from `EnemyController` into `IMovementStrategy` implementations. `GroundedPath` walks on the ground. `FlyingPath` hovers at a height. Adding a new movement type no longer requires editing `EnemyController`.

## The Problem

You want a flying enemy. Right now, `EnemyController` has movement hardcoded inline:

```csharp
private void Update()
{
    Vector3 target = path.GetWaypointPosition(_currentWaypointIndex);
    transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
}
```

To add flying, you'd have to either:
1. Copy `EnemyController` into `FlyingEnemyController` with a Y offset — 90% duplicated code
2. Add `if (isFlying)` checks inside `EnemyController` — grows with every new type

Both approaches violate the Open/Closed Principle. Every new movement type means editing existing, working code.

## IMovementStrategy.cs

```csharp
using System;
using Enemies.Controllers;

namespace Interfaces
{
    public interface IMovementStrategy
    {
        void Initialize(EnemyController enemy);
        void Tick(EnemyController enemy);
        event Action OnMovementCompleted;
    }
}
```

The strategy receives `EnemyController` — it needs access to `Path`, `CurrentWayPointIndex`, and `transform` to do its job. This is intentional: the strategy operates *on* the controller, not independently.

## GroundedPath.cs

```csharp
using System;
using Enemies.Controllers;
using Interfaces;
using Systems.Game;
using UnityEngine;

namespace Strategies.Movement
{
    public class GroundedPath : IMovementStrategy
    {
        private readonly float _moveSpeed;
        private EnemyPath _path;

        public event Action OnMovementCompleted;

        public GroundedPath(float moveSpeed)
        {
            _moveSpeed = moveSpeed;
        }

        public void Initialize(EnemyController enemy)
        {
            _path = enemy.Path;
            enemy.CurrentWayPointIndex = 0;
            enemy.transform.position = _path.StartPosition;
        }

        public void Tick(EnemyController enemy)
        {
            if (_path == null) return;

            int index = enemy.CurrentWayPointIndex;

            if (!_path.HasWaypoint(index))
            {
                OnMovementCompleted?.Invoke();
                return;
            }

            Vector3 target = _path.GetWaypointPosition(index);
            enemy.transform.position = Vector3.MoveTowards(
                enemy.transform.position, target, _moveSpeed * Time.deltaTime);

            if (_path.IsAtWaypoint(index, enemy.transform.position))
            {
                enemy.CurrentWayPointIndex++;
            }
        }
    }
}
```

This is the same movement logic that was inline in `EnemyController.Update()`. Extracted without changes.

## FlyingPath.cs

```csharp
using System;
using Enemies.Controllers;
using Interfaces;
using Systems.Game;
using UnityEngine;

namespace Strategies.Movement
{
    public class FlyingPath : IMovementStrategy
    {
        private readonly float _moveSpeed;
        private readonly float _flyingHeight;
        private EnemyPath _path;

        public event Action OnMovementCompleted;

        public FlyingPath(float moveSpeed, float flyingHeight)
        {
            _moveSpeed = moveSpeed;
            _flyingHeight = flyingHeight;
        }

        public void Initialize(EnemyController enemy)
        {
            _path = enemy.Path;
            enemy.CurrentWayPointIndex = 0;
            enemy.transform.position = _path.StartPosition + Vector3.up * _flyingHeight;
        }

        public void Tick(EnemyController enemy)
        {
            if (_path == null) return;

            int index = enemy.CurrentWayPointIndex;

            if (!_path.HasWaypoint(index))
            {
                OnMovementCompleted?.Invoke();
                return;
            }

            Vector3 waypointPos = _path.GetWaypointPosition(index);
            Vector3 target = waypointPos + Vector3.up * _flyingHeight;
            enemy.transform.position = Vector3.MoveTowards(
                enemy.transform.position, target, _moveSpeed * Time.deltaTime);

            if (_path.IsAtWaypoint(index, enemy.transform.position - Vector3.up * _flyingHeight))
            {
                enemy.CurrentWayPointIndex++;
            }
        }
    }
}
```

**Difference from GroundedPath:** Y offset. `Initialize` adds `Vector3.up * _flyingHeight` to the start position. `Tick` targets the waypoint position + height offset. `IsAtWaypoint` compares against the ground-level position (subtracting the offset).

## MovementType enum + MovementConfig.cs

```csharp
using UnityEngine;

namespace Data
{
    public enum MovementType
    {
        Grounded,
        Flying
    }

    [CreateAssetMenu(fileName = "MovementConfig", menuName = "TD/Movement Config")]
    public class MovementConfig : ScriptableObject
    {
        [SerializeField] private MovementType type;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField, Range(0f, 10f)] private float flyingHeight;

        public MovementType Type => type;
        public float MoveSpeed => moveSpeed;
        public float FlyingHeight => flyingHeight;
    }
}
```

ScriptableObject configs let you create data assets in the Inspector instead of hardcoding values. `MC_Grounded` has type=Grounded, speed=5, flyingHeight=0. `MC_Flying` has type=Flying, speed=5, flyingHeight=2.

## StrategyFactory.cs

```csharp
using Data;
using Interfaces;
using Strategies.Movement;

namespace Systems.Parsing
{
    public static class StrategyFactory
    {
        public static IMovementStrategy CreateMovement(MovementConfig config)
        {
            return config.Type switch
            {
                MovementType.Grounded => new GroundedPath(config.MoveSpeed),
                MovementType.Flying => new FlyingPath(config.MoveSpeed, config.FlyingHeight),
                _ => new GroundedPath(config.MoveSpeed)
            };
        }
    }
}
```

The factory maps `MovementType` enum to concrete classes. `EnemyController` calls one method and gets the right strategy. The `_` default case means an invalid enum value falls back to Grounded.

## EnemyData.cs

```csharp
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "TD/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private MovementConfig movementConfig;
        [SerializeField] private float startHealth = 100f;
        [SerializeField] private int goldGiven = 10;
        [SerializeField] private int damage = 1;

        public MovementConfig MovementConfig => movementConfig;
        public float StartHealth => startHealth;
        public int GoldGiven => goldGiven;
        public int Damage => damage;
    }
}
```

`EnemyData` holds `MovementConfig` (a reference to another SO). Health is still a plain float — it becomes a `HealthConfig` reference in Episode 07.

## EnemyController.cs (refactored)

```csharp
using Data;
using Interfaces;
using Systems.Game;
using Systems.Parsing;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable
    {
        [SerializeField] private EnemyHealthBar healthBar;

        public EnemyPath Path { get; private set; }
        public int CurrentWayPointIndex { get; set; }
        public IMovementStrategy Movement { get; private set; }

        private float _currentHealth;
        private float _startHealth;
        private int _goldGiven;
        private int _damage;

        // ITargetable
        public Vector3 Position => transform.position;
        public bool IsAlive => _currentHealth > 0;

        public void Initialize(EnemyData data, EnemyPath path)
        {
            Path = path;
            _startHealth = data.StartHealth;
            _currentHealth = _startHealth;
            _goldGiven = data.GoldGiven;
            _damage = data.Damage;

            Movement = StrategyFactory.CreateMovement(data.MovementConfig);
            Movement.Initialize(this);
            Movement.OnMovementCompleted += OnReachedEnd;
        }

        private void Update()
        {
            if (!IsAlive) return;

            Movement.Tick(this);

            if (healthBar != null)
            {
                healthBar.SetHealth(_currentHealth, _startHealth);
                healthBar.SetPosition(transform.position);
            }
        }

        public void TakeDamage(float damage)
        {
            _currentHealth -= damage;
            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
        }

        private void Die()
        {
            PlayerStats.Instance.AddGold(_goldGiven);
            Destroy(gameObject);
        }

        private void OnReachedEnd()
        {
            Movement.OnMovementCompleted -= OnReachedEnd;
            PlayerStats.Instance.SubtractLives(_damage);
            Destroy(gameObject);
        }
    }
}
```

**What changed from Episode 05:**

| Before | After |
|--------|-------|
| `[SerializeField] float moveSpeed` | Gone — moved into MovementConfig |
| `[SerializeField] EnemyPath path` | `public EnemyPath Path { get; private set; }` — strategy needs access |
| `_currentWaypointIndex` private | `public int CurrentWayPointIndex { get; set; }` — strategy needs access |
| Inline `Vector3.MoveTowards` in Update | `Movement.Tick(this)` — one line |
| Inline path setup in Start | `Initialize(data, path)` method — creates strategy via factory |
| `[SerializeField] float startHealth` | `_startHealth` set from `EnemyData` parameter |

**EnemyController no longer contains any movement logic.** It delegates to `Movement.Tick(this)`. Adding "TeleportPath" = one new class + one factory case. EnemyController never changes.

## Unity Editor Setup

### 1. Create MovementConfig SOs

1. Right-click in Project → Create > TD > Movement Config
2. Name `MC_Grounded`: Type=Grounded, Move Speed=5, Flying Height=0
3. Name `MC_Flying`: Type=Flying, Move Speed=5, Flying Height=2

### 2. Create EnemyData SOs

1. Right-click → Create > TD > Enemy Data
2. Name `ED_Basic`: Movement=MC_Grounded, Health=100, Gold=10, Damage=1
3. Name `ED_Flying`: Movement=MC_Flying, Health=100, Gold=15, Damage=1

### 3. Update Spawner

Your test script now uses `Initialize(data, path)`:

```csharp
public class EnemySpawnerTest : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private EnemyPath path;
    [SerializeField] private GameObject enemyPrefab;

    private void Start()
    {
        GameObject enemy = Instantiate(enemyPrefab);
        enemy.GetComponent<EnemyController>().Initialize(enemyData, path);
    }
}
```

### 4. Test

1. Spawn with `ED_Basic` — enemy walks on ground (same as before)
2. Spawn with `ED_Flying` — enemy floats 2 units above ground, follows same path
3. Both enemies take damage and die the same way
4. EnemyController code is identical for both — only the data differs

## Debugging

| Symptom | Cause | Fix |
|---------|-------|-----|
| `NullReferenceException` in Movement.Tick | `Initialize` not called | Spawner must call `Initialize(data, path)` |
| Enemy doesn't spawn at path start | Movement.Initialize not setting position | Check GroundedPath.Initialize sets transform.position |
| Flying enemy walks on ground | MC_Flying has type=Grounded | Verify MovementConfig Type is set to Flying |
| Factory returns wrong strategy | Enum mismatch | Check MovementConfig SO type matches intended strategy |
| `_path == null` in Tick | Path not passed to Initialize | Ensure `enemy.Path = path` is set before `Movement.Initialize` |
| `OnMovementCompleted` not firing | No subscriber | Ensure `Movement.OnMovementCompleted += OnReachedEnd` in Initialize |