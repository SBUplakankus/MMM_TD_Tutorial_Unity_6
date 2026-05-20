# Episode 11: Audio Middleware — Implementation Guide

## What You're Building

A pooled one-shot audio system driven by ScriptableObject data and direct registry subscription. Game code raises events on registry channels (e.g. `Services.Get<CombatEvents>().EnemyDeath.Raise(gold)`), and AudioController listens and plays the matching AudioData through a pooled AudioSource. No AudioEventLinker SOs, no VoidEventChannel SOs — the mapping is code-based via `[SerializeField]` AudioData fields on the AudioController, and subscriptions go directly to the event registries accessed through the service locator.

---

## Files & Order

1. `Assets/Scripts/Data/AudioData.cs` — complete the TODOs (pure config SO, references Unity assets)
2. `Assets/Scripts/Audio/AudioPoolHandler.cs` — complete the TODOs (update pool access to use Services)
3. `Assets/Scripts/Systems/Game/AudioController.cs` — complete the TODOs (subscribe directly to registries)

---

## Implementation

### 1. AudioData.cs — complete

No structural changes. This stays as a ScriptableObject because it references `AudioClip` and `AudioMixerGroup` — Unity assets that can't live in a plain C# class. Complete the two TODO helper methods.

```csharp
using UnityEngine;
using UnityEngine.Audio;

namespace Data
{
    [CreateAssetMenu(fileName = "AudioData", menuName = "Scriptable Objects/Data/Audio")]
    public class AudioData : ScriptableObject
    {
        #region Fields

        [Header("Audio Clips")]
        [SerializeField] private AudioClip[] clips;

        [Header("Mixing")]
        [SerializeField] private AudioMixerGroup outputGroup;
        [SerializeField] private float volume = 1f;

        [Header("Pitch Randomization")]
        [SerializeField] private Vector2 pitchRange = new(0.95f, 1.05f);

        [Header("Spatial Settings")]
        [SerializeField] private bool is3D;
        [SerializeField] [Range(0f, 1f)] private float spatialBlend = 1f;

        [Header("Priority")]
        [SerializeField] private int priority = 128;

        #endregion

        #region Properties

        public AudioClip[] Clips => clips;
        public AudioMixerGroup OutputGroup => outputGroup;
        public float Volume => volume;
        public Vector2 PitchRange => pitchRange;
        public bool Is3D => is3D;
        public float SpatialBlend => spatialBlend;
        public int Priority => priority;

        #endregion

        #region Helpers

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        public float GetRandomPitch()
        {
            return Random.Range(pitchRange.x, pitchRange.y);
        }

        #endregion
    }
}
```

**What changed from old version:** Nothing. AudioData is pure config — it holds no event references and has no awareness of the event system. It's unchanged because the architectural shift (removing VoidEventChannel SOs, removing AudioEventLinker SOs) doesn't affect a pure data container.

---

### 2. AudioPoolHandler.cs — complete

The key change: pool access now uses `Services.Get<ObjectPoolManager>()` instead of `ObjectPoolManager.Instance`. This class also implements `IPoolable` so the pool manager can call `Reset()` when the object returns.

```csharp
using Core;
using Data;
using Interfaces;
using Systems.Managers;
using UnityEngine;

namespace Audio
{
    public class AudioPoolHandler : MonoBehaviour, IPoolable
    {
        #region Fields

        private AudioSource _audioSource;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
        }

        #endregion

        #region Public API

        public void Play(AudioData data, Vector3 position)
        {
            transform.position = position;

            _audioSource.clip = data.GetRandomClip();
            if (_audioSource.clip == null) return;

            _audioSource.outputAudioMixerGroup = data.OutputGroup;
            _audioSource.volume = data.Volume;
            _audioSource.pitch = data.GetRandomPitch();
            _audioSource.spatialBlend = data.Is3D ? data.SpatialBlend : 0f;
            _audioSource.priority = data.Priority;

            _audioSource.Play();

            Services.Get<ObjectPoolManager>().ReturnDelayed("audioSource", gameObject,
                _audioSource.clip.length + 0.1f);
        }

        public void ReturnToPool()
        {
            if (_audioSource != null && _audioSource.isPlaying)
                _audioSource.Stop();

            Services.Get<ObjectPoolManager>().Return("audioSource", gameObject);
        }

        #endregion

        #region IPoolable

        public void Reset()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.clip = null;
                _audioSource.volume = 1f;
                _audioSource.pitch = 1f;
            }
        }

        #endregion
    }
}
```

**Key decisions:**

- `Play` uses `Services.Get<ObjectPoolManager>().ReturnDelayed` with clip length + 0.1s buffer. The AudioSource plays to completion, then the pool manager calls `Return()` which calls `IPoolable.Reset()`, which clears the clip. No coroutines in this class.
- If `GetRandomClip` returns null (no clips assigned), `Play` exits early — no crash, no sound, just a silent skip.
- `ReturnToPool` is a manual return path if you need to cut a sound short (e.g. scene transition). The normal flow relies on `ReturnDelayed`.
- `Reset` (IPoolable) is called by ObjectPoolManager when the object is returned to the pool. Cleans state so the next Play starts fresh.
- **Changed from old version:** All `ObjectPoolManager.Instance` calls replaced with `Services.Get<ObjectPoolManager>()`. Added `using Core;` import. Implemented `IPoolable` interface explicitly.

---

### 3. AudioController.cs — complete

This is the big architectural change. Instead of wiring AudioEventLinker SOs, AudioController subscribes directly to event channels on the CombatEvents and GameEvents registries, accessed via `Services.Get<T>()`. The AudioData-to-event mapping is code-based via `[SerializeField]` fields.

```csharp
using Core;
using Data;
using Events.Registries;
using Systems.Managers;
using UnityEngine;

namespace Systems.Game
{
    public class AudioController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private AudioData enemyDeathAudio;
        [SerializeField] private AudioData towerPlacedAudio;
        [SerializeField] private AudioData uiClickAudio;

        private EventChannel<int> _enemyDeathChannel;
        private EventChannel _towerPlacedChannel;

        private Action<int> _enemyDeathHandler;
        private Action _towerPlacedHandler;

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            var combat = Services.Get<CombatEvents>();
            _enemyDeathChannel = combat.EnemyDeath;
            _enemyDeathHandler = OnEnemyDeath;
            _enemyDeathChannel.Subscribe(_enemyDeathHandler);

            var game = Services.Get<GameEvents>();
            _towerPlacedChannel = game.TowerPlaced;
            _towerPlacedHandler = OnTowerPlaced;
            _towerPlacedChannel.Subscribe(_towerPlacedHandler);
        }

        private void OnDisable()
        {
            _enemyDeathChannel.Unsubscribe(_enemyDeathHandler);
            _towerPlacedChannel.Unsubscribe(_towerPlacedHandler);
        }

        #endregion

        #region Private Methods

        private void OnEnemyDeath(int goldReward)
        {
            PlayAudio2D(enemyDeathAudio);
        }

        private void OnTowerPlaced()
        {
            PlayAudio2D(towerPlacedAudio);
        }

        #endregion

        #region Public API

        public void PlayAudio(AudioData data, Vector3 position)
        {
            var obj = Services.Get<ObjectPoolManager>().Get("audioSource", position, Quaternion.identity);
            var handler = obj.GetComponent<AudioPoolHandler>();
            handler.Play(data, position);
        }

        public void PlayAudio2D(AudioData data)
        {
            PlayAudio(data, Vector3.zero);
        }

        #endregion
    }
}
```

**Key decisions:**

- **NOT a singleton.** This is a MonoBehaviour on a scene GameObject. Audio is scene-scoped. If you need persistent audio across scene loads, wrap it separately — don't bake it into this controller.

- **Store handler delegates as fields.** The `_enemyDeathHandler` and `_towerPlacedHandler` fields ensure we subscribe and unsubscribe the *same* delegate. If you used lambdas inline (`combat.EnemyDeath.Subscribe(gold => OnEnemyDeath(gold))`), each call creates a new delegate instance and `Unsubscribe` won't find the original. Storing the delegate as a field fixes this.

- **Store channel references.** `_enemyDeathChannel` and `_towerPlacedChannel` are cached so `OnDisable` can unsubscribe without calling `Services.Get` again (the service might already be cleared during scene teardown).

- **PlayAudio2D for event-driven sounds.** When events fire (enemy death, tower placed), the AudioController plays them 2D (spatialBlend = 0). For 3D positional audio, game code should call `PlayAudio(data, position)` directly. This split keeps the event channels simple (void or payload-only, no position data) while still supporting positional audio when needed.

- **AudioData fields are inspector-wired.** Drag the AudioData SOs onto the AudioController in the inspector. This replaces the old AudioEventLinker SOs — simpler, fewer moving parts, one fewer SO type to maintain.

- **Changed from old version:** Removed `AudioEventLinker[]` array and `Dictionary<VoidEventChannel, AudioData>`. Removed `List<VoidEventChannel>` subscription tracking. Replaced with direct registry subscription via `Services.Get<T>()`. Pool access changed from `ObjectPoolManager.Instance` to `Services.Get<ObjectPoolManager>()`.

---

## Unity Editor Setup

### Step 1: Create AudioMixer

1. Right-click in `Assets/Audio/` → **Create → Audio Mixer**
2. Name it `GameMixer`
3. Open the mixer, add three groups:
   - **SFX** — for gameplay sounds (tower fire, enemy death, projectile hit)
   - **Music** — for background music
   - **UI** — for button clicks, shop sounds
4. Right-click the Volume parameter on each group → **Expose Volume**
5. Rename exposed parameters in the mixer inspector: `SFXVolume`, `MusicVolume`, `UIVolume`

### Step 2: Create AudioData ScriptableObjects

Create these in `Assets/ScriptableObjects/Audio/` (or wherever you organize SOs):

| AudioData Name | Clips | Output Group | Volume | Pitch Range | Is3D | Spatial-Blend | Priority |
|---|---|---|---|---|---|---|---|
| SFX_EnemyDeath | death_01, death_02, death_03 | SFX | 0.8 | (0.9, 1.1) | false | 0 | 128 |
| SFX_TowerFire | fire_01, fire_02 | SFX | 0.7 | (0.95, 1.05) | false | 0 | 100 |
| SFX_ProjectileHit | hit_01, hit_02, hit_03 | SFX | 0.5 | (0.9, 1.1) | false | 0 | 128 |
| UI_ButtonClick | click_01 | UI | 1.0 | (0.98, 1.02) | false | 0 | 50 |
| UI_ShopPurchase | purchase_01, purchase_02 | UI | 0.9 | (0.95, 1.05) | false | 0 | 50 |
| Music_Gameplay | bgm_01 | Music | 0.3 | (1.0, 1.0) | false | 0 | 0 |

### Step 3: Create AudioPoolHandler Prefab

1. Create empty GameObject, name it `AudioPoolHandler`
2. Add `AudioPoolHandler` component (script already adds AudioSource in Awake if missing)
3. Add `AudioSource` component manually if you want to set defaults in inspector (optional — Awake will add it)
4. Drag into `Assets/Prefabs/` to make a prefab
5. Delete from scene

### Step 4: Register Pool in ObjectPoolManager

On your ObjectPoolManager scene object, add a PoolConfig entry:

| Key | Prefab | Default Size | Max Size |
|---|---|---|---|
| audioSource | AudioPoolHandler prefab | 10 | 30 |

### Step 5: Add AudioController to Scene

1. Create empty GameObject named `AudioController`
2. Add `AudioController` component
3. In the inspector, drag AudioData SOs onto the fields:
   - **Enemy Death Audio** → SFX_EnemyDeath
   - **Tower Placed Audio** → SFX_TowerFire (or a dedicated SFX_TowerPlaced if you have one)
   - **UI Click Audio** → UI_ButtonClick

### Step 6: Verify Services Registration

Open `GameBootstrapper` and confirm these services are registered in Awake:

```csharp
Services.Register<ObjectPoolManager>(objectPoolManager);
Services.Register<CombatEvents>(new CombatEvents());
Services.Register<GameEvents>(new GameEvents());
```

And cleared in OnDestroy:

```csharp
Services.Get<CombatEvents>().Clear();
Services.Get<GameEvents>().Clear();
Services.Clear();
```

If `CombatEvents` or `GameEvents` isn't registered, `AudioController.OnEnable()` will throw `KeyNotFoundException` when it calls `Services.Get<T>()`.

---

## Test Plan

| # | Test | Expected Result |
|---|---|---|
| 1 | Play mode, enemy dies (EnemyDeath raised with gold value) | SFX_EnemyDeath plays with random pitch |
| 2 | Play mode, tower placed (TowerPlaced raised) | Tower placed sound plays, routed to SFX mixer group |
| 3 | Rapid-fire 10 events quickly | 10 pooled AudioSources play simultaneously, no clipping if mixer is set |
| 4 | After sounds finish | AudioSources auto-return to pool (check ObjectPoolManager active count) |
| 5 | Call `PlayAudio(data, position)` with 3D AudioData | Sound plays at position, spatial blend = 1 |
| 6 | Call `PlayAudio2D(data)` | Sound plays at zero, spatial blend = 0 |
| 7 | Disable AudioController (OnDisable) | All event subscriptions removed, no stale listeners |
| 8 | Re-enable AudioController | Subscriptions rebuild, sounds play normally |
| 9 | AudioData with empty clips array | `Play` exits silently, no crash |
| 10 | AudioMixer SFX group volume to -80dB | No SFX sounds audible, UI still plays |
| 11 | Services.Get<CombatEvents>() before GameBootstrapper runs | KeyNotFoundException — AudioController must spawn after bootstrapper |

---

## Debugging Tips

**`KeyNotFoundException` when accessing `Services.Get<CombatEvents>()`:**
- The registry wasn't registered in GameBootstrapper.Awake(). Add `Services.Register<CombatEvents>(new CombatEvents())` to GameBootstrapper.
- AudioController's OnEnable runs before GameBootstrapper's Awake. Check script execution order — GameBootstrapper must initialize first, or place AudioController on a child that activates after the bootstrapper.

**Sound doesn't play but no errors:**
- Check if the AudioData field is assigned on AudioController. An unassigned `[SerializeField]` is silently null.
- Verify AudioMixer groups are assigned in AudioData SOs. An ungrouped AudioSource goes to master output but may be silent if the mixer is bypassed.
- Check `AudioListener` exists in the scene (usually on Main Camera).

**Sound plays multiple times or leaks between scenes:**
- Forgot to unsubscribe in OnDisable. Verify `_enemyDeathChannel.Unsubscribe(_enemyDeathHandler)` is actually called.
- EventChannel.Clear() not called during scene teardown. Verify GameBootstrapper.OnDestroy calls `Clear()` on all registries.

**3D sound comes from wrong position:**
- Verify `AudioPoolHandler.Play` sets `transform.position` to the passed position *before* calling `_audioSource.Play()`.
- Check that the AudioData SO has `Is3D = true` and `SpatialBlend = 1`. If these are false/zero, the sound plays 2D regardless of position.

**No sound but handler is called:**
- `GetRandomClip()` returned null (empty clips array on the AudioData). Put a `Debug.Log` before the null check in `AudioPoolHandler.Play` to confirm.
- Pool key mismatch — the string `"audioSource"` must match exactly between `AudioController.PlayAudio`, `AudioPoolHandler.ReturnToPool`, and the PoolConfig on ObjectPoolManager.

**AudioSource not returning to pool:**
- `ReturnDelayed` relies on ObjectPoolManager's coroutine. If ObjectPoolManager is destroyed (scene load) before the delay fires, the coroutine dies silently. For scene transitions, consider manually calling `ReturnToPool` on all active pool objects first.

**Sounds play but are cut off early:**
- `ReturnDelayed` uses `clip.length + 0.1f`. If pitch is very low, actual play time exceeds the scheduled return. Increase the buffer (0.1f → 0.3f) if needed.