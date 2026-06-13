using Systems.Managers;
using UnityEngine;

namespace Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        // TODO: Episode 09 — Composition root: register all services in Awake, Clear in OnDestroy
        // TODO: Episode 10 — Register CombatEvents + EconomyEvents
        
        [SerializeField] private GameUpdateManager gameUpdateManager;
        [SerializeField] private ObjectPoolManager objectPoolManager;

        private void Awake()
        {
            
            Services.Register(gameUpdateManager);
            Services.Register(objectPoolManager);
        }

        private void OnDestroy() => Services.Clear();
    }
}