# Episode 09: Service Locator

## What You're Building

A single `Services` static class replaces all scattered `Instance` singletons. `GameBootstrapper` becomes the composition root that wires all dependencies in one place.

## The Problem

Episode 08 left us with `ObjectPoolManager.Instance`. Episode 05 gave us `PlayerStats.Instance`. Soon there'll be `WaveManager.Instance`. Every class hides its dependencies — you have to read every method to discover them. You can't swap implementations for testing. Initialization order is undefined across GameObjects.

## Services.cs

```csharp
using System;
using System.Collections.Generic;

namespace Core
{
    public static class Services
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T service) where T : class
        {
            Type type = typeof(T);
            if (_services.ContainsKey(type))
                throw new InvalidOperationException(
                    $"Service {type.Name} already registered.");
            _services[type] = service;
        }

        public static T Get<T>() where T : class
        {
            Type type = typeof(T);
            if (_services.TryGetValue(type, out object service))
                return (T)service;
            throw new KeyNotFoundException(
                $"Service {type.Name} not registered. Add it to GameBootstrapper.");
        }

        public static void Clear()
        {
            _services.Clear();
        }
    }
}
```

- `Register<T>` throws on duplicate — catches double-registration bugs
- `Get<T>` throws with a message pointing to GameBootstrapper — the most common debugging question
- `Clear()` called in `OnDestroy` to prevent stale references on scene unload

## GameBootstrapper.cs

```csharp
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
        }

        private void OnDestroy()
        {
            Services.Get<PlayerStats>().Cleanup();
            Services.Clear();
        }

        private void RegisterServices()
        {
            Services.Register(objectPoolManager);

            var playerStats = new PlayerStats(startingGold, startingLives);
            Services.Register(playerStats);
        }
    }
}
```

**Registration order matters:**
1. `ObjectPoolManager` first — spawning needs the pool immediately
2. `PlayerStats` — plain C# class, created after managers

`WaveManager` is NOT registered here — it doesn't exist yet. Episode 11 adds it.

## PlayerStats.cs (plain C# class)

```csharp
using System;

namespace Systems.Game
{
    public class PlayerStats
    {
        public int Gold { get; private set; }
        public int Lives { get; private set; }

        public event Action<int> OnGoldChanged;
        public event Action<int> OnLivesChanged;

        public PlayerStats(int startingGold, int startingLives)
        {
            Gold = startingGold;
            Lives = startingLives;
        }

        public void Initialize() { }

        public void AddGold(int amount)
        {
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        public bool RemoveGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            return true;
        }

        public void SubtractLives(int amount)
        {
            Lives -= amount;
            OnLivesChanged?.Invoke(Lives);
        }

        public void Cleanup() { }
    }
}
```

**What changed from Episode 05:**

| Before | After |
|--------|-------|
| MonoBehaviour | Plain C# class |
| `Instance` singleton property | Gone — accessed via `Services.Get<PlayerStats>()` |
| `Awake()` initialization | Constructor + `Initialize()` |
| `DontDestroyOnLoad` | Gone — GameBootstrapper manages lifecycle |

Plain C# because PlayerStats has no Transform, no Collider, no render — it's data + logic only. MonoBehaviour would add weight for no benefit.

## EnemyController.cs (Services integration)

```csharp
using Core;
using Data;
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

        // ITargetable
        public Vector3 Position => transform.position;
        public bool IsAlive => Health != null && Health.IsAlive;

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
            Services.Get<PlayerStats>().AddGold(_goldGiven);
            Movement.OnMovementCompleted -= OnReachedEnd;
            Services.Get<ObjectPoolManager>().Return("enemy", gameObject);
        }

        private void OnReachedEnd()
        {
            Services.Get<PlayerStats>().SubtractLives(_damage);
            Movement.OnMovementCompleted -= OnReachedEnd;
            Services.Get<ObjectPoolManager>().Return("enemy", gameObject);
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

**What changed from Episode 08:**

| Before | After |
|--------|-------|
| `PlayerStats.Instance.AddGold(...)` | `Services.Get<PlayerStats>().AddGold(...)` |
| `ObjectPoolManager.Instance.Return(...)` | `Services.Get<ObjectPoolManager>().Return(...)` |
| `ObjectPoolManager.Instance` property | Gone from ObjectPoolManager |

Every other file that used `.Instance` follows the same pattern:

```csharp
// TowerFiring: before
ObjectPoolManager.Instance.Get(projectilePoolKey, ...);
// TowerFiring: after
Services.Get<ObjectPoolManager>().Get(projectilePoolKey, ...);

// ProjectileBase: before
ObjectPoolManager.Instance.Return(poolKey, gameObject);
// ProjectileBase: after
Services.Get<ObjectPoolManager>().Return(poolKey, gameObject);

// EconomyUI: before
PlayerStats.Instance.OnGoldChanged += ...
// EconomyUI: after
Services.Get<PlayerStats>().OnGoldChanged += ...
```

## Debugging

| Symptom | Cause | Fix |
|---------|-------|-----|
| `KeyNotFoundException` | Forgot to register in GameBootstrapper | Add `Services.Register(...)` |
| `InvalidOperationException: already registered` | Same type registered twice | Check GameBootstrapper for duplicates |
| Services persist across scenes | `Services.Clear()` not called | Ensure GameBootstrapper.OnDestroy calls Clear |
| `PlayerStats.Instance` compile error | Old code not updated | Replace with `Services.Get<PlayerStats>()` |