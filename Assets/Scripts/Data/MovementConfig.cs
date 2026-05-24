using Enums;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "MovementConfig", menuName = "TD/Movement Config")]
    public class MovementConfig : ScriptableObject
    {
        // TODO: Episode 06 — SO fields: MovementType type, float moveSpeed, float flyingHeight
        
        public MovementType type;
        public float moveSpeed = 5f;
        [Range(0f, 1f)] public float flyingHeight;
    }
}