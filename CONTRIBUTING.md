# Contributing

Thanks for contributing to `report-bridge`.

## Before You Start

Please read these documents first:

- `README.md`
- `docs/installation.md`
- `docs/dependencies.md`
- `docs/tauri-integration.md`

## Repository Rules

- Do not commit Grid++Report SDK installers
- Do not commit Grid++Report runtime installers
- Do not commit vendor DLLs or other redistributed binaries unless their redistribution terms are explicitly confirmed and documented
- Keep the boundary clear between open-source wrapper code and proprietary vendor dependencies

## Pull Request Checklist

Before opening a PR, please include:

- a clear summary of the change
- whether it affects development setup, runtime deployment, or Tauri integration
- any Grid++Report version assumptions you discovered
- any manual verification steps you ran

## Documentation Expectations

If you change setup, packaging, or vendor integration behavior, update the relevant docs in the same PR:

- `README.md`
- `docs/installation.md`
- `docs/dependencies.md`
- `docs/tauri-integration.md`

## Scope Guidance

Good contributions:

- clearer setup steps
- safer runtime handling
- better API ergonomics
- better documentation for packaging and deployment
- integration improvements for Tauri hosts

Be careful with contributions that:

- assume vendor binaries are checked into the repo
- depend on undocumented local machine setup
- mix host-app concerns into the standalone bridge without explanation
