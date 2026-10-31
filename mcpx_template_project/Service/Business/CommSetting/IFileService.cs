using System.Collections.Generic;
using System.Threading.Tasks;

namespace mcpx_template_project.Service.Business.CommSetting
{
    /// <summary>
    /// 文件服务接口：支持本地文件夹和UNC网络路径的文件浏览与打开
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// 获取指定路径下的所有文件和文件夹
        /// </summary>
        /// <param name="path">本地文件夹路径或UNC网络路径（如\\10.0.68.132\共享文件夹）</param>
        /// <returns>文件和文件夹列表</returns>
        Task<List<FileInfo>> GetFilesAsync(string path);

        /// <summary>
        /// 使用系统默认程序打开指定文件
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <param name="fileName">文件名</param>
        /// <returns>是否成功打开</returns>
        Task<bool> OpenFileAsync(string path, string fileName);

        /// <summary>
        /// 检查路径是否可访问
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns>是否可访问</returns>
        bool IsPathAccessible(string path);
    }

    /// <summary>
    /// 文件信息
    /// </summary>
    public class FileInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime? LastModified { get; set; }
        public bool IsDirectory { get; set; }
    }
}
