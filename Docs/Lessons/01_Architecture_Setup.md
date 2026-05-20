# Episode 01: Architecture & Project Setup

!!! info "Episode Type: Walkthrough"
    This episode is a **code-reading walkthrough** — no code changes. You'll explore the repo structure, understand the architecture, and see every stub that will be filled across the course.

---

## Video

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden;">
  <iframe src="https://www.youtube.com/embed/EP01_PLACEHOLDER" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%;" frameborder="0" allowfullscreen></iframe>
</div>

**Duration:** ~10 min

---

## Learning Objectives

By the end of this episode you will be able to:

1. Navigate the `Assets/Scripts/` directory structure and explain what each subfolder contains
2. Identify which classes are **implemented** vs which are **empty stubs**
3. Read the data flow diagram and trace how Towers, Projectiles, Enemies, Events, and the WaveManager connect
4. Explain the design philosophy: decoupled systems, data-driven via ScriptableObjects, strategy pattern for composition

---

## Prerequisites

- **Unity 6.3** installed (LTS)
- C# familiarity — you should be comfortable reading class definitions, interfaces, and basic Unity lifecycle methods
- **PrimeTween** from the Unity Asset Store (used for tweening; see [Tweening](../Concepts/Tweening.md))

---

## Key Concepts

| Concept | Role in This Project | Learn More |
|---------|---------------------|------------|
| Decoupled systems | Event channels connect systems without direct references | [Observer Pattern](../Concepts/Observer_Pattern.md) |
| Data-driven design | ScriptableObjects hold all configurable values | [Scriptable Objects](../Concepts/Scriptable_Objects.md) |
| Strategy pattern | Health & Movement are pluggable SO strategies | [Abstraction](../Concepts/Abstraction.md) |
| Interface contracts | `IDamageable`, `ITargetable`, etc. define behaviour without coupling | [Interfaces](../Concepts/Interfaces.md) |

---

## Code Roadmap

This episode touches no code — it's a reading tour. Every file listed below will be **implemented** in later episodes.

---

## Architecture Context

For the full architecture document, see [Game Architecture](../Game_Architecture.md) and [Course Concepts](../Concepts.md).

---

## Step-by-Step Walkthrough

### Step 1: Clone the repo, open in Unity, verify the scene loads

```
git clone <repo-url>
```

1. Open the project in **Unity 6.3**.
2. Open the main game scene.
3. Press **Play** — you should see an empty play area with a path. Nothing happens yet because systems are stubs.

!!! tip "If Unity prompts for script compilation errors"
    Make sure PrimeTween is installed from the Asset Store. The project references it for tweening.

---

### Step 2: Walk the project structure — `Scripts/` subfolders

```
Assets/Scripts/
├── Audio/                        # Audio playback — pool handler & event linker
│   ├── AudioEventLinker.cs       # SO bridge: VoidEventChannel → AudioData
│   └── AudioPoolHandler.cs       # Pooled AudioSource wrapper
├── Base/                         # Shared base components
│   └── HealthComponent.cs        # Stub — generic health component
├── Data/                         # ScriptableObject data definitions
│   ├── AudioData.cs              # Audio clip configuration SO
│   └── EnemyData.cs              # Enemy strategy composition SO
├── Enemies/
│   ├── Components/
│   │   └── EnemyHealthbar.cs     # Stub — health bar UI on enemy
│   └── Controllers/
│       └── EnemyController.cs    # Main enemy MonoBehaviour (implemented)
├── Events/                       # Observer pattern event channels
│   ├── EventChannelBase.cs       # Optional base class
│   ├── TypedEventChannel.cs      # Generic event channel <T>
│   └── VoidEventChannel.cs       # No-payload event channel
├── Interfaces/                   # Behaviour contracts
│   ├── IDamageable.cs            # TakeDamage(float)
│   ├── IPoolable.cs              # Reset()
│   ├── ISelectable.cs            # OnSelected / OnDeselected
│   ├── ITargetable.cs            # Position, IsAlive
│   └── IUpdatable.cs             # Tick()
├── Projectiles/                  # Projectile types
│   ├── ArrowProjectile.cs        # Piercing arrow (stub)
│   ├── BombProjectile.cs         # AOE bomb (stub)
│   └── ProjectileBase.cs         # Base class with pooling (stub)
├── Strategies/                   # Pluggable SO strategies
│   ├── Health/
│   │   ├── ArmouredHealth.cs     # % damage reduction (implemented)
│   │   ├── HealthStrategy.cs     # Abstract base SO (implemented)
│   │   ├── NormalHealth.cs       # Straight subtraction (implemented)
│   │   ├── RegenHealth.cs        # Health regen over time (partial)
│   │   └── ShieldHealth.cs       # Shield-then-health (stub)
│   ├── Movement/
│   │   ├── FlyingPath.cs         # Elevated waypoint following (implemented)
│   │   ├── GroundedPath.cs       # Ground waypoint following (implemented)
│   │   └── MovementStrategy.cs   # Abstract base SO (implemented)
│   └── Targeting/
│       └── TargetingStrategy.cs   # Abstract base SO (implemented)
├── Systems/
│   ├── Game/                     # Core game systems
│   │   ├── AudioController.cs    # Audio playback manager (stub)
│   │   ├── EnemyPath.cs          # Waypoint path data (implemented)
│   │   ├── EnemySpawner.cs       # Coroutine-based spawning (stub)
│   │   ├── PlayerStats.cs        # Stub
│   │   ├── ShopController.cs     # Stub
│   │   └── TowerNode.cs          # Stub
│   ├── Managers/                 # Global managers
│   │   ├── EnemyManager.cs       # Stub
│   │   ├── ObjectPoolManager.cs  # Pool manager (stub)
│   │   ├── TowerManager.cs       # Stub
│   │   ├── UpdateManager.cs      # Custom tick manager (stub)
│   │   └── WaveManager.cs        # Wave progression (stub)
│   └── Parsing/
│       └── CsvWaveParser.cs      # CSV wave data parser (stub)
└── Towers/                       # Tower systems
    ├── TowerController.cs        # Tower root controller (stub)
    ├── TowerDetection.cs         # Target scanning (stub)
    └── TowerFiring.cs            # Projectile dispatch (stub)
```

---

### Step 3: Walk existing **implemented** code

These files contain working logic already:

| File | What It Does |
|------|-------------|
| `EnemyController.cs` | Main enemy MonoBehaviour — holds `HealthStrategy` and `MovementStrategy` references, delegates `Tick()` and `TakeDamage()` to them. Implements `IDamageable` and `ITargetable`. |
| `EnemyData.cs` | ScriptableObject combining a `HealthStrategy` ref + `MovementStrategy` ref + stats. The composition hub for enemy types. |
| `EnemyPath.cs` | Holds a `Transform[]` of waypoints. Provides `GetWaypointPosition()`, `HasWaypoint()`, `IsAtWaypoint()` for movement strategies. |
| `HealthStrategy.cs` | Abstract SO base — `startHealth`, `CurrentHealth`, `OnDeath` event, `Initialize()`, `TakeDamage()` abstract methods. |
| `NormalHealth.cs` | Straight `CurrentHealth -= amount` damage. |
| `ArmouredHealth.cs` | Reduced damage: `amount * (1 - armourPercent)`. |
| `MovementStrategy.cs` | Abstract SO base — `moveSpeed`, `Path` ref, `OnMovementCompletion` event, `Initialize()`, abstract `Tick()` and `SetStartPosition()`. |
| `GroundedPath.cs` | Waypoint-to-waypoint movement along the ground plane. |
| `FlyingPath.cs` | Same waypoints + `flyingHeight` Y offset for flying enemies. |
| `TargetingStrategy.cs` | Abstract SO base for tower target selection (`First`, `Last`, `Strong`, `Close`). |
| `IDamageable.cs` | `TakeDamage(float)` contract. |
| `ITargetable.cs` | `Position` and `IsAlive` contract. |
| `ISelectable.cs` | `OnSelected()` / `OnDeselected()` contract. |
| `IUpdatable.cs` | `Tick()` contract for custom update manager. |
| `IPoolable.cs` | `Reset()` contract for object pooling. |
| `TypedEventChannel<T>` | Generic SO event channel with `Raise(T)`, `Subscribe()`, `Unsubscribe()`. |
| `VoidEventChannel.cs` | No-payload event channel — `Raise()`, `Subscribe()`, `Unsubscribe()`. |
| `AudioEventLinker.cs` | SO bridging a `VoidEventChannel` to an `AudioData` — pure data wiring, no code needed. |
| `AudioData.cs` | SO holding audio clips, mixer group, pitch range, spatial settings — property accessors implemented, `GetRandomClip()` and `GetRandomPitch()` are stubs. |

---

### Step 4: Walk empty **stubs**

These files exist but contain only empty MonoBehaviours or `// TODO` comments. They'll be filled across the course:

| File | Purpose (to be implemented) |
|------|------------------------------|
| `ObjectPoolManager.cs` | Singleton pool manager — `PoolConfig[]`, dictionary of `ObjectPool<GameObject>`, `Get()`, `Return()`, `ReturnDelayed()` |
| `UpdateManager.cs` | Singleton tick manager — `IUpdatable` registration, priority-based tick intervals |
| `WaveManager.cs` | Wave progression controller — CSV parsing, wave state machine, spawner coordination |
| `EnemySpawner.cs` | Coroutine-based enemy spawning from pool |
| `EnemyManager.cs` | Enemy tracking / counting |
| `TowerManager.cs` | Tower placement tracking |
| `TowerController.cs` | Tower root — detection → firing pipeline |
| `TowerDetection.cs` | Sphere overlap scanning for `ITargetable` enemies |
| `TowerFiring.cs` | Projectile dispatch from pool with cooldown |
| `ProjectileBase.cs` | Base projectile with pooling, `Launch()`, `Move()`, `OnHit()` |
| `ArrowProjectile.cs` | Piercing arrow — non-homing, multi-hit |
| `BombProjectile.cs` | AOE explosion — non-homing, area damage |
| `AudioController.cs` | Audio system — event-driven playback via pool |
| `AudioPoolHandler.cs` | Pooled `AudioSource` wrapper |
| `PlayerStats.cs` | Player gold, lives, score |
| `ShopController.cs` | Tower shop UI + purchase logic |
| `TowerNode.cs` | Tower placement node on the grid |
| `EnemyHealthbar.cs` | Health bar UI following enemy |
| `HealthComponent.cs` | Generic reusable health component |
| `RegenHealth.cs` | Health strategy with regeneration (partial — `Regen()` is stub) |
| `ShieldHealth.cs` | Shield-then-health damage strategy |
| `CsvWaveParser.cs` | Parse wave definition CSV into `WaveEntry` structs |

---

### Step 5: Data flow architecture

The diagram below shows how the major systems communicate. Events (drawn as `≈≈≈`) decouple systems so they never hold direct references to each other.

```
┌──────────────────┐                      ┌──────────────────┐
│   WaveManager    │──── Spawns batch ───▶│  EnemySpawner   │
└──────────────────┘                      └──────────────────┘
        │                                          │
        │ ≈≈≈ onWaveStarted                       │ Get from pool
        │ ≈≈≈ onWaveComplete                       ▼
        ▼                                  ┌──────────────────┐
┌──────────────────┐   ≈≈≈ onEnemyDeath   │ ObjectPoolManager│
│  PlayerStats     │◀──────────────────── │                  │
│  (Gold, Lives)   │                      └──────────────────┘
└──────────────────┘                               ▲
        │                                          │ Return to pool
        │ ≈≈≈ onGoldChanged                        │
        ▼                                          │
┌──────────────────┐   Fetch projectile      ┌─────┴────────────┐
│       UI         │◀────────────────────    │  Projectiles     │
│ (HUD, Shop,     │   ≈≈≈ onUIRefresh       │ ArrowProjectile  │
│  Healthbars)    │                          │ BombProjectile   │
└──────────────────┘                          └──────────────────┘
        ▲                                              ▲
        │ ≈≈≈ onTowerPlaced                           │ OnHit → TakeDamage
        │                                              │
┌───────┴──────────┐                           ┌───────┴──────────┐
│   TowerNode      │──── Places tower ────▶    │ TowerController  │
│  (placement)     │                           │  ┌─Detection     │
└──────────────────┘                           │  └─Firing ───────┼──▶ Get from pool
                                               └──────────────────┘

   Key:
   ──── Direct call (method invocation, object reference)
   ≈≈≈ Event channel (decoupled — observer pattern)
   ───▶ Data flow direction
```

**Trace a complete flow — Enemy death:**

1. `TowerFiring` fetches projectile from `ObjectPoolManager.Get()`
2. Projectile `Move()` → `OnHit()` calls `IDamageable.TakeDamage()` on enemy
3. `EnemyController` delegates to `HealthStrategy.TakeDamage()`
4. `HealthStrategy.CheckForDeath()` fires `OnDeath` event
5. `EnemyController.Die()` raises `onEnemyDeath` event with `GoldGiven`
6. `PlayerStats` listens → adds gold → raises `onGoldChanged`
7. `UI` listens → updates HUD
8. `ObjectPoolManager.Return()` deactivates the enemy, calls `IPoolable.Reset()`

!!! tip "Why events?"
    Notice that `EnemyController` never references `PlayerStats` or `UI`. Events let you add new listeners (e.g., achievements, analytics) without modifying the enemy code at all. Read more: [Observer Pattern](../Concepts/Observer_Pattern.md).

---

## Design Philosophy

Three pillars drive every decision in this codebase:

1. **Decoupled systems** — Event channels (`VoidEventChannel`, `TypedEventChannel<T>`) replace direct references. Systems talk through events, not through each other.

2. **Data-driven via ScriptableObjects** — Every configurable value lives in an SO. Designers can create new enemy types, tweak health, adjust move speed — all without touching code. See [Scriptable Objects](../Concepts/Scriptable_Objects.md).

3. **Strategy pattern for composition** — `EnemyController` doesn't *know* whether it's armoured or flying. It *delegates* to `HealthStrategy` and `MovementStrategy`. Adding "Armoured Flying" means wiring a new `EnemyData` SO — zero code changes. See [Abstraction](../Concepts/Abstraction.md).

---

## Episode Recap

- The repo is structured into focused subfolders: `Interfaces/`, `Strategies/`, `Systems/`, `Towers/`, `Enemies/`, `Data/`, `Events/`, `Audio/`, `Projectiles/`
- **Implemented code**: `EnemyController`, `EnemyData`, `EnemyPath`, both strategy hierarchies, all interfaces, event channels
- **Stubs**: ~20 files with `// TODO` comments — you'll fill these across the course
- Data flows through **events** (decoupled) and **direct calls** (within a system)
- The strategy pattern lets you compose new enemy types by wiring SOs in the Inspector

---

## Challenge

Draw your own data flow diagram adding a hypothetical **Achievement System**. Consider:

- What events would the Achievement System listen to? (`onEnemyDeath`, `onWaveComplete`, `onTowerPlaced`…)
- What events would it raise? (`onAchievementUnlocked` — what payload?)
- Which existing systems would react to achievement events? (UI for toast notification, AudioController for sound effect)
- Does the Achievement System need `IUpdatable`? Why or why not?

Sketch the diagram on paper or in a tool like draw.io, then add your new connections to the ASCII diagram above.