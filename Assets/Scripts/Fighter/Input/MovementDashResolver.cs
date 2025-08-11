using UnityEngine;
using Combat;

namespace Fighter.InputSystem {
    /// <summary>
    /// Detects forward-forward / back-back within a short window from CommandQueue tokens
    /// and requests dash or backdash on the fighter.
    /// </summary>
    public class MovementDashResolver : MonoBehaviour {
        public FighterController fighter;
        public CommandQueue commandQueue;
        public float dashWindow = 0.25f;

        CommandToken lastDir = CommandToken.Neutral;
        float lastTime;

        void Awake() {
            if (!fighter) fighter = GetComponent<FighterController>();
            if (!commandQueue) commandQueue = GetComponent<CommandQueue>();
            if (!commandQueue && fighter) commandQueue = fighter.gameObject.AddComponent<CommandQueue>();
            if (commandQueue) commandQueue.OnEnqueued += OnToken;
        }
        void OnDestroy() { if (commandQueue) commandQueue.OnEnqueued -= OnToken; }

        void OnToken(CommandToken tok) {
            if (tok != CommandToken.Forward && tok != CommandToken.Back) return;
            float now = Time.time;
            if (tok == lastDir && (now - lastTime) <= dashWindow) {
                bool isBack = (tok == CommandToken.Back);
                fighter.RequestDash(isBack);
                lastTime = 0; lastDir = CommandToken.Neutral; // reset
            } else {
                lastDir = tok; lastTime = now;
            }
        }
    }
}