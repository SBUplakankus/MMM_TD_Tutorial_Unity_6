using Data;
using Enums;
using Interfaces;
using Strategies.Health;
using Strategies.Movement;

namespace Systems.Parsing
{
    public static class StrategyFactory
    {
        // TODO: Episode 06 — static IMovementStrategy CreateMovement(MovementConfig config)
        
        public static IMovementStrategy CreateMovement(MovementConfig config)
        {
            return config.type switch
            {
                MovementType.Grounded => new GroundedPath(config.moveSpeed),
                MovementType.Flying => new FlyingPath(config.moveSpeed, config.flyingHeight),
                _ => new GroundedPath(config.moveSpeed)
            };
        }
        
        // TODO: Episode 07 — static IHealthStrategy CreateHealth(HealthConfig config)
        // TODO: Episode 12 — Add Shield + Regen cases to CreateHealth
    }
}