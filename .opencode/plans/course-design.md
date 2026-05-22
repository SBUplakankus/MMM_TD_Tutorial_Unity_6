# Tower Defense Tutorial Series — Course Design

## Overview

A 14-episode YouTube series teaching Unity intermediates how to demonstrate clean architecture patterns using a tower defense as the test rig. The viewer builds naive systems first, then refactors each piece with proper patterns as the pain of not having them becomes real. The output is working systems that prove concepts, not a shippable product.

**Target audience**: Unity intermediates — comfortable with MonoBehaviour, SerializeField, basic C#. Not comfortable with design patterns or architecture.

**Starting point**: Viewer has a Unity project with 3D assets and a map layout. No code. All scripts are written from scratch.

**End state**: Working systems that demonstrate 6 architecture patterns, composed in a tower defense rig with pre-placed towers, CSV-driven waves, 6+ enemy types composed from strategies, object pooling, service locator, and event-driven economy. Gold + Lives UI, health bars with Slider, shield bars, wave counter. The tower defense is the demonstration vehicle, not the product.

---

## Teaching Principles

1. **Working demo first** — Every episode ends with a demonstrable system the viewer can observe working. No "trust me, you'll need this later."
2. **Patterns from pain** — Each pattern is introduced at the exact moment the viewer feels the problem it solves. Never before.
3. **One concept per episode** — Tight focus. If an episode needs to teach two things, split it.
4. **No forward references** — Code at episode N only uses systems built in episodes 1-N. If a future system is needed, use the naive version until that episode arrives.
5. **UI inline** — Visual feedback is created in the same episode that needs it. No separate UI episode.
6. **Show the naive version first** — Every refactor episode shows why the current code is painful, then fixes it.

---

## Concepts Taught

### Architecture Patterns
| Pattern | Episode | Pain That Motivates It |
|---------|---------|----------------------|
| C# Interfaces | 2 | "Tower needs to interact with enemy without knowing EnemyController" |
| Strategy Pattern | 6-7 | "Adding flying enemy means copying EnemyController" |
| Factory Pattern | 6-7 | "Strategy creation is scattered, can't be data-driven" |
| Object Pooling | 8 | "GC.Alloc spikes from Instantiate/Destroy in Profiler" |
| Service Locator | 9 | "3+ singletons scattered, hidden dependencies, can't test" |
| Observer (Event Channels) | 10 | "Adding audio/UI to Die() means editing EnemyController every time" |

### Implementation Techniques
| Technique | Episode | Where Used |
|-----------|---------|------------|
| ScriptableObject configs | 6-7 | MovementConfig, HealthConfig, EnemyData |
| readonly record structs | 7, 11 | DamageResult, WaveData |
| Enums for type dispatch | 6-7 | MovementType, HealthType |
| Thin controller composition | 7 | EnemyController delegates to strategies |
| Pre-warming pools | 8 | ObjectPoolManager.Awake() |
| Composition root | 9 | GameBootstrapper |
| Static provider for stateless strategies | 13 | TargetingProvider |

---

## Episode Sequence

### Phase 1: Naive Combat Loop

The viewer builds a working combat loop with no patterns. Everything is hardcoded, inline, and direct. The goal: something that works end-to-end as fast as possible.

---

#### Episode 1: Enemy Movement

**What works after**: Enemy walks along waypoints on the map.

**Scripts created**: EnemyPath.cs, EnemyController.cs

**Episode structure**:
1. Set up waypoints on the map (Empty GameObjects in a path)
2. Build EnemyPath: stores Transform[] waypoints, exposes GetWaypointPosition, HasWaypoint, IsAtWaypoint
3. Build EnemyController: references EnemyPath, MoveTowards in Update, advances waypoint index
4. Test: enemy walks from start to end of path

**Key decisions**:
- EnemyPath is a MonoBehaviour on a path GameObject, not on the enemy
- EnemyController references EnemyPath (injected via Initialize or serialized field)
- No interfaces yet — just raw EnemyController
- Destroy(gameObject) when enemy reaches end (pooling comes later)

**No UI this episode.**

---

#### Episode 2: Interfaces

**What works after**: Enemy implements IDamageable + ITargetable. Click-to-damage test works. Health bar shows damage visually.

**Scripts created**: IDamageable.cs, ITargetable.cs, EnemyHealthBar.cs
**Scripts modified**: EnemyController.cs (implements interfaces)

**Episode structure**:
1. The problem: "What if towers need to interact with enemies but shouldn't know about EnemyController?" — introduce interfaces as contracts
2. Define IDamageable: TakeDamage(float), IsAlive
3. Define ITargetable: Position, IsAlive
4. EnemyController implements both interfaces
5. Build EnemyHealthBar using Unity Slider (world-space, parented under enemy)
6. Update EnemyController.Update to drive health bar fill from _currentHealth / _startHealth
7. Build ClickDamageTest script (raycast, TryGetComponent<IDamageable>, TakeDamage)
8. Test: click enemy 4 times, health bar decreases, enemy dies

**UI this episode**: Enemy health bar (Slider component, world-space)

**ITargetable initially has only**: Position, IsAlive. PathProgress is added in Episode 14 (Targeting) when it's actually needed.

---

#### Episode 3: Tower Detection

**What works after**: Tower detects enemies in range, selects nearest, draws debug gizmo to target.

**Scripts created**: TowerDetection.cs
**Scripts modified**: none

**Episode structure**:
1. Build TowerDetection: OverlapSphere, find nearest enemy via ITargetable
2. Cache current target, check IsAlive each frame (stale target invalidation)
3. OnDrawGizmos: draw line from tower to CurrentTarget Position + draw detection radius sphere
4. Test: enemy walks by tower, gizmo line tracks it, enemy leaves range, line disappears

**Key decisions**:
- Uses ITargetable (from Ep 2), not EnemyController — proves interface value
- OverlapSphere (Alloc version for simplicity)
- Targeting is hardcoded to "closest" — First/Last/Strong/Close comes in Ep 13

**No UI this episode.** Gizmo lines provide the visual test.

---

#### Episode 4: Tower Firing

**What works after**: Tower fires projectiles at detected enemy. Projectile hits, deals damage via IDamageable. Enemy dies from projectile damage. Complete combat loop!

**Scripts created**: ProjectileBase.cs, TowerFiring.cs
**Scripts modified**: TowerDetection.cs (expose CurrentTarget for TowerFiring)

**Episode structure**:
1. Build ProjectileBase: moves toward ITargetable, calls TakeDamage on hit, Destroy(gameObject)
2. Build TowerFiring: references TowerDetection, fires on cooldown when HasTarget, Instantiates projectile
3. Wire TowerFiring to TowerDetection in Inspector
4. Test: tower fires at enemy, projectile hits, enemy takes damage, health bar decreases, enemy eventually dies

**Key decisions**:
- Projectile uses Instantiate/Destroy — naive version. Pooling replaces in Ep 8.
- ProjectileBase.Launch takes ITargetable — uses IDamageable on hit
- TowerFiring holds fire rate, projectile prefab, fire point Transform
- This is the **first moment the full combat loop works** — major milestone

**No UI this episode.** Health bar from Ep 2 shows the damage.

---

#### Episode 5: Player Economy

**What works after**: Enemy death gives gold. Enemy reaching end costs lives. Gold and lives shown on screen.

**Scripts created**: PlayerStats.cs (MonoBehaviour singleton for now)
**Scripts modified**: EnemyController.cs (Die() calls PlayerStats, OnReachedEnd calls PlayerStats)

**Episode structure**:
1. Build PlayerStats: Gold, Lives, AddGold, SubtractLives — MonoBehaviour singleton with Instance
2. EnemyController.Die(): PlayerStats.Instance.AddGold(GoldGiven)
3. EnemyController.OnReachedEnd(): PlayerStats.Instance.SubtractLives(Damage)
4. Build Gold/Lives UI: screen-space Canvas, two Text elements bound to PlayerStats
5. Add UI update: PlayerStats fires simple C# event when Gold or Lives changes, UI subscribes
6. Test: kill enemy -> gold goes up, enemy reaches end -> lives go down

**Key decisions**:
- PlayerStats is a MonoBehaviour singleton — Service Locator replaces in Ep 9
- Simple C# event on PlayerStats for UI update — Event Channels replace in Ep 10
- This creates the full system loop: enemies spawn -> walk -> tower shoots -> enemy dies -> gold tracked -> more content

**UI this episode**: Screen-space Canvas with Gold counter and Lives counter.

---

### Phase 2: Strategy Pattern

The viewer has one enemy type. Now they want variety. The naive approach (copy EnemyController) is painful. This is the moment Strategy Pattern solves a real, felt problem.

---

#### Episode 6: Movement Strategies

**What works after**: Same observable behavior, but GroundedPath and FlyingPath are separate strategy classes. Adding new enemy movement types no longer requires editing EnemyController.

**Scripts created**: IMovementStrategy.cs, GroundedPath.cs, FlyingPath.cs, MovementConfig.cs (SO + MovementType enum)
**Scripts modified**: EnemyController.cs (delegates movement to IMovementStrategy), EnemyData.cs (add MovementConfig field)
**Scripts created**: StrategyFactory.cs (CreateMovement only)

**Episode structure**:
1. THE PAIN: "I want a flying enemy. I have to copy EnemyController." Show the copy approach and its problems (combinatorial explosion, bug-fix duplication).
2. Extract IMovementStrategy: Initialize(enemy), Tick(enemy), OnMovementCompleted event
3. Build GroundedPath: moves along EnemyPath at ground level
4. Build FlyingPath: moves along EnemyPath with Y offset
5. Build MovementConfig SO + MovementType enum for data-driven selection
6. Build StrategyFactory.CreateMovement(config) — switch expression on MovementType
7. Refactor EnemyController: add IMovementStrategy Movement property, call Movement.Tick(this) in Update
8. Update EnemyData SO to carry MovementConfig
9. Test: spawn ground enemy (walks), spawn flying enemy (hovers), same controller handles both

**Key decisions**:
- EnemyController exposes Path and CurrentWayPointIndex as public — strategies need them
- Movement.OnMovementCompleted += OnReachedEnd — event subscription for path completion
- Factory is static class with switch expression — simple, testable
- StrategyFactory only has CreateMovement — CreateHealth added in Ep 7

**No UI this episode.** Visual difference is the flying enemy floating.

---

#### Episode 7: Health Strategies

**What works after**: IHealthStrategy extracted, NormalHealth and ArmouredHealth work. 4 composed enemy types. EnemyController becomes a thin orchestrator. Health bar reads from IHealthStrategy.

**Scripts created**: IHealthStrategy.cs, DamageResult.cs (readonly record struct), HealthConfig.cs (SO + HealthType enum), NormalHealth.cs, ArmouredHealth.cs
**Scripts modified**: EnemyController.cs (delegates health to IHealthStrategy), EnemyData.cs (add HealthConfig field), EnemyHealthBar.cs (reads from IHealthStrategy)
**Scripts modified**: StrategyFactory.cs (add CreateHealth)

**Episode structure**:
1. THE PAIN: "I want an armoured enemy. I have to edit EnemyController.TakeDamage or copy the class."
2. Define IHealthStrategy: Initialize, TakeDamage->DamageResult, Tick, IsAlive, CurrentHealth, MaxHealth
3. Build DamageResult readonly record struct with Alive()/Dead() factory methods
4. Build NormalHealth: simple subtraction, same as what was inline
5. Build ArmouredHealth: applies armourPercent reduction before subtraction
6. Build HealthConfig SO + HealthType enum
7. Add StrategyFactory.CreateHealth(config) — NormalHealth and ArmouredHealth cases
8. Refactor EnemyController: inline health -> IHealthStrategy, TakeDamage uses DamageResult
9. Update EnemyData to carry HealthConfig
10. Update EnemyHealthBar: Slider.value = Health.CurrentHealth / Health.MaxHealth
11. Create 4 EnemyData SOs: Basic, Armoured, Flying, FlyingArmoured
12. Test: kill each type, verify armoured takes more hits, health bar shows strategy data

**UI this episode**: Health bar updated to use Slider.value = Health.CurrentHealth / Health.MaxHealth.

---

### Phase 3: Scale Systems

The viewer has working systems with 4 enemy types. Now they want waves of many enemies. Instantiate/Destroy creates GC spikes. Scattered singletons create hidden dependencies.

---

#### Episode 8: Object Pooling

**What works after**: No GC.Alloc spikes during runtime. Enemies and projectiles recycled. Pre-warming forces all allocations into startup.

**Scripts created**: ObjectPoolManager.cs (MonoBehaviour with Instance for now), PoolConfig class, IPoolable.cs
**Scripts modified**: EnemyController.cs (implements IPoolable, Die() -> pool Return), EnemySpawner.cs (use pool Get), ProjectileBase.cs (implements IPoolable, Return), TowerFiring.cs (use pool Get)

**Episode structure**:
1. Open Profiler with current build. Show GC.Alloc spikes from Instantiate/Destroy during a wave.
2. Build IPoolable: Reset() method called on pool return
3. Build ObjectPoolManager with Unity.Pool ObjectPool, PoolConfig array, pre-warming
4. ObjectPoolManager uses Instance singleton — Service Locator replaces in Ep 9
5. Refactor EnemyController: Die() -> ObjectPoolManager.Instance.Return("enemy", gameObject), add IPoolable.Reset()
6. Refactor ProjectileBase: OnHit -> Return(poolKey, gameObject), add IPoolable.Reset()
7. Refactor TowerFiring: Instantiate -> ObjectPoolManager.Instance.Get(poolKey, pos, rot)
8. Build EnemySpawner: uses pool.Get instead of Instantiate
9. Open Profiler again — compare. GC.Alloc gone during runtime.
10. Test: spawn 20 enemies, kill them all, spawn again — no new allocations

**Key decisions**:
- Instance singleton now — Services.Get in Ep 9
- Pool keys are strings — "enemy", "arrow", "bomb"
- IPoolable.Reset() clears all runtime state
- OnReachedEnd also returns to pool (not just Die)
- Pre-warming in Awake: Get all defaultSize objects, then Release them all

**No UI this episode.** Performance improvement is proven via Profiler comparison.

---

#### Episode 9: Service Locator

**What works after**: Single Services class replaces all Instance singletons. GameBootstrapper wires dependencies in one place.

**Scripts created**: Services.cs (static class), GameBootstrapper.cs (MonoBehaviour composition root)
**Scripts modified**: ObjectPoolManager.cs (remove Instance), PlayerStats.cs (remove Instance, become plain C# class), EnemyController.cs (.Instance -> Services.Get<T>()), ProjectileBase.cs, TowerFiring.cs, EnemySpawner.cs, ClickDamageTest.cs

**Episode structure**:
1. THE PAIN: "I have ObjectPoolManager.Instance, PlayerStats.Instance, and soon WaveManager.Instance. Every class hides its dependencies."
2. Build Services: static Dictionary<Type, object>, Register, Get, Clear
3. Build GameBootstrapper: registers all managers in Awake, calls Clear in OnDestroy
4. Refactor ObjectPoolManager: remove Instance property and DontDestroyOnLoad
5. Refactor PlayerStats: remove Instance, register in GameBootstrapper as plain C# class
6. Replace all .Instance calls with Services.Get<T>()
7. Test: same observable behavior, all dependencies declared in GameBootstrapper

**Key decisions**:
- GameBootstrapper is the ONLY place that resolves dependencies (composition root)
- Services.Clear() in GameBootstrapper.OnDestroy
- Registration order matters: ObjectPoolManager first
- PlayerStats becomes plain C# class — registered as new PlayerStats() in GameBootstrapper
- GameBootstrapper does NOT register WaveManager or EventChannels yet — those come in Ep 11 and 10

**No UI this episode.** Same systems, cleaner access pattern.

---

### Phase 4: Decoupling

Adding new reactions to enemy death means editing EnemyController.Die() every time.

---

#### Episode 10: Event Channels

**What works after**: EnemyController.Die() raises events, never calls PlayerStats directly. Any new system can react without touching EnemyController.

**Scripts created**: EventChannel.cs, EventChannelT.cs, CombatEvents.cs, EconomyEvents.cs
**Scripts modified**: GameBootstrapper.cs (register event registries), PlayerStats.cs (subscribe in Initialize, unsubscribe in Cleanup), EnemyController.cs (raise events in Die/OnReachedEnd), Gold/Lives UI (subscribe to events)

**Episode structure**:
1. THE PAIN: "I want to play a sound when an enemy dies. I have to edit EnemyController.Die(). Every new feature touches the same method."
2. Build EventChannel: Raise(), Register/Unregister Action callbacks
3. Build EventChannel<T>: Raise(T), Register/Unregister Action<T> callbacks
4. Build CombatEvents: EnemyDeath (EventChannel<int> for gold), EnemyReachedEnd (EventChannel<int> for damage)
5. Build EconomyEvents: GoldChanged (EventChannel<int>), LivesChanged (EventChannel<int>)
6. Register event registries in GameBootstrapper
7. Refactor EnemyController.Die(): raise CombatEvents.EnemyDeath.Raise(GoldGiven) instead of calling PlayerStats
8. Refactor PlayerStats: subscribe to CombatEvents in Initialize, unsubscribe in Cleanup
9. Refactor Gold/Lives UI: subscribe to EconomyEvents for display updates
10. Test: same observable behavior, but EnemyController never references PlayerStats

**Key decisions**:
- EventChannel and EventChannel<T> are plain C# — no Unity lifecycle
- Event registries (CombatEvents, EconomyEvents) are static classes with static EventChannel fields
- Registered in Services via GameBootstrapper
- PlayerStats.Cleanup() called in GameBootstrapper.OnDestroy
- EnemyController.OnEnable caches Services references (OnEnable runs on pool reactivation; Awake only runs once on creation)

**UI this episode**: Gold/Lives counter subscribes to EconomyEvents instead of polling PlayerStats.

---

### Phase 5: Proving the Architecture

Architecture scales cleanly. New systems added, zero core changes required.

---

#### Episode 11: Wave System

**What works after**: CSV-defined waves spawn enemies automatically. Wave counter shown on screen.

**Scripts created**: CsvWaveParser.cs, WaveManager.cs, EnemySpawner.cs
**Scripts modified**: GameBootstrapper.cs (register WaveManager)

**Episode structure**:
1. Define CSV format: wave,enemyType,count,spawnDelay,interval
2. Build CsvWaveParser: parse TextAsset CSV into WaveData readonly record structs
3. Build EnemySpawner: coroutine that spawns batch with spawn delay + interval
4. Build WaveManager: state machine (Idle->Spawning->Waiting), reads CSV, drives EnemySpawner
5. Register WaveManager in GameBootstrapper
6. Raise WaveEvents.WaveStarted, WaveEvents.WaveComplete for UI
7. Test: waves auto-advance, different enemy types spawn, economy persists

**UI this episode**: Wave counter (e.g., "Wave 3/10") subscribes to WaveEvents.

---

#### Episode 12: Update Manager

**What works after**: One native-to-managed transition per frame instead of N. EnemyController and ProjectileBase use managed updates.

**Scripts created**: IUpdatable.cs, UpdateManager.cs
**Scripts modified**: EnemyController.cs (Update -> ManagedUpdate, IUpdatable, register/unregister), ProjectileBase.cs (Update -> ManagedUpdate, IUpdatable, register/unregister), IHealthStrategy.cs (Tick() -> Tick(float deltaTime)), NormalHealth.cs, ArmouredHealth.cs (accept deltaTime, still no-op), GameBootstrapper.cs (register UpdateManager)

**Episode structure**:
1. THE PAIN: Open Profiler during wave with 50+ enemies — count native Update() dispatches
2. Build IUpdatable: ManagedUpdate(float deltaTime)
3. Build UpdateManager: List<IUpdatable> with pending add/remove buffers, _isIterating flag
4. Refactor EnemyController: Update() -> ManagedUpdate(), register in Initialize, unregister in Reset
5. Refactor ProjectileBase: Update() -> ManagedUpdate(), register in Launch, unregister in ReturnToPool/Reset
6. Update IHealthStrategy.Tick signature: Tick() -> Tick(float deltaTime) — NormalHealth/ArmouredHealth accept but ignore
7. Register UpdateManager in GameBootstrapper (order: 2, after ObjectPoolManager)
8. Test: same observable behavior, Profiler shows fewer native dispatches

**Key decisions**:
- Pending add/remove lists prevent concurrent modification during iteration
- Time.deltaTime cached once and passed as parameter — testable and avoids repeated property fetch
- _isIterating flag is zero-alloc alternative to copying list each frame
- IHealthStrategy.Tick gains deltaTime parameter here because UpdateManager makes it available
- RegenHealth (Episode 13) is the first consumer of deltaTime

**No UI this episode.** Performance improvement proven via Profiler comparison.

---

#### Episode 13: Advanced Health

**What works after**: ShieldHealth and RegenHealth work. Zero changes to EnemyController. Strategy pattern payoff demonstrated.

**Scripts created**: ShieldHealth.cs, RegenHealth.cs
**Scripts modified**: StrategyFactory.cs (add Shield + Regen cases), HealthConfig.cs (add ShieldAmount, RegenRate fields)

**Episode structure**:
1. Build ShieldHealth: shield absorbs damage first, then health
2. Build RegenHealth: Tick(deltaTime) restores health over time, capped at MaxHealth
3. Add ShieldHealth and RegenHealth cases to StrategyFactory
4. Update HealthConfig SO with ShieldAmount and RegenRate fields
5. Create new EnemyData SOs for shield and regen types
6. ZERO CHANGES to EnemyController — payoff episode
7. Test: shield absorbs hits, regen heals over time, compose with all movement types

**UI this episode**: Shield bar (second Slider fill, blue, behind green health fill).

---

### Phase 6: Refinement

---

#### Episode 14: Targeting Strategies

**What works after**: Towers support First/Last/Strong/Close targeting, swappable at runtime.

**Scripts created**: ITargetingStrategy.cs, FirstTargeting.cs, LastTargeting.cs, StrongTargeting.cs, CloseTargeting.cs, TargetingProvider.cs
**Scripts modified**: ITargetable.cs (add PathProgress, CurrentHealth), EnemyController.cs (add PathProgress, CurrentHealth), TowerDetection.cs (accept ITargetingStrategy)

**Episode structure**:
1. THE PAIN: "All towers target closest. I want First or Strong. Editing TowerDetection for each new sort is messy."
2. Add PathProgress and CurrentHealth to ITargetable (needed for First/Last/Strong)
3. Build ITargetingStrategy: GetTarget(IEnumerable<ITargetable>, Vector3 towerPosition)
4. Build 4 targeting strategies: First, Last, Strong, Close
5. Build TargetingProvider: static instances for stateless strategies
6. Refactor TowerDetection: accept ITargetingStrategy, default to Close
7. Add runtime switching: TowerDetection.SetTargeting(TargetPriority enum)
8. Test: cycle targeting, different enemies get prioritized

**No UI this episode.**

---

## EnemyController Evolution

| Episode | Die() / OnReachedEnd() | Health | Movement | Pooling |
|---------|----------------------|--------|----------|---------|
| 1 | Destroy(gameObject) | inline float | inline MoveTowards | N/A |
| 2 | Destroy(gameObject) | inline float | inline MoveTowards | N/A |
| 4 | Destroy(gameObject) | inline float | inline MoveTowards | N/A |
| 5 | PlayerStats.Instance.AddGold() | inline float | inline MoveTowards | N/A |
| 6 | PlayerStats.Instance.AddGold() | inline float | IMovementStrategy | N/A |
| 7 | PlayerStats.Instance.AddGold() | IHealthStrategy | IMovementStrategy | N/A |
| 8 | ObjectPoolManager.Instance.Return() | IHealthStrategy | IMovementStrategy | IPoolable |
| 9 | Services.Get<ObjectPoolManager>().Return() | IHealthStrategy | IMovementStrategy | IPoolable |
| 10 | CombatEvents.EnemyDeath.Raise() | IHealthStrategy | IMovementStrategy | IPoolable + OnEnable cache |
| 12 | unchanged | IHealthStrategy | IMovementStrategy | IPoolable + IUpdatable |
| 14 | unchanged | + CurrentHealth on ITargetable | + PathProgress on ITargetable | unchanged |

---

## UI Summary

| Episode | UI Element | Implementation |
|---------|------------|----------------|
| 2 | Enemy health bar | Unity Slider (world-space) |
| 5 | Gold + Lives counters | Screen-space Canvas, Text |
| 7 | Health bar reads strategy | Slider.value = Health.CurrentHealth / Health.MaxHealth |
| 10 | Gold/Lives via events | Subscribes to EconomyEvents |
| 11 | Wave counter | Text, subscribes to WaveEvents |
| 12 | — | Update Manager: no visual UI, proven via Profiler |
| 13 | Shield bar | Second Slider fill (blue) behind health (green) |

---

## Out of Scope

- Tower shop / purchase UI
- Tower placement on nodes
- Game-over screen
- Tower upgrade system
- Audio middleware
- Main menu
- Save/load
- Multiple maps