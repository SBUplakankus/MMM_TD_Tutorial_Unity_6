# Episode 10: UI Toolkit HUD

> Duration: ~15-20 min

## Tutorial Video

<iframe width="560" height="315" src="https://www.youtube.com/embed/PLACEHOLDER_EP10" title="Episode 10: UI Toolkit HUD" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

## Overview

This episode covers building the game HUD using Unity's **UI Toolkit** (UITK) — Unity 6's recommended UI system for both runtime and editor interfaces. UITK provides a modern separation of structure (UXML), style (USS), and logic (C#) that pairs naturally with the event-driven architecture we've built.

**Why UI Toolkit over IMGUI/uGUI?**

- **IMGUI** — editor-only, not suitable for runtime HUDs
- **uGUI** — legacy canvas-based system, works but lacks USS styling and data binding
- **UI Toolkit** — web-like workflow (UXML/USS), better performance for complex HUDs, native data binding in Unity 6

## Learning Objectives

<!-- TODO: Fill in your learning objectives, e.g.: -->
- Create a game HUD layout using UXML
- Style the HUD with USS to match the game aesthetic
- Build a UI controller that subscribes to event channels for data-driven updates
- Implement a tower shop panel triggered by tower node interaction
- Display tower stats using the ISelectable interface

## Key Concepts

- **UI Toolkit basics** — UXML (structure), USS (style), UIDocument (runtime component)
- **Data binding in UITK** — connecting visual elements to runtime data
- **Game Constants for UI style strings** — see [Game Constants](../Concepts/Game_Constants.md)
- **Event-driven UI updates** — subscribe to VoidEventChannel/TypedEventChannel instead of polling

## Code Roadmap

### Files You'll Work On

<!-- TODO: List the UITK files you create, e.g.: -->
| File | Purpose |
|------|---------|
| *(fill in)* | UXML layout for HUD |
| *(fill in)* | USS stylesheet for HUD |
| *(fill in)* | UI controller script |
| *(fill in)* | Tower shop controller |
| *(fill in)* | Game constants for UITK style strings |

### Prerequisites

- Episode 08 event system must be complete — the UI subscribes to the same event channels
- [Game Constants](../Concepts/Game_Constants.md) concept reviewed — UITK uses string constants for USS class names

## Architecture Context

```text
Game Events ──→ EventChannels ──→ UI Controller ──→ UIDocument ──→ UXML/USS
                  │                    │
                  │                    ├── Queries visual elements by USS class
                  │                    ├── Updates text, visibility, style
                  │                    └── Subscribes on enable, unsubscribes on disable
                  │
            TowerNode ──→ ShopController ──→ Shop Panel (UXML)
                  │
            ISelectable ──→ Info Panel ──→ Tower stats display
```

The UI layer is a **pure consumer** of events. It never drives game logic — it only reflects state changes that already happened through the event system.

## Step-by-Step Implementation Guide

### Step 1: HUD Layout (UXML)

<!-- TODO: Describe your UXML structure -->

Build the HUD layout with these regions:

- **Top bar** — player health, gold, wave counter
- **Bottom bar** — tower shop buttons
- **Side panel** — tower info on selection

```xml
<!-- TODO: Provide your UXML structure example -->
<ui:UXML xmlns:ui="...">
  <!-- Top bar: health, gold, wave -->
  <!-- Bottom bar: tower shop -->
  <!-- Side panel: tower info -->
</ui:UXML>
```

!!! tip "UXML class naming" Use consistent USS class names and store them as constants in `GameConstants`. See [Game Constants](../Concepts/Game_Constants.md) for the pattern.

### Step 2: HUD Styling (USS)

<!-- TODO: Describe your USS approach -->

Style the HUD to match your game aesthetic:

- Colour palette consistent with visual theme
- Font sizes for readability at game resolution
- Flexbox layout for responsive element arrangement
- Transitions for smooth value changes (health bar drain, gold counter tick)

```css
/* TODO: Provide your USS style examples */
```

### Step 3: UI Controller

<!-- TODO: Describe your controller pattern -->

The UI controller bridges event channels and visual elements:

- Subscribes to `VoidEventChannel` and `TypedEventChannel<T>` on enable
- Queries `UIDocument.rootVisualElement` for target elements using USS class names
- Updates element properties (text, style classes, visibility) when events fire
- Unsubscribes on disable

```csharp
// TODO: Provide your UI controller outline
public class HUDController : MonoBehaviour
{
    // TODO: UIDocument reference
    // TODO: Event channel subscriptions
    // TODO: Visual element queries (cached on start)
    // TODO: Event handler methods that update visual elements
}
```

### Step 4: Game Constants for UI

<!-- TODO: Describe your constants approach -->

Use `GameConstants` for USS class name strings to avoid magic strings:

```csharp
// TODO: Show UIToolkitStyles constants class
public static class UIToolkitStyles
{
    // TODO: const string fields for each USS class name
    // e.g. public const string HealthBar = "health-bar";
    // e.g. public const string GoldText = "gold-text";
}
```

See [Game Constants](../Concepts/Game_Constants.md) for the full pattern and rationale.

### Step 5: Tower Shop UI

<!-- TODO: Describe your shop flow -->

Interaction flow:

1. Player clicks a `TowerNode` on the grid
2. `TowerNode` raises an event or directly activates the `ShopController`
3. `ShopController` shows the shop panel near the clicked node
4. Player selects a tower prefab → shop raises placement event
5. Shop panel hides after selection or dismissal

```text
TowerNode (click) ──→ ShopController ──→ Show shop panel (UXML)
                                          │
                        Player selects tower ──→ OnTowerSelected event
                                          │
                              Shop panel hides
```

### Step 6: Tower Info Panel

<!-- TODO: Describe your info panel implementation -->

The info panel displays stats for the currently selected tower:

- Uses the `ISelectable` interface to query tower data
- Subscribes to selection events (tower clicked/selected)
- Updates display: tower name, damage, range, upgrade status
- Hides when selection is cleared

```csharp
// TODO: Show info panel outline
public class TowerInfoPanel : MonoBehaviour
{
    // TODO: ISelectable reference
    // TODO: Visual element references
    // TODO: Subscribe to selection events
    // TODO: Update display when selection changes
}
```

## Episode Recap

<!-- TODO: Summarise what was accomplished -->

- Built HUD layout with UXML
- Styled with USS for game aesthetic
- Connected UI controller to event channels for data-driven updates
- Implemented tower shop and info panels

## Challenge

<!-- TODO: Add your challenge, e.g.: -->
- Add a wave preview panel that shows upcoming enemy compositions
- Add tower upgrade UI with cost validation
- Implement HUD animations using PrimeTween (see [Tweening](../Concepts/Tweening.md))

!!! info "This episode is self-directed"
    The UITK implementation is left to you as the course creator. This outline provides the structure — fill in the code and explanations based on your UI design decisions. The architecture (event-driven, data-driven, no polling) is the important part. The specific visual design is yours to choose.