# 报表预览

> Status: Agent可接手

## Parent

`.scratch/report-bridge/PRD.md`

## What to build

实现报表预览功能。Tauri 端传入模板路径和数据集 JSON，桥接程序打开锐浪报表预览窗口，用户关闭后返回。

**端点**：
- `POST /api/preview`
  - 请求体：`{ "templatePath": "...", "data": { "columns": [...], "rows": [...] } }`
  - **同步阻塞**：HTTP 请求挂起，直到用户关闭预览窗口
  - 响应：`{ "success": true }`

**内部流程**：
1. 调用 GridReportService 加载模板、绑定数据
2. 将报表对象绑定到预览控件（`PrintViewer.Report = report`）
3. 以模态方式打开预览窗口（`PrintViewer.PrintPreview(true)`）
4. 释放 COM 资源

**内部模块**：
- `Controllers/PreviewController.cs`：参数校验 + 调用服务
- `Services/GridReportService.cs`：新增 `Preview()` 方法
- `Models/ApiModels.cs`：新增 `PreviewRequest` 模型
- `HttpServer.cs`：路由表新增 `/api/preview`

## Acceptance criteria

- [ ] 传入合法模板 + JSON 数据，预览窗口打开并正确显示报表内容
- [ ] 关闭预览窗口后 HTTP 返回 200
- [ ] 缺少 templatePath 时返回 400
- [ ] 模板文件不存在时返回 500 + 错误信息
- [ ] COM 资源在操作完成后正确释放

## Blocked by

- `01-csharp-bridge-skeleton` — 依赖 HttpServer 骨架和路由机制
