# 模板文件管理

> Status: Agent可接手

## Parent

`.scratch/report-bridge/PRD.md`

## What to build

实现报表模板文件的管理功能，包括列出模板和删除模板。

**列出模板**：
- `GET /api/templates?dir=<路径>` → 扫描指定目录下所有 `.grf` 文件
- 返回每个模板的：名称、完整路径、文件大小、最后修改时间
- 如果目录不存在，返回错误

**删除模板**：
- `DELETE /api/templates/<URL编码的完整路径>` → 删除指定 `.grf` 文件
- 只允许删除 `.grf` 后缀文件，拒绝其他后缀
- 文件不存在时返回错误

**内部模块**：
- `Controllers/TemplateController.cs`：路由处理，区分 GET/DELETE
- `Services/TemplateService.cs`：实际的文件系统操作
- `Models/ApiModels.cs`：新增 `TemplateInfo` 模型
- `HttpServer.cs`：路由表新增 `/api/templates` 匹配

## Acceptance criteria

- [ ] `GET /api/templates?dir=C:\valid\path` 返回模板列表 JSON
- [ ] 列表按文件名排序，字段完整（name/fullPath/size/lastModified）
- [ ] 目录不存在时返回 500 + 错误信息
- [ ] `DELETE /api/templates/<grf文件路径>` 成功删除文件返回 200
- [ ] `DELETE /api/templates/<非grf文件>` 返回 500 + 错误信息拒绝
- [ ] 文件不存在时返回 500 + 错误信息

## Blocked by

- `01-csharp-bridge-skeleton` — 依赖 HttpServer 骨架和路由机制
