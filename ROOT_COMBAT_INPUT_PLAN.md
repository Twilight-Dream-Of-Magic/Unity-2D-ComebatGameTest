# Combat Input Matching Plan (Temporary)

本計畫案暫時關閉並移除現有「搓招（Special）」系統，改以 KMP 演算法對 `List<string>` 序列做匹配。

- 關閉項目
  - 關閉 `SpecialInputResolver`/`SpecialMatcher`/`SpecialExecutor` 整套系統，不再在 `BattleAutoSetup` 中附加/組裝。
  - `RuntimeConfig.specialsEnabled` 保持可切換，但預設關閉。

- 新方案（KMP）
  - 玩家與 AI 產生的離散指令流以 `List<string>` 存儲（例如：`Down`, `Forward`, `Light`, ...）。
  - 針對每個欲偵測的序列（例如「下 前 重」或「下 下 輕」），以 KMP 預處理生成部分匹配表，線性時間掃描輸入流。
  - 命中後，透過路由器去調用核心「執行器/接收器」：
    - 傷害類：走 `FighterActor.EnterAttackHFSM(trigger)` 或 Offense.Flat 入口。
    - 治療類：走 `FighterActor.ExecuteHeal(trigger)`。
  - 滿足「若當前為攻擊中則請求取消；若為中立則直接執行」的規則。

- 資料結構草案
  - `InputSequence`：`name`, `pattern: List<string>`, `trigger`, `kind(Damage|Heal)`。
  - `KmpMatcher`：`BuildLps(List<string>)`, `Find(List<string> haystack, List<string> needle)`。
  - `SequenceRouter`：命中後根據 `kind` 路由到攻擊或治療。

- 後續
  - 先以最小可行版本替換，觀察輸入行為與誤判；必要時加入時間窗或清空策略。
  - 待你確認後，我再落實代碼與接線。