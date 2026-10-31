using System.Collections.Generic;
using System.Threading.Tasks;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Service.Admin
{
    public interface IPostService
    {
        Task<PagedResult<SysPostVO>> QueryPostsPagedAsync(SysPostQuery query, PagingParameters paging);

        Task<List<SysPostVO>> QueryAllPostsAsync();

        /// <summary>
        /// 根据用户ID查询其关联的岗位ID列表
        /// </summary>
        Task<List<long>> QueryPostIdsByUserIdAsync(long userId);

        /// <summary>
        /// 根据ID查询岗位
        /// </summary>
        Task<SysPostVO?> GetPostByIdAsync(long postId);

        /// <summary>
        /// 新增岗位
        /// </summary>
        Task<SysPostVO> InsertPostAsync(SysPost post);

        /// <summary>
        /// 修改岗位
        /// </summary>
        Task<SysPostVO> UpdatePostAsync(SysPost post);

        /// <summary>
        /// 删除岗位
        /// </summary>
        Task DeletePostAsync(long postId);

        /// <summary>
        /// 切换岗位状态
        /// </summary>
        Task SwitchStatusAsync(long postId, string status);
    }
}
