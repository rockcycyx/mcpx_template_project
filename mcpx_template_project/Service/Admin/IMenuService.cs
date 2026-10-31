using System.Collections.Generic;
using System.Threading.Tasks;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Service.Admin
{
    public interface IMenuService
    {
        /// <summary>
        /// 查询菜单列表
        /// </summary>
        Task<List<SysMenuVO>> QueryMenuListAsync(SysMenuQuery query);

        /// <summary>
        /// 查询菜单列表并构建树形结构
        /// </summary>
        Task<List<SysMenuVO>> QueryMenuTreeAsync(SysMenuQuery query);

        /// <summary>
        /// 查询菜单列表
        /// </summary>
        Task<List<SysMenuVO>> QueryAllMenusAsync();

        /// <summary>
        /// 新增菜单
        /// </summary>
        Task<ApiResult<SysMenu>> InsertMenuAsync(SysMenu menu);

        /// <summary>
        /// 修改菜单
        /// </summary>
        Task<ApiResult<SysMenu>> UpdateMenuAsync(SysMenu menu);

        /// <summary>
        /// 删除菜单
        /// </summary>
        Task<ApiResult<int>> DeleteMenuAsync(long menuId);

        /// <summary>
        /// 根据ID查询菜单
        /// </summary>
        Task<SysMenu?> GetMenuByIdAsync(long menuId);

        /// <summary>
        /// 切换菜单状态
        /// </summary>
        Task SwitchStatusAsync(long menuId, string status);
    }
}
