using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "TD/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        // TODO: Episode 07 — Composed enemy data: HealthConfig, MovementConfig, goldGiven, damage
        
        public MovementConfig movementConfig;
        public HealthConfig healthConfig;
        public int goldGiven = 10;
        public int livesTaken = 1;
    }
}