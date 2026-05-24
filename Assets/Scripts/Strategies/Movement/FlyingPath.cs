using System;
using Enemies.Controllers;
using Interfaces;
using Systems.Game;
using UnityEngine;

namespace Strategies.Movement
{
    public class FlyingPath : IMovementStrategy
    {
        // TODO: Episode 06 — Flying movement: follow waypoints with Y offset (flyingHeight)
        
        private readonly float _moveSpeed;
        private readonly float _flyingHeight;
        private EnemyPath _path;

        public FlyingPath(float moveSpeed, float flyingHeight)
        {
            _moveSpeed = moveSpeed;
            _flyingHeight = flyingHeight;
        }

        public void Init(EnemyController enemy)
        {
            _path = enemy.Path;
            enemy.CurrentWaypointIndex = 0;
            enemy.transform.position = _path.StartPosition + Vector3.up * _flyingHeight;
        }

        public bool Tick(EnemyController enemy)
        {
            if (!_path) return false;

            var index = enemy.CurrentWaypointIndex;

            if (!_path.HasWaypoint(index)) return true;

            var target = _path.GetWaypointPosition(index) + Vector3.up * _flyingHeight;
            enemy.transform.position = Vector3.MoveTowards(
                enemy.transform.position, target, _moveSpeed * Time.deltaTime);
            enemy.transform.LookAt(target);

            if (_path.IsAtWaypoint(index, enemy.transform.position - Vector3.up * _flyingHeight))
                enemy.CurrentWaypointIndex++;

            return false;
        }
    }
}