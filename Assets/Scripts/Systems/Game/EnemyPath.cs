using UnityEngine;

namespace Systems.Game
{
    public class EnemyPath : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;

        private const float WaypointReachedDistance = 0.1f;

        public int WaypointCount => waypoints.Length;
        public Vector3 StartPosition => waypoints[0].position;
        public Vector3 EndPosition => waypoints[^1].position;

        public Vector3 GetWaypointPosition(int index)
        {
            return index >= 0 && index < waypoints.Length
                ? waypoints[index].position
                : EndPosition;
        }

        public bool HasWaypoint(int index)
        {
            return index >= 0 && index < waypoints.Length;
        }

        public bool IsAtWaypoint(int index, Vector3 currentPosition)
        {
            if (index < 0 || index >= waypoints.Length) return false;

            var sqrDist = (currentPosition - waypoints[index].position).sqrMagnitude;
            return sqrDist <= WaypointReachedDistance * WaypointReachedDistance;
        }
    }
}