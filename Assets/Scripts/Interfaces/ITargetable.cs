using UnityEngine;

namespace Interfaces
{
    public interface ITargetable
    {
        // TODO: Episode 02 — Define the targeting contract
        Vector3 Position { get; }
        bool IsAlive { get; }
        
        // TODO: Episode 13 — Add when building targeting strategies
        float PathProgress { get; }
        float CurrentHealth { get; }
    }
}