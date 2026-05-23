using Projectiles;
using Towers;
using UnityEngine;

namespace Towers
{
    [RequireComponent(typeof(TowerDetection))]
    public class TowerFiring : MonoBehaviour
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
            var projectileObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            var projectile = projectileObj.GetComponent<ProjectileBase>();
            launcher.LookAt(_detection.CurrentTarget.Position);
            projectile.Launch(_detection.CurrentTarget);
            _fireCooldown = fireRate;
        }
        
        private void Awake() => _detection = GetComponent<TowerDetection>();
        
        private void Update()
        {
            _fireCooldown -= Time.deltaTime;

            if (!_detection.HasTarget || !(_fireCooldown <= 0f)) return;
            Fire();
        }

        // TODO: Episode 08 — Replace Instantiate with pool Get
        // TODO: Episode 09 — Replace Instance with Services.Get
    }
}