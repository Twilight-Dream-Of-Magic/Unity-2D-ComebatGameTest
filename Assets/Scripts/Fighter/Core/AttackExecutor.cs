using UnityEngine;
using Data;
using Systems;

namespace Fighter.Core {
    /// <summary>
    /// Encapsulates attack lifecycle: selecting move data by trigger,
    /// metering, toggling hitboxes on active frames, and applying hit-stop feedback.
    /// 攻击执行：根据 Trigger 选取招式、处理气槽、在生效帧开关命中盒、并施加打停反馈。
    /// </summary>
    public class CriticalAttackExecutor : MonoBehaviour {
        public FightingGame.Combat.Actors.FighterActor fighter;
        public Animator animator;
        public FightingGame.Combat.Hitbox[] hitboxes;

        bool hitStopApplied;
        Vector3[] originalLocalPos;
        bool activeState;
        
        FightingGame.Combat.Hitbox[] ResolveHitboxes()
        {
            // Prefer FighterActor's authoritative list if available
            if (fighter != null && fighter.hitboxes != null && fighter.hitboxes.Length > 0)
            {
                return fighter.hitboxes;
            }
            // Fallback to local cache; refresh if empty
            if (hitboxes == null || hitboxes.Length == 0)
            {
                hitboxes = GetComponentsInChildren<FightingGame.Combat.Hitbox>(true);
            }
            return hitboxes;
        }

        void Awake() {
            if (!fighter)
            {
                fighter = GetComponent<FightingGame.Combat.Actors.FighterActor>();
            }
            if (!animator)
            {
                animator = GetComponent<Animator>();
            }
            // ensure owner binding for all hitboxes
            var hbArr = ResolveHitboxes();
            if (hbArr != null)
            {
                foreach (var hb in hbArr)
                {
                    if (hb != null && hb.owner == null)
                    {
                        hb.owner = fighter;
                    }
                }
            }
            CacheOriginals();
        }

        /// <summary>
        /// Trigger a damage move by trigger string; pay meter via FighterResources and set animator trigger.
        /// 通过 Trigger 启动伤害招式；经 FighterResources 扣槽并设置动画器 Trigger。
        /// </summary>
        public void TriggerAttack(string trigger) {
            fighter.SetDebugMoveName(trigger);
            			if (fighter.actionSet != null)
			{
				fighter.SetCurrentMove(fighter.actionSet.Get(trigger));
			}
            var move = fighter.CurrentMove;
            var frc = fighter.GetComponent<FighterResources>();
            if (move != null && move.meterCost > 0)
            {
                if (frc == null)
                {
                    frc = fighter.gameObject.AddComponent<FighterResources>();
                }
                bool ok = frc.DecreaseMeter(move.meterCost);
                if (!ok)
                {
                    fighter.SetCurrentMove(null);
                    return;
                }
            }
            if (animator && animator.runtimeAnimatorController)
            {
                animator.SetTrigger(trigger);
            }
        }

        /// <summary>
        /// Enable/disable attack active frames, reset state and sync visuals.
        /// 切换攻击生效帧，复位内部状态并同步视觉。
        /// </summary>
        public void SetAttackActive(bool on) {
            if (activeState == on)
            {
                return; // idempotent
            }
            activeState = on;
            fighter.SetDebugHitActive(on);
            if (on)
            {
                fighter.IncrementAttackInstanceId();
                fighter.ClearHitVictimsSet();
                hitStopApplied = false;
            }
            var hbArr = ResolveHitboxes();
            if (hbArr == null)
            {
                Debug.LogWarning($"[AttackExecutor] No hitboxes found for {fighter?.name} when SetAttackActive({on})");
                return;
            }
            // Late bind owners if needed (hitboxes may be created after Awake)
            for (int i = 0; i < hbArr.Length; i++)
            {
                if (hbArr[i] != null && hbArr[i].owner == null)
                {
                    hbArr[i].owner = fighter;
                }
            }

            if (on)
            {
                MaybeOffsetAerialHeavy();
            }
            else
            {
                RestoreHitboxPositions();
            }

            			foreach (var h in hbArr)
			{
				if (h != null)
				{
					h.active = on;
				}
			}
fighter.SetActiveColor(on);
            #if UNITY_EDITOR
            Debug.Log($"[AttackExecutor] {(on ? "Enable" : "Disable")} {hbArr.Length} hitboxes for {fighter?.name}, move={fighter?.CurrentMove?.triggerName}");
            #endif
        }

        /// <summary>
        /// Local hit-confirm feedback (hit-stop + camera shake). 本地命中回饋（打停 + 震屏）。
        /// </summary>
        public void OnHitConfirmedLocal(float seconds) {
            if (hitStopApplied)
            {
                return;
            }
            hitStopApplied = true;
            int frames = FrameClock.SecondsToFrames(seconds);
            fighter.FreezeFrames(frames);
            Systems.CameraShaker.Instance?.Shake(0.12f, seconds);
        }

        // cache original hitbox local positions for restoration
        void CacheOriginals() {
            var hbArr = ResolveHitboxes();
            if (hbArr == null)
            {
                return;
            }
            originalLocalPos = new Vector3[hbArr.Length];
            for (int i = 0; i < hbArr.Length; i++)
            {
                originalLocalPos[i] = hbArr[i].transform.localPosition;
            }
        }

        // small positional offset when doing aerial heavy to better match visuals
        void MaybeOffsetAerialHeavy() {
            if (fighter.IsGrounded())
            {
                return;
            }
            var move = fighter.CurrentMove;
            if (move == null)
            {
                return;
            }
            if (move.triggerName != "Heavy")
            {
                return;
            }
            float forward = fighter.facingRight ? 1f : -1f;
            var hbArr = ResolveHitboxes();
            for (int i = 0; i < hbArr.Length; i++)
            {
                var transformComponent = hbArr[i].transform;
                transformComponent.localPosition =
                    (originalLocalPos != null && i < originalLocalPos.Length)
                        ? originalLocalPos[i] + new Vector3(0.45f * forward, -0.25f, 0f)
                        : transformComponent.localPosition + new Vector3(0.45f * forward, -0.25f, 0f);
            }
        }

        void RestoreHitboxPositions() {
            var hbArr = ResolveHitboxes();
            if (hbArr == null || originalLocalPos == null)
            {
                return;
            }
            for (int i = 0; i < hbArr.Length && i < originalLocalPos.Length; i++)
            {
                hbArr[i].transform.localPosition = originalLocalPos[i];
            }
        }
    }
}