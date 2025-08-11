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
        IInputSource selected;

        void Awake() {
            fighter = GetComponent<FighterController>();
            sources = GetComponents<IInputSource>();
        }

        void Update() {
            if (!fighter) return;
            sources = GetComponents<IInputSource>();
            if (sources == null || sources.Length == 0) return;

            selected = PickStrictByType(sources);
            if (selected != null && selected.TryGetCommands(fighter, out var cmds)) {
                fighter.SetCommands(cmds);
            }
        }

        IInputSource PickStrictByType(IInputSource[] list) {
            // Strict separation: Player > Scripted > AI. If a type exists, ignore others entirely.
            IInputSource player = null, scripted = null, ai = null;
            foreach (var s in list) {
                if (s is PlayerInputSource && (s as Behaviour).enabled) { player = s; break; }
            }
            if (player != null) return player;
            foreach (var s in list) if (s is ScriptedInputSource && (s as Behaviour).enabled) { scripted = s; break; }
            if (scripted != null) return scripted;
            foreach (var s in list) if (s is AIInputSource && (s as Behaviour).enabled) { ai = s; break; }
            return ai;
        }
    }
}