# Episode 06: Movement Strategies

## What You're Building

Extract movement logic from `EnemyController` into `IMovementStrategy` implementations. `GroundedPath` walks along waypoints at ground level. `FlyingPath` follows the same waypoints at a fixed height. Adding a new movement type requires one new class and one factory case — `EnemyController` never changes.

## The Problem

You want a flying enemy. The movement logic in `EnemyController.Update` is hardcoded:

```csharp
var target = path.GetWaypointPosition(_currentWaypointIndex);
transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
transform.LookAt(target);

if (path.IsAtWaypoint(_currentWaypointIndex, transform.position))
    _currentWaypointIndex++;
```

To add flying you'd either copy `EnemyController` into `FlyingEnemyController` — 90% duplicated — or add `if (isFlying)` branches that grow with every new type. Both mean editing working code every time.

The Strategy Pattern moves each movement behaviour into its own class. `EnemyController` calls one method and reacts to the result.

## MovementConfig.cs

No dependencies. Pure data.

```csharp
namespace Enums
{
    public enum MovementType
    {
        Grounded,
        Flying
    }
}

using Enums;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "MovementConfig", menuName = "TD/Movement Config")]
    public class MovementConfig : ScriptableObject
    {
        // TODO: Episode 06 — SO fields: MovementType type, float moveSpeed, float flyingHeight
        
        public MovementType type;
        public float moveSpeed = 5f;
        [Range(0f, 10f)] public float flyingHeight;
    }
}
```

## EnemyData.cs

References `MovementConfig`. Health stays a plain float here — Episode 07 extracts it into its own config.

```csharp
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "TD/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        public MovementConfig movementConfig;
        public float startHealth = 100f;
        public int goldGiven = 10;
        public int livesTaken = 1;
    }
}
```

## IMovementStrategy.cs

The contract every movement strategy must fulfil. `Init` runs once on setup. `Tick` runs every frame and returns `true` when the path is complete.

```csharp
using Enemies.Controllers;

namespace Interfaces
{
    public interface IMovementStrategy
    {
        void Init(EnemyController enemy);
        bool Tick(EnemyController enemy);
    }
}
```

Both methods take `EnemyController` — the strategy needs access to the enemy's path, waypoint index, and transform to do its job. What those look like on `EnemyController` is up next.

## EnemyController.cs — part one

The strategies need to read and write two things on the controller: the path and the current waypoint index. Both were private before. Promote them, add the strategy field. Everything else stays exactly as it was in Episode 05.

```csharp
// Add these — strategies need to read and write them
public EnemyPath Path { get; private set; }
public int CurrentWaypointIndex { get; set; }

// Add this — holds whichever strategy is active
private IMovementStrategy _movement;
```

The rest of `EnemyController` — `TakeDamage`, `Die`, `HandleEndReached`, `Start`, `Update` — is unchanged. The game still runs exactly as it did in Episode 05. Nothing is wired up yet.

## GroundedPath.cs

`EnemyController.Path` and `EnemyController.CurrentWaypointIndex` now exist, so the strategy can use them.

```csharp
using Enemies.Controllers;
using Interfaces;
using UnityEngine;

namespace Strategies.Movement
{
    public class GroundedPath : IMovementStrategy
    {
        private readonly float _moveSpeed;
        private EnemyPath _path;

        public GroundedPath(float moveSpeed) => _moveSpeed = moveSpeed;

        public void Init(EnemyController enemy)
        {
            _path = enemy.Path;
            enemy.CurrentWaypointIndex = 0;
            enemy.transform.position = _path.StartPosition;
        }

        public bool Tick(EnemyController enemy)
        {
            if (!_path) return false;

            var index = enemy.CurrentWaypointIndex;

            if (!_path.HasWaypoint(index)) return true;

            var target = _path.GetWaypointPosition(index);
            enemy.transform.position = Vector3.MoveTowards(
                enemy.transform.position, target, _moveSpeed * Time.deltaTime);
            enemy.transform.LookAt(target);

            if (_path.IsAtWaypoint(index, enemy.transform.position))
                enemy.CurrentWaypointIndex++;

            return false;
        }
    }
}
```

This is the same logic that was inline in `EnemyController.Update` — extracted without changes. `LookAt` moves here too since orientation is movement behaviour, not controller behaviour.

**Why does the constructor only take `moveSpeed`?** The factory has the config data when it creates the strategy, but the enemy doesn't exist yet. `Init` receives the live enemy once it does. Constructor takes what's known at creation time; `Init` takes what only exists at runtime.

## FlyingPath.cs

```csharp
using Enemies.Controllers;
using Interfaces;
using UnityEngine;

namespace Strategies.Movement
{
    public class FlyingPath : IMovementStrategy
    {
        private readonly float _moveSpeed;
        private readonly float _flyingHeight;
        private EnemyPath _path;

        public FlyingPath(float moveSpeed, float flyingHeight)
        {
            _moveSpeed = moveSpeed;
            _flyingHeight = flyingHeight;
        }

        public void Init(EnemyController enemy)
        {
            _path = enemy.Path;
            enemy.CurrentWaypointIndex = 0;
            enemy.transform.position = _path.StartPosition + Vector3.up * _flyingHeight;
        }

        public bool Tick(EnemyController enemy)
        {
            if (!_path) return false;

            var index = enemy.CurrentWaypointIndex;

            if (!_path.HasWaypoint(index)) return true;

            var target = _path.GetWaypointPosition(index) + Vector3.up * _flyingHeight;
            enemy.transform.position = Vector3.MoveTowards(
                enemy.transform.position, target, _moveSpeed * Time.deltaTime);
            enemy.transform.LookAt(target);

            if (_path.IsAtWaypoint(index, enemy.transform.position - Vector3.up * _flyingHeight))
                enemy.CurrentWaypointIndex++;

            return false;
        }
    }
}
```

Identical structure to `GroundedPath` with one difference: the Y offset applied throughout. The `IsAtWaypoint` check subtracts the offset — it compares against the ground-level waypoint position, not the elevated one. Without this the enemy never registers arrival at the final waypoint and loops forever.

## StrategyFactory.cs

Both strategies exist now. The factory maps config data to the right implementation.

```csharp
using Data;
using Interfaces;
using Strategies.Movement;

namespace Systems.Parsing
{
    public static IMovementStrategy CreateMovement(MovementConfig config)
        {
            return config.type switch
            {
                MovementType.Grounded => new GroundedPath(config.moveSpeed),
                MovementType.Flying => new FlyingPath(config.moveSpeed, config.flyingHeight),
                _ => new GroundedPath(config.moveSpeed)
            };
        }
}
```

The `_` default falls back to Grounded — an unrecognised type doesn't crash, it just walks.

## EnemyController.cs — part two

`EnemyData`, `StrategyFactory`, and both strategy implementations all exist now. Wire it all together.

**What's removed:**

- `[SerializeField] private float moveSpeed` — lives in `MovementConfig`
- `[SerializeField] private EnemyPath path` — passed via `Initialize`
- `[SerializeField] private float startHealth` — comes from `EnemyData`
- `[SerializeField] private int goldGiven` — comes from `EnemyData`
- `[SerializeField] private int livesTaken` — comes from `EnemyData`
- `private int _currentWaypointIndex` — replaced by `CurrentWaypointIndex`
- `Start()` — initialization moves into `Initialize`
- Inline movement in `Update` — replaced by `_movement.Tick(this)`

**What's added:**

- `Initialize(EnemyData, EnemyPath, PlayerStats)` — sets up all runtime state and creates the strategy
- `_movement.Tick(this)` in `Update` — one call replaces the entire movement block

```csharp
using Data;
using Enemies.Components;
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
        public int CurrentWaypointIndex { get; set; }
        public Vector3 Position => transform.position;
        public bool IsAlive => _currentHealth > 0;

        private IMovementStrategy _movement;
        private PlayerStats _playerStats;
        private float _currentHealth;
        private float _startHealth;
        private int _goldGiven;
        private int _livesTaken;

        public void Initialize(EnemyData data, EnemyPath path, PlayerStats playerStats)
        {
            Path = path;
            _playerStats = playerStats;
            _startHealth = data.startHealth;
            _currentHealth = _startHealth;
            _goldGiven = data.goldGiven;
            _livesTaken = data.livesTaken;

            _movement = StrategyFactory.CreateMovement(data.movementConfig);
            _movement.Init(this);

            healthBar.Hide();
        }

        private void Update()
        {
            if (!IsAlive) return;

            if (_movement.Tick(this))
            {
                HandleEndReached();
                return;
            }
        }

        public void TakeDamage(float damage)
        {
            _currentHealth -= damage;
            healthBar.Show();
            healthBar.UpdateValue(Mathf.Clamp01(_currentHealth / _startHealth));

            if (_currentHealth > 0) return;
            _currentHealth = 0;
            Die();
        }

        private void Die()
        {
            _playerStats.AddGold(_goldGiven);
            Destroy(gameObject);
        }

        private void HandleEndReached()
        {
            _playerStats.RemoveLives(_livesTaken);
            Destroy(gameObject);
        }
    }
}
```

`Update` is now four lines. `TakeDamage`, `Die`, and `HandleEndReached` are unchanged from Episode 05.

## Unity Editor Setup

### 1. Create MovementConfig assets

1. Right-click in Project → Create > TD > Movement Config
2. `MC_Grounded`: Type = Grounded, Move Speed = 5
3. `MC_Flying`: Type = Flying, Move Speed = 5, Flying Height = 2

### 2. Create EnemyData assets

1. Right-click → Create > TD > Enemy Data
2. `ED_Basic`: Movement Config = MC_Grounded, Start Health = 100, Gold Given = 10, Lives Taken = 1
3. `ED_Flying`: Movement Config = MC_Flying, Start Health = 100, Gold Given = 15, Lives Taken = 1

### 3. Update the spawner

```csharp
using Data;
using Enemies.Controllers;
using Systems.Game;
using UnityEngine;

public class EnemySpawnerTest : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private EnemyPath path;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private GameObject enemyPrefab;

    private void Start()
    {
        var enemy = Instantiate(enemyPrefab).GetComponent<EnemyController>();
        enemy.Initialize(enemyData, path, playerStats);
    }
}
```

### 4. Test

1. Spawn with `ED_Basic` — ground movement, identical to Episode 05
2. Swap to `ED_Flying` — enemy floats at height, follows same path
3. Both take damage and die identically
4. `EnemyController` is the same for both — only the data asset differs

## Debugging

| Symptom                              | Cause                                 | Fix                                                       |
| ------------------------------------ | ------------------------------------- | --------------------------------------------------------- |
| NullRef in `_movement.Tick`          | `Initialize` not called               | Spawner must call `Initialize` after `Instantiate`        |
| Enemy spawns at scene origin         | `Init` not setting position           | Check `GroundedPath.Init` sets `enemy.transform.position` |
| Flying enemy walks on ground         | `MC_Flying` Type set to Grounded      | Set Type = Flying on the MovementConfig asset             |
| Flying enemy loops at final waypoint | `IsAtWaypoint` not subtracting height | Compare ground-level position, not elevated position      |
| Gold not awarded on death            | `_playerStats` null                   | Ensure PlayerStats is passed into `Initialize`            |