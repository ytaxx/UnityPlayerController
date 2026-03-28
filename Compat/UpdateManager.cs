using System;
using System.Collections.Generic;
using UnityEngine;

namespace Optimization.Core
{
    /// <summary>
    /// Small replacement for an UpdateManager. Provides Register/Unregister and invokes
    /// callbacks on Update. This keeps PlayerStamina working without the original core.
    /// </summary>
    public class UpdateManager : MonoBehaviour
    {
        public enum UpdateGroup { Normal, Late, Fixed }

        private static UpdateManager _instance;
        public static UpdateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("UpdateManager");
                    _instance = go.AddComponent<UpdateManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private struct Entry { public object owner; public UpdateGroup group; public Action<float> callback; }
        private readonly List<Entry> entries = new List<Entry>();

        public void Register(object owner, UpdateGroup group, Action<float> callback)
        {
            if (owner == null || callback == null) return;
            entries.Add(new Entry { owner = owner, group = group, callback = callback });
        }

        public void Unregister(object owner)
        {
            if (owner == null) return;
            entries.RemoveAll(e => e.owner == owner);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            // execute Normal group callbacks
            for (int i = 0; i < entries.Count; ++i)
            {
                var e = entries[i];
                if (e.group == UpdateGroup.Normal)
                {
                    try { e.callback?.Invoke(dt); } catch { }
                }
            }
        }
    }
}
