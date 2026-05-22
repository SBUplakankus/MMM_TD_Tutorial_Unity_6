using Interfaces;
using UnityEngine;

namespace Projectiles
{
    public class ProjectileBase : MonoBehaviour
    {
        // TODO: Episode 04 — Homing projectile toward ITargetable
        // Launch stores target, Update moves toward it, OnHit deals damage via IDamageable
        // Destroy on hit, destroy on target lost, safety Destroy after maxLifetime
        // TODO: Episode 08 — Replace Destroy with object pool Return, add IPoolable
        // TODO: Episode 09 — Replace Instance with Services.Get
    }
}