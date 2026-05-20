using System;
using Enemies.Controllers;

namespace Interfaces
{
    public interface IMovementStrategy
    {
        // TODO: Initialize — set start position, waypoint index, store path reference
        void Initialize(EnemyController enemy);

        // TODO: Tick — move toward next waypoint each frame
        void Tick(EnemyController enemy);

        // TODO: Fired when enemy reaches the last waypoint
        event Action OnMovementCompleted;
    }
}