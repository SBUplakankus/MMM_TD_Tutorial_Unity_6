# Episode 13 — Audio Middleware

<!-- Video placeholder -->
<iframe width="560" height="315" src="https://www.youtube.com/embed/PLACEHOLDER_EP13" frameborder="0"></iframe>

---

## Learning Objectives

- Recognise the problems with `AudioSource.PlayClipAtPoint()` (no pooling, no mixer control, pitch-identical)
- Design `AudioData` ScriptableObjects as pure configuration containers (clip array, mixer group, pitch range)
- Implement `AudioController` that subscribes to event registries and plays sounds via pooled one-shots
- Implement `AudioPoolHandler` as an `IPoolable` pooled `AudioSource`
- Integrate audio with the Service Locator and Object Pool systems from earlier episodes

## Key Concepts

- [Observer Pattern](../Concepts/Observer_Pattern.md)
- [Object Pooling](../Concepts/Object_Pooling.md)
- [Service Locator](../Concepts/Service_Locator.md)

---

## What We're Starting With

The game is silent. No audio plays when enemies die, towers fire, projectiles impact, or waves start. The event system from Episode 10 is firing events like `CombatEvents.EnemyDied` and `WaveEvents.WaveStarted`, but nothing is listening for them.

---

## The Naive Version

```csharp
// The WRONG way — scattering PlayClipAtPoint everywhere
public class EnemyController : MonoBehaviour
{
    // TODO: DON'T DO THIS — the problem
    //       [SerializeField] AudioClip _deathSound;
    //
    //       public void Die()
    //       {
    //           AudioSource.PlayClipAtPoint(_deathSound, transform.position);
    //           // Problems:
    //           //   1. Creates a new AudioSource GameObject every call → GC pressure
    //           //   2. No mixer routing → can't group sounds (SFX, music, ambient)
    //           //   3. Same pitch every time → repetitive and fatiguing
    //           //   4. No volume control per sound type
    //           //   5. Can't stop all SFX when pausing
    //           //   6. Audio clips are serialized on every enemy — no central config
    //       }
}
```

This approach distributes audio responsibility across every gameplay class. Want to change the volume of all explosion sounds? You're editing ten different scripts. Want to add pitch randomisation? More conditionals everywhere.

---

## The Refactor

We build a **centralised audio system** with three components:

- `AudioData` — ScriptableObject holding pure config (clips, mixer group, pitch range, volume)
- `AudioController` — subscribes to event registries, looks up `AudioData`, plays via pooled one-shots
- `AudioPoolHandler` — `IPoolable` wrapper around `AudioSource`, pooled by `ObjectPoolManager`

### Architecture Context

```
┌───────────────────────────────────────────────────────────┐
│                     AudioController                        │
│                                                           │
│  Subscribes to:                                           │
│    CombatEvents.EnemyDied       → Play("EnemyDeath")     │
│    CombatEvents.EnemyReachedEnd → Play("EnemyReachEnd")  │
│    CombatEvents.TowerFired     → Play("TowerFire")       │
│    WaveEvents.WaveStarted      → Play("WaveStart")       │
│    WaveEvents.WaveCompleted    → Play("WaveComplete")     │
│    EconomyEvents.GoldChanged   → Play("GoldEarned")       │
│    GameEvents.GameOver         → Play("GameOver")         │
│                                                           │
│  Play(soundId) ──► looks up AudioData ──►                 │
│     gets clip (random from array) ──►                     │
│     gets pooled AudioPoolHandler ──►                       │
│     configures AudioSource ──►                            │
│     Play() + auto-return to pool after clip length        │
└───────────────────────────────────────────────────────────┘

┌──────────────┐       ┌──────────────────┐       ┌─────────────────┐
│  AudioData    │       │ AudioPoolHandler │       │ ObjectPoolManager│
│  (SO config)  │──────►│ (IPoolable       │──────►│ (pool manager)   │
│  clips[]      │       │  AudioSource)    │       │                  │
│  mixerGroup   │       │  Reset()         │       │  Get/Return      │
│  pitchRange   │       │  auto-return     │       │                  │
│  volume       │       └──────────────────┘       └─────────────────┘
└──────────────┘
```

### Code Roadmap

| File | Purpose |
|------|---------|
| `Audio/AudioData.cs` | ScriptableObject — pure config (clip array, mixer group, pitch range, volume) |
| `Audio/AudioController.cs` | Subscribes to events, plays sounds via pooled one-shots |
| `Audio/AudioPoolHandler.cs` | IPoolable AudioSource wrapper, auto-returns to pool after clip finishes |
| `Core/GameBootstrapper.cs` | Register AudioController in Services |

**Files that require NO changes**: `EnemyController.cs`, `ProjectileBase.cs`, `TowerController.cs`, `PlayerStats.cs` — audio is entirely event-driven.

---

## Step-by-Step Implementation

### Step 1 — Create AudioData ScriptableObject

Create `Audio/AudioData.cs`:

```csharp
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    [CreateAssetMenu(fileName = "AudioData", menuName = "TD/AudioData")]
    public class AudioData : ScriptableObject
    {
        // TODO: public AudioClip[] clips;
        //       Array of clips — Play() picks one at random for variety

        // TODO: public AudioMixerGroup mixerGroup;
        //       Routes sound to the correct mixer group (SFX, Music, Ambient)

        // TODO: public Vector2 pitchRange = new Vector2(0.9f, 1.1f);
        //       Min/max pitch — randomised per play to avoid mechanical repetition

        // TODO: [Range(0f, 1f)] public float volume = 1f;

        // TODO: public bool loop = false;

        // TODO: [Header("Pooling")]
        //       public int prewarmCount = 3;
        //       How many AudioPoolHandler instances to pre-allocate
    }
}
```

AudioData is a **pure data container**. It has no behaviour — no `Play()` method, no `Update()`. It's a config file that other systems read.

### Step 2 — Create AudioPoolHandler

Create `Audio/AudioPoolHandler.cs`:

```csharp
using Interfaces;
using UnityEngine;
using System.Collections;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioPoolHandler : MonoBehaviour, IPoolable
    {
        // TODO: private AudioSource _audioSource;
        // TODO: private float _returnToPoolTime;
        // TODO: private bool _isActive;

        // TODO: public void Play(AudioClip clip, AudioMixerGroup mixerGroup,
        //                         float volume, float pitch, bool loop)
        //       1. Configure _audioSource with clip, mixerGroup, volume, pitch, loop
        //       2. Set _isActive = true
        //       3. _audioSource.Play()
        //       4. If not looping: start coroutine to return to pool after clip.length

        // TODO: public void Stop()
        //       _audioSource.Stop()
        //       Return to pool

        // TODO: IPoolable.Reset() implementation
        //       1. _isActive = false
        //       2. _audioSource.clip = null
        //       3. _audioSource.outputAudioMixerGroup = null
        //       4. _audioSource.Stop()
        //       5. StopAllCoroutines()

        // TODO: private IEnumerator ReturnAfterDelay(float delay)
        //       yield return new WaitForSeconds(delay)
        //       if (_isActive) ReturnToPool()

        // TODO: private void ReturnToPool()
        //       _isActive = false
        //       Services.Get<ObjectPoolManager>().ReturnAudioHandler(gameObject)
    }
}
```

The handler auto-returns to the pool when its clip finishes. This means audio plays don't create garbage — the same `AudioSource` GameObjects are reused forever.

### Step 3 — Register audio pool in ObjectPoolManager

Update `Systems/Managers/ObjectPoolManager.cs`:

```csharp
// TODO: Add a pool for AudioPoolHandler GameObjects
//       private ObjectPool<GameObject> _audioPool;
//
//       In Initialise():
//       Create the pool with createFunc that instantiates
//       the AudioPoolHandler prefab, actionOnGet calls Reset(),
//       actionOnRelease deactivates it, etc.
//
//       public GameObject GetAudioHandler() → _audioPool.Get()
//       public void ReturnAudioHandler(GameObject go) → _audioPool.Release(go)
```

Pre-warm the pool based on the maximum concurrent sounds you expect (start with 10-15).

### Step 4 — Create AudioController

Create `Audio/AudioController.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using Events.Registries;
using Data;
using Core;

namespace Audio
{
    public class AudioController : MonoBehaviour
    {
        // TODO: [SerializeField] private AudioData _enemyDeathSound;
        // TODO: [SerializeField] private AudioData _enemyReachEndSound;
        // TODO: [SerializeField] private AudioData _towerFireSound;
        // TODO: [SerializeField] private AudioData _waveStartSound;
        // TODO: [SerializeField] private AudioData _waveCompleteSound;
        // TODO: [SerializeField] private AudioData _goldEarnedSound;
        // TODO: [SerializeField] private AudioData _gameOverSound;

        // TODO: Dictionary<string, AudioData> _soundLookup;
        //       Built in Awake for fast lookup by sound ID

        // TODO: void Awake()
        //       1. Build _soundLookup dictionary:
        //          {"EnemyDeath", _enemyDeathSound},
        //          {"EnemyReachEnd", _enemyReachEndSound},
        //          etc.
        //       2. Subscribe to event registries:
        //          Services.Get<CombatEvents>().EnemyDied.Subscribe(OnEnemyDied);
        //          Services.Get<CombatEvents>().EnemyReachedEnd.Subscribe(OnEnemyReachedEnd);
        //          Services.Get<CombatEvents>().TowerFired.Subscribe(OnTowerFired);
        //          Services.Get<WaveEvents>().WaveStarted.Subscribe(OnWaveStarted);
        //          Services.Get<WaveEvents>().WaveCompleted.Subscribe(OnWaveCompleted);
        //          Services.Get<EconomyEvents>().GoldChanged.Subscribe(OnGoldChanged);
        //          Services.Get<GameEvents>().GameOver.Subscribe(OnGameOver);

        // TODO: void OnDestroy()
        //       Unsubscribe from ALL events to prevent ghost listeners

        // TODO: private void Play(string soundId)
        //       1. Look up AudioData from _soundLookup
        //       2. If not found, log warning and return
        //       3. Pick random clip from data.clips array
        //       4. Calculate random pitch between data.pitchRange.x and data.pitchRange.y
        //       5. Get pooled AudioPoolHandler from ObjectPoolManager
        //       6. Call handler.Play(clip, data.mixerGroup, data.volume, pitch, data.loop)

        // TODO: Event handler methods — each calls Play() with the right ID:
        //       private void OnEnemyDied(EnemyData _)      → Play("EnemyDeath")
        //       private void OnEnemyReachedEnd(EnemyData _) → Play("EnemyReachEnd")
        //       private void OnTowerFired(TowerData _)     → Play("TowerFire")
        //       private void OnWaveStarted(int _)          → Play("WaveStart")
        //       private void OnWaveCompleted(int _)         → Play("WaveComplete")
        //       private void OnGoldChanged(int _)           → Play("GoldEarned")
        //       private void OnGameOver()                   → Play("GameOver")
    }
}
```

**Key insight**: `AudioController` is the **only** class that knows about audio. `EnemyController` doesn't have an `AudioSource`. `PlayerStats` doesn't play sounds. Audio is fully driven by event subscriptions — any system can raise an event, and audio responds without any cross-dependency.

### Step 5 — Wire AudioController in GameBootstrapper

Update `Core/GameBootstrapper.cs`:

```csharp
// TODO: [SerializeField] AudioController _audioController;
//
//       In Awake():
//       Services.Register(_audioController);
```

### Step 6 — Create AudioData ScriptableObjects

In the Unity Editor:

1. Right-click in Project → Create → TD → AudioData
2. Create one SO for each sound event:
   - `EnemyDeath_AudioData` — assign death clips, SFX mixer, pitch range 0.85-1.15
   - `TowerFire_AudioData` — assign tower shot clips, SFX mixer, pitch range 0.9-1.1
   - `WaveStart_AudioData` — assign wave horn/announce clip, SFX mixer, pitch range 1-1
   - `GoldEarned_AudioData` — assign coin/clink clips, SFX mixer, pitch range 0.95-1.05
3. Assign these SOs to the `AudioController` inspector fields

### Step 7 — Set up Audio Mixer groups

1. Create an Audio Mixer (Assets → Create → Audio Mixer)
2. Add groups: `SFX`, `Music`, `Ambient`
3. Attach the mixer to each `AudioData`'s `mixerGroup` field
4. Now you can control SFX volume, Music volume, etc. independently

---

## Episode Recap

- **Naive**: `AudioSource.PlayClipAtPoint()` everywhere → no pooling, no mixer, same pitch, scattered audio logic
- **Refactor**: `AudioData` SO for pure config → `AudioController` subscribes to events → `AudioPoolHandler` via object pool → auto-return after clip finishes
- Zero gameplay classes know about audio — `EnemyController`, `ProjectileBase`, `TowerController`, `PlayerStats` are untouched
- Pitch randomisation per play makes repeated sounds feel natural
- Mixer routing enables master volume, SFX volume, and music volume controls
- Audio pool pre-warming means zero allocations during gameplay

---

## Challenge

1. Add **footstep audio** for enemies. Create a new `EnemyFootstep` AudioData with a longer pitch range (0.7-1.3), and have `EnemyController` raise a new `CombatEvents.EnemyFootstep` event at regular intervals during movement. `AudioController` subscribes and plays footstep sounds. How does this interact with the UpdateManager from Episode 09?

2. Add **music crossfading**. Create a `MusicController` that subscribes to `WaveEvents.WaveStarted` and `GameEvents.GameOver`, fading between calm and combat music tracks using `AudioMixerSnapshot.TransitionTo()`. Should this use pooled audio or a dedicated `AudioSource`? Why?