using UnityEngine;
using Optimization.Core;
using Ytax.Core;

public class HeadBob : MonoBehaviour
{
    // camera settings
    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform playerBody;
    [SerializeField] private float nearClipPlane = 0.01f;

    // effects
    [Header("Effects")]
    [SerializeField] private bool useHeadBob = true;
    [SerializeField] private float bobFrequency = 12.0f;
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float bobHorizontalAmplitude = 0.025f;
    [SerializeField] private float idleBobFrequency = 0.6f;
    [SerializeField] private float idleBobAmplitude = 0.012f;
    [SerializeField] private float bobBlendSmoothTime = 0.18f;
    [SerializeField] private float camPositionSmoothTime = 0.06f;
    [SerializeField] private float breathFrequency = 0.45f;
    [SerializeField] private float breathAmplitude = 0.012f;
    [SerializeField] private float breathPitchAmount = 0.25f;
    [SerializeField] private float breathRollAmount = 0.35f;

    // bob look
    [Header("Bob Look")]
    [SerializeField] private float bobPitchAmount = 0.6f;
    [SerializeField] private float bobRollAmount = 0.4f;
    [SerializeField] private float bobYawAmount = 0.4f;
    [SerializeField, Range(0f, 1f)] private float bobIntensity = 0.5f;

    // stabilizer
    [Header("Stabilizer")]
    [SerializeField] private bool useStabilizer = true;
    [SerializeField] private float focusDistance = 5.0f;

    // fov kick
    [Header("FOV Kick")]
    [SerializeField] private bool useFovKick = true;
    [SerializeField] private float defaultFov = 60f;
    [SerializeField] private float runFov = 75f;
    [SerializeField] private float walkFov = 62f;
    [SerializeField] private float fovChangeSpeed = 5f;
    [SerializeField] private float fovSmoothTime = 0.12f;

    // camera collision
    [Header("Camera Collision")]
    [SerializeField] private float cameraCollisionRadius = 0.12f;
    [SerializeField] private float cameraCollisionOffset = 0.05f;
    [SerializeField] private LayerMask cameraCollisionMask = ~0;

    // components and state
    private Transform cameraTransform;
    private PlayerMovement movement;
    private MouseLook mouseLook;

    private float bobTimer;
    private float breathTimer;
    private float idleTimer;
    private Vector3 defaultCamLocalPos;
    private Vector3 camPosSmoothVelocity;
    private float bobBlend;
    private float bobBlendVelocity;
    private float fovVelocity;

    private Vector3 lastPosition;

    // unity callbacks
    private void Awake()
    {
        if (playerCamera == null) playerCamera = GetComponent<Camera>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null) cameraTransform = playerCamera.transform;

        movement = GetComponent<PlayerMovement>();
        if (movement == null) movement = GetComponentInParent<PlayerMovement>();

        mouseLook = GetComponent<MouseLook>();
        if (mouseLook == null) mouseLook = GetComponentInParent<MouseLook>();
        if (mouseLook == null && playerCamera != null) mouseLook = playerCamera.GetComponent<MouseLook>();

        if (playerBody == null && movement != null) playerBody = movement.transform;
        if (playerBody == null) playerBody = transform.parent;
    }

    private void Start()
    {
        if (playerCamera != null)
        {
            defaultCamLocalPos = cameraTransform.localPosition;
            playerCamera.fieldOfView = defaultFov;
            playerCamera.nearClipPlane = nearClipPlane;
        }

        if (playerBody != null) lastPosition = playerBody.position;
    }

    private void OnEnable()
    {
        if (playerBody != null) lastPosition = playerBody.position;
        camPosSmoothVelocity = Vector3.zero;
        bobBlend = 0f;
        bobBlendVelocity = 0f;
        fovVelocity = 0f;
        bobTimer = 0f;
        breathTimer = 0f;
        idleTimer = 0f;
    }

    private void LateUpdate()
    {
        ProfilerMarkers.HeadBob.Begin();
        if (movement == null || playerCamera == null || playerBody == null) { ProfilerMarkers.HeadBob.End(); return; }
        if (!movement.IsGrounded)
        {
            lastPosition = playerBody.position;
            ProfilerMarkers.HeadBob.End();
            return;
        }

        Vector2 moveInput = movement.CurrentInput;
        float inputMag = moveInput.magnitude;

        float actualHorSpeed = 0f;
        if (Time.deltaTime > 0f)
        {
            Vector3 actualVel = (playerBody.position - lastPosition) / Time.deltaTime;
            actualHorSpeed = new Vector2(actualVel.x, actualVel.z).magnitude;
        }

        if (useHeadBob)
        {
            // bob math
            float inputBlend = Mathf.Clamp01(inputMag);
            float maxSpeed = movement.IsRunning ? movement.RunSpeed : movement.WalkSpeed;
            float movementBlend = Mathf.Clamp01(actualHorSpeed / Mathf.Max(0.01f, maxSpeed));
            // Smooth return-to-neutral: when movement stops, moveBlendTarget drops to 0
            // and bobBlend smoothly decays, crossfading from walk bob to idle sway.
            float moveBlendTarget = inputBlend * movementBlend;
            bobBlend = Mathf.SmoothDamp(bobBlend, moveBlendTarget, ref bobBlendVelocity, bobBlendSmoothTime);

            float speedMultiplier = movement.IsRunning ? 1.5f : 1.0f;
            float speedFactor = Mathf.Clamp01(movement.CurrentVelocity.magnitude / Mathf.Max(0.01f, maxSpeed));

            // Frame-rate independence: timers advance by Time.deltaTime * frequency,
            // so the sine waves progress at the same rate per second regardless of FPS.
            // At 30 FPS deltaTime ~0.033 (larger steps), at 144 FPS ~0.007 (smaller steps),
            // but the total advancement per second is identical.
            bobTimer += Time.deltaTime * bobFrequency * Mathf.Lerp(0.9f, 1.2f, speedFactor) * speedMultiplier;
            breathTimer += Time.deltaTime * breathFrequency;
            idleTimer += Time.deltaTime * idleBobFrequency;
            const float timerWrap = 6.2831853f * 2f; // 4pi covers sin(t) and sin(t*0.5)
            if (bobTimer > timerWrap) bobTimer -= timerWrap;
            if (breathTimer > timerWrap) breathTimer -= timerWrap;
            if (idleTimer > timerWrap) idleTimer -= timerWrap;

            float moveBobY = Mathf.Sin(bobTimer) * bobAmplitude * Mathf.Lerp(0.6f, 1f, inputMag) * bobIntensity;

            float idleBobY = Mathf.Sin(idleTimer) * idleBobAmplitude;

            // compute horizontal bob directly without creating intermediate locals that could be flagged as unused
            float bobX = Mathf.Lerp(
                Mathf.Sin(idleTimer * 0.5f) * bobHorizontalAmplitude * idleBobAmplitude,
                Mathf.Sin(bobTimer) * bobHorizontalAmplitude * Mathf.Lerp(0.6f, 1f, inputMag) * bobIntensity,
                bobBlend);

            float bobY = Mathf.Lerp(idleBobY, moveBobY, bobBlend);

            float breathBlend = 1f - bobBlend;
            float breathSignal = (Mathf.Sin(breathTimer) + 0.25f * Mathf.Sin(breathTimer * 2f)) * breathAmplitude * breathBlend;
            bobY += breathSignal;
            bobX += breathSignal * 0.25f;

            float breathPitch = Mathf.Sin(breathTimer + 0.4f) * breathPitchAmount * breathBlend;
            float breathRoll = Mathf.Sin(breathTimer * 0.7f + 1.2f) * breathRollAmount * breathBlend;

            // rotational bob while moving
            float movementBobPitch = -Mathf.Abs(Mathf.Sin(bobTimer)) * bobPitchAmount * bobBlend * bobIntensity;
            float movementBobRoll = Mathf.Sin(bobTimer * 0.5f) * bobRollAmount * bobBlend * bobIntensity;
            float movementBobYaw = Mathf.Cos(bobTimer * 0.5f) * bobYawAmount * bobBlend * bobIntensity;

            // stabilizer, counter-rotate to keep focus point stable
            float compPitch = 0f;
            float compYaw = 0f;
            if (useStabilizer && focusDistance > 0.01f)
            {
                compPitch = Mathf.Atan2(bobY, focusDistance) * Mathf.Rad2Deg;
                compYaw = Mathf.Atan2(-bobX, focusDistance) * Mathf.Rad2Deg;
            }

            float combinedPitch = breathPitch + movementBobPitch + compPitch;
            float combinedRoll = breathRoll + movementBobRoll;
            float combinedYaw = movementBobYaw + compYaw;

            if (mouseLook != null && mouseLook.enabled)
            {
                mouseLook.SetBreathOffset(combinedPitch, combinedRoll, combinedYaw);
            }

            float baseY = defaultCamLocalPos.y;
            Vector3 desiredLocal = new Vector3(defaultCamLocalPos.x + bobX, baseY + bobY, defaultCamLocalPos.z);
            Vector3 headOrigin = playerBody.TransformPoint(new Vector3(0f, baseY, 0f));
            Vector3 desiredWorld = playerBody.TransformPoint(desiredLocal);

            // collision math
            RaycastHit hit;
            Vector3 dir = desiredWorld - headOrigin;
            float dist = dir.magnitude;
            Vector3 targetWorld = desiredWorld;
            if (dist > 0.001f && Physics.SphereCast(headOrigin, cameraCollisionRadius, dir.normalized, out hit, dist, cameraCollisionMask, QueryTriggerInteraction.Ignore))
            {
                targetWorld = hit.point - dir.normalized * cameraCollisionOffset;
            }

            // SmoothDamp is inherently frame-rate independent: it uses Time.deltaTime
            // internally and converges at the same rate regardless of frame count.
            cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, targetWorld, ref camPosSmoothVelocity, camPositionSmoothTime);
        }
        else
        {
            // Smooth return-to-neutral: when useHeadBob is off, the camera position
            // smoothly decays back to the default local position via SmoothDamp.
            float baseY = defaultCamLocalPos.y;
            Vector3 targetLocal = new Vector3(defaultCamLocalPos.x, baseY, defaultCamLocalPos.z);
            Vector3 targetWorld = playerBody.TransformPoint(targetLocal);
            cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, targetWorld, ref camPosSmoothVelocity, camPositionSmoothTime);
            if (mouseLook != null) mouseLook.SetBreathOffset(0f, 0f);
        }

        if (useFovKick)
        {
            // fov math
            bool isRunning = movement.IsRunning && actualHorSpeed > 0.1f;
            bool isWalking = !isRunning && actualHorSpeed > 0.05f;
            float targetFov = isRunning ? runFov : (isWalking ? walkFov : defaultFov);
            // use fov change speed to tune how aggressively fov changes relative to the smooth time
            float effectiveFovSmooth = Mathf.Max(0.0001f, fovSmoothTime / Mathf.Max(0.0001f, fovChangeSpeed));
            playerCamera.fieldOfView = Mathf.SmoothDamp(playerCamera.fieldOfView, targetFov, ref fovVelocity, effectiveFovSmooth);
        }

        lastPosition = playerBody.position;
        ProfilerMarkers.HeadBob.End();
    }
}
