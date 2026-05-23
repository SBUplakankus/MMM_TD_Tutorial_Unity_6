using Interfaces;
using UnityEngine;

namespace Projectiles
{
    public class ProjectileBase : MonoBehaviour
    {
        // TODO: Episode 04 — Homing projectile toward ITargetable
        // Launch stores target, Update moves toward it, OnHit deals damage via IDamageable
        // Destroy on hit, destroy on target lost, safety Destroy after maxLifetime
        
        [SerializeField] protected float speed = 20f;
        [SerializeField] protected float damage = 10f;
        [SerializeField] protected float maxLifetime = 5f;
        
        protected ITargetable Target;

        public virtual void Launch(ITargetable target)
        {
            Target = target;
            transform.LookAt(target.Position);
            Destroy(gameObject, maxLifetime);
        }

        protected virtual void OnHit(ITargetable target)
        {
            if(target is IDamageable damageable)
                damageable.TakeDamage(damage);
            
            Destroy(gameObject);
        }

        private void Update()
        {
            if (Target is not { IsAlive: true })
            {
                Destroy(gameObject);
                return;
            }
            
            var step = speed * Time.deltaTime;
            var dist = Vector3.Distance(transform.position, Target.Position);

            if (dist <= step)
            {
                OnHit(Target);
                return;
            }
            
            var dir = (Target.Position - transform.position).normalized;
            transform.Translate(dir * step, Space.World);
        }
        
        // TODO: Episode 08 — Replace Destroy with object pool Return, add IPoolable
        // TODO: Episode 09 — Replace Instance with Services.Get
    }
}