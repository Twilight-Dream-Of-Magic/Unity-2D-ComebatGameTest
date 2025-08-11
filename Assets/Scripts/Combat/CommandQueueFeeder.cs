using UnityEngine;
using Combat;

namespace Fighter {
    /// <summary>
    /// Legacy adapter to translate simple command tokens to FighterController combo cancels.
    /// Prefer using InputSystem.InputDriver + IInputSource and let SpecialInputResolver/ComboHandler consume queues.
    /// Enhancement: If not currently attacking, a Light/Heavy token will start a new attack immediately.
    /// Also supports Throw when near opponent (J+K default).
    /// </summary>
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
                if (tok == CommandToken.Light || tok == CommandToken.Heavy) {
                    string currentName = fighter.StateMachine?.Current?.Name ?? string.Empty;
                    bool inAttack = currentName.StartsWith("Attack");
                    commandQueue.TryDequeue(out _);
                    if (inAttack) {
                        fighter.RequestComboCancel(tok == CommandToken.Light ? "Light" : "Heavy");
                    } else {
                        if (tok == CommandToken.Light) fighter.StateMachine.SetState(fighter.AttackLight);
                        else fighter.StateMachine.SetState(fighter.AttackHeavy);
                    }
                } else if (tok == CommandToken.Throw) {
                    // simple proximity check
                    commandQueue.TryDequeue(out _);
                    if (fighter.opponent && Vector2.Distance(fighter.transform.position, fighter.opponent.position) < 1.0f) {
                        fighter.StateMachine.SetState(fighter.Throw);
                        var opp = fighter.opponent.GetComponent<FighterController>();
                        fighter.ApplyThrowOn(opp);
                    }
                }
            }
        }
    }
}