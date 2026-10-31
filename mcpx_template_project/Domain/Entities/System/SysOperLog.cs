using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mcpx_template_project.Domain.Entities.System
{
    /// <summary>
    /// 操作日志记录
    /// </summary>
    [Table("sys_oper_log")]
    [Comment("操作日志记录")]
    public class SysOperLog
    {
        /// <summary>
        /// 日志主键
        /// </summary>
        [Key]
        [Column("oper_id")]
        [Comment("日志主键")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OperId { get; set; }

        /// <summary>
        /// 模块标题
        /// </summary>
        [Column("title")]
        [Comment("模块标题")]
        [MaxLength(50)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 业务类型（0其它 1新增 2修改 3删除）
        /// </summary>
        [Column("business_type")]
        [Comment("业务类型（0其它 1新增 2修改 3删除）")]
        public int BusinessType { get; set; } = 0;

        /// <summary>
        /// 方法名称
        /// </summary>
        [Column("method")]
        [Comment("方法名称")]
        [MaxLength(200)]
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// 操作类别（0其它 1后台用户 2手机端用户）
        /// </summary>
        [Column("operator_type")]
        [Comment("操作类别（0其它 1后台用户 2手机端用户）")]
        public int OperatorType { get; set; } = 0;

        /// <summary>
        /// 操作人员
        /// </summary>
        [Column("oper_name")]
        [Comment("操作人员")]
        [MaxLength(50)]
        public string OperName { get; set; } = string.Empty;

        /// <summary>
        /// 部门名称
        /// </summary>
        [Column("dept_name")]
        [Comment("部门名称")]
        [MaxLength(50)]
        public string DeptName { get; set; } = string.Empty;

        /// <summary>
        /// 主机地址
        /// </summary>
        [Column("oper_ip")]
        [Comment("主机地址")]
        [MaxLength(128)]
        public string OperIp { get; set; } = string.Empty;

        /// <summary>
        /// 操作地点
        /// </summary>
        [Column("oper_location")]
        [Comment("操作地点")]
        [MaxLength(255)]
        public string OperLocation { get; set; } = string.Empty;

        /// <summary>
        /// 请求参数
        /// </summary>
        [Column("oper_param")]
        [Comment("请求参数")]
        public string OperParam { get; set; } = string.Empty;

        /// <summary>
        /// 返回参数
        /// </summary>
        [Column("json_result")]
        [Comment("返回参数")]
        public string JsonResult { get; set; } = string.Empty;

        /// <summary>
        /// 操作状态（0正常 1异常）
        /// </summary>
        [Column("status")]
        [Comment("操作状态（0正常 1异常）")]
        public int Status { get; set; } = 0;

        /// <summary>
        /// 错误消息
        /// </summary>
        [Column("error_msg")]
        [Comment("错误消息")]
        public string ErrorMsg { get; set; } = string.Empty;

        /// <summary>
        /// 操作时间
        /// </summary>
        [Column("oper_time")]
        [Comment("操作时间")]
        public DateTime? OperTime { get; set; }

        /// <summary>
        /// 消耗时间（毫秒）
        /// </summary>
        [Column("cost_time")]
        [Comment("消耗时间")]
        public long CostTime { get; set; } = 0;
    }
}
