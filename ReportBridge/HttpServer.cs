using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace ReportBridge
{
    /// <summary>
    /// HTTP Server - HttpListener 封装 + 路由分发
    /// </summary>
    public class HttpServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public int Port { get; }

        public HttpServer(int port)
        {
            Port = port;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        }

        public void Start()
        {
            _listener.Start();
            Console.Error.WriteLine($"[Bridge] HTTP 服务已启动: http://127.0.0.1:{Port}");

            // 异步接收请求
            ThreadPool.QueueUserWorkItem(_ => ListenLoop(_cts.Token));
        }

        private void ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    // 每个请求在线程池处理，支持并发
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(ctx));
                }
                catch (HttpListenerException) when (token.IsCancellationRequested)
                {
                    break; // 正常关闭
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Bridge] 请求处理异常: {ex.Message}");
                }
            }
        }

        private void HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                var path = ctx.Request.Url.AbsolutePath;
                var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                // 添加 CORS 头，方便 Vue 前端直接调试
                ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
                ctx.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, DELETE, OPTIONS");
                ctx.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

                if (ctx.Request.HttpMethod == "OPTIONS")
                {
                    ctx.Response.StatusCode = 204;
                    ctx.Response.Close();
                    return;
                }

                Route(ctx, segments);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Bridge] 处理请求异常: {ex.Message}");
                try
                {
                    WriteJson(ctx, 500,
                        Models.ApiResponse.Fail($"服务器内部错误: {ex.Message}"));
                }
                catch
                {
                    // response 已经关闭或无法写入，只能忽略
                    Console.Error.WriteLine($"[Bridge] 无法写入错误响应");
                }
            }
        }

        /// <summary>
        /// 路由分发
        /// </summary>
        private void Route(HttpListenerContext ctx, string[] segments)
        {
            // /api/health
            if (segments.Length >= 2 && segments[0] == "api" && segments[1] == "health")
            {
                Controllers.HealthController.Handle(ctx);
            }
            // /api/templates[/...]
            else if (segments.Length >= 2 && segments[0] == "api" && segments[1] == "templates")
            {
                Controllers.TemplateController.Handle(ctx, segments);
            }
            // /api/design
            else if (segments.Length == 2 && segments[0] == "api" && segments[1] == "design")
            {
                Controllers.DesignController.Handle(ctx);
            }
            // /api/preview
            else if (segments.Length == 2 && segments[0] == "api" && segments[1] == "preview")
            {
                Controllers.PreviewController.Handle(ctx);
            }
            // /api/render
            else if (segments.Length == 2 && segments[0] == "api" && segments[1] == "render")
            {
                Controllers.RenderController.Handle(ctx);
            }
            else
            {
                WriteJson(ctx, 404,
                    Models.ApiResponse.Fail($"未知接口: {ctx.Request.Url.AbsolutePath}"));
            }
        }

        /// <summary>
        /// 从请求体读取 JSON 并反序列化
        /// </summary>
        public static T ReadJson<T>(HttpListenerContext ctx)
        {
            using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
            {
                var body = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(body)) return default;

                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(body)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));
                    return (T)serializer.ReadObject(ms);
                }
            }
        }

        /// <summary>
        /// 写入 JSON 响应
        /// </summary>
        public static void WriteJson(HttpListenerContext ctx, int statusCode, Models.ApiResponse response)
        {
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = "application/json; charset=utf-8";

            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(Models.ApiResponse));
                serializer.WriteObject(ms, response);
                var json = Encoding.UTF8.GetString(ms.ToArray());
                var buffer = Encoding.UTF8.GetBytes(json);
                ctx.Response.ContentLength64 = buffer.Length;
                ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            ctx.Response.Close();
        }

        public void Stop()
        {
            _cts.Cancel();
            try { _listener.Stop(); } catch { }
            _listener.Close();
        }

        public void Dispose()
        {
            _cts.Dispose();
            Stop();
        }
    }
}
