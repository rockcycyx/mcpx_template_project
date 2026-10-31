namespace mcpx_template_project.Helper;

/// <summary>
/// Excel 列配置（导出/导入通用）
/// </summary>
public class ExcelColumnConfig<T>
{
    /// <summary>列标题</summary>
    public string Header { get; set; } = string.Empty;

    // ---- 导出相关 ----

    /// <summary>从实体中取值（导出用）</summary>
    public Func<T, object?>? Selector { get; set; }

    /// <summary>导出时的显示值转换器</summary>
    public Func<object?, object?>? DisplayConverter { get; set; }

    // ---- 导入相关 ----

    /// <summary>将单元格值写入实体（导入用）</summary>
    public Action<T, object?>? Setter { get; set; }

    /// <summary>导入时的原始值转换器</summary>
    public Func<object?, object?>? ParseConverter { get; set; }
}
