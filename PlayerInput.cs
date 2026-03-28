using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    // When true, input values are ignored (useful for UI overlays)
    private bool _blockInput;
    public bool BlockInput
    {
        get => _blockInput;
        set
        {
            _blockInput = value;
            if (value) ResetInputs();
        }
    }
    // input actions
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction runAction;

    // public values
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool RunHeld { get; private set; }
    public bool JumpPressed { get; private set; }

    // lifecycle
    private void Awake()
    {
        if (moveAction == null)
        {
            moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            moveAction.AddCompositeBinding("2DVector(mode=2)")
                .With("Up", "<Keyboard>/w")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d")
                .With("Right", "<Keyboard>/rightArrow");
        }

        if (lookAction == null)
        {
            lookAction = new InputAction("Look", InputActionType.Value, expectedControlType: "Vector2");
            lookAction.AddBinding("<Mouse>/delta");
        }

        if (jumpAction == null)
        {
            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
        }

        if (runAction == null)
        {
            runAction = new InputAction("Run", InputActionType.Button);
            runAction.AddBinding("<Keyboard>/leftShift");
        }
    }

    private void OnEnable()
    {
        if (moveAction == null) Awake();

        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;
        lookAction.performed += OnLook;
        lookAction.canceled += OnLook;
        runAction.performed += OnRun;
        runAction.canceled += OnRun;
        jumpAction.performed += OnJump;

        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        runAction.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
            moveAction.Disable();
        }
        if (lookAction != null)
        {
            lookAction.performed -= OnLook;
            lookAction.canceled -= OnLook;
            lookAction.Disable();
        }
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJump;
            jumpAction.Disable();
        }
        if (runAction != null)
        {
            runAction.performed -= OnRun;
            runAction.canceled -= OnRun;
            runAction.Disable();
        }

        ResetInputs();
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
        lookAction?.Dispose();
        jumpAction?.Dispose();
        runAction?.Dispose();
    }

    // callbacks
    private void OnMove(InputAction.CallbackContext context)
    {
        if (BlockInput) Move = Vector2.zero;
        else Move = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        if (BlockInput) Look = Vector2.zero;
        else Look = context.ReadValue<Vector2>();
    }

    private void OnRun(InputAction.CallbackContext context)
    {
        if (BlockInput) RunHeld = false;
        else RunHeld = context.ReadValueAsButton();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (!BlockInput) JumpPressed = true;
    }

    // helpers
    public bool ConsumeJump()
    {
        if (!JumpPressed) return false;
        JumpPressed = false;
        return true;
    }

    /// <summary>
    /// clear cached input state useful when disabling input to avoid stuck input when keys are held
    /// </summary>
    public void ResetInputs()
    {
        Move = Vector2.zero;
        Look = Vector2.zero;
        RunHeld = false;
        JumpPressed = false;
    }
}
