using Interfaces;

namespace Strategies.Health
{
    // TODO: Plain class implementing IHealthStrategy — no longer a ScriptableObject
    // Constructor takes (int startHealth, int shieldPoints)
    // Holds: _startHealth, _shieldPoints, _currentHealth, _currentShield
    // Initialize() sets _currentHealth = _startHealth, _currentShield = _shieldPoints
    // TakeDamage(): if _currentShield > 0, shield absorbs damage first
    //   If shield >= amount, reduce shield, return Alive
    //   If shield < amount, overflow goes to _currentHealth, check death
    //   If shield depleted, remaining damage goes to _currentHealth normally
    // Tick() is empty
    public class ShieldHealth : IHealthStrategy
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