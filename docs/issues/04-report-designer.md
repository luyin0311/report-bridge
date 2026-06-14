# 报表设计器

> Status: Agent可接手

## Parent

`.scratch/report-bridge/PRD.md`

## What to build

实现报表设计器功能。Tauri 端通过 HTTP 调用打开锐浪报表设计器窗口，用户在设计器中编辑模板，关闭窗口后返回保存结果。

**端点**：

- `POST /api/design`
  - 请求体：`{ "templatePath": "C:\\reports\\sales.grf" }`
  - **同步阻塞**：HTTP 请求挂起，直到用户关闭设计器窗口
  - 响应：`{ "success": true, "data": { "savedPath": "...", "changed": true/false } }`

**内部流程**：

1. 如果模板文件已存在，加载到报表对象
2. 将报表对象绑定到设计器控件（`Designer.Report = report`）
3. 设置窗口标题
4. 以模态方式打开设计器（`Designer.Design(true)`）
5. 用户保存 → 写回文件，changed=true；用户取消 → changed=false
6. 释放 COM 资源

**内部模块**：

- `Controllers/DesignController.cs`：参数校验 + 调用服务
- `Services/GridReportService.cs`：新增 `Design()` 方法
- `Models/ApiModels.cs`：新增 `DesignRequest`、`DesignResult` 模型
- `HttpServer.cs`：路由表新增 `/api/design`

## Acceptance criteria

- [ ] 传入已存在的模板路径，设计器打开并显示模板内容
- [ ] 传入新文件路径（不存在），设计器打开空白报表
- [ ] 用户在设计中保存后关闭 → HTTP 返回 `changed: true`，文件已写入
- [ ] 用户取消关闭 → HTTP 返回 `changed: false`
- [ ] 缺少 templatePath 时返回 400
- [ ] COM 资源在操作完成后正确释放

## Blocked by

- `01-csharp-bridge-skeleton` — 依赖 HttpServer 骨架和路由机制
