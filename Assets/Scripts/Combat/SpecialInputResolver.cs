using System.Collections.Generic;
using UnityEngine;
using Combat;
using Data;

namespace Fighter {
    public class SpecialInputResolver : MonoBehaviour {
        public FighterController fighter;
        public CommandQueue commandQueue;
        public SpecialMoveSet specialSet;
        public float historyLifetime = 0.8f;

        readonly List<(CommandToken tok, float t)> history = new();

        void Awake() {
            if (!fighter) fighter = GetComponent<FighterController>();
            if (!commandQueue) commandQueue = GetComponent<CommandQueue>();
            if (commandQueue) commandQueue.OnEnqueued += OnToken;
        }

        void OnDestroy() {
            if (commandQueue) commandQueue.OnEnqueued -= OnToken;
        }

        void OnToken(CommandToken tok) {
            history.Add((tok, Time.time));
            Cleanup();
            TryMatch();
        }

        void Cleanup() {
            float now = Time.time;
            for (int i = history.Count - 1; i >= 0; i--) if (now - history[i].t > historyLifetime) history.RemoveAt(i);
        }

        void TryMatch() {
            if (!fighter || specialSet == null || specialSet.specials == null) return;
            foreach (var sp in specialSet.specials) {
                if (MatchTail(sp.sequence, sp.maxWindowSeconds)) {
                    fighter.RequestComboCancel(sp.triggerName);
                    history.Clear();
                    break;
                }
            }
        }

        bool MatchTail(CommandToken[] seq, float window) {
            if (seq == null || seq.Length == 0) return false;
            float now = Time.time;
            int idx = seq.Length - 1;
            for (int i = history.Count - 1; i >= 0 && idx >= 0; i--) {
                var h = history[i];
                if (now - h.t > window) return false;
                if (h.tok == seq[idx]) idx--;
            }
            return idx < 0; // all matched in order within window
        }
    }
}