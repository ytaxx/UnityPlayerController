using UnityEngine.Profiling;

namespace Optimization.Core
{
    /// <summary>
    /// Minimal profiler marker shim. Uses Unity's Profiler if available, otherwise acts as a no-op.
    /// Matches the minimal API used in the player scripts.
    /// </summary>
    public static class ProfilerMarkers
    {
        public static class PlayerMovement
        {
            public static void Begin() { Profiler.BeginSample("PlayerMovement"); }
            public static void End() { Profiler.EndSample(); }
        }

        public static class HeadBob
        {
            public static void Begin() { Profiler.BeginSample("HeadBob"); }
            public static void End() { Profiler.EndSample(); }
        }
    }
}
