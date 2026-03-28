# Player (Unity) — First-Person Player Components

Collection of small, focused Unity components for a responsive first-person player: movement, input, camera look, head bob, and stamina (sprint). Designed to be dropped into a Unity project and tweaked via the Inspector.

Highlights
- Lightweight, modular components that separate input, movement, camera, and audio/visual effects.
- Works with Unity Input System (new) or can be adapted for other input systems.
- Small compatibility shims included so the code compiles without the original project cores.

Requirements
- Unity 2019.4 LTS or newer (2020.3+ recommended).
- (Optional) Unity Input System package (com.unity.inputsystem) if you want the provided bindings.

Quick install
1. Copy the `Player` folder into your project's `Assets/` folder.
2. Open Unity and let it compile.
3. Create an empty GameObject named `Player` and add these components (in order of dependency):
   - `CharacterController` (built-in)
   - `PlayerInput`
   - `PlayerMovement`
   - `PlayerStamina` (optional, used by sprinting)
4. Create a child `Camera` under `Player` and add `MouseLook` and `HeadBob` (or attach `MouseLook` to a camera holder).

Scene setup (quick)
- Ensure `PlayerMovement` and `PlayerInput` are on the same GameObject (required by the code).
- `PlayerMovement` requires a `CharacterController` on the same GameObject.
- Tune serialized fields in the Inspector (speeds, jump height, FOV, bob amounts).

Usage summary
- `PlayerInput` — provides runtime values: Move (Vector2), Look (Vector2), RunHeld (bool), JumpPressed (bool) and helpers `ConsumeJump()` / `ResetInputs()`.
- `PlayerMovement` — handles movement, jumping, coyote time, smoothing, sprint integration. Public API: `CurrentVelocity`, `IsGrounded`, `IsRunning`, `CurrentInput`, `WalkSpeed`, `RunSpeed`, `CurrentSpeed`. Methods: `PauseMovement()` / `ResumeMovement()`.
- `PlayerStamina` — manages sprint budget and recharge. Public API: `StaminaPercent`, `IsSprinting`, `SetSprinting()`, `OnStaminaStateChanged` event. It also publishes `StaminaChangedEvent` to `GameEventBus` when present.
- `MouseLook` — camera rotation and tilt. API: `SetLookAngles(pitch,yaw)` and `SetBreathOffset(pitch,roll,yaw)`.
- `HeadBob` — camera bob, breath sway, FOV kick, and collision handling. Configured in Inspector.

Documentation
- Read the API summary: [docs/API.md](docs/API.md)
- Read the integration and usage guide: [docs/USAGE.md](docs/USAGE.md)

Examples
- The repository currently includes compatibility shims under `Compat/` so the assets compile standalone. You can create a simple scene by following the Quick install steps.

Testing & CI
- This is a Unity codebase; automated play-mode tests are not included. For CI you can add Unity Test Runner jobs or a headless Linux build pipeline.

Contributing
- See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution workflow and testing suggestions.

License
- MIT — see [LICENSE](LICENSE).
