using System.Collections.Generic;
using Enemies.Controllers;
using Interfaces;
using Projectiles;
using UnityEngine;
using UnityEngine.Pool;

namespace Systems.Managers
{
    public class ObjectPoolManager : MonoBehaviour
    {
        // TODO: Episode 08 — Unity.Pool ObjectPool keyed by string, pre-warm in Awake
        // Get(key, pos, rot), Return(key, obj) calls IPoolable.Reset, ReturnDelayed
        // Instance singleton for now
        // TODO: Episode 09 — Remove Instance, access via Services.Get
        
        [Header("Prefabs")]
        [SerializeField] private EnemyController enemyPrefab;
        [SerializeField] private ProjectileBase projectilePrefab;

        [Header("Pool Settings")]
        [SerializeField] private int defaultCapacity = 200;
        [SerializeField] private int prewarmCount = 100;
        [SerializeField] private int maxSize = 1000;

        private Transform _enemyRoot;
        private Transform _projectileRoot;

        private ObjectPool<EnemyController> _enemyPool;
        private ObjectPool<ProjectileBase> _projectilePool;
        
        private ObjectPool<T> CreatePreWarmedPool<T>(T prefab, Transform root) where T : Component
        {
            var pool = new ObjectPool<T>(
                createFunc: () =>
                {
                    var obj = Instantiate(prefab, root);
                    obj.gameObject.SetActive(false);
                    return obj;
                },
                actionOnGet: c => c.gameObject.SetActive(true),
                actionOnRelease: c =>
                {
                    c.transform.SetParent(root);
                    c.gameObject.SetActive(false);
                },
                actionOnDestroy: c => Destroy(c.gameObject),
                collectionCheck: Application.isEditor,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
            
            var prewarmStorage = new List<T>(prewarmCount);
            
            for(int i = 0; i < prewarmCount; i++)
                prewarmStorage.Add(pool.Get());
            
            for(int i = 0; i < prewarmCount; i++)
                pool.Release(prewarmStorage[i]);
            
            return pool;
        }

        private void InitPools()
        {
            _enemyRoot = CreateRoot("Enemies");
            _projectileRoot = CreateRoot("Projectiles");
            
            _enemyPool = CreatePreWarmedPool(enemyPrefab, _enemyRoot);
            _projectilePool = CreatePreWarmedPool(projectilePrefab, _projectileRoot);
        }

        private Transform CreateRoot(string rootName)
        {
            var go = new GameObject(rootName);
            go.transform.SetParent(transform);
            return go.transform;
        }

        /// <summary>
        /// Retrieves an enemy from the pool at the given position.
        /// </summary>
        public EnemyController GetEnemy(Vector3 position, Quaternion rotation)
        {
            var enemy = _enemyPool.Get();
            enemy.transform.SetPositionAndRotation(position, rotation);
            return enemy;
        }

        /// <summary>
        /// Returns an enemy to its pool.
        /// </summary>
        public void ReturnEnemy(EnemyController enemy) => _enemyPool.Release(enemy);

        /// <summary>
        /// Retrieves a projectile from the pool at the given position.
        /// </summary>
        public ProjectileBase GetProjectile(Vector3 position, Quaternion rotation)
        {
            var projectile = _projectilePool.Get();
            projectile.transform.SetPositionAndRotation(position, rotation);
            return projectile;
        }

        /// <summary>
        /// Returns a projectile to its pool.
        /// </summary>
        public void ReturnProjectile(ProjectileBase projectile) => _projectilePool.Release(projectile);

        private void Awake()
        {
            InitPools();
        }
    }
}