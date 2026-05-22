# Episode 05: Enemy Type Composition

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden; max-width: 100%; margin: 1.5rem 0;">
  <iframe src="https://www.youtube.com/embed/PLACEHOLDER_EP05" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; border:0;" title="Episode 05" allowfullscreen></iframe>
</div>

## Learning Objectives

- Create new health and movement strategies (armoured, flying) without touching `EnemyController`
- Compose enemy types from config data using ScriptableObjects
- Use `EnemyData` SO to bundle `HealthConfig` + `MovementConfig` into a single enemy definition
- Extend `StrategyFactory` to handle new strategy types

## Key Concepts

- [Strategy Pattern](../Concepts/Strategy_Pattern.md)
- [Factory Pattern](../Concepts/Factory_Pattern.md)

## What We're Starting With

- `NormalHealth` strategy works — enemies take damage and die
- `GroundedPath` strategy works — enemies walk the waypoint path
- `StrategyFactory` creates strategies from configs
- `EnemyController` delegates to `IMovementStrategy` and `IHealthStrategy`

---

## The Naive Version

We need an armoured enemy that takes reduced damage. The "obvious" copy-paste approach:

```csharp
namespace Strategies.Health
{
    public class ArmouredHealth : IHealthStrategy
    {
        // TODO: copy NormalHealth, add _armour field
        // TODO: TakeDamage → reduce amount by armour BEFORE subtracting
    }
}
```

And we need a flying enemy. Copy-paste `GroundedPath`:

```csharp
namespace Strategies.Movement
{
    public class FlyingPath : IMovementStrategy
    {
        // TODO: copy GroundedPath, add _height offset
        // TODO: same Initialise, same Tick, but with height applied to position
    }
}
```

Wait — these aren't copy-paste any more. Each strategy is genuinely *different behaviour*. `ArmouredHealth` isn't `NormalHealth` with a tweak — it *reduces incoming damage*. `FlyingPath` isn't `GroundedPath` with a tweak — it *moves at a different altitude*.

**This is where the Strategy Pattern pays off.** Each new strategy is a *new class*, not a new controller. `EnemyController` stays the same.

---

## The Refactor

We add three new strategy classes and one new data SO:

| New Class | What It Does |
|-----------|-------------|
| `ArmouredHealth` | Reduces incoming damage by a flat armour value |
| `FlyingPath` | Moves along waypoints at a configurable height |
| `EnemyData` SO | Bundles `HealthConfig` + `MovementConfig` into one asset |

### Config SOs (new fields)

```csharp
// HealthConfig — add armour field
namespace Data
{
    [CreateAssetMenu(menuName = "TD/HealthConfig")]
    public class HealthConfig : ScriptableObject
    {
        // TODO: float MaxHealth = 100f;
        // TODO: float Armour = 0f;       // NEW: flat damage reduction
        // TODO: HealthType Type;          // NEW: Normal, Armoured, etc.
    }
}
```

```csharp
// MovementConfig — already has Type enum from Episode 03
namespace Data
{
    public enum MovementType { Grounded, Flying }
}
```

### EnemyData SO (new)

```csharp
namespace Data
{
    [CreateAssetMenu(menuName = "TD/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        // TODO: string DisplayName;
        // TODO: HealthConfig HealthConfig;
        // TODO: MovementConfig MovementConfig;
        // TODO: GameObject Prefab;  // the enemy prefab to spawn
    }
}
```

---

## Code Roadmap

| File | Purpose |
|------|---------|
| `Strategies/Health/ArmouredHealth.cs` | Health strategy with flat damage reduction |
| `Strategies/Movement/FlyingPath.cs` | Movement strategy with height offset |
| `Data/HealthConfig.cs` | Updated — `Armour` field, `HealthType` enum |
| `Data/MovementConfig.cs` | Already has `MovementType.Flying` |
| `Data/EnemyData.cs` | SO bundling health + movement configs |
| `Systems/Parsing/StrategyFactory.cs` | Updated — handles `ArmouredHealth` and `FlyingPath` |
| `Enemies/Controllers/EnemyController.cs` | Updated — uses `EnemyData` instead of separate configs |

### EnemyController.cs — skeleton after refactor

```csharp
namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable
    {
        // TODO: [SerializeField] EnemyData _data;
        // TODO: IMovementStrategy _movement;
        // TODO: IHealthStrategy _health;
        // TODO: EnemyHealthBar _healthBar;

        // TODO: Start — create strategies via StrategyFactory using _data configs
        // TODO: Update — _movement.Tick(Time.deltaTime)
        // TODO: TakeDamage → _health.TakeDamage, update bar, handle death
    }
}
```

---

## Step-by-Step Implementation

### 1 — Update HealthConfig with HealthType and Armour

```csharp
namespace Data
{
    public enum HealthType { Normal, Armoured }

    [CreateAssetMenu(menuName = "TD/HealthConfig")]
    public class HealthConfig : ScriptableObject
    {
        // TODO: public HealthType Type = HealthType.Normal;
        // TODO: public float MaxHealth = 100f;
        // TODO: public float Armour = 0f;
    }
}
```

### 2 — Implement ArmouredHealth

Create `Assets/Scripts/Strategies/Health/ArmouredHealth.cs`:

```csharp
namespace Strategies.Health
{
    public class ArmouredHealth : IHealthStrategy
    {
        // TODO: float _currentHealth;
        // TODO: float _maxHealth;
        // TODO: float _armour;

        // TODO: Initialise(HealthConfig config) — store max, armour, set current = max
        // TODO: TakeDamage(float amount):
        //   — float effective = Mathf.Max(amount - _armour, 0f);
        //   — _currentHealth -= effective;
        //   — bool killed = _currentHealth <= 0;
        //   — clamp _currentHealth to 0 if killed
        //   — return DamageResult.Create(effective, _currentHealth, killed);
        // TODO: IsAlive, CurrentHealth, MaxHealth properties
    }
}
```

> **Why `Mathf.Max(amount - _armour, 0f)`?** Armour prevents small amounts from dealing *negative* damage (which would heal). Clamping to zero ensures damage never heals.

### 3 — Implement FlyingPath

Create `Assets/Scripts/Strategies/Movement/FlyingPath.cs`:

```csharp
namespace Strategies.Movement
{
    public class FlyingPath : IMovementStrategy
    {
        // TODO: Transform _enemy;
        // TODO: Transform[] _waypoints;
        // TODO: int _waypointIndex;
        // TODO: float _speed;
        // TODO: float _height;    // Y offset
        // TODO: float _pathProgress;

        // TODO: Initialise(Transform enemy, Transform[] waypoints, float speed, float height)
        //   — store all fields
        //   — snap enemy to first waypoint + height offset

        // TODO: Tick(float deltaTime)
        //   — same movement logic as GroundedPath
        //   — BUT set enemy.position.y = waypoint.y + _height after each move

        // TODO: PathProgress => _pathProgress;
    }
}
```

> `FlyingPath` is almost identical to `GroundedPath` — the only difference is a Y-axis offset. But it's *behaviourally distinct*: a flying enemy ignores ground obstacles and has a visual height. Future episodes might add hovering animation or shadow projection.

### 4 — Update StrategyFactory

```csharp
namespace Systems.Parsing
{
    public static class StrategyFactory
    {
        public static IHealthStrategy CreateHealth(HealthConfig config)
        {
            // TODO: switch on config.Type
            //   — HealthType.Normal → return new NormalHealth();
            //   — HealthType.Armoured → return new ArmouredHealth();
            //   — default → return new NormalHealth();
        }

        public static IMovementStrategy CreateMovement(MovementConfig config)
        {
            // TODO: switch on config.Type
            //   — MovementType.Grounded → return new GroundedPath(config.Speed);
            //   — MovementType.Flying → return new FlyingPath(config.Speed, config.Height);
            //   — default → return new GroundedPath(config.Speed);
        }
    }
}
```

> **Note:** `IMovementStrategy.Initialise` takes different parameters for different strategies. We'll refine this in a later episode using a config object pattern. For now, pass what each strategy needs.

### 5 — Create EnemyData SO

Create `Assets/Scripts/Data/EnemyData.cs`:

```csharp
namespace Data
{
    [CreateAssetMenu(menuName = "TD/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        // TODO: public string DisplayName;
        // TODO: public HealthConfig HealthConfig;
        // TODO: public MovementConfig MovementConfig;
        // TODO: public GameObject Prefab;
    }
}
```

### 6 — Refactor EnemyController to use EnemyData

```csharp
namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable
    {
        [SerializeField] private EnemyData _data;
        [SerializeField] private EnemyPath _path;
        [SerializeField] private EnemyHealthBar _healthBar;

        private IMovementStrategy _movement;
        private IHealthStrategy _health;

        private void Start()
        {
            // TODO: _movement = StrategyFactory.CreateMovement(_data.MovementConfig);
            // TODO: _movement.Initialise(transform, _path.Waypoints, _data.MovementConfig.Speed, ...);

            // TODO: _health = StrategyFactory.CreateHealth(_data.HealthConfig);
            // TODO: _health.Initialise(_data.HealthConfig);

            // TODO: _healthBar.Initialise(_health.MaxHealth);
        }

        // ... Update, TakeDamage, ITargetable same as Episode 04
    }
}
```

### 7 — Create enemy data assets

Create these ScriptableObject assets in Unity:

| Asset | HealthConfig | MovementConfig |
|-------|-------------|----------------|
| `Enemy_Normal` | Type: Normal, MaxHealth: 100, Armour: 0 | Type: Grounded, Speed: 3 |
| `Enemy_Armoured` | Type: Armoured, MaxHealth: 200, Armour: 10 | Type: Grounded, Speed: 2 |
| `Enemy_Flying` | Type: Normal, MaxHealth: 60, Armour: 0 | Type: Flying, Speed: 4, Height: 3 |

Drag the appropriate config assets into each `EnemyData`. Create three enemy prefabs, each referencing a different `EnemyData`.

---

## Episode Recap

- **Naive instinct**: copy-paste strategy classes with tweaks — but each strategy is genuinely different behaviour
- **Composition**: `ArmouredHealth` and `FlyingPath` are new strategy classes, not new controllers
- `EnemyData` SO bundles configs — one asset = one enemy type definition
- `StrategyFactory` maps config types to strategy classes — the switch statement that replaces copy-paste
- **Key insight**: `EnemyController` was not modified to support armoured or flying enemies. Zero controller changes.

Episode 06 introduces targeting — how towers decide *which* enemy to shoot. The same Strategy Pattern applies: `ITargetingStrategy`.

## Challenge

Create a fast, fragile enemy type: `HealthType.Normal` with `MaxHealth = 30`, `MovementType.Grounded` with `Speed = 6`. Create a new `EnemyData` asset for it and a matching prefab. Verify in Play mode — you should have four distinct enemy behaviours, all driven by config data with no new code.

<details>
<summary>Hint</summary>

Just create a new `HealthConfig` (Normal, 30hp) and `MovementConfig` (Grounded, Speed 6), then create an `EnemyData` that references both. Assign it on a new prefab. Done — no C# changes needed.

</details>