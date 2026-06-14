# report-bridge

> A Windows-side bridge that exposes Grid++Report capabilities to a Tauri + Rust desktop app through a local HTTP API.

## Overview

`report-bridge` runs as a Tauri Sidecar on Windows.

It starts a localhost-only HTTP server, prints the selected port to stdout, and exposes report operations such as health checks, template management, report design, preview, and export.

This repository contains the bridge application itself plus a Rust integration module for Tauri.

## Why This Exists

Grid++Report is a Windows-native reporting tool. Tauri applications, especially those using a Rust backend and web frontend, cannot call the report designer and viewer directly.

This project isolates that Windows-specific integration behind a standalone process and a small local HTTP contract.

## Features

- Starts on a random `127.0.0.1` port and reports it through stdout
- Exposes HTTP endpoints for health, templates, design, preview, and render/export
- Keeps the report designer and print preview in the bridge process
- Includes a Rust-side Tauri integration module for sidecar lifecycle and HTTP calls
- Documents the commercial Grid++Report dependency separately from the open-source wrapper code

## Repository Structure

```text
ReportBridge/             C# / .NET Framework 4.8 bridge application
  Controllers/            HTTP endpoint handlers
  Models/                 Request / response models
  Services/               Grid++Report and template services
  HttpServer.cs           HttpListener server and routing
  Program.cs              Process startup and shutdown flow

tauri-integration/src/    Rust module for Tauri sidecar lifecycle and HTTP client

docs/
  PRD.md                  Product and delivery context
  issues/                 Task breakdown imported from planning
  installation.md         Local setup and runtime deployment guide
  dependencies.md         Dependency matrix and public source links
  tauri-integration.md    How to embed the bridge in a Tauri app
```

## Architecture

```text
Tauri app
  -> launches report-bridge.exe as a sidecar
  -> reads {"port":<port>} from stdout
  -> calls localhost HTTP API
  -> receives JSON responses

report-bridge.exe
  -> hosts HttpListener on 127.0.0.1
  -> dispatches /api/* routes
  -> uses Grid++Report COM objects for design / preview / export
```

## HTTP API

- `GET /api/health`
- `GET /api/templates?dir=...`
- `DELETE /api/templates/{urlEncodedFullPath}`
- `POST /api/design`
- `POST /api/preview`
- `POST /api/render`

For request and response details, see `docs/PRD.md` and the models in `ReportBridge/Models/ApiModels.cs`.

## Quick Start

### 1. Read the dependency notes first

This repository is open source, but the underlying Grid++Report SDK/runtime is a separate proprietary dependency and is not bundled here.

Start with:

- `docs/dependencies.md`
- `docs/installation.md`
- `docs/tauri-integration.md`

### 2. Install prerequisites

Required for development:

- Windows
- Visual Studio or MSBuild capable of building `.NET Framework 4.8`
- Grid++Report developer package from the vendor

Required for runtime on target machines:

- Grid++Report end-user deployment package, or an equivalent vendor-approved runtime deployment method

### 3. Build the C# bridge

Open `ReportBridge/ReportBridge.csproj` in Visual Studio and build it.

Or use MSBuild:

```powershell
msbuild .\ReportBridge\ReportBridge.csproj /p:Configuration=Release
```

### 4. Integrate with Tauri

- Copy the built `report-bridge.exe` into your Tauri app's `src-tauri/binaries/`
- Add the sidecar configuration in `tauri.conf.json`
- Import and register `tauri-integration/src/bridge.rs`

A full walkthrough is available in `docs/tauri-integration.md`.

## Dependency Policy

This repository does **not** include:

- Grid++Report SDK installers
- Grid++Report runtime installers
- Grid++Report COM DLLs
- Vendor-owned sample assets not cleared for redistribution

If you are contributing to this repository, do not commit vendor binaries or installer packages.

## Documentation

- Setup and deployment: `docs/installation.md`
- Dependency matrix and official links: `docs/dependencies.md`
- Tauri embedding guide: `docs/tauri-integration.md`
- Product context: `docs/PRD.md`
- Delivery slices: `docs/issues/`

## Current Status

What exists now:

- Bridge process startup flow
- Local HTTP server and route dispatching
- Template management endpoints
- Design, preview, and render service wrappers
- Tauri-side Rust bridge module

What is still missing or unverified:

- Verified build instructions in a clean Windows machine
- End-to-end runtime validation against a real Tauri host app
- Published release artifacts
- Automated tests around externally observable behavior

## Contributing

Please read `CONTRIBUTING.md` before opening a pull request.

The main rule for this repository is simple: the wrapper code is open, but vendor dependencies must stay out of version control.

## License

This repository is licensed under the MIT License. See `LICENSE`.
