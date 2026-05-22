# Episode 06: Targeting System

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden; max-width: 100%; margin: 1.5rem 0;">
  <iframe src="https://www.youtube.com/embed/PLACEHOLDER_EP06" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; border: 0;" title="Episode 06" allowfullscreen></iframe>
</div>

## Learning Objectives

- Recognise the problem with a hardcoded targeting sort
- Extract targeting logic into `ITargetingStrategy`
- Implement four targeting strategies: First, Last, Strong, Close
- Use `PathProgress` from `ITargetable` to enable First/Last targeting

## Key Concepts

- [Strategy Pattern](../Concepts/Strategy_Pattern.md)

## What We're Starting With

- Enemies walk the path with swappable `IMovementStrategy` and `IHealthStrategy`
- `ITargetable` exposes `Position`, `IsAlive`, and `PathProgress`
- Towers exist but have no targeting logic yet

---

## The Naive Version

The simplest targeting: sort enemies by distance to the tower, pick the closest.

```csharp
namespace Towers
{
    public class TowerDetection : MonoBehaviour
    {
        // TODO: List<ITargetable> _inRange;

        // TODO: ITargetable FindTarget()
        // {
        //     // Sort by distance to tower
        //     // Return the closest alive enemy
        // }
    }
}
```

**The problem:** "closest enemy" is only *one* targeting priority. BTD6 offers First (furthest along path), Last (last enemy to enter), Strong (most HP), and Close (nearest). Hardcoding any one of these means we'd need to rewrite `TowerDetection` for each priority.

Sound familiar? It's the same copy-paste explosion we solved in Episode 03 with the Strategy Pattern.

---

## The Refactor

Extract `ITargetingStrategy` so each tower can use a different priority:

| Strategy | Selects the enemy that is... |
|----------|------------------------------|
| `FirstTargeting` | Furthest along the path (`PathProgress` highest) |
| `LastTargeting` | Least far along the path (`PathProgress` lowest) |
| `StrongTargeting` | Has the most `CurrentHealth` |
| `CloseTargeting` | Nearest to the tower (distance) |

We also add a `TargetPriority` enum so the UI can display options.

---

## Code Roadmap

| File | Purpose |
|------|---------|
| `Interfaces/ITargetingStrategy.cs` | Contract for selecting a target from a list |
| `Data/TargetPriority.cs` | Enum: First, Last, Strong, Close |
| `Strategies/Targeting/FirstTargeting.cs` | Picks highest `PathProgress` |
| `Strategies/Targeting/LastTargeting.cs` | Picks lowest `PathProgress` |
| `Strategies/Targeting/StrongTargeting.cs` | Picks highest `CurrentHealth` |
| `Strategies/Targeting/CloseTargeting.cs` | Picks nearest by distance |
| `Towers/TowerDetection.cs` | Holds current strategy, finds targets each frame |

### ITargetingStrategy.cs — skeleton

```csharp
namespace Interfaces
{
    public interface ITargetingStrategy
    {
        // TODO: ITargetable SelectTarget(List<ITargetable> targets, Vector3 towerPosition);
    }
}
```

### TowerDetection.cs — skeleton

```csharp
namespace Towers
{
    public class TowerDetection : MonoBehaviour
    {
        // TODO: ITargetingStrategy _targeting;
        // TODO: float _range;
        // TODO: List<ITargetable> _inRange;

        // TODO: assign targeting strategy based on TargetPriority
        // TODO: FindTarget() → filter alive + in range → _targeting.SelectTarget(...)
    }
}
```

---

## Step-by-Step Implementation

### 1 — Create TargetPriority enum

Create `Assets/Scripts/Data/TargetPriority.cs`:

```csharp
namespace Data
{
    public enum TargetPriority
    {
        First,
        Last,
        Strong,
        Close
    }
}
```

### 2 — Create ITargetingStrategy

Create `Assets/Scripts/Interfaces/ITargetingStrategy.cs`:

```csharp
namespace Interfaces
{
    public interface ITargetingStrategy
    {
        // TODO: ITargetable SelectTarget(List<ITargetable> targets, Vector3 towerPosition);
    }
}
```

> `towerPosition` is needed for `CloseTargeting` to compute distances. The other strategies ignore it.

### 3 — Implement FirstTargeting

Create `Assets/Scripts/Strategies/Targeting/FirstTargeting.cs`:

```csharp
namespace Strategies.Targeting
{
    public class FirstTargeting : ITargetingStrategy
    {
        public ITargetable SelectTarget(List<ITargetable> targets, Vector3 towerPosition)
        {
            // TODO: iterate targets
            // TODO: track highest PathProgress
            // TODO: return the target with highest PathProgress (or null if empty)
        }
    }
}
```

> "First" means the enemy furthest along the path — the one closest to the exit. `PathProgress` makes this trivial.

### 4 — Implement LastTargeting

Create `Assets/Scripts/Strategies/Targeting/LastTargeting.cs`:

```csharp
namespace Strategies.Targeting
{
    public class LastTargeting : ITargetingStrategy
    {
        public ITargetable SelectTarget(List<ITargetable> targets, Vector3 towerPosition)
        {
            // TODO: iterate targets
            // TODO: track lowest PathProgress
            // TODO: return the target with lowest PathProgress (or null if empty)
        }
    }
}
```

> "Last" is the enemy that entered most recently — lowest progress.

### 5 — Implement StrongTargeting

Create `Assets/Scripts/Strategies/Targeting/StrongTargeting.cs`:

```csharp
namespace Strategies.Targeting
{
    public class StrongTargeting : ITargetingStrategy
    {
        public ITargetable SelectTarget(List<ITargetable> targets, Vector3 towerPosition)
        {
            // TODO: iterate targets
            // TODO: find the one with highest CurrentHealth
            // TODO: if tied, pick the one with higher PathProgress
            // TODO: return strongest (or null)
        }
    }
}
```

> Note: `ITargetable` doesn't expose `CurrentHealth` yet. But `IDamageable` does through `IHealthStrategy`. We'll need to add `MaxHealth` to `ITargetable` or cast. For now, we check through `IHealthStrategy.CurrentHealth` — the targeting strategy can receive targets that also implement `IDamageable`.

### 6 — Implement CloseTargeting

Create `Assets/Scripts/Strategies/Targeting/CloseTargeting.cs`:

```csharp
namespace Strategies.Targeting
{
    public class CloseTargeting : ITargetingStrategy
    {
        public ITargetable SelectTarget(List<ITargetable> targets, Vector3 towerPosition)
        {
            // TODO: iterate targets
            // TODO: compute Vector3.Distance(t.Position, towerPosition)
            // TODO: track minimum distance
            // TODO: return nearest (or null)
        }
    }
}
```

### 7 — Update TowerDetection to use strategies

```csharp
namespace Towers
{
    public class TowerDetection : MonoBehaviour
    {
        [SerializeField] private float _range = 5f;
        [SerializeField] private TargetPriority _priority = TargetPriority.First;

        private ITargetingStrategy _targeting;
        private List<ITargetable> _inRange;

        private void Start()
        {
            // TODO: switch on _priority to assign _targeting
            //   — First → new FirstTargeting()
            //   — Last → new LastTargeting()
            //   — Strong → new StrongTargeting()
            //   — Close → new CloseTargeting()
        }

        public ITargetable FindTarget()
        {
            // TODO: filter _inRange for IsAlive and within _range
            // TODO: return _targeting.SelectTarget(filtered, transform.position)
        }

        // TODO: OnTriggerEnter/Exit to add/remove from _inRange
    }
}
```

### 8 — Add StrongTargeting support

`StrongTargeting` needs access to `CurrentHealth`. We need `ITargetable` to expose it. Add to `ITargetable`:

```csharp
namespace Interfaces
{
    public interface ITargetable
    {
        Vector3 Position { get; }
        bool IsAlive { get; }
        float PathProgress { get; }
        float CurrentHealth { get; }  // NEW — needed for StrongTargeting
    }
}
```

And implement in `EnemyController`:

```csharp
public float CurrentHealth => _health.CurrentHealth;
```

### 9 — Test targeting priorities

1. Place a tower with `TargetPriority.First` — it should target the enemy closest to the exit
2. Change to `TargetPriority.Close` — it should target the nearest enemy
3. Use the click-to-damage test to weaken one enemy, set `TargetPriority.Strong` — it should target the healthiest

> We're not shooting yet (that's Episode 07). For now, use `Debug.DrawLine` from the tower to `FindTarget().Position` to visualise which enemy is selected.

---

## Episode Recap

- **Naive**: hardcoded "find nearest enemy" in `TowerDetection` — can't swap priorities
- **Refactor**: `ITargetingStrategy` lets each tower choose First, Last, Strong, or Close
- `PathProgress` (from `ITargetable`) is the key to First/Last targeting — it's why we added it back in Episode 01
- `CurrentHealth` was added to `ITargetable` to support `StrongTargeting`
- Four small strategy classes, each ~15 lines — easily testable, easily swappable

Episode 07 gives towers the ability to shoot. We introduce projectiles — another place where the Strategy Pattern will shine.

## Challenge

Add a `MostProgressTargeting` strategy that picks the enemy with the highest `PathProgress` among those within range. This is subtly different from `FirstTargeting` because it filters by range first, then picks. How does this differ from what `FirstTargeting` already does? (Hint: it might not — but think about edge cases where range filtering changes the result.)

<details>
<summary>Hint</summary>

`FirstTargeting` receives a pre-filtered list from `TowerDetection.FindTarget()`. The range filtering happens *before* the strategy sees the list. So `FirstTargeting` already only considers enemies within range. In practice, `MostProgressTargeting` and `FirstTargeting` behave the same. The real differentiation comes when you add tie-breaking rules — e.g., if two enemies have the same `PathProgress`, which one wins?

</details>