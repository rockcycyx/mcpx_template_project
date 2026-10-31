namespace mcpx_template_project.Domain.Common
{
    /// <summary>
    /// 统一返回包装模型
    /// </summary>
    public class ApiResult<T>
    {
        /// <summary>
        /// 状态码 200成功 其他失败
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 提示信息
        /// </summary>
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 业务主体数据
        /// </summary>
        public T? Data { get; set; }

        public bool IsSuccess => Code == 200;
    }
}
