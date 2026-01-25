# Abstraction

## Purpose
Using abstract classes helps us reduce repetitive code while giving elements of our game a base class to inherit from, 
giving us a clear and pre-defined structure. 

In this course we will be using abstract base classes for our Towers, Projectiles, and Enemies.

---

## Implementation

Enemies, Towers and Projectiles will all share the same core functions and components that we can define in a dedicated base class.

### Enemy Base

- **Movement:** Each enemy will have a movement call each frame. However this may differ between enemy types. The movement code for flying enemies will need to be different to that of grounded enemies.
- **Health:** All enemies need to take damage so we can define that here.
- **Spawn:** All enemies need to be spawned in and despawned.
- **Data:** All enemies will have a Data Scriptable Object defining their stats.
- **Interfaces:** All enemies will contain the IDamageable and ITargetable interfaces so towers can attack them.

### Tower Base

- **Attacking:** Each tower will have a form of attacking enemies along the path.
- **Scanning:** All towers will need to scan to see what enemies are in range and sort based on attack priority.
- **Buy/Sell:** All towers can be bought, placed, sold and removed from the map.
- **Upgrades:** All towers can be upgraded.
- **Data:** All towers will have a base Data Scriptable Object defining their stats.
- **Interfaces:** All towers will contain an IPlaceable interface so they can be put on the map by the player. 

### Projectile Base

- **Movement:** All projecticles need to move in world space.
- **Spawn:** All projecticles need to spawn in and despawn on contact.
- **Damage:** All projectiles need to damage the enemies they come into contact with.

---

## Expansion

Now that we have defined the base classes, we will easily be able to develop a wide variety of enemies, towers and projecticles. 

### Enemy Examples

- **Stamdard Enemy**: Basic movement logic to follow along the path on the ground.
- **Flying Enemy**: Unique movement logic to fly over the path, avoid obstacles and go direct to the end point.
- **Bomb Enemy:** On death, instead of just despawning, the enemy blows up damaging everything around them.

### Tower Examples

- **Standard Tower:** Shoots a basic projectile in a straight line towards the enemy.
- **AOE Tower:** Damages all enemies in an AOE Around it instead of launching projectiles.

### Projectile Examples

- **Arrow:** Moves in a straight line penetrating through multiple enemies.
- **Bomb:** Moves in a straight line damaging all enemies in an AOE on impact.
- **Missile:** Homes in on an enemy, blowing up on impact.
- **Catapult:** Flies up in an arc, landing in the target area damaging enamies in an AOE.

---
