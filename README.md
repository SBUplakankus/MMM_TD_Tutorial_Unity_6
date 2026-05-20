# Tower Defense Tutorial - Massive Melt Media

---

## About This Tutorial
The goal of this tutorial is to help developers take the next step in their game development journey
by teaching them key concepts and design patterns that can be used in any future games.

The main focus is on high-level game architecture and design choices as opposed to specific tower defence game mechanics.
You will learn the benefits of a clean project structure and pick up some tips and tricks Unity developers use to improve the
performance and scalability of larger projects.

---

## What You'll Learn
- Decoupling systems through the Observer Pattern with ScriptableObject Event Channels
- Efficient object reuse through Object Pooling with Unity's ObjectPool<T>
- Composable enemy behaviors through the Strategy Pattern with ScriptableObjects
- Data-driven wave design through runtime CSV parsing
- BTD6-style priority targeting (First/Last/Strong/Close)
- Event-driven audio middleware with AudioMixer groups and pooled one-shot sources
- Interface-based architecture (IDamageable, ITargetable, IUpdatable, IPoolable)
- Custom Update Manager for performance optimization
- UI Toolkit for game HUD

---

## Prerequisites
- Roughly a year of experience in the Unity engine *(You've done the beginner stuff)*
- Familiarity with C# or a similar language
- Developed using Unity 6.3 using the 3D URP but should work on similar or later versions

---

## How to Use This Repo
- The main branch contains the most up to date version of the game
- If you want a head start, you can pull the startup branch
- You will need to get PrimeTween from the asset store and import it *(It's free)*

---

## Project Documents
| Name                                 | Description                                               | 
|--------------------------------------|-----------------------------------------------------------|
| [Architecture](Docs/Game_Architecture.md) | A high level overview of the games architecture           |
| [Lessons](Docs/Lessons.md)           | Documents for each video lesson in the course             |
| [Concepts](Docs/Concepts.md)         | Documents for each game development concept in the course |
| [Assets](Docs/Assets.md)             | Assets used throughout the project                        |

---