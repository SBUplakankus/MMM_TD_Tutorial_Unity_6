using Enemies.Controllers;
using UnityEngine;

namespace Strategies.Movement
{
    public class FlyingPath : MovementStrategy
    {
        [SerializeField] [Range(0, 5)] private float flyingHeight;
        
        public override void Initialize(EnemyController enemy)
        {
            base.Initialize(enemy);
            SetStartPosition(enemy);
        }

        protected override void SetStartPosition(EnemyController enemy)
        {
            var pos = Path.StartPosition;
            pos.y += flyingHeight;
            enemy.transform.position = pos;
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
            target.y += flyingHeight;

            enemy.transform.position = Vector3.MoveTowards(
                enemy.transform.position,
                target,
                moveSpeed * Time.deltaTime
            );

            if (Path.IsAtWaypoint(target, enemy.transform.position))
            {
                enemy.CurrentWayPointIndex++;
            }
        }
    }
}