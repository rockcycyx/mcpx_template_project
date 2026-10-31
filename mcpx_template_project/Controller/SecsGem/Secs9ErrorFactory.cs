using Secs4Net;

namespace mcpx_template_project.Controller.SecsGem
{
    /// <summary>
    /// S9 错误消息类型枚举（设备端 → Host 主动上报，SECS-I/II 标准）。
    /// </summary>
    public enum Secs9ErrorType : byte
    {
        /// <summary>S9F1：无法识别的设备 ID</summary>
        UnrecognizedDeviceId = 1,

        /// <summary>S9F3：无法识别的 Stream</summary>
        UnrecognizedStream = 3,

        /// <summary>S9F5：无法识别的 Function</summary>
        UnrecognizedFunction = 5,

        /// <summary>S9F7：非法数据</summary>
        IllegalData = 7,

        /// <summary>S9F9：事务计时器超时</summary>
        TransactionTimeout = 9,

        /// <summary>S9F11：数据过长</summary>
        DataTooLong = 11,

        /// <summary>S9F13：会话超时</summary>
        ConversationTimeout = 13,
    }

    /// <summary>
    /// S9 错误消息工厂：构造标准 S9Fx 消息（MHEAD + 附加数据）。
    /// S9Fx 报文结构：L,2 [ MHEAD, (可选附加项) ]，其中 MHEAD = L,3 [DeviceId B, Stream B, Function B]。
    /// </summary>
    public static class Secs9ErrorFactory
    {
        /// <summary>
        /// 构造 S9Fx 错误消息。
        /// </summary>
        /// <param name="type">错误类型（决定 F 值）</param>
        /// <param name="deviceId">本机设备 ID（0~32767，S9F1 时可为对端非法 ID）</param>
        /// <param name="relatedStream">关联的原始消息 Stream（S9F3 时为非法 Stream，其余为原消息 Stream）</param>
        /// <param name="relatedFunction">关联的原始消息 Function（S9F5 时为非法 Function，其余为原消息 Function）</param>
        public static SecsMessage Create(Secs9ErrorType type, ushort deviceId, byte relatedStream, byte relatedFunction)
        {
            byte f = (byte)type;

            // MHEAD = L,3 [DeviceId B, Stream B, Function B]
            var mhead = Item.L(
                Item.B(deviceId == 0 ? (byte)0 : (byte)deviceId), // DeviceId 低字节（标准为 B[1]）
                Item.B(relatedStream),
                Item.B(relatedFunction));

            // S9Fx 均为 L,2 [MHEAD, 空 List]（附加数据项按具体错误可选，此处统一为空 L）
            return new SecsMessage(9, f, replyExpected: false)
            {
                Name = $"ErrorReport {type}",
                SecsItem = Item.L(mhead, Item.L())
            };
        }

        /// <summary>
        /// 快捷构造：无法识别的 Stream（S9F3）。
        /// </summary>
        public static SecsMessage UnrecognizedStream(ushort deviceId, byte stream)
            => Create(Secs9ErrorType.UnrecognizedStream, deviceId, stream, 0);

        /// <summary>
        /// 快捷构造：无法识别的 Function（S9F5）。
        /// </summary>
        public static SecsMessage UnrecognizedFunction(ushort deviceId, byte stream, byte function)
            => Create(Secs9ErrorType.UnrecognizedFunction, deviceId, stream, function);

        /// <summary>
        /// 快捷构造：非法数据（S9F7）。
        /// </summary>
        public static SecsMessage IllegalData(ushort deviceId, byte stream, byte function)
            => Create(Secs9ErrorType.IllegalData, deviceId, stream, function);
    }
}
