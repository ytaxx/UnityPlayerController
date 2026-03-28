namespace Ytax.Core.Events
{
    /// <summary>
    /// Lightweight event type used by PlayerStamina.
    /// </summary>
    public class StaminaChangedEvent
    {
        public float Percent { get; }
        public bool IsSprinting { get; }

        public StaminaChangedEvent(float percent, bool isSprinting)
        {
            Percent = percent;
            IsSprinting = isSprinting;
        }
    }
}
