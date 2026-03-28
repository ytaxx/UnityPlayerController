# API Reference

Detailed reference for the public API exposed by the player components. Copy/paste the small snippets below into your UI or other scripts to read runtime state.

## PlayerMovement
- Properties:
  - `Vector3 CurrentVelocity` — current world-space velocity.
  - `bool IsGrounded` — true when `CharacterController.isGrounded` is true.
  - `bool IsRunning` — true while stamina/system reports sprint active.
  - `Vector2 CurrentInput` — last processed movement input (x = strafe, y = forward/back).
  - `float WalkSpeed`, `RunSpeed`, `CurrentSpeed` — configured and current speed values.
- Methods:
  - `void PauseMovement()` — freeze horizontal movement (vertical gravity still applies). Useful for UI/dialogue.
  - `void ResumeMovement()` — re-enable normal movement.

Example: read current speed from another script
```csharp
var movement = player.GetComponent<PlayerMovement>();
float current = movement.CurrentSpeed;
bool grounded = movement.IsGrounded;
```

## PlayerInput
- Properties:
  - `Vector2 Move` — normalized movement vector from input.
  - `Vector2 Look` — mouse delta per-frame.
  - `bool RunHeld` — true while run key is held.
  - `bool JumpPressed` — raw jump press state (consumed via `ConsumeJump`).
  - `bool BlockInput` — setting true clears inputs and ignores callbacks (useful for UI overlays).
- Methods:
  - `bool ConsumeJump()` — returns true one-time when a jump button was pressed and clears the flag.
  - `void ResetInputs()` — clears Move/Look/RunHeld/JumpPressed.

Note: `PlayerInput` uses the Unity Input System. If you don't use the package you can replace its internals with your own input source while keeping the public surface unchanged.

## PlayerStamina
- Properties:
  - `float StaminaPercent` — normalized [0..1] stamina for UI.
  - `float CurrentStamina`, `float MaxStamina` — raw values.
  - `bool IsSprinting` — true while sprinting this frame.
  - `bool CanSprint` — true when not depleted.
  - `bool IsInRechargeDelay` — true while a cooldown/recharge delay is active.
- Methods/Events:
  - `void SetSprinting(bool sprinting, bool runHeld)` — called by `PlayerMovement` to request sprinting; stamina enforces it.
  - `event Action<float,bool> OnStaminaStateChanged` — legacy delegate delivering `(staminaPercent, isSprinting)` when state changes.
  - `GameEventBus.Publish(new StaminaChangedEvent(...))` — also published when `GameEventBus` is present.

Example: simple UI binder
```csharp
var stamina = player.GetComponent<PlayerStamina>();
stamina.OnStaminaStateChanged += (percent, sprinting) => {
    staminaBar.fillAmount = percent;
};
```

## MouseLook
- Methods:
  - `void SetBreathOffset(float pitch, float roll, float yaw = 0f)` — apply breathing offsets (used by `HeadBob`).
  - `void SetLookAngles(float pitch, float yaw)` — immediately set look orientation.

Example: align camera on respawn
```csharp
mouseLook.SetLookAngles(0f, player.transform.eulerAngles.y);
```

## HeadBob
- Primarily configured via serialized fields. `HeadBob` invokes `MouseLook.SetBreathOffset` to combine breathing and bob effects.

Compatibility notes
- The repository contains `Compat/` shims for `Optimization.Core` and `Szatyorg.Core` (small `GameEventBus`, `StaminaChangedEvent`, `UpdateManager`, `ProfilerMarkers`) to allow compilation without the original internal packages. Replace with your game's real systems if desired.
