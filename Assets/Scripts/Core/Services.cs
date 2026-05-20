using System.Collections.Generic;

namespace Core
{
    // TODO: Simple service locator — Register services in GameBootstrapper.Awake()
    // TODO: Access anywhere via Services.Get<T>()
    // TODO: Call Services.Clear() in GameBootstrapper.OnDestroy() to clean up
    public static class Services
    {
        // TODO: Dictionary<Type, object> to store registered services

        // TODO: Register<T>(T service) where T : class — add to dictionary

        // TODO: Get<T>() where T : class — retrieve from dictionary, cast and return

        // TODO: Clear() — clear dictionary on scene unload / app quit
    }
}