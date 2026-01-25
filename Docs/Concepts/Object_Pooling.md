# Object Pooling

## Purpose
Object Pooling is very valuable optimisation technique in games that nowadays is incredibly easy to implement in Unity
thanks to the engines own Object Pool class.

Instead of Instantiating and Destroying objects, you instead enable and disable objects, 
reusing them when possible and only instantiating when neccesary.

Pooled objects must be properly reset when reused to avoid carrying over state from previous uses.

Instantiation and Destroy calls can use up a lot of processing power when calling frequently on objects such as projectiles or particle effects,
so if you have not used it before, you will often notice a significant performance improvement.

Not every object needs pooling, but objects that are created and destroyed frequently are ideal candidates.

---

## Implementation

In our project, we will be using Object Pooling for Enemies, Projectiles and Particle Effects.
We will be Pre-Warming our pools at the start of the game. 
This means creating a number of objects at the start that can then be called by the game instead of needing to instatiate objects for the first few calls.

Our ObjectPoolManager will be a singleton class that can be accessed by any class in the game.
For this will include the Wave Spawner for Enemies, Tower Controllers for Projectiles, and Enemy Controller for hit and death effects.

We will create the pools using the Unity Object Pool Class and give each a return and fetch functions.