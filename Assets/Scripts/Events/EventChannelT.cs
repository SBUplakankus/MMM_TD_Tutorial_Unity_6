using System;

namespace Events
{
    // TODO: Typed event channel — carries a payload of type T when raised
    // TODO: Same lifecycle as EventChannel but with Action<T> instead of Action
    // TODO: Subscribe/Unsubscribe in OnEnable/OnDisable on MonoBehaviour listeners
    // TODO: Call Clear() on scene transition to prevent leaked subscriptions
    public class EventChannel<T>
    {
        // TODO: private event Action<T> Handlers
        // TODO: Raise(T value) => Handlers?.Invoke(value)
        // TODO: Subscribe(Action<T> handler) => Handlers += handler
        // TODO: Unsubscribe(Action<T> handler) => Handlers -= handler
        // TODO: Clear() => Handlers = null
    }
}