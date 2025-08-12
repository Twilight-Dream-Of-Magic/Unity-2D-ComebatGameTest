using System.Collections.Generic;
using UnityEngine;
using System;

namespace Combat {
    public class CommandQueue : MonoBehaviour {
        public float bufferWindow = 0.25f;
        public Data.InputTuningConfig tuning;
        readonly Queue<(CommandToken token, float time)> queueNormal = new();
        readonly Queue<(CommandToken token, float time)> queueCombo = new();
        public event Action<CommandToken> OnEnqueued;

        void Awake() {
            if (tuning) bufferWindow = tuning.commandBufferWindow;
        }

        // Legacy API maps to Normal channel
        public void Enqueue(CommandToken token) { EnqueueNormal(token); }
        public bool TryPeek(out CommandToken token) { return TryPeekNormal(out token); }
        public bool TryDequeue(out CommandToken token) { return TryDequeueNormal(out token); }
        public void Clear() { queueNormal.Clear(); queueCombo.Clear(); }

        // Normal channel
        public void EnqueueNormal(CommandToken token) {
            if (token == CommandToken.None) return;
            queueNormal.Enqueue((token, Time.time));
            OnEnqueued?.Invoke(token);
            Cleanup(queueNormal);
        }
        public bool TryPeekNormal(out CommandToken token) {
            Cleanup(queueNormal);
            if (queueNormal.Count > 0) { token = queueNormal.Peek().token; return true; }
            token = CommandToken.None; return false;
        }
        public bool TryDequeueNormal(out CommandToken token) {
            Cleanup(queueNormal);
            if (queueNormal.Count > 0) { token = queueNormal.Dequeue().token; return true; }
            token = CommandToken.None; return false;
        }

        // Combo channel (for future use if we want to push explicit cancel tokens)
        public void EnqueueCombo(CommandToken token) {
            if (token == CommandToken.None) return;
            queueCombo.Enqueue((token, Time.time));
            Cleanup(queueCombo);
        }
        public bool TryPeekCombo(out CommandToken token) {
            Cleanup(queueCombo);
            if (queueCombo.Count > 0) { token = queueCombo.Peek().token; return true; }
            token = CommandToken.None; return false;
        }
        public bool TryDequeueCombo(out CommandToken token) {
            Cleanup(queueCombo);
            if (queueCombo.Count > 0) { token = queueCombo.Dequeue().token; return true; }
            token = CommandToken.None; return false;
        }

        void Cleanup(Queue<(CommandToken token, float time)> q) {
            while (q.Count > 0 && Time.time - q.Peek().time > bufferWindow) q.Dequeue();
        }
    }
}