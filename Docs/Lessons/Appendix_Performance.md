# Appendix: Performance & Polish

> Bonus material — not a full episode. Three techniques to tighten up your tower defence project.

---

## Section 1: Update Manager

### Why

Unity calls `Update()` on every active `MonoBehaviour` every frame. For a handful of objects this is fine. For hundreds of enemies, towers, and projectiles, the per-MonoBehaviour update dispatch overhead adds up — even when most objects don't need per-frame logic.

### What

A single `MonoBehaviour` manages all updates. Objects implement `IUpdatable` and register/unregister with the `UpdateManager`.

```csharp
public interface IUpdatable
{
    void Tick(float deltaTime);
}

public enum UpdatePriority
{
    High,    // Every frame — movement, aiming, projectile flight
    Medium,   // ~0.15s interval — tower scanning, cooldown ticks
    Low       // ~0.4s interval — path recalculation, expensive queries
}
```

```csharp
public class UpdateManager : MonoBehaviour
{
    private List<IUpdatable> _highPriority;
    private List<IUpdatable> _mediumPriority;
    private List<IUpdatable> _lowPriority;

    private float _mediumTimer;
    private float _lowTimer;

    // TODO: Register(IUpdatable, UpdatePriority) — add to correct list
    // TODO: Unregister(IUpdatable) — remove from all lists

    private void Update()
    {
        float dt = Time.deltaTime;

        // TODO: Tick all _highPriority every frame
        // TODO: Accumulate _mediumTimer, tick _mediumPriority when >= 0.15s, reset
        // TODO: Accumulate _lowTimer, tick _lowPriority when >= 0.4s, reset
    }
}
```

**Registration pattern in managed objects:**

```csharp
private void OnEnable()
{
    // TODO: UpdateManager.Instance.Register(this, UpdatePriority.High);
}

private void OnDisable()
{
    // TODO: UpdateManager.Instance.Unregister(this);
}
```

!!! tip "Priority tiers are guidelines" Not every object fits neatly into a tier. A tower's aiming logic might be High, but its scanning logic might be Medium. If an object needs multiple tick rates, split it into two `IUpdatable` implementations or use internal timers.

See [Update Manager](../Concepts/Update_Manager.md) for the full concept breakdown.

---

## Section 2: Game Constants

### Why

Magic strings and magic numbers spread through codebases like weeds. A USS class name typo in UITK silently breaks styling. A wave interval value duplicated across three scripts diverges after one edit. Constants centralise these values and make them grep-able.

### What

A static class with `const` values for rarely-changing configuration:

```csharp
public static class GameConstants
{
    public static class Gameplay
    {
        public const int StartingGold = 100;
        public const int StartingHealth = 20;
        public const float WaveInterval = 5f;
        public const float SellRefundRatio = 0.7f;
    }

    public static class UIToolkitStyles
    {
        public const string HealthBar = "health-bar";
        public const string GoldText = "gold-text";
        public const string WaveText = "wave-text";
        public const string TowerShopPanel = "tower-shop-panel";
        public const string TowerInfoPanel = "tower-info-panel";
        public const string SelectedTowerName = "selected-tower-name";
        public const string SelectedTowerDamage = "selected-tower-damage";
        public const string HideClass = "hidden";
    }

    public static class LocalizationKeys
    {
        public const string TowerNamePrefix = "tower.name.";
        public const string TowerDescPrefix = "tower.desc.";
        public const string WaveAnnouncement = "ui.wave.announcement";
        public const string GameOver = "ui.game.over";
    }
}
```

### When to Use Constants vs. ScriptableObjects

| Use Constants for | Use ScriptableObjects for |
|---|---|
| Values that rarely change | Values that designers tune frequently |
| String keys (USS classes, localisation IDs) | Gameplay tuning (damage, speed, range) |
| Compile-time known values | Runtime-configurable values |
| Cross-system identifiers | Per-instance data |

!!! warning "Constants are NOT for gameplay tuning"
    `StartingGold = 100` is fine as a constant if it's a design decision that rarely changes. But tower damage, enemy speed, and wave budgets should live in SOs so designers can iterate without code changes. Constants are for **identifiers and fixed values**, not balancing knobs.

See [Game Constants](../Concepts/Game_Constants.md) for the full concept breakdown.

---

## Section 3: PrimeTween

### Why

Polish matters. A health bar that slides smoothly, a button that scales on hover, a damage number that floats up and fades — these micro-animations make a game feel responsive and professional. Raw code with `Mathf.Lerp` in `Update` works, but a tweening library handles easing, sequencing, and cleanup for you.

### What

**PrimeTween** is a lightweight, allocation-free tweening package (free on the Unity Asset Store). It provides a clean API for animating properties with easing curves.

**Usage in this project:**

- Enemy health bar shake/slide on damage
- UI button hover press effect (scale)
- Damage numbers floating up and fading out
- Gold counter tick animation

### Keep Tweening Visual-Only

!!! warning "Never drive gameplay logic through tweens"
    Tweens are for visual polish — making things look and feel good. They are **not** for game logic. A projectile's position must be calculated in `Update`/`Tick`, not tweened. A tower's cooldown must be tracked with a timer, not a tween duration. Tweening gameplay values leads to desyncs, impossible-to-debug edge cases, and non-deterministic behaviour.

### Example Concepts

```csharp
// TODO: Button hover — scale tween on pointer enter
// Tween.Scale(buttonTransform, new Vector3(1.05f, 1.05f, 1f), 0.1f, Ease.OutQuad);

// TODO: Button hover end — scale back
// Tween.Scale(buttonTransform, Vector3.one, 0.1f, Ease.OutQuad);

// TODO: Health bar damage shake
// Tween.Shake(healthBarTransform, strength: 5f, duration: 0.2f);

// TODO: Damage number float-up
// Tween.PositionY(damageNumberTransform, endValue, duration: 0.5f, Ease.OutQuad);
// Tween.Alpha(damageNumberElement, 0f, duration: 0.3f, delay: 0.2f);
```

See [Tweening](../Concepts/Tweening.md) for the full concept breakdown.

---

## Key Takeaways

1. **UpdateManager** — centralised update loop with priority tiers reduces per-MonoBehaviour dispatch overhead and gives you control over tick frequency
2. **GameConstants** — eliminate magic strings and numbers with a static constants class; use for identifiers and fixed values, not gameplay tuning
3. **PrimeTween** — polish your game with smooth micro-animations; keep tweening visual-only, never drive gameplay logic through tweens