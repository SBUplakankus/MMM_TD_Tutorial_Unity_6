using UnityEngine;

namespace Data
{
    // TODO: Enum for movement strategy types — drives StrategyFactory switch
    public enum MovementType
    {
        Grounded,
        Flying
    }

    [CreateAssetMenu(fileName = "MovementConfig", menuName = "Scriptable Objects/Config/Movement")]
    public class MovementConfig : ScriptableObject
    {
        // TODO: type (MovementType enum) — which strategy to create
        // TODO: moveSpeed (float)
        // TODO: flyingHeight (float, Range 0-5, Flying only)
        // TODO: Properties for each field
    }
}