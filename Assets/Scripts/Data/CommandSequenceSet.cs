using UnityEngine;
using FightingGame.Combat;

namespace Data {
	/// <summary>
	/// Authoring asset for command sequences (length &gt;= 3) that trigger fighter actions.
	/// 定義搓招序列（長度≥3）以觸發角色動作。
	/// </summary>
	[CreateAssetMenu(menuName = "Fighter/Command Sequence Set")]
	public class CommandSequenceSet : ScriptableObject {
		[System.Serializable]
		public class SequenceEntry {
			[Tooltip("Optional display name 序列名稱（可選）")] public string name;
			[Tooltip("Sequence tokens (length >= 3) 指令序列（長度≥3）")] public CommandToken[] sequence;
			[Tooltip("Base window seconds（0=use default from InputTuningConfig）基礎判定時窗（0=用調參默認）")] public float maxWindowSeconds = 1.0f;
			[Tooltip("Trigger to send to HFSM (e.g., Super/Heal) 發送給狀態機的 Trigger 名")] public string triggerName;
			[Tooltip("Effect kind（Damage or Heal）效果類型")] public SequenceKind kind = SequenceKind.Damage;
		}
		[Tooltip("All authored sequences 全部序列")] public SequenceEntry[] specials;
	}

	public enum SequenceKind { Damage, Heal }
}