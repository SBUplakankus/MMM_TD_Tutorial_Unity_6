using System.Collections;
using System.Collections.Generic;
using Core;
using Data;
using Systems.Managers;
using Systems.Parsing;
using UnityEngine;

namespace Systems.Game
{
    public class EnemySpawner : MonoBehaviour
    {
        #region Fields

        [SerializeField] private Dictionary<string, EnemyData> enemyDataLookup;

        private int _remainingInBatch;

        #endregion

        #region Properties

        public bool IsBatchComplete => _remainingInBatch <= 0;

        #endregion

        #region Public API

        public void StartBatch(IEnumerable<WaveEntry> entries, EnemyPath path)
        {
            // TODO: Calculate total spawn count for _remainingInBatch
            // TODO: For each entry, start Coroutine: SpawnEnemyRoutine(entry, path)
            // NOTE: EnemyPath is now passed as a parameter, not a serialized field
        }

        #endregion

        #region Private Methods

        private IEnumerator SpawnEnemyRoutine(WaveEntry entry, EnemyPath path)
        {
            // TODO: Loop SpawnCount times
            // TODO: Wait SpawnInterval seconds
            // TODO: Fetch enemy from Services.Get<ObjectPoolManager>().Get("enemy", path.StartPosition, Quaternion.identity)
            // TODO: Get EnemyController, call Initialize(enemyDataLookup[entry.EnemyId], path)
            // TODO: Decrement _remainingInBatch
            return null;
        }

        #endregion
    }
}