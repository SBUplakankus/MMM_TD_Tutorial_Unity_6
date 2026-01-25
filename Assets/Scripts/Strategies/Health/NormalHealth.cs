using Enemies.Controllers;
using UnityEngine;

namespace Strategies.Health
{
    [CreateAssetMenu(menuName = "Strategies/Health/Normal")]
    public class NormalHealth : HealthStrategy
    {
        public override void Initialize(EnemyController enemy)
        {
            InitHealth();
        }

        public override void TakeDamage(EnemyController enemy, float amount)
        {
            CurrentHealth -= amount;
            CheckForDeath();
        }
    }
}