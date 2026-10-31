using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Service.Admin
{
    public interface IUserService
    {
        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="userName">账号</param>
        /// <param name="password">密码</param>
        /// <returns></returns>
        Task<ApiResult<SysUser>> LoginAsync(string userName, string password);

        /// <summary>
        /// 分页查询用户
        /// </summary>
        /// <param name="query">User查询参数</param>
        /// <param name="paging">分页查询参数</param>
        /// <returns></returns>
        Task<PagedResult<SysUserVO>> QueryUsersPagedAsync(SysUserQuery query, PagingParameters paging);

        /// <summary>
        /// 新增用户
        /// </summary>
        /// <param name="user">用户实体</param>
        /// <param name="roleIds">角色ID列表</param>
        /// <param name="postIds">岗位ID列表</param>
        /// <returns></returns>
        Task<ApiResult<SysUser>> InsertUserAsync(SysUser user, List<long> roleIds, List<long> postIds);

        /// <summary>
        /// 修改用户
        /// </summary>
        /// <param name="user">用户实体</param>
        /// <param name="roleIds">角色ID列表</param>
        /// <param name="postIds">岗位ID列表</param>
        /// <returns></returns>
        Task<ApiResult<SysUser>> UpdateUserAsync(SysUser user, List<long> roleIds, List<long> postIds);

        /// <summary>
        /// 批量删除用户（逻辑删除）
        /// </summary>
        /// <param name="userIds">用户ID列表</param>
        /// <returns>删除的用户数量</returns>
        Task<ApiResult<int>> DeleteUsersAsync(List<long> userIds);

        Task<ApiResult<int>> InsertUsersAsync(List<SysUser> users);

        /// <summary>
        /// 更新用户基本资料（昵称、邮箱、手机号、性别）
        /// </summary>
        Task<ApiResult<SysUser>> UpdateUserProfileAsync(long userId, string nickName, string email, string phoneNumber, string sex);

        /// <summary>
        /// 修改密码
        /// </summary>
        Task<ApiResult<int>> ChangePasswordAsync(long userId, string oldPassword, string newPassword);

        /// <summary>
        /// 切换用户状态
        /// </summary>
        Task SwitchStatusAsync(long userId, string status);
    }
}
