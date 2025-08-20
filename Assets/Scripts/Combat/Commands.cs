namespace FightingGame.Combat
{
	/// <summary>
	/// CommandToken — represents input tokens for combat commands.
	/// EN: Enumeration of combat input tokens (attack strengths, directional inputs, neutral, etc.).
	/// ZH: 格鬥指令輸入的枚舉（攻擊強度、方向輸入、中立等）。
	/// </summary>
	public enum CommandToken
	{
		/// <summary>Represents no input. 無輸入。</summary>
		None,

		/// <summary>Light attack input. 輕攻擊輸入。</summary>
		Light,

		/// <summary>Heavy attack input. 重攻擊輸入。</summary>
		Heavy,

		/// <summary>Throw action input. 投技輸入。</summary>
		Throw,

		/// <summary>Up direction input. 向上輸入。</summary>
		Up,

		/// <summary>Down direction input. 向下輸入。</summary>
		Down,

		/// <summary>Forward direction input (relative to facing). 向前輸入（依角色面向）。</summary>
		Forward,

		/// <summary>Back direction input (relative to facing). 向後輸入（依角色面向）。</summary>
		Back,

		/// <summary>Neutral (no directional input). 中立輸入。</summary>
		Neutral
	}

	/// <summary>
	/// CommandChannel — categorizes the command queue channels.
	/// EN: Indicates which input queue the token is associated with (Normal vs Combo).
	/// ZH: 指示命令所屬的輸入通道（普通或連段）。
	/// </summary>
	public enum CommandChannel
	{
		/// <summary>Normal channel. 普通通道。</summary>
		Normal,

		/// <summary>Combo channel. 連段通道。</summary>
		Combo
	}
}
