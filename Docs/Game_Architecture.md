# Game Architecture

## Project Overview
This tower defense game implements intermediate Unity patterns with a focus on clean architecture, performance optimization, and maintainability.

---

## Core Systems

| System | Purpose | Key Components | Pattern Used |
|--------|---------|----------------|--------------|
| **Event System** | Decouple game systems through event-driven communication | `VoidEventChannel`, `TypedEventChannel<T>`, `AudioEventLinker` | Observer Pattern |
| **Data Management** | Data-driven design for game balancing | `EnemyData`, `AudioData`, `AudioEventLinker` ScriptableObjects | ScriptableObjects |
| **Object Pooling** | Optimize performance by reusing objects | `ObjectPoolManager`, `PoolConfig`, `IPoolable` | Object Pool Pattern |
| **Update Manager** | Control update frequency for performance | `UpdateManager`, `IUpdatable` interface | Custom Update System |
| **Game Constants** | Centralize configuration values | `GameConstants` static class | Constants Pattern |
| **Strategy System** | Compose enemy behaviors without inheritance | `HealthStrategy`, `MovementStrategy`, `TargetingStrategy` | Strategy Pattern |
| **Audio Middleware** | Event-driven, data-driven audio with no singletons | `AudioController`, `AudioPoolHandler`, `AudioEventLinker` | Event-Driven Audio |

---

## Data Flow Architecture

```
┌─────────────┐    Events    ┌─────────────┐
│   Towers    │──────────────▶│    UI       │
└─────────────┘              └─────────────┘
        │                          ▲
        ▼ (Projectiles)           │ (Gold/Health Updates)
┌─────────────┐    Events    ┌─────────────┐
│   Enemies   │──────────────▶│  PlayerStats│
└─────────────┘              └─────────────┘
        │                          │
        ▼ (Wave Events)            ▼ (Purchase Events)
┌─────────────┐              ┌─────────────┐
│Wave Manager │◀─────────────│ Tower Shop  │
└─────────────┘    Events    └─────────────┘
        │
        ▼ (CSV Parse)
┌─────────────┐
│Enemy Spawner│──▶ ObjectPoolManager ──▶ EnemyController
└─────────────┘

┌─────────────┐    EventLinkers   ┌──────────────┐
│ Game Events │──────────────────▶│AudioController│──▶ AudioPoolHandler
└─────────────┘                   └──────────────┘    └▶ AudioMixer Groups
```

---

## Directory Structure

```
Assets/
├── Scripts/
│   ├── Audio/                     # Audio middleware
│   │   ├── AudioPoolHandler.cs
│   │   └── AudioEventLinker.cs
│   ├── Base/                      # Shared components
│   │   └── HealthComponent.cs
│   ├── Data/                      # ScriptableObject definitions
│   │   ├── AudioData.cs
│   │   └── EnemyData.cs
│   ├── Enemies/                   # Enemy system
│   │   ├── Components/
│   │   │   └── EnemyHealthbar.cs
│   │   └── Controllers/
│   │       └── EnemyController.cs
│   ├── Events/                    # SO event channels
│   │   ├── EventChannelBase.cs
│   │   ├── VoidEventChannel.cs
│   │   └── TypedEventChannel.cs
│   ├── Interfaces/                # Behavior contracts
│   │   ├── IDamageable.cs
│   │   ├── IPoolable.cs
│   │   ├── ISelectable.cs
│   │   ├── ITargetable.cs
│   │   └── IUpdatable.cs
│   ├── Projectiles/               # Projectile system
│   │   ├── ProjectileBase.cs
│   │   ├── ArrowProjectile.cs
│   │   └── BombProjectile.cs
│   ├── Strategies/                # Strategy pattern implementations
│   │   ├── Health/
│   │   │   ├── HealthStrategy.cs
│   │   │   ├── NormalHealth.cs
│   │   │   ├── ArmouredHealth.cs
│   │   │   ├── ShieldHealth.cs
│   │   │   └── RegenHealth.cs
│   │   ├── Movement/
│   │   │   ├── MovementStrategy.cs
│   │   │   ├── GroundedPath.cs
│   │   │   └── FlyingPath.cs
│   │   └── Targeting/
│   │       └── TargetingStrategy.cs
│   ├── Systems/                   # Game systems and managers
│   │   ├── Game/
│   │   │   ├── AudioController.cs
│   │   │   ├── EnemyPath.cs
│   │   │   ├── EnemySpawner.cs
│   │   │   ├── PlayerStats.cs
│   │   │   ├── ShopController.cs
│   │   │   └── TowerNode.cs
│   │   ├── Managers/
│   │   │   ├── EnemyManager.cs
│   │   │   ├── ObjectPoolManager.cs
│   │   │   ├── TowerManager.cs
│   │   │   ├── UpdateManager.cs
│   │   │   └── WaveManager.cs
│   │   └── Parsing/
│   │       └── CsvWaveParser.cs
│   └── Towers/                    # Tower system
│       ├── TowerController.cs
│       ├── TowerDetection.cs
│       └── TowerFiring.cs
├── Data/                          # SO asset instances
│   └── Waves/
│       └── wave_data.csv
├── Prefabs/
├── Scenes/
├── UI/
└── Audio/
```

---

## Key Design Decisions

### 1. Event-Driven Communication
- **Why**: Decouples systems for better maintainability
- **Implementation**: ScriptableObject Event Channels + AudioEventLinker
- **Benefit**: Easy testing and system isolation, zero singletons for audio

### 2. Data-Driven Design
- **Why**: Non-programmer friendly balancing
- **Implementation**: ScriptableObjects for all configurable data, CSV for wave data
- **Benefit**: Quick iteration without code changes, runtime wave editing

### 3. Strategy Pattern Composition
- **Why**: Add enemy/tower behaviors without modifying existing code
- **Implementation**: Abstract Strategy SOs for Health, Movement, Targeting
- **Benefit**: Mix-and-match strategies to create new types from existing pieces

### 4. Performance Optimizations
- **Custom Update Manager**: Reduces unnecessary Update() calls
- **Object Pooling**: Eliminates Instantiate/Destroy overhead
- **Interface-based Systems**: Enables efficient targeting and damage
- **Pooled AudioSource**: Eliminates one-shot SFX allocation spikes

---

## Integration Examples

### Tower Placement Flow:
1. Player selects tower in `TowerShop` UI
2. `TowerPlacement` system activates with tower data
3. Valid placement triggers `OnTowerPlaced` event
4. `PlayerStats` deducts gold (listens to event)
5. `AudioController` plays build sound (via AudioEventLinker)
6. UI updates display (listens to event)

### Enemy Death Flow:
1. Tower projectile hits enemy
2. Enemy triggers `OnEnemyDeath` event with reward value
3. `PlayerStats` adds gold (listener)
4. `WaveManager` tracks remaining enemies (listener)
5. `AudioController` plays death sound (via AudioEventLinker)
6. Object Pool returns enemy to pool

### Wave Start Flow:
1. `WaveManager.StartNextWave()` parses next wave batch
2. Passes batch entries to `EnemySpawner.StartBatch()`
3. Spawner starts coroutines per entry with spawn intervals
4. Each spawn fetches from `ObjectPoolManager`
5. `OnWaveStarted` event raised for UI and audio

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

---