# Episode 02: Interfaces

## What You're Building

Extract `IDamageable` and `ITargetable` interfaces from `EnemyController`. Add health and a click-to-damage test. Add a health bar using a Unity Slider so damage is visible.

## The Problem

Episode 1's `EnemyController` is a concrete class. If a tower wants to shoot an enemy, it needs to know about `EnemyController` specifically. If a projectile wants to deal damage, same thing. Every system that interacts with enemies is coupled to this one class. When we add other damageable things later (barriers, destructible props), every caller needs updating.

Interfaces solve this: `IDamageable` says "you can take damage" and `ITargetable` says "you can be targeted." Towers and projectiles interact with the interfaces, not the concrete class.

## IDamageable.cs

```csharp
namespace Interfaces
{
    public interface IDamageable
    {
        void TakeDamage(float damage);
        bool IsAlive { get; }
    }
}
```

Two members. `TakeDamage(float)` is the action. `IsAlive` lets callers check before acting — a tower shouldn't waste a projectile on a dead enemy.

## ITargetable.cs

```csharp
using UnityEngine;

namespace Interfaces
{
    public interface ITargetable
    {
        Vector3 Position { get; }
        bool IsAlive { get; }
    }
}
```

Two members. `Position` — a tower needs to know where the enemy is to aim. `IsAlive` — a tower should stop targeting dead enemies.

**Why not just put TakeDamage on ITargetable?** Because "can be targeted" and "can be damaged" are separate concerns. A stealth enemy might be damageable but not targetable. A wall might be targetable but use a different damage system. Interfaces should be small and focused.

## EnemyController.cs (updated)

```csharp
using Systems.Game;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float startHealth = 100f;
        [SerializeField] private EnemyPath path;

        private int _currentWaypointIndex;
        private float _currentHealth;

        // ITargetable
        public Vector3 Position => transform.position;
        public bool IsAlive => _currentHealth > 0;

        private void Start()
        {
            _currentHealth = startHealth;
            _currentWaypointIndex = 0;
            transform.position = path.StartPosition;
        }

        private void Update()
        {
            if (path == null || !IsAlive) return;

            if (!path.HasWaypoint(_currentWaypointIndex))
            {
                Destroy(gameObject);
                return;
            }

            Vector3 target = path.GetWaypointPosition(_currentWaypointIndex);
            transform.position = Vector3.MoveTowards(
                transform.position, target, moveSpeed * Time.deltaTime);

            if (path.IsAtWaypoint(_currentWaypointIndex, transform.position))
            {
                _currentWaypointIndex++;
            }
        }

        // IDamageable
        public void TakeDamage(float damage)
        {
            _currentHealth -= damage;
            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Destroy(gameObject);
            }
        }
    }
}
```

**What changed from Episode 1:**

- Added `IDamageable` and `ITargetable` to the implements list
- Added `startHealth` serialized field, `_currentHealth` private field
- `IsAlive` property reads from `_currentHealth`
- `Position` property returns `transform.position`
- `TakeDamage(float)` subtracts damage, clamps to 0, destroys on death
- `Update()` now checks `!IsAlive` as an early return

## EnemyHealthBar.cs

A health bar on the enemy using a Unity Slider. The Slider goes from 0 to 1, where 1 = full health and 0 = dead.

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Enemies.Components
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Slider healthBar;

        public void UpdateValue(float healthPercent) => healthBar.value = healthPercent;
        public void Hide() => gameObject.SetActive(false);
        public void Show() => gameObject.SetActive(true);
    }
}
```

The health bar is a separate component — not embedded in EnemyController. This keeps EnemyController focused on movement and health logic, and the health bar handles its own display.

## EnemyController.cs (with health bar wiring)

```csharp
using Systems.Game;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable
    {
        [SerializeField] private float moveSpeed = 1f;
        [SerializeField] private float startHealth = 100f;
        [SerializeField] private EnemyPath path;
        [SerializeField] private EnemyHealthBar healthBar;
        
        private int _currentWaypointIndex;
        private float _currentHealth;
        
        public Vector3 Position => transform.position;
        public bool IsAlive => _currentHealth > 0;

        private void Start()
        {
            _currentWaypointIndex = 0;
            _currentHealth = startHealth;
            transform.position = path.StartPosition;
            healthBar.Hide();
        }

        private void Update()
        {
            if(!path || !IsAlive) return;

            if (!path.HasWaypoint(_currentWaypointIndex))
            {
                Destroy(gameObject);
                return;
            }
            
            var target = path.GetWaypointPosition(_currentWaypointIndex);
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            transform.LookAt(target);

            if (path.IsAtWaypoint(_currentWaypointIndex, transform.position))
                _currentWaypointIndex++;
        }

        public void TakeDamage(float damage)
        {
            _currentHealth -= damage;
            healthBar.Show();
            healthBar.UpdateValue(Mathf.Clamp01(_currentHealth / startHealth));
            
            if (!(_currentHealth <= 0)) return;
            _currentHealth = 0;
            Destroy(gameObject);
        }
    }
}
```

The `if (healthBar != null)` check means the health bar is optional — the enemy works without it.

## ClickDamageTest.cs

A test script that lets you click enemies to damage them. Uses `IDamageable`, not `EnemyController`.

```csharp
using Interfaces;
using UnityEngine;

public class ClickDamageTest : MonoBehaviour
{
    [SerializeField] private float damagePerClick = 25f;
        private Camera _camera;

#if UNITY_EDITOR
        private void Awake() => _camera = Camera.main;
        private void Update()
        {
            if (Pointer.current == null) return;
            if (!Pointer.current.press.wasPressedThisFrame) return;
            if (!_camera) return;

            var screenPosition = Pointer.current.position.ReadValue();
            var ray = _camera.ScreenPointToRay(screenPosition);
            
            if (!Physics.Raycast(ray, out var hit)) return;
            if (!hit.collider.TryGetComponent<IDamageable>(out var damageable)) return;
            if (!damageable.IsAlive) return;
            
            damageable.TakeDamage(damagePerClick);
            Debug.Log($"Dealt {damagePerClick} damage to {hit.collider.name}");
        }
#endif
}
```

**Note the interface usage**: `TryGetComponent<IDamageable>` — this script has no reference to `EnemyController`. It works with anything that implements `IDamageable`. This is the payoff of interfaces.

## Unity Editor Setup

### 1. Create the Health Bar

1. Create a Canvas (Render Mode: World Space) as a child of the Enemy
2. Set the Canvas size to something small (Width: 2, Height: 0.3)
3. Add a Slider as a child of the Canvas (Remove the "Handle Slide Area" child — we don't need a drag handle)
4. Set the Slider: Min Value = 0, Max Value = 1, Whole Numbers = off
5. Set the "Fill" image color to green, "Background" to red
6. Add `EnemyHealthBar` script to the Canvas or to a child GameObject
7. Assign the Slider to the `slider` field in Inspector
8. Position the Canvas above the enemy (offset Y around 2)

### 2. Wire EnemyController

1. Add the `EnemyHealthBar` reference to EnemyController's `healthBar` field

### 3. Add ClickDamageTest

1. Create an empty GameObject named "TestRunner"
2. Add `ClickDamageTest` component
3. Set `damage per click = 25`

## Test Plan

1. Press Play — enemy walks, health bar full (green)
2. Click enemy once — health bar decreases slightly, Console logs damage
3. Click 3 more times (4 total x 25 = 100) — health bar empties, enemy is destroyed
4. Verify Console shows 4 damage logs
5. Try clicking where no enemy is — no error, no log

## Debugging

| Symptom                                     | Cause                                           | Fix                                                                           |
| ------------------------------------------- | ----------------------------------------------- | ----------------------------------------------------------------------------- |
| Click does nothing                          | No Collider on enemy, or wrong layer            | Add a Collider (Box or Capsule) to enemy GameObject                           |
| `TryGetComponent<IDamageable>` returns null | EnemyController doesn't implement the interface | Verify `IDamageable` is in the implements list                                |
| Health bar doesn't appear                   | Slider not assigned or Canvas not world-space   | Check EnemyHealthBar slider reference, set Canvas to World Space              |
| Health bar faces wrong way                  | Canvas not set to face camera                   | Add a `LookAt(Camera.main.transform)` in LateUpdate, or use a Billboard setup |
| Enemy dies on first click                   | `startHealth` is 0 or very low                  | Set `startHealth = 100` in Inspector                                          |