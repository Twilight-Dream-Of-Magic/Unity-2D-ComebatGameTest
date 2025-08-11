using UnityEngine;

namespace Fighter.InputSystem {
    /// <summary>
    /// Abstraction for producing FighterCommands each frame. Implementations can be player input,
    /// AI logic, replay, or network input. Attach as a component on the same GameObject as FighterController.
    /// </summary>
    public interface IInputSource {
        /// <summary>
        /// Optionally produce a command snapshot for this frame.
        /// Return true if commands are valid and should be applied.
        /// </summary>
        bool TryGetCommands(FighterController fighter, out FighterCommands commands);
    }
}