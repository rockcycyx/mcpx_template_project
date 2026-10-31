using System.Collections.Generic;
using System.Threading.Tasks;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Service.Admin
{
    public interface IRoleService
    {
        Task<PagedResult<SysRoleVO>> QueryRolesPagedAsync(SysRoleQuery query, PagingParameters paging);

        /// <summary>
        /// 根据用户ID查询其关联的角色ID列表
        /// </summary>
        Task<List<long>> QueryRoleIdsByUserIdAsync(long userId);

        /// <summary>
        /// 根据角色ID列表查询角色信息
        /// </summary>
        Task<List<SysRoleVO>> QueryRolesByIdsAsync(List<long> roleIds);

        /// <summary>
        /// 查询所有正常状态的角色
        /// </summary>
        Task<List<SysRoleVO>> QueryAllActiveRolesAsync();

        /// <summary>
        /// 新增角色
        /// </summary>
        Task<ApiResult<SysRole>> InsertRoleAsync(SysRole role);

        /// <summary>
        /// 修改角色
        /// </summary>
        Task<ApiResult<SysRole>> UpdateRoleAsync(SysRole role);

        /// <summary>
        /// 删除角色
        /// </summary>
        Task<ApiResult<int>> DeleteRoleAsync(long roleId);

        /// <summary>
        /// 根据ID查询角色
        /// </summary>
        Task<SysRole?> GetRoleByIdAsync(long roleId);

        /// <summary>
        /// 查询角色关联的菜单ID列表
        /// </summary>
        Task<List<long>> QueryMenuIdsByRoleIdAsync(long roleId);

        /// <summary>
        /// 更新角色菜单权限（先删除再插入）
        /// </summary>
        Task UpdateRoleMenusAsync(long roleId, List<long> menuIds);

        /// <summary>
        /// 切换角色状态
        /// </summary>
        Task SwitchStatusAsync(long roleId, string status);
    }
}
