using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.System
{
    /// <summary>
    /// 字典数据表实体类
    /// 对应数据库表：sys_dict_data
    /// </summary>
    [Table("sys_dict_data")]
    public class SysDictData : BaseEntity
    {
        /// <summary>
        /// 字典编码（主键，自增）
        /// </summary>
        [Key]
        [Column("dict_code")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DictCode { get; set; }

        /// <summary>
        /// 字典排序
        /// </summary>
        [Column("dict_sort")]
        public int? DictSort { get; set; }

        /// <summary>
        /// 字典标签
        /// </summary>
        [MaxLength(100)]
        [Column("dict_label")]
        public string? DictLabel { get; set; }

        /// <summary>
        /// 字典键值
        /// </summary>
        [MaxLength(100)]
        [Column("dict_value")]
        public string? DictValue { get; set; }

        /// <summary>
        /// 字典类型
        /// </summary>
        [MaxLength(100)]
        [Column("dict_type")]
        public string? DictType { get; set; }

        /// <summary>
        /// 样式属性（其他样式扩展）
        /// </summary>
        [MaxLength(100)]
        [Column("css_class")]
        public string? CssClass { get; set; }

        /// <summary>
        /// 表格回显样式
        /// </summary>
        [MaxLength(100)]
        [Column("list_class")]
        public string? ListClass { get; set; }

        /// <summary>
        /// 是否默认（Y是 N否）
        /// </summary>
        [MaxLength(1)]
        [Column("is_default")]
        public string? IsDefault { get; set; }

        /// <summary>
        /// 状态（0正常 1停用）
        /// </summary>
        [MaxLength(1)]
        [Column("status")]
        public string? Status { get; set; }
    }
}