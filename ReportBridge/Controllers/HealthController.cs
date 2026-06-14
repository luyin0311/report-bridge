using System.Net;
using ReportBridge.Models;

namespace ReportBridge.Controllers
{
    /// <summary>
    /// GET /api/health - 健康检查
    /// </summary>
    public static class HealthController
    {
        public static void Handle(HttpListenerContext ctx)
        {
            var response = ApiResponse.Ok(new
            {
                status = "ok",
                version = "1.0.0"
            });
            HttpServer.WriteJson(ctx, 200, response);
        }
    }
}
