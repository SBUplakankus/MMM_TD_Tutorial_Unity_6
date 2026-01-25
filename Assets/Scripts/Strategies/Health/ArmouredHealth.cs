using Enemies.Controllers;
using Strategies.Health;
using UnityEngine;

namespace Strategies.Health
{
[CreateAssetMenu(menuName = "Strategies/Health/Armoured")]
    public class ArmouredHealth : HealthStrategy
    {
        [Range(0, 0.99f)] 
        [SerializeField] private float armourPercent = 0.2f;
        
        public override void Initialize(EnemyController enemy)
        {
            InitHealth();
        }

        public override void TakeDamage(EnemyController enemy, float amount)
        {
            var reducedDamage = amount * (1f - armourPercent);
            CurrentHealth -= reducedDamage;
            CheckForDeath();
        }
    }
}