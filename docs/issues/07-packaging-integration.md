# Tauri 打包集成

> Status: Agent可接手

## Parent

`.scratch/report-bridge/PRD.md`

## What to build

将桥接程序集成到 Tauri 项目的构建和打包流程中，确保最终用户通过一个安装包即可使用全部功能。

**配置项**：
1. 将编译好的 `report-bridge.exe` 放入 `src-tauri/binaries/` 目录
2. 在 `tauri.conf.json` 中声明 `externalBin: ["binaries/report-bridge.exe"]`
3. 在 `src-tauri/src/main.rs` 中注册 `bridge.rs` 模块和所有 Tauri commands
4. 在 `setup` 钩子或首次调用时初始化桥接

**构建流程**（文档化）：
1. 先编译 C# 桥接程序 → 得到 `report-bridge.exe`
2. 复制到 `src-tauri/binaries/`
3. 执行 `tauri build`

**验证**：
- `tauri build` 产出的安装包中包含 `report-bridge.exe`
- 安装后可正常运行
- 卸载时桥接程序一并清理

## Acceptance criteria

- [ ] `tauri.conf.json` 正确声明 externalBin
- [ ] `main.rs` 中注册了所有 bridge commands
- [ ] `tauri build` 成功，安装包包含 `report-bridge.exe`
- [ ] 安装后启动应用，桥接程序自动运行且 `/api/health` 可达
- [ ] 关闭应用后桥接程序进程退出
- [ ] 有清晰的构建文档说明编译→复制→打包流程

## Blocked by

- `06-tauri-rust-bridge` — 需要 Rust 端桥接模块完成
