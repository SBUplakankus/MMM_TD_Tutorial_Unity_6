using Data;
using Enemies.Components;
using Interfaces;
using Systems.Game;
using Systems.Parsing;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable
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

        [SerializeField] private EnemyHealthBar healthBar;

        public EnemyPath Path { get; private set; }
        public int CurrentWaypointIndex { get; set; }
        public Vector3 Position => transform.position;
        public bool IsAlive => _currentHealth > 0;

        private IMovementStrategy _movement;
        private PlayerStats _playerStats;
        private float _currentHealth;
        private float _startHealth;
        private int _goldGiven;
        private int _livesTaken;

        public void Initialize(EnemyData data, EnemyPath path, PlayerStats playerStats)
        {
            Path = path;
            _playerStats = playerStats;
            _startHealth = data.startHealth;
            _currentHealth = _startHealth;
            _goldGiven = data.goldGiven;
            _livesTaken = data.livesTaken;

            _movement = StrategyFactory.CreateMovement(data.movementConfig);
            _movement.Init(this);

            healthBar.Hide();
        }

        private void Update()
        {
            if (!IsAlive) return;

            if (_movement.Tick(this))
            {
                HandleEndReached();
                return;
            }
        }

        public void TakeDamage(float damage)
        {
            _currentHealth -= damage;
            healthBar.Show();
            healthBar.UpdateValue(Mathf.Clamp01(_currentHealth / _startHealth));

            if (_currentHealth > 0) return;
            _currentHealth = 0;
            Die();
        }

        private void Die()
        {
            _playerStats.AddGold(_goldGiven);
            Destroy(gameObject);
        }

        private void HandleEndReached()
        {
            _playerStats.RemoveLives(_livesTaken);
            Destroy(gameObject);
        }
    }
}