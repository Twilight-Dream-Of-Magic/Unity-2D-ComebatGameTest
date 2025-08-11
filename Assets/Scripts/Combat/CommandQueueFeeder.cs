using UnityEngine;
using Combat;

namespace Fighter {
    public class CommandQueueFeeder : MonoBehaviour {
        public FighterController fighter;
        public CommandQueue commandQueue;

        void Awake() {
            if (!fighter) fighter = GetComponent<FighterController>();
            if (!commandQueue) commandQueue = GetComponent<CommandQueue>();
            if (!commandQueue && fighter) commandQueue = fighter.gameObject.AddComponent<CommandQueue>();
        }

        void Update() {
            if (!fighter || !commandQueue) return;
            if (commandQueue.TryPeek(out var tok)) {
                switch (tok) {
                    case CommandToken.Light:
                        fighter.RequestComboCancel("Light");
                        commandQueue.TryDequeue(out _);
                        break;
                    case CommandToken.Heavy:
                        fighter.RequestComboCancel("Heavy");
                        commandQueue.TryDequeue(out _);
                        break;
                }
            }
        }
    }
}