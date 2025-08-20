using System;
using System.Collections.Generic;
using UnityEngine;

namespace FightingGame.Combat
{
	/// <summary>
	/// CommandQueue buffers input tokens with timing and dispatches them to registered handlers in priority order.
	/// EN: Buffered input queue with time windows per channel; dispatch is prioritized and consumable per-token.
	///     - Clear, expanded control flow (no compressed one-liners).
	///     - Tab indentation (TabSize = 4), no spaces; readable braces and explicit branches/loops.
	/// ZH: 具時間窗口的輸入命令佇列；按優先級分發並可被處理器「消費」。
	///     - 控制流程全部展開（不壓縮單行）。
	///     - 使用制表符縮排（TabSize = 4），大括號與分支/迴圈清晰分行。
	/// </summary>
	public class CommandQueue : MonoBehaviour
	{
		/// <summary>Normal-channel buffer window in seconds. 普通通道緩衝窗口（秒）。</summary>
		public float bufferWindowNormal = 0.25f;

		/// <summary>Combo-channel buffer window in seconds. 連段通道緩衝窗口（秒）。</summary>
		public float bufferWindowCombo = 0.25f;

		/// <summary>Use unscaled time when true (ignore timeScale). 若為 true 使用非縮放時間。</summary>
		public bool useUnscaledTime = false;

		/// <summary>Optional tuning ScriptableObject to override windows. 可選配置覆寫窗口參數。</summary>
		public Data.InputTuningConfig tuning;

		private readonly Queue<(CommandToken token, float time)> queueNormal = new Queue<(CommandToken token, float time)>();
		private readonly Queue<(CommandToken token, float time)> queueCombo  = new Queue<(CommandToken token, float time)>();

		// Prioritized, consumable handlers per token
		private struct Handler
		{
			public int priority;
			public Func<float, bool> fn;
		}

		private readonly Dictionary<CommandToken, List<Handler>> normalHandlers = new Dictionary<CommandToken, List<Handler>>();
		private readonly Dictionary<CommandToken, List<Handler>> comboHandlers  = new Dictionary<CommandToken, List<Handler>>();

		/// <summary>Optional tap for any Normal token (non-consumable). 普通通道可選監聽（不消費）。</summary>
		public Action<float> OnAnyNormal;

		/// <summary>Optional tap for any Combo token (non-consumable). 連段通道可選監聽（不消費）。</summary>
		public Action<float> OnAnyCombo;

		private void Awake()
		{
			if (this.tuning != null)
			{
				this.bufferWindowNormal = this.tuning.commandBufferWindow;
				this.bufferWindowCombo  = this.tuning.commandBufferWindow;
			}
		}

		private float Now
		{
			get
			{
				if (this.useUnscaledTime)
				{
					return Time.unscaledTime;
				}
				else
				{
					return Time.time;
				}
			}
		}

		/// <summary>
		/// Registers a prioritized, consumable handler for a token on a given channel.
		/// 為指定通道的 Token 註冊可消費的優先級處理器。
		/// </summary>
		public void RegisterHandler(CommandChannel channel, CommandToken token, Func<float, bool> handler, int priority = 0)
		{
			Dictionary<CommandToken, List<Handler>> map;

			if (channel == CommandChannel.Normal)
			{
				map = this.normalHandlers;
			}
			else
			{
				map = this.comboHandlers;
			}

			List<Handler> list;
			if (!map.TryGetValue(token, out list))
			{
				list = new List<Handler>();
				map[token] = list;
			}

			Handler entry = new Handler
			{
				priority = priority,
				fn = handler
			};

			list.Add(entry);

			// Sort by descending priority for deterministic dispatch
			list.Sort(CompareHandlerByPriority);
		}

		/// <summary>
		/// Unregisters a previously registered handler.
		/// 反註冊處理器。
		/// </summary>
		public void UnregisterHandler(CommandChannel channel, CommandToken token, Func<float, bool> handler)
		{
			Dictionary<CommandToken, List<Handler>> map;

			if (channel == CommandChannel.Normal)
			{
				map = this.normalHandlers;
			}
			else
			{
				map = this.comboHandlers;
			}

			List<Handler> list;
			if (!map.TryGetValue(token, out list))
			{
				return;
			}

			for (int i = list.Count - 1; i >= 0; i--)
			{
				if (list[i].fn == handler)
				{
					list.RemoveAt(i);
				}
			}
		}

		/// <summary>Clears both normal and combo queues. 清空普通與連段通道佇列。</summary>
		public void Clear()
		{
			this.queueNormal.Clear();
			this.queueCombo.Clear();
		}

		// Normal channel
		/// <summary>Enqueue a token into the Normal channel and dispatch to handlers. 普通通道入隊並分發。</summary>
		public void EnqueueNormal(CommandToken token)
		{
			if (token == CommandToken.None)
			{
				return;
			}

			float time = this.Now;

			this.queueNormal.Enqueue((token, time));

			this.Cleanup(this.queueNormal, this.bufferWindowNormal);

			if (this.OnAnyNormal != null)
			{
				this.OnAnyNormal.Invoke(time);
			}

			List<Handler> list;
			if (this.normalHandlers.TryGetValue(token, out list))
			{
				for (int i = 0; i < list.Count; i++)
				{
					bool hasHandler = (list[i].fn != null);

					if (hasHandler)
					{
						bool consumed = list[i].fn.Invoke(time);

						if (consumed)
						{
							break;
						}
					}
				}
			}
		}

		/// <summary>Non-destructively peek the next Normal token. 普通通道窺視（不出隊）。</summary>
		public bool TryPeekNormal(out CommandToken token)
		{
			this.Cleanup(this.queueNormal, this.bufferWindowNormal);

			if (this.queueNormal.Count > 0)
			{
				token = this.queueNormal.Peek().token;
				return true;
			}
			else
			{
				token = CommandToken.None;
				return false;
			}
		}

		/// <summary>Dequeue the next Normal token. 普通通道出隊。</summary>
		public bool TryDequeueNormal(out CommandToken token)
		{
			this.Cleanup(this.queueNormal, this.bufferWindowNormal);

			if (this.queueNormal.Count > 0)
			{
				token = this.queueNormal.Dequeue().token;
				return true;
			}
			else
			{
				token = CommandToken.None;
				return false;
			}
		}

		// Combo channel
		/// <summary>Enqueue a token into the Combo channel and dispatch to handlers. 連段通道入隊並分發。</summary>
		public void EnqueueCombo(CommandToken token)
		{
			if (token == CommandToken.None)
			{
				return;
			}

			float time = this.Now;

			this.queueCombo.Enqueue((token, time));

			this.Cleanup(this.queueCombo, this.bufferWindowCombo);

			if (this.OnAnyCombo != null)
			{
				this.OnAnyCombo.Invoke(time);
			}

			List<Handler> list;
			if (this.comboHandlers.TryGetValue(token, out list))
			{
				for (int i = 0; i < list.Count; i++)
				{
					bool hasHandler = (list[i].fn != null);

					if (hasHandler)
					{
						bool consumed = list[i].fn.Invoke(time);

						if (consumed)
						{
							break;
						}
					}
				}
			}
		}

		/// <summary>Non-destructively peek the next Combo token. 連段通道窺視（不出隊）。</summary>
		public bool TryPeekCombo(out CommandToken token)
		{
			this.Cleanup(this.queueCombo, this.bufferWindowCombo);

			if (this.queueCombo.Count > 0)
			{
				token = this.queueCombo.Peek().token;
				return true;
			}
			else
			{
				token = CommandToken.None;
				return false;
			}
		}

		/// <summary>Dequeue the next Combo token. 連段通道出隊。</summary>
		public bool TryDequeueCombo(out CommandToken token)
		{
			this.Cleanup(this.queueCombo, this.bufferWindowCombo);

			if (this.queueCombo.Count > 0)
			{
				token = this.queueCombo.Dequeue().token;
				return true;
			}
			else
			{
				token = CommandToken.None;
				return false;
			}
		}

		/// <summary>
		/// Removes expired tokens from the queue based on the window.
		/// 按時間窗口移除過期 Token。
		/// </summary>
		private void Cleanup(Queue<(CommandToken token, float time)> queue, float window)
		{
			float now = this.Now;

			while (queue.Count > 0 && (now - queue.Peek().time) > window)
			{
				queue.Dequeue();
			}
		}

		private static int CompareHandlerByPriority(Handler a, Handler b)
		{
			// Descending priority (higher priority first)
			return b.priority.CompareTo(a.priority);
		}
	}
}
