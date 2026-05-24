using System;
using Enemies.Controllers;
using Interfaces;
using Systems.Game;
using UnityEngine;

namespace Strategies.Movement
{
    public class GroundedPath : IMovementStrategy
    {
        // TODO: Episode 06 — Ground movement: follow EnemyPath waypoints at ground level
        
        private readonly float _moveSpeed;
        private EnemyPath _path;

        public GroundedPath(float moveSpeed) => _moveSpeed = moveSpeed;

        public void Init(EnemyController enemy)
        {
            _path = enemy.Path;
            enemy.CurrentWaypointIndex = 0;
            enemy.transform.position = _path.StartPosition;
        }

        public bool Tick(EnemyController enemy)
        {
            if (!_path) return false;

            var index = enemy.CurrentWaypointIndex;

            if (!_path.HasWaypoint(index)) return true;

            var target = _path.GetWaypointPosition(index);
            enemy.transform.position = Vector3.MoveTowards(
                enemy.transform.position, target, _moveSpeed * Time.deltaTime);
            enemy.transform.LookAt(target);

            if (_path.IsAtWaypoint(index, enemy.transform.position))
                enemy.CurrentWaypointIndex++;

            return false;
        }
    }
}