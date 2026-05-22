# Course Lessons

| # | Title | Key Concepts |
|---|-------|-------------|
| 1 | [Enemy Movement](Lessons/01_Enemy_Movement.md) | EnemyPath, waypoint movement, basic EnemyController |
| 2 | [Interfaces](Lessons/02_Interfaces.md) | IDamageable, ITargetable, contracts vs implementations |
| 3 | [Strategy Pattern + Factory](Lessons/03_Strategy_Pattern_Factory.md) | IMovementStrategy, IHealthStrategy, StrategyFactory, HealthConfig/MovementConfig SOs |
| 4 | [Health & Damage](Lessons/04_Health_Damage.md) | NormalHealth, DamageResult, TakeDamage loop, EnemyHealthBar |
| 5 | [Enemy Type Composition](Lessons/05_Enemy_Type_Composition.md) | ArmouredHealth, FlyingPath, EnemyData SO combos, zero-change extensibility |
| 6 | [Targeting System](Lessons/06_Targeting_System.md) | ITargetingStrategy, First/Last/Strong/Close, TargetPriority |
| 7 | [Projectiles](Lessons/07_Projectiles.md) | ProjectileBase, ArrowProjectile, TowerFiring, cooldown system |
| 8 | [Object Pooling](Lessons/08_Object_Pooling.md) | Unity ObjectPool<T>, IPoolable, pre-warming, GC spike elimination |
| 9 | [Service Locator](Lessons/09_Service_Locator.md) | Services, GameBootstrapper, PlayerStats as plain C# |
| 10 | [Event Channels](Lessons/10_Event_Channels.md) | EventChannel<T>, registries, subscribe/unsubscribe lifecycle |
| 11 | [Wave System](Lessons/11_Wave_System.md) | CSV parsing, WaveManager, batch spawning, coroutines |
| 12 | [Advanced Behaviors](Lessons/12_Advanced_Behaviors.md) | ShieldHealth, RegenHealth, strategy extensibility proof |
| 13 | [Audio Middleware](Lessons/13_Audio_Middleware.md) | AudioData SO, AudioController, pooled one-shots, AudioMixer |
| 14 | [UI Toolkit HUD](Lessons/14_UITK_HUD.md) | UI Toolkit, UXML, USS, event-driven UI |
| - | [Appendix: Performance & Upgrade Path](Lessons/Appendix_Performance.md) | UpdateManager, GameConstants, PrimeTween, SL→DI |