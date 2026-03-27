using UnityEngine;

public class HeadBob : MonoBehaviour
{
    // components
    [Header("Components")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform playerBody;

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
    [SerializeField] private float breathHorizontalScale = 0.25f;

    // bob look
    [Header("Bob Look")]
    [SerializeField] private float bobPitchAmount = 0.6f;
    [SerializeField] private float bobRollAmount = 0.4f;
    [SerializeField] private float bobYawAmount = 0.4f;
    [SerializeField, Range(0f, 1f)] private float bobIntensity = 0.5f;

    // sprint bob
    [Header("Sprint Bob")]
    [SerializeField] private float runBobSpeedMultiplier = 1.5f;

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
    [SerializeField] private float fovSmoothTime = 0.12f;

    // camera collision
    [Header("Camera Collision")]
    [SerializeField] private float cameraCollisionRadius = 0.12f;
    [SerializeField] private float cameraCollisionOffset = 0.05f;
    [SerializeField] private LayerMask cameraCollisionMask = ~0;

    // components and state
    private PlayerMovement movement;
    private MouseLook mouseLook;

    private float bobTimer;
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
            defaultCamLocalPos = playerCamera.transform.localPosition;
            playerCamera.fieldOfView = defaultFov;
            playerCamera.nearClipPlane = 0.01f;
        }

        if (playerBody != null) lastPosition = playerBody.position;
    }

    private void LateUpdate()
    {
        if (movement == null || playerCamera == null || playerBody == null) return;
        if (!movement.IsGrounded)
        {
            lastPosition = playerBody.position;
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
            float moveBlendTarget = inputBlend * movementBlend;
            bobBlend = Mathf.SmoothDamp(bobBlend, moveBlendTarget, ref bobBlendVelocity, bobBlendSmoothTime);

            // use serialized run multiplier instead of hardcoded 1.5f
            float speedMultiplier = movement.IsRunning ? runBobSpeedMultiplier : 1.0f;
            float speedFactor = Mathf.Clamp01(movement.CurrentVelocity.magnitude / Mathf.Max(0.01f, maxSpeed));
            bobTimer += Time.deltaTime * bobFrequency * Mathf.Lerp(0.9f, 1.2f, speedFactor) * speedMultiplier;

            float moveBobY = Mathf.Sin(bobTimer) * bobAmplitude * Mathf.Lerp(0.6f, 1f, inputMag) * bobIntensity;

            float idlePhase = Time.time * idleBobFrequency;
            float idleBobY = Mathf.Sin(idlePhase) * idleBobAmplitude;

            float bobX = Mathf.Lerp(
                Mathf.Sin(idlePhase * 0.5f) * bobHorizontalAmplitude * idleBobAmplitude,
                Mathf.Sin(bobTimer) * bobHorizontalAmplitude * Mathf.Lerp(0.6f, 1f, inputMag) * bobIntensity,
                bobBlend);

            float bobY = Mathf.Lerp(idleBobY, moveBobY, bobBlend);

            float breathBlend = 1f - bobBlend;
            float breathSignal = (Mathf.Sin(Time.time * breathFrequency) + 0.25f * Mathf.Sin(Time.time * breathFrequency * 2f)) * breathAmplitude * breathBlend;
            bobY += breathSignal;
            // use serialized breathHorizontalScale instead of magic 0.25f
            bobX += breathSignal * breathHorizontalScale;

            float breathPitch = Mathf.Sin(Time.time * breathFrequency + 0.4f) * breathPitchAmount * breathBlend;
            float breathRoll = Mathf.Sin(Time.time * breathFrequency * 0.7f + 1.2f) * breathRollAmount * breathBlend;

            // rotational bob while moving
            float movementBobPitch = -Mathf.Abs(Mathf.Sin(bobTimer)) * bobPitchAmount * bobBlend * bobIntensity;
            float movementBobRoll = Mathf.Sin(bobTimer * 0.5f) * bobRollAmount * bobBlend * bobIntensity;
            float movementBobYaw = Mathf.Cos(bobTimer * 0.5f) * bobYawAmount * bobBlend * bobIntensity;

            // stabilizer -- counter-rotate to keep focus point stable
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

            // null check before enabled check to prevent NullReferenceException
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

            playerCamera.transform.position = Vector3.SmoothDamp(playerCamera.transform.position, targetWorld, ref camPosSmoothVelocity, camPositionSmoothTime);
        }
        else
        {
            float baseY = defaultCamLocalPos.y;
            Vector3 targetLocal = new Vector3(defaultCamLocalPos.x, baseY, defaultCamLocalPos.z);
            Vector3 targetWorld = playerBody.TransformPoint(targetLocal);
            playerCamera.transform.position = Vector3.SmoothDamp(playerCamera.transform.position, targetWorld, ref camPosSmoothVelocity, camPositionSmoothTime);

            // match the 3-argument signature used in the bob branch to keep calls consistent
            if (mouseLook != null) mouseLook.SetBreathOffset(0f, 0f, 0f);
        }

        if (useFovKick)
        {
            // fov math -- removed fovChangeSpeed division as it created a confusing
            // compound tuning relationship. fovSmoothTime now directly controls blend speed.
            bool isRunning = movement.IsRunning && actualHorSpeed > 0.1f;
            bool isWalking = !isRunning && actualHorSpeed > 0.05f;
            float targetFov = isRunning ? runFov : (isWalking ? walkFov : defaultFov);
            playerCamera.fieldOfView = Mathf.SmoothDamp(playerCamera.fieldOfView, targetFov, ref fovVelocity, fovSmoothTime);
        }

        lastPosition = playerBody.position;
    }
}
