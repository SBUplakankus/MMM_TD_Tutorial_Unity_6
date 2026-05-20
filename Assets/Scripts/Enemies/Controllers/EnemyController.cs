using Core;
using Data;
using Enemies.Components;
using Events.Registries;
using Interfaces;
using Systems.Game;
using Systems.Parsing;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable
    {
        #region Fields

        [Header("Enemy UI")]
        [SerializeField] private EnemyHealthBar healthBar;

        #endregion

        #region Properties

        public EnemyPath Path { get; private set; }
        public int CurrentWayPointIndex { get; set; }
        public IHealthStrategy Health { get; private set; }
        public IMovementStrategy Movement { get; private set; }
        public int GoldGiven { get; private set; }
        public int Damage { get; private set; }

        public Vector3 Position => transform.position;
        public bool IsAlive => Health != null && Health.IsAlive;

        #endregion

        #region Class Methods

        public void Initialize(EnemyData data, EnemyPath path)
        {
            // TODO: Store Path reference
            // TODO: Create strategies via StrategyFactory
            // TODO: Store GoldGiven and Damage from data
            // TODO: Call Health.Initialize() and Movement.Initialize(this)
            // TODO: Subscribe to Movement.OnMovementCompleted → OnReachedEnd
        }

        public void Die()
        {
            // TODO: Raise Services.Get<CombatEvents>().EnemyDeath.Raise(GoldGiven)
            // TODO: Unsubscribe from Movement.OnMovementCompleted
            // TODO: Return to pool via Services.Get<ObjectPoolManager>().Return("enemy", gameObject)
        }

        #endregion

        #region Unity Methods

        private void Update()
        {
            // TODO: Call Health.Tick(Time.deltaTime)
            // TODO: Call Movement.Tick(this)
        }

        #endregion

        #region IDamageable

        public void TakeDamage(float damage)
        {
            // TODO: Call Health.TakeDamage(damage) — returns DamageResult
            // TODO: If result.Died, call Die()
        }

        #endregion

        #region Private Methods

        private void OnReachedEnd()
        {
            // TODO: Raise Services.Get<CombatEvents>().EnemyReachedEnd.Raise(Damage)
            // TODO: Return to pool
        }

        #endregion
    }
}