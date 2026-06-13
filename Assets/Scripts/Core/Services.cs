using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public static class Services
    {
        // TODO: Episode 09 — Static service locator
        // Dictionary<Type, object>, Register<T>, Get<T>, Clear

        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T instance) where T : class
        {
            if (instance == null)
            {
                Debug.LogError($"Services: Tried to register null for {typeof(T).Name}");
                return;
            }

            if (_services.ContainsKey(typeof(T)))
                Debug.LogWarning($"Services: Overwriting existing registration for {typeof(T).Name}");

            _services[typeof(T)] = instance;
        }

        public static T Get<T>() where T : class
        {
            if (!_services.TryGetValue(typeof(T), out var service))
            {
                Debug.LogError($"Services: No {typeof(T).Name} registered");
                return null;
            }

            return service as T;
        }

        public static void Unregister<T>() => _services.Remove(typeof(T));

        public static void Clear() => _services.Clear();

    }
}