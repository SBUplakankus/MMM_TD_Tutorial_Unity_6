using Structs;

namespace Interfaces
{
    public interface IHealthStrategy
    {
        // TODO: Episode 07 — Health strategy contract
        void Init();
        DamageResult TakeDamage(float amount);
        void Tick(float deltaTime);
        bool IsAlive { get; }
        float CurrentHealth { get; }
        float MaxHealth { get; }
    }

    // TODO: Episode 07 — DamageResult readonly struct
    // bool Died, float DamageDealt
    // Static factories: Alive(damageDealt), Dead(damageDealt)
}