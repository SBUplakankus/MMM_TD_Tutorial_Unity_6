using Core;
using Events.Registries;
using Systems.Managers;
using UnityEngine;

namespace Systems.Game
{
    public class AudioController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private Data.AudioData enemyDeathAudio;
        [SerializeField] private Data.AudioData towerFireAudio;
        [SerializeField] private Data.AudioData towerPlacedAudio;
        [SerializeField] private Data.AudioData uiClickAudio;

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            // TODO: Subscribe to registry channels via Services.Get<CombatEvents>().EnemyDeath.Subscribe(...)
            // TODO: Subscribe to GameEvents.TowerPlaced
            // TODO: Store handler references for unsubscription
        }

        private void OnDisable()
        {
            // TODO: Unsubscribe from all registry channels
        }

        #endregion

        #region Private Methods

        private void OnEnemyDeath(int goldReward)
        {
            // TODO: Play enemyDeathAudio via AudioPoolHandler from Services.Get<ObjectPoolManager>()
        }

        private void OnTowerPlaced()
        {
            // TODO: Play towerPlacedAudio
        }

        #endregion
    }
}