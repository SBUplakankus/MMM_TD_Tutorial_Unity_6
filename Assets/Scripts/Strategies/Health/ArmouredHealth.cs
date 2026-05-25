using Interfaces;
using Structs;
using UnityEngine;

namespace Strategies.Health
{
    public class ArmouredHealth : IHealthStrategy
    {
        // TODO: Episode 07 — Reduces incoming damage by (1 - armourPercent) before subtracting
        private readonly int _startHealth;
        private readonly float _armourStrength;
        private float _currentHealth;

        public ArmouredHealth(int startHealth, float armourStrength)
        {
            _startHealth = startHealth;
            _armourStrength = armourStrength;
        }

        public void Init()
        {
            _currentHealth = _startHealth;
        }

        public DamageResult TakeDamage(float amount)
        {
            var damage = amount - (amount * _armourStrength);
            _currentHealth -= damage;

            if (_currentHealth > 0f) return DamageResult.Alive(damage);
            
            _currentHealth = 0f;
            return DamageResult.Dead(damage);

        }

        public void Tick(float deltaTime) { }

        public bool IsAlive => _currentHealth > 0f;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _startHealth;
    }
}