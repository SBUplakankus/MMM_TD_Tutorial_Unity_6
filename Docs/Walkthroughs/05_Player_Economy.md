# Episode 05: Player Economy

## What You're Building

When an enemy dies, gold is awarded. When an enemy reaches the end of the path, lives are deducted. Gold and lives are displayed on-screen. This demonstrates the singleton as the simplest access pattern — Episode 9 replaces it with Service Locator.

## PlayerStats.cs

For now, a simple MonoBehaviour singleton. Episode 9 replaces this with a Service Locator.

```csharp
using UnityEngine;
using System;

namespace Systems.Game
{
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        public int Gold { get; private set; }
        public int Lives { get; private set; }

        public event Action<int> OnGoldChanged;
        public event Action<int> OnLivesChanged;

        [SerializeField] private int startingGold = 100;
        [SerializeField] private int startingLives = 20;

        private void Awake()
        {
            Instance = this;
            Gold = startingGold;
            Lives = startingLives;
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        public bool RemoveGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            return true;
        }

        public void SubtractLives(int amount)
        {
            Lives -= amount;
            OnLivesChanged?.Invoke(Lives);
        }
    }
}
```

**Why C# events (`OnGoldChanged`)?** The UI needs to know when gold changes. Polling in Update is wasteful. A simple C# event lets the UI subscribe and update only when the value changes. Episode 10 replaces these with Event Channels — the same idea, just centralized.

## EnemyController.cs (updated — economy calls)

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
        [SerializeField] private EnemyHealthBar healthBar;
        [SerializeField] private int goldGiven = 10;
        [SerializeField] private int damage = 1;

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
                OnReachedEnd();
                return;
            }

            Vector3 target = path.GetWaypointPosition(_currentWaypointIndex);
            transform.position = Vector3.MoveTowards(
                transform.position, target, moveSpeed * Time.deltaTime);

            if (path.IsAtWaypoint(_currentWaypointIndex, transform.position))
            {
                _currentWaypointIndex++;
            }

            if (healthBar != null)
            {
                healthBar.SetHealth(_currentHealth, startHealth);
                healthBar.SetPosition(transform.position);
            }
        }

        // IDamageable
        public void TakeDamage(float damage)
        {
            _currentHealth -= damage;
            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
        }

        private void Die()
        {
            PlayerStats.Instance.AddGold(goldGiven);
            Destroy(gameObject);
        }

        private void OnReachedEnd()
        {
            PlayerStats.Instance.SubtractLives(damage);
            Destroy(gameObject);
        }
    }
}
```

**What changed from Episode 02:**
- Added `goldGiven` and `damage` serialized fields
- `TakeDamage` now calls `Die()` method instead of inline `Destroy`
- `Die()` calls `PlayerStats.Instance.AddGold(goldGiven)` before destroying
- Path-end now calls `OnReachedEnd()` which calls `PlayerStats.Instance.SubtractLives(damage)`

## UI: Gold and Lives Display

Create a screen-space Canvas with two Text elements:

```csharp
using Systems.Game;
using TMPro;
using UnityEngine;

namespace UI
{
    public class EconomyUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text livesText;

        private void Start()
        {
            UpdateGoldDisplay(PlayerStats.Instance.Gold);
            UpdateLivesDisplay(PlayerStats.Instance.Lives);

            PlayerStats.Instance.OnGoldChanged += UpdateGoldDisplay;
            PlayerStats.Instance.OnLivesChanged += UpdateLivesDisplay;
        }

        private void OnDestroy()
        {
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.OnGoldChanged -= UpdateGoldDisplay;
                PlayerStats.Instance.OnLivesChanged -= UpdateLivesChanged;
            }
        }

        private void UpdateGoldDisplay(int gold)
        {
            goldText.text = $"Gold: {gold}";
        }

        private void UpdateLivesDisplay(int lives)
        {
            livesText.text = $"Lives: {lives}";
        }
    }
}
```

Subscribes to `PlayerStats` events in `Start`, unsubscribes in `OnDestroy`. Gold and lives text update immediately when values change.

## Unity Editor Setup

### 1. Create PlayerStats

1. Create empty GameObject named "PlayerStats"
2. Add `PlayerStats` component
3. Set `starting gold = 100`, `starting lives = 20`

### 2. Create Economy UI

1. Create a Canvas (Render Mode: Screen Space - Overlay)
2. Add two TextMeshPro elements: "Gold: 100" and "Lives: 20"
3. Position them in the top-left corner
4. Add `EconomyUI` script to the Canvas
5. Assign the Text references in Inspector

### 3. Test the Full Loop

1. Press Play
2. Enemy walks, tower shoots, enemy dies
3. Gold increases by `goldGiven` per kill (check top-left counter)
4. If enemy reaches the end: lives decrease
5. Multiple enemies: gold stacks, lives stack

## Debugging

| Symptom | Cause | Fix |
|---------|-------|-----|
| `NullReferenceException` on `PlayerStats.Instance` | PlayerStats not in scene | Create the PlayerStats GameObject |
| Gold doesn't update on UI | Events not subscribed | Check EconomyUI has Text references, PlayerStats exists |
| UI text stays at 0 | `startingGold` / `startingLives` not set | Set values in Inspector |
| Enemy doesn't give gold | `goldGiven` is 0 | Set `goldGiven = 10` on EnemyController |
| Lives don't decrease on path end | `OnReachedEnd` not called | Verify path `HasWaypoint` returns false at the last waypoint |