using System;
using System.Net;
using System.Web;
using ReportBridge.Models;
using ReportBridge.Services;

namespace ReportBridge.Controllers
{
    /// <summary>
    /// GET    /api/templates?dir=...    - 列出模板
    /// DELETE /api/templates/{name}     - 删除模板
    /// </summary>
    public static class TemplateController
    {
        private static readonly TemplateService _service = new TemplateService();

        public static void Handle(HttpListenerContext ctx, string[] segments)
        {
            var method = ctx.Request.HttpMethod.ToUpper();

            if (method == "GET")
            {
                HandleList(ctx);
            }
            else if (method == "DELETE" && segments.Length >= 3)
            {
                var fullPath = HttpUtility.UrlDecode(segments[2]);
                HandleDelete(ctx, fullPath);
            }
            else
            {
                HttpServer.WriteJson(ctx, 405, ApiResponse.Fail("不支持的方法"));
            }
        }

        private static void HandleList(HttpListenerContext ctx)
        {
            var dir = ctx.Request.QueryString["dir"];

            if (string.IsNullOrEmpty(dir))
            {
                HttpServer.WriteJson(ctx, 400,
                    ApiResponse.Fail("缺少 dir 参数"));
                return;
            }

            try
            {
                var templates = _service.ListTemplates(dir);
                HttpServer.WriteJson(ctx, 200,
                    ApiResponse.Ok(new { templates }));
            }
            catch (Exception ex)
            {
                HttpServer.WriteJson(ctx, 500, ApiResponse.Fail(ex.Message));
            }
        }

        private static void HandleDelete(HttpListenerContext ctx, string templateDir)
        {
            try
            {
                _service.DeleteTemplate(templateDir);
                HttpServer.WriteJson(ctx, 200, ApiResponse.Ok());
            }
            catch (Exception ex)
            {
                HttpServer.WriteJson(ctx, 500, ApiResponse.Fail(ex.Message));
            }
        }
    }
}
