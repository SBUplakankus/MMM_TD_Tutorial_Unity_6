# Episode 11 — Wave System

<!-- Video placeholder -->
<iframe width="560" height="315" src="https://www.youtube.com/embed/PLACEHOLDER_EP11" frameborder="0"></iframe>

---

## Learning Objectives

- Recognise the limitations of manually placing enemies in the Inspector
- Design a CSV wave data format for data-driven spawning
- Implement `CsvWaveParser` to read and validate wave definitions
- Build `WaveManager` to drive wave sequencing and completion
- Implement `EnemySpawner` coroutine-based batch spawning with delays
- Connect wave events to the event channel system from Episode 10

## Key Concepts

- [Factory Pattern](../Concepts/Factory_Pattern.md)
- [Observer Pattern](../Concepts/Observer_Pattern.md)

---

## What We're Starting With

Enemies spawn manually — you place an EnemyController prefab in the Scene, press Play, and watch one enemy walk the path. Spawning more enemies means duplicating GameObjects or writing ad-hoc spawn code. There's no concept of "waves" — just individual enemy placement.

---

## The Naive Version

```csharp
// EnemySpawner.cs — the problem
public class EnemySpawner : MonoBehaviour
{
    // TODO: Manual inspector placement — the problem
    //       [SerializeField] GameObject _enemyPrefab;
    //       [SerializeField] Transform _spawnPoint;
    //
    //       void Start() {
    //           // Spawn ONE enemy. Want more? Duplicate in the inspector.
    //           // Want different types? More serialized fields.
    //           // Want wave timing? Write a new coroutine every time.
    //           var go = Services.Get<ObjectPoolManager>().GetEnemy();
    //           go.transform.position = _spawnPoint.position;
    //       }
}
```

This approach doesn't scale. Every new enemy type, wave configuration, or timing change requires editing the scene or writing custom code. Game designers can't tweak wave data without developer help.

---

## The Refactor

We move enemy spawning to a **data-driven** system. Wave definitions live in a CSV file that designers can edit without touching code. `WaveManager` reads the data, and `EnemySpawner` handles batch spawning with coroutines.

### Architecture Context

```
  wave_data.csv
       │
       ▼
 ┌─────────────────┐
 │  CsvWaveParser  │  ──reads──►  List<WaveEntry>
 └─────────────────┘                      │
                                          │
 ┌─────────────────┐                      │
 │  WaveManager    │◄─────────────────────┘
 │  (drives state) │──► raises WaveEvents.WaveStarted
 │                 │──► raises WaveEvents.WaveCompleted
 └───────┬─────────┘
         │ starts waves
         ▼
 ┌─────────────────┐      ┌──────────────────┐
 │ EnemySpawner    │─────►│ ObjectPoolManager │
 │ (coroutine      │      │ (pooled enemies)  │
 │  batch spawn)   │      └──────────────────┘
 └─────────────────┘
         │
         │ on enemy death
         ▼
 ┌──────────────┐
 │ CombatEvents │──► PlayerStats, WaveManager, ...
 └──────────────┘
```

### Code Roadmap

| File | Purpose |
|------|---------|
| `Data/wave_data.csv` | Wave definitions — enemy type, count, delay, per wave |
| `Systems/Parsing/CsvWaveParser.cs` | Reads and validates CSV into `List<WaveEntry>` |
| `Systems/Game/WaveManager.cs` | Drives wave state machine, raises wave events |
| `Systems/Game/EnemySpawner.cs` | Coroutine-based batch spawning with delays |
| `Data/EnemyData.cs` | May need a lookup method from enemy type string |

---

## Step-by-Step Implementation

### Step 1 — Design the CSV format

Create `Data/wave_data.csv`:

```csv
WaveID,EnemyType,Count,SpawnDelay
1,Normal,5,1.0
1,Fast,3,0.6
2,Normal,8,0.8
2,Armoured,2,1.5
3,Normal,10,0.5
3,Fast,5,0.4
3,Armoured,3,1.2
```

**Rows with the same WaveID spawn concurrently** in sequence. When all enemies in a wave are dead, the next wave begins. Designers can edit this file without touching code.

### Step 2 — Create the WaveEntry data class

Add to `Data/WaveEntry.cs` or extend an existing data file:

```csharp
namespace Data
{
    [System.Serializable]
    public struct WaveEntry
    {
        // TODO: int WaveID — which wave this entry belongs to
        // TODO: string EnemyType — lookup key for EnemyData (e.g. "Normal", "Fast", "Armoured")
        // TODO: int Count — how many of this enemy type to spawn
        // TODO: float SpawnDelay — seconds between each enemy in this batch
    }
}
```

### Step 3 — Implement CsvWaveParser

Create `Systems/Parsing/CsvWaveParser.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using Data;
using UnityEngine;

namespace Systems.Parsing
{
    public static class CsvWaveParser
    {
        // TODO: public static List<WaveEntry> Parse(TextAsset csvFile)
        //       1. Split csvFile.text by newlines
        //       2. Skip the header row
        //       3. For each line, split by comma
        //       4. Parse WaveID (int), EnemyType (string), Count (int), SpawnDelay (float)
        //       5. Validate: Count > 0, SpawnDelay >= 0, known EnemyType
        //       6. Return List<WaveEntry> sorted by WaveID then order of appearance

        // TODO: private static bool ValidateEnemyType(string type)
        //       Check against known EnemyData entries
        //       Log a warning and skip if unknown type
    }
}
```

### Step 4 — Update EnemyData with type lookup

Update `Data/EnemyData.cs`:

```csharp
namespace Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "TD/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        // TODO: Add a public string enemyType field
        //       (e.g. "Normal", "Fast", "Armoured")
        //       This matches the EnemyType column in wave_data.csv

        // ... existing fields (prefab, health, speed, goldReward, etc.)
    }
}
```

We also need a way to look up `EnemyData` by type string. Add a lookup registry:

```csharp
namespace Data
{
    public static class EnemyDataRegistry
    {
        // TODO: Private static Dictionary<string, EnemyData> _entries

        // TODO: public static void Register(EnemyData data)
        //       Add to dictionary keyed by data.enemyType

        // TODO: public static EnemyData Get(string enemyType)
        //       Return matching EnemyData, throw if not found
    }
}
```

Register each `EnemyData` ScriptableObject in `GameBootstrapper` or via an `EnemyDataCatalog` MonoBehaviour that holds a list and calls `Register` on Awake.

### Step 5 — Implement WaveManager

Create `Systems/Game/WaveManager.cs`:

```csharp
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data;
using Events.Registries;
using Core;
using UnityEngine;

namespace Systems.Game
{
    public class WaveManager : MonoBehaviour
    {
        // TODO: [SerializeField] TextAsset _waveCsv;
        // TODO: Private List<WaveEntry> _waveEntries;
        // TODO: Private int _currentWave = 0;
        // TODO: Private int _highestWave;
        // TODO: Private int _enemiesAliveThisWave = 0;

        // TODO: void Start()
        //       1. Parse CSV: _waveEntries = CsvWaveParser.Parse(_waveCsv);
        //       2. Determine highest WaveID
        //       3. Subscribe to CombatEvents.EnemyDied and EnemyReachedEnd
        //       4. Start first wave: StartCoroutine(RunWave(1));

        // TODO: IEnumerator RunWave(int waveId)
        //       1. Filter _waveEntries where WaveID == waveId
        //       2. Set _enemiesAliveThisWave = sum of Count for this wave
        //       3. Raise WaveEvents.WaveStarted with waveId
        //       4. For each batch in the wave, start EnemySpawner.SpawnBatch coroutine
        //       5. Wait for all batches to complete spawning
        //       6. (Wave completion is detected by the event handler below)

        // TODO: private void OnEnemyDied(EnemyData data)
        //       Decrement _enemiesAliveThisWave
        //       If _enemiesAliveThisWave <= 0:
        //           Raise WaveEvents.WaveCompleted with waveId
        //           If more waves remain: StartCoroutine(RunWave(_currentWave + 1))
        //           Else: raise GameEvents.GameOver (win condition)

        // TODO: private void OnEnemyReachedEnd(EnemyData data)
        //       Same decrement logic — enemy left the field either way

        // TODO: void OnDestroy()
        //       Unsubscribe from all events
    }
}
```

### Step 6 — Implement EnemySpawner batch logic

Update `Systems/Game/EnemySpawner.cs`:

```csharp
using System.Collections;
using Data;
using UnityEngine;

namespace Systems.Game
{
    public class EnemySpawner : MonoBehaviour
    {
        // TODO: [SerializeField] Transform _spawnPoint;
        // TODO: [SerializeField] Transform _waypointContainer; (path reference)

        // TODO: public IEnumerator SpawnBatch(WaveEntry entry)
        //       1. Look up EnemyData: EnemyDataRegistry.Get(entry.EnemyType)
        //       2. For i = 0 to entry.Count:
        //           a. Get pooled enemy from ObjectPoolManager
        //           b. Get IPoolable component and call Reset()
        //           c. Set position to _spawnPoint.position
        //           d. Configure EnemyController with the looked-up EnemyData
        //           e. Register with UpdateManager
        //           f. yield return new WaitForSeconds(entry.SpawnDelay)
        //
        //       This coroutine yields between each spawn, creating
        //       a staggered stream of enemies rather than a mass instantiation.

        // TODO: Remove any old Start() manual spawning code
    }
}
```

### Step 7 — Wire it all up in GameBootstrapper

Update `Core/GameBootstrapper.cs`:

```csharp
// TODO: Add WaveManager reference
//       [SerializeField] WaveManager _waveManager;

// TODO: In Awake(), after existing registrations:
//       Services.Register(_waveManager);
```

---

## Episode Recap

- **Naive**: Manually placing enemy prefabs in the Inspector — no waves, no data, no designer control
- **Refactor**: CSV wave data → `CsvWaveParser` → `WaveManager` drives wave state → `EnemySpawner` handles batch spawning with coroutines
- Enemies with the same `WaveID` spawn concurrently; the next wave starts when all enemies from the current wave are dead
- Wave events (`WaveStarted`, `WaveCompleted`) integrate with the event channel system from Episode 10
- Designers can tweak `wave_data.csv` without touching any code

---

## Challenge

1. Add a `Formation` column to the CSV (e.g. `Line`, `VShape`, `Scatter`). Implement formation logic in `EnemySpawner` that offsets spawn positions based on the formation type. How does this change `WaveEntry` and `SpawnBatch`?

2. Add "boss waves" — a special wave with a single very-tough enemy. What changes to the CSV and `WaveManager` would support this? Hint: consider a `Scaling` column with multiplier values.