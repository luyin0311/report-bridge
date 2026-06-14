using System;
using System.Data;
using System.Threading;
using ReportBridge.Models;

namespace ReportBridge.Services
{
    /// <summary>
    /// 锐浪报表核心操作服务
    /// 封装 Grid++Report SDK 的调用
    /// 
    /// 重要：锐浪报表 UI 控件（设计器、预览窗口）必须在 STA 线程上运行。
    /// HttpListener 的回调运行在线程池 MTA 线程上，直接创建 COM UI 对象会
    /// 导致 InvalidCastException 或界面异常。
    /// 
    /// 因此 Design 和 Preview 操作通过 RunOnStaThread 在独立 STA 线程上执行。
    /// Render（导出）不涉及 UI，可以在任意线程上运行。
    /// </summary>
    public class GridReportService : IDisposable
    {
        private readonly dynamic _report;
        private readonly dynamic _designer;
        private readonly dynamic _viewer;

        /// <summary>
        /// 构造函数会创建 COM 对象。必须在 STA 线程上调用（Design/Preview），
        /// 或在不需要 UI 控件的场景下（Render only）可任意线程调用。
        /// </summary>
        public GridReportService()
        {
            try
            {
                _report = Activator.CreateInstance(Type.GetTypeFromProgID("Gridpp.Report"));
                _viewer = Activator.CreateInstance(Type.GetTypeFromProgID("Gridpp.PrintViewer"));
                _designer = Activator.CreateInstance(Type.GetTypeFromProgID("Gridpp.Designer"));
            }
            catch (Exception ex)
            {
                throw new Exception("无法初始化锐浪报表组件，请确认已安装锐浪报表 SDK。", ex);
            }
        }

        /// <summary>
        /// 加载报表模板和数据，准备报表
        /// </summary>
        private void PrepareReport(string templatePath, ReportDataset dataset)
        {
            // 加载模板文件
            _report.LoadFromFile(templatePath);

            if (dataset != null && dataset.Columns != null && dataset.Columns.Count > 0)
            {
                // 从 JSON 数据构造 DataTable
                var dt = new DataTable();
                foreach (var col in dataset.Columns)
                {
                    dt.Columns.Add(col, typeof(string));
                }

                if (dataset.Rows != null)
                {
                    foreach (var row in dataset.Rows)
                    {
                        var dr = dt.NewRow();
                        for (int i = 0; i < row.Count && i < dt.Columns.Count; i++)
                        {
                            dr[i] = row[i]?.ToString() ?? "";
                        }
                        dt.Rows.Add(dr);
                    }
                }

                // 将 DataTable 绑定到报表数据源
                // 锐浪报表的数据绑定方式取决于具体版本
                _report.DetailGrid.Recordset = dt;
            }

            // 重新计算报表
            _report.Recalc();
        }

        /// <summary>
        /// 打开设计器（同步阻塞）
        /// </summary>
        public (string savedPath, bool changed) Design(string templatePath)
        {
            var result = (savedPath: templatePath, changed: false);

            // 加载现有模板（如果存在）
            if (System.IO.File.Exists(templatePath))
            {
                _report.LoadFromFile(templatePath);
            }

            // 关联设计器与报表
            _designer.Report = _report;

            // 设计器窗口标题
            _designer.SetTitle($"设计报表 - {templatePath}");

            // 打开设计器窗口
            if (_designer.Design(true))  // true = 模态
            {
                // 用户点击了保存
                _report.SaveToFile(templatePath);
                result.savedPath = templatePath;
                result.changed = true;
            }
            // 用户取消关闭：changed 保持 false

            return result;
        }

        /// <summary>
        /// 打开预览窗口（同步阻塞）
        /// </summary>
        public void Preview(string templatePath, ReportDataset dataset)
        {
            PrepareReport(templatePath, dataset);

            // 关联预览控件与报表
            _viewer.Report = _report;

            // 模态预览
            _viewer.PrintPreview(true);
        }

        /// <summary>
        /// 导出报表为文件（非 UI 操作）
        /// </summary>
        public string Render(string templatePath, ReportDataset dataset,
                             string format, string outputPath)
        {
            PrepareReport(templatePath, dataset);

            // 确保输出目录存在
            var dir = System.IO.Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            switch (format.ToLower())
            {
                case "pdf":
                    _report.ExportToFile("PDF", outputPath);
                    break;
                case "html":
                    _report.ExportToFile("HTML", outputPath);
                    break;
                case "xls":
                case "xlsx":
                    _report.ExportToFile("XLS", outputPath);
                    break;
                case "csv":
                    _report.ExportToFile("CSV", outputPath);
                    break;
                case "image":
                case "png":
                    _report.ExportToFile("IMG", outputPath);
                    break;
                default:
                    throw new ArgumentException($"不支持的导出格式: {format}");
            }

            return outputPath;
        }

        // ── STA 线程辅助 ──

        /// <summary>
        /// 在 STA 线程上执行设计器操作
        /// </summary>
        public static (string savedPath, bool changed) RunDesignOnSta(string templatePath)
        {
            (string savedPath, bool changed) result = default;
            Exception error = null;

            var thread = new Thread(() =>
            {
                try
                {
                    using (var service = new GridReportService())
                    {
                        result = service.Design(templatePath);
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (error != null) throw error;
            return result;
        }

        /// <summary>
        /// 在 STA 线程上执行预览操作
        /// </summary>
        public static void RunPreviewOnSta(string templatePath, ReportDataset dataset)
        {
            Exception error = null;

            var thread = new Thread(() =>
            {
                try
                {
                    using (var service = new GridReportService())
                    {
                        service.Preview(templatePath, dataset);
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (error != null) throw error;
        }

        /// <summary>
        /// 在任意线程上执行导出操作（不涉及 UI，无需 STA）
        /// </summary>
        public static string RunRender(string templatePath, ReportDataset dataset,
                                       string format, string outputPath)
        {
            using (var service = new GridReportService())
            {
                return service.Render(templatePath, dataset, format, outputPath);
            }
        }

        // ── Dispose ──

        public void Dispose()
        {
            if (_report != null)
            {
                try { _report.Quit(); } catch { }
                System.Runtime.InteropServices.Marshal.ReleaseComObject(_report);
            }
            if (_designer != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(_designer);
            }
            if (_viewer != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(_viewer);
            }
        }
    }
}
