using System.Collections.Generic;
using UnityEngine;
using System;

namespace Combat {
    public class CommandQueue : MonoBehaviour {
        public float bufferWindow = 0.25f;
        readonly Queue<(CommandToken token, float time)> queue = new();
        public event Action<CommandToken> OnEnqueued;

        public void Enqueue(CommandToken token) {
            if (token == CommandToken.None) return;
            queue.Enqueue((token, Time.time));
            OnEnqueued?.Invoke(token);
            Cleanup();
        }

        public bool TryPeek(out CommandToken token) {
            Cleanup();
            if (queue.Count > 0) { token = queue.Peek().token; return true; }
            token = CommandToken.None; return false;
        }

        public bool TryDequeue(out CommandToken token) {
            Cleanup();
            if (queue.Count > 0) { token = queue.Dequeue().token; return true; }
            token = CommandToken.None; return false;
        }

        void Cleanup() {
            while (queue.Count > 0 && Time.time - queue.Peek().time > bufferWindow) queue.Dequeue();
        }

        public void Clear() { queue.Clear(); }
    }
}