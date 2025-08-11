using UnityEngine;
using Combat;

namespace Fighter.InputSystem {
    /// <summary>
    /// Scripted demo input source that plays a showcase timeline:
    /// fast walk-in -> L-L-H x2 -> block tap -> dodge -> Super (D,F/H) -> rest -> loop.
    /// Uses explicit per-phase flags to avoid missed inputs.
    /// </summary>
    public class ScriptedInputSource : MonoBehaviour, IInputSource {
        public FighterController fighter;
        public Transform opponent;
        public CommandQueue commandQueue;
        public CommandQueueFeeder feeder;

        [Header("Pacing")]
        public float walkInSeconds = 0.7f;
        public float comboGap = 0.25f;
        public float restBetweenRounds = 1.0f;

        float phaseT;
        int phase; // 0 walk, 1 combo1, 2 combo2, 3 block+dodge, 4 super, 5 rest

        // flags per phase
        bool c1L1, c1L2, c1H;
        bool c2L1, c2L2, c2H;
        bool sDown, sFwd, sHeavy;

        void Awake() {
            if (!fighter) fighter = GetComponent<FighterController>();
            if (!opponent && fighter && fighter.opponent) opponent = fighter.opponent;
            if (!commandQueue) commandQueue = gameObject.AddComponent<CommandQueue>();
            if (!feeder) feeder = gameObject.AddComponent<CommandQueueFeeder>();
            feeder.fighter = fighter; feeder.commandQueue = commandQueue;
        }

        void OnEnable() { phase = 0; phaseT = 0f; ResetFlags(); }

        void ResetFlags() { c1L1 = c1L2 = c1H = c2L1 = c2L2 = c2H = sDown = sFwd = sHeavy = false; }

        public bool TryGetCommands(FighterController f, out FighterCommands commands) {
            commands = default;
            if (!fighter) return false;

            phaseT += Time.deltaTime;
            var cmd = new FighterCommands();

            switch (phase) {
                case 0: // walk-in
                    cmd.moveX = 1.0f;
                    if (phaseT >= walkInSeconds) { phase = 1; phaseT = 0f; ResetFlags(); }
                    break;
                case 1: // L-L-H
                    if (!c1L1 && phaseT >= 0.00f) { commandQueue.Enqueue(CommandToken.Light); c1L1 = true; }
                    if (!c1L2 && phaseT >= 0.18f) { commandQueue.Enqueue(CommandToken.Light); c1L2 = true; }
                    if (!c1H  && phaseT >= 0.36f) { commandQueue.Enqueue(CommandToken.Heavy); c1H = true; }
                    if (phaseT >= 0.36f + comboGap) { phase = 2; phaseT = 0f; ResetFlags(); }
                    break;
                case 2: // L-L-H second
                    if (!c2L1 && phaseT >= 0.00f) { commandQueue.Enqueue(CommandToken.Light); c2L1 = true; }
                    if (!c2L2 && phaseT >= 0.18f) { commandQueue.Enqueue(CommandToken.Light); c2L2 = true; }
                    if (!c2H  && phaseT >= 0.36f) { commandQueue.Enqueue(CommandToken.Heavy); c2H = true; }
                    if (phaseT >= 0.36f + comboGap) { phase = 3; phaseT = 0f; ResetFlags(); }
                    break;
                case 3: // short block then dodge
                    cmd.block = phaseT < 0.25f;
                    if (Mathf.Abs(phaseT - 0.35f) < Time.deltaTime * 1.1f) cmd.dodge = true;
                    if (phaseT >= 0.6f) { phase = 4; phaseT = 0f; ResetFlags(); }
                    break;
                case 4: // Super D,F,H
                    if (!sDown) { commandQueue.Enqueue(CommandToken.Down); sDown = true; }
                    if (!sFwd && phaseT >= 0.12f) {
                        float dx = opponent ? Mathf.Sign(opponent.position.x - fighter.transform.position.x) : (fighter.facingRight ? 1f : -1f);
                        commandQueue.Enqueue(dx > 0 ? CommandToken.Forward : CommandToken.Back); sFwd = true;
                    }
                    if (!sHeavy && phaseT >= 0.24f) { commandQueue.Enqueue(CommandToken.Heavy); sHeavy = true; }
                    if (phaseT >= 0.24f + 0.2f) { phase = 5; phaseT = 0f; ResetFlags(); }
                    break;
                case 5: // rest
                    if (phaseT >= restBetweenRounds) { phase = 0; phaseT = 0f; ResetFlags(); }
                    break;
            }

            commands = cmd;
            return true;
        }
    }
}