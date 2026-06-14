# Tauri Integration

## Goal

This repository includes a Rust-side integration module at `tauri-integration/src/bridge.rs`.

Its job is to:

- start `report-bridge.exe` as a Tauri sidecar
- read the startup port from stdout
- wait for the local HTTP API to become healthy
- expose high-level Rust methods and Tauri commands

## Add Required Rust Dependencies

Add the required dependencies to your Tauri project's `Cargo.toml`.

```toml
[dependencies]
reqwest = { version = "0.12", features = ["json"] }
serde = { version = "1", features = ["derive"] }
serde_json = "1"
tokio = { version = "1", features = ["sync", "time"] }
```

## Add the Sidecar Binary

Build `report-bridge.exe`, then copy it into:

```text
src-tauri/binaries/report-bridge.exe
```

## Configure `tauri.conf.json`

Add the sidecar binary to your bundle config:

```json
{
  "bundle": {
    "externalBin": ["binaries/report-bridge.exe"]
  }
}
```

## Import the Module

Copy or adapt `tauri-integration/src/bridge.rs` into your Tauri backend.

At minimum, your `main.rs` needs to:

1. register a shared `ReportBridge` state
2. register the bridge commands in the invoke handler

Example shape:

```rust
mod bridge;

use bridge::ReportBridge;
use std::sync::Mutex;

fn main() {
    tauri::Builder::default()
        .manage(Mutex::new(ReportBridge::default()))
        .invoke_handler(tauri::generate_handler![
            bridge::bridge_ensure_started,
            bridge::bridge_health,
            bridge::bridge_design,
            bridge::bridge_preview,
            bridge::bridge_render,
            bridge::bridge_list_templates,
            bridge::bridge_delete_template,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
```

## Startup Contract

The C# bridge writes a single JSON line to stdout when it starts:

```json
{"port":12345}
```

The Rust module reads that first stdout line, extracts the port, then polls:

```text
GET /api/health
```

Only after the health endpoint succeeds does it store the base URL and mark the bridge as ready.

## Command Surface

The integration module exposes these Tauri commands:

- `bridge_ensure_started`
- `bridge_health`
- `bridge_list_templates`
- `bridge_delete_template`
- `bridge_design`
- `bridge_preview`
- `bridge_render`

## Packaging Notes

- `report-bridge.exe` must be included with the desktop application bundle
- target machines still need the correct Grid++Report runtime/deployment package
- this repository does not provide vendor binaries automatically

## Operational Notes

- the bridge only listens on `127.0.0.1`
- design and preview are user-driven blocking operations
- export/render can run without showing UI, but still requires the vendor components to be installed
