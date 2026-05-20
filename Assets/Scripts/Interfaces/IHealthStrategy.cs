using System;

namespace Interfaces
{
    public interface IHealthStrategy
    {
        // TODO: Initialize — set starting health from config values
        void Initialize();

        // TODO: TakeDamage — apply damage logic, return DamageResult with Died flag
        DamageResult TakeDamage(float amount);

        // TODO: Tick — per-frame logic (regen, etc). Default empty for most implementations.
        void Tick(float deltaTime);

        bool IsAlive { get; }
        float CurrentHealth { get; }
        float MaxHealth { get; }
    }

    // TODO: DamageResult — returned by TakeDamage so EnemyController can check if enemy died
    public readonly struct DamageResult
    {
        // TODO: Died bool, DamageDealt float
        // TODO: Static helper methods: Alive(damageDealt) and Dead(damageDealt)
    }
}