using UnityEngine;
using Combat;

namespace Fighter.InputSystem {
    /// <summary>
    /// Player keyboard input implementation of IInputSource. Produces FighterCommands
    /// and enqueues command tokens for specials/combos.
    /// </summary>
    [DefaultExecutionOrder(40)]
    public class PlayerInputSource : MonoBehaviour, IInputSource {
        public CommandQueue commandQueue;
        public CommandQueueFeeder feeder;
        public float horizontalScale = 1f;

        void Awake() {
            if (!commandQueue) commandQueue = GetComponent<CommandQueue>();
            if (!commandQueue) commandQueue = gameObject.AddComponent<CommandQueue>();
            commandQueue.bufferWindow = 0.35f; // more forgiving buffer for player
            if (!feeder) feeder = gameObject.AddComponent<CommandQueueFeeder>();
            feeder.commandQueue = commandQueue;
            if (!feeder.fighter) feeder.fighter = GetComponent<FighterController>();
        }

        public bool TryGetCommands(FighterController fighter, out FighterCommands commands) {
            commands = default;
            if (!fighter) return false;

            // prioritize Throw if J+K simultaneous this frame
            bool jDown = Input.GetKeyDown(KeyCode.J);
            bool kDown = Input.GetKeyDown(KeyCode.K);
            bool jHeld = Input.GetKey(KeyCode.J);
            bool kHeld = Input.GetKey(KeyCode.K);
            bool throwThisFrame = (jHeld && kDown) || (kHeld && jDown);
            if (throwThisFrame) {
                commandQueue.EnqueueNormal(CommandToken.Throw);
            } else {
                if (jDown) commandQueue.EnqueueNormal(CommandToken.Light);
                if (kDown) commandQueue.EnqueueNormal(CommandToken.Heavy);
            }

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) commandQueue.EnqueueNormal(CommandToken.Up);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) commandQueue.EnqueueNormal(CommandToken.Down);
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) commandQueue.EnqueueNormal(fighter.facingRight ? CommandToken.Back : CommandToken.Forward);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) commandQueue.EnqueueNormal(fighter.facingRight ? CommandToken.Forward : CommandToken.Back);

            // per-frame command snapshot
            commands = new FighterCommands {
                moveX = Input.GetAxisRaw("Horizontal") * horizontalScale,
                jump = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow),
                crouch = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
                light = jDown,
                heavy = kDown,
                block = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                dodge = Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.RightControl) || Input.GetKeyDown(KeyCode.LeftControl)
            };
            return true;
        }
    }
}