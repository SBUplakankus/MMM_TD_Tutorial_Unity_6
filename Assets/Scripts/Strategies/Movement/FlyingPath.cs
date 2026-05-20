using System;
using Enemies.Controllers;
using Interfaces;
using UnityEngine;

namespace Strategies.Movement
{
    // TODO: Plain class implementing IMovementStrategy — no longer a ScriptableObject
    // Constructor takes (float moveSpeed, float flyingHeight)
    // Holds: _moveSpeed, _flyingHeight, _path (EnemyPath reference)
    // Initialize(enemy): same as GroundedPath but Y offset by _flyingHeight
    // Tick(enemy): same as GroundedPath but target Y is waypoint Y + _flyingHeight
    //   Uses IsAtWaypoint(targetPosition, enemyPosition) overload (not index-based)
    public class FlyingPath : IMovementStrategy
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