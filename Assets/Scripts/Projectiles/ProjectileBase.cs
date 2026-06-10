using Enums;
using Interfaces;
using Systems.Managers;
using UnityEngine;

namespace Projectiles
{
    public class ProjectileBase : MonoBehaviour, IUpdateable
    {
        // TODO: Episode 04 — Homing projectile toward ITargetable
        // Launch stores target, Update moves toward it, OnHit deals damage via IDamageable
        // Destroy on hit, destroy on target lost, safety Destroy after maxLifetime
        
        [SerializeField] protected float speed = 20f;
        [SerializeField] protected float damage = 10f;
        [SerializeField] protected float maxLifetime = 5f;
        
        protected ITargetable Target;
        private float _lifetimeTimer; 

        public virtual void Launch(ITargetable target)
        {
            Target = target;
            transform.LookAt(target.Position);
            
            _lifetimeTimer = 0f; 
        }

        protected virtual void OnHit(ITargetable target)
        {
            if(target is IDamageable damageable)
                damageable.TakeDamage(damage);
            
            ObjectPoolManager.Instance.ReturnProjectile(this);
        }
        
        public void Tick(float deltaTime)
        { 
            _lifetimeTimer += deltaTime;
            if (_lifetimeTimer >= maxLifetime || Target is not { IsAlive: true })
            {
                ObjectPoolManager.Instance.ReturnProjectile(this);
                return;
            }

            var step = speed * deltaTime;
            var dist = Vector3.Distance(transform.position, Target.Position);

            if (dist <= step)
            {
                OnHit(Target);
                return;
            }
            
            var dir = (Target.Position - transform.position).normalized;
            transform.Translate(dir * step, Space.World);
        }
        
        private void OnEnable() => GameUpdateManager.Instance.Register(this, UpdatePriority.High);
        private void OnDisable() => GameUpdateManager.Instance.Unregister(this);
        
        // TODO: Episode 08 — Replace Destroy with object pool Return, add IPoolable
        // TODO: Episode 09 — Replace Instance with Services.Get
    }
}