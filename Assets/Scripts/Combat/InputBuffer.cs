using System.Collections.Generic;
using UnityEngine;

namespace FightingGame.Combat {
    public class InputBuffer : MonoBehaviour {
        [System.Serializable]
        public struct TimedToken { public CommandToken token; public float time; }

        public float bufferWindow = 0.4f;
        private readonly List<TimedToken> buffer = new();

        public void Push(CommandToken token) {
            if (token == CommandToken.None) return;
            buffer.Add(new TimedToken { token = token, time = Time.time });
            Cleanup();
        }

        public bool Match(params CommandToken[] seq) {
            Cleanup();
            int idx = seq.Length - 1;
            for (int i = buffer.Count - 1; i >= 0 && idx >= 0; i--) {
                if (buffer[i].token == seq[idx]) idx--;
            }
            return idx < 0;
        }

        public void Clear() { buffer.Clear(); }

        private void Cleanup() {
            float now = Time.time;
            buffer.RemoveAll(t => now - t.time > bufferWindow);
        }
    }
}