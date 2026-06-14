# Installation

## Scope

This document explains how to set up `report-bridge` for:

- local development
- runtime deployment on a target Windows machine
- Tauri sidecar integration

## Important Boundary

This repository contains the bridge code, but **does not ship Grid++Report itself**.

You must obtain the required Grid++Report packages from the vendor's official pages.

## Prerequisites

| Item | Needed For | Notes |
| --- | --- | --- |
| Windows | Development and runtime | Grid++Report desktop components are Windows-native. |
| .NET Framework 4.8 build tools | Development | Required to build `ReportBridge/ReportBridge.csproj`. |
| Grid++Report developer package | Development | Needed to develop, build against, and validate the COM-based report components. |
| Grid++Report end-user deployment package | Runtime | Needed on target machines unless you use another vendor-approved deployment approach. |
| Rust / Tauri toolchain | Tauri integration | Only needed if you embed the bridge into a Tauri app. |

## Step 1: Install the Grid++Report developer package

Use the vendor's developer download page:

- Developer package page: `http://www.rubylong.cn/gridreport/download.htm`
- Product overview: `http://www.rubylong.cn/gridreport/product-overview.htm`

The official download page explicitly distinguishes the **developer package** from the **end-user deployment package**.

Recommended workflow:

1. Download the latest developer package from the official page.
2. Install it on your Windows development machine.
3. Verify that the vendor's examples or designer can start successfully before debugging this repository.

## Step 2: Install .NET build tooling

You need a Windows environment capable of building `.NET Framework 4.8` projects.

Typical options:

- Visual Studio with the .NET desktop development workload
- MSBuild from a Visual Studio installation or compatible Build Tools installation

## Step 3: Build the bridge application

From the repository root:

```powershell
msbuild .\ReportBridge\ReportBridge.csproj /p:Configuration=Release
```

You can also open `ReportBridge/ReportBridge.csproj` directly in Visual Studio and build from there.

If startup later fails with an error like `无法初始化锐浪报表组件，请确认已安装锐浪报表 SDK。`, the most likely cause is that the vendor SDK/runtime is not installed correctly on the machine.

## Step 4: Prepare runtime deployment

Use the vendor's official end-user deployment page:

- End-user deployment page: `http://www.rubylong.cn/gridreport/download-enduser.htm`

That page lists several packages. For this project, the most relevant one is the **Grid++Report6 客户端发布完整安装包** because it includes the C/S report components and related runtime files.

General rule:

- Development machine: install the **developer package**
- End-user machine: install the **end-user deployment package** or deploy the runtime in a vendor-approved way

Do not assume that building this repository alone is enough to run the bridge on a clean machine.

## Step 5: Integrate with a Tauri application

If you are embedding `report-bridge` in a Tauri app:

1. Build `report-bridge.exe`
2. Copy it to `src-tauri/binaries/`
3. Configure it as an external binary in `tauri.conf.json`
4. Import `tauri-integration/src/bridge.rs` into your Tauri backend

A full example is in `docs/tauri-integration.md`.

## Troubleshooting

### COM component initialization fails

Symptoms:

- `无法初始化锐浪报表组件，请确认已安装锐浪报表 SDK。`
- report design / preview / render fails before any business logic runs

Checks:

- Confirm the developer package or runtime package is installed on the current machine
- Confirm you are testing on Windows
- Confirm the vendor components are available to the current process environment

### Bridge starts but sidecar integration fails

Checks:

- Make sure `report-bridge.exe` is copied into the correct Tauri `binaries/` directory
- Make sure `tauri.conf.json` lists the external binary
- Make sure your Tauri app reads the first stdout line as `{"port":...}`

### Runtime works on the development machine but not on the target machine

This usually means the target machine is missing the Grid++Report runtime/deployment package.

Re-check the vendor's end-user deployment guidance:

- `http://www.rubylong.cn/gridreport/download-enduser.htm`
