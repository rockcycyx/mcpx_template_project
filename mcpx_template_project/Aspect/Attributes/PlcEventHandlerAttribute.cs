using mcpx_template_project.Core.Handler;

namespace mcpx_template_project.Service.Business.Trigger
{
    /// <summary>
    /// PLC 事件处理器特性：标注在 <see cref="IPlcEventHandler"/> 实现类上，
    /// 声明该处理器的唯一 Key 与展示名（供配点界面下拉框展示，避免手输错误）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PlcEventHandlerAttribute : Attribute
    {
        /// <summary>处理器唯一 Key（表中 action_key 存此值）</summary>
        public string Key { get; }

        /// <summary>处理器显示名（下拉框展示，如「写入日志」「触发报警灯」）</summary>
        public string DisplayName { get; }

        /// <summary>处理器描述（可选，辅助说明）</summary>
        public string? Description { get; set; }

        public PlcEventHandlerAttribute(string key, string displayName)
        {
            Key = key;
            DisplayName = displayName;
        }
    }
}
