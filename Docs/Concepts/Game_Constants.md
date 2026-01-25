# Game Constants

## Purpose

Having centralised static Game Constants classes is incredibly important in larger projects. 
While we won't be using these a whole lot in the project, if you ever plan on using Unities Localisation package or UI Toolkit these are essential.

The goal of game constants are to reduce the use of magic variables.
You should avoid declaring important or repeated strings and values directly in code.

Constants classes are static so their values can be accessed globally without creating instances.

---

## Implementation

Game constants are best used for values that rarely change and are not part of game balance. 
Gameplay tuning values should generally live in Scriptable Objects.

However, for the sake of introducing them we will use a constants class for:
- Starting Cash
- Starting Health
- Wave Interval

---

## Additional Examples

### Localisation Keys

When using the Unity Localisation Package, you need to reference the table key instead of the string contents. 
This is where having a  constants class will be crucial for code structure.

It would look something like this:

```csharp
    public class LocalizationKeys
    {
        // Table Name
        public const string MainTable = "MainGame";
       
        // Table Keys
        public const string Play = "play";
        public const string StartGame = "start-game";
        public const string Resume = "resume";
        public const string Pause = "pause";
        public const string Close = "close";
        public const string Controls = "controls";
        public const string Quit = "quit";
        public const string ExitGame = "exit-game";
    }
```

### UI Toolkit Styles

When creating UI through code with the UI Toolkit, you need to use strings constantly to add styling to your elements.
While this is not ideal and will hopefully be changed in the future, for now the safest approach is a constants class.

Here's a example from a game I'm currently developing:

```csharp
    public static class UIToolkitStyles
    {
        public const string Container = "container";
        public const string PanelBody = "panel-body";
        public const string PanelHeader = "panel-header";
        public const string PanelFooter = "panel-footer";
        public const string PanelTitle = "panel-title";
        public const string PanelContent = "panel-content";
        public const string PanelButton = "panel-button";
        public const string PanelSlider = "panel-slider";
    }
```

---
