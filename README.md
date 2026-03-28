#  Unity Player Controller

This repository contains a small set of Unity scripts that provide a basic first-person player. The code is modular so you can use only the parts you need. Everything is easy to change through the Inspector.

Who this is for
- Beginners learning how player controllers work.
- Developers who want a simple, tweakable starting point for first-person controls.

What is included
- `PlayerInput` - simple input wrapper that exposes `Move`, `Look`, `RunHeld`, and `ConsumeJump()`.
- `PlayerMovement` - walking, running, jumping, coyote time, and smooth acceleration.
- `PlayerStamina` - simple sprint stamina with drain and recharge.
- `MouseLook` - camera rotation with optional tilt and smoothing.
- `HeadBob` - camera bobbing, breathing, and FOV effects with collision handling.
- `Compat/` - small shims so code compiles without internal project packages.

Minimum requirements
- Unity 2019.4 or newer. Unity 2020.3 LTS is recommended.

Setup
1. Copy the `Player` folder into your project's `Assets` folder.
2. Open the project in Unity and wait for scripts to compile.
3. Create an empty GameObject called `Player` at the scene origin.
4. Add a `CharacterController` component to `Player` (built-in component).
5. Add these scripts to the `Player` GameObject: `PlayerInput`, `PlayerMovement`, and optionally `PlayerStamina`.
6. Create a child GameObject for the camera. Add your `Camera` under it.
7. Attach `MouseLook` to the camera or its parent, and attach `HeadBob` to the camera.

Starter inspector values
- `PlayerMovement`: WalkSpeed = 2, RunSpeed = 4, JumpHeight = 1.2
- `MouseLook`: MouseSensitivity = 0.2, LookSmoothTime = 0.06
- `HeadBob`: UseHeadBob = true, BobFrequency = 12

Basic controls (default)
- Move: W/A/S/D or arrow keys
- Look: Mouse
- Run: Left Shift
- Jump: Space

How to use in your game
- Read runtime state from `PlayerMovement` (e.g., `IsGrounded`, `CurrentVelocity`) to drive animations.
- Bind UI to `PlayerStamina.OnStaminaStateChanged` or read `StaminaPercent` each frame.
- Use `PlayerMovement.PauseMovement()` to freeze player input during dialog or menus. Call `ResumeMovement()` when done.

Technical details
- Components are small, single-responsibility MonoBehaviours. Public properties provide runtime state; methods are side-effect free except where intended (for example `SetSprinting`).
- `PlayerInput` builds simple Input System actions at runtime. If you prefer another input layer, keep the public properties (`Move`, `Look`, `RunHeld`, `ConsumeJump()`) and replace the internal bindings.
- `PlayerMovement` relies on `CharacterController` for collision and grounding. Movement uses `Mathf.MoveTowards` for speed smoothing and separate gravity integration for stable jumping and coyote time.
- `PlayerStamina` updates via `Compat/UpdateManager` when the project's event system is not present. It exposes `OnStaminaStateChanged` and also publishes `StaminaChangedEvent` to `GameEventBus` if available.
- `HeadBob` and `MouseLook` use SmoothDamp and time-scaled timers so motion is frame-rate independent. Camera collision is handled with a sphere cast to avoid clipping into geometry.
- `Compat/` contains small shims: `UpdateManager` (per-frame callbacks), `GameEventBus` (Publish no-op), `StaminaChangedEvent`, and `ProfilerMarkers`. Replace these with your game's systems for tighter integration.
- Performance notes: avoid heavy work in Update callbacks. The included `UpdateManager` batches callbacks; if you integrate into an existing scheduler prefer grouped updates and early returns when components are disabled.
- Extensibility: read/write the public properties from other scripts, subscribe to `OnStaminaStateChanged`, or override a component by subclassing and swapping the script on the GameObject.

Where to go next
- See [docs/USAGE.md](docs/USAGE.md) for detailed setup and troubleshooting.
- See [docs/API.md](docs/API.md) for quick code snippets and examples.

License
- MIT. See [LICENSE](LICENSE) for details.
