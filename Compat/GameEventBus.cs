using System;
using UnityEngine;

namespace Ytax.Core
{
    /// <summary>
    /// Minimal compatibility shim for the project's event bus.
    /// Publish methods are no-ops so consuming code can remain unchanged.
    /// </summary>
    public static class GameEventBus
    {
        public static void Publish<T>(T evt) { }
        public static void Publish(object evt) { }
    }

    /// <summary>
    /// Minimal global cache used by PlayerMovement to register a player transform.
    /// </summary>
    public static class GlobalCache
    {
        public static Transform PlayerTransform { get; private set; }
        public static void RegisterPlayer(Transform t) { PlayerTransform = t; }
    }
}
