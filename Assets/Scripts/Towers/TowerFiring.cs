using Core;
using Interfaces;
using Systems.Managers;
using UnityEngine;

namespace Towers
{
    public class TowerFiring : MonoBehaviour
    {
        #region Fields

        [SerializeField] private float fireRate;
        [SerializeField] private string projectilePoolKey;
        [SerializeField] private Transform firePoint;

        private float _cooldownTimer;

        #endregion

        #region Properties

        public bool CanFire => _cooldownTimer <= 0f;

        #endregion

        #region Lifecycle

        private void Update()
        {
            // TODO: Decrement _cooldownTimer by Time.deltaTime
        }

        #endregion

        #region Public API

        public void TryFire(ITargetable target)
        {
            // TODO: If !CanFire, return
            // TODO: Fetch projectile from Services.Get<ObjectPoolManager>().Get(projectilePoolKey, firePoint.position, firePoint.rotation)
            // TODO: Get ProjectileBase component and call Launch(target)
            // TODO: Reset _cooldownTimer to 1f / fireRate
        }

        #endregion
    }
}