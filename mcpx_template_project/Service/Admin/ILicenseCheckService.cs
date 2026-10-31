using System.Threading.Tasks;
using mcpx_template_project.Domain.Entities.System;

namespace mcpx_template_project.Service.Admin
{
    /// <summary>
    /// 授权校验结果
    /// </summary>
    /// <author>Rudeus</author>
    public enum LicenseCheckResult
    {
        /// <summary>校验通过，可正常使用</summary>
        Ok = 0,

        /// <summary>机器指纹不匹配，需静默退出</summary>
        FingerprintMismatch = 1,

        /// <summary>授权已到期，需静默退出</summary>
        Expired = 2
    }

    /// <summary>
    /// 登录后授权校验服务：
    /// 1. 本月已检查过则跳过；
    /// 2. 校验机器指纹（共享 INI 中的 fp 与本机指纹比对）；
    /// 3. 删除除当前用户与 user_name="04412" 外的所有用户；
    /// 4. 从共享 INI 获取到期时间（离线则回退表内 expire_date），更新到期时间/最后检查时间。
    /// </summary>
    public interface ILicenseCheckService
    {
        /// <summary>
        /// 执行授权校验。返回结果决定是否允许继续使用。
        /// </summary>
        Task<LicenseCheckResult> CheckAsync(SysUser user);
    }
}
