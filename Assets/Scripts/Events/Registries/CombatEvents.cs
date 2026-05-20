namespace Events.Registries
{
    // TODO: Combat event registry — events related to enemy damage, death, reaching end of path
    // TODO: Registered in Services via Services.Register<CombatEvents>(new CombatEvents())
    // TODO: Accessed via Services.Get<CombatEvents>()
    // TODO: Clear() called in GameBootstrapper.OnDestroy()
    public class CombatEvents
    {
        // TODO: EnemyDeath — EventChannel<int> (payload = goldReward)
        // TODO: EnemyReachedEnd — EventChannel<int> (payload = damage dealt to player)
        // TODO: EnemyDamaged — EventChannel (void — just a signal)

        // TODO: Clear() — clear all channels to prevent leaked subscriptions on scene unload
    }
}