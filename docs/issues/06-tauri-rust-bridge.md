# Tauri Rust 桥接模块

> Status: Agent可接手

## Parent

`.scratch/report-bridge/PRD.md`

## What to build

在 Tauri 项目的 Rust 端实现桥接客户端模块，负责 Sidecar 进程管理和所有报表 API 的 HTTP 调用。

**Sidecar 生命周期管理**：

1. 首次调用时通过 Tauri Sidecar 启动 `report-bridge.exe`
2. 从子进程 stdout 首行解析端口号
3. 轮询 `/api/health` 直到 HTTP 服务就绪
4. App 退出时自动 kill 子进程（通过 Drop trait）

**HTTP 客户端**：

- 为每个报表 API 端点封装对应的 Rust 方法
- 数据模型与 C# 端一一对应（serde 序列化/反序列化）
- 错误处理：网络错误、业务错误、超时

**Tauri Commands**（注册给前端调用）：

- `bridge_ensure_started` — 确保桥接程序运行
- `bridge_health` — 健康检查
- `bridge_list_templates` — 列出模板
- `bridge_delete_template` — 删除模板
- `bridge_design` — 打开设计器
- `bridge_preview` — 打开预览
- `bridge_render` — 导出报表

**内部模块**：

- `src-tauri/src/bridge.rs`：所有代码放一个模块

**依赖**（Cargo.toml）：

- `reqwest`（JSON feature）
- `serde` / `serde_json`
- `tokio`（sync, time features）

## Acceptance criteria

- [ ] 模块编译通过，无警告
- [ ] `bridge_ensure_started` 成功后后续所有 command 可用
- [ ] 设计/预览/导出/模板管理 各 command 输入输出类型正确
- [ ] 桥接程序未启动时调用 command 返回明确错误
- [ ] Sidecar 崩溃时 `ensure_started` 返回错误
- [ ] App 退出时子进程被 kill

## Blocked by

- `01-csharp-bridge-skeleton`
- `02-template-management`
- `03-report-export`
- `04-report-designer`
- `05-report-preview`
