using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/Data/Enemy")]
    public class EnemyData : ScriptableObject
    {
        #region Fields

        [Header("Enemy Config")]
        [SerializeField] private HealthConfig healthConfig;
        [SerializeField] private MovementConfig movementConfig;

        [Header("Enemy Stats")]
        [SerializeField] private int goldGiven;
        [SerializeField] private int damage;

        #endregion

        #region Properties

        public HealthConfig HealthConfig => healthConfig;
        public MovementConfig MovementConfig => movementConfig;
        public int GoldGiven => goldGiven;
        public int Damage => damage;

        #endregion
    }
}