# Episode 04: Health & Damage

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden; max-width: 100%; margin: 1.5rem 0;">
  <iframe src="https://www.youtube.com/embed/PLACEHOLDER_EP04" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; border:0;" title="Episode 04" allowfullscreen></iframe>
</div>

## Learning Objectives

- Implement a concrete health strategy (`NormalHealth`)
- Wire up the full `TakeDamage` pipeline from tower → interface → strategy
- Add visual health feedback with `EnemyHealthBar`
- Understand why `DamageResult` matters for game events

## Key Concepts

- [Strategy Pattern](../Concepts/Strategy_Pattern.md)

## What We're Starting With

- `EnemyController` delegates movement to `IMovementStrategy` (via `GroundedPath`)
- `IHealthStrategy` interface exists but has no concrete implementation
- `HealthConfig` SO exists with `MaxHealth`
- `DamageResult` struct exists but isn't used yet
- Enemies walk but can't die

---

## The Naive Version

Before strategies, health was just a field in the controller:

```csharp
namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable
    {
        private float _currentHealth;

        public void TakeDamage(float amount)
        {
            // TODO: _currentHealth -= amount;
            // TODO: if (_currentHealth <= 0) Destroy(gameObject);
        }
    }
}
```

**The problem:** health logic is welded to the controller. Armour? Regeneration? Shields? Each one means editing `EnemyController` — the class grows and grows. We already have the `IHealthStrategy` slot from Episode 03. Time to use it.

---

## The Refactor

We add `NormalHealth` — a plain health strategy with no armour, no regen, just a health pool.

```
Tower → IDamageable.TakeDamage → IHealthStrategy.TakeDamage → DamageResult
```

We also add `EnemyHealthBar` as a **component** on the enemy — not inside the health strategy, not inside the controller.

---

## Code Roadmap

| File | Purpose |
|------|---------|
| `Strategies/Health/NormalHealth.cs` | Basic health strategy — reduces health, returns `DamageResult` |
| `Enemies/Components/EnemyHealthBar.cs` | Visual health bar above enemy |
| `Enemies/Controllers/EnemyController.cs` | Updated — uses `IHealthStrategy`, wires up health bar |

### NormalHealth.cs — skeleton

```csharp
namespace Strategies.Health
{
    public class NormalHealth : IHealthStrategy
    {
        // TODO: float _currentHealth;
        // TODO: float _maxHealth;

        // TODO: Initialise(HealthConfig config) — store max, set current = max
        // TODO: TakeDamage(float amount) → return DamageResult
        // TODO: IsAlive => _currentHealth > 0
        // TODO: CurrentHealth => _currentHealth
        // TODO: MaxHealth => _maxHealth
    }
}
```

### EnemyHealthBar.cs — skeleton

```csharp
namespace Enemies.Components
{
    public class EnemyHealthBar : MonoBehaviour
    {
        // TODO: reference to fill image (Transform scale on X axis)
        // TODO: track max + current health
        // TODO: SetHealth(float current, float max) → update fill scale
    }
}
```

---

## Step-by-Step Implementation

### 1 — Implement NormalHealth

Create `Assets/Scripts/Strategies/Health/NormalHealth.cs`:

```csharp
namespace Strategies.Health
{
    public class NormalHealth : IHealthStrategy
    {
        private float _currentHealth;
        private float _maxHealth;

        public void Initialise(HealthConfig config)
        {
            // TODO: _maxHealth = config.MaxHealth;
            // TODO: _currentHealth = _maxHealth;
        }

        public DamageResult TakeDamage(float amount)
        {
            // TODO: _currentHealth -= amount;
            // TODO: bool killed = _currentHealth <= 0;
            // TODO: if killed, clamp _currentHealth to 0
            // TODO: return DamageResult.Create(amount, _currentHealth, killed);
        }

        public bool IsAlive => _currentHealth > 0;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
    }
}
```

### 2 — Update StrategyFactory to create NormalHealth

In `StrategyFactory.CreateHealth`:

```csharp
public static IHealthStrategy CreateHealth(HealthConfig config)
{
    // TODO: return new NormalHealth();
    // (future: switch on config type for ArmouredHealth, etc.)
}
```

### 3 — Wire up EnemyController.TakeDamage

```csharp
// Inside EnemyController — IDamageable implementation

public void TakeDamage(float amount)
{
    // TODO: var result = _health.TakeDamage(amount);
    // TODO: if (result.WasKilled) handle death (destroy or disable)
}
```

### 4 — Add EnemyHealthBar

Create `Assets/Scripts/Enemies/Components/EnemyHealthBar.cs`:

```csharp
namespace Enemies.Components
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Transform _fillTransform;

        private float _maxHealth;

        public void Initialise(float maxHealth)
        {
            // TODO: _maxHealth = maxHealth;
            // TODO: UpdateFill(maxHealth); // start full
        }

        public void UpdateFill(float currentHealth)
        {
            // TODO: float ratio = currentHealth / _maxHealth;
            // TODO: _fillTransform.localScale = new Vector3(ratio, 1f, 1f);
        }
    }
}
```

> The health bar is a UI element parented to the enemy. `Initialise` is called from `EnemyController.Start` after the health strategy is set up.

### 5 — Connect the health bar in EnemyController

```csharp
// Inside EnemyController

[SerializeField] private EnemyHealthBar _healthBar;

private void Start()
{
    // ... strategy creation from Episode 03 ...

    // TODO: _healthBar.Initialise(_health.MaxHealth);
}

public void TakeDamage(float amount)
{
    // TODO: var result = _health.TakeDamage(amount);
    // TODO: _healthBar.UpdateFill(result.RemainingHealth);
    // TODO: if (result.WasKilled) HandleDeath();
}

private void HandleDeath()
{
    // TODO: destroy or disable the enemy
}
```

### 6 — Test: click to damage

Add a temporary test so you can verify health and the bar:

```csharp
// In EnemyController.Update, TEMPORARY:
// TODO: if Input.GetKeyDown(KeyCode.Space), TakeDamage(25f);
```

Press Play, hit Space 4 times — the health bar should shrink and the enemy should die on the 4th hit.

**Remove the test input before moving on.**

---

## Episode Recap

- **Naive**: hardcoded `_currentHealth` field in `EnemyController` — no room for armour, shields, regen
- **Refactor**: `NormalHealth` strategy encapsulates health logic — `EnemyController` never touches health data directly
- `DamageResult` tells the caller *what happened*: damage dealt, remaining health, was it killed
- `EnemyHealthBar` is a **component**, not embedded in the strategy — it observes health changes
- The full pipeline: caller → `IDamageable.TakeDamage` → `IHealthStrategy.TakeDamage` → `DamageResult`

Right now we only have `NormalHealth`. Episode 05 adds `ArmouredHealth` and `FlyingPath` — and shows how Strategy + Composition means **zero changes to EnemyController**.

## Challenge

Display the numeric health value alongside the bar. Add a `TextMeshPro` element above the enemy that shows `CurrentHealth / MaxHealth` and update it in `EnemyHealthBar.UpdateFill`.

<details>
<summary>Hint</summary>

Add a `[SerializeField] private TMP_Text _healthText;` field to `EnemyHealthBar`. In `UpdateFill`, set `_healthText.text = $"{currentHealth:0}/{_maxHealth:0}";`. Remember to add `using TMPro;` and import the TextMeshPro package if you haven't already.

</details>