using System;
using System.Net;
using ReportBridge.Models;
using ReportBridge.Services;

namespace ReportBridge.Controllers
{
    /// <summary>
    /// POST /api/design - 打开设计器（同步阻塞）
    /// </summary>
    public static class DesignController
    {
        public static void Handle(HttpListenerContext ctx)
        {
            var request = HttpServer.ReadJson<DesignRequest>(ctx);

            if (request == null || string.IsNullOrEmpty(request.TemplatePath))
            {
                HttpServer.WriteJson(ctx, 400,
                    ApiResponse.Fail("缺少 templatePath"));
                return;
            }

            try
            {
                var (savedPath, changed) =
                    GridReportService.RunDesignOnSta(request.TemplatePath);

                HttpServer.WriteJson(ctx, 200,
                    ApiResponse.Ok(new DesignResult
                    {
                        SavedPath = savedPath,
                        Changed = changed
                    }));
            }
            catch (Exception ex)
            {
                HttpServer.WriteJson(ctx, 500, ApiResponse.Fail(ex.Message));
            }
        }
    }
}
