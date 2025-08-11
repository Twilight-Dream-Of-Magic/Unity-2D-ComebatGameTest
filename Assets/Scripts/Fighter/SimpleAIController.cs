using UnityEngine;
using Systems;
using Fighter.AI;

namespace Fighter {
    public class SimpleAIController : MonoBehaviour {
        public FighterController fighter;
        public AIConfig easy;
        public AIConfig normal;
        public AIConfig hard;
        float attackCd;

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
            float dx = fighter.opponent.position.x - fighter.transform.position.x;
            float dist = Mathf.Abs(dx);

            FighterCommands c = new FighterCommands();
            c.moveX = dist > cfg.approachDistance ? Mathf.Sign(dx) : (dist < cfg.retreatDistance ? -Mathf.Sign(dx) : 0f);

            bool shouldBlock = Random.value < cfg.blockProbability && dist < 2.5f;
            c.block = shouldBlock;

            if (attackCd > 0) attackCd -= Time.deltaTime;
            if (attackCd <= 0 && dist < 2.0f && !shouldBlock) {
                c.light = Random.value < 0.7f;
                c.heavy = !c.light;
                attackCd = Random.Range(cfg.attackCooldownRange.x, cfg.attackCooldownRange.y);
            }
            fighter.SetCommands(c);
        }
    }
}