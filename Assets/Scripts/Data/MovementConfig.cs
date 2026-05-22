using UnityEngine;

namespace Data
{
    public enum MovementType
    {
        Grounded,
        Flying
    }

    [CreateAssetMenu(fileName = "MovementConfig", menuName = "TD/Movement Config")]
    public class MovementConfig : ScriptableObject
    {
        // TODO: Episode 06 — SO fields: MovementType type, float moveSpeed, float flyingHeight
        // Properties for each field
    }
}