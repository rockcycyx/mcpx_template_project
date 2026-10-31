using System.IO;
using OfficeOpenXml;

namespace mcpx_template_project.Helper;

/// <summary>
/// Excel 导入导出工具类（基于 EPPlus）
/// </summary>
public static class ExcelHelper
{
    static ExcelHelper()
    {
        // EPPlus 8+ 废弃了 LicenseContext，改用 ExcelPackage.License.SetXXX()
        // 旧写法 LicenseContext = NonCommercial 会抛 LicenseContextPropertyObsoleteException
        ExcelPackage.License.SetNonCommercialPersonal("mcpx_template_project");
    }

    /// <summary>
    /// 导出数据到 Excel 文件
    /// </summary>
    public static void ExportToExcel<T>(IEnumerable<T> data, string filePath, List<ExcelColumnConfig<T>> columns)
    {
        using var package = new ExcelPackage(new FileInfo(filePath));
        var ws = package.Workbook.Worksheets.Add("Sheet1");

        // 写入表头
        for (var i = 0; i < columns.Count; i++)
        {
            ws.Cells[1, i + 1].Value = columns[i].Header;
        }

        // 写入数据
        var row = 2;
        foreach (var item in data)
        {
            for (var col = 0; col < columns.Count; col++)
            {
                var config = columns[col];
                if (config.Selector == null) continue;

                var value = config.Selector(item);
                if (config.DisplayConverter != null)
                {
                    value = config.DisplayConverter(value);
                }
                ws.Cells[row, col + 1].Value = value;
            }
            row++;
        }

        // 自动列宽
        ws.Cells[ws.Dimension.Address].AutoFitColumns();

        package.Save();
    }

    /// <summary>
    /// 从 Excel 文件导入数据
    /// </summary>
    public static List<T> ImportFromExcel<T>(string filePath, List<ExcelColumnConfig<T>> columns, Func<T> createNew)
    {
        using var package = new ExcelPackage(new FileInfo(filePath));
        var ws = package.Workbook.Worksheets.FirstOrDefault();
        if (ws == null) return new List<T>();

        var result = new List<T>();
        var rowCount = ws.Dimension?.Rows ?? 0;
        var colCount = ws.Dimension?.Columns ?? 0;

        // 从第 2 行开始读取（第 1 行为表头）
        for (var row = 2; row <= rowCount; row++)
        {
            var entity = createNew();
            for (var col = 1; col <= colCount && col <= columns.Count; col++)
            {
                var config = columns[col - 1];
                if (config.Setter == null) continue;

                var value = ws.Cells[row, col].Value;
                if (config.ParseConverter != null)
                {
                    value = config.ParseConverter(value);
                }
                config.Setter(entity, value);
            }
            result.Add(entity);
        }

        return result;
    }
}
