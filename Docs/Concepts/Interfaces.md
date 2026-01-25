# Interfaces

## Purpose

Interfaces allow us to define what an object can do without defining how it does it.
Essentially, it forces classes to implement a specific set of functions that other systems can rely on

For example all enemies will need to implement IDamageable and have a function called TakeDamage, otherwise the game wouldn't work.

---

## Implementation

### IDamageable

The `IDamageable` interface defines anything that can **receive damage**.

This will be used by enemies so that when projectiles come into contact with them, they can do damage.

This lets us differentiate enemies from environmental obstacles or other towers the projectile might hit.

### ITargetable

The `ITargetable` interface defines anything that can be **targeted by towers**.

Towers will scan for any objects that contain ITargetable in their range before sorting them based on their given attack priority. 

This prevents any non-targetable objects interfering being mistaken for valid targets.

### IPlaceable

The `IPlaceable` interface defines anything that can be **placed on the map by the player**.

For this project it will only refer to towers, but you could easily use this to define barriers or traps to be placed on the map to assist you.

### IUpdateable

The `IUpdateable` interface defines anything that can be updated through the Game Update Manager.

Since we are implementing our own Updates and not using the built in MonoBehaviour Update, we need to add this interface to any objects that can be updated.

The Update Manager then iterates through all subscribed objects each frame.

---
