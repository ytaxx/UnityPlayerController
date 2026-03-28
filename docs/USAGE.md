# Usage & Integration

This guide walks through creating a playable player in a scene and wiring basic UI hooks.

Prerequisites
- Unity 2019.4+ (2020.3 recommended).
- (Optional) Unity Input System package if you want the default key bindings.

Create the player
1. Create an empty GameObject named `Player` at the scene origin.
2. Add a built-in `CharacterController` component to the `Player` GameObject.
3. Add the following scripts to the same GameObject (component order is not strict, but dependencies assume `PlayerInput` and `PlayerMovement` are present):
	 - `PlayerInput`
	 - `PlayerMovement`
	 - `PlayerStamina` (optional; adds sprinting/stamina behavior)
4. Create a child GameObject called `CameraHolder` and attach your `Camera` as its child. Attach `MouseLook` to `CameraHolder` or the `Camera` and `HeadBob` to the `Camera` object.

Recommended inspector settings (starting points)
- `PlayerMovement`:
	- WalkSpeed: 2.0
	- RunSpeed: 4.0
	- JumpHeight: 1.2
- `MouseLook`:
	- MouseSensitivity: 0.2
	- LookSmoothTime: 0.06
- `HeadBob`:
	- UseHeadBob: true
	- BobFrequency: 12

Input system notes
- `PlayerInput` configures the Input System actions at runtime with keyboard and mouse bindings. If you don't use the Unity Input System package you can replace `PlayerInput` internals while keeping the public properties (`Move`, `Look`, `RunHeld`, `ConsumeJump()`).

UI integration examples
- Bind stamina UI via delegate:
```csharp
var stamina = player.GetComponent<PlayerStamina>();
stamina.OnStaminaStateChanged += (percent, sprinting) => staminaBar.fillAmount = percent;
```
- Or subscribe to published event (when `GameEventBus` present):
```csharp
// your event system should receive StaminaChangedEvent
```

Pausing movement for UI
- Call `PlayerMovement.PauseMovement()` to temporarily freeze horizontal movement while allowing gravity to continue (useful for dialog or menus). Call `ResumeMovement()` to restore control.

Troubleshooting
- Camera snaps on enable: ensure `MouseLook` has its `playerBody` or camera reference set and smoothing values are reset when enabling/disabling.
- No input: install Unity Input System package or provide a compatible `PlayerInput` implementation.

Advanced
- Replace `Compat/UpdateManager` and `GameEventBus` with your game's existing update/event systems for tighter integration.
