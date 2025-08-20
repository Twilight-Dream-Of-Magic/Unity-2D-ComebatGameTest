using UnityEngine;

namespace Fighter {
	[CreateAssetMenu(menuName = "Fighter/Stats")]
	public class FighterStats : ScriptableObject
	{
		[Header("Health/Meter Ranges")]
		public int minHealth = 0;
		public int maxHealth = 5000;
		public int minMeter = 0;
		public int maxMeter = 1000;
		public float walkSpeed = 6f;
		public float jumpForce = 12f;
		public float gravityScale = 4f;
		public float blockDamageRatio = 0.2f;
		public float dodgeDuration = 0.25f;
		public float dodgeInvulnerable = 0.2f;
		[Header("Dodge Teleport")]
		[Tooltip("Dash/Dodge instantaneous teleport distance along pressed horizontal direction (world X).")]
		public float dodgeTeleportDistance = 5f;
		[Tooltip("Cooldown after a dodge teleport during which dodge cannot be re-triggered.")]
		public float dodgeTeleportCooldown = 0.35f;
		public float hitStop = 0.06f;

		[Header("Wakeup/Downed")]
		public float softKnockdownTime = 0.6f;
		public float hardKnockdownTime = 1.0f;
		public float wakeupInvuln = 0.25f;

		[Header("Jump Tokens")]
		public int maxAirJumps = 1;
		public float jumpCoyoteTime = 0.1f;
		public float jumpBufferTime = 0.15f;
		public float minJumpInterval = 0.05f;
		[Tooltip("令牌桶容量（可连续跳的最大爆发次数）")] public int jumpTokenCapacity = 4;
		[Tooltip("每个窗口补充的令牌数量（例如2表示每个窗口补2次）")] public int jumpTokensPerWindow = 2;
		[Tooltip("令牌补充窗口长度（秒），例如3秒补2个令牌 => 2/3 tokens/s")] public float jumpTokenWindowSeconds = 3f;

		[Header("Passive: Knockdown Shield")]
		[Tooltip("玩家被命中会消耗能量来避免倒地（若能量充足）")] public bool preventKnockdownIfMeter = true;
		public int preventKnockdownMeterCost = 100;
	}
}