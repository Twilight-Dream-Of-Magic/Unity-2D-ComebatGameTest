using UnityEngine;

namespace Fighter.AI
{
	/// <summary>
	/// ScriptableObject configuration for fighter AI decision making.
	/// �������Y AI �ěQ�߅����O����
	/// </summary>
	[CreateAssetMenu(menuName = "Fighter/AI Config")]
	public class AIConfig : ScriptableObject
	{
		/// <summary>
		/// Probability that the AI will attempt to block an incoming attack.
		/// AI �Lԇ���R���u�����ęC�ʡ�
		/// </summary>
		[Range(0f, 1f)]
		public float blockProbability = 0.2f;

		/// <summary>
		/// Range of cooldown times between consecutive attacks.
		/// �B�m����֮�g����s�r�g������
		/// </summary>
		public Vector2 attackCooldownRange = new Vector2(0.6f, 1.2f);

		/// <summary>
		/// Distance threshold for AI to begin approaching the opponent.
		/// �_ʼ�ӽ����ֵľ��x�ֵ��
		/// </summary>
		public float approachDistance = 2.2f;

		/// <summary>
		/// Distance threshold for AI to begin retreating from the opponent.
		/// �_ʼ�h�x���ֵľ��x�ֵ��
		/// </summary>
		public float retreatDistance = 1.0f;
	}
}