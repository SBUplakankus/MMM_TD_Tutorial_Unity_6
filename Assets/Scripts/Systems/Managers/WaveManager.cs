using Core;
using Events.Registries;
using Systems.Managers;
using UnityEngine;

namespace Systems.Game
{
    public class WaveManager : MonoBehaviour
    {
        #region Fields

        [SerializeField] private TextAsset waveCsvFile;
        [SerializeField] private EnemySpawner enemySpawner;

        #endregion

        #region Properties

        public int CurrentWave { get; private set; }
        public int TotalWaves { get; private set; }

        #endregion

        #region Lifecycle

        private void Awake()
        {
            // TODO: Parse CSV, group by WaveId, set TotalWaves
            // TODO: Register self in Services: Services.Register<WaveManager>(this)
        }

        #endregion

        #region Public API

        public void StartNextWave()
        {
            // TODO: Increment CurrentWave
            // TODO: If > TotalWaves, raise Services.Get<WaveEvents>().AllWavesCompleted.Raise()
            // TODO: Else pass batch to enemySpawner, raise Services.Get<WaveEvents>().WaveStarted.Raise(CurrentWave)
        }

        public void CheckWaveProgress()
        {
            // TODO: If spawner complete and no enemies alive, raise WaveCompleted
        }

        #endregion
    }
}