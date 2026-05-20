using Events.Registries;
using Systems.Game;
using Systems.Managers;
using UnityEngine;

namespace Core
{
    // TODO: Composition root — the ONE place where all services are wired together
    // TODO: Add this MonoBehaviour to a GameObject in your scene
    // TODO: In Awake(): register all services and event registries with Services.Register<T>()
    // TODO: In OnDestroy(): call Clear() on all event registries, then Services.Clear()
    public class GameBootstrapper : MonoBehaviour
    {
        // TODO: Serialized references to scene MonoBehaviours (pool manager, wave manager, audio controller, etc.)
        // TODO: In Awake(): create PlayerStats, register all services, initialize systems that need explicit wiring
        // TODO: In OnDestroy(): clear all event registries to prevent leaked subscriptions, then Services.Clear()
    }
}