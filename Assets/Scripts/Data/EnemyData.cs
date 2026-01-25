using Strategies.Health;
using Strategies.Movement;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/Data/Enemy")]
    public class EnemyData : ScriptableObject
    {
        #region Fields
        
        [Header("Enemy Strategies")]
        [SerializeField] private HealthStrategy health;
        [SerializeField] private MovementStrategy movement;
        
        [Header("Enemy Stats")]
        [SerializeField] private int goldGiven;
        [SerializeField] private int damage;

        #endregion
        
        #region Properties
        
        public HealthStrategy Health => health;
        public MovementStrategy Movement   => movement;
        public int GoldGiven => goldGiven;
        public int Damage => damage;
        
        #endregion
    }
}
