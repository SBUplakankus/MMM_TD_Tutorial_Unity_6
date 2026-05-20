using Interfaces;

namespace Strategies.Health
{
    // TODO: Plain class implementing IHealthStrategy — no longer a ScriptableObject
    // Constructor takes (int startHealth, float armourPercent)
    // Holds: _startHealth, _armourPercent, _currentHealth
    // TakeDamage() reduces amount by (1 - armourPercent), returns DamageResult
    // Tick() is empty
    public class ArmouredHealth : IHealthStrategy
    {
        // TODO: Implement IHealthStrategy
        public void Initialize()
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