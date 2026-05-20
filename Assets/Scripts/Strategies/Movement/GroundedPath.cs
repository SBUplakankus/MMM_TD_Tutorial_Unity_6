using System;
using Enemies.Controllers;
using Interfaces;
using UnityEngine;

namespace Strategies.Movement
{
    // TODO: Plain class implementing IMovementStrategy — no longer a ScriptableObject
    // Constructor takes (float moveSpeed)
    // Holds: _moveSpeed, _path (EnemyPath reference)
    // Initialize(enemy): store enemy.Path, set waypoint index to 0, set position to path.StartPosition
    // Tick(enemy): move toward current waypoint at _moveSpeed * Time.deltaTime
    //   If no more waypoints, fire OnMovementCompleted
    //   If at waypoint, increment CurrentWayPointIndex
    public class GroundedPath : IMovementStrategy
    {
        // TODO: Implement IMovementStrategy
        public void Initialize(EnemyController enemy)
        {
            throw new NotImplementedException();
        }

        public void Tick(EnemyController enemy)
        {
            throw new NotImplementedException();
        }

        public event Action OnMovementCompleted;
    }
}