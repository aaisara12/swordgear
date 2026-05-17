# Input System

## Purpose
Provides touch/mouse-driven joystick input for player movement and action inputs, with custom Unity Input System interactions for hold and tap gestures.

---

## Key Scripts

| Script | Path |
|---|---|
| `PlayerGameplayInputManager` | `Assets/Aaron/Scripts/PlayerGameplayInputManager.cs` |
| `SimulatedJoysticksStateController` | `Assets/Aaron/Scripts/SimulatedJoysticksStateController.cs` |
| `JoystickControlRegion` | `Assets/Aaron/Scripts/Input/Joystick/JoystickControlRegion.cs` |
| `JoystickDragAction` | `Assets/Aaron/Scripts/Input/Joystick/JoystickDragAction.cs` |
| `OriginFollowJoystickDragAction` | `Assets/Aaron/Scripts/Input/Joystick/OriginFollowJoystickDragAction.cs` |
| `OriginRootedJoystickDragAction` | `Assets/Aaron/Scripts/Input/Joystick/OriginRootedJoystickDragAction.cs` |
| `JoystickVisual` | `Assets/Aaron/Scripts/Input/Joystick/JoystickVisual.cs` |
| `JoystickVisualProvider` | `Assets/Aaron/Scripts/Input/Joystick/JoystickVisualProvider.cs` |
| `TargetCentricMouseClickDrivenJoystickControlRegion` | `Assets/Aaron/Scripts/Input/Joystick/TargetCentricMouseClickDrivenJoystickControlRegion.cs` |
| `HoldInteraction2d` | `Assets/Aaron/Scripts/Input/HoldInteraction2d.cs` |
| `ButtonHoldWithSteadyJoystickInteraction` | `Assets/Aaron/Scripts/Input/ButtonHoldWithSteadyJoystickInteraction.cs` |
| `ButtonTapWithSteadyJoystickInteraction` | `Assets/Aaron/Scripts/Input/ButtonTapWithSteadyJoystickInteraction.cs` |
| `ZoneReleaseInteraction` | `Assets/Aaron/Scripts/Input/ZoneReleaseInteraction.cs` |
| `OnScreenLeftClickButton` | `Assets/Aaron/Scripts/Input/OnScreenLeftClickButton.cs` |
| `PlayerControls` | `Assets/Aaron/PlayerControls.cs` (generated) |

---

## Architecture

The game uses **Unity's Input System** with a generated `PlayerControls` action asset. The main action map is consumed by `PlayerGameplayInputManager`, which translates raw input into calls on the currently linked `PlayerGameplayPawn`.

**Pawn linking** is explicit:
- `PlayerGameplayInputManager.LinkPawn(pawn)` — called when the pawn spawns.
- `PlayerGameplayInputManager.UnlinkCurrentPawn()` — called on despawn.

This means input is fully disabled when no pawn is active.

---

## Joystick System

On touch/mobile, movement uses a **simulated virtual joystick** rather than the device's physical stick:
- `JoystickControlRegion` defines the screen region that captures touch drag input.
- `JoystickDragAction` computes a normalized direction vector from the drag.
- Two implementations: `OriginFollowJoystickDragAction` (origin follows finger) and `OriginRootedJoystickDragAction` (fixed origin).
- `JoystickVisual` renders the joystick knob and base.

---

## Custom Interactions

Custom `IInputInteraction` implementations extend Unity's interaction system:

| Class | Behaviour |
|---|---|
| `HoldInteraction2d` | Hold gesture on a 2D touch region |
| `ButtonHoldWithSteadyJoystickInteraction` | Button hold while joystick is held steady |
| `ButtonTapWithSteadyJoystickInteraction` | Button tap while joystick is held steady |
| `ZoneReleaseInteraction` | Fires on release from a touch zone |

Register custom interactions in an `[InitializeOnLoad]` class or `RuntimeInitializeOnLoadMethod` before Input System initializes.

---

## Platform Differences

`PlatformAssetSwitcher` swaps certain assets (e.g., joystick visuals) based on the current platform at runtime.
