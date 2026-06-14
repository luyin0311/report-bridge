# Dependencies

## Open-Source vs Proprietary Boundary

`report-bridge` is open-source wrapper code around a proprietary reporting dependency.

This repository contains:

- the C# bridge application
- the Tauri Rust integration module
- project documentation and planning artifacts

This repository does **not** contain:

- Grid++Report SDK installers
- Grid++Report runtime installers
- Grid++Report COM DLLs
- vendor documentation copied into the repo

## Dependency Matrix

| Dependency | Required For | Bundled In Repo | How To Obtain |
| --- | --- | --- | --- |
| Windows | Development and runtime | No | Use a supported Windows machine |
| .NET Framework 4.8 tooling | Development | No | Visual Studio / Build Tools |
| Grid++Report developer package | Development | No | Official developer download page |
| Grid++Report end-user deployment package | Runtime | No | Official end-user deployment page |
| Rust toolchain | Tauri integration | No | Rustup / Tauri setup |
| Tauri host app | Final embedding | No | Your application repository |

## Grid++Report Packages Used By This Project

The bridge service creates these COM ProgIDs at runtime:

- `Gridpp.Report`
- `Gridpp.PrintViewer`
- `Gridpp.Designer`

Because of that, machines running the bridge must already have the necessary Grid++Report components installed and available.

## Official Public Links

Vendor public pages referenced by this repository:

- Product overview: `http://www.rubylong.cn/gridreport/product-overview.htm`
- Developer package download page: `http://www.rubylong.cn/gridreport/download.htm`
- End-user deployment page: `http://www.rubylong.cn/gridreport/download-enduser.htm`

Key takeaways from those pages:

- The developer package is intended for software developers building with Grid++Report
- The end-user deployment package is intended for machines that only need to run software built with Grid++Report
- The product overview states that Grid++Report desktop components are Windows-oriented and meant to be embedded inside an application rather than exposed directly to end users

## Contributor Rules

If you contribute to this repository:

- do not commit vendor DLLs
- do not commit vendor installers or zip packages
- do not assume the repo is self-contained without vendor runtime installation
- document any version-sensitive behavior you discover while integrating with a specific Grid++Report release

## What To Document When You Change Vendor Integration

If a pull request changes the Grid++Report integration, include:

- which public vendor page you used as reference
- whether the change affects development setup, runtime deployment, or both
- whether the change assumes a specific Grid++Report version
