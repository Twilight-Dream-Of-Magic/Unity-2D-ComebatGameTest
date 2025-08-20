using UnityEngine;

namespace Fighter.AI
{
	/// <summary>
	/// ScriptableObject configuration for fighter AI decision making.
	/// 包含格鬥 AI 的決策參數設定。
	/// </summary>
	[CreateAssetMenu(menuName = "Fighter/AI Config")]
	public class AIConfig : ScriptableObject
	{
		/// <summary>
		/// Probability that the AI will attempt to block an incoming attack.
		/// AI 嘗試防禦來襲攻擊的機率。
		/// </summary>
		[Range(0f, 1f)]
		public float blockProbability = 0.2f;

		/// <summary>
		/// Range of cooldown times between consecutive attacks.
		/// 連續攻擊之間的冷卻時間範圍。
		/// </summary>
		public Vector2 attackCooldownRange = new Vector2(0.6f, 1.2f);

		/// <summary>
		/// Distance threshold for AI to begin approaching the opponent.
		/// 開始接近對手的距離閾值。
		/// </summary>
		public float approachDistance = 2.2f;

		/// <summary>
		/// Distance threshold for AI to begin retreating from the opponent.
		/// 開始遠離對手的距離閾值。
		/// </summary>
		public float retreatDistance = 1.0f;
	}
}