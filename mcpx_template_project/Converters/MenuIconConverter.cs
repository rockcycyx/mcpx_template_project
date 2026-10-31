using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Data;
using iNKORE.UI.WPF.Modern.Common.IconKeys;

namespace mcpx_template_project
{
    /// <summary>
    /// 菜单图标转换器：把 sys_menu.icon 列的值转换为 iNKORE 官方 FontIcon 控件可用的图标数据（FontIconData）。
    /// 支持的 icon 值（不区分大小写）：
    ///   1. 官方 SegoeFluentIcons 名称（如 BulletedList、Setting、Contact —— 与官方文档一致）
    ///   2. 语义别名（如 system/user/role/menu/log/home，向后兼容旧数据）
    ///   3. 4 位十六进制字形码（如 E713、E8FD）
    ///   4. icon 为空时按菜单名称关键字推断（如名称含"用户"→联系人）
    ///   5. 以上全部未命中时按菜单类型兜底（M目录→Folder，C菜单→Document）
    /// </summary>
    public class MenuIconConverter : IMultiValueConverter
    {
        /// <summary>Segoe Fluent Icons / Segoe MDL2 Assets 字体族（Win11/Win10 自动回退）</summary>
        public const string IconFontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets";

        /// <summary>官方 SegoeFluentIcons 图标名称缓存（首次使用时反射构建）</summary>
        private static Dictionary<string, object>? _officialIcons;

        /// <summary>语义别名 → 官方字形码（向后兼容旧数据）</summary>
        public static readonly Dictionary<string, string> IconGlyphs = new(StringComparer.OrdinalIgnoreCase)
        {
            ["home"]       = "E80F", // Home
            ["system"]     = "E713", // Setting
            ["user"]       = "E77B", // Contact
            ["peoples"]    = "E716", // People
            ["role"]       = "E716", // People
            ["customer"]   = "E716", // People
            ["menu"]       = "E8FD", // BulletedList
            ["log"]        = "E8A5", // Document
            ["dictionary"] = "E82D", // Dictionary
            ["monitor"]    = "E9D9", // Diagnostic
            ["quality"]    = "E73E", // CheckMark
            ["order"]      = "E719", // Shop
            ["estimation"] = "E8EF", // Calculator
            ["document"]   = "E8F1", // Library
            ["material"]   = "E7B8", // Package
            ["product"]    = "E7B8", // Package
            ["bom"]        = "E8A9", // ViewAll
            ["notice"]     = "E91C", // ActionCenter
            ["workorder"]  = "E90F", // Repair
            ["labor"]      = "E916", // Stopwatch
            ["schedule"]   = "E916", // Stopwatch
            ["time"]       = "E916", // Stopwatch
            ["dept"]       = "E825", // Bank
            ["post"]       = "E821", // Work
            ["base"]       = "E838", // FolderOpen
            ["business"]   = "E719", // Shop
            ["edit"]       = "E70F", // Edit
            ["search"]     = "E721", // Search
            ["download"]   = "E896", // Download
            ["save"]       = "E74E", // Save
            ["delete"]     = "E74D", // Delete
            ["add"]        = "E710", // Add
            ["refresh"]    = "E72C", // Refresh
            ["lock"]       = "E72E", // Lock
            ["password"]   = "E72E", // Lock
            ["info"]       = "E946", // Info
            ["warning"]    = "E7BA", // Warning
        };

        /// <summary>菜单名称关键字 → 字形码（icon 列为空时兜底）</summary>
        private static readonly (string Keyword, string Glyph)[] NameKeywords =
        {
            ("首页", "E80F"), ("主页", "E80F"),
            ("质检", "E73E"),
            ("系统", "E713"),
            ("监控", "E9D9"),
            ("日志", "E8A5"),
            ("用户", "E77B"),
            ("角色", "E716"),
            ("客户", "E716"),
            ("菜单", "E8FD"),
            ("部门", "E825"),
            ("岗位", "E821"),
            ("通知", "E91C"), ("公告", "E91C"),
            ("字典", "E82D"),
            ("订单", "E719"),
            ("估价", "E8EF"),
            ("资料", "E8F1"), ("文档", "E8F1"),
            ("物料", "E7B8"), ("产品", "E7B8"),
            ("BOM", "E8A9"),
            ("工单", "E90F"),
            ("工时", "E916"), ("定时", "E916"), ("任务", "E916"),
        };

        private static readonly Regex HexGlyphRegex = new("^[0-9A-Fa-f]{4}$", RegexOptions.Compiled);

        /// <summary>
        /// 多值转换：[0]=Icon，[1]=MenuName，[2]=MenuType（XAML 用 MultiBinding 传入）。
        /// 返回 FontIconData，直接用于 ui:FontIcon 的 Icon 属性。
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var icon = values.Length > 0 ? values[0] as string : null;
            var menuName = values.Length > 1 ? values[1] as string : null;
            var menuType = values.Length > 2 ? values[2] as string : null;
            return ResolveIcon(icon, menuName, menuType);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        /// <summary>
        /// 解析图标。menuName 用于关键字兜底，menuType 用于最终兜底（M 目录 / C 菜单）。
        /// </summary>
        public static object ResolveIcon(string? icon, string? menuName = null, string? menuType = null)
        {
            // 1. '#' 或空 → 走名称/类型兜底
            if (!string.IsNullOrWhiteSpace(icon) && icon != "#")
            {
                var name = icon.Trim();

                // 2. 官方 SegoeFluentIcons 名称（如 BulletedList、Setting）
                var official = TryGetOfficialIcon(name);
                if (official != null)
                    return official;

                // 3. 语义别名
                if (IconGlyphs.TryGetValue(name, out var glyph))
                    return FromGlyph(glyph);

                // 4. 4 位十六进制字形码
                if (HexGlyphRegex.IsMatch(name))
                    return FromGlyph(name.ToUpperInvariant());
            }

            // 5. 菜单名称关键字推断
            if (!string.IsNullOrWhiteSpace(menuName))
            {
                foreach (var (keyword, glyph) in NameKeywords)
                {
                    if (menuName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        return FromGlyph(glyph);
                }
            }

            // 6. 按菜单类型兜底：目录→文件夹，菜单→文档
            return FromGlyph(menuType == "C" ? "E8A5" : "E8B7");
        }

        /// <summary>字形码 → FontIconData（iNKORE 官方 API，字体族自动回退）</summary>
        private static object FromGlyph(string hexGlyph)
            => SegoeFluentIcons.CreateIcon(System.Convert.ToInt32(hexGlyph, 16));

        /// <summary>按名称反射查找 SegoeFluentIcons 的静态图标成员（如 BulletedList/Setting）。注意图标成员既有静态属性也有静态只读字段，两类都要遍历。</summary>
        private static object? TryGetOfficialIcon(string name)
        {
            if (_officialIcons == null)
            {
                var cache = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                var flags = BindingFlags.Public | BindingFlags.Static;

                foreach (var prop in typeof(SegoeFluentIcons).GetProperties(flags))
                {
                    if (prop.Name == nameof(SegoeFluentIcons.FontFamily))
                        continue;
                    try
                    {
                        var value = prop.GetValue(null);
                        if (value != null)
                            cache[prop.Name] = value;
                    }
                    catch
                    {
                        // 忽略个别无法读取的属性
                    }
                }

                foreach (var field in typeof(SegoeFluentIcons).GetFields(flags))
                {
                    try
                    {
                        var value = field.GetValue(null);
                        if (value != null)
                            cache[field.Name] = value;
                    }
                    catch
                    {
                        // 忽略个别无法读取的字段
                    }
                }

                _officialIcons = cache;
            }
            return _officialIcons.TryGetValue(name, out var result) ? result : null;
        }
    }
}
