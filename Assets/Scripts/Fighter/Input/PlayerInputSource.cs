using UnityEngine;
using Combat;

namespace Fighter.InputSystem {
    /// <summary>
    /// Player keyboard input implementation of IInputSource. Produces FighterCommands
    /// and enqueues command tokens for specials/combos.
    /// </summary>
    public class PlayerInputSource : MonoBehaviour, IInputSource {
        public CommandQueue commandQueue;
        public CommandQueueFeeder feeder;
        public float horizontalScale = 1f;

        void Awake() {
            if (!commandQueue) commandQueue = gameObject.AddComponent<CommandQueue>();
            commandQueue.bufferWindow = 0.35f; // more forgiving buffer for player
            if (!feeder) feeder = gameObject.AddComponent<CommandQueueFeeder>();
            feeder.commandQueue = commandQueue;
            if (!feeder.fighter) feeder.fighter = GetComponent<FighterController>();
        }

        public bool TryGetCommands(FighterController fighter, out FighterCommands commands) {
            commands = default;
            if (!fighter) return false;

            // token enqueue on key down
            if (Input.GetKeyDown(KeyCode.J)) commandQueue.Enqueue(CommandToken.Light);
            if (Input.GetKeyDown(KeyCode.K)) commandQueue.Enqueue(CommandToken.Heavy);
            if (Input.GetKey(KeyCode.J) && Input.GetKeyDown(KeyCode.K) || Input.GetKey(KeyCode.K) && Input.GetKeyDown(KeyCode.J)) commandQueue.Enqueue(CommandToken.Throw);
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) commandQueue.Enqueue(CommandToken.Up);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) commandQueue.Enqueue(CommandToken.Down);
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) commandQueue.Enqueue(fighter.facingRight ? CommandToken.Back : CommandToken.Forward);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) commandQueue.Enqueue(fighter.facingRight ? CommandToken.Forward : CommandToken.Back);

            // per-frame command snapshot
            commands = new FighterCommands {
                moveX = Input.GetAxisRaw("Horizontal") * horizontalScale,
                jump = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow),
                crouch = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
                light = false,
                heavy = false,
                block = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                dodge = Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.RightControl) || Input.GetKeyDown(KeyCode.LeftControl)
            };
            return true;
        }
    }
}