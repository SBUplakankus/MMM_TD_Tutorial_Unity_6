using System.Collections.Generic;
using Core;
using Interfaces;
using UnityEngine;

namespace Towers
{
    public enum TargetPriority
    {
        First,
        Last,
        Strong,
        Close
    }

    public class TowerDetection : MonoBehaviour
    {
        #region Fields

        [SerializeField] private float detectionRadius;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private TargetPriority targetPriority;

        private ITargetingStrategy _targetingStrategy;
        private List<ITargetable> _targetsInRange;

        #endregion

        #region Properties

        public ITargetable CurrentTarget { get; private set; }
        public bool HasTarget => CurrentTarget != null;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            // TODO: Initialize _targetsInRange list
            // TODO: Set _targetingStrategy from TargetPriority enum using TargetingProvider or switch
        }

        #endregion

        #region Public API

        public void ScanForTargets()
        {
            // TODO: OverlapSphereNonAlloc with detectionRadius and enemyLayer
            // TODO: Filter ITargetable, filter IsAlive
            // TODO: Call SelectTarget()
        }

        public void SetTargeting(ITargetingStrategy strategy)
        {
            // TODO: Swap targeting strategy at runtime (BTD6-style)
            _targetingStrategy = strategy;
        }

        public void SetTargeting(TargetPriority priority)
        {
            // TODO: Map enum to ITargetingStrategy instance
        }

        public void ClearTarget()
        {
            // TODO: Set CurrentTarget to null
        }

        #endregion

        #region Private Methods

        private void SelectTarget()
        {
            // TODO: Use _targetingStrategy.GetTarget(_targetsInRange, transform.position)
            // TODO: Assign result to CurrentTarget
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // TODO: Draw detection radius sphere
        }

        #endregion
    }
}