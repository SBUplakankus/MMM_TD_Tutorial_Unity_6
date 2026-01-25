using UnityEngine;

namespace Interfaces
{
    public interface ITargetable
    {
        public Vector3 Position { get; }
        public bool IsAlive { get; }
    }
}