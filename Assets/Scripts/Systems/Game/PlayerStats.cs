namespace Systems.Game
{
    // TODO: Plain C# class — no MonoBehaviour
    // TODO: Created in GameBootstrapper and registered in Services
    // TODO: Holds Gold, Lives
    // TODO: Constructor or Initialize(int startingGold, int startingLives)
    // TODO: AddGold(int) — adds gold, raises EconomyEvents.GoldChanged
    // TODO: RemoveGold(int) — subtracts gold if sufficient, raises EconomyEvents.GoldChanged
    // TODO: LoseLife() — decrements lives, raises EconomyEvents.LivesChanged
    // TODO: Subscribe to CombatEvents.EnemyDeath in Initialize — adds gold from reward
    // TODO: Subscribe to CombatEvents.EnemyReachedEnd in Initialize — loses a life
    // TODO: Unsubscribe in a Cleanup() method called from GameBootstrapper.OnDestroy()
    public class PlayerStats
    {
        // TODO: Implement
    }
}