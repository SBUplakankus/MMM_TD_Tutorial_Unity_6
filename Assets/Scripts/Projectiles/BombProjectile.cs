using Interfaces;
using UnityEngine;

namespace Projectiles
{
    public class BombProjectile : ProjectileBase
    {
        #region Fields

        [SerializeField] private float explosionRadius;
        [SerializeField] private LayerMask enemyLayer;

        #endregion

        #region Protected Methods

        protected override void Move()
        {
            // TODO: Move in a straight line towards TargetPosition
            // TODO: Bomb does NOT home — direction set at launch
        }

        protected override void OnHitPosition(Vector3 position)
        {
            // TODO: Use OverlapSphere at position with explosionRadius and enemyLayer
            // TODO: For each collider, get IDamageable and apply damage
            // TODO: Play explosion VFX (fetch from pool if available)
            // TODO: Call ReturnToPool()
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // TODO: Draw explosion radius sphere
        }

        #endregion
    }
}