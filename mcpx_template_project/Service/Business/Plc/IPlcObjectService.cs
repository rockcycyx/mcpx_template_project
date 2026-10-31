using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Service.Business.Plc
{
    /// <summary>
    /// 通用 PLC 对象读写门面：面向对象地读写 PLC 点位，屏蔽底层地址细节与握手协议。
    /// 业务代码只需「给对象属性赋值 → 调 WriteAsync；调 Read 拿回当前值」，
    /// 注解扫描、点位分类、触发/成功握手、地址解析等样板逻辑全部沉到本门面。
    /// </summary>
    /// <author>Rudeus</author>
    public interface IPlcObjectService
    {
        /// <summary>
        /// 把业务对象写入指定通讯设定对应的 PLC。
        /// 流程（按注解自动适配）：逐字段写值 → 触发位置 1 → 轮询回复位（0=处理中/1=ACK/2=NAK）→ 触发位复归 0 → 回复位复归 0。
        /// </summary>
        /// <param name="settingId">通讯设定ID（目标 PLC）</param>
        /// <param name="obj">已赋值的业务对象，取各 [PlcWrite] 属性当前值下发</param>
        Task<PlcWriteResult> WriteAsync<T>(long settingId, T obj) where T : class;

        /// <summary>
        /// 从通讯缓存读取业务对象所有 [PlcRead] 点位的当前值。
        /// </summary>
        /// <returns>属性名 → 当前值（文本）的原始字典</returns>
        IReadOnlyDictionary<string, string> Read<T>(long settingId) where T : class;

        /// <summary>指定通讯设定是否已连接</summary>
        bool IsConnected(long settingId);

        /// <summary>读取回复位（[PlcWrite(Role=Reply)]）当前是否为 ACK；无回复位时恒返回 false。</summary>
        bool IsWriteSuccess<T>(long settingId) where T : class;
    }
}
