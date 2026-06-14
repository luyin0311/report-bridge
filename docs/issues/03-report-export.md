# 报表导出

> Status: Agent可接手

## Parent

`.scratch/report-bridge/PRD.md`

## What to build

实现报表的渲染导出功能。Tauri 端传入模板路径、数据集 JSON 和导出参数，桥接程序加载模板、绑定数据、导出文件并返回文件路径。

**端点**：
- `POST /api/render`
  - 请求体：`{ "templatePath", "data": { "columns": [...], "rows": [...] }, "format": "pdf", "outputPath": "..." }`
  - 响应：`{ "success": true, "data": { "filePath": "..." } }`

**内部流程**：
1. 加载 `.grf` 模板文件
2. 将 JSON 数据集转换为 DataTable
3. 将 DataTable 绑定到锐浪报表的 DetailGrid.Recordset
4. 调用 `Recalc()` 重算报表
5. 根据 format 参数导出对应格式
6. 返回输出文件路径

**支持的导出格式**：PDF、HTML、XLS、CSV、PNG

**内部模块**：
- `Controllers/RenderController.cs`：参数校验 + 调用服务
- `Services/GridReportService.cs`：锐浪报表核心操作（加载、数据绑定、导出）
- `Models/ApiModels.cs`：新增 `RenderRequest`、`ReportDataset`、`RenderResult` 模型
- `HttpServer.cs`：路由表新增 `/api/render`

## Acceptance criteria

- [ ] 给定合法模板 + JSON 数据，导出 PDF 成功，文件生成在指定路径
- [ ] 返回的 `filePath` 与请求的 `outputPath` 一致
- [ ] 支持的格式（pdf/html/xls/csv/png）均能正确导出
- [ ] 不支持的 format 返回 500 + 错误信息
- [ ] 缺少 templatePath 或 outputPath 时返回 400
- [ ] 模板文件不存在时返回 500 + 错误信息

## Blocked by

- `01-csharp-bridge-skeleton` — 依赖 HttpServer 骨架和路由机制
