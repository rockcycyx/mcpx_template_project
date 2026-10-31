using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Model;
using SecsSxFyEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsSxFy;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// SECS/GEM SxFy 权限表业务服务：secs_sxfy 表的增删改查。
    /// 每个条目定义某 (Stream, Function) 在各控制状态下是否允许执行。
    /// </summary>
    public interface ISecsSxFyService
    {
        /// <summary>查询所有 SxFy 权限条目（按实例过滤）</summary>
        Task<List<SecsSxFyEntity>> QueryAllAsync(long instanceId);

        /// <summary>分页查询 SxFy 权限条目</summary>
        Task<PagedResult<SecsSxFyEntity>> QueryPagedAsync(long instanceId, PagingParameters paging, SecsSxFyQuery? query = null);

        /// <summary>按 ID 查询单个条目</summary>
        Task<SecsSxFyEntity?> GetByIdAsync(long id);

        /// <summary>按 (stream, function) 查询条目（用于权限校验）</summary>
        Task<SecsSxFyEntity?> GetByStreamFunctionAsync(long instanceId, byte stream, byte function);

        /// <summary>新增条目</summary>
        Task<SecsSxFyEntity> InsertAsync(SecsSxFyEntity entity);

        /// <summary>修改条目</summary>
        Task<SecsSxFyEntity> UpdateAsync(SecsSxFyEntity entity);

        /// <summary>删除条目</summary>
        Task DeleteAsync(long id);
    }
}
