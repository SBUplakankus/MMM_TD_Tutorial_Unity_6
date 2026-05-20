using System;

namespace Events
{
    // TODO: Void event channel — no payload, just a signal that something happened
    // TODO: Subscribe/Unsubscribe in OnEnable/OnDisable on MonoBehaviour listeners
    // TODO: Call Clear() on scene transition to prevent leaked subscriptions
    public class EventChannel
    {
        // TODO: private event Action Handlers
        // TODO: Raise() => Handlers?.Invoke()
        // TODO: Subscribe(Action handler) => Handlers += handler
        // TODO: Unsubscribe(Action handler) => Handlers -= handler
        // TODO: Clear() => Handlers = null
    }
}