using System.Collections.Generic;
using System.Threading.Tasks;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Service.Admin
{
    public interface IDeptService
    {
        /// <summary>
        /// 查询部门列表
        /// </summary>
        Task<List<SysDeptVO>> QueryDeptListAsync(SysDeptQuery query);

        /// <summary>
        /// 查询部门列表并构建树形结构
        /// </summary>
        Task<List<SysDeptVO>> QueryDeptTreeAsync(SysDeptQuery query);

        /// <summary>
        /// 根据ID查询部门
        /// </summary>
        Task<SysDept?> GetDeptByIdAsync(long deptId);

        /// <summary>
        /// 新增部门
        /// </summary>
        Task<ApiResult<SysDept>> InsertDeptAsync(SysDept dept);

        /// <summary>
        /// 修改部门
        /// </summary>
        Task<ApiResult<SysDept>> UpdateDeptAsync(SysDept dept);

        /// <summary>
        /// 删除部门
        /// </summary>
        Task<ApiResult<int>> DeleteDeptAsync(long deptId);

        /// <summary>
        /// 切换部门状态
        /// </summary>
        Task SwitchStatusAsync(long deptId, string status);
    }
}
