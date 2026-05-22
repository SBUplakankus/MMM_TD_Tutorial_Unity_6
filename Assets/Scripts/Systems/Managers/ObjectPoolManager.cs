using System.Collections.Generic;
using Interfaces;
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