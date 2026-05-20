using Interfaces;
using UnityEngine;

namespace Projectiles
{
    public class ArrowProjectile : ProjectileBase
    {
        #region Fields

        [SerializeField] private int pierceCount;
        private int _pierceRemaining;

        #endregion

        #region Public API

        public override void Launch(ITargetable target)
        {
            base.Launch(target);
            _pierceRemaining = pierceCount;
        }

        #endregion

        #region Protected Methods

        protected override void Move()
        {
            // TODO: Move in a straight line towards TargetPosition
            // TODO: Arrow does NOT home — direction is set at launch
        }

        protected override void OnHit(ITargetable target)
        {
            // TODO: Apply damage to target
            // TODO: Decrement _pierceRemaining
            // TODO: If _pierceRemaining <= 0, call ReturnToPool()
            // TODO: Otherwise, continue moving (don't return)
        }

        #endregion

        #region IPoolable

        public override void Reset()
        {
            base.Reset();
            _pierceRemaining = pierceCount;
        }

        #endregion
    }
}