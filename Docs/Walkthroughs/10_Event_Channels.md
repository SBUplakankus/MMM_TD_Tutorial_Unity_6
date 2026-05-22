# Episode 10: Event Channels

## What You're Building

Decouple `EnemyController.Die()` from `PlayerStats`. Instead of calling `Services.Get<PlayerStats>().AddGold()` directly, raise a `CombatEvents.EnemyDeath` event. PlayerStats subscribes and reacts. Any number of systems can react to enemy death without `EnemyController` knowing about them.

## The Problem

`EnemyController.Die()` currently calls `PlayerStats.AddGold` directly. If you want to also play a death sound, show a damage number, track an achievement, or update a quest — you edit `Die()` every time. Every new feature touches the same method. This violates Open/Closed.

Event channels fix this: `Die()` raises an event. Any number of systems subscribe. Zero coupling.

## EventChannel.cs + EventChannelT.cs

```csharp
using System;
using System.Collections.Generic;

namespace Events
{
    public class EventChannel
    {
        private readonly List<Action> _listeners = new();

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i]?.Invoke();
        }

        public void Register(Action listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void Unregister(Action listener)
        {
            _listeners.Remove(listener);
        }
    }

    public class EventChannel<T>
    {
        private readonly List<Action<T>> _listeners = new();

        public void Raise(T value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i]?.Invoke(value);
        }

        public void Register(Action<T> listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void Unregister(Action<T> listener)
        {
            _listeners.Remove(listener);
        }
    }
}
```

**Why iterate backwards?** If a listener removes itself during `Raise()` (e.g., a one-shot listener), iterating forwards would skip elements or throw. Backwards iteration is safe against mid-raise modifications.

**Why prevent duplicate registration?** Accidentally subscribing the same method twice causes it to fire twice. The `Contains` check is a safety net.

## CombatEvents.cs

```csharp
using Events;

namespace Events.Registries
{
    public class CombatEvents
    {
        public readonly EventChannel<int> EnemyDeath = new();
        public readonly EventChannel<int> EnemyReachedEnd = new();
    }
}
```

`EnemyDeath` carries `int` — the gold value. `EnemyReachedEnd` carries `int` — the lives damage.

## EconomyEvents.cs

```csharp
using Events;

namespace Events.Registries
{
    public class EconomyEvents
    {
        public readonly EventChannel<int> GoldChanged = new();
        public readonly EventChannel<int> LivesChanged = new();
    }
}
```

Raised by `PlayerStats` when values change. UI subscribes to these instead of polling.

## GameBootstrapper.cs (updated)

```csharp
using Core;
using Events.Registries;
using Systems.Game;
using Systems.Managers;
using UnityEngine;

namespace Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private ObjectPoolManager objectPoolManager;

        [SerializeField] private int startingGold = 100;
        [SerializeField] private int startingLives = 20;

        private void Awake()
        {
            RegisterServices();
            InitializeServices();
        }

        private void OnDestroy()
        {
            Services.Get<PlayerStats>().Cleanup();
            Services.Clear();
        }

        private void RegisterServices()
        {
            Services.Register(objectPoolManager);

            var combatEvents = new CombatEvents();
            Services.Register(combatEvents);

            var economyEvents = new EconomyEvents();
            Services.Register(economyEvents);

            var playerStats = new PlayerStats(startingGold, startingLives);
            Services.Register(playerStats);
        }

        private void InitializeServices()
        {
            Services.Get<PlayerStats>().Initialize();
        }
    }
}
```

Event registries are registered before `PlayerStats` so that `PlayerStats.Initialize()` can subscribe to them.

## PlayerStats.cs (event subscriber)

```csharp
using System;
using Core;
using Events.Registries;

namespace Systems.Game
{
    public class PlayerStats
    {
        public int Gold { get; private set; }
        public int Lives { get; private set; }

        public event Action<int> OnGoldChanged;
        public event Action<int> OnLivesChanged;

        private CombatEvents _combatEvents;
        private EconomyEvents _economyEvents;

        public PlayerStats(int startingGold, int startingLives)
        {
            Gold = startingGold;
            Lives = startingLives;
        }

        public void Initialize()
        {
            _combatEvents = Services.Get<CombatEvents>();
            _economyEvents = Services.Get<EconomyEvents>();

            _combatEvents.EnemyDeath.Register(OnEnemyDeath);
            _combatEvents.EnemyReachedEnd.Register(OnEnemyReachedEnd);
        }

        public void Cleanup()
        {
            _combatEvents.EnemyDeath.Unregister(OnEnemyDeath);
            _combatEvents.EnemyReachedEnd.Unregister(OnEnemyReachedEnd);
        }

        private void OnEnemyDeath(int gold)
        {
            AddGold(gold);
        }

        private void OnEnemyReachedEnd(int damage)
        {
            SubtractLives(damage);
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
            _economyEvents?.GoldChanged.Raise(Gold);
        }

        public bool RemoveGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            _economyEvents?.GoldChanged.Raise(Gold);
            return true;
        }

        public void SubtractLives(int amount)
        {
            Lives -= amount;
            OnLivesChanged?.Invoke(Lives);
            _economyEvents?.LivesChanged.Raise(Lives);
        }
    }
}
```

**What changed from Episode 09:**

| Before | After |
|--------|-------|
| `Initialize()` was empty | Subscribes to CombatEvents |
| `Cleanup()` was empty | Unsubscribes from CombatEvents |
| No event awareness | Reacts to `EnemyDeath` and `EnemyReachedEnd` |
| Only local C# events | Also raises `EconomyEvents` for UI |

**Why keep the local `OnGoldChanged` event?** Some UI elements may already subscribe to it. Removing it would break existing code. Both coexist — local events for direct subscribers, EconomyEvents for decoupled subscribers. You can remove the local events later.

## EnemyController.cs (event raiser)

```csharp
using Core;
using Data;
using Events.Registries;
using Interfaces;
using Systems.Game;
using Systems.Managers;
using Systems.Parsing;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable, IPoolable
    {
        [SerializeField] private EnemyHealthBar healthBar;

        public EnemyPath Path { get; private set; }
        public int CurrentWayPointIndex { get; set; }
        public IHealthStrategy Health { get; private set; }
        public IMovementStrategy Movement { get; private set; }
        private int _goldGiven;
        private int _damage;

        private CombatEvents _combatEvents;
        private ObjectPoolManager _poolManager;

        // ITargetable
        public Vector3 Position => transform.position;
        public bool IsAlive => Health != null && Health.IsAlive;

        private void OnEnable()
        {
            _combatEvents = Services.Get<CombatEvents>();
            _poolManager = Services.Get<ObjectPoolManager>();
        }

        public void Initialize(EnemyData data, EnemyPath path)
        {
            Path = path;
            Health = StrategyFactory.CreateHealth(data.HealthConfig);
            Movement = StrategyFactory.CreateMovement(data.MovementConfig);
            _goldGiven = data.GoldGiven;
            _damage = data.Damage;

            Health.Initialize();
            Movement.Initialize(this);
            Movement.OnMovementCompleted += OnReachedEnd;
        }

        private void Update()
        {
            if (!IsAlive) return;

            Health.Tick(Time.deltaTime);
            Movement.Tick(this);

            if (healthBar != null)
            {
                healthBar.SetHealth(Health.CurrentHealth, Health.MaxHealth);
                healthBar.SetPosition(transform.position);
            }
        }

        public void TakeDamage(float damage)
        {
            DamageResult result = Health.TakeDamage(damage);
            if (result.Died)
                Die();
        }

        private void Die()
        {
            _combatEvents.EnemyDeath.Raise(_goldGiven);
            Movement.OnMovementCompleted -= OnReachedEnd;
            _poolManager.Return("enemy", gameObject);
        }

        private void OnReachedEnd()
        {
            _combatEvents.EnemyReachedEnd.Raise(_damage);
            Movement.OnMovementCompleted -= OnReachedEnd;
            _poolManager.Return("enemy", gameObject);
        }

        // IPoolable
        public void Reset()
        {
            CurrentWayPointIndex = 0;
            Health = null;
            Movement = null;
            _goldGiven = 0;
            _damage = 0;
            Path = null;
        }
    }
}
```

**What changed from Episode 09:**

| Before | After |
|--------|-------|
| `Services.Get<PlayerStats>().AddGold(...)` in Die | `_combatEvents.EnemyDeath.Raise(_goldGiven)` |
| `Services.Get<PlayerStats>().SubtractLives(...)` in OnReachedEnd | `_combatEvents.EnemyReachedEnd.Raise(_damage)` |
| `Services.Get<ObjectPoolManager>()` called each time | Cached in `OnEnable` |
| `Services.Get<CombatEvents>()` called each time | Cached in `OnEnable` |

**Why cache in OnEnable instead of Initialize?** `OnEnable` runs every time the object is activated from the pool. `Initialize` only runs once per pool cycle. But `OnEnable` is guaranteed to run before `Initialize` because pool `Get()` calls `SetActive(true)` first. Caching here avoids repeated `Services.Get` calls. `Awake` only runs once on first creation — it doesn't run on pool reactivation.

## EconomyUI.cs (event subscriber)

```csharp
using Core;
using Events.Registries;
using TMPro;
using UnityEngine;

namespace UI
{
    public class EconomyUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text livesText;

        private EconomyEvents _economyEvents;

        private void Start()
        {
            _economyEvents = Services.Get<EconomyEvents>();
            _economyEvents.GoldChanged.Register(UpdateGoldDisplay);
            _economyEvents.LivesChanged.Register(UpdateLivesDisplay);

            UpdateGoldDisplay(Services.Get<PlayerStats>().Gold);
            UpdateLivesDisplay(Services.Get<PlayerStats>().Lives);
        }

        private void OnDestroy()
        {
            _economyEvents?.GoldChanged.Unregister(UpdateGoldDisplay);
            _economyEvents?.LivesChanged.Unregister(UpdateLivesDisplay);
        }

        private void UpdateGoldDisplay(int gold) => goldText.text = $"Gold: {gold}";
        private void UpdateLivesDisplay(int lives) => livesText.text = $"Lives: {lives}";
    }
}
```

**What changed from Episode 05:** Subscribes to `EconomyEvents` instead of `PlayerStats.OnGoldChanged`. Same result, but the UI no longer depends on `PlayerStats` directly.

## Debugging

| Symptom | Cause | Fix |
|---------|-------|-----|
| Enemy dies but no gold | CombatEvents not registered in GameBootstrapper | Add `Services.Register(new CombatEvents())` |
| Gold updates twice | Subscribed to both PlayerStats event and EconomyEvents | Remove one subscription |
| NullRef on _combatEvents in Die | OnEnable not called | Ensure enemy is activated (SetActive true) before Die is called |
| Events stop after scene reload | Cleanup not called | GameBootstrapper.OnDestroy calls PlayerStats.Cleanup and Services.Clear |
| Listener fires twice | Registered same method twice | EventChannel prevents duplicates, but check for double Register calls |