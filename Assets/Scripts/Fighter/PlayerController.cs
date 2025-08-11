using UnityEngine;
using Combat;

namespace Fighter {
    public class PlayerController : MonoBehaviour {
        public FighterController fighter;
        public CommandQueue commandQueue;
        public CommandQueueFeeder feeder;

        void Awake() {
            if (!commandQueue) commandQueue = gameObject.AddComponent<CommandQueue>();
            commandQueue.bufferWindow = 0.35f; // 玩家更强的输入缓冲
            if (!feeder) feeder = gameObject.AddComponent<CommandQueueFeeder>();
            if (!fighter) fighter = GetComponent<FighterController>();
            feeder.fighter = fighter;
            feeder.commandQueue = commandQueue;
        }

        private void Update() {
            if (fighter == null) return;
            if (Input.GetKeyDown(KeyCode.J)) commandQueue.Enqueue(CommandToken.Light);
            if (Input.GetKeyDown(KeyCode.K)) commandQueue.Enqueue(CommandToken.Heavy);
            // directions
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) commandQueue.Enqueue(CommandToken.Up);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) commandQueue.Enqueue(CommandToken.Down);
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) commandQueue.Enqueue(fighter.facingRight ? CommandToken.Back : CommandToken.Forward);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) commandQueue.Enqueue(fighter.facingRight ? CommandToken.Forward : CommandToken.Back);

            FighterCommands c = new FighterCommands {
                moveX = Input.GetAxisRaw("Horizontal"),
                jump = Input.GetKeyDown(KeyCode.Space),
                crouch = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
                light = false,
                heavy = false,
                block = Input.GetKey(KeyCode.LeftShift),
                dodge = Input.GetKeyDown(KeyCode.L)
            };
            fighter.SetCommands(c);
        }
    }
}