using UnityEngine;

namespace Fighter.InputSystem {
    /// <summary>
    /// Pulls commands from any IInputSource attached to the same GameObject and applies them to FighterController.
    /// This decouples the fighter from concrete input implementations and replaces legacy controller wrappers.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class InputDriver : MonoBehaviour {
        FighterController fighter;
        IInputSource[] sources;

        void Awake() {
            fighter = GetComponent<FighterController>();
            sources = GetComponents<IInputSource>();
        }

        void Update() {
            if (!fighter) return;
            // refresh for dynamic add/remove
            if (sources == null || sources.Length == 0) sources = GetComponents<IInputSource>();
            if (sources == null || sources.Length == 0) return;

            foreach (var s in sources) {
                if (s != null && s.TryGetCommands(fighter, out var cmds)) { fighter.SetCommands(cmds); break; }
            }
        }
    }
}