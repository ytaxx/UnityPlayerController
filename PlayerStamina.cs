using System;
using UnityEngine;
using Optimization.Core;
using Ytax.Core;
using Ytax.Core.Events;

/// <summary>
/// Owns player stamina values and runtime drain/recharge logic.
/// - 10s full sprint budget from 100% to 0%
/// - 5s full recharge from 0% to 100%
///
/// <para><b>Architecture:</b> Legacy delegate <see cref="OnStaminaStateChanged"/>
/// is retained for backward compat. New subscribers should use
/// <see cref="GameEventBus"/>{<see cref="StaminaChangedEvent"/>}.</para>
///
/// PlayerMovement drives sprint intent via <see cref="SetSprinting(bool, bool)"/>.
/// UI reads <see cref="StaminaPercent"/> and <see cref="IsSprinting"/>.
/// </summary>
public class PlayerStamina : MonoBehaviour
{
    // Percent (0..1) at or below which stamina is considered depleted.
    private const float DepletedPercentThresholdConst = 0.04f;

    [Header("Stamina Timing")]
    [Tooltip("Seconds from full stamina to empty while sprinting continuously.")]
    [SerializeField, Min(0.1f)] private float sprintDurationSeconds = 10f;

    [Tooltip("Seconds from empty stamina to full while not sprinting.")]
    [SerializeField, Min(0.1f)] private float rechargeDurationSeconds = 5f;

    [Header("Runtime")]
    [Tooltip("Current stamina value.")]
    [SerializeField] private float currentStamina = 100f;

    [SerializeField, Min(1f)] private float maxStamina = 100f;

    private bool isSprinting;

    [Header("Behavior")]
    [Tooltip("Cooldown (seconds) after stopping sprint before recharge begins.")]
    [SerializeField, Min(0f)] private float rechargeCooldownAfterSprint = 0.5f;

    // runtime
    private float rechargeDelayTimer = 0f;
    private bool runHeldIntent = false;
    // When the player depletes stamina while holding run, block recharge until they release run.
    private bool blockRechargeUntilRunReleased = false;

    /// <summary>True while a recharge delay or cooldown is active.</summary>
    public bool IsInRechargeDelay => rechargeDelayTimer > 0f;

    /// <summary>Percent threshold used to decide when stamina is considered empty (0..1).</summary>
    public float DepletedPercentThreshold => DepletedPercentThresholdConst;

    /// <summary>True when stamina is low enough to be treated as depleted.</summary>
    public bool IsDepleted => StaminaPercent <= DepletedPercentThresholdConst;

    /// <summary>Raised when stamina percent or sprint state changes.</summary>
    public event Action<float, bool> OnStaminaStateChanged;

    /// <summary>Normalized stamina [0..1] for UI.</summary>
    public float StaminaPercent => Mathf.Clamp01(currentStamina / maxStamina);

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;

    /// <summary>True while movement is actively sprinting this frame.</summary>
    public bool IsSprinting => isSprinting;

    /// <summary>Movement checks this before allowing sprint speed.</summary>
    public bool CanSprint => StaminaPercent > DepletedPercentThresholdConst;

    private float DrainPerSecond => maxStamina / sprintDurationSeconds;
    private float RechargePerSecond => maxStamina / rechargeDurationSeconds;

    private void Awake()
    {
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    private void OnEnable()
    {
        OnStaminaStateChanged?.Invoke(StaminaPercent, isSprinting);
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Register(this, UpdateManager.UpdateGroup.Normal, OnTick);
    }

    private void OnDisable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Unregister(this);
    }

    private void OnTick(float dt)
    {
        ProfilerMarkers.PlayerMovement.Begin();
        float beforePercent = StaminaPercent;
        bool beforeSprint = isSprinting;

        if (isSprinting)
        {
            currentStamina -= DrainPerSecond * dt;
            currentStamina = Mathf.Max(0f, currentStamina);

            if (StaminaPercent <= DepletedPercentThresholdConst)
            {
                currentStamina = 0f;
                isSprinting = false;

                if (runHeldIntent)
                {
                    blockRechargeUntilRunReleased = true;
                    rechargeDelayTimer = 0f;
                }
                else
                {
                    rechargeDelayTimer = rechargeCooldownAfterSprint;
                    blockRechargeUntilRunReleased = false;
                }
            }
        }
        else if (currentStamina < maxStamina)
        {
            if (runHeldIntent)
            {
                rechargeDelayTimer = 0f;
                if (StaminaPercent <= DepletedPercentThresholdConst)
                {
                    blockRechargeUntilRunReleased = true;
                }
            }
            else
            {
                if (blockRechargeUntilRunReleased)
                {
                    rechargeDelayTimer = rechargeCooldownAfterSprint;
                    blockRechargeUntilRunReleased = false;
                }

                if (rechargeDelayTimer > 0f)
                {
                    rechargeDelayTimer = Mathf.Max(0f, rechargeDelayTimer - dt);
                }
                else
                {
                    currentStamina += RechargePerSecond * dt;
                    currentStamina = Mathf.Min(currentStamina, maxStamina);
                }
            }
        }

        bool staminaChanged = !Mathf.Approximately(beforePercent, StaminaPercent);
        bool sprintChanged = beforeSprint != isSprinting;
        if (staminaChanged || sprintChanged)
        {
            OnStaminaStateChanged?.Invoke(StaminaPercent, isSprinting);
            GameEventBus.Publish(new StaminaChangedEvent(StaminaPercent, isSprinting));
        }
        ProfilerMarkers.PlayerMovement.End();
    }

    /// <summary>
    /// Called by PlayerMovement once per frame.
    /// Sprinting can only remain true while stamina is available.
    /// </summary>
    public void SetSprinting(bool sprinting, bool runHeld)
    {
        // update run-held intent
        bool wasHoldingRun = runHeldIntent;
        runHeldIntent = runHeld;

        // If the player starts holding run, clear any pending recharge delay and prevent recharge
        if (runHeldIntent && !wasHoldingRun)
        {
            rechargeDelayTimer = 0f;
        }

        bool next = sprinting && CanSprint;
        if (isSprinting == next) return;

        isSprinting = next;
        OnStaminaStateChanged?.Invoke(StaminaPercent, isSprinting);
        GameEventBus.Publish(new StaminaChangedEvent(StaminaPercent, isSprinting));
    }
}
