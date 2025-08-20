using UnityEngine;

namespace Data
{
	/// <summary>
	/// A collection mapping input triggers to combat action definitions.
	/// 將輸入觸發詞映射到對應的戰鬥動作定義的集合。
	/// </summary>
	[CreateAssetMenu(menuName = "Fighter/Action Set")]
	public class CombatActionSet : ScriptableObject
	{
		[System.Serializable]
		public struct Entry
		{
			public string triggerName; // Input trigger keyword / 輸入觸發詞
			public CombatActionDefinition actionDefinition; // Corresponding action definition / 對應的戰鬥動作定義
		}

		[Header("Action Entries")]
		public Entry[] entries; // List of trigger-action mappings / 觸發詞與動作定義的映射列表

		/// <summary>
		/// Get the combat action definition for a given trigger.
		/// 根據輸入的觸發詞查找並返回戰鬥動作定義。
		/// </summary>
		/// <param name="trigger">Input trigger keyword / 輸入觸發詞</param>
		/// <returns>CombatActionDefinition if found, otherwise null / 如果找到返回戰鬥動作定義，否則返回 null</returns>
		public CombatActionDefinition Get(string trigger)
		{
			if (entries == null)
			{
				return null;
			}

			for (int i = 0; i < entries.Length; i++)
			{
				if (entries[i].triggerName == trigger)
				{
					return entries[i].actionDefinition;
				}
			}
			return null;
		}
	}
}
