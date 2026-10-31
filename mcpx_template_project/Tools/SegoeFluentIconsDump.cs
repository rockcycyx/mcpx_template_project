using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using iNKORE.UI.WPF.Modern.Common.IconKeys;

namespace mcpx_template_project.Tools
{
    /// <summary>
    /// 测试工具：反射输出 SegoeFluentIcons 的全部官方图标名称。
    /// 注意：SegoeFluentIcons 的图标是「静态只读字段」（public static readonly），
    /// 不是属性 —— 用 GetProperties 只能拿到 FontFamily，必须用 GetFields。
    /// </summary>
    public static class SegoeFluentIconsDump
    {
        /// <summary>反射收集 SegoeFluentIcons 全部图标名（静态属性 + 静态字段，去重排序）。</summary>
        public static List<string> GetAllIconNames()
        {
            var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var flags = BindingFlags.Public | BindingFlags.Static;

            foreach (var p in typeof(SegoeFluentIcons).GetProperties(flags))
            {
                if (p.Name != nameof(SegoeFluentIcons.FontFamily))
                    names.Add(p.Name);
            }

            foreach (var f in typeof(SegoeFluentIcons).GetFields(flags))
            {
                names.Add(f.Name);
            }

            return new List<string>(names);
        }

        /// <summary>把全部图标名写入指定文件（每行一个，首行为总数）。</summary>
        public static void DumpToFile(string path)
        {
            var names = GetAllIconNames();
            var sb = new StringBuilder();
            sb.AppendLine($"SegoeFluentIcons 图标总数: {names.Count}");
            foreach (var n in names)
                sb.AppendLine(n);

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
    }
}
