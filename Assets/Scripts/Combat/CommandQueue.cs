using System.Collections.Generic;
using UnityEngine;
using System;

namespace FightingGame.Combat {
    /// <summary>
    /// CommandQueue buffers input tokens with timing and dispatches them to registered handlers
    /// in priority order. Handlers return true to consume the token.
    /// 命令队列带时间戳缓存输入 Token，并按优先级分发到已注册的处理器；处理器返回 true 表示消费该输入。
    /// </summary>
    public class CommandQueue : MonoBehaviour {
        /// <summary>Normal-channel buffer window in seconds. 普通通道缓冲窗口（秒）。</summary>
        public float bufferWindowNormal = 0.25f;
        /// <summary>Combo-channel buffer window in seconds. 连段通道缓冲窗口（秒）。</summary>
        public float bufferWindowCombo = 0.25f;
        /// <summary>Use unscaled time when true (ignore timeScale). 若为 true 使用非缩放时间。</summary>
        public bool useUnscaledTime = false;
        /// <summary>Optional tuning ScriptableObject to override windows. 可选配置覆盖窗口参数。</summary>
        public Data.InputTuningConfig tuning;

        readonly Queue<(CommandToken token, float time)> queueNormal = new();
        readonly Queue<(CommandToken token, float time)> queueCombo = new();

        // Prioritized, consumable handlers per token
        struct Handler { public int priority; public Func<float, bool> fn; }
        readonly Dictionary<CommandToken, List<Handler>> normalHandlers = new();
        readonly Dictionary<CommandToken, List<Handler>> comboHandlers = new();
        /// <summary>Optional tap for any Normal token (non-consumable). 普通通道可选监听（不消费）。</summary>
        public Action<float> OnAnyNormal;
        /// <summary>Optional tap for any Combo token (non-consumable). 连段通道可选监听（不消费）。</summary>
        public Action<float> OnAnyCombo;

        void Awake() {
            if (tuning) { bufferWindowNormal = tuning.commandBufferWindow; bufferWindowCombo = tuning.commandBufferWindow; }
        }

        float Now => useUnscaledTime ? Time.unscaledTime : Time.time;

        /// <summary>
        /// Registers a prioritized, consumable handler for a token on a given channel.
        /// 为指定通道的 Token 注册可消费的优先级处理器。
        /// </summary>
        public void RegisterHandler(CommandChannel channel, CommandToken token, Func<float, bool> handler, int priority = 0) {
            var map = channel == CommandChannel.Normal ? normalHandlers : comboHandlers;
            if (!map.TryGetValue(token, out var list)) { list = new List<Handler>(); map[token] = list; }
            list.Add(new Handler { priority = priority, fn = handler });
            list.Sort((a, b) => b.priority.CompareTo(a.priority));
        }
        /// <summary>
        /// Unregisters a previously registered handler.
        /// 反注册处理器。
        /// </summary>
        public void UnregisterHandler(CommandChannel channel, CommandToken token, Func<float, bool> handler) {
            var map = channel == CommandChannel.Normal ? normalHandlers : comboHandlers;
            if (!map.TryGetValue(token, out var list)) return;
            for (int i = list.Count - 1; i >= 0; i--) if (list[i].fn == handler) list.RemoveAt(i);
        }

        // Legacy API maps to Normal channel
        /// <summary>Legacy enqueue mapped to Normal channel. 旧版入队（映射到普通通道）。</summary>
        public void Enqueue(CommandToken token) { EnqueueNormal(token); }
        /// <summary>Legacy peek mapped to Normal channel. 旧版窥视（映射到普通通道）。</summary>
        public bool TryPeek(out CommandToken token) { return TryPeekNormal(out token); }
        /// <summary>Legacy dequeue mapped to Normal channel. 旧版出队（映射到普通通道）。</summary>
        public bool TryDequeue(out CommandToken token) { return TryDequeueNormal(out token); }
        /// <summary>Clears both normal and combo queues. 清空普通与连段通道队列。</summary>
        public void Clear() { queueNormal.Clear(); queueCombo.Clear(); }

        // Normal channel
        /// <summary>Enqueue a token into the Normal channel and dispatch to handlers. 普通通道入队并分发。</summary>
        public void EnqueueNormal(CommandToken token) {
            if (token == CommandToken.None) return;
            float time = Now;
            queueNormal.Enqueue((token, time));
            Cleanup(queueNormal, bufferWindowNormal);
            OnAnyNormal?.Invoke(time);
            if (normalHandlers.TryGetValue(token, out var list)) {
                for (int i = 0; i < list.Count; i++) { if (list[i].fn != null && list[i].fn.Invoke(time)) break; }
            }
        }
        /// <summary>Non-destructively peek the next Normal token. 普通通道窥视（不出队）。</summary>
        public bool TryPeekNormal(out CommandToken token) {
            Cleanup(queueNormal, bufferWindowNormal);
            if (queueNormal.Count > 0) { token = queueNormal.Peek().token; return true; }
            token = CommandToken.None; return false;
        }
        /// <summary>Dequeue the next Normal token. 普通通道出队。</summary>
        public bool TryDequeueNormal(out CommandToken token) {
            Cleanup(queueNormal, bufferWindowNormal);
            if (queueNormal.Count > 0) { token = queueNormal.Dequeue().token; return true; }
            token = CommandToken.None; return false;
        }

        // Combo channel
        /// <summary>Enqueue a token into the Combo channel and dispatch to handlers. 连段通道入队并分发。</summary>
        public void EnqueueCombo(CommandToken token) {
            if (token == CommandToken.None) return;
            float time = Now;
            queueCombo.Enqueue((token, time));
            Cleanup(queueCombo, bufferWindowCombo);
            OnAnyCombo?.Invoke(time);
            if (comboHandlers.TryGetValue(token, out var list)) {
                for (int i = 0; i < list.Count; i++) { if (list[i].fn != null && list[i].fn.Invoke(time)) break; }
            }
        }
        /// <summary>Non-destructively peek the next Combo token. 连段通道窥视（不出队）。</summary>
        public bool TryPeekCombo(out CommandToken token) {
            Cleanup(queueCombo, bufferWindowCombo);
            if (queueCombo.Count > 0) { token = queueCombo.Peek().token; return true; }
            token = CommandToken.None; return false;
        }
        /// <summary>Dequeue the next Combo token. 连段通道出队。</summary>
        public bool TryDequeueCombo(out CommandToken token) {
            Cleanup(queueCombo, bufferWindowCombo);
            if (queueCombo.Count > 0) { token = queueCombo.Dequeue().token; return true; }
            token = CommandToken.None; return false;
        }

        /// <summary>
        /// Removes expired tokens from the queue based on the window.
        /// 按时间窗口移除过期 Token。
        /// </summary>
        void Cleanup(Queue<(CommandToken token, float time)> queue, float window) {
            float now = Now;
            while (queue.Count > 0 && now - queue.Peek().time > window) queue.Dequeue();
        }
    }
}