using Enums;
using Interfaces;
using NUnit.Framework;
using Systems.Managers;
using UnityEngine;

namespace Towers
{
    public class TowerDetection : MonoBehaviour, IUpdateable
    {
        // TODO: Episode 03 — Detect enemies in radius, select nearest via ITargetable
        // OverlapSphere, find closest alive ITargetable, cache as CurrentTarget
        // OnDrawGizmos: cyan sphere for radius, red line to target

        [SerializeField] private float detectionRange = 5f;
        [SerializeField] private LayerMask enemyLayer;
        
        public ITargetable CurrentTarget {get; private set;}
        public bool HasTarget => CurrentTarget is { IsAlive: true };

        private void ScanForTargets()
        {
            CurrentTarget = null;
            
            var hits = Physics.OverlapSphere(transform.position, detectionRange, enemyLayer);
            var closestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent(out ITargetable target)) continue;
                var distance = Vector3.Distance(transform.position, hit.transform.position);
                if (!(distance < closestDistance)) continue;
                closestDistance = distance;
                CurrentTarget = target;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            if (!HasTarget) return;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, CurrentTarget.Position);
        }
        
        public void Tick(float deltaTime) => ScanForTargets();
        private void OnEnable() => GameUpdateManager.Instance.Register(this, UpdatePriority.Medium);
        private void OnDisable() => GameUpdateManager.Instance.Unregister(this);

        // TODO: Episode 13 — Swap inline sort for ITargetingStrategy
    }
}