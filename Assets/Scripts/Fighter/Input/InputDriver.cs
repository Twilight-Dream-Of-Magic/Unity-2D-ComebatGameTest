using UnityEngine;

namespace Fighter.InputSystem {
    /// <summary>
    /// Pulls commands from any IInputSource attached to the same GameObject and applies them to FighterController.
    /// Strictly selects a single authority source (Player > Scripted > AI) at Awake and disables others to
    /// prevent control contention.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class InputDriver : MonoBehaviour {
        FighterController fighter;
        IInputSource[] sources;
        IInputSource selected;

        void Awake() {
            fighter = GetComponent<FighterController>();
            sources = GetComponents<IInputSource>();
            selected = PickStrictByType(sources);
            EnforceSingleAuthority(selected, sources);
        }

        void Update() {
            if (!fighter || selected == null) return;
            if (selected.TryGetCommands(fighter, out var cmds)) {
                fighter.SetCommands(cmds);
            }
        }

        IInputSource PickStrictByType(IInputSource[] list) {
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

        void EnforceSingleAuthority(IInputSource winner, IInputSource[] all) {
            if (all == null) return;
            foreach (var s in all) {
                if (s == null) continue;
                var b = s as Behaviour;
                if (b == null) continue;
                b.enabled = (s == winner);
            }
            if (winner == null) {
                Debug.LogWarning($"[InputDriver] No IInputSource enabled on {name}. Fighter will be idle.");
            }
        }

        /// <summary>
        /// Optional: force switch the active input source at runtime (e.g., for demo takeover).
        /// </summary>
        public void ForceSelect(IInputSource source) {
            if (source == null) return;
            selected = source;
            EnforceSingleAuthority(selected, sources);
        }
    }
}