using UnityEngine;

namespace Data
{
    public enum HealthType
    {
        Normal,
        Armoured
        // TODO: Episode 12 — Add Shield, Regen
    }

    [CreateAssetMenu(fileName = "HealthConfig", menuName = "TD/Health Config")]
    public class HealthConfig : ScriptableObject
    {
        // TODO: Episode 07 — SO fields: HealthType type, int startHealth, float armourPercent
        // Properties for each field
        // TODO: Episode 12 — Add float shieldAmount, float regenRate
    }
}