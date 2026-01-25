using UnityEngine;

namespace Systems.Game
{
    public class EnemyPath : MonoBehaviour
    {
        #region Fields

        [SerializeField] private Transform[] waypoints;

        private const float WaypointReachedDistance = 0.1f;

        #endregion

        #region Properties

        public int WaypointCount => waypoints.Length;

        public Vector3 StartPosition => waypoints[0].position;
        public Vector3 EndPosition => waypoints[^1].position;

        #endregion

        #region Public API

        public Vector3 GetWaypointPosition(int index)
        {
            return !IsValidIndex(index) ? EndPosition : waypoints[index].position;
        }

        public bool HasWaypoint(int index)
        {
            return index >= 0 && index < waypoints.Length;
        }

        public bool IsAtWaypoint(int index, Vector3 currentPosition)
        {
            if (!IsValidIndex(index))
                return false;

            var sqrDistance = 
                (currentPosition - waypoints[index].position).sqrMagnitude;

            return sqrDistance <= WaypointReachedDistance * WaypointReachedDistance;
        }
        
        public bool IsAtWaypoint(Vector3 waypoint, Vector3 currentPosition)
        {
            var sqrDistance = (currentPosition - waypoint).sqrMagnitude;
            return sqrDistance <= WaypointReachedDistance * WaypointReachedDistance;
        }

        #endregion

        #region Helpers

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < waypoints.Length;
        }

        #endregion
    }
}