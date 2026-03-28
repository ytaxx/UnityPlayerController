using UnityEngine;
using Optimization.Core;
using Ytax.Core;

public class MouseLook : MonoBehaviour
{
    // camera settings
    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float mouseSensitivity = 0.2f;
    [SerializeField] private float lookSmoothTime = 0.06f;
    [SerializeField] private float lookXLimit = 85.0f;

    // tilt settings
    [Header("Camera Tilt")]
    [SerializeField] private bool useCameraTilt = true;
    [SerializeField] private float tiltAngle = 10.0f;
    [SerializeField] private float tiltSpeed = 8.0f;
    [SerializeField] private float lookTiltSensitivity = 0.2f;
    [SerializeField] private float tiltSmoothTime = 0.12f;
    [SerializeField] private float mantleSmoothTime = 0.12f;
    [SerializeField] private float breathSmoothTime = 0.12f;
    // higher default because sensitivity is now lower (0.2 vs 10)
    [SerializeField] private float mouseSpeedToTilt = 2.5f;

    // references
    [Header("References")]
    [SerializeField] private Transform playerBody;

    // runtime state
    private PlayerInput input;
    private Transform cameraTransform;
    private float rotationX;
    private Vector2 smoothedLook;
    private Vector2 lookSmoothVelocity;
    private float currentTilt;
    private float mantleRoll;
    private float targetMantleRoll;
    private float mantleSmoothVelocity;
    private float tiltSmoothVelocity;

    private float targetBreathPitch;
    private float targetBreathRoll;
    private float targetBreathYaw;
    private float breathPitchVelocity;
    private float breathRollVelocity;
    private float breathYawVelocity;

    private float breathPitch;
    private float breathRoll;
    private float breathYaw;

    // unity callbacks
    private void Awake()
    {
        if (playerCamera == null) playerCamera = GetComponent<Camera>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null) cameraTransform = playerCamera.transform;

        input = GetComponent<PlayerInput>();
        if (input == null) input = GetComponentInParent<PlayerInput>();

        if (playerBody == null && input != null) playerBody = input.transform;
        if (playerBody == null) playerBody = transform.parent;
    }

    private void OnEnable()
    {
        // Reset smooth state so re-enabling after the console doesn't cause a camera snap
        smoothedLook = Vector2.zero;
        lookSmoothVelocity = Vector2.zero;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (playerCamera == null || input == null) return;

        // Look input: mouse delta from Input System is per-frame displacement (not velocity).
        // At low FPS each delta is larger, at high FPS smaller, but the total rotation
        // per second is the same because fewer/more frames compensate. SmoothDamp is
        // frame-rate independent internally, so the smoothed look tracks correctly.
        Vector2 look = input.Look;
        Vector2 target = look * mouseSensitivity;
        smoothedLook = Vector2.SmoothDamp(smoothedLook, target, ref lookSmoothVelocity, lookSmoothTime);

        // Apply smoothed displacement directly — no deltaTime multiplication needed
        // because mouse delta is already a displacement, not a velocity.
        float mouseX = smoothedLook.x;
        float mouseY = smoothedLook.y;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        if (useCameraTilt)
        {
            // Frame-rate independent tilt: raw smoothedLook.x is per-frame displacement,
            // which is ~2x larger at 30 FPS vs 60 FPS. Normalizing to a 60 FPS reference
            // frame ensures tilt sensitivity stays consistent at any framerate.
            // dtScale = referenceDt / actualDt — at 30 FPS this is 0.5 (halves the doubled
            // displacement), at 144 FPS this is 2.4 (scales up the smaller displacement).
            const float kReferenceDt = 1f / 60f;
            float dtScale = (Time.deltaTime > 0f) ? kReferenceDt / Time.deltaTime : 1f;
            float mouseSpeed = Mathf.Abs(smoothedLook.x) * dtScale;
            float tiltFactor = Mathf.Clamp01(mouseSpeed * mouseSpeedToTilt * lookTiltSensitivity);
            float targetTilt = -Mathf.Sign(smoothedLook.x) * tiltAngle * tiltFactor;
            // Smooth return-to-neutral: when mouse is still, smoothedLook decays to zero,
            // targetTilt becomes zero, and SmoothDamp eases currentTilt back to neutral.
            float effectiveTiltSmooth = Mathf.Max(0.0001f, tiltSmoothTime / Mathf.Max(0.0001f, tiltSpeed));
            currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltSmoothVelocity, effectiveTiltSmooth);
        }

        breathPitch = Mathf.SmoothDamp(breathPitch, targetBreathPitch, ref breathPitchVelocity, breathSmoothTime);
        breathRoll = Mathf.SmoothDamp(breathRoll, targetBreathRoll, ref breathRollVelocity, breathSmoothTime);
        breathYaw = Mathf.SmoothDamp(breathYaw, targetBreathYaw, ref breathYawVelocity, breathSmoothTime);

        mantleRoll = Mathf.SmoothDamp(mantleRoll, targetMantleRoll, ref mantleSmoothVelocity, mantleSmoothTime);

        // apply rotation math
        float appliedPitch = rotationX + breathPitch;
        float appliedRoll = currentTilt + breathRoll + mantleRoll;
        float appliedYaw = breathYaw;
        cameraTransform.localRotation = Quaternion.Euler(appliedPitch, appliedYaw, appliedRoll);

        if (playerBody != null)
            playerBody.rotation *= Quaternion.Euler(0f, mouseX, 0f);
    }

    public void SetBreathOffset(float pitch, float roll, float yaw = 0f)
    {
        targetBreathPitch = pitch;
        targetBreathRoll = roll;
        targetBreathYaw = yaw;
    }

    /// <summary>
    /// Immediately set the absolute look angles so that when MouseLook is enabled
    /// the camera and body orientations match the provided pitch/yaw.
    /// </summary>
    public void SetLookAngles(float pitch, float yaw)
    {
        rotationX = pitch;
        smoothedLook = Vector2.zero;
        lookSmoothVelocity = Vector2.zero;

        if (playerBody != null)
        {
            playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        float appliedPitch = rotationX + breathPitch;
        float appliedRoll = currentTilt + breathRoll + mantleRoll;
        float appliedYaw = breathYaw;
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(appliedPitch, appliedYaw, appliedRoll);
        }
    }
}
