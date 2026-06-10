using Data;
using Enemies.Controllers;
using UnityEngine;

namespace Systems.Game
{
    public class EnemySpawner : MonoBehaviour
    {
        // TODO: Episode 11 — Coroutine-based batch spawner, uses pool and EnemyData lookup
        
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private EnemyPath path;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private int spawnCount;
        [SerializeField] private float timeBetweenSpawns;
        
        private int _enemiesSpawned;
        private float _timer;

        private void SpawnEnemy()
        {
            var enemy = Instantiate(enemyPrefab).GetComponent<EnemyController>();
            enemy.Initialize(enemyData, path, playerStats);
            _timer = 0;
        }

        private bool CanSpawn()
        {
            _timer += Time.deltaTime;
            return _timer >= timeBetweenSpawns;
        }

        private void Update()
        {
            if (CanSpawn())
                SpawnEnemy();
        }
    }
}