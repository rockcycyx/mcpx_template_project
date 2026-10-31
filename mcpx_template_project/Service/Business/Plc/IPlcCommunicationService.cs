using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Service.Business.Plc
{
    /// <summary>
    /// PLC 通讯引擎服务：管理与三菱Q系列PLC的连接、批量读取点位、暂存数据。
    /// 目前仅支持三菱 MELSEC-Q 系列（MC协议 / 3E帧 / TCP / 二进制）。
    /// 区域配置(CommSettingArea)中的 Name=设备代号(D/W/M...)、Position=PLC起始地址、Quantity=读取点数。
    /// </summary>
    /// <author>Rudeus</author>
    public interface IPlcCommunicationService
    {
        /// <summary>
        /// 根据通讯设定建立与PLC的连接，并初始化暂存点位列表。
        /// 连接失败时不抛异常，返回 false 并设置 LastError。
        /// </summary>
        /// <param name="setting">通讯设定（含区域配置明细）</param>
        /// <returns>true=连接成功；false=失败（详见 LastError）</returns>
        Task<bool> ConnectAsync(CommSettingVO setting);

        /// <summary>
        /// 最近一次连接失败的错误描述（含PLC错误码翻译和排查建议）。
        /// 连接成功后为 null。
        /// </summary>
        string? LastError { get; }

        /// <summary>
        /// 断开指定通讯设定的PLC连接，保留暂存数据（标记为离线）。
        /// </summary>
        void Disconnect(long settingId);

        /// <summary>
        /// 检查指定通讯设定是否处于已连接状态。
        /// </summary>
        bool IsConnected(long settingId);

        /// <summary>
        /// 批量读取指定通讯设定所有区域配置对应的PLC点位，更新暂存缓存并返回最新值。
        /// </summary>
        Task<List<PointValueVO>> ReadAllPointsAsync(long settingId);

        /// <summary>
        /// 获取所有已连接通讯设定的暂存点位数据（快照）。
        /// </summary>
        List<PointValueVO> GetCachedPoints();

        /// <summary>
        /// 获取所有已连接的通讯设定ID列表。
        /// </summary>
        List<long> GetConnectedSettingIds();

        /// <summary>
        /// 写入单个点位值到指定通讯设定对应的 PLC。
        /// 根据设备代号自动分派位/字设备，按值类型调用 McpX 对应写方法。
        /// 写入失败时返回 false 并记录日志，不抛异常。
        /// </summary>
        /// <param name="settingId">通讯设定ID（目标 PLC）</param>
        /// <param name="deviceCode">设备代号（D/W/M/X/Y ...）</param>
        /// <param name="plcAddress">实际 PLC 地址（已含段内偏移）</param>
        /// <param name="value">要写入的值（bool 或数值类型）</param>
        Task<bool> WriteSinglePointAsync(long settingId, string deviceCode, string plcAddress, object value);

        /// <summary>
        /// 读取指定通讯设定下单个点位的当前值（位设备返回 "ON"/"OFF"，字设备返回数值文本）。
        /// 读取失败或未连接时返回 null。
        /// </summary>
        /// <param name="settingId">通讯设定ID</param>
        /// <param name="deviceCode">设备代号（D/W/M/X/Y ...）</param>
        /// <param name="plcAddress">PLC 地址</param>
        Task<string?> ReadSinglePointAsync(long settingId, string deviceCode, string plcAddress);

        /// <summary>
        /// 按指定类型读取单个字设备的当前值（如 float→Float32 占 2 字、double→占 4 字）。
        /// 用于读取浮点点位（有效功率/产量等），弥补通用字读取仅按 ushort 的不足。
        /// 读取失败或未连接时返回该类型默认值。
        /// </summary>
        /// <typeparam name="T">目标值类型（float/double/int 等 unmanaged 类型）</typeparam>
        /// <param name="settingId">通讯设定ID</param>
        /// <param name="deviceCode">设备代号（D/W 等字设备）</param>
        /// <param name="plcAddress">PLC 地址</param>
        Task<T> ReadTypedPointAsync<T>(long settingId, string deviceCode, string plcAddress) where T : unmanaged;

        /// <summary>
        /// 更新缓存中某点位的文本值（仅写内存缓存，不向 PLC 下发）。
        /// 供类型化读取（浮点等）把结果回填缓存，使「从缓存读」的界面能拿到正确类型值。
        /// </summary>
        /// <param name="settingId">通讯设定ID</param>
        /// <param name="deviceCode">设备代号</param>
        /// <param name="plcAddress">PLC 地址</param>
        /// <param name="textValue">要写入缓存的文本值</param>
        void SetCachedValue(long settingId, string deviceCode, string plcAddress, string textValue);
    }
}
