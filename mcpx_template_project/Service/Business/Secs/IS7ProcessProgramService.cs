using Secs4Net;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// Stream 7（工艺程序管理）业务服务。
    /// PPID 对应业务侧「配方名」（recipe.recipe_name）。
    /// 工艺程序内容直接映射 recipe 的配方参数字段（不额外存储，recipe 的 16 个参数即为 S7 工艺程序参数）。
    /// </summary>
    public interface IS7ProcessProgramService
    {
        /// <summary>
        /// S7F19 当前 EPPD 请求：返回设备存储的所有 PPID 列表（S7F20）。
        /// </summary>
        Task<List<string>> GetPpidListAsync();

        /// <summary>
        /// S7F17 删除工艺程序：删除指定 PPID（空列表=删除全部）。
        /// 返回 ACKC7（0=接受，4=PPID 未找到）。
        /// </summary>
        Task<byte> DeletePpidAsync(IReadOnlyList<string> ppids);

        /// <summary>
        /// S7F1 工艺程序装载查询：校验 PPID 是否存在，返回 PPGNT。
        /// </summary>
        /// <param name="ppid">工艺程序 ID（配方名）</param>
        Task<byte> InquireLoadAsync(string ppid);

        /// <summary>
        /// S7F23 格式化工艺程序下发：把 PPID + CCODE + 参数列表反解为配方并新增（或更新）。
        /// 参数列表按固定顺序映射到 recipe 的 16 个配方参数字段。
        /// </summary>
        /// <param name="ppid">工艺程序 ID（配方名）</param>
        /// <param name="ccode">命令码 CCODE</param>
        /// <param name="pparms">参数列表（16 个，顺序见客户文档）</param>
        /// <returns>ACKC7（0=接受，其他=拒绝）。</returns>
        Task<byte> StoreProcessProgramAsync(string ppid, string ccode, IReadOnlyList<Item> pparms);

        /// <summary>
        /// S7F25 格式化工艺程序请求：按 PPID 取配方，返回命令码 + 已按类型转好的参数 Item 列表（S7F26 用）。
        /// </summary>
        /// <param name="ppid">工艺程序 ID（配方名）</param>
        /// <returns>PPID 存在返回 (ccode, 参数 Item 列表)；不存在返回 null。</returns>
        Task<ProcessProgramData?> GetProcessProgramAsync(string ppid);
    }

    /// <summary>
    /// 格式化工艺程序数据：命令码 CCODE + 参数列表（已按各字段类型转成 SECS Item）。
    /// S7F26 结构：L,4 [PPID A, MDLN A, SOFTREV A, L,1 [L,2 [CCODE A, L,16 [参数...]]]]。
    /// </summary>
    public sealed record ProcessProgramData(string Ccode, IReadOnlyList<Item> Params);
}
