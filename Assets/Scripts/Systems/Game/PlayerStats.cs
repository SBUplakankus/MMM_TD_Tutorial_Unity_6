using UnityEngine;

namespace Systems.Game
{
    public class PlayerStats : MonoBehaviour
    {
        // TODO: Episode 05 — Gold and lives tracker, MonoBehaviour singleton for now
        // Track Gold and Lives with C# events (OnGoldChanged, OnLivesChanged)
        // Die() calls AddGold, OnReachedEnd() calls SubtractLives
        
        [SerializeField] private int startingGold = 100;
        [SerializeField] private int startingLives = 20;

        public int Gold { get; private set; }
        public int Lives { get; private set; }

        private void Awake()
        {
            Gold = startingGold;
            Lives = startingLives;
        }
        
        public void AddGold(int amount) => Gold += amount;
        public void AddLives(int amount) => Lives += amount;
        public void RemoveGold(int amount) => Gold -= amount;
        public void RemoveLives(int amount) => Lives -= amount;

        // TODO: Episode 09 — Convert to plain C# class, remove Instance, use Services
        // TODO: Episode 10 — Subscribe to CombatEvents in Initialize/Cleanup
    }
}