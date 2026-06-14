using System;
using System.Net;
using ReportBridge.Models;
using ReportBridge.Services;

namespace ReportBridge.Controllers
{
    /// <summary>
    /// POST /api/preview - 打开预览窗口（同步阻塞）
    /// </summary>
    public static class PreviewController
    {
        public static void Handle(HttpListenerContext ctx)
        {
            var request = HttpServer.ReadJson<PreviewRequest>(ctx);

            if (request == null || string.IsNullOrEmpty(request.TemplatePath))
            {
                HttpServer.WriteJson(ctx, 400,
                    ApiResponse.Fail("缺少 templatePath"));
                return;
            }

            try
            {
                GridReportService.RunPreviewOnSta(request.TemplatePath, request.Data);
                HttpServer.WriteJson(ctx, 200, ApiResponse.Ok());
            }
            catch (Exception ex)
            {
                HttpServer.WriteJson(ctx, 500, ApiResponse.Fail(ex.Message));
            }
        }
    }
}
