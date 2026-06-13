using System.Collections.Generic;
using Core;
using Enums;
using Factories;
using Interfaces;
using Strategies.Targeting;
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
        [SerializeField] private TargetingType targetingType;

        public ITargetable CurrentTarget { get; private set; }
        public bool HasTarget => CurrentTarget is { IsAlive: true };

        private ITargetingStrategy _targeting;

        private void Awake() => _targeting = StrategyFactory.CreateTargeting(targetingType);

        private void ScanForTargets()
        {
            var hits = Physics.OverlapSphere(transform.position, detectionRange, enemyLayer);
            var candidates = new List<ITargetable>(hits.Length);

            foreach (var hit in hits)
                if (hit.TryGetComponent(out ITargetable target) && target.IsAlive)
                    candidates.Add(target);

            CurrentTarget = candidates.Count > 0
                ? _targeting.GetTarget(candidates, transform.position)
                : null;
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
        private void OnEnable() => Services.Get<GameUpdateManager>().Register(this, UpdatePriority.Medium);
        private void OnDisable() => Services.Get<GameUpdateManager>().Unregister(this);

        // TODO: Episode 13 — Swap inline sort for ITargetingStrategy
    }
}
