using System;
using System.Net;
using ReportBridge.Models;
using ReportBridge.Services;

namespace ReportBridge.Controllers
{
    /// <summary>
    /// POST /api/render - 导出报表文件
    /// </summary>
    public static class RenderController
    {
        public static void Handle(HttpListenerContext ctx)
        {
            var request = HttpServer.ReadJson<RenderRequest>(ctx);

            if (request == null || string.IsNullOrEmpty(request.TemplatePath))
            {
                HttpServer.WriteJson(ctx, 400,
                    ApiResponse.Fail("缺少 templatePath"));
                return;
            }

            if (string.IsNullOrEmpty(request.OutputPath))
            {
                HttpServer.WriteJson(ctx, 400,
                    ApiResponse.Fail("缺少 outputPath"));
                return;
            }

            if (request.Data == null || request.Data.Columns == null ||
                request.Data.Columns.Count == 0)
            {
                HttpServer.WriteJson(ctx, 400,
                    ApiResponse.Fail("缺少数据源 data"));
                return;
            }

            try
            {
                var filePath = GridReportService.RunRender(
                    request.TemplatePath,
                    request.Data,
                    request.Format,
                    request.OutputPath);

                HttpServer.WriteJson(ctx, 200,
                    ApiResponse.Ok(new RenderResult { FilePath = filePath }));
            }
            catch (Exception ex)
            {
                HttpServer.WriteJson(ctx, 500, ApiResponse.Fail(ex.Message));
            }
        }
    }
}
