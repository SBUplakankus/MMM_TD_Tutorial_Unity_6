using System.IO;
using Data;
using Enemies.Components;
using Interfaces;
using Strategies.Health;
using Strategies.Movement;
using Systems.Game;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable
    {
        #region Fields

        [Header("Enemy UI")]
        [SerializeField] private EnemyHealthBar healthBar;
        
        #endregion
        
        #region Properties

        public EnemyPath Path { get; private set; }
        public int CurrentWayPointIndex { get; set; }
        public HealthStrategy Health { get;  private set; }
        public MovementStrategy Movement { get; private set; }
        public int GoldGiven { get; private set; }
        public int Damage { get; private set; }
        
        #endregion

        #region Class Methods

        private void InitData(EnemyData data)
        {
            Health = data.Health;
            Movement = data.Movement;
            GoldGiven = data.GoldGiven;
            Damage = data.Damage;
        }

        private void InitStrategy()
        {
            Health.Initialize(this);
            Movement.Initialize(this);
        }

        public void Initialize(EnemyData data, EnemyPath path)
        {
            Path = path;
            InitData(data);
            InitStrategy();
        }
        
        public void Die()
        {
            Destroy(gameObject);
        }
        
        #endregion
        
        #region Unity Methods

        private void Update()
        {
            Movement.Tick(this);
        }
        
        #endregion

        public void TakeDamage(float damage)
        {
            Health.TakeDamage(this, damage);
        }
    }
}
