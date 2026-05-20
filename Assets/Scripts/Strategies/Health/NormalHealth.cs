using Interfaces;

namespace Strategies.Health
{
    // TODO: Plain class implementing IHealthStrategy — no longer a ScriptableObject
    // Constructor takes (int startHealth)
    // Holds: _startHealth, _currentHealth
    // Initialize() sets _currentHealth = _startHealth
    // TakeDamage() subtracts amount, returns DamageResult.Dead/Alive based on _currentHealth
    // Tick() is empty — no per-frame logic needed
    public class NormalHealth : IHealthStrategy
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