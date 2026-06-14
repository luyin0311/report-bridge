# report-bridge

> A Windows-side report bridge for integrating Grid++Report with a Tauri + Rust desktop app.

## What This Project Is

`report-bridge` is a standalone bridge process designed to run as a Tauri Sidecar.

It starts a local HTTP server on `127.0.0.1`, prints the chosen port to stdout, and exposes report operations such as health checks, template management, design, preview, and render/export.

## Repository Structure

```text
ReportBridge/             C# / .NET Framework 4.8 bridge application
  Controllers/            HTTP endpoint handlers
  Models/                 Request / response models
  Services/               Report and template services
  HttpServer.cs           HttpListener server and routing
  Program.cs              Process startup and shutdown flow

tauri-integration/src/    Rust module for Tauri sidecar lifecycle and HTTP client

docs/                     PRD and issue breakdown imported from the original planning workspace
```

## Current Scope

- Start bridge process on a random localhost port
- Print `{\"port\":<port>}` to stdout for sidecar discovery
- Expose HTTP endpoints for:
  - `/api/health`
  - `/api/templates`
  - `/api/design`
  - `/api/preview`
  - `/api/render`
- Provide a Rust-side integration module for Tauri

## Build Notes

### C# bridge

- Target framework: `.NET Framework 4.8`
- Project file: `ReportBridge/ReportBridge.csproj`

### Tauri integration

- Rust integration module lives in `tauri-integration/src/bridge.rs`
- Expected dependencies include `reqwest`, `serde`, `serde_json`, and `tokio`

## Planning Docs

- PRD: `docs/PRD.md`
- Issues: `docs/issues/`

## Known Gaps

- Public source references still need to be added
- Build and runtime validation have not yet been executed in this environment
- Grid++Report SDK and runtime installation instructions should be documented before public use
