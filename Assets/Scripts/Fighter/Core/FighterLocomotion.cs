using UnityEngine;
using Systems;
using FightingGame.Combat;

namespace Fighter.Core
{
	/// <summary>
	/// Encapsulates locomotion: ground and air movement, jumping, auto-facing, grounded checks.
	/// 封裝角色位移：地面/空中移動、跳躍、自動朝向、落地檢測。
	/// 責任：提供簡單操作，將細節與物理處理從控制器中抽離。
	/// </summary>
	public class FighterLocomotion : MonoBehaviour
	{
		[Header("References")]
		public FightingGame.Combat.Actors.FighterActor fighter; // 角色實例
		public new Rigidbody2D rigidbody;                       // 物理剛體
		public CapsuleCollider2D bodyCollider;                  // 身體膠囊碰撞體
		public Animator animator;                               // 動畫器

		[Header("Config")]
		public bool enableBodyPushOut = false;                  // 是否啟用角色間推擠避免重疊

		private void Awake()
		{
			if (!fighter)
				fighter = GetComponent<FightingGame.Combat.Actors.FighterActor>();
			if (!rigidbody)
				rigidbody = GetComponent<Rigidbody2D>();
			if (!bodyCollider)
				bodyCollider = GetComponent<CapsuleCollider2D>();
			if (!animator)
				animator = GetComponent<Animator>();
		}

		/// <summary>
		/// Move fighter on the ground according to input scale.
		/// 依輸入比例執行地面移動。
		/// </summary>
		public void Move(float inputX)
		{
			float speed = fighter.stats != null ? fighter.stats.walkSpeed : 6f;
			rigidbody.velocity = new Vector2(inputX * speed, rigidbody.velocity.y);
			if (enableBodyPushOut)
				ResolveOverlapPushOut();
		}

		/// <summary>
		/// Stop horizontal movement completely.
		/// 停止水平速度。
		/// </summary>
		public void HaltHorizontal()
		{
			rigidbody.velocity = new Vector2(0, rigidbody.velocity.y);
		}

		/// <summary>
		/// Move fighter in the air according to input scale.
		/// 依輸入比例執行空中移動。
		/// </summary>
		public void AirMove(float inputX)
		{
			float speed = fighter.stats != null ? fighter.stats.walkSpeed : 6f;
			rigidbody.velocity = new Vector2(inputX * speed, rigidbody.velocity.y);
			if (enableBodyPushOut)
				ResolveOverlapPushOut();
		}

		/// <summary>
		/// Perform a jump with vertical velocity and optional animation trigger.
		/// 執行跳躍並觸發動畫。
		/// </summary>
		public void Jump()
		{
			float jumpForce = fighter.stats != null ? fighter.stats.jumpForce : 12f;
			rigidbody.velocity = new Vector2(rigidbody.velocity.x, jumpForce);
			if (animator && animator.runtimeAnimatorController)
				animator.SetTrigger("Jump");
		}

		/// <summary>
		/// Check if fighter is grounded using OverlapBox or Raycast fallback.
		/// 使用重疊盒檢測是否在地面上。
		/// </summary>
		public bool IsGrounded(LayerMask groundMask)
		{
			if (!bodyCollider)
				return Physics2D.Raycast(transform.position, Vector2.down, 0.2f, groundMask);
			Bounds bounds = bodyCollider.bounds;
			Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y - 0.05f);
			Vector2 boxSize = new Vector2(bounds.size.x * 0.9f, 0.1f);
			return Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundMask) != null;
		}

		/// <summary>
		/// Automatically face opponent horizontally.
		/// 自動朝向對手。
		/// </summary>
		public void AutoFaceOpponent()
		{
			if (!fighter || !fighter.opponent) return;
			bool shouldFaceRight = transform.position.x <= fighter.opponent.position.x;
			if (shouldFaceRight != fighter.facingRight)
			{
				fighter.facingRight = shouldFaceRight;
				Vector3 scale = transform.localScale;
				scale.x = Mathf.Abs(scale.x) * (fighter.facingRight ? 1 : -1);
				transform.localScale = scale;
			}
		}

		/// <summary>
		/// Freeze or unfreeze animator and physics simulation.
		/// 凍結或解凍動畫與物理。
		/// </summary>
		public void ApplyFreezeVisual(bool frozen)
		{
			if (animator)
				animator.speed = frozen ? 0f : 1f;
			if (rigidbody)
				rigidbody.simulated = !frozen;
		}

		/// <summary>
		/// Slightly nudge position horizontally (safe in FixedUpdate).
		/// 輕微水平推移（適用於 FixedUpdate）。
		/// </summary>
		public void NudgeHorizontal(float deltaX)
		{
			if (Mathf.Abs(deltaX) <= 0.0001f)
				return;
			Vector2 pos = rigidbody.position;
			float targetX = pos.x + deltaX;
			rigidbody.MovePosition(new Vector2(targetX, pos.y));
		}

		/// <summary>
		/// Resolve overlap by pushing fighter away from other body volumes.
		/// 避免角色之間重疊並推開，防止牆角困住。
		/// </summary>
		private void ResolveOverlapPushOut()
		{
			if (!bodyCollider)
				return;
			Bounds bounds = bodyCollider.bounds;
			Collider2D[] hits = Physics2D.OverlapBoxAll(bounds.center, bounds.size * 0.98f, 0f);
			foreach (var hit in hits)
			{
				if (hit == null || hit.attachedRigidbody == rigidbody)
					continue;
				if (hit.GetComponent<BodyVolume>() == null)
					continue;

				Bounds other = hit.bounds;
				if (!bounds.Intersects(other)) continue;

				float deltaLeft = other.max.x - bounds.min.x;
				float deltaRight = bounds.max.x - other.min.x;
				float push = Mathf.Abs(deltaLeft) < Mathf.Abs(deltaRight) ? -deltaLeft : deltaRight;
				rigidbody.position += new Vector2(push * 1.01f, 0f);
			}

			// Clamp to arena bounds (demo only, optional)
			float x = Mathf.Clamp(rigidbody.position.x, -10f, 10f);
			rigidbody.position = new Vector2(x, rigidbody.position.y);
		}
	}
}
