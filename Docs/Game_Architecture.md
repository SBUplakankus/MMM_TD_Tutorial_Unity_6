# Game Architecture

## Project Overview
This tower defense game implements intermediate Unity patterns with a focus on clean architecture, performance optimization, and maintainability.

---

## Core Systems

| System | Purpose | Key Components | Pattern Used |
|--------|---------|----------------|--------------|
| **Event System** | Decouple game systems through event-driven communication | `VoidEventChannel`, `GameEvent<T>`, `GameEventListener` | Observer Pattern |
| **Data Management** | Data-driven design for game balancing | `TowerData`, `EnemyData`, `WaveData` ScriptableObjects | ScriptableObjects |
| **Object Pooling** | Optimize performance by reusing objects | `ObjectPool<T>`, `ProjectilePool`, `EnemyPool` | Object Pool Pattern |
| **Update Manager** | Control update frequency for performance | `UpdateManager`, `IUpdatable` interface | Custom Update System |
| **Game Constants** | Centralize configuration values | `GameConstants` static class | Constants Pattern |

---

## Data Flow Architecture

```
┌─────────────┐    Events    ┌─────────────┐
│   Towers    │──────────────▶│    UI       │
└─────────────┘              └─────────────┘
        │                          ▲
        ▼ (Projectiles)            │ (Gold/Health Updates)
┌─────────────┐    Events    ┌─────────────┐
│   Enemies   │──────────────▶│  Economy    │
└─────────────┘              └─────────────┘
        │                          │
        ▼ (Wave Events)            ▼ (Purchase Events)
┌─────────────┐              ┌─────────────┐
│Wave Manager │◀─────────────│ Tower Shop  │
└─────────────┘    Events    └─────────────┘
```

---

## Directory Structure

```
Assets/
├── Scripts/
│   ├── Core/                 # Architectural foundation
│   │   ├── GameConstants.cs
│   │   ├── UpdateManager.cs
│   │   └── GameEvents.cs
│   ├── Data/                 # ScriptableObject definitions
│   │   ├── TowerData.cs
│   │   ├── EnemyData.cs
│   │   └── WaveData.cs
│   ├── Patterns/             # Design pattern implementations
│   │   ├── ObjectPool.cs
│   │   └── EventChannels/
│   ├── Tower/
│   │   ├── BaseTower.cs     # Abstract class
│   │   ├── BasicTower.cs
│   │   └── Targeting/
│   ├── Enemy/
│   │   ├── BaseEnemy.cs     # Abstract class
│   │   └── EnemyAI.cs
│   └── UI/
├── Data/                     # SO asset instances
│   ├── Towers/
│   ├── Enemies/
│   └── Waves/
├── Prefabs/
├── Scenes/
├── UI/
└── Audio/
```

---

## Key Design Decisions

### 1. Event-Driven Communication
- **Why**: Decouples systems for better maintainability
- **Implementation**: ScriptableObject Event Channels
- **Benefit**: Easy testing and system isolation

### 2. Data-Driven Design
- **Why**: Non-programmer friendly balancing
- **Implementation**: ScriptableObjects for all configurable data
- **Benefit**: Quick iteration without code changes

### 3. Performance Optimizations
- **Custom Update Manager**: Reduces unnecessary Update() calls
- **Object Pooling**: Eliminates Instantiate/Destroy overhead
- **Interface-based Systems**: Enables efficient targeting and damage

### 4. Abstraction Layers
- **Base Classes** (`BaseTower`, `BaseEnemy`): Shared functionality
- **Interfaces** (`IDamageable`, `ITargetable`): Contract-based design
- **Benefit**: Easy extensibility and code reuse

---

## Integration Examples

### Tower Placement Flow:
1. Player selects tower in `TowerShop` UI
2. `TowerPlacement` system activates with `TowerData`
3. Valid placement triggers `OnTowerPlaced` event
4. `EconomyManager` deducts gold (listens to event)
5. `AudioManager` plays build sound (listens to event)
6. `UIManager` updates display (listens to event)

### Enemy Death Flow:
1. Tower projectile hits enemy
2. Enemy triggers `OnEnemyDeath` event with reward value
3. `EconomyManager` adds gold (listener)
4. `WaveManager` tracks remaining enemies (listener)
5. `AchievementSystem` checks for milestones (listener)
6. Object Pool returns enemy to pool

---

## Performance Considerations

| System | Optimization | Impact |
|--------|-------------|---------|
| **Update Manager** | Batched updates by priority | Reduces CPU overhead by 40-60% |
| **Object Pooling** | Reuse projectiles/enemies | Eliminates GC spikes |
| **Event System** | Delegate-based callbacks | Minimal overhead for communication |
| **Data Access** | Cached ScriptableObject references | Faster than Resources.Load |

This architecture provides a scalable foundation that teaches professional Unity development patterns while keeping the game mechanics accessible for learning.

---