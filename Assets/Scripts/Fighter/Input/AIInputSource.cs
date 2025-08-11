using UnityEngine;
using Combat;
using Systems;

namespace Fighter.InputSystem {
    /// <summary>
    /// Smarter AI implementation of IInputSource. Produces approach/retreat/block/attack, plus dodge on threat,
    /// okizeme pressure, anti-air, and whiff punish behavior. Adapts to life/meter lead and corner state.
    /// </summary>
    public class AIInputSource : MonoBehaviour, IInputSource {
        public FighterController fighter;
        public AI.AIConfig easy;
        public AI.AIConfig normal;
        public AI.AIConfig hard;

        [Header("Behavior Tuning")]
        public float minStateTime = 0.25f;
        public float attackRange = 1.35f;
        public float engageDistance = 2.1f;
        public float disengageDistance = 2.5f;
        public float microStepSpeed = 0.35f;
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

        // situational weights
        [Header("Situational Weights")]
        public float aggressionBase = 0.5f;           // 0..1 base aggression
        public float lifeLeadAggression = 0.003f;      // per HP lead adds aggression
        public float meterLeadAggression = 0.001f;     // per meter lead adds aggression
        public float cornerRiskPenalty = 0.25f;        // when cornered, reduce aggression
        public float okizemeBias = 0.35f;              // extra aggression on opponent wakeup/knockdown

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

            // situational context
            float aggression = aggressionBase;
            if (opp) {
                int lifeLead = fighter.currentHealth - opp.currentHealth;
                int meterLead = fighter.meter - opp.meter;
                aggression = Mathf.Clamp01(aggressionBase + lifeLeadAggression * lifeLead + meterLeadAggression * meterLead);
                if (IsCornered(fighter)) aggression = Mathf.Max(0f, aggression - cornerRiskPenalty);
                if (IsOpponentVulnerable(opp)) aggression = Mathf.Clamp01(aggression + okizemeBias);
            }

            stateTimer += Time.deltaTime;
            blockHold = Mathf.Max(0f, blockHold - Time.deltaTime);
            specialCd = Mathf.Max(0f, specialCd - Time.deltaTime);
            retreatTimer = Mathf.Max(0f, retreatTimer - Time.deltaTime);
            jumpCd = Mathf.Max(0f, jumpCd - Time.deltaTime);
            dodgeCd = Mathf.Max(0f, dodgeCd - Time.deltaTime);

            // exclusive intent selection
            string intent = "";

            if (fighter.CanTechThrow()) { commands.light = true; intent = "Tech"; FinalizeMove(ref commands); return true; }

            if (stateTimer >= minStateTime) {
                var next = state;
                if (IsOpponentVulnerable(opp)) next = AIState.Okizeme;
                else if (oppThreat && blockHold <= 0f) {
                    next = (Random.value < dodgeProbability && dodgeCd <= 0f && fighter.IsGrounded()) ? AIState.EvadeDodge : AIState.EvadeBlock;
                }
                else if (oppWhiff && dist <= attackRange * 1.3f) next = AIState.WhiffPunish;
                else {
                    if (dist <= attackRange) next = aggression > 0.35f ? AIState.Attack : AIState.Retreat;
                    else if (dist >= disengageDistance) next = AIState.Approach;
                    else if (dist > attackRange && dist < engageDistance) next = AIState.Approach;
                    else next = AIState.Retreat;
                }
                if (next != state) { state = next; stateTimer = 0f; }
            }

            bool canSuper = (specialCd <= 0f) && fighter.meter >= 500;
            if (fighter.HasRecentHitConfirm() && canSuper) { QueueSuper(dx); specialCd = specialCooldown; intent = "Super"; }

            float targetMoveX = 0f;
            switch (state) {
                case AIState.Okizeme:
                    intent = string.IsNullOrEmpty(intent) ? "Okizeme" : intent;
                    if (dist > attackRange * 0.8f) targetMoveX = Mathf.Sign(dx) * microStepSpeed;
                    if (attackCd <= 0f) {
                        if (Random.value < 0.25f && opp && Vector2.Distance(opp.transform.position, fighter.transform.position) < 1.0f) {
                            commandQueue.EnqueueNormal(CommandToken.Throw); attackCd = 0.5f;
                        } else { commandQueue.EnqueueNormal(CommandToken.Light); attackCd = Random.Range(0.28f, 0.5f); }
                    }
                    break;
                case AIState.Approach:
                    intent = string.IsNullOrEmpty(intent) ? "Approach" : intent;
                    targetMoveX = Mathf.Sign(dx);
                    if (Random.value < jumpProbability && jumpCd <= 0f && fighter.CanJump()) { commands.jump = true; jumpCd = jumpCooldown; }
                    break;
                case AIState.Retreat:
                    intent = string.IsNullOrEmpty(intent) ? "Retreat" : intent;
                    targetMoveX = -Mathf.Sign(dx); if (retreatTimer <= 0f) retreatTimer = retreatHold; break;
                case AIState.EvadeBlock:
                    intent = "Block"; commands.block = true; blockHold = minBlockHold; break;
                case AIState.EvadeDodge:
                    intent = "Dodge"; commands.dodge = true; dodgeCd = 2.0f; break;
                case AIState.WhiffPunish:
                    intent = "WhiffPunish"; commandQueue.EnqueueNormal(CommandToken.Heavy); attackCd = 0.5f; break;
                case AIState.Attack:
                    intent = string.IsNullOrEmpty(intent) ? "Attack" : intent;
                    if (attackCd <= 0f) { commandQueue.EnqueueNormal(Random.value < 0.6f ? CommandToken.Light : CommandToken.Heavy); attackCd = Random.Range(0.35f,0.6f); }
                    targetMoveX = (dist > attackRange*0.85f) ? Mathf.Sign(dx) * microStepSpeed : 0f;
                    break;
            }

            // exclusive: if taking defensive intent, do not also enqueue attacks
            if (intent == "Block" || intent == "Dodge" || intent == "Tech") {
                // keep only defense flags in commands
            } else {
                // movement intents keep moveX; attacks are already enqueued via Normal channel
                smoothedMoveX = Mathf.MoveTowards(smoothedMoveX, targetMoveX, moveSmooth * Time.deltaTime);
                if (state == AIState.Retreat && retreatTimer > 0f) smoothedMoveX = -Mathf.Sign(dx);
                commands.moveX = smoothedMoveX;
            }

            if (oppAir && dist < antiAirRange && attackCd <= 0f && intent != "Block" && intent != "Dodge") { commandQueue.EnqueueNormal(CommandToken.Heavy); attackCd = 0.5f; }

            FinalizeMove(ref commands);
            return true;
        }

        void FinalizeMove(ref FighterCommands c) { /* reserved for future sanitation */ }

        void QueueSuper(float dx) {
            commandQueue.EnqueueNormal(CommandToken.Down);
            commandQueue.EnqueueNormal(Mathf.Sign(dx) > 0 ? CommandToken.Forward : CommandToken.Back);
            commandQueue.EnqueueNormal(CommandToken.Heavy);
        }

        bool IsOpponentVulnerable(FighterController opp) {
            if (!opp || opp.StateMachine == null || opp.StateMachine.Current == null) return false;
            string n = opp.StateMachine.Current.Name;
            return n.StartsWith("Downed") || n == "Wakeup";
        }

        bool IsCornered(FighterController who) {
            var framer = Systems.CameraFramer.Instance ? Systems.CameraFramer.Instance : UnityEngine.Object.FindObjectOfType<Systems.CameraFramer>();
            if (!framer) return false;
            float halfX = framer.arenaHalfExtents.x;
            float x = who.transform.position.x;
            return Mathf.Abs(halfX - Mathf.Abs(x)) < 1.5f; // near side wall
        }

        float ScoreApproach(float dist) => Mathf.Clamp01((dist - attackRange) / (disengageDistance - attackRange));
        float ScoreRetreat(float dist) => Mathf.Clamp01((attackRange - dist) / attackRange) * 0.2f;
        float ScoreJumpIn(bool oppAir, float dist) => (jumpCd <= 0 && !oppAir && dist > attackRange * 0.9f) ? 0.4f : 0f;
        float ScoreAntiAir(bool oppAir, float dist) => (oppAir && dist < antiAirRange) ? 0.8f : 0f;
        float ScorePressure(bool oppThreat, float dist) => (!oppThreat && dist <= attackRange) ? 0.7f : 0.2f;
        float ScoreSuper(bool can, float dist) => can && dist <= attackRange ? 0.9f : 0f;
    }
}