namespace mcpx_template_project.Domain.Common
{
    /// <summary>
    /// 通用状态枚举
    /// 0 = 正常, 1 = 停用
    /// </summary>
    public enum CommonStatus
    {
        /// <summary>正常</summary>
        Normal = 0,

        /// <summary>停用</summary>
        Disabled = 1
    }

    /// <summary>
    /// CommonStatus 扩展方法
    /// </summary>
    public static class CommonStatusExtensions
    {
        public static string ToStatusString(this CommonStatus status) => ((int)status).ToString();

        public static CommonStatus ToCommonStatus(this string? status)
        {
            return status == "1" ? CommonStatus.Disabled : CommonStatus.Normal;
        }

        public static bool IsActive(this string? status) => status != "1";

        public static string Toggle(this string? status) => status == "1" ? "0" : "1";
    }
}