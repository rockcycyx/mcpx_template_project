using System.Threading;

namespace mcpx_template_project.Core.Context
{
    /// <summary>
    /// SECS 设备上下文：在多实例场景下，让 Controller（消息处理器）能拿到「当前消息来自哪个实例」。
    /// 原理：每个通讯引擎实例在分发消息前，把自身的 instanceId 写入 AsyncLocal，
    /// 随 async 调用链自动流转，Controller 通过 <see cref="CurrentInstanceId"/> 读取，无需改方法签名。
    /// </summary>
    public interface ISecsContext
    {
        /// <summary>当前消息所属的 SECS 实例 ID；未在分发链中时为 null。</summary>
        long? CurrentInstanceId { get; }

        /// <summary>设置当前实例 ID（引擎分发消息前调用，返回可释放的还原作用域）。</summary>
        IDisposable SetInstance(long instanceId);
    }

    /// <summary>
    /// 基于 <see cref="AsyncLocal{T}"/> 的实现。AsyncLocal 值随 ExecutionContext 流转，
    /// 在 async/await 调用链中保持隔离且自动还原，天然适配多引擎并发分发。
    /// </summary>
    public sealed class SecsContext : ISecsContext
    {
        private readonly AsyncLocal<long?> _currentInstanceId = new();

        public long? CurrentInstanceId => _currentInstanceId.Value;

        public IDisposable SetInstance(long instanceId)
        {
            var previous = _currentInstanceId.Value;
            _currentInstanceId.Value = instanceId;
            return new RestoreScope(() => _currentInstanceId.Value = previous);
        }

        private sealed class RestoreScope : IDisposable
        {
            private readonly Action _restore;

            public RestoreScope(Action restore) => _restore = restore;

            public void Dispose() => _restore();
        }
    }
}
