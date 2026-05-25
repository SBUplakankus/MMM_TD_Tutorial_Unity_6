using Interfaces;
using Structs;

namespace Strategies.Health
{
    public class ShieldHealth : IHealthStrategy
    {
        // TODO: Episode 12 — Shield absorbs damage first, overflow hits health
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