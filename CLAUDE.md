# AllDebrid Client — Project Guidelines

## Git commit format

```
Area: short imperative phrase
```

- No period at the end. 72 characters max for the subject line.
- Use the component or layer as the area. Examples already in history:
  `Server:`, `Client:`, `Data:`, `Tests:`, `Docs:`, `Repo:`, `CI:`, `Rebrand:`
- One logical change per commit — no "and" commits, split them.
- Multi-line body is fine for commits that need explanation.

## Versioning

Format: `UPSTREAM.MAJOR.MINOR.PATCH.FORK_REVISION`  
Current upstream base: `2.0.116` — first fork release is `2.0.116.1`.

- Bump `.FORK_REVISION` for every release cut from this fork.
- Reset to `1` when syncing a new upstream version.
- Tag as `v2.0.116.N` — CI reads the tag and sets `Version`/`AssemblyVersion` automatically.
- Never manually edit version in `.csproj` files for releases.

## CHANGELOG

- `## [Unreleased]` is the active working section.
- Before a release: rename it to `## [2.0.116.N] - YYYY-MM-DD`, add empty `[Unreleased]` above.
- On upstream sync: add `## [2.0.X] - YYYY-MM-DD (upstream sync)` between fork sections.

## Project structure

- `server/` — .NET 9 solution (`AdbClient.sln`) with 4 projects: Data, Service, Service.Test, Web
- `client/` — Angular frontend (builds into `server/AdbClient.Web/wwwroot/`)
- `root/` — Docker/s6-overlay service definitions
- `.github/workflows/` — CI: `dotnet-test.yml` (push), `build-release.yaml` (tag), `build-docker-image.yml` (tag)

## Local build

```powershell
# Frontend
cd client && npm run build

# Backend (full publish)
dotnet publish server/AdbClient.Web/AdbClient.Web.csproj -c Release -o publish
```

## Naming conventions

This is a fork of `rogerfar/rdt-client`. All identifiers have been rebranded:
- `RdtClient` → `AdbClient`, `rdt-client` → `alldebrid-client`, `rdt` → `adb`
- Do not reintroduce `Rdt`, `RDT`, or `rdt` prefixes in new code.
