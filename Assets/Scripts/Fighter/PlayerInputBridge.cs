using UnityEngine;

namespace Fighter {
    public class PlayerInputBridge : MonoBehaviour {
        public FighterController fighter;
        private void Update() {
            if (fighter == null) return;
            bool jumpKey = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetButtonDown("Jump");
            bool crouchKey = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            bool blockKey = Input.GetKey(KeyCode.LeftShift);
            bool dodgeKey = Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.LeftControl);

            FighterCommands c = new FighterCommands {
                moveX = Input.GetAxisRaw("Horizontal"),
                jump = jumpKey,
                crouch = crouchKey,
                light = Input.GetKeyDown(KeyCode.J),
                heavy = Input.GetKeyDown(KeyCode.K),
                block = blockKey,
                dodge = dodgeKey
            };
            fighter.SetCommands(c);
        }
    }
}