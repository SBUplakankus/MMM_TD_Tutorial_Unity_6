# Object Pooling

## Overview
Reuse objects instead of creating and destroying them. Pre-warm a pool of inactive objects, activate when needed, deactivate and return when done.

## Why Use It
- Eliminate GC spikes from frequent Instantiate/Destroy
- Consistent frame times during heavy spawning
- Unity's ObjectPool<T> provides this out of the box

## When Not to Use
- Objects that are rarely spawned (UI panels, one-time effects)
- When pool size is unpredictable and unbounded
- When object initialization is more expensive than just instantiation

## In This Project
- `ObjectPoolManager` — manages multiple pools by key string
- `PoolConfig` — inspector configuration per pool (prefab, defaultSize, maxSize)
- `IPoolable` — Reset() method called when object returns to pool
- Pre-warming on Awake for consistent first-frame performance

## Code Example
```csharp
// Fetch from pool
var enemy = Services.Get<ObjectPoolManager>().Get("enemy", position, rotation);
enemy.GetComponent<EnemyController>().Initialize(data, path);

// Return to pool
public void Die()
{
    Services.Get<CombatEvents>().EnemyDeath.Raise(GoldGiven);
    Services.Get<ObjectPoolManager>().Return("enemy", gameObject);
}
```

Key gotcha: IPoolable.Reset() must clear ALL per-instance state. If Reset() doesn't clear the IHealthStrategy reference, pooled enemies retain old health.

Related: [Interfaces](Interfaces.md)