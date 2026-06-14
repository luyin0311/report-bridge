# C# Bridge 骨架与健康检查

> Status: Agent可接手

## Parent

`.scratch/report-bridge/PRD.md`

## What to build

搭建 C# (.NET Framework 4.8) 报表桥接程序的基础骨架，实现启动流程和健康检查端点。

**启动流程**：
1. 程序启动时在 127.0.0.1 上绑定随机空闲端口
2. 通过 stdout 首行输出 `{"port":XXXXX}` 供 Tauri Sidecar 读取
3. 启动 HttpListener 监听 HTTP 请求
4. 监听 stdin 关闭信号，收到后优雅退出

**健康检查端点**：
- `GET /api/health` → 返回 `{"success":true,"data":{"status":"ok","version":"1.0.0"}}`

**内部模块**：
- `Program.cs`：入口，端口发现，stdout 输出，等待退出
- `HttpServer.cs`：HttpListener 封装，路由分发（先只接入 health），JSON 读写工具方法
- `Models/ApiModels.cs`：统一 `ApiResponse` 模型
- `Controllers/HealthController.cs`：健康检查处理
- `.csproj`：项目文件，目标 .NET Framework 4.8，引用 System.Web.Extensions

## Acceptance criteria

- [ ] 程序启动后 stdout 输出合法 JSON 包含 port 字段
- [ ] `curl http://127.0.0.1:{port}/api/health` 返回 200 + 正确 JSON 响应
- [ ] 访问未知路径返回 404 + JSON 错误信息
- [ ] stdin 收到 EOF 时程序正常退出
- [ ] CORS 头正确设置（便于调试）
- [ ] 项目可用 MSBuild 编译通过

## Blocked by

None - 可立即开始
