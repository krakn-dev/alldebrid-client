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

Format: `MAJOR.MINOR.PATCH` ([Semantic Versioning](https://semver.org/spec/v2.0.0.html)).  
Current version: `1.0.0`. Forked from upstream rogerfar/rdt-client v2.0.116.

- Bump `PATCH` for backwards-compatible bug fixes.
- Bump `MINOR` for backwards-compatible new features.
- Bump `MAJOR` for breaking changes.
- Tag releases as `v1.0.0`, `v1.1.0`, etc. — CI reads the tag and sets `Version`/`AssemblyVersion` automatically.
- Never manually edit `<Version>` in `.csproj` files for releases; the CI overwrites it from the tag.

## CHANGELOG

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

- `## [Unreleased]` is the active working section.
- Before a release: rename it to `## [X.Y.Z] - YYYY-MM-DD`, add empty `## [Unreleased]` above.
- Each version section uses subsections: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`.
- Keep compare links at the bottom of the file up to date.

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
