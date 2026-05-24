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

        private void Start()
        {
            var enemy = Instantiate(enemyPrefab).GetComponent<EnemyController>();
            enemy.Initialize(enemyData, path, playerStats);
        }
    }
}