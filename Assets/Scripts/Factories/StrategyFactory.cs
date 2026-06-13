using System;
using Data;
using Enums;
using Interfaces;
using Strategies.Health;
using Strategies.Movement;
using Strategies.Targeting;

namespace Factories
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
        
        public static IHealthStrategy CreateHealth(HealthConfig config)
        {
            return config.type switch
            {
                HealthType.Normal   => new NormalHealth(config.startHealth),
                HealthType.Armoured => new ArmouredHealth(config.startHealth, config.armourStrength),
                _                  => new NormalHealth(config.startHealth)
            };
        }
        
        // TODO: Episode 12 — Add Shield + Regen cases to CreateHealth

        public static ITargetingStrategy CreateTargeting(TargetingType type)
        {
            return type switch
            {
                TargetingType.Nearest   => new TargetClosest(),
                TargetingType.First     => new TargetFirst(),
                TargetingType.Last      => new TargetLast(),
                TargetingType.Weakest   => new TargetWeakest(),
                TargetingType.Strongest => new TargetStrongest(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}