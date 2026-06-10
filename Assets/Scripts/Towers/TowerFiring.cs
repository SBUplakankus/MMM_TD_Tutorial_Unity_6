using Enums;
using Interfaces;
using Projectiles;
using Systems.Managers;
using Towers;
using UnityEngine;

namespace Towers
{
    [RequireComponent(typeof(TowerDetection))]
    public class TowerFiring : MonoBehaviour, IUpdateable
    {
        // TODO: Episode 04 — Fire projectiles at TowerDetection.CurrentTarget on cooldown
        // Instantiate projectile, call Launch(detection.CurrentTarget), reset cooldown
        
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform launcher;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 1f;
        
        private TowerDetection _detection;
        private float _fireCooldown;

        private void Fire()
        {
            var projectileObj = ObjectPoolManager.Instance.GetProjectile(firePoint.position, firePoint.rotation);
            var projectile = projectileObj.GetComponent<ProjectileBase>();
            launcher.LookAt(_detection.CurrentTarget.Position);
            projectile.Launch(_detection.CurrentTarget);
            _fireCooldown = fireRate;
        }
        
        private void Awake() => _detection = GetComponent<TowerDetection>();
        
        public void Tick(float deltaTime)
        { 
            _fireCooldown -= Time.deltaTime;

            if (!_detection.HasTarget || !(_fireCooldown <= 0f)) return;
            Fire();
        }
        private void OnEnable() => GameUpdateManager.Instance.Register(this, UpdatePriority.High);
        private void OnDisable() => GameUpdateManager.Instance.Unregister(this);

        // TODO: Episode 08 — Replace Instantiate with pool Get
        // TODO: Episode 09 — Replace Instance with Services.Get
    }
}