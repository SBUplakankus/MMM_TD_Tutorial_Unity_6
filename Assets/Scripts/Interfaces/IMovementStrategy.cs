using System;
using Enemies.Controllers;

namespace Interfaces
{
    public interface IMovementStrategy
    {
        // TODO: Episode 06 — Movement strategy contract
        void Init(EnemyController enemy);
        bool Tick(EnemyController enemy);
    }
}