# Episode 05: Player Economy

## What You're Building

When an enemy dies, gold is awarded. When an enemy reaches the end of the path, lives are deducted. Gold and lives are displayed on-screen. This is the naive version — `EconomyUI` polls values every frame. Episode 10 replaces polling with Event Channels, which is the moment you'll feel why they matter.

## The Singleton Problem — Skipped Intentionally

The common approach here is a `PlayerStats` singleton: `public static PlayerStats Instance`. It works. It's also the first step toward a codebase where every class silently depends on every other class through global state — and you don't find out until something breaks and the call stack spans six unrelated files.

`PlayerStats` is game state, not infrastructure. It doesn't need global access. It needs to be in the right place at the right time. A serialized reference achieves that with zero magic and one field in the Inspector. Episode 9's Service Locator handles infrastructure — pooling, update management, event registries. `PlayerStats` stays a direct reference throughout the course.

## PlayerStats.cs

```csharp
using UnityEngine;

namespace Systems.Game
{
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private int startingGold = 100;
        [SerializeField] private int startingLives = 20;

        public int Gold { get; private set; }
        public int Lives { get; private set; }

        private void Awake()
        {
            Gold = startingGold;
            Lives = startingLives;
        }
        
        public void AddGold(int amount) => Gold += amount;
        public void AddLives(int amount) => Lives += amount;
        public void RemoveGold(int amount) => Gold -= amount;
        public void RemoveLives(int amount) => Lives -= amount;
    }
}
```

No events, no singleton. Just state and methods that modify it. Public getters let anything with a reference read the current values. Episode 10 adds notification — for now, anything that needs to know the current value asks directly.

## EnemyController.cs (updated — economy calls)

```csharp
using Interfaces;
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
        
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private int goldGiven = 10;
        [SerializeField] private int livesTaken = 1;
        
        private int _currentWaypointIndex;
        private float _currentHealth;
        
        public Vector3 Position => transform.position;
        public bool IsAlive => _currentHealth > 0;

        private void Die()
        {
            playerStats.AddGold(goldGiven);
            Destroy(gameObject);
        }

        private void HandleEndReached()
        {
            playerStats.RemoveLives(livesTaken);
            Destroy(gameObject);
        }

        public void TakeDamage(float damage)
        {
            _currentHealth -= damage;
            healthBar.Show();
            healthBar.UpdateValue(Mathf.Clamp01(_currentHealth / startHealth));
            
            if (!(_currentHealth <= 0)) return;
            _currentHealth = 0;
            Die();
        }

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
                HandleEndReached();
                return;
            }
            
            var target = path.GetWaypointPosition(_currentWaypointIndex);
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            transform.LookAt(target);

            if (path.IsAtWaypoint(_currentWaypointIndex, transform.position))
                _currentWaypointIndex++;
        }
    }
}
```

**What changed from Episode 04:**

- Added `playerStats`, `goldGiven`, and `damage` serialized fields
- `TakeDamage` now calls `Die()` instead of inline `Destroy`
- `Die()` calls `playerStats.AddGold(goldGiven)` before destroying
- `OnReachedEnd()` calls `playerStats.SubtractLives(damage)`

**Why a serialized reference instead of a singleton?** The enemy has a slot for `PlayerStats`. You drag the scene object in. The dependency is visible, explicit, and inspectable. When Episode 11 introduces wave spawning, the spawner injects the reference at spawn time — one line, no architectural change required.

## EconomyUI.cs

```csharp
using Systems.Game;
using TMPro;
using UnityEngine;

namespace UI
{
    public class EconomyUI : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text livesText;

        private void Update()
        {
            goldText.text = $"Gold: {playerStats.Gold}";
            livesText.text = $"Lives: {playerStats.Lives}";
        }
    }
}
```

Simple and honest. Reads the current values every frame and writes them to the text. It works. It's also wasteful — updating a text component every frame even when nothing has changed. This is the exact pain Episode 10 fixes with Event Channels: instead of asking "what is the value?" sixty times per second, the UI reacts only when the value actually changes.

## Unity Editor Setup

### 1. Create PlayerStats

1. Create an empty GameObject named "PlayerStats"
2. Add the `PlayerStats` component
3. Set `starting gold = 100`, `starting lives = 20`

### 2. Update Enemy

1. Select your enemy (scene-placed for now)
2. In the `EnemyController` component, drag the `PlayerStats` GameObject into the `player stats` field
3. Set `gold given = 10`, `damage = 1`

> **Note:** Scene-placed enemies wire the reference directly in the Inspector. Episode 11 introduces wave spawning — at that point the spawner injects the reference via `Init()`. The prefab field becomes the injection point, nothing else changes.

### 3. Create Economy UI

1. Create a Canvas (Render Mode: Screen Space — Overlay)
2. Add two TextMeshPro elements: one for gold, one for lives
3. Position them in the top-left corner
4. Add `EconomyUI` to the Canvas GameObject
5. Drag the `PlayerStats` scene object into the `player stats` field
6. Assign both Text references

### 4. Test the Full Loop

1. Press Play
2. Enemy walks, tower shoots, enemy takes damage
3. On death: gold counter increases
4. If enemy reaches path end: lives counter decreases
5. Kill multiple enemies: gold stacks correctly

## What Episode 10 Changes

`EnemyController.Die()` calls `playerStats.AddGold()` directly. If you want death to also play a sound, show a damage number, or update a quest — you edit `Die()` every time. `EconomyUI` polls every frame regardless of whether anything changed.

Episode 10 fixes both. `Die()` raises an event. `PlayerStats` subscribes and reacts. `EconomyUI` subscribes and updates only when values change. `EnemyController` stops knowing `PlayerStats` exists. The serialized reference on `EnemyController` is removed entirely at that point.

## Debugging

| Symptom                                   | Cause                                          | Fix                                                                           |
| ----------------------------------------- | ---------------------------------------------- | ----------------------------------------------------------------------------- |
| `NullReferenceException` on `playerStats` | Reference not assigned in Inspector            | Drag the PlayerStats GameObject into the field                                |
| UI always shows 0                         | `playerStats` reference missing on `EconomyUI` | Assign PlayerStats to EconomyUI in Inspector                                  |
| Enemy doesn't give gold                   | `goldGiven` is 0                               | Set `gold given = 10` on EnemyController                                      |
| Lives don't decrease                      | `OnReachedEnd` not called                      | Verify path `HasWaypoint` returns false at the final waypoint                 |
| Prefab enemy has no PlayerStats reference | Prefab can't hold a scene object reference     | Use scene-placed enemies for Episode 05; Episode 11 handles spawned injection |