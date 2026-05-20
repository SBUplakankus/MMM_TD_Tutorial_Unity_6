# Episode 07: Runtime CSV Wave Data

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden;">
  <iframe src="https://www.youtube.com/embed/EPISODE_07_PLACEHOLDER" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; border:0;" allowfullscreen></iframe>
</div>

## Learning Objectives

- Parse CSV data at runtime using `TextAsset` and string splitting
- Design a data-driven wave configuration system that non-programmers can edit
- Implement batch spawning with per-entry coroutine timers
- Wire CSV data through the spawning pipeline to the object pool

## Key Concepts

- **Runtime CSV parsing** — split `TextAsset.text` by newlines and commas, no external libraries
- **TextAsset** — Unity's wrapper for text files, assigned via inspector
- **Data-driven wave configuration** — game behaviour defined in a spreadsheet, not in code
- **Batch spawning** — entries with the same `WaveID` spawn concurrently on independent timers
- **EnemyData lookup** — `Dictionary<string, EnemyData>` maps CSV `EnemyID` strings to ScriptableObject assets

See also: [Scriptable Objects](../Concepts/Scriptable_Objects.md)

## Code Roadmap

| File | Role |
|------|------|
| `CsvWaveParser.cs` | Parses `TextAsset` CSV into `List<WaveEntry>` |
| `WaveManager.cs` | Groups entries by wave, manages wave state, triggers spawner |
| `EnemySpawner.cs` | Runs batch coroutines, fetches enemies from pool |
| `wave_data.csv` | Designer-editable wave configuration |
| `EnemyData.cs` | SO asset defining enemy properties (health, speed, gold, etc.) |

## Architecture Context

```
wave_data.csv (TextAsset)
  → CsvWaveParser.Parse
  → WaveManager (groups by WaveID, tracks state)
    → EnemySpawner.StartBatch
      → Coroutine per WaveEntry
        → ObjectPoolManager.Get (spawn enemy from pool)
        → EnemyController.Init (load EnemyData by ID)
```

Data flows from a CSV file through parsing, grouping, and batch spawning all the way down to pooled enemy instantiation.

## The CSV Format

```csv
WaveID,EnemyID,SpawnCount,SpawnInterval
1,basic,5,1.0
1,fast,3,0.8
2,basic,8,0.9
2,armoured,2,1.5
```

- **Same WaveID = concurrent batch** — all entries with WaveID `1` spawn at the same time
- **Each entry in a batch runs independently** — its own coroutine, its own interval timer
- **SpawnInterval** — seconds between each enemy in that entry (not between entries)

Example: Wave 1 starts, `basic` enemies spawn every 1.0s while `fast` enemies spawn every 0.8s simultaneously.

## Step-by-Step Implementation Guide

### Step 1: Why CSV?

Traditional approach: hardcode wave data in a `MonoBehaviour` or create a `ScriptableObject` per wave. Problems:

- Programmers must edit every wave change
- No spreadsheet tooling for designers
- Version control diffs are noisy for binary SO assets
- Scaling to 50+ waves is painful

CSV advantages:

- Designers edit in Excel, Google Sheets, or any text editor
- Version-control friendly — plain text diffs
- No Unity needed to author wave content
- Easy to bulk-edit (transpose, find-replace, formulas)

### Step 2: Walk `WaveEntry` and `CsvWaveParser`

```csharp
[Serializable]
public struct WaveEntry
{
    public int WaveId;
    public string EnemyId;
    public int SpawnCount;
    public float SpawnInterval;
}
```

```csharp
public static class CsvWaveParser
{
    public static List<WaveEntry> Parse(TextAsset csvFile)
    {
        var entries = new List<WaveEntry>();

        // Split by newlines, skip header row (index 0)
        // For each line: split by comma, parse fields into WaveEntry
        // Add to entries list
        // TODO: implement parsing logic

        return entries;
    }
}
```

- Static utility class — no state, just transforms text to data
- Skips the header row (WaveID,EnemyID,SpawnCount,SpawnInterval)
- Returns a flat list — grouping happens in `WaveManager`

!!! warning "TextAsset vs Resources"
    `TextAsset` must be referenced in the inspector (drag into the `WaveManager` field). Do **NOT** use `Resources.Load` in this project. The Resources folder pattern creates hidden coupling and makes asset cleanup error-prone.

### Step 3: Walk `WaveManager`

```csharp
public enum WaveState { Idle, Active, Complete }

public class WaveManager : MonoBehaviour
{
    [SerializeField] private TextAsset waveCsv;
    [SerializeField] private EnemySpawner enemySpawner;

    private Dictionary<int, List<WaveEntry>> _waves;
    private int _currentWave;
    private WaveState _state;

    // TODO: Event channels for onWaveStarted, onWaveComplete, onAllWavesComplete

    private void Awake()
    {
        var entries = CsvWaveParser.Parse(waveCsv);
        // Group entries by WaveId into Dictionary<int, List<WaveEntry>>
        // TODO: implement grouping with LINQ or loop
    }

    public void StartNextWave()
    {
        _currentWave++;
        _state = WaveState.Active;
        // Get batch for _currentWave from _waves dictionary
        // Pass to enemySpawner.StartBatch()
        // Raise onWaveStarted event
        // TODO: implement
    }

    public void CheckWaveProgress()
    {
        // Called when enemies die (via event or polling)
        // If batch is complete, set state to Complete, raise onWaveComplete
        // If no more waves, raise onAllWavesComplete
        // TODO: implement
    }
}
```

- Parses CSV once on `Awake`, groups entries into a dictionary keyed by `WaveId`
- `WaveState` tracks whether a wave is running, idle, or done
- `StartNextWave` increments the wave counter and delegates batch spawning
- `CheckWaveProgress` is the callback hook for when the wave might be over

### Step 4: Walk `EnemySpawner`

```csharp
public class EnemySpawner : MonoBehaviour
{
    private int _remainingInBatch;

    public void StartBatch(List<WaveEntry> batch)
    {
        _remainingInBatch = 0;

        foreach (var entry in batch)
        {
            _remainingInBatch += entry.SpawnCount;
            StartCoroutine(SpawnEnemyRoutine(entry));
        }
    }

    private IEnumerator SpawnEnemyRoutine(WaveEntry entry)
    {
        for (int i = 0; i < entry.SpawnCount; i++)
        {
            yield return new WaitForSeconds(entry.SpawnInterval);

            // Fetch enemy from pool via ObjectPoolManager
            // Initialize with EnemyData lookup by entry.EnemyId
            // TODO: implement spawn + init

            _remainingInBatch--;
        }
    }

    public bool IsBatchComplete => _remainingInBatch <= 0;
}
```

- Each `WaveEntry` in the batch gets its own coroutine
- Coroutines run independently — `fast` enemies at 0.8s interval spawn faster than `basic` at 1.0s
- `_remainingInBatch` tracks total spawns across all concurrent coroutines
- `IsBatchComplete` is the signal `WaveManager` polls or subscribes to

!!! info "Coroutine timing"
    The first enemy in each entry spawns *after* the first `SpawnInterval` delay (because `WaitForSeconds` is before the spawn logic). If you want instant first spawns, move the yield to the end of the loop.

### Step 5: EnemyData Lookup

```csharp
// In WaveManager or a dedicated lookup class
[SerializeField] private EnemyData[] enemyDataAssets;
private Dictionary<string, EnemyData> _enemyDataLookup;

private void BuildLookup()
{
    _enemyDataLookup = new Dictionary<string, EnemyData>();
    foreach (var data in enemyDataAssets)
    {
        _enemyDataLookup[data.EnemyId] = data;
    }
}
```

- Assign all `EnemyData` SOs in the inspector as an array
- Build a `Dictionary<string, EnemyData>` on `Awake` for O(1) lookup
- The `EnemyId` string in the CSV must exactly match `EnemyData.EnemyId`
- EnemySpawner passes the looked-up `EnemyData` to `EnemyController.Init`

### Step 6: Wire in Unity

1. Create `wave_data.csv` in your project (any folder — it becomes a `TextAsset`)
2. Drag it into the `waveCsv` field on `WaveManager`
3. Create `EnemyData` SOs for each enemy type: `basic`, `fast`, `armoured`
4. Drag all `EnemyData` SOs into the `enemyDataAssets` array on `WaveManager`
5. Assign `EnemySpawner` reference on `WaveManager`
6. Add enemy prefabs to `ObjectPoolManager` pool configs with matching pool keys

### Step 7: Test the Full Flow

Walk through what happens when the game starts a wave:

1. `WaveManager.StartNextWave()` — increments wave, gets batch from dictionary
2. `EnemySpawner.StartBatch(batch)` — starts a coroutine per entry
3. Each coroutine: `WaitForSeconds(interval)` → pool spawn → `Init(enemyData)` → decrement remaining
4. As enemies die: `WaveManager.CheckWaveProgress()` → `IsBatchComplete` → wave over
5. Next `StartNextWave()` call begins the following wave
6. When `_currentWave` exceeds the last key in `_waves`, raise `onAllWavesComplete`

## Episode Recap

- CSV wave data enables designer-driven configuration without Unity or code changes
- `CsvWaveParser` transforms `TextAsset` text into structured `WaveEntry` data
- `WaveManager` groups entries by wave and manages state transitions
- `EnemySpawner` runs concurrent coroutines — one per entry in the batch
- EnemyData lookup bridges the CSV string ID to runtime `EnemyData` SO assets
- The full pipeline: CSV → parse → group → batch spawn → pool → init → play

## Challenge

Add a **"SubWave"** concept within a single `WaveID`. How would you modify the CSV format to support delayed sub-waves? For example:

- Wave 1 starts: spawn `basic` enemies immediately
- 10 seconds later: spawn `armoured` enemies as a surprise second wave within Wave 1

Consider:

- What new column(s) would you add to the CSV? (e.g., `SubWaveDelay`?)
- How would `WaveManager` handle a delay before starting certain entries in the batch?
- Could you reuse `SpawnInterval` for intra-entry timing and add a separate `BatchDelay` for inter-sub-wave timing?
- How does this affect `CheckWaveProgress` — when is a wave with delayed sub-waves truly "complete"?