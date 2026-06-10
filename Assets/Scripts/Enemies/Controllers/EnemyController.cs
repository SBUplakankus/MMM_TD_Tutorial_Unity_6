using Data;
using Enemies.Components;
using Enums;
using Factories;
using Interfaces;
using Systems.Game;
using Systems.Managers;
using Systems.Parsing;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable, IUpdateable
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
        public bool IsAlive => _health != null && _health.IsAlive;

        private IHealthStrategy _health;
        private IMovementStrategy _movement;
        private PlayerStats _playerStats;
        private int _goldGiven;
        private int _livesTaken;

        public void Initialize(EnemyData data, EnemyPath path, PlayerStats playerStats)
        {
            Path = path;
            _playerStats = playerStats;
            _goldGiven = data.goldGiven;
            _livesTaken = data.livesTaken;

            _health = StrategyFactory.CreateHealth(data.healthConfig);
            _health.Init();

            _movement = StrategyFactory.CreateMovement(data.movementConfig);
            _movement.Init(this);

            healthBar.Hide();
        }

        public void TakeDamage(float damage)
        {
            var result = _health.TakeDamage(damage);
            healthBar.Show();

            healthBar.UpdateValue(Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth));

            if (result.Died)
                Die();
        }

        private void Die()
        {
            _playerStats.AddGold(_goldGiven);
            ObjectPoolManager.Instance.ReturnEnemy(this);
        }

        private void HandleEndReached()
        {
            _playerStats.RemoveLives(_livesTaken);
            ObjectPoolManager.Instance.ReturnEnemy(this);
        }
        
        public void Tick(float deltaTime)
        { 
            if (!IsAlive) return;

            _health.Tick(Time.deltaTime);

            if (!_movement.Tick(this)) return;
            HandleEndReached();
        }
        
        private void OnEnable() => GameUpdateManager.Instance.Register(this, UpdatePriority.High);
        private void OnDisable() => GameUpdateManager.Instance.Unregister(this);
    }
}