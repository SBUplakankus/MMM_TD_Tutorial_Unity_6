# Episode 07: Runtime CSV Wave Data — Implementation Guide

## What You're Building

The wave system reads enemy spawn data from a CSV file at runtime, groups it by wave ID, and spawns enemies in timed batches. This episode implements:
- **CsvWaveParser** — static parser that converts `TextAsset` CSV into `List<WaveEntry>` structs
- **WaveManager** — orchestrator that parses on Awake, transitions through wave states, delegates spawning
- **EnemySpawner** — coroutine-based spawner that runs per-entry spawn routines and tracks batch completion

The CSV already exists at `Assets/Data/Waves/wave_data.csv`. The `WaveEntry` struct already exists in `CsvWaveParser.cs`.

Key design: `EnemySpawner` uses a `List<EnemyData>` lookup instead of the `Dictionary<string, EnemyData>` that's in the skeleton — Unity can't serialize `Dictionary` in the inspector, so we use a list with a runtime lookup built in `Awake`.

## Files & Order

| # | File | Action |
|---|------|--------|
| 1 | `Assets/Scripts/Systems/Parsing/CsvWaveParser.cs` | UPDATE — full parsing |
| 2 | `Assets/Scripts/Systems/Managers/WaveManager.cs` | UPDATE — full implementation |
| 3 | `Assets/Scripts/Systems/Managers/EnemyManager.cs` | UPDATE — add enemy tracking |
| 4 | `Assets/Scripts/Systems/Game/EnemySpawner.cs` | UPDATE — full implementation |

## Implementation

### 1. CsvWaveParser.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Parsing
{
    public struct WaveEntry
    {
        public int WaveId;
        public string EnemyId;
        public int SpawnCount;
        public float SpawnInterval;
    }

    public static class CsvWaveParser
    {
        private const string Separator = ",";

        public static List<WaveEntry> Parse(TextAsset csvFile)
        {
            var entries = new List<WaveEntry>();

            if (csvFile == null || string.IsNullOrEmpty(csvFile.text))
            {
                Debug.LogWarning("[CsvWaveParser] CSV file is null or empty.");
                return entries;
            }

            var lines = csvFile.text.Split('\n');

            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim('\r', ' ');
                if (string.IsNullOrEmpty(line)) continue;

                var fields = line.Split(Separator);
                if (fields.Length < 4)
                {
                    Debug.LogWarning($"[CsvWaveParser] Skipping malformed row {i}: expected 4 fields, got {fields.Length}");
                    continue;
                }

                if (!int.TryParse(fields[0], out var waveId))
                {
                    Debug.LogWarning($"[CsvWaveParser] Skipping row {i}: invalid WaveId '{fields[0]}'");
                    continue;
                }

                var enemyId = fields[1].Trim();

                if (!int.TryParse(fields[2], out var spawnCount))
                {
                    Debug.LogWarning($"[CsvWaveParser] Skipping row {i}: invalid SpawnCount '{fields[2]}'");
                    continue;
                }

                if (!float.TryParse(fields[3], out var spawnInterval))
                {
                    Debug.LogWarning($"[CsvWaveParser] Skipping row {i}: invalid SpawnInterval '{fields[3]}'");
                    continue;
                }

                entries.Add(new WaveEntry
                {
                    WaveId = waveId,
                    EnemyId = enemyId,
                    SpawnCount = spawnCount,
                    SpawnInterval = spawnInterval
                });
            }

            return entries;
        }
    }
}
```

### 2. WaveManager.cs

```csharp
using System.Collections.Generic;
using System.Linq;
using Events;
using Systems.Game;
using Systems.Parsing;
using UnityEngine;

namespace Systems.Managers
{
    public enum WaveState
    {
        Idle,
        Active,
        Complete
    }

    public class WaveManager : MonoBehaviour
    {
        #region Fields

        [SerializeField] private TextAsset waveCsvFile;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private EnemyManager enemyManager;

        [Header("Event Channels")]
        [SerializeField] private VoidEventChannel onWaveStarted;
        [SerializeField] private VoidEventChannel onWaveComplete;
        [SerializeField] private VoidEventChannel onAllWavesComplete;

        private Dictionary<int, List<WaveEntry>> _waves;
        private int _currentWave;

        #endregion

        #region Properties

        public WaveState State { get; private set; }
        public int CurrentWave => _currentWave;
        public int TotalWaves { get; private set; }

        #endregion

        #region Lifecycle

        private void Awake()
        {
            var entries = CsvWaveParser.Parse(waveCsvFile);

            _waves = entries
                .GroupBy(e => e.WaveId)
                .ToDictionary(g => g.Key, g => g.ToList());

            TotalWaves = _waves.Count;
            _currentWave = 0;
            State = WaveState.Idle;
        }

        #endregion

        #region Public API

        public void StartNextWave()
        {
            _currentWave++;

            if (_currentWave > TotalWaves)
            {
                onAllWavesComplete?.Raise();
                return;
            }

            State = WaveState.Active;

            var waveEntries = _waves[_currentWave];
            enemySpawner.StartBatch(waveEntries);

            onWaveStarted?.Raise();
        }

        public void CheckWaveProgress()
        {
            if (State != WaveState.Active) return;
            if (!enemySpawner.IsBatchComplete) return;
            if (enemyManager.AliveCount > 0) return;

            State = WaveState.Complete;
            onWaveComplete?.Raise();
        }

        #endregion
    }
}
```

### 3. EnemyManager.cs — Add tracking

We need `EnemyManager` to track how many enemies are alive so `WaveManager.CheckWaveProgress` can tell when a wave is truly done (all spawned AND all killed or reached end).

```csharp
using System.Collections.Generic;
using Enemies.Controllers;
using Interfaces;
using UnityEngine;

namespace Systems.Managers
{
    public class EnemyManager : MonoBehaviour
    {
        #region Fields

        private readonly HashSet<ITargetable> _aliveEnemies = new HashSet<ITargetable>();

        #endregion

        #region Properties

        public int AliveCount => _aliveEnemies.Count;

        #endregion

        #region Public API

        public void Register(EnemyController enemy)
        {
            _aliveEnemies.Add(enemy);
        }

        public void Unregister(EnemyController enemy)
        {
            _aliveEnemies.Remove(enemy);
        }

        #endregion
    }
}
```

**Update EnemyController** to register/unregister with EnemyManager:

Add to `EnemyController.Initialize()`:
```csharp
public void Initialize(EnemyData data, EnemyPath path)
{
    Path = path;
    InitData(data);
    InitStrategy();
    EnemyManager.Instance?.Register(this);
}
```

Add to `EnemyController.Die()`:
```csharp
public void Die()
{
    EnemyManager.Instance?.Unregister(this);
    ObjectPoolManager.Instance.Return("enemy", gameObject);
}
```

**EnemyManager needs an Instance property** — add a simple singleton:

```csharp
public static EnemyManager Instance { get; private set; }

private void Awake()
{
    Instance = this;
}
```

Full updated EnemyManager:

```csharp
using System.Collections.Generic;
using Enemies.Controllers;
using Interfaces;
using UnityEngine;

namespace Systems.Managers
{
    public class EnemyManager : MonoBehaviour
    {
        #region Fields

        private readonly HashSet<ITargetable> _aliveEnemies = new HashSet<ITargetable>();

        #endregion

        #region Properties

        public static EnemyManager Instance { get; private set; }
        public int AliveCount => _aliveEnemies.Count;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            Instance = this;
        }

        #endregion

        #region Public API

        public void Register(EnemyController enemy)
        {
            _aliveEnemies.Add(enemy);
        }

        public void Unregister(EnemyController enemy)
        {
            _aliveEnemies.Remove(enemy);
        }

        #endregion
    }
}
```

### 4. EnemySpawner.cs

Note: The skeleton has `Dictionary<string, EnemyData> enemyDataLookup` but Unity cannot serialize dictionaries. Replace with a `List<EnemyData>` and build a runtime lookup.

```csharp
using System.Collections;
using System.Collections.Generic;
using Data;
using Systems.Managers;
using UnityEngine;

namespace Systems.Game
{
    public class EnemySpawner : MonoBehaviour
    {
        #region Fields

        [SerializeField] private EnemyPath path;
        [SerializeField] private List<EnemyData> enemyDataList;
        [SerializeField] private string enemyPoolKey = "enemy";

        private Dictionary<string, EnemyData> _enemyDataLookup;
        private List<Coroutine> _activeSpawns;
        private int _remainingInBatch;

        #endregion

        #region Properties

        public bool IsBatchComplete => _remainingInBatch <= 0;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _enemyDataLookup = new Dictionary<string, EnemyData>();
            _activeSpawns = new List<Coroutine>();

            foreach (var data in enemyDataList)
            {
                var key = data.name.ToLower();
                if (!_enemyDataLookup.ContainsKey(key))
                {
                    _enemyDataLookup[key] = data;
                }
            }
        }

        #endregion

        #region Public API

        public void StartBatch(IEnumerable<WaveEntry> entries)
        {
            _remainingInBatch = 0;

            foreach (var entry in entries)
            {
                _remainingInBatch += entry.SpawnCount;
                var coroutine = StartCoroutine(SpawnEnemyRoutine(entry));
                _activeSpawns.Add(coroutine);
            }
        }

        #endregion

        #region Private Methods

        private IEnumerator SpawnEnemyRoutine(WaveEntry entry)
        {
            var enemyId = entry.EnemyId.ToLower();

            if (!_enemyDataLookup.TryGetValue(enemyId, out var enemyData))
            {
                Debug.LogError($"[EnemySpawner] No EnemyData found for id '{entry.EnemyId}'. Skipping batch.");
                _remainingInBatch -= entry.SpawnCount;
                yield break;
            }

            for (var i = 0; i < entry.SpawnCount; i++)
            {
                yield return new WaitForSeconds(entry.SpawnInterval);

                var enemyObj = ObjectPoolManager.Instance.Get(enemyPoolKey, path.StartPosition, Quaternion.identity);
                if (enemyObj == null)
                {
                    Debug.LogError("[EnemySpawner] Failed to get enemy from pool.");
                    _remainingInBatch--;
                    continue;
                }

                var controller = enemyObj.GetComponent<Enemies.Controllers.EnemyController>();
                if (controller != null)
                {
                    controller.Initialize(enemyData, path);
                }

                _remainingInBatch--;
            }
        }

        #endregion
    }
}
```

### Full updated EnemyController.cs (with EnemyManager integration)

```csharp
using Data;
using Enemies.Components;
using Interfaces;
using Strategies.Health;
using Strategies.Movement;
using Systems.Game;
using Systems.Managers;
using UnityEngine;

namespace Enemies.Controllers
{
    public class EnemyController : MonoBehaviour, IDamageable, ITargetable
    {
        #region Fields

        [Header("Enemy UI")]
        [SerializeField] private EnemyHealthBar healthBar;

        #endregion

        #region Properties

        public EnemyPath Path { get; private set; }
        public int CurrentWayPointIndex { get; set; }
        public HealthStrategy Health { get; private set; }
        public MovementStrategy Movement { get; private set; }
        public int GoldGiven { get; private set; }
        public int Damage { get; private set; }

        public Vector3 Position => transform.position;
        public bool IsAlive => Health != null && Health.CurrentHealth > 0f;
        public float HealthValue => Health != null ? Health.CurrentHealth : 0f;

        public float PathProgress
        {
            get
            {
                if (Path == null) return 0f;

                var index = CurrentWayPointIndex;
                var totalWaypoints = Path.WaypointCount;

                if (totalWaypoints <= 1) return index;

                var progress = (float)index;

                if (Path.HasWaypoint(index))
                {
                    var nextWaypoint = Path.GetWaypointPosition(index);
                    var prevWaypoint = index > 0
                        ? Path.GetWaypointPosition(index - 1)
                        : Path.StartPosition;

                    var segmentLength = Vector3.Distance(prevWaypoint, nextWaypoint);
                    if (segmentLength > 0.001f)
                    {
                        var distToNext = Vector3.Distance(transform.position, nextWaypoint);
                        var normalized = 1f - Mathf.Clamp01(distToNext / segmentLength);
                        progress += normalized;
                    }
                }

                return progress;
            }
        }

        #endregion

        #region Class Methods

        private void InitData(EnemyData data)
        {
            Health = data.Health;
            Movement = data.Movement;
            GoldGiven = data.GoldGiven;
            Damage = data.Damage;
        }

        private void InitStrategy()
        {
            Health.Initialize(this);
            Movement.Initialize(this);
        }

        public void Initialize(EnemyData data, EnemyPath path)
        {
            Path = path;
            InitData(data);
            InitStrategy();
            EnemyManager.Instance?.Register(this);
        }

        public void Die()
        {
            EnemyManager.Instance?.Unregister(this);
            ObjectPoolManager.Instance.Return("enemy", gameObject);
        }

        #endregion

        #region Unity Methods

        private void Update()
        {
            Movement.Tick(this);
        }

        #endregion

        public void TakeDamage(float damage)
        {
            Health.TakeDamage(this, damage);
        }
    }
}
```

**NOTE on ITargetable.Health vs EnemyController.HealthValue**: The property on ITargetable is called `Health` but `Health` is already taken by `HealthStrategy Health` on EnemyController. So the explicit interface property is named `HealthValue` on the class, and we use explicit implementation:

Actually — we need to handle the naming clash. `ITargetable.Health` clashes with `EnemyController.Health` (the HealthStrategy). Solution: use explicit interface implementation on EnemyController:

```csharp
float ITargetable.Health => Health != null ? Health.CurrentHealth : 0f;
```

This way `Health` still refers to the `HealthStrategy` property for internal use, while `ITargetable.Health` is available when accessed through the interface. Update the property section:

```csharp
public HealthStrategy Health { get; private set; }
// ...
float ITargetable.Health => Health?.CurrentHealth ?? 0f;
```

Remove the `HealthValue` property — use explicit interface implementation instead.

## Unity Editor Setup

1. **EnemyData assets**: Create one per enemy type with names matching the CSV `EnemyID` column (case-insensitive):
   - "basic" → create EnemyData SO named "Basic"
   - "fast" → "Fast"
   - "armoured" → "Armoured"
   - "flying" → "Flying"
   - "shield" → "Shield"
   - "regen" → "Regen"

2. **EnemySpawner setup**:
   - Add `EnemySpawner` to a GameObject in the scene
   - Assign `path` reference
   - Add all EnemyData assets to `enemyDataList`
   - Set `enemy pool key` to "enemy"

3. **WaveManager setup**:
   - Add `WaveManager` to a GameObject
   - Assign `waveCsvFile` → drag `wave_data.csv` from Assets/Data/Waves/
   - Assign `enemy spawner` and `enemy manager` references
   - Event channels (not created yet — wire in Ep08)

4. **EnemyManager**: Add to scene, singleton persists.

5. **Enemy pool key**: Ensure ObjectPoolManager has an "enemy" pool config pointing to your enemy prefab.

## Test Plan

| Test | Steps | Expected |
|------|-------|----------|
| CSV parsing | Run with wave_data.csv assigned | 16 WaveEntry structs parsed (17 lines minus header), no warnings |
| Malformed row | Add `,bad,data` line to CSV | Warning logged, row skipped, remaining entries parse fine |
| Empty CSV | Assign empty TextAsset | Empty list returned, no crash |
| Wave grouping | Log `_waves.Count` after Awake | 5 waves (IDs 1-5) |
| StartNextWave | Call StartNextWave() | Spawner receives 2 entries for wave 1 (5 basic + 3 fast) |
| Spawn timing | Watch enemies spawn | Basic at 1.0s intervals, fast at 0.8s intervals |
| BatchComplete | After all wave 1 enemies spawned | `IsBatchComplete` returns true |
| CheckWaveProgress | After all enemies die/reach end | WaveState transitions to Complete, event raised |
| EnemyData missing | Set EnemyID to "nonexistent" in CSV | Error logged, batch skipped, _remainingInBatch adjusted |
| All waves done | Call StartNextWave 6 times | onAllWavesComplete raised on 6th call |
| EnemyManager tracking | Spawn enemies, log AliveCount | Count increases on spawn, decreases on Die() |

## Debugging Tips

- **0 entries parsed**: Check `waveCsvFile` isn't null. Open the TextAsset in inspector — it should show the CSV text. If it's blank, re-import the file.
- **Wrong number of entries**: The CSV has Windows line endings (`\r\n`). `Split('\n')` handles this because we `Trim('\r')` each line. If you see extra empty entries, check for trailing newlines in the CSV.
- **EnemyData lookup fails**: Names must match. The lookup uses `data.name.ToLower()` — this is the **asset name** in Unity (the filename without extension), not a custom field. Name your SO assets exactly "Basic", "Fast", etc.
- **Spawner doesn't spawn**: Verify ObjectPoolManager has an "enemy" pool. Verify `path.StartPosition` isn't at (0,0,0) off-screen.
- **IsBatchComplete never true**: Each spawn decrements `_remainingInBatch` including failed spawns. If pool returns null, the count still decrements. If it's stuck positive, a coroutine may have thrown.
- **WaveManager.CheckWaveProgress never transitions**: Must be called from somewhere — either from Update loop or from event callbacks. Add it to WaveManager.Update() for now, optimize later.
- **Enemy still alive after reaching end**: The `GroundedPath.Tick()` calls `CompleteMovement()` when out of waypoints, but nothing calls `Die()` on the enemy. You need to wire `Movement.OnMovementCompletion` to `enemy.Die()` — add this in `EnemyController.Initialize()`:

```csharp
private void InitStrategy()
{
    Health.Initialize(this);
    Movement.Initialize(this);
    Movement.OnMovementCompletion += Die;
}
```

And clean up the subscription — call `Movement.OnMovementCompletion -= Die` in `Die()`. Full updated InitStrategy and Die:

```csharp
private void InitStrategy()
{
    Health.Initialize(this);
    Movement.Initialize(this);
    Movement.OnMovementCompletion += Die;
}

public void Die()
{
    Movement.OnMovementCompletion -= Die;
    EnemyManager.Instance?.Unregister(this);
    ObjectPoolManager.Instance.Return("enemy", gameObject);
}
```