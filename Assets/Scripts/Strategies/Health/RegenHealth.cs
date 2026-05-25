using Interfaces;
using Structs;

namespace Strategies.Health
{
    public class RegenHealth : IHealthStrategy
    {
        // TODO: Episode 12 — Regenerates health over time in Tick, capped at MaxHealth
        public void Init()
        {
            throw new System.NotImplementedException();
        }

        public DamageResult TakeDamage(float amount)
        {
            throw new System.NotImplementedException();
        }

        public void Tick(float deltaTime)
        {
            throw new System.NotImplementedException();
        }

        public bool IsAlive { get; }
        public float CurrentHealth { get; }
        public float MaxHealth { get; }
    }
}