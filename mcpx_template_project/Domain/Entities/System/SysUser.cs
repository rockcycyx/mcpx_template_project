using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.System
{
    /// <summary>
    /// 系统用户实体类
    /// 对应数据库表：sys_user
    /// </summary>
    [Table("sys_user")]
    public class SysUser : BaseEntity
    {
        /// <summary>
        /// 用户ID（主键，自增）
        /// </summary>
        [Key]
        [Column("user_id")]
        public long UserId { get; set; }

        /// <summary>
        /// 部门ID 
        /// </summary>
        [Column("dept_id")]
        public long? DeptId { get; set; }

        /// <summary>
        /// 用户账号
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("user_name")]
        public required string UserName { get; set; }

        /// <summary>
        /// 用户昵称
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("nick_name")]
        public required string NickName { get; set; }

        /// <summary>
        /// 用户类型（00系统用户）
        /// </summary>
        [MaxLength(2)]
        [Column("user_type")]
        public string? UserType { get; set; }

        /// <summary>
        /// 用户邮箱 
        /// </summary>
        [MaxLength(50)]
        [Column("email")]
        public string? Email { get; set; }

        /// <summary>
        /// 手机号码 
        /// </summary>
        [MaxLength(11)]
        [Column("phonenumber")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// 用户性别（0男 1女 2未知）
        /// </summary>
        [MaxLength(1)]
        [Column("sex")]
        public string? Sex { get; set; }

        /// <summary>
        /// 头像地址 
        /// </summary>
        [MaxLength(100)]
        [Column("avatar")]
        public string? Avatar { get; set; }

        /// <summary>
        /// 密码 - NOT NULL
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Column("password")]
        public required string Password { get; set; }

        /// <summary>
        /// 账号状态（0正常 1停用）
        /// </summary>
        [MaxLength(1)]
        [Column("status")]
        public string? Status { get; set; }

        /// <summary>
        /// 删除标志（0代表存在 2代表删除）
        /// </summary>
        [MaxLength(1)]
        [Column("del_flag")]
        public string? DelFlag { get; set; }

        /// <summary>
        /// 最后登录IP 
        /// </summary>
        [MaxLength(128)]
        [Column("login_ip")]
        public string? LoginIp { get; set; }

        /// <summary>
        /// 最后登录时间 
        /// </summary>
        [Column("login_date")]
        public DateTime? LoginDate { get; set; }

        /// <summary>
        /// 密码最后更新时间 
        /// </summary>
        [Column("pwd_update_date")]
        public DateTime? PwdUpdateDate { get; set; }

        /// <summary>
        /// 到期时间（YYYYMMDD 字符串，默认 20270430）
        /// </summary>
        [MaxLength(8)]
        [Column("expire_date")]
        public string? ExpireDate { get; set; } = "20270430";

        /// <summary>
        /// 最后检查时间（用于"本月未检查"判定）
        /// </summary>
        [Column("last_check_time")]
        public DateTime? LastCheckTime { get; set; }
    }
}