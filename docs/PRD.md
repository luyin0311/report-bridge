# 报表桥接程序 PRD

> Status: Agent可接手

## Problem Statement

用户正在开发一个基于 Tauri（前端 Vue + 后端 Rust）的零售管理客户端。业务中需要使用锐浪报表（Grid++Report）进行报表设计、预览和导出。然而，锐浪报表是一个仅支持 Windows 原生技术的 .NET 组件，不兼容 Web 前端技术和 Rust 后端。Tauri 项目本身无法直接集成锐浪报表。

## Solution

开发一个独立的报表桥接程序，作为 Tauri Sidecar 运行。该程序内嵌 HTTP Server，通过标准 HTTP API 向 Tauri 端暴露锐浪报表的全部能力。用户在 Tauri 客户端中的报表操作将自动路由到桥接程序，由桥接程序调用锐浪报表 SDK 完成实际工作，然后将结果返回。

整个桥接程序与 Tauri 应用打包为同一个安装包，对最终用户完全透明。

## User Stories

1. 作为门店店员，我想要在 Tauri 客户端中选择一个报表模板并预览，以便在营业中快速查看数据。
2. 作为门店店长，我想要在报表预览后将结果导出为 PDF，以便归档或打印。
3. 作为报表管理员，我想要在 Tauri 客户端中打开锐浪报表设计器修改模板，以便根据业务变化调整报表格式。
4. 作为报表管理员，我想要新建一个空白报表模板并进入设计器，以便创建新的报表类型。
5. 作为报表管理员，我想要查看某个目录下已有的所有报表模板列表，以便了解和管理现有模板。
6. 作为报表管理员，我想要删除不再使用的旧报表模板，以便保持模板库整洁。
7. 作为开发人员，我想要桥接程序在 Tauri 应用启动时自动启动、退出时自动关闭，以便用户无需手动管理进程。
8. 作为开发人员，我想要当前端通过 Tauri invoke 调用报表操作时接口语义清晰，以便快速接入和维护。
9. 作为最终用户，我想要整个系统的安装和卸载只需一个安装包，以便获得与其他桌面软件一致的体验。

## Implementation Decisions

### 架构决策

- **通信模型**：Tauri Sidecar 管理进程生命周期 + HTTP 通信。桥接程序作为 Tauri Sidecar 启动，由 Tauri 自动管理子进程的创建和销毁。通信层走本地 HTTP（127.0.0.1 随机端口），请求/响应采用 JSON 格式。
- **端口发现**：桥接程序启动时在本地回环地址上绑定随机空闲端口，将端口号以 `{"port":XXXXX}` 格式写入 stdout 首行。Tauri Rust 端通过 Sidecar stdout 事件读取端口号后，后续所有通信通过 HTTP 进行。
- **进程布局**：单 Sidecar 实例内嵌 HTTP Server（HttpListener），所有报表操作复用一个报表引擎实例。

### 技术栈

- **桥接程序**：C# / .NET Framework 4.8，Windows Forms（用于承载锐浪设计器和预览窗口）
- **HTTP 通信**：服务端使用 `System.Net.HttpListener`，客户端（Rust 端）使用 `reqwest`
- **序列化**：双方使用 JSON，C# 端 `DataContractJsonSerializer`，Rust 端 `serde_json`

### API 契约

| 方法     | 路径                                  | 说明           | 行为                                                                       |
| -------- | ------------------------------------- | -------------- | -------------------------------------------------------------------------- |
| `GET`    | `/api/health`                         | 健康检查       | 立即返回 `{"success":true,"data":{"status":"ok"}}`                         |
| `GET`    | `/api/templates?dir=...`              | 列出 .grf 模板 | 返回目录下所有模板文件信息（名称、路径、大小、修改时间）                   |
| `DELETE` | `/api/templates/{urlEncodedFullPath}` | 删除模板       | 仅允许 .grf 后缀文件                                                       |
| `POST`   | `/api/design`                         | 打开设计器     | **同步阻塞**，等待用户关闭窗口后返回保存路径和变更状态                     |
| `POST`   | `/api/preview`                        | 打开预览窗口   | **同步阻塞**，等待用户关闭窗口                                             |
| `POST`   | `/api/render`                         | 导出报表       | 加载模板 + 数据 → 导出为指定格式文件（PDF/HTML/XLS/CSV/PNG），返回输出路径 |

### 数据流

- **数据传输方式**：JSON 推模式。Tauri 端将完整数据以 JSON 格式通过 HTTP Body 传递给桥接程序。桥接程序将 JSON 数据转换为 DataTable 后绑定到锐浪报表的数据源。
- **数据集格式**：

```json
{
  "columns": ["商品名称", "销售数量", "销售金额"],
  "rows": [
    ["苹果", 10, 50.0],
    ["香蕉", 5, 25.0]
  ]
}
```

### UI 交互模式

- 设计器和预览窗口以**模态窗口**形式弹出，HTTP 请求同步阻塞直到用户关闭窗口。响应中包含操作结果（如是否保存、保存路径）。
- 窗口宿主在桥接程序进程中，与 Tauri 窗口相互独立。

### 模块划分

- **C# 端**：`HttpServer`（HTTP 监听 + 路由）、`Controllers`（各端点处理逻辑）、`GridReportService`（锐浪报表 SDK 封装）、`TemplateService`（模板文件管理）、`Models`（请求/响应数据类型）
- **Rust 端**：`bridge.rs`（Sidecar 生命周期管理 + HTTP 客户端 + Tauri Commands），作为 `src-tauri/src/` 下的独立模块

### 安全性

- HTTP 服务仅监听 `127.0.0.1`，不对外暴露
- 随机端口避免端口冲突
- 模板文件操作仅允许 `.grf` 后缀

## Testing Decisions

### 测试策略

只测试外部可观察行为，不测试内部实现细节。

### 需要测试的模块

- **桥接程序 HTTP API**：使用 HTTP 客户端（如 curl 或 reqwest）对每个端点进行集成测试，验证：
  - 健康检查返回正确状态
  - 列出/删除模板正确响应文件系统操作
  - 导出接口输出正确格式的文件到指定路径
- **Tauri bridge 模块**：Mock 本地 HTTP Server，验证 Rust 端 HTTP 客户端正确构造请求和解析响应

### 不需要测试的范围

- 锐浪报表 SDK 内部行为（第三方组件，由厂商保证）
- 设计器和预览 UI 交互（纯 GUI 操作，由人工验收）

## Out of Scope

- 多用户并发设计——单用户桌面客户端，不存在并发设计场景
- 跨平台支持——锐浪报表仅支持 Windows，桥接程序不规划 macOS/Linux 支持
- 远程报表服务——桥接程序仅服务于本地 Tauri 进程，不暴露为网络服务
- 报表数据的增删改——数据由 Tauri 端管理，桥接程序只消费数据
- 模板的在线同步/版本控制——本地文件管理即可
- Web 端直接调用（Vue 前端 fetch 直连）——不鼓励但技术上可行（CORS 头已添加），主要使用 Rust invoke 调用

## Further Notes

- **锐浪 SDK 依赖**：开发机需安装锐浪报表 SDK（Grid++Report 6.x 或更高版本），部署机需安装锐浪报表运行时。
- **STA 线程**：锐浪报表 UI 控件需要 STA 线程，托管在 Windows Forms 应用上下文中。
- **构建流程**：`report-bridge.exe` 编译后需手动复制到 `src-tauri/binaries/` 目录，再执行 `tauri build`。
- **日志**：桥接程序通过 stderr 输出日志，Tauri 端可通过 Sidecar 的 Stderr 事件获取调试信息。
