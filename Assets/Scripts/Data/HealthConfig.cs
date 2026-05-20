using UnityEngine;

namespace Data
{
    // TODO: Enum for health strategy types — drives StrategyFactory switch
    public enum HealthType
    {
        Normal,
        Armoured,
        Shield,
        Regen
    }

    [CreateAssetMenu(fileName = "HealthConfig", menuName = "Scriptable Objects/Config/Health")]
    public class HealthConfig : ScriptableObject
    {
        // TODO: type (HealthType enum) — which strategy to create
        // TODO: startHealth (int)
        // TODO: armourPercent (float, Range 0-0.99, Armoured only)
        // TODO: shieldPoints (int, Shield only)
        // TODO: regenRate (float, Regen only)
        // TODO: regenDelay (float, Regen only)
        // TODO: Properties for each field
    }
}