using System;
using Enemies.Controllers;
using UnityEngine;

namespace Strategies.Health
{
    public abstract class HealthStrategy : ScriptableObject
    {
        [SerializeField] protected int startHealth = 100;
        protected float CurrentHealth;
        
        public event Action OnDeath;

        public abstract void Initialize(EnemyController enemy);
        public abstract void TakeDamage(EnemyController enemy, float amount);

        protected void InitHealth()
        {
            CurrentHealth = startHealth;
        }
        
        protected void CheckForDeath()
        {
            if (CurrentHealth > 0) return;
            OnDeath?.Invoke();
        }
    }
}