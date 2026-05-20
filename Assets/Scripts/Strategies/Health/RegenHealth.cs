using Interfaces;

namespace Strategies.Health
{
    // TODO: Plain class implementing IHealthStrategy — no longer a ScriptableObject
    // Constructor takes (int startHealth, float regenRate, float regenDelay)
    // Holds: _startHealth, _regenRate, _regenDelay, _currentHealth, _timeSinceLastDamage
    // Initialize() sets _currentHealth = _startHealth, _timeSinceLastDamage = 0
    // TakeDamage() subtracts amount, resets _timeSinceLastDamage = 0, returns DamageResult
    // Tick(deltaTime):
    //   Increment _timeSinceLastDamage by deltaTime
    //   If _timeSinceLastDamage >= _regenDelay AND _currentHealth < _startHealth:
    //     Heal by _regenRate * deltaTime, clamp to _startHealth
    public class RegenHealth : IHealthStrategy
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