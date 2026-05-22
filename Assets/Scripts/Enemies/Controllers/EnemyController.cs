using Systems.Game;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour
    {
        // Episode 01: Basic movement along EnemyPath waypoints
        //   [SerializeField] float moveSpeed, EnemyPath path
        //   Start: snap to path start, set waypoint index
        //   Update: MoveTowards waypoint, advance on arrival, Destroy at end
        //
        // Episode 02: Add IDamageable + ITargetable, inline health, health bar
        //   [SerializeField] float startHealth, EnemyHealthBar healthBar
        //   Add _currentHealth, IsAlive, Position, TakeDamage, Die()
        //
        // Episode 05: Player economy calls in Die() and OnReachedEnd()
        //   PlayerStats.Instance.AddGold / SubtractLives
        //
        // Episode 06: Delegate movement to IMovementStrategy
        //   Add Initialize(EnemyData, EnemyPath), Path and CurrentWayPointIndex public props
        //   Remove inline MoveTowards, call Movement.Tick(this)
        //
        // Episode 07: Delegate health to IHealthStrategy
        //   Replace _currentHealth with IHealthStrategy Health property
        //   TakeDamage calls Health.TakeDamage, check DamageResult.Died
        //   Add Health.Tick(Time.deltaTime) in Update
        //
        // Episode 08: Object pooling — add IPoolable, replace Destroy with pool Return
        //   Implement Reset() to clear all runtime state
        //
        // Episode 09: Replace .Instance with Services.Get<T>()
        //
        // Episode 10: Replace direct PlayerStats calls with CombatEvents.Raise()
        //   Cache services in OnEnable (runs on pool reactivation)
        //
        // Episode 13: Add PathProgress and CurrentHealth to ITargetable

        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private EnemyPath path;

        private int _currentWaypointIndex;

        private void Start()
        {
            _currentWaypointIndex = 0;
            transform.position = path.StartPosition;
        }

        private void Update()
        {
            if(!path) return;

            if (!path.HasWaypoint(_currentWaypointIndex))
            {
                Destroy(gameObject);
                return;
            }
            
            var target = path.GetWaypointPosition(_currentWaypointIndex);
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            transform.LookAt(target);

            if (path.IsAtWaypoint(_currentWaypointIndex, transform.position))
                _currentWaypointIndex++;
        }
    }
}