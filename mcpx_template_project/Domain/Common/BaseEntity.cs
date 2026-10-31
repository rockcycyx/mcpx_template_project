using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mcpx_template_project.Domain.Common
{
    /// <summary>
    /// 实体基类，包含所有实体共有的审计字段
    /// </summary>
    /// <author>Rudeus</author>
    public abstract class BaseEntity
    {
        /// <summary>
        /// 时间格式常量
        /// </summary>
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// 创建者
        /// </summary>
        [MaxLength(64)]
        [Column("create_by")]
        public string? CreateBy { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("create_time")]
        public DateTime? CreateTime { get; set; }

        /// <summary>
        /// 更新者
        /// </summary>
        [MaxLength(64)]
        [Column("update_by")]
        public string? UpdateBy { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Column("update_time")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [MaxLength(500)]
        [Column("remark")]
        public string? Remark { get; set; }
    }
}