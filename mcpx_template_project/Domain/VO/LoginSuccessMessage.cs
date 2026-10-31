using mcpx_template_project.Domain.Entities.System;

namespace mcpx_template_project.Domain.VO
{
    public class LoginSuccessMessage
    {
        public SysUser User { get; }

        public LoginSuccessMessage(SysUser user)
        {
            User = user;
        }
    }
}