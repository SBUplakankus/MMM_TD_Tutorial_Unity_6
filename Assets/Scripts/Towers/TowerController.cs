
using UnityEngine;

namespace Towers
{
    public class TowerController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private TowerDetection detection;
        [SerializeField] private TowerFiring firing;

        #endregion

        #region Properties

        public bool IsActive { get; private set; }

        #endregion

        #region Lifecycle

        private void Update()
        {
            // TODO: If !IsActive, return
            // TODO: Call detection.ScanForTargets() (respect UpdateManager timing later)
            // TODO: If detection.HasTarget, call firing.TryFire(detection.CurrentTarget)
        }

        #endregion

        #region Public API

        public void Activate()
        {
            // TODO: Set IsActive to true, tower is placed and operational
        }

        public void Deactivate()
        {
            // TODO: Set IsActive to false, tower is sold or being moved
        }

        #endregion
    }
}