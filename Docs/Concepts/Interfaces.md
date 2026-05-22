# Interfaces

## Overview
A contract that defines what a class can do, without saying how it does it. Methods and properties with no implementation.

## Why Use It
- **Decoupling** — depend on the contract, not the concrete class
- **Testability** — mock implementations for unit tests
- **Flexibility** — swap implementations without changing consumers
- **Composition** — a class can implement multiple interfaces (IDamageable AND ITargetable)

## When Not to Use
- Only one implementation will ever exist
- The interface has only one method and the class is trivially small
- You're creating interfaces for every class "just in case" (YAGNI)

## In This Project
- `IDamageable` — anything that can take damage (enemies, destructible props)
- `ITargetable` — anything a tower can target (enemies)
- `IPoolable` — anything that can be reset and returned to an object pool
- `IHealthStrategy` — health behavior contract (Normal, Armoured, Shield, Regen)
- `IMovementStrategy` — movement behavior contract (Grounded, Flying)
- `ITargetingStrategy` — targeting behavior contract (First, Last, Strong, Close)

## Code Example
```csharp
public interface IDamageable
{
    void TakeDamage(float amount);
}

public interface ITargetable
{
    Vector3 Position { get; }
    bool IsAlive { get; }
    float PathProgress { get; }
}

// TowerDetection works with ANY ITargetable
// It doesn't know or care about EnemyController
private ITargetable SelectTarget()
{
    return _targetingStrategy.GetTarget(_targetsInRange, transform.position);
}
```

Related: [Strategy Pattern](Strategy_Pattern.md) | [Observer Pattern](Observer_Pattern.md)