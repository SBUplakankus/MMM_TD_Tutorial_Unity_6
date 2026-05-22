# Factory Pattern

## Overview
Create objects without specifying the exact class to create. A factory method decides which concrete class to instantiate based on input (typically an enum or config).

## Why Use It
- Decouple creation from usage (EnemyController doesn't need to know about ArmouredHealth)
- Centralize object creation logic in one place
- Easy to add new types — just add a case to the factory

## When Not to Use
- Only one type exists (no need for a factory)
- When a simple constructor call is clearer

## In This Project
- `StrategyFactory.CreateHealth(HealthConfig)` — enum switch returns IHealthStrategy
- `StrategyFactory.CreateMovement(MovementConfig)` — enum switch returns IMovementStrategy
- Config SOs hold the data + type enum, factory reads the enum and constructs the right class

## Code Example
```csharp
public static class StrategyFactory
{
    public static IHealthStrategy CreateHealth(HealthConfig config)
    {
        return config.type switch
        {
            HealthType.Normal   => new NormalHealth(config.startHealth),
            HealthType.Armoured => new ArmouredHealth(config.startHealth, config.armourPercent),
            HealthType.Shield   => new ShieldHealth(config.startHealth, config.shieldPoints),
            HealthType.Regen    => new RegenHealth(config.startHealth, config.regenRate, config.regenDelay),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
```

Adding a new health type = new class + new factory case. EnemyController never changes.

Related: [Strategy Pattern](Strategy_Pattern.md)