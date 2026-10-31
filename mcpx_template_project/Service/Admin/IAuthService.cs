using System.Collections.Generic;
using System.Threading.Tasks;
using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Service.Admin
{
    public interface IAuthService
    {
        Task<List<string>> GetPermsAsync(long userId);

        Task<List<SysMenuVO>> GetMenuTreeAsync(long userId);

        Task<bool> IsAdminAsync(long userId);
    }
}
