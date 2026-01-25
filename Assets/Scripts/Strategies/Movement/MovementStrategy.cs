using System;
using Enemies.Controllers;
using Systems.Game;
using UnityEngine;

namespace Strategies.Movement
{
    public abstract class MovementStrategy : ScriptableObject
    {
        #region Fields

        [SerializeField] protected float moveSpeed;
        protected EnemyPath Path;
        
        public event Action OnMovementCompletion;

        #endregion

        #region Properties

        public float MoveSpeed => moveSpeed;

        #endregion
        
        #region Methods

        public virtual void Initialize(EnemyController enemy)
        {
            OnMovementCompletion = null;
            enemy.CurrentWayPointIndex = 0;
            SetPath(enemy);
        }
        
        private void SetPath(EnemyController enemy) => Path = enemy.Path;

        protected abstract void SetStartPosition(EnemyController enemy); 
        
        protected void CompleteMovement() => OnMovementCompletion?.Invoke();
        
        public abstract void Tick(EnemyController enemy);
        
        #endregion
    }
}