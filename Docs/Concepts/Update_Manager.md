# Update Manager

## Purpose
The base Unity update has some safety checks checks that when called on a large number of MonoBehaviours every frame,
can add unnecessary overhead in games.
As long as we carefully control how updates are registered and executed, these checks are not necessary.

In a Tower Defence game where you have alot happening at once, you want to optimise performance which is why we will be implementing our own custom update manager.

This will only call the Unity Update once in the manager itself. 
This manager will be a singleton class that contains a reference to any IUpdateable interfaces in the scene.
Classes will register and unregister from the Update Manager on enable and disable.

Not every project needs a custom update manager, 
but in systems-heavy games like tower defence it provides better control over performance.

---

## Implementation

We create a singleton Game Update Manager class and reference this in any classes that would have previously used the Unity Update.
We will have an Update Priority enum so we can have High, Medium and Low Priority Update.

- **High:** Updates every frame - Movement, Aiming, Animations
- **Medium:** Updates every 0.15 seconds - Target Scanning, Cooldown Checks
- **Low:** Updates every 0.4 seconds - Path Calculations, Expensive Logic

Through this we can specify what needs to seperate out our updates so not everything is being called 60 times per second when it can work just as well being called 6-7 or 2-3 times per second.

---