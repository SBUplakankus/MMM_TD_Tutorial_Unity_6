# Service Locator

## Overview
A global registry where systems register themselves, and other systems look them up by type. Access via `Services.Get<T>()`.

## Why Use It
- One consistent access pattern for managers and event registries
- No multi-singleton sprawl (one Service Locator instead of N .Instance properties)
- Easy to swap implementations for testing

## Tradeoffs
- **Hides dependencies** — you can't tell what a class needs from its API
- **Runtime errors** — KeyNotFoundException if you forget to register a service
- **Tight coupling to the locator** — every system depends on the static Services class

## When Not to Use
- Small projects where direct references are sufficient
- When you need compile-time dependency guarantees (use DI instead)
- When constructor injection is practical (see [Upgrade Path](#upgrade-path))

## In This Project
- `Services` static class — Register, Get, Clear
- `GameBootstrapper` — registers all services in Awake, clears in OnDestroy
- All access: `Services.Get<ObjectPoolManager>()`, `Services.Get<CombatEvents>()`

## Code Example
```csharp
public static class Services
{
    private static readonly Dictionary<Type, object> _services = new();
    
    public static void Register<T>(T service) where T : class => _services[typeof(T)] = service;
    public static T Get<T>() where T : class => (T)_services[typeof(T)];
    public static void Clear() => _services.Clear();
}
```

## Upgrade Path
Service Locator → Manual DI → DI Container (VContainer)
- **Manual DI**: `enemySpawner.Initialize(poolManager)` — explicit dependencies
- **VContainer**: automatic constructor injection, lifetimes, scoping

Related: [Observer Pattern](Observer_Pattern.md)