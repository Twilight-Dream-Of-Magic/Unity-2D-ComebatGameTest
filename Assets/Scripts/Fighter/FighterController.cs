

namespace FightingGame.Combat.Actors
{
	/// <summary>
	/// Team tag.
	/// </summary>
	public enum FighterTeam { Player, AI }

	/// <summary>
	/// Normalized command snapshot provided each frame by an input source.
	/// </summary>
	public struct FighterCommands
	{
		public float moveX;
		public bool jump, crouch, light, heavy, block, dodge;
	}
}