# Episode 11: Audio Middleware

!!! info "Episode Type: Code Lesson"
    You'll build a data-driven, event-driven audio system using registries and the service locator. ~15-20 min.

---

## Video

<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden;">
  <iframe src="https://www.youtube.com/embed/PLACEHOLDER_EP11" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%;" frameborder="0" allowfullscreen></iframe>
</div>

---

## Learning Objectives

By the end of this episode you will be able to:

1. Build a **data-driven, event-driven** audio system using `AudioData` SOs and event registries
2. Understand why **no singleton** is the right architecture for audio
3. Implement **AudioData SOs** that encapsulate clips, mixer routing, pitch randomization, and 3D settings
4. Create a **pooled one-shot AudioSource** system for SFX without allocation overhead
5. Wire `AudioController` directly to event registries via `Services.Get<T>()` — no intermediary linker class

---

## Key Concepts

| Concept | Summary | Learn More |
|---------|---------|------------|
| AudioData SO | Clips + mixer group + pitch range + 3D settings in one scriptable object | — |
| AudioMixer groups | SFX / Music / UI routing for volume control and snapshot transitions | — |
| Event-driven play | Sounds trigger from event registries, not direct method calls | [Observer Pattern](../Concepts/Observer_Pattern.md) |
| 3D spatial audio | Tower firing uses positional audio at the tower's world position | — |
| Pooled one-shot AudioSource | Pooled `AudioSource` components for SFX, auto-returned after clip ends | [Object Pooling](../Concepts/Object_Pooling.md) |
| No singleton | `AudioController` is a MonoBehaviour event listener, not globally accessible | — |

---

## Code Roadmap

### Files You'll Create

| File | Purpose |
|------|---------|
| `AudioData.cs` | SO — defines clip array, mixer group, pitch range, 3D settings |
| `AudioPoolHandler.cs` | Pooled AudioSource — plays one-shot, auto-returns to pool |
| `AudioController.cs` | MonoBehaviour — subscribes to registries, plays audio via pool |

### Files You'll Reference

| File | Why |
|------|-----|
| `Events/Registries/CombatEvents.cs` | `EnemyDeath`, `EnemyReachedEnd` — combat sounds |
| `Events/Registries/GameEvents.cs` | `TowerPlaced`, `GamePaused` — game sounds |
| `Core/Services.cs` | `Services.Get<CombatEvents>()`, `Services.Get<ObjectPoolManager>()` |

### Prerequisites

- Episode 08 event system complete — `EventChannel`, registries, `Services` must be working
- Object pooling system operational — `ObjectPoolManager` must provide pool functionality (see [Object Pooling](../Concepts/Object_Pooling.md))
- AudioMixer asset created in Unity with SFX, Music, and UI groups

---

## Architecture Context

```text
Game Events via Services          AudioController                Pool
────────────────────            ┌──────────────┐            ┌──────────────┐
Services.Get<CombatEvents>()   │              │            │              │
  .EnemyDeath ────────────────→│ HandleDeath  │            │ObjectPool    │
                                │              │──── Get ──→│ Manager      │
Services.Get<GameEvents>()      │ HandlePlace  │            │              │
  .TowerPlaced ───────────────→│              │←── Return ─│              │
                                │              │            └──────────────┘
                          ┌─────┤              │
                          │     └──────────────┘
                          ▼
                   AudioData SO (inspector fields)
                   (death sound)  (build sound)  (fire sound)
                          │
                          ▼
                   AudioPoolHandler (from pool)
                          │
                          ▼
                   AudioSource ──→ AudioMixer Group ──→ Output
                                  (SFX/Music/UI)
```

**Flow:** Game code raises events on registries. `AudioController` subscribes in `OnEnable`, listens for events, fetches an `AudioPoolHandler` from the pool via `Services.Get<ObjectPoolManager>()`, configures it from the matching `AudioData` field, and plays. The pool handler auto-returns when the clip finishes.

No game code ever calls `AudioController.Play()` directly. No game code references `AudioData` directly. The entire audio pipeline is wired through event registries and inspector-configured SOs.

---

## Step-by-Step Implementation Guide

### Step 1: Architecture Decision — No Singleton

The audio system does **not** use a singleton pattern. There is no `AudioController.Instance`.

**Why:**

- Singletons create hidden global coupling — any code can call `AudioManager.PlaySFX()` at any time
- Event-driven design means game code raises events; audio *responds* to events
- `AudioController` is a **listener**, not a service locator
- If you need positional audio, the caller already has position context — pass it through the event, not a global lookup

**What `AudioController` is:**

- A `MonoBehaviour` on a GameObject in the scene
- Subscribes to event registries in `OnEnable`, unsubscribes in `OnDisable`
- Accesses the pool via `Services.Get<ObjectPoolManager>()`

### Step 2: AudioData SO

`AudioData` encapsulates everything needed to play a sound. No game code configures `AudioSource` properties — the SO does it.

```csharp
[CreateAssetMenu(menuName = "Audio/AudioData")]
public class AudioData : ScriptableObject
{
    public AudioClip[] clips;
    public AudioMixerGroup outputGroup;
    public float volume = 1f;
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    public bool is3D;
    [Range(0f, 1f)] public float spatialBlend = 1f;
    public int priority = 128;

    public AudioClip GetRandomClip()
    {
        // TODO: Return random clip from clips array
        // TODO: If clips.Length == 0, return null
    }

    public float GetRandomPitch()
    {
        // TODO: Return Random.Range(pitchRange.x, pitchRange.y)
    }
}
```

**Field breakdown:**

| Field | Purpose |
|-------|---------|
| `clips` | Array for variety — random selection per play avoids repetitive feel |
| `outputGroup` | Routes to SFX, Music, or UI mixer group |
| `volume` | Per-sound volume (mixer group controls master volume) |
| `pitchRange` | Subtle randomization — `0.95` to `1.05` keeps sounds natural |
| `is3D` | Flag for spatial audio (tower firing vs UI clicks) |
| `spatialBlend` | `0` = fully 2D, `1` = fully 3D |
| `priority` | Voice limiting — lower values = higher priority when voices are full |

!!! tip "Pitch Randomization"
    Slightly randomizing pitch (0.95–1.05) makes repeated sounds feel natural. The human ear detects exact repetition quickly — even ±5% variation breaks the pattern. Too wide a range (e.g. 0.8–1.2) sounds wrong — keep it subtle.

### Step 3: AudioPoolHandler — Pooled One-Shot AudioSource

Each SFX play grabs an `AudioPoolHandler` from the object pool, configures it from `AudioData`, plays, and auto-returns.

```csharp
public class AudioPoolHandler : MonoBehaviour
{
    private AudioSource _audioSource;

    private void Awake()
    {
        // TODO: Get or add AudioSource component
    }

    public void Play(AudioData data, Vector3 position)
    {
        // TODO: Configure _audioSource from AudioData:
        //   clip = data.GetRandomClip()
        //   outputAudioMixerGroup = data.outputGroup
        //   volume = data.volume
        //   pitch = data.GetRandomPitch()
        //   priority = data.priority
        //   spatialBlend = data.is3D ? data.spatialBlend : 0f
        //   Set position if 3D
        //   Play()
        //   Schedule ReturnToPool after clip length
    }

    public void ReturnToPool()
    {
        // TODO: Stop AudioSource
        // TODO: Reset AudioSource state
        // TODO: Return to Services.Get<ObjectPoolManager>()
    }

    private void Reset()
    {
        // TODO: Clear clip, reset volume/pitch/spatialBlend to defaults
    }
}
```

**Lifecycle:**

1. `AudioController` fetches `AudioPoolHandler` from pool via `Services.Get<ObjectPoolManager>()`
2. Calls `Play(audioData, position)` — configures and starts playback
3. After clip length, `ReturnToPool()` fires — stops source, resets state, returns to pool

No `Instantiate`/`Destroy` per SFX. No growing AudioSource count. The pool reuses the same handlers.

### Step 4: AudioController — Direct Registry Subscriptions

`AudioController` subscribes directly to event registries — no intermediary linker class needed.

```csharp
public class AudioController : MonoBehaviour
{
    [Header("Combat Sounds")]
    [SerializeField] private AudioData enemyDeathSFX;
    [SerializeField] private AudioData enemyReachedEndSFX;

    [Header("Game Sounds")]
    [SerializeField] private AudioData towerPlacedSFX;
    [SerializeField] private AudioData gamePausedSFX;

    private void OnEnable()
    {
        // TODO: Services.Get<CombatEvents>().EnemyDeath.Subscribe(OnEnemyDeath);
        // TODO: Services.Get<CombatEvents>().EnemyReachedEnd.Subscribe(OnEnemyReachedEnd);
        // TODO: Services.Get<GameEvents>().TowerPlaced.Subscribe(OnTowerPlaced);
        // TODO: Services.Get<GameEvents>().GamePaused.Subscribe(OnGamePaused);
    }

    private void OnDisable()
    {
        // TODO: Services.Get<CombatEvents>().EnemyDeath.Unsubscribe(OnEnemyDeath);
        // TODO: Services.Get<CombatEvents>().EnemyReachedEnd.Unsubscribe(OnEnemyReachedEnd);
        // TODO: Services.Get<GameEvents>().TowerPlaced.Unsubscribe(OnTowerPlaced);
        // TODO: Services.Get<GameEvents>().GamePaused.Unsubscribe(OnGamePaused);
    }

    private void OnEnemyDeath(int goldReward)
    {
        // TODO: PlayAudio2D(enemyDeathSFX)
    }

    private void OnEnemyReachedEnd()
    {
        // TODO: PlayAudio2D(enemyReachedEndSFX)
    }

    private void OnTowerPlaced()
    {
        // TODO: PlayAudio2D(towerPlacedSFX)
    }

    private void OnGamePaused()
    {
        // TODO: PlayAudio2D(gamePausedSFX)
    }

    private void PlayAudio(AudioData data, Vector3 position)
    {
        // TODO: Fetch AudioPoolHandler from Services.Get<ObjectPoolManager>()
        // TODO: Call handler.Play(data, position)
    }

    private void PlayAudio2D(AudioData data)
    {
        // TODO: Fetch AudioPoolHandler from Services.Get<ObjectPoolManager>()
        // TODO: Call handler.Play(data, Vector3.zero) with 2D config
    }
}
```

**Key points:**

- Each `[SerializeField] AudioData` field maps directly to a specific event — inspector wiring of *which sound plays*
- Subscriptions are explicit — you can see exactly which events trigger which sounds
- Pool access via `Services.Get<ObjectPoolManager>()` — no singleton reference needed
- The controller is a **subscriber**, not a global service
- No `AudioController.Instance` — game code never references this class

### Step 5: AudioMixer Setup (Unity Editor)

Create the mixer and route groups:

1. **Create AudioMixer** asset (Right-click → Create → Audio → Audio Mixer)
2. **Add three groups:**
   - `SFX` — game sound effects (enemy deaths, tower shots, projectile impacts)
   - `Music` — background music tracks
   - `UI` — button clicks, panel opens, notification sounds
3. **Each `AudioData` SO** references the correct group via `outputGroup`
4. **Expose Volume parameters** — right-click the Volume property on each group → "Expose to script". This lets UI sliders control group volume at runtime.
5. **Create Snapshots** (optional, see challenge) — e.g. "Menu" and "Combat" snapshots for music crossfade

### Step 6: Wire Examples

Connect game events to audio through `AudioController` inspector fields:

| Game Event | AudioData Field | Mixer Group | 2D/3D | Notes |
|---|---|---|---|---|
| `CombatEvents.EnemyDeath` | `enemyDeathSFX` | SFX | 2D | Random clip from array, pitch varied |
| `CombatEvents.EnemyReachedEnd` | `enemyReachedEndSFX` | SFX | 2D | Alert sound |
| `GameEvents.TowerPlaced` | `towerPlacedSFX` | SFX | 2D | Confirmation sound |
| `GameEvents.GamePaused` | `gamePausedSFX` | UI | 2D | Short UI feedback |

!!! warning "3D Audio needs position data"
    Tower firing sounds should be 3D spatial. `CombatEvents.EnemyDeath` carries an `int` (gold), not a position. Options:
    
    1. Add a `TypedEventChannel<Vector3>` to a registry for positional audio events
    2. Add a separate `PlayAudio(AudioData, Vector3)` method on `AudioController` that tower code calls directly (breaks purity but practical)
    3. Create a struct with event data + position (e.g. `TowerFiredReport`), sent via a typed channel
    
    Option 3 is the cleanest — it maintains event-driven design while carrying the position data the audio system needs.

---

## Episode Recap

- Built a **data-driven audio system** — `AudioData` SOs encapsulate all playback settings
- Built an **event-driven pipeline** — `AudioController` subscribes directly to registries via `Services.Get<T>()`
- Implemented **pooled one-shot** `AudioPoolHandler` — no instantiate/destroy per SFX
- `AudioController` is a **listener, not a singleton** — game code never references it directly
- `[SerializeField] AudioData` fields map events to sounds — pure inspector wiring, no code per sound
- Pool access via `Services.Get<ObjectPoolManager>()` — no singleton reference on AudioController
- Routed audio through **AudioMixer groups** (SFX/Music/UI) for volume control

---

## Challenge

Design a **music crossfade system** using AudioMixer Snapshots.

1. Create two snapshots: **"Menu"** (Music at 0dB, SFX attenuated) and **"Combat"** (Music at -3dB, SFX at 0dB)
2. What events from `WaveEvents` would trigger the transition? (e.g. `WaveStarted` → Combat, `WaveCompleted` → Menu)
3. How would you blend between snapshots? `AudioMixer.TransitionToSnapshots()` takes an array and weights — design the transition curve.
4. Should the crossfade be managed by `AudioController`, or a separate `MusicController`?

!!! info "Snapshot transitions"
    `AudioMixer.TransitionToSnapshots()` accepts an array of snapshots and a weight array. To crossfade, transition from the current snapshot to the target with a timed blend. The `AudioMixerGroup` volume is controlled per-snapshot — you can attenuate groups independently in each snapshot.