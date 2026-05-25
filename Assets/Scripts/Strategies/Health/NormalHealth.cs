using Interfaces;
using Structs;

namespace Strategies.Health
{
    public class NormalHealth : IHealthStrategy
    {
        // TODO: Episode 07 — Simple subtraction health, no special mechanics
        
        private readonly int _startHealth;
        private float _currentHealth;

        public NormalHealth(int startHealth) => _startHealth = startHealth;

        public void Init() => _currentHealth = _startHealth;

        public DamageResult TakeDamage(float amount)
        {
            _currentHealth -= amount;

            if (!(_currentHealth <= 0f)) return DamageResult.Alive(amount);
            
            _currentHealth = 0f;
            return DamageResult.Dead(amount);

        }

        public void Tick(float deltaTime) { }

        public bool IsAlive => _currentHealth > 0f;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _startHealth;
    }
}