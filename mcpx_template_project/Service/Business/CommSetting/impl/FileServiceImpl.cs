using mcpx_template_project.Service.Business.CommSetting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace mcpx_template_project.Service.Business.CommSetting.impl
{
    /// <summary>
    /// 文件服务实现：支持本地文件夹和UNC网络路径的文件浏览与打开
    /// </summary>
    public class FileServiceImpl : IFileService
    {
        public Task<List<FileInfo>> GetFilesAsync(string path)
        {
            var files = new List<FileInfo>();

            if (string.IsNullOrWhiteSpace(path))
                return Task.FromResult(files);

            try
            {
                // 本地路径或UNC网络路径
                if (Directory.Exists(path))
                {
                    var directoryInfo = new DirectoryInfo(path);

                    // 添加子目录
                    var directories = directoryInfo.GetDirectories()
                        .Select(d => new FileInfo
                        {
                            Name = d.Name,
                            FullPath = d.FullName,
                            Size = 0,
                            LastModified = d.LastWriteTime,
                            IsDirectory = true
                        });
                    files.AddRange(directories);

                    // 添加文件
                    var fileEntries = directoryInfo.GetFiles()
                        .Select(f => new FileInfo
                        {
                            Name = f.Name,
                            FullPath = f.FullName,
                            Size = f.Length,
                            LastModified = f.LastWriteTime,
                            IsDirectory = false
                        });
                    files.AddRange(fileEntries);
                }
            }
            catch
            {
                // 异常时返回空列表
            }

            return Task.FromResult(files);
        }

        public Task<bool> OpenFileAsync(string path, string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(fileName))
                    return Task.FromResult(false);

                string fullPath = Path.Combine(path, fileName);

                if (File.Exists(fullPath))
                {
                    // 使用系统默认程序打开文件
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = fullPath,
                        UseShellExecute = true
                    };
                    Process.Start(processStartInfo);
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public bool IsPathAccessible(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                return Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }
    }
}
