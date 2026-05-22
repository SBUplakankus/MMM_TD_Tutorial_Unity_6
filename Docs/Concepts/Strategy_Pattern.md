# Strategy Pattern

## Overview
Define a family of algorithms, encapsulate each one, and make them interchangeable. Lets the algorithm vary independently from the client that uses it.

## Why Use It
- Swap behavior at runtime (armoured vs normal health)
- Add new types without changing existing code
- Compose behaviors (health type + movement type = new enemy)

## When Not to Use
- Only one behavior variant exists
- The behavior is trivially small (a single if/else)
- You haven't felt the pain of copy-pasting yet

## In This Project
- `IHealthStrategy` — NormalHealth, ArmouredHealth, ShieldHealth, RegenHealth
- `IMovementStrategy` — GroundedPath, FlyingPath
- `ITargetingStrategy` — FirstTargeting, LastTargeting, StrongTargeting, CloseTargeting
- Strategies are plain C# classes, NOT ScriptableObjects
- Created by StrategyFactory from HealthConfig/MovementConfig enum
- Each enemy gets its own instance — no shared state bug

## Code Example
Simple example showing the pattern:
```csharp
public interface IHealthStrategy
{
    DamageResult TakeDamage(float amount);
    bool IsAlive { get; }
}

public class NormalHealth : IHealthStrategy
{
    private float _currentHealth;
    
    public NormalHealth(int startHealth) => _currentHealth = startHealth;
    
    public DamageResult TakeDamage(float amount)
    {
        _currentHealth -= amount;
        return _currentHealth <= 0 ? DamageResult.Dead() : DamageResult.Alive();
    }
    
    public bool IsAlive => _currentHealth > 0;
}
```

Related: [Factory Pattern](Factory_Pattern.md) | [Interfaces](Interfaces.md)