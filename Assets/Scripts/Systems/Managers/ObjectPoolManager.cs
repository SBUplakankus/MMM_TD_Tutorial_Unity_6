using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Systems.Managers
{
    public class ObjectPoolManager : MonoBehaviour
    {
        #region Fields

        [SerializeField] private PoolConfig[] poolConfigs;

        private Dictionary<string, ObjectPool<GameObject>> _pools;
        private Dictionary<string, Transform> _poolContainers;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            // TODO: Initialize _pools and _poolContainers dictionaries
            // TODO: Register self in Services: Services.Register<ObjectPoolManager>(this)
            // TODO: Call PreWarmPools()
        }

        #endregion

        #region Public API

        public GameObject Get(string key, Vector3 position, Quaternion rotation)
        {
            // TODO: Get pool from _pools by key
            // TODO: Fetch object from pool, set position and rotation
            return null;
        }

        public void Return(string key, GameObject obj)
        {
            // TODO: Get pool from _pools by key
            // TODO: Call IPoolable.Reset() if the object implements IPoolable
            // TODO: Return to pool
        }

        public void ReturnDelayed(string key, GameObject obj, float delay)
        {
            // TODO: Start coroutine to return after delay
        }

        #endregion

        #region Private Methods

        private void PreWarmPools()
        {
            // TODO: Iterate poolConfigs, create pools, pre-warm
        }

        #endregion
    }

    [System.Serializable]
    public class PoolConfig
    {
        public string key;
        public GameObject prefab;
        public int defaultSize = 10;
        public int maxSize = 50;
    }
}