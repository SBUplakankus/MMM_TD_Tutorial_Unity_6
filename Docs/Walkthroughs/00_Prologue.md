# Prologue: Why This Course Exists

## Who This Is For

You know Unity. You can make a MonoBehaviour, wire up a SerializeField, and get something moving on screen. But when your project grows past one script, things fall apart. Copy-pasted code. Singletons everywhere. Methods that know about every other class. Adding one feature breaks three others.

This course fixes that. Not with theory — with working systems you build and observe every single episode.

## The Core Idea

**Patterns are not prerequisites. They are responses to pain.**

Most tutorials teach the Strategy Pattern first, then try to find a use for it. We do the opposite. You build something that works naively. You feel the problem. Then you learn the pattern that solves it. Every concept in this course is introduced at the exact moment you need it — never before.

This is not "learn design patterns." This is "learn when design patterns are worth using."

The tower defense is the **demonstration vehicle**, not the product. You are not building a game. You are building working systems that prove architecture concepts. The tower defense provides a context where those concepts are visible, tangible, and refactorable. The output is systems, not a shippable product.

## What You'll Build

Working demonstrations of 6 architecture patterns, using a tower defense as the test rig:

- Interfaces as contracts — towers and projectiles interact with enemies without knowing their concrete class
- Strategy Pattern — interchangeable behaviors composed from data, not code changes
- Factory Pattern — data-driven object creation from ScriptableObject configs
- Object Pooling — zero-allocation runtime via pre-warmed recycling
- Service Locator — centralized dependency resolution replacing scattered singletons
- Observer Pattern (Event Channels) — decoupled producer-consumer communication

By the final episode, you can add a brand-new enemy type by writing one class and one factory case. The code you already wrote never changes. That's the proof that the architecture works.

## The Six Rules

### 1. Working Demo First

Every episode ends with a system you can observe working. No "trust me, this matters later." The demo validates the concept. If an episode doesn't produce a demonstrable result, it's the wrong episode.

### 2. Patterns From Pain

Each pattern is introduced at the exact moment the viewer feels the problem it solves. Strategy Pattern appears when you need a flying enemy and realize copy-pasting EnemyController creates a maintenance nightmare. Object Pooling appears when you open the Profiler and see GC.Alloc spikes. Service Locator appears when your third singleton makes dependencies invisible.

### 3. One Concept Per Episode

Tight focus. If an episode teaches two things, it teaches neither well. The Strategy Pattern gets two episodes — one for movement, one for health — because they are separate extractions with separate payoffs.

### 4. No Forward References

Code at Episode N only uses systems built in Episodes 1 through N. If a future system is needed, the naive version stays until that episode arrives. This means `EnemyController` evolves across 8 episodes — and each evolution is small, understandable, and reversible.

### 5. UI Inline

Visual feedback is created in the same episode that needs it. Health bars appear when health strategies need visualization. Gold and lives counters appear when economy needs display. No separate UI episode — UI demonstrates the system, not the other way around.

### 6. Naive First, Then Refactor

Build the working system naively. See why it's painful. Then fix it. This is how real development works. You don't architect the perfect system on day one. You build something that works, identify the friction, and refactor. Every refactor episode shows the pain first.

## Episode Map

### Phase 1: Foundational Systems (Episodes 1-5)

Build the base systems with no patterns. No architecture. Just Make It Work. These systems create the seams that later episodes refactor.

| Episode | Builds | Concept Demonstrated |
|---------|--------|---------------------|
| 1 | Enemy walks along waypoints | Data separation: EnemyPath holds waypoint data, EnemyController holds movement logic |
| 2 | Interfaces + health bar + click-to-damage | IDamageable/ITargetable contracts proven at runtime via click test |
| 3 | Tower detects enemies in range | ITargetable used by a separate system — interface decoupling demonstrated |
| 4 | Tower fires projectiles | Full interface chain: ITargetable → ProjectileBase → IDamageable — contracts compose correctly |
| 5 | Gold, lives, economy UI | Singleton as the simplest access pattern — naive version that Episode 9 replaces |

After Episode 5, the base systems work. They're naive. That's the point — naive code is the testbed for refactoring.

### Phase 2: Strategy Pattern (Episodes 6-7)

The viewer wants enemy variety. The naive approach means copying code. The Strategy Pattern solves this at the exact moment of pain.

| Episode | Extracts | Concept Demonstrated |
|---------|----------|---------------------|
| 6 | IMovementStrategy from inline movement | Strategy Pattern: delegation replaces duplication, factory enables data-driven composition |
| 7 | IHealthStrategy from inline health | Thin orchestrator: EnemyController delegates to strategies, contains zero type-specific logic |

### Phase 3: Scale Systems (Episodes 8-9)

Waves of enemies expose two problems: GC spikes from Instantiate/Destroy, and scattered singletons hiding dependencies.

| Episode | Solves | Concept Demonstrated |
|---------|--------|---------------------|
| 8 | Object Pooling | Pre-warmed recycling: measure with Profiler, prove with numbers |
| 9 | Service Locator | Composition root: one place wires all dependencies, replaces hidden singleton access |

### Phase 4: Decoupling (Episode 10)

Adding reactions to enemy death means editing EnemyController every time.

| Episode | Solves | Concept Demonstrated |
|---------|--------|---------------------|
| 10 | Event Channels | Observer Pattern: producers raise events, consumers subscribe, zero coupling between them |

### Phase 5: Proving the Architecture (Episodes 11-13)

New systems added. Zero core changes required.

| Episode | Adds | Changed Lines in EnemyController |
|---------|------|-------------------------------|
| 11 | CSV waves, WaveManager spawner | 0 |
| 12 | IUpdatable, UpdateManager | ~8 changed (Update → ManagedUpdate, register/unregister) |
| 13 | ShieldHealth, RegenHealth | 0 |

Episode 13 is the proof. Two new systems added. EnemyController unchanged (from Episode 12). The architecture delivers on its promise.

### Phase 6: Refinement (Episode 14)

| Episode | Adds | Concept Demonstrated |
|---------|------|---------------------|
| 14 | First/Last/Strong/Close targeting | Stateless strategies: zero-alloc singleton instances shared across all towers |

## What This Course Is Not

- Not a Unity beginner tutorial. You should know what a MonoBehaviour is.
- Not a C# beginner tutorial. You should know what an interface is at the language level.
- Not a game. The tower defense is a demonstration rig. The output is working systems that prove concepts, not a shippable product.
- Not a game design course. The mechanics are minimal because the architecture is the content.
- Not a WPF/MVVM/enterprise patterns course. Every pattern here solves a real problem in a real Unity project. If it doesn't, we don't teach it.
- Not an incomplete project missing features. There is no shop, no tower placement, no game-over screen because those are UI features that don't demonstrate architecture concepts. The systems you build can support them — but building them is not this course's purpose.

## Starting Point

You have a Unity project with 3D assets and a map. No scripts. All code is written from scratch during the series. The scripts folder contains empty stub files with TODO comments referencing which episode implements them.

## Prerequisites

- Unity 2022.3+ installed
- Basic C# (classes, methods, properties, generics)
- Comfortable with Unity Inspector, prefabs, and the Scene view
- Willingness to open the Profiler and read the numbers