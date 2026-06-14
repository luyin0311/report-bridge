using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace ReportBridge
{
    /// <summary>
    /// 报表桥接程序入口
    /// 启动流程：
    ///   1. 找空闲端口
    ///   2. stdout 输出 {"port":XXXXX}（供 Tauri Sidecar 读取）
    ///   3. 启动 HTTP Server
    ///   4. 等待关闭信号
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // 隐藏控制台窗口（可选，用户不需要看到它）
            // 如果不想隐藏，注释掉下面两行
            // var handle = NativeMethods.GetConsoleWindow();
            // NativeMethods.ShowWindow(handle, 0);

            try
            {
                // 1. 找到一个空闲端口
                int port = FindFreePort();
                Console.Error.WriteLine($"[Bridge] 找到空闲端口: {port}");

                // 2. 输出端口号到 stdout（Tauri Rust 端通过 Sidecar stdout 读取）
                // 注意：只能输出这一行 JSON，后续 stdout 不再使用
                var startupInfo = Encoding.UTF8.GetBytes(
                    $"{{\"port\":{port}}}{Environment.NewLine}");
                var stdout = Console.OpenStandardOutput();
                stdout.Write(startupInfo, 0, startupInfo.Length);
                stdout.Flush();

                Console.Error.WriteLine($"[Bridge] 端口已通知父进程");

                // 3. 启动 HTTP Server
                using (var server = new HttpServer(port))
                {
                    server.Start();

                    // 4. 等待程序退出信号
                    var exitEvent = new ManualResetEvent(false);
                    Console.CancelKeyPress += (s, e) =>
                    {
                        e.Cancel = true;
                        exitEvent.Set();
                    };

                    // 同时也监听父进程退出（stdin 关闭）
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            var stdin = Console.OpenStandardInput();
                            var buf = new byte[1];
                            while (stdin.Read(buf, 0, 1) > 0) { }
                        }
                        catch { }
                        exitEvent.Set();
                    });

                    Console.Error.WriteLine("[Bridge] 等待请求中...");
                    exitEvent.WaitOne();
                    Console.Error.WriteLine("[Bridge] 正在退出...");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Bridge] 启动失败: {ex.Message}");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// 在 127.0.0.1 上找一个空闲端口
        /// </summary>
        static int FindFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
