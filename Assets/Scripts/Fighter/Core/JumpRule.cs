using UnityEngine;

namespace Fighter.Core
{
	/// <summary>
	/// Centralized jump rules for a fighter.
	/// Encapsulates: maximum air jumps, coyote time, input buffer, minimal interval between jumps,
	/// and a token bucket limiter for repeated jumps.
	/// Both player and AI follow the same rule.
	/// 角色跳躍規則統一管理：最大空中跳躍數、土狼時間、輸入緩衝、最小間隔、以及令牌桶限制。
	/// 玩家與 AI 使用相同規則。
	/// </summary>
	public class JumpRule : MonoBehaviour
	{
		[Header("Limits")]
		public int maximumAirJumps = 1;

		[Header("Timing")]
		public float coyoteTime = 0.1f;
		public float bufferTime = 0.15f;
		public float minimumInterval = 0.05f;

		[Header("Token Bucket")]
		public int tokenCapacity = 4;
		public int tokensPerWindow = 2;
		public float tokenWindowSeconds = 3f;

		private int airJumpsUsed;
		private float timeSinceLeftGround;
		private float timeSinceLastJump;
		private float bufferTimer = 999f;
		private bool jumpHeld;

		private float tokens;
		private float tokenAccumulator;
		private bool wasGrounded;

		private void Start()
		{
			var stats = GetComponent<FightingGame.Combat.Actors.FighterActor>()?.stats;
			if (stats != null)
			{
				maximumAirJumps = Mathf.Max(0, stats.maxAirJumps);
				coyoteTime = Mathf.Max(0f, stats.jumpCoyoteTime);
				bufferTime = Mathf.Max(0f, stats.jumpBufferTime);
				minimumInterval = Mathf.Max(0f, stats.minJumpInterval);
				tokenCapacity = Mathf.Max(0, stats.jumpTokenCapacity);
				tokensPerWindow = Mathf.Max(0, stats.jumpTokensPerWindow);
				tokenWindowSeconds = Mathf.Max(0.0001f, stats.jumpTokenWindowSeconds);
			}
			tokens = tokenCapacity;
		}

		/// <summary>
		/// Call every frame to update grounded state and jump request input.
		/// 每幀調用，更新落地狀態與跳躍輸入。
		/// </summary>
		public void Tick(bool grounded, bool requested)
		{
			if (grounded)
			{
				if (!wasGrounded)
				{
					timeSinceLeftGround = 0f;
					airJumpsUsed = 0;
				}
				else
				{
					timeSinceLeftGround = 0f;
				}
			}
			else
			{
				timeSinceLeftGround += Time.deltaTime;
			}
			wasGrounded = grounded;

			timeSinceLastJump += Time.deltaTime;

			// detect input edge and held state
			if (requested)
			{
				jumpHeld = true;
				bufferTimer = 0f;
			}
			else
			{
				bufferTimer += Time.deltaTime;
				jumpHeld = false;
			}

			// token bucket refill: tokensPerWindow every tokenWindowSeconds
			if (tokensPerWindow > 0 && tokenWindowSeconds > 0.0001f)
			{
				tokenAccumulator += (tokensPerWindow / tokenWindowSeconds) * Time.deltaTime;
				if (tokenAccumulator >= 1f)
				{
					int add = Mathf.FloorToInt(tokenAccumulator);
					tokenAccumulator -= add;
					tokens = Mathf.Min(tokenCapacity, tokens + add);
				}
			}
		}

		/// <summary>
		/// Explicitly set whether the jump input is held.
		/// 顯式設定是否持續按住跳躍。
		/// </summary>
		public void SetJumpHeld(bool held)
		{
			jumpHeld = held;
		}

		/// <summary>
		/// Whether a jump can be performed now, considering limits and timing.
		/// 是否允許執行跳躍（檢查限制與時序）。
		/// </summary>
		public bool CanPerformJump(bool grounded)
		{
			if (timeSinceLastJump < minimumInterval)
			{
				return false;
			}

			if (tokens <= 0f)
			{
				return false;
			}

			if (grounded)
			{
				return true;
			}

			// allow coyote time
			if (!grounded && timeSinceLeftGround <= coyoteTime)
			{
				return true;
			}

			// allow air jumps within maximum limit
			return airJumpsUsed < maximumAirJumps;
		}

		/// <summary>
		/// Whether buffered or held input should auto-consume now to perform a jump.
		/// 是否應自動消耗緩衝或持續輸入來執行跳躍。
		/// </summary>
		public bool ShouldConsumeBufferedJump(bool grounded)
		{
			bool buffered = bufferTimer <= bufferTime;
			bool held = jumpHeld;
			return (buffered || held) && CanPerformJump(grounded);
		}

		/// <summary>
		/// Notify the rule that a jump has been executed.
		/// 通知規則已經執行跳躍。
		/// </summary>
		public void NotifyJumpExecuted(bool wasGrounded)
		{
			if (!wasGrounded && timeSinceLeftGround > 0f)
			{
				airJumpsUsed++;
			}

			timeSinceLastJump = 0f;
			bufferTimer = 999f; // clear buffer

			if (tokenCapacity > 0)
			{
				tokens = Mathf.Max(0, tokens - 1f);
			}
		}
	}
}
