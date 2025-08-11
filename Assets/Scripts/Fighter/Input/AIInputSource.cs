using UnityEngine;
using Combat;
using Systems;

namespace Fighter.InputSystem {
    /// <summary>
    /// Smarter AI implementation of IInputSource. Produces approach/retreat/block/attack, plus dodge on threat,
    /// okizeme pressure, anti-air, and whiff punish behavior.
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
        public float dodgeProbability = 0.4f;

        float attackCd;
        float stateTimer;
        float blockHold;
        float specialCd;
        float retreatTimer;
        float smoothedMoveX;
        float jumpCd;
        float dodgeCd;

        enum AIState { Approach, Attack, Retreat, EvadeBlock, EvadeDodge, WhiffPunish, Okizeme }
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
            bool oppWhiff = opp && !opp.debugHitActive && Random.value < 0.25f; // cheap whiff sense

            stateTimer += Time.deltaTime;
            blockHold = Mathf.Max(0f, blockHold - Time.deltaTime);
            specialCd = Mathf.Max(0f, specialCd - Time.deltaTime);
            retreatTimer = Mathf.Max(0f, retreatTimer - Time.deltaTime);
            jumpCd = Mathf.Max(0f, jumpCd - Time.deltaTime);
            dodgeCd = Mathf.Max(0f, dodgeCd - Time.deltaTime);

            if (stateTimer >= minStateTime) {
                var next = state;
                if (oppThreat && blockHold <= 0f) {
                    // prefer dodge sometimes
                    if (Random.value < dodgeProbability && dodgeCd <= 0f && fighter.IsGrounded()) next = AIState.EvadeDodge; else next = AIState.EvadeBlock;
                }
                else if (oppWhiff && dist <= attackRange * 1.2f) next = AIState.WhiffPunish;
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

            // hit-confirm super
            if (fighter.HasRecentHitConfirm() && canSuper) {
                QueueSuper(dx);
                specialCd = specialCooldown;
            }

            var c = new FighterCommands();
            float targetMoveX = 0f;

            switch (state) {
                case AIState.Approach:
                    targetMoveX = Mathf.Sign(dx);
                    if (Random.value < jumpProbability && jumpCd <= 0f && fighter.CanJump()) { c.jump = true; jumpCd = jumpCooldown; }
                    break;
                case AIState.Retreat:
                    targetMoveX = -Mathf.Sign(dx); if (retreatTimer <= 0f) retreatTimer = retreatHold; break;
                case AIState.EvadeBlock:
                    c.block = true; blockHold = minBlockHold; break;
                case AIState.EvadeDodge:
                    c.dodge = true; dodgeCd = 2.0f; break;
                case AIState.WhiffPunish:
                    commandQueue.Enqueue(CommandToken.Heavy); attackCd = 0.5f; break;
                case AIState.Okizeme:
                    // simple: micro step + Light
                    if (attackCd <= 0f) { commandQueue.Enqueue(CommandToken.Light); attackCd = Random.Range(0.3f,0.6f); }
                    targetMoveX = (dist > attackRange*0.7f) ? Mathf.Sign(dx) * microStepSpeed : 0f;
                    break;
                case AIState.Attack:
                    if (attackCd <= 0f) { commandQueue.Enqueue(Random.value < 0.65f ? CommandToken.Light : CommandToken.Heavy); attackCd = Random.Range(0.4f,0.7f); }
                    targetMoveX = (dist > attackRange*0.8f) ? Mathf.Sign(dx) * microStepSpeed : 0f;
                    break;
            }

            // anti-air opportunistically
            if (oppAir && dist < antiAirRange && attackCd <= 0f) { commandQueue.Enqueue(CommandToken.Heavy); attackCd = 0.5f; }

            // smooth movement to avoid jitter
            smoothedMoveX = Mathf.MoveTowards(smoothedMoveX, targetMoveX, moveSmooth * Time.deltaTime);
            if (state == AIState.Retreat && retreatTimer > 0f) smoothedMoveX = -Mathf.Sign(dx);
            c.moveX = smoothedMoveX;

            if (attackCd > 0) attackCd -= Time.deltaTime;
            commands = c;
            return true;
        }

        void QueueSuper(float dx) {
            commandQueue.Enqueue(CommandToken.Down);
            commandQueue.Enqueue(Mathf.Sign(dx) > 0 ? CommandToken.Forward : CommandToken.Back);
            commandQueue.Enqueue(CommandToken.Heavy);
        }

        float ScoreApproach(float dist) => Mathf.Clamp01((dist - attackRange) / (disengageDistance - attackRange));
        float ScoreRetreat(float dist) => Mathf.Clamp01((attackRange - dist) / attackRange) * 0.2f;
        float ScoreJumpIn(bool oppAir, float dist) => (jumpCd <= 0 && !oppAir && dist > attackRange * 0.9f) ? 0.4f : 0f;
        float ScoreAntiAir(bool oppAir, float dist) => (oppAir && dist < antiAirRange) ? 0.8f : 0f;
        float ScorePressure(bool oppThreat, float dist) => (!oppThreat && dist <= attackRange) ? 0.7f : 0.2f;
        float ScoreSuper(bool can, float dist) => can && dist <= attackRange ? 0.9f : 0f;
    }
}