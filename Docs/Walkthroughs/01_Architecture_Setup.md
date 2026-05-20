# Episode 1: Architecture & Project Setup — Implementation Guide

## What You're Building
No C# code this episode. You're verifying the existing architecture works end-to-end: scene hierarchy, service locator, event registries, enemy movement along waypoints, damage, and death. You're also establishing a profiler baseline and a pre-warm checklist for future episodes.

**Core architecture pattern:** Services are registered in `GameBootstrapper` (the composition root) and accessed everywhere via `Services.Get<T>()`. Event registries (`CombatEvents`, `WaveEvents`, `EconomyEvents`, `GameEvents`) are plain C# classes registered the same way — no ScriptableObject event channels. This is the ONE place all wiring happens.

## Files & Order
No new files. You'll create a temporary test script and then delete it. Existing files to verify:
1. `Assets/Scripts/Core/Services.cs` — static service locator
2. `Assets/Scripts/Core/GameBootstrapper.cs` — composition root MonoBehaviour
3. `Assets/Scripts/Events/Registries/CombatEvents.cs` — combat event registry
4. `Assets/Scripts/Events/Registries/WaveEvents.cs` — wave event registry
5. `Assets/Scripts/Events/Registries/EconomyEvents.cs` — economy event registry
6. `Assets/Scripts/Events/Registries/GameEvents.cs` — game event registry
7. `Assets/Scripts/Enemies/Controllers/EnemyController.cs` — already implements IDamageable + ITargetable
8. `Assets/Scripts/Systems/Parsing/StrategyFactory.cs` — creates strategies from config
9. `Assets/Scripts/Systems/Game/PlayerStats.cs` — plain C# class, no MonoBehaviour
10. `Assets/Scripts/Data/EnemyData.cs` — already has CreateAssetMenu

## Implementation

### GameBootstrapper Setup

The `GameBootstrapper` is the composition root — the **one** place where all services are registered and wired together. No other script should hold cross-system references or do manual wiring.

**Registration order in `Awake()` matters:**

1. **MonoBehaviour services** (scene instances) — `ObjectPoolManager`, `UpdateManager` — these need to be in the scene and registered first because other services may depend on them
2. **Event registries** (plain C#) — `CombatEvents`, `WaveEvents`, `EconomyEvents`, `GameEvents` — these are `new`'d here and registered. They must exist before anything subscribes
3. **Plain C# services** — `PlayerStats` — constructed here with dependencies, registered after registries so it can subscribe to events in its constructor/Initialize
4. **MonoBehaviours that subscribe** — `AudioController`, `WaveManager`, `EnemySpawner` — these subscribe in their own `OnEnable()` after all services are registered

**Cleanup in `OnDestroy()`:**
1. Call `Cleanup()` on `PlayerStats` (unsubscribes from events)
2. Call `Clear()` on every event registry (prevents leaked subscriptions on scene reload)
3. Call `Services.Clear()` (removes all registered services)

```csharp
using Events.Registries;
using Systems.Game;
using Systems.Managers;
using UnityEngine;

namespace Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private ObjectPoolManager objectPoolManager;
        [SerializeField] private UpdateManager updateManager;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private AudioController audioController;
        [SerializeField] private EnemySpawner enemySpawner;

        [Header("Game Config")]
        [SerializeField] private int startingGold = 100;
        [SerializeField] private int startingLives = 20;

        private CombatEvents _combatEvents;
        private WaveEvents _waveEvents;
        private EconomyEvents _economyEvents;
        private GameEvents _gameEvents;
        private PlayerStats _playerStats;

        private void Awake()
        {
            // 1. MonoBehaviour services (scene instances)
            Services.Register<ObjectPoolManager>(objectPoolManager);
            Services.Register<UpdateManager>(updateManager);

            // 2. Event registries (plain C#)
            _combatEvents = new CombatEvents();
            _waveEvents = new WaveEvents();
            _economyEvents = new EconomyEvents();
            _gameEvents = new GameEvents();

            Services.Register<CombatEvents>(_combatEvents);
            Services.Register<WaveEvents>(_waveEvents);
            Services.Register<EconomyEvents>(_economyEvents);
            Services.Register<GameEvents>(_gameEvents);

            // 3. Plain C# services
            _playerStats = new PlayerStats();
            _playerStats.Initialize(startingGold, startingLives);
            Services.Register<PlayerStats>(_playerStats);

            // 4. MonoBehaviours that subscribe do so in their own OnEnable()
            // WaveManager, AudioController, EnemySpawner — no wiring needed here
        }

        private void OnDestroy()
        {
            _playerStats?.Cleanup();

            _combatEvents?.Clear();
            _waveEvents?.Clear();
            _economyEvents?.Clear();
            _gameEvents?.Clear();

            Services.Clear();
        }
    }
}
```

**Scene setup:**
1. Create a GameObject named `GameBootstrapper` at the root of the scene hierarchy
2. Add the `GameBootstrapper` MonoBehaviour component
3. Drag all manager references into it:
   - `objectPoolManager` → `Managers/ObjectPoolManager` GameObject
   - `updateManager` → `Managers/UpdateManager` GameObject
   - `waveManager` → `Managers/WaveManager` GameObject
   - `audioController` → `Managers/AudioController` GameObject
   - `enemySpawner` → `Game/EnemySpawner` GameObject
4. Set `startingGold` = `100`, `startingLives` = `20`

### Data Flow Diagram

```
╔══════════════════════════════════════════════════════════════════╗
║                    GameBootstrapper (Awake)                     ║
║  Registers all services and event registries into Services     ║
╚═════════════════════════╦══════════════════════════════════════╝
                          │
           ┌──────────────┼──────────────┐
           ▼              ▼              ▼
    ┌──────────┐   ┌──────────┐   ┌──────────┐
    │Services  │   │Services  │   │Services  │
    │.Get<O..> │   │.Get<C..> │   │.Get<P..> │
    │ObjectPool│   │CombatEvt │   │PlayerSts │
    │Manager   │   │          │   │          │
    └────┬─────┘   └────┬─────┘   └────┬─────┘
         │              │              │
         │         ┌────┴────┐         │
         ▼         ▼         ▼         ▼
  ┌──────────┐ ┌──────┐ ┌──────┐ ┌──────────┐
  │ Enemy    │ │Enemy │ │Enemy │ │PlayerSts │
  │ Spawner  │ │Die() │ │RchEnd│ │ subscribes│
  │ .Get()   │ │.Raise│ │.Raise│ │ to events │
  └──────────┘ └──────┘ └──────┘ └──────────┘

Access pattern:   Services.Get<T>()        (NOT singletons)
Communication:    Event registries          (NOT SO event channels)
Entry point:     GameBootstrapper.Awake()  (NOT per-script Init)
```

**How data flows:**
- **Service access** — any script calls `Services.Get<T>()` to reach a registered service. No `Instance` singletons, no `FindObjectOfType`, no serialized cross-references between systems
- **Event communication** — systems communicate through event registries. `Services.Get<CombatEvents>().EnemyDeath.Raise(gold)` sends, `Services.Get<CombatEvents>().EnemyDeath.Subscribe(handler)` receives
- **Composition root** — GameBootstrapper is the only place that knows about concrete types and their wiring. All other scripts code to interfaces and Services

### Temporary Test Script: `Assets/Scripts/Tests/ArchitectureTest.cs`

Create this, use it, then delete it. It validates the entire enemy pipeline without the pool manager (which isn't implemented yet) and verifies the service locator works.

```csharp
using Core;
using Data;
using Enemies.Controllers;
using Events.Registries;
using Systems.Game;
using Systems.Parsing;
using UnityEngine;

namespace Tests
{
    public class ArchitectureTest : MonoBehaviour
    {
        [SerializeField] private EnemyData testData;
        [SerializeField] private EnemyPath testPath;
        [SerializeField] private GameObject enemyPrefab;

        private EnemyController _enemy;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SpawnEnemy();

            if (Input.GetKeyDown(KeyCode.Alpha2))
                DamageEnemy(25f);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                KillEnemy();

            if (Input.GetKeyDown(KeyCode.Alpha4))
                TestServices();

            if (Input.GetKeyDown(KeyCode.Alpha5))
                TestEvents();
        }

        private void SpawnEnemy()
        {
            var go = Instantiate(enemyPrefab);
            _enemy = go.GetComponent<EnemyController>();
            _enemy.Initialize(testData, testPath);
            Debug.Log($"[Test] Enemy spawned. Health strategy: {_enemy.Health.GetType().Name}");
        }

        private void DamageEnemy(float amount)
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            _enemy.TakeDamage(amount);
            Debug.Log($"[Test] Took {amount} damage. IsAlive: {_enemy.IsAlive}");
        }

        private void KillEnemy()
        {
            if (_enemy == null) return;
            while (_enemy.IsAlive)
                _enemy.TakeDamage(25f);
            Debug.Log($"[Test] Enemy killed. IsAlive: {_enemy.IsAlive}");
        }

        private void TestServices()
        {
            var pool = Services.Get<ObjectPoolManager>();
            Debug.Log($"[Test] Services.Get<ObjectPoolManager>() = {(pool != null ? pool.gameObject.name : "null")}");

            var combat = Services.Get<CombatEvents>();
            Debug.Log($"[Test] Services.Get<CombatEvents>() = {(combat != null ? "found" : "null")}");

            var player = Services.Get<PlayerStats>();
            Debug.Log($"[Test] Services.Get<PlayerStats>() = {(player != null ? "found" : "null")}");
        }

        private void TestEvents()
        {
            var combat = Services.Get<CombatEvents>();
            combat.EnemyDeath.Subscribe(gold => Debug.Log($"[Test] EnemyDeath event fired! Gold reward: {gold}"));
            Debug.Log("[Test] Subscribed to CombatEvents.EnemyDeath. Kill an enemy (3) to see the event fire.");
        }
    }
}
```

**Notes on what this validates:**
- `Initialize()` chains through `StrategyFactory.CreateHealth()` + `StrategyFactory.CreateMovement()` → `Health.Initialize()` + `Movement.Initialize(this)`
- `TakeDamage()` delegates to `Health.TakeDamage(damage)` — returns `DamageResult`, checked for `Died` — the strategy pattern works
- `IsAlive` reads `Health.IsAlive` after damage — the property getter isn't broken
- `Movement.Tick(this)` in `Update()` drives the enemy along waypoints
- **Press 4:** verifies `Services.Get<T>()` returns the correct scene instances and registries
- **Press 5:** subscribes to a registry event — kill an enemy to verify the event fires

**Gotcha:** Enemies currently use `Destroy()` in `Die()` — that's fine for this test. Object pooling comes in Episode 04.

## Unity Editor Setup

### Scene Hierarchy — What Must Exist

```
Scene Root
├── GameBootstrapper              (GameBootstrapper component — WIRES EVERYTHING)
├── Managers
│   ├── ObjectPoolManager        (ObjectPoolManager component — empty for now, Episode 04)
│   ├── UpdateManager            (UpdateManager component — empty for now)
│   ├── WaveManager               (WaveManager component — empty for now)
│   └── AudioController           (AudioController component — empty for now)
├── Game
│   ├── EnemySpawner              (EnemySpawner component — empty for now)
│   └── EnemyPath                 (EnemyPath component + child waypoint GameObjects)
├── Enemy (Prefab or scene instance for testing)
│   └── EnemyController component
│   └── EnemyHealthBar child
└── Camera
```

**Key difference from previous version:** `GameBootstrapper` is at the root. No `PlayerStats` GameObject — PlayerStats is a plain C# class created by the bootstrapper, not a MonoBehaviour in the scene.

### GameBootstrapper Setup

1. Create empty GameObject named `GameBootstrapper` at the scene root
2. Add `GameBootstrapper` component
3. Drag references from the Hierarchy:
   - `objectPoolManager` → drag `Managers/ObjectPoolManager`
   - `updateManager` → drag `Managers/UpdateManager`
   - `waveManager` → drag `Managers/WaveManager`
   - `audioController` → drag `Managers/AudioController`
   - `enemySpawner` → drag `Game/EnemySpawner`
4. Set `startingGold` = `100`, `startingLives` = `20`

**Why GameBootstrapper exists:** Without it, every manager would need to know about every other manager — serialized references create a tangled web. The bootstrapper is the ONE place that knows about concrete types. All other scripts access services through `Services.Get<T>()`, which decouples them from the scene hierarchy entirely.

### EnemyPath Setup (This is the most common mistake area)

1. Create empty GameObject named `EnemyPath` under `Game/`
2. Add `EnemyPath` component
3. Create empty child GameObjects for waypoints: `WP0`, `WP1`, `WP2`, etc.
4. Position waypoints in the scene along the desired path
5. **CRITICAL:** Select the `EnemyPath` GameObject, drag the waypoint transforms into the `waypoints` array in the Inspector
6. Order matters — `waypoints[0]` is `StartPosition`, `waypoints[^1]` is `EndPosition`

**Waypoint values for testing (top-down view, Y=0 for grounded):**
| Waypoint | Position |
|----------|----------|
| WP0 | (0, 0, 0) |
| WP1 | (5, 0, 0) |
| WP2 | (5, 0, 5) |
| WP3 | (0, 0, 5) |

### EnemyData SO Creation (Minimal Test Data)

1. Right-click in `Assets/Data/Enemies/` → Create → Scriptable Objects → Data → Enemy
2. Name it `TestEnemy`
3. Settings:
   - Health: Leave null for now (create NormalHealth SO next)
   - Movement: Leave null for now (create GroundedPath SO next)
   - Gold Given: `10`
   - Damage: `1`

### NormalHealth SO Creation

1. Right-click in `Assets/Data/Enemies/` → Create → Strategies → Health → Normal
2. Name it `NormalHealth`
3 - Start Health: `100`

### GroundedPath SO Creation

1. GroundedPath does NOT have `[CreateAssetMenu]` — it's not decorated in the codebase
2. **Workaround:** Select the `TestEnemy` EnemyData SO. Click the small circle icon next to the Movement field. In the picker, search for "GroundedPath". If none exist yet, you need to create one programmatically or add `[CreateAssetMenu]` temporarily
3. **Better workaround for now:** Add `[CreateAssetMenu(menuName = "Strategies/Movement/Grounded")]` to `GroundedPath.cs` temporarily, create the asset, then you can remove the attribute
4. Move Speed: `3`

### Enemy Prefab/GameObject

1. Create a Cube or Capsule primitive named `Enemy`
2. Add `EnemyController` component
3. Assign the `healthBar` field if you have an EnemyHealthBar child (can be null for testing)
4. **CRITICAL:** The EnemyController needs a Collider for tower detection later. Add a CapsuleCollider now
5. **CRITICAL:** Set the enemy's Layer to `Enemy` (create the layer if it doesn't exist: Edit → Project Settings → Tags and Layers)

### ArchitectureTest Setup

1. Create empty GameObject named `ArchitectureTest`
2. Add `ArchitectureTest` component
3. Assign:
   - testData: `TestEnemy` (the EnemyData SO)
   - testPath: drag the `EnemyPath` GameObject
   - enemyPrefab: drag the `Enemy` GameObject (or a prefab if you made one)

## Test Plan

### Test 1: Enemy Spawns and Moves
1. Play
2. Press `1` — enemy should appear at WP0 (0,0,0)
3. Watch the enemy move toward WP1, then WP2, then WP3
4. Console output: `[Test] Enemy spawned. Health strategy: NormalHealth`

### Test 2: TakeDamage Reduces Health
1. Play
2. Press `1` to spawn
3. Press `2` — console shows `[Test] Took 25 damage. IsAlive: True`
4. Press `2` three more times (total 100 damage) — console shows `IsAlive: False`
5. The `OnDeath` event fires (check by adding a debug listener if needed)

### Test 3: Overkill Deaths
1. Press `3` — while loop deals 25f chunks until `IsAlive` is false
2. Verify `Die()` is called (currently a no-op with TODO comments, so just verify no exceptions)

### Test 4: FlyingPath Works
1. Switch the TestEnemy's Movement field to a FlyingPath SO (create one with `[CreateAssetMenu(menuName = "Strategies/Movement/Flying")]` temporarily)
2. Set `flyingHeight` to `2`
3. Play, press `1`, verify enemy spawns at `(0, 2, 0)` and moves at Y=2

### Test 5: ArmouredHealth Damage Reduction
1. Switch the TestEnemy's Health field to an ArmouredHealth SO (create one via Create → Strategies → Health → Armoured, startHealth=200, armourPercent=0.3)
2. Play, press `1`, press `2` (25f damage)
3. Expected: `CurrentHealth = 200 - (25 * 0.7) = 200 - 17.5 = 182.5`
4. Verify the enemy doesn't die after 8 hits of 25 (total applied = 8 * 17.5 = 140, still at 60)

### Test 6: GameBootstrapper Service Registration
1. Play
2. Press `4` — TestServices() runs
3. Verify console:
   - `Services.Get<ObjectPoolManager>() = ObjectPoolManager` (scene instance found)
   - `Services.Get<CombatEvents>() = found` (registry found)
   - `Services.Get<PlayerStats>() = found` (plain C# service found)
4. If any say "null", the bootstrapper didn't register that service — check Awake() order

### Test 7: Event Registry Communication
1. Play
2. Press `5` — subscribes to CombatEvents.EnemyDeath
3. Press `1` to spawn an enemy
4. Press `3` to kill it
5. Verify console: `[Test] EnemyDeath event fired! Gold reward: 10`
6. If the event doesn't fire, CombatEvents wasn't registered — check GameBootstrapper.Awake()

### Test 8: Clean Scene Transition (No Leaked Subscriptions)
1. Play
2. Press `5` — subscribe to event
3. Press `1`, press `3` — verify event fires
4. Stop playing
5. Verify: no `NullReferenceException` or "MissingReferenceException" in console
6. This proves `GameBootstrapper.OnDestroy()` calls `Clear()` on all event registries and `Services.Clear()`
7. Without that cleanup, event subscriptions would leak into the next play session

## Profiler Baseline

1. Open Window → Analysis → Profiler
2. Enable **Play Mode** in the profiler (not Edit mode)
3. Play the scene with NO enemies spawned
4. Record 60 frames
5. Note these values:
   - **GC.Alloc per frame** — should be near 0B (no enemies, no spawning)
   - **Main Thread** frame time — should be under 1ms
6. Write these baseline numbers down. Every future episode compares against them.
7. Then spawn 10 enemies and note the delta — this is your "active enemy" baseline

Typical baselines for an empty scene with just managers (all TODO stubs):
- GC.Alloc: 0–48B (just Unity overhead)
- Frame time: 0.3–0.8ms

## Pre-Warm Checklist for Later Episodes

These managers MUST exist in the scene AND be wired into GameBootstrapper before their code is implemented:

| Manager | Episode Ready | Scene GameObject | Registered As | Key References |
|---------|--------------|-----------------|---------------|----------------|
| ObjectPoolManager | Episode 04 | Managers/ObjectPoolManager | Self-registers in Awake | PoolConfig[] array |
| UpdateManager | Episode 05+ | Managers/UpdateManager | Self-registers in Awake | Tick intervals |
| WaveManager | Episode 06+ | Managers/WaveManager | Via GameBootstrapper serialized ref | waveCsvFile, EnemySpawner ref |
| EnemySpawner | Episode 06+ | Game/EnemySpawner | Via GameBootstrapper serialized ref | EnemyPath ref |
| AudioController | Episode 08+ | Managers/AudioController | Via GameBootstrapper serialized ref | AudioEventLinker[] |
| PlayerStats | Episode 07+ | (none — plain C#) | GameBootstrapper creates via `new` | startingGold, startingLives |
| CombatEvents | Episode 03+ | (none — plain C#) | GameBootstrapper creates via `new` | EnemyDeath, EnemyReachedEnd |
| WaveEvents | Episode 06+ | (none — plain C#) | GameBootstrapper creates via `new` | WaveStarted, WaveCompleted |
| EconomyEvents | Episode 07+ | (none — plain C#) | GameBootstrapper creates via `new` | GoldChanged, LivesChanged |
| GameEvents | Episode 08+ | (none — plain C#) | GameBootstrapper creates via `new` | TowerPlaced, GamePaused |

**GameBootstrapper wiring checklist (do this NOW):**
- [ ] ObjectPoolManager reference dragged to bootstrapper
- [ ] UpdateManager reference dragged to bootstrapper
- [ ] WaveManager reference dragged to bootstrapper
- [ ] AudioController reference dragged to bootstrapper
- [ ] EnemySpawner reference dragged to bootstrapper
- [ ] startingGold and startingLives configured

**Layer setup (do this NOW):**
- Layer 6 (or next free): `Enemy` — assign to all enemy GameObjects
- Layer 7 (or next free): `Projectile` — assign to projectile prefabs later
- Layer 8 (or next free): `Tower` — assign to tower GameObjects later
- Physics matrix: Enemy ↔ Projectile = enabled. Everything else you can disable cross-collision.

## Common Setup Mistakes

| Mistake | Symptom | Fix |
|---------|---------|-----|
| Forgot to assign EnemyPath to spawner/test | NullReferenceException in `Movement.Initialize()` → `SetPath()` | Drag EnemyPath GameObject to the serialized field |
| Forgot to assign EnemyData SO | NullReferenceException in `InitData()` → `data.Health` | Create and assign an EnemyData SO |
| Waypoints array is empty | Enemy doesn't move, or `IndexOutOfRangeException` | Drag waypoint transforms into the array |
| Waypoints out of order | Enemy moves erratically, backtracks | Check the array order in Inspector |
| Collider layer mismatch (later) | TowerDetection.OverlapSphere finds nothing | Set enemy Layer to `Enemy`, set `enemyLayer` mask on TowerDetection |
| HealthStrategy SO not assigned in EnemyData | NullReferenceException in `InitStrategy()` | Assign a NormalHealth or ArmouredHealth SO to the Health field |
| MovementStrategy SO not assigned | NullReferenceException in `Movement.Tick()` | Assign GroundedPath or FlyingPath SO |
| GroundedPath/FlyingPath no CreateAssetMenu | Can't create movement SOs from right-click menu | Add attribute temporarily, or use the object picker circle on EnemyData fields |
| Enemy prefab missing EnemyController | `GetComponent<EnemyController>()` returns null | Make sure the prefab has the component |
| Multiple EnemyPath components on same GameObject | Confusing which path is active | One EnemyPath per GameObject |
| GameBootstrapper not in scene | `KeyNotFoundException` from `Services.Get<T>()` | Add GameBootstrapper to the scene |
| Manager not wired in GameBootstrapper | `KeyNotFoundException` for that specific service | Drag the manager reference into the bootstrapper's serialized field |
| Event registries not cleared on stop | Leaked subscriptions, NullReferenceExceptions on next play | GameBootstrapper.OnDestroy() must call Clear() on all registries then Services.Clear() |
| Services.Get<T>() called before Awake | `KeyNotFoundException` — service not registered yet | Registration order matters. GameBootstrapper.Awake() runs first if it's the first script |

## Debugging Tips

**Enemy doesn't move:**
- Check `MoveSpeed` on the MovementStrategy SO — is it 0?
- Check `EnemyPath.waypoints` — is the array populated?
- Add `Debug.Log($"Tick: {enemy.transform.position}");` inside `GroundedPath.Tick()` temporarily

**NullReferenceException in Initialize:**
- The chain is: `Initialize(data, path)` → `InitData(data)` → `InitStrategy()` → `Health.Initialize(this)`
- Check which reference is null by adding a null check before each call
- Most common: `data.Health` or `data.Movement` is null because the SO fields weren't assigned

**TakeDamage doesn't reduce health:**
- For ArmouredHealth: `armourPercent = 0.3` means only 70% of damage applies. `TakeDamage(10)` → `CurrentHealth -= 7`
- For NormalHealth: straightforward subtraction, should always work
- Check that you're looking at `CurrentHealth` not `startHealth` (the latter is the SO field, it doesn't change)

**IsAlive returns true after death:**
- `IsAlive` reads `Health.CurrentHealth > 0f` — if health is exactly 0.00001 it's still "alive"
- For ArmouredHealth with non-integer damage, floating point can leave tiny remainders
- Fix: use `Health.CurrentHealth <= 0f` threshold or `Mathf.Epsilon` comparison (but the existing code uses `> 0f` which is fine for typical values)

**KeyNotFoundException from Services.Get<T>():**
- The service wasn't registered in GameBootstrapper.Awake()
- Check the registration order — some services depend on others being registered first
- Add a `Debug.Log` in GameBootstrapper.Awake() after each registration to confirm order

**Events not firing:**
- Forgot to subscribe — check that the subscriber's OnEnable() calls `Services.Get<EventType>().Channel.Subscribe()`
- Forgot to register the event registry in GameBootstrapper — `Services.Get<CombatEvents>()` will throw
- Scene transition cleared the registry but subscriber didn't re-subscribe — all registries are cleared in OnDestroy(); on scene reload, OnEnable() must re-subscribe