# Game Architecture

## Project Overview
This tower defense game teaches intermediate Unity patterns with a focus on clean architecture, performance optimization, and maintainability. Each concept is introduced one at a time — naive implementation first, then refactor to the pattern.

---

## Core Systems

| System | Purpose | Key Components | Pattern Used |
|--------|---------|----------------|--------------|
| **Strategy System** | Compose enemy behaviors without inheritance | `IHealthStrategy`, `IMovementStrategy`, `ITargetingStrategy`, `StrategyFactory` | Strategy Pattern |
| **Service Locator** | Centralized access to managers and registries | `Services`, `GameBootstrapper` | Service Locator |
| **Event System** | Decouple game systems through event-driven communication | `EventChannel<T>`, `EventChannel`, registry classes | Observer Pattern |
| **Object Pooling** | Optimize performance by reusing objects | `ObjectPoolManager`, `PoolConfig`, `IPoolable` | Object Pool Pattern |
| **Data Management** | Data-driven design for game balancing | `HealthConfig`, `MovementConfig`, `EnemyData`, `AudioData` | Data-Driven SOs |
| **Wave System** | Data-driven wave spawning from CSV | `CsvWaveParser`, `WaveManager`, `EnemySpawner` | Data Parsing |
| **Audio Middleware** | Event-driven, pooled audio | `AudioController`, `AudioPoolHandler` | Event-Driven Audio |
| **Update Manager** | Control update frequency for performance | `UpdateManager`, `IUpdatable` | Custom Update System |

---

## Data Flow Architecture

```
┌─────────────┐   Services    ┌──────────────┐
│GameBootstrapper│────────────▶│   Services   │
└─────────────┘              └──────────────┘
         │                          │
         │ registers                │ Get<T>()
         ▼                          ▼
┌─────────────┐              ┌──────────────┐
│ObjectPool   │              │Event Registries│
│  Manager    │              │ Combat/Wave/  │
│  Manager    │              │ Economy/Game  │
└─────────────┘              └──────────────┘

┌─────────────┐  EventChannel  ┌─────────────┐
│   Towers    │───────────────▶│    UI       │
└─────────────┘                └─────────────┘
         │                            ▲
         ▼ (Projectiles)             │ (Gold/Lives)
┌─────────────┐  EventChannel  ┌─────────────┐
│   Enemies   │───────────────▶│ PlayerStats │
└─────────────┘                └─────────────┘
         │                            │
         ▼ (WaveEvents)               ▼ (EconomyEvents)
┌─────────────┐                ┌─────────────┐
│Wave Manager │◀───────────────│ Tower Shop  │
└─────────────┘  EventChannel   └─────────────┘
         │
         ▼ (CSV Parse)
┌─────────────┐
│Enemy Spawner│──▶ Services.Get<ObjectPoolManager>() ──▶ EnemyController
└─────────────┘

┌─────────────┐  Services.Get<CombatEvents>()  ┌──────────────┐
│ Game Events │────────────────────────────────▶│AudioController│──▶ AudioPoolHandler
└─────────────┘                                  └──────────────┘    └▶ AudioMixer Groups
```

---

## Directory Structure

```
Assets/
├── Scripts/
│   ├── Audio/                     # Audio middleware
│   │   ├── AudioPoolHandler.cs
│   │   └── AudioController.cs    # Subscribes to event registries
│   ├── Core/                      # Service locator & bootstrapper
│   │   ├── Services.cs           # Static service locator
│   │   └── GameBootstrapper.cs   # Composition root
│   ├── Data/                      # ScriptableObject configs (pure data)
│   │   ├── AudioData.cs
│   │   ├── DamageResult.cs       # Readonly struct for TakeDamage return
│   │   ├── EnemyData.cs          # Holds HealthConfig + MovementConfig refs
│   │   ├── HealthConfig.cs       # Pure config SO + HealthType enum
│   │   └── MovementConfig.cs     # Pure config SO + MovementType enum
│   ├── Enemies/                   # Enemy system
│   │   ├── Components/
│   │   │   └── EnemyHealthBar.cs
│   │   └── Controllers/
│   │       └── EnemyController.cs # Implements IDamageable, ITargetable, IPoolable
│   ├── Events/                    # Pure C# event channels
│   │   ├── EventChannel.cs       # Void variant
│   │   ├── EventChannelT.cs      # Typed variant <T>
│   │   └── Registries/           # Feature-organized event containers
│   │       ├── CombatEvents.cs
│   │       ├── WaveEvents.cs
│   │       ├── EconomyEvents.cs
│   │       └── GameEvents.cs
│   ├── Interfaces/                # Behavior contracts
│   │   ├── IDamageable.cs
│   │   ├── IHealthStrategy.cs
│   │   ├── IMovementStrategy.cs
│   │   ├── ITargetingStrategy.cs
│   │   ├── IPoolable.cs
│   │   ├── ISelectable.cs
│   │   ├── ITargetable.cs
│   │   └── IUpdatable.cs
│   ├── Projectiles/               # Projectile system
│   │   ├── ProjectileBase.cs
│   │   ├── ArrowProjectile.cs
│   │   └── BombProjectile.cs
│   ├── Strategies/                # Strategy pattern implementations (plain C# classes)
│   │   ├── Health/
│   │   │   ├── NormalHealth.cs
│   │   │   ├── ArmouredHealth.cs
│   │   │   ├── ShieldHealth.cs
│   │   │   └── RegenHealth.cs
│   │   ├── Movement/
│   │   │   ├── GroundedPath.cs
│   │   │   └── FlyingPath.cs
│   │   └── Targeting/
│   │       ├── FirstTargeting.cs
│   │       ├── LastTargeting.cs
│   │       ├── StrongTargeting.cs
│   │       └── CloseTargeting.cs
│   ├── Systems/                   # Game systems and managers
│   │   ├── Game/
│   │   │   ├── AudioController.cs
│   │   │   ├── EnemyPath.cs
│   │   │   ├── EnemySpawner.cs
│   │   │   ├── PlayerStats.cs    # Plain C# class, registered in Services
│   │   │   ├── ShopController.cs
│   │   │   └── TowerNode.cs
│   │   ├── Managers/
│   │   │   ├── ObjectPoolManager.cs  # Registered in Services
│   │   │   ├── UpdateManager.cs      # Registered in Services
│   │   │   └── WaveManager.cs
│   │   └── Parsing/
│   │       ├── CsvWaveParser.cs
│   │       └── StrategyFactory.cs    # Creates strategies from config enum
│   └── Towers/                    # Tower system
│       ├── TowerController.cs
│       ├── TowerDetection.cs
│       └── TowerFiring.cs
├── Data/                          # SO asset instances + CSV
│   └── Waves/
│       └── wave_data.csv
├── Prefabs/
├── Scenes/
├── UI/
└── Audio/
```

---

## Key Design Decisions

### 1. Pure C# Interfaces + Factory (NOT Abstract ScriptableObjects)
- **Why**: SOs are shared references — `CurrentHealth` on a health strategy SO would be shared across all enemies using that SO
- **Implementation**: `IHealthStrategy`, `IMovementStrategy`, `ITargetingStrategy` as pure interfaces. `StrategyFactory` creates instances from `HealthConfig`/`MovementConfig` type enums
- **Benefit**: Each enemy gets its own strategy instance, no shared-state bug, no `Instantiate()` workaround needed

### 2. Pure C# Event Channels (NOT ScriptableObject Events)
- **Why**: SO event channels require .asset files, cause merge conflicts, and can't be easily tested
- **Implementation**: `EventChannel<T>` and `EventChannel` base classes, organized into registries (`CombatEvents`, `WaveEvents`, etc.) registered in `Services`
- **Benefit**: No missing references, no merge conflicts, reusable across engines/languages, consistent `Services.Get<T>()` access

### 3. Service Locator (NOT Multiple Singletons)
- **Why**: Scattered `.Instance` singletons create tight coupling and inconsistent access patterns
- **Implementation**: `Services` static class with `Register<T>`/`Get<T>`/`Clear`. `GameBootstrapper` as composition root
- **Benefit**: One access pattern for everything, easy to test and swap, upgrade path to DI

### 4. Strategy Pattern Composition
- **Why**: Add enemy/tower behaviors without modifying existing code
- **Implementation**: Plain C# classes implementing strategy interfaces, created by `StrategyFactory`
- **Benefit**: New types = new config + factory case. Zero changes to `EnemyController`, `TowerDetection`, `ProjectileBase`

### 5. Performance Optimizations
- **Custom Update Manager**: Batched updates by priority (High/Medium/Low tick intervals)
- **Object Pooling**: Eliminates Instantiate/Destroy overhead and GC spikes
- **Interface-based Systems**: Enables efficient targeting and damage
- **Pooled AudioSource**: Eliminates one-shot SFX allocation spikes

---

## Integration Examples

### Tower Placement Flow:
1. Player selects tower in `TowerShop` UI
2. `TowerNode` places tower at valid position
3. `GameEvents.TowerPlaced.Raise()` via Services
4. `PlayerStats` deducts gold (subscribes to `EconomyEvents.GoldChanged`)

### Enemy Death Flow:
1. Tower projectile hits enemy
2. `IHealthStrategy.TakeDamage()` returns `DamageResult` with `Died = true`
3. `EnemyController.Die()` raises `Services.Get<CombatEvents>().EnemyDeath.Raise(GoldGiven)`
4. `PlayerStats` adds gold (subscribes to `CombatEvents.EnemyDeath`)
5. `AudioController` plays death sound (subscribes to `CombatEvents.EnemyDeath`)
6. `ObjectPoolManager` returns enemy to pool

### Wave Start Flow:
1. `WaveManager.StartNextWave()` parses next wave batch from CSV
2. Passes batch entries to `EnemySpawner.StartBatch(entries, path)`
3. Spawner starts coroutines per entry with spawn intervals
4. Each spawn fetches from `Services.Get<ObjectPoolManager>()`
5. `Services.Get<WaveEvents>().WaveStarted.Raise(waveNumber)`

---

## Performance Considerations

| System | Optimization | Impact |
|--------|-------------|--------|
| **Update Manager** | Batched updates by priority | Reduces CPU overhead by 40-60% |
| **Object Pooling** | Reuse projectiles/enemies/audio sources | Eliminates GC spikes |
| **Event System** | Delegate-based callbacks | Minimal overhead for communication |
| **Data Access** | Cached ScriptableObject references | Faster than Resources.Load |
| **Audio Pooling** | Pooled one-shot AudioSource | No allocation per sound play |
| **CSV Parsing** | One-time parse on Awake | No runtime parsing overhead |
| **Service Locator** | Dictionary lookup | Negligible vs direct reference |