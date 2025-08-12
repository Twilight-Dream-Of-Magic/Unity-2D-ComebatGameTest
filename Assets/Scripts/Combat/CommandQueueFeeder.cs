using UnityEngine;
using Combat;

namespace Fighter {
    [DefaultExecutionOrder(50)]
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

            if (commandQueue.TryPeekCombo(out var comboTok)) {
                commandQueue.TryDequeueCombo(out _);
                if (comboTok == CommandToken.Light || comboTok == CommandToken.Heavy) {
                    fighter.RequestComboCancel(comboTok == CommandToken.Light ? "Light" : "Heavy");
                    return;
                }
            }

            if (commandQueue.TryPeekNormal(out var tok)) {
                if (tok == CommandToken.Light || tok == CommandToken.Heavy) {
                    string currentName = fighter.StateMachine?.Current?.Name ?? string.Empty;
                    bool inAttack = currentName.StartsWith("Attack");
                    commandQueue.TryDequeueNormal(out _);
                    if (inAttack) {
                        fighter.RequestComboCancel(tok == CommandToken.Light ? "Light" : "Heavy");
                    } else {
                        if (tok == CommandToken.Light) fighter.StateMachine.SetState(fighter.AttackLight);
                        else fighter.StateMachine.SetState(fighter.AttackHeavy);
                    }
                } else if (tok == CommandToken.Throw) {
                    commandQueue.TryDequeueNormal(out _);
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