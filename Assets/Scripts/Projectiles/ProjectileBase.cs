using Core;
using Interfaces;
using Systems.Managers;
using UnityEngine;

namespace Projectiles
{
    public class ProjectileBase : MonoBehaviour, IPoolable
    {
        #region Fields

        [SerializeField] protected float moveSpeed;
        [SerializeField] protected float damage;
        [SerializeField] protected string poolKey;

        protected ITargetable Target;
        protected Vector3 TargetPosition;
        protected bool HasTarget;

        #endregion

        #region Lifecycle

        private void Update()
        {
            // TODO: Call Move() each frame
            // TODO: If target no longer alive, set HasTarget = false
        }

        #endregion

        #region Public API

        public virtual void Launch(ITargetable target)
        {
            // TODO: Set Target and HasTarget = true
        }

        public virtual void Launch(Vector3 position)
        {
            // TODO: Set TargetPosition, HasTarget = false
        }

        #endregion

        #region Protected Methods

        protected virtual void Move()
        {
            // TODO: Move toward Target.Position or TargetPosition
        }

        protected virtual void OnHit(ITargetable target)
        {
            // TODO: If target is IDamageable, call TakeDamage
            // TODO: Call ReturnToPool()
        }

        protected virtual void OnHitPosition(Vector3 position)
        {
            // TODO: AOE damage in radius
            // TODO: Call ReturnToPool()
        }

        protected void ReturnToPool()
        {
            // TODO: Services.Get<ObjectPoolManager>().Return(poolKey, gameObject)
        }

        #endregion

        #region IPoolable

        public virtual void Reset()
        {
            // TODO: Clear Target, HasTarget, position/rotation
        }

        #endregion
    }
}