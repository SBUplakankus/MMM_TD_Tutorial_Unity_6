using Enemies.Controllers;
using UnityEngine;

namespace Strategies.Movement
{
    public class GroundedPath : MovementStrategy
    {
        public override void Initialize(EnemyController enemy)
        {
            base.Initialize(enemy);
            SetStartPosition(enemy);
        }

        protected override void SetStartPosition(EnemyController enemy)
        {
            enemy.transform.position = Path.StartPosition;
        }

        public override void Tick(EnemyController enemy)
        {
            var index = enemy.CurrentWayPointIndex;

            if (!Path.HasWaypoint(index))
            {
                CompleteMovement();
                return;
            }

            var target = Path.GetWaypointPosition(index);

            enemy.transform.position = Vector3.MoveTowards(
                enemy.transform.position,
                target,
                moveSpeed * Time.deltaTime
            );

            if (Path.IsAtWaypoint(index, enemy.transform.position))
            {
                enemy.CurrentWayPointIndex++;
            }
        }
    }
}