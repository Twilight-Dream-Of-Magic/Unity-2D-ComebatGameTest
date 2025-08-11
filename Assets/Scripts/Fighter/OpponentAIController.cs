using UnityEngine;
using Systems;
using Fighter.AI;
using Combat;

namespace Fighter {
    public class OpponentAIController : MonoBehaviour {
        public FighterController fighter;
        public AIConfig easy;
        public AIConfig normal;
        public AIConfig hard;

        [Header("Behavior Tuning")]
        public float minStateTime = 0.25f;
        public float attackRange = 1.35f;
        public float engageDistance = 1.9f;   // switch to approach if beyond this
        public float disengageDistance = 2.3f; // consider too far
        public float microStepSpeed = 0.25f;   // slight step-in during attack
        public float reactBlockProbability = 0.7f;
        public float specialProbability = 0.2f; // chance to attempt special if meter allows

        float attackCd;
        float stateTimer;

        enum AIState { Approach, Attack, Retreat, EvadeBlock }
        AIState state = AIState.Approach;

        CommandQueue commandQueue;
        CommandQueueFeeder feeder;

        void Awake() {
            commandQueue = GetComponent<CommandQueue>();
            if (!commandQueue) commandQueue = gameObject.AddComponent<CommandQueue>();
            feeder = GetComponent<CommandQueueFeeder>();
            if (!feeder) feeder = gameObject.AddComponent<CommandQueueFeeder>();
            feeder.fighter = fighter ? fighter : GetComponent<FighterController>();
            feeder.commandQueue = commandQueue;
        }

        AIConfig CurrentConfig() {
            switch (GameManager.Instance ? GameManager.Instance.difficulty : Difficulty.Normal) {
                case Difficulty.Easy: return easy ? easy : normal;
                case Difficulty.Hard: return hard ? hard : normal;
                default: return normal;
            }
        }

        private void Update() {
            if (fighter == null || fighter.opponent == null) return;
            var cfg = CurrentConfig();
            var opp = fighter.opponent.GetComponent<FighterController>();
            float dx = fighter.opponent.position.x - fighter.transform.position.x;
            float dist = Mathf.Abs(dx);
            bool oppThreat = opp && opp.debugHitActive && Random.value < reactBlockProbability;

            // state transitions with dwell time and hysteresis
            stateTimer += Time.deltaTime;
            if (stateTimer >= minStateTime) {
                var next = state;
                if (oppThreat) next = AIState.EvadeBlock;
                else if (dist <= attackRange) next = AIState.Attack;
                else if (dist >= disengageDistance) next = AIState.Approach;
                else if (dist > attackRange && dist < engageDistance) next = AIState.Approach;
                else next = AIState.Retreat;

                if (next != state) { state = next; stateTimer = 0f; }
            }

            FighterCommands c = new FighterCommands();
            switch (state) {
                case AIState.Approach:
                    c.moveX = Mathf.Sign(dx);
                    break;
                case AIState.Retreat:
                    c.moveX = -Mathf.Sign(dx);
                    break;
                case AIState.EvadeBlock:
                    c.block = true;
                    c.moveX = -Mathf.Sign(dx) * 0.5f;
                    break;
                case AIState.Attack:
                    // slight step-in if a bit outside
                    if (dist > attackRange * 0.9f) c.moveX = Mathf.Sign(dx) * microStepSpeed; else c.moveX = 0f;
                    if (attackCd <= 0 && dist <= attackRange + 0.25f) {
                        bool trySpecial = Random.value < specialProbability && fighter.meter >= 500;
                        if (trySpecial) {
                            // simple special: Down, Forward, Heavy => "Super" (configured in SpecialMoveSet)
                            commandQueue.Enqueue(CommandToken.Down);
                            commandQueue.Enqueue(Mathf.Sign(dx) > 0 ? CommandToken.Forward : CommandToken.Back);
                            commandQueue.Enqueue(CommandToken.Heavy);
                        } else {
                            bool useLight = Random.value < 0.75f;
                            commandQueue.Enqueue(useLight ? CommandToken.Light : CommandToken.Heavy);
                        }
                        attackCd = Random.Range(cfg.attackCooldownRange.x * 0.8f, cfg.attackCooldownRange.y * 0.9f);
                    }
                    break;
            }

            if (attackCd > 0) attackCd -= Time.deltaTime;
            fighter.SetCommands(c);
        }
    }
}