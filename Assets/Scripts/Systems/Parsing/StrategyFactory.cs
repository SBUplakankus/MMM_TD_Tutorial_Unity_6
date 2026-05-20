using Data;
using Interfaces;

namespace Systems.Parsing
{
    // TODO: Static factory — creates IHealthStrategy/IMovementStrategy instances from config SOs
    // Uses enum switch pattern: config.type => new ConcreteStrategy(config values)
    public static class StrategyFactory
    {
        // TODO: CreateHealth(HealthConfig) — switch on config.type, return IHealthStrategy
        // NormalHealth(startHealth), ArmouredHealth(startHealth, armourPercent),
        // ShieldHealth(startHealth, shieldPoints), RegenHealth(startHealth, regenRate, regenDelay)

        // TODO: CreateMovement(MovementConfig) — switch on config.type, return IMovementStrategy
        // GroundedPath(moveSpeed), FlyingPath(moveSpeed, flyingHeight)
    }
}