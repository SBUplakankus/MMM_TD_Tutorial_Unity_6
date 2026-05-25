using Enums;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "HealthConfig", menuName = "TD/Health Config")]
    public class HealthConfig : ScriptableObject
    {
        public HealthType type;
        public int startHealth = 100;
        [Range(0,0.99f)] public float armourStrength;
    }
}