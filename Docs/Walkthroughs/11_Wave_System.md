# Episode 11: Wave System

## What You're Building

CSV-driven waves of enemies spawn automatically. `WaveManager` drives a state machine. `EnemySpawner` handles batch spawning with coroutines. A wave counter appears in the UI.

## Wave Data Format

Your CSV file (`Assets/Data/Waves/wave_data.csv`):

```csv
wave,enemyType,count,spawnDelay,interval
1,Basic,5,0,1.0
2,Basic,8,0,0.8
3,Armoured,3,0,1.5
4,Basic,5,0,1.0
4,Flying,5,5,0.8
5,FlyingArmoured,5,0,1.0
```

- `wave`: wave number (same number = same wave, different enemy types)
- `enemyType`: maps to an EnemyData SO name
- `count`: how many of this type
- `spawnDelay`: seconds before this batch starts (0 = start with wave)
- `interval`: seconds between spawns within the batch

## WaveData struct + CsvWaveParser.cs

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Systems.Parsing
{
    public readonly record struct WaveData(int Wave, string EnemyType, int Count, float SpawnDelay, float Interval);

    public class CsvWaveParser
    {
        public List<WaveData> Parse(TextAsset csvFile)
        {
            var lines = csvFile.text.Split('\n');
            var result = new List<WaveData>();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] parts = line.Split(',');
                if (parts.Length < 5) continue;

                result.Add(new WaveData(
                    wave: int.Parse(parts[0]),
                    enemyType: parts[1].Trim(),
                    count: int.Parse(parts[2]),
                    spawnDelay: float.Parse(parts[3]),
                    interval: float.Parse(parts[4])
                ));
            }

            return result;
        }

        public Dictionary<int, List<WaveData>> GroupByWave(List<WaveData> data)
        {
            return data
                .GroupBy(d => d.Wave)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }
}
```

**Why parse CSV at runtime, not in editor?** Design-driven data. Designers edit CSV in a spreadsheet. The system reads it at startup. No ScriptableObject wrangling for 50+ wave definitions.

## EnemySpawner.cs

```csharp
using System.Collections;
using System.Collections.Generic;
using Core;
using Data;
using Systems.Game;
using Systems.Managers;
using UnityEngine;

namespace Systems.Game
{
    public class EnemySpawner
    {
        private readonly Dictionary<string, EnemyData> _enemyDataMap;
        private readonly EnemyPath _path;
        private readonly MonoBehaviour _coroutineHost;

        public EnemySpawner(Dictionary<string, EnemyData> enemyDataMap, EnemyPath path, MonoBehaviour coroutineHost)
        {
            _enemyDataMap = enemyDataMap;
            _path = path;
            _coroutineHost = coroutineHost;
        }

        public void SpawnBatch(List<WaveData> batchData, System.Action onBatchComplete)
        {
            _coroutineHost.StartCoroutine(SpawnBatchRoutine(batchData, onBatchComplete));
        }

        private IEnumerator SpawnBatchRoutine(List<WaveData> batchData, System.Action onBatchComplete)
        {
            foreach (WaveData data in batchData)
            {
                if (data.SpawnDelay > 0)
                    yield return new WaitForSeconds(data.SpawnDelay);

                for (int i = 0; i < data.Count; i++)
                {
                    SpawnEnemy(data.EnemyType);
                    if (i < data.Count - 1)
                        yield return new WaitForSeconds(data.Interval);
                }
            }

            onBatchComplete?.Invoke();
        }

        private void SpawnEnemy(string enemyType)
        {
            if (!_enemyDataMap.TryGetValue(enemyType, out EnemyData data)) return;

            GameObject enemy = Services.Get<ObjectPoolManager>().Get("enemy", _path.StartPosition, Quaternion.identity);
            enemy.GetComponent<EnemyController>().Initialize(data, _path);
        }
    }
}
```

Plain C# class, not MonoBehaviour. Needs a `MonoBehaviour` host for coroutines — `WaveManager` provides this.

## WaveManager.cs

```csharp
using System.Collections.Generic;
using Core;
using Systems.Parsing;
using UnityEngine;

namespace Systems.Managers
{
    public class WaveManager : MonoBehaviour
    {
        public enum State { Idle, Spawning, Waiting }

        [SerializeField] private TextAsset waveCsv;
        [SerializeField] private EnemyPath path;
        [SerializeField] private EnemyData[] enemyDataAssets;

        private State _state;
        private int _currentWave;
        private int _totalWaves;
        private Dictionary<int, List<WaveData>> _waveData;
        private EnemySpawner _spawner;
        private int _activeEnemies;

        public State CurrentState => _state;
        public int CurrentWave => _currentWave;
        public int TotalWaves => _totalWaves;

        public event System.Action<int> OnWaveStarted;
        public event System.Action<int> OnWaveComplete;

        private void Start()
        {
            var parser = new CsvWaveParser();
            var allData = parser.Parse(waveCsv);
            _waveData = parser.GroupByWave(allData);
            _totalWaves = _waveData.Count;
            _currentWave = 0;

            var enemyDataMap = new Dictionary<string, EnemyData>();
            foreach (EnemyData data in enemyDataAssets)
                enemyDataMap[data.name] = data;

            _spawner = new EnemySpawner(enemyDataMap, path, this);

            StartNextWave();
        }

        public void StartNextWave()
        {
            _currentWave++;

            if (_currentWave > _totalWaves)
            {
                _state = State.Idle;
                return;
            }

            _state = State.Spawning;
            OnWaveStarted?.Invoke(_currentWave);

            _spawner.SpawnBatch(_waveData[_currentWave], () =>
            {
                _state = State.Waiting;
            });
        }

        public void OnEnemyDied()
        {
            _activeEnemies--;
            CheckWaveComplete();
        }

        public void OnEnemyReachedEnd()
        {
            _activeEnemies--;
            CheckWaveComplete();
        }

        private void CheckWaveComplete()
        {
            if (_state != State.Waiting) return;
            if (_activeEnemies > 0) return;

            OnWaveComplete?.Invoke(_currentWave);
            StartNextWave();
        }
    }
}
```

**Note:** `_activeEnemies` tracking needs to be wired up. The simplest approach: `WaveManager` subscribes to `CombatEvents.EnemyDeath` and `CombatEvents.EnemyReachedEnd` to decrement the counter. The spawner increments it on each spawn.

## GameBootstrapper.cs (add WaveManager)

```csharp
using Core;
using Events.Registries;
using Systems.Game;
using Systems.Managers;
using UnityEngine;

namespace Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private ObjectPoolManager objectPoolManager;
        [SerializeField] private WaveManager waveManager;

        [SerializeField] private int startingGold = 100;
        [SerializeField] private int startingLives = 20;

        private void Awake()
        {
            RegisterServices();
            InitializeServices();
        }

        private void OnDestroy()
        {
            Services.Get<PlayerStats>().Cleanup();
            Services.Clear();
        }

        private void RegisterServices()
        {
            Services.Register(objectPoolManager);

            var combatEvents = new CombatEvents();
            Services.Register(combatEvents);

            var economyEvents = new EconomyEvents();
            Services.Register(economyEvents);

            var playerStats = new PlayerStats(startingGold, startingLives);
            Services.Register(playerStats);
        }

        private void InitializeServices()
        {
            Services.Get<PlayerStats>().Initialize();
        }
    }
}
```

`WaveManager` stays as a MonoBehaviour on its own GameObject (it needs coroutine support). We don't register it in Services yet — it can be added if other systems need to access it. For now, it self-manages.

## WaveUI.cs

```csharp
using Systems.Managers;
using TMPro;
using UnityEngine;

namespace UI
{
    public class WaveUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private WaveManager waveManager;

        private void Start()
        {
            waveManager.OnWaveStarted += OnWaveStarted;
        }

        private void OnDestroy()
        {
            waveManager.OnWaveStarted -= OnWaveStarted;
        }

        private void OnWaveStarted(int wave)
        {
            waveText.text = $"Wave {wave}/{waveManager.TotalWaves}";
        }
    }
}
```

## Unity Editor Setup

### 1. Create wave_data.csv

1. In `Assets/Data/Waves/`, create `wave_data.csv` with content from above
2. In Unity Inspector, set the CSV file's import settings to ensure it's a TextAsset

### 2. Wire WaveManager

1. Create empty GameObject "WaveManager"
2. Add `WaveManager` component
3. Drag `wave_data.csv` into the `waveCsv` field
4. Drag `EnemyPath` into `path` field
5. Set `enemyDataAssets` array: add all EnemyData SOs (ED_Basic, ED_Armoured, ED_Flying, ED_FlyingArmoured)

### 3. Add WaveUI

1. Add a TextMeshPro element to the screen-space Canvas
2. Add `WaveUI` component
3. Assign text reference and WaveManager reference

## Debugging

| Symptom | Cause | Fix |
|---------|-------|-----|
| No enemies spawn | CSV not assigned or empty | Check waveManager.waveCsv in Inspector |
| Only one enemy type per wave | CSV typo in enemyType name | Names must exactly match EnemyData SO names |
| Waves don't advance | `_activeEnemies` never reaches 0 | Wire up CombatEvents subscription in WaveManager |
| NullRef in EnemySpawner | EnemyData name mismatch | Verify SO names match CSV enemyType column |
| Enemies spawn but don't move | Initialize not called | EnemySpawner calls Initialize — check enemyDataMap lookup |