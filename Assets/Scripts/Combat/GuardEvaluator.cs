namespace FightingGame.Combat
{
	/// <summary>
	/// GuardEvaluator — determines whether blocking is possible given fighter state and attack level.
	/// EN: Provides KOF-like guard rules and optional timing checks.
	/// ZH: 判定格擋是否成立的靜態工具，包含 KOF 風格的防禦規則與時間限制檢查。
	/// </summary>
	public static class GuardEvaluator
	{
		/// <summary>
		/// KOF-like guard rules.
		/// EN:
		/// - Must be on ground and holding block
		/// - High / Overhead: must be standing (not crouching)
		/// - Low: must be crouching
		/// - Mid: both postures allowed
		/// ZH:
		/// - 必須在地面並按住防禦
		/// - 上段 / 上挑：必須站立（不可蹲下）
		/// - 下段：必須蹲下
		/// - 中段：站立 / 蹲下皆可
		/// </summary>
		public static bool CanBlock(bool isHoldingBlock, bool isGrounded, bool isCrouching, HitLevel level)
		{
			if (!isHoldingBlock || !isGrounded)
			{
				return false;
			}

			switch (level)
			{
				case HitLevel.High:
				{
					return !isCrouching;
				}

				case HitLevel.Mid:
				{
					return true;
				}

				case HitLevel.Low:
				{
					return isCrouching;
				}

				case HitLevel.Overhead:
				{
					return !isCrouching;
				}

				default:
				{
					return true;
				}
			}
		}

		/// <summary>
		/// Extended with timing window.
		/// EN: Requires not locked and within max hold duration.
		/// ZH: 擴充檢查時間窗口，需未被防禦鎖定且在最大防禦持續內。
		/// </summary>
		public static bool CanBlockTimed(FightingGame.Combat.Actors.FighterActor fighter, bool isHoldingBlock, bool isGrounded, bool isCrouching, HitLevel level, float maxHoldSeconds)
		{
			if (fighter != null && fighter.IsBlockLocked())
			{
				return false;
			}

			if (maxHoldSeconds > 0f)
			{
				if (fighter == null)
				{
					return false;
				}

				if (fighter.GetBlockHeldSeconds() > maxHoldSeconds)
				{
					return false;
				}
			}

			return CanBlock(isHoldingBlock, isGrounded, isCrouching, level);
		}
	}
}
