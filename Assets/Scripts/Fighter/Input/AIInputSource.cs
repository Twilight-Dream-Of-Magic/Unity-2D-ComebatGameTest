using UnityEngine;
using Combat;
using Systems;

namespace Fighter.InputSystem {
    /// <summary>
    /// AI implementation of IInputSource. Encapsulates the previous OpponentAIController logic to produce
    /// approach/retreat/block/attack intents and enqueues special/attack tokens.
    /// </summary>
    public class AIInputSource : MonoBehaviour, IInputSource {
        public FighterController fighter;
        public AI.AIConfig easy;
        public AI.AIConfig normal;
        public AI.AIConfig hard;

        [Header("Behavior Tuning")]
        public float minStateTime = 0.25f;
        public float attackRange = 1.35f;
        public float engageDistance = 1.9f;
        public float disengageDistance = 2.3f;
        public float microStepSpeed = 0.25f;
        public float reactBlockProbability = 0.7f;
        public float specialProbability = 0.2f;
        [Header("Stability")]
        public float minBlockHold = 0.35f;
        public float specialCooldown = 2.5f;
        public float moveSmooth = 8f;
        public float retreatHold = 0.6f;
        public float jumpCooldown = 1.8f;
        public float jumpProbability = 0.25f;
        public float antiAirRange = 1.6f;

        float attackCd;
        float stateTimer;
        float blockHold;
        float specialCd;
        float retreatTimer;
        float smoothedMoveX;
        float jumpCd;

        enum AIState { Approach, Attack, Retreat, EvadeBlock }
        AIState state = AIState.Approach;

        CommandQueue commandQueue;
        CommandQueueFeeder feeder;

        void Awake() {
            if (!fighter) fighter = GetComponent<FighterController>();
            commandQueue = GetComponent<CommandQueue>();
            if (!commandQueue) commandQueue = gameObject.AddComponent<CommandQueue>();
            feeder = GetComponent<CommandQueueFeeder>();
            if (!feeder) feeder = gameObject.AddComponent<CommandQueueFeeder>();
            feeder.fighter = fighter;
            feeder.commandQueue = commandQueue;
        }

        AI.AIConfig CurrentConfig() {
            switch (GameManager.Instance ? GameManager.Instance.difficulty : Difficulty.Normal) {
                case Difficulty.Easy: return easy ? easy : normal;
                case Difficulty.Hard: return hard ? hard : normal;
                default: return normal;
            }
        }

        public bool TryGetCommands(FighterController fighter, out FighterCommands commands) {
            commands = default;
            if (fighter == null || fighter.opponent == null) return false;
            var cfg = CurrentConfig();
            var opp = fighter.opponent.GetComponent<FighterController>();
            float dx = fighter.opponent.position.x - fighter.transform.position.x;
            float dist = Mathf.Abs(dx);
            bool oppThreat = opp && opp.debugHitActive && Random.value < reactBlockProbability;
            bool oppAir = opp && !opp.IsGrounded();

            stateTimer += Time.deltaTime;
            blockHold = Mathf.Max(0f, blockHold - Time.deltaTime);
            specialCd = Mathf.Max(0f, specialCd - Time.deltaTime);
            retreatTimer = Mathf.Max(0f, retreatTimer - Time.deltaTime);
            jumpCd = Mathf.Max(0f, jumpCd - Time.deltaTime);

            if (stateTimer >= minStateTime) {
                var next = state;
                if (oppThreat && blockHold <= 0f) { next = AIState.EvadeBlock; }
                else if (dist <= attackRange) next = AIState.Attack;
                else if (dist >= disengageDistance) next = AIState.Approach;
                else if (dist > attackRange && dist < engageDistance) next = AIState.Approach;
                else next = AIState.Retreat;
                if (next != state) { state = next; stateTimer = 0f; }
            }

            // utility scores
            float sApproach = ScoreApproach(dist);
            float sRetreat = ScoreRetreat(dist);
            float sJump = ScoreJumpIn(oppAir, dist);
            float sAA = ScoreAntiAir(oppAir, dist);
            float sPress = ScorePressure(oppThreat, dist);
            bool canSuper = (specialCd <= 0f) && fighter.meter >= 500;
            float sSuper = ScoreSuper(canSuper, dist);

            // pick best
            float best = 0f; int choice = 0; // 1 approach,2 retreat,3 jump,4 AA,5 press,6 super
            void Pick(float s, int id) { if (s > best) { best = s; choice = id; } }
            Pick(sApproach,1); Pick(sRetreat,2); Pick(sJump,3); Pick(sAA,4); Pick(sPress,5); Pick(sSuper,6);

            var c = new FighterCommands();
            float targetMoveX = 0f;
            switch (choice) {
                case 1: targetMoveX = Mathf.Sign(dx); break;
                case 2: targetMoveX = -Mathf.Sign(dx); if (retreatTimer <= 0f) retreatTimer = retreatHold; break;
                case 3: if (jumpCd <= 0f && fighter.CanJump()) { wantJump = true; jumpCd = jumpCooldown; } break;
                case 4: // simple anti-air: stand Heavy
                    commandQueue.Enqueue(CommandToken.Heavy); attackCd = 0.4f; break;
                case 5: // pressure string
                    if (attackCd <= 0) { commandQueue.Enqueue(Random.value < 0.6f ? CommandToken.Light : CommandToken.Heavy); attackCd = Random.Range(0.4f,0.7f); }
                    targetMoveX = (dist > attackRange*0.8f) ? Mathf.Sign(dx) * microStepSpeed : 0f;
                    break;
                case 6: // super
                    commandQueue.Enqueue(CommandToken.Down);
                    commandQueue.Enqueue(Mathf.Sign(dx) > 0 ? CommandToken.Forward : CommandToken.Back);
                    commandQueue.Enqueue(CommandToken.Heavy);
                    specialCd = specialCooldown;
                    break;
                default:
                    targetMoveX = Mathf.Sign(dx);
                    break;
            }

            // if airborne and cooldown allows, sometimes go for an air double-jump to adjust arc
            if (!fighter.IsGrounded() && jumpCd <= 0f && fighter.CanJump() && Random.value < 0.25f) { wantJump = true; jumpCd = jumpCooldown; }

            // smooth movement to avoid jitter
            smoothedMoveX = Mathf.MoveTowards(smoothedMoveX, targetMoveX, moveSmooth * Time.deltaTime);
            // hold retreat a bit to avoid spam switching
            if (state == AIState.Retreat && retreatTimer > 0f) smoothedMoveX = -Mathf.Sign(dx);
            c.moveX = smoothedMoveX;
            if (wantJump) c.jump = true;

            if (attackCd > 0) attackCd -= Time.deltaTime;
            commands = c;
            return true;
        }

        bool wantJump;

        float ScoreApproach(float dist) => Mathf.Clamp01((dist - attackRange) / (disengageDistance - attackRange));
        float ScoreRetreat(float dist) => Mathf.Clamp01((attackRange - dist) / attackRange) * (blockHold > 0 ? 0.5f : 0.2f);
        float ScoreJumpIn(bool oppAir, float dist) => (jumpCd <= 0 && !oppAir && dist > attackRange * 0.9f) ? 0.4f : 0f;
        float ScoreAntiAir(bool oppAir, float dist) => (oppAir && dist < antiAirRange) ? 0.8f : 0f;
        float ScorePressure(bool oppThreat, float dist) => (!oppThreat && dist <= attackRange) ? 0.7f : 0.2f;
        float ScoreDefense(bool oppThreat) => oppThreat ? 0.6f : 0.1f;
        float ScoreSuper(bool can, float dist) => can && dist <= attackRange ? 0.9f : 0f;
    }
}