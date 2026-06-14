using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReportBridge.Models;

namespace ReportBridge.Services
{
    /// <summary>
    /// 模板文件管理服务
    /// </summary>
    public class TemplateService
    {
        /// <summary>
        /// 列出指定目录下所有 .grf 模板文件
        /// </summary>
        public List<TemplateInfo> ListTemplates(string directory)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"目录不存在: {directory}");
            }

            var dir = new DirectoryInfo(directory);
            return dir.GetFiles("*.grf")
                .Select(f => new TemplateInfo
                {
                    Name = f.Name,
                    FullPath = f.FullName,
                    Size = f.Length,
                    LastModified = f.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                })
                .OrderBy(f => f.Name)
                .ToList();
        }

        /// <summary>
        /// 删除模板文件（仅允许 .grf 后缀）
        /// </summary>
        public void DeleteTemplate(string fullPath)
        {
            if (!fullPath.EndsWith(".grf", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("只能删除 .grf 模板文件");
            }

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"模板文件不存在: {fullPath}");
            }

            File.Delete(fullPath);
        }
    }
}
