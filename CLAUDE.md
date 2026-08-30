# AllDebrid Client

<!-- ============================================================ -->
<!-- PINNED CONTEXT — read every turn, update when rules change   -->
<!-- ============================================================ -->
## Pinned Context

> Treat this block as the durable rulebook. When a persistent rule changes, edit this section in the same turn.

- **Commits:** `Area: short imperative phrase`, no period, 72 chars max. Common areas: `Server`, `Client`, `Data`, `Tests`, `Docs`, `Repo`, `CI`, `Docker`.
- **Attribution:** do not add generated-by, assistant, Codex, Claude, or similar AI attribution to code, docs, comments, commits, or metadata.
- **Branches:** work on `main` unless the user explicitly asks for a branch. Keep history flat with fast-forward merges when possible.
- **Versioning:** SemVer tags are `vX.Y.Z`. CI sets release versions from tags; do not manually edit project `<Version>` values for releases.
- **Changelog:** keep `[Unreleased]` current using Keep a Changelog subsections.
- **Naming:** do not introduce `Rdt`, `RDT`, or `rdt` prefixes. Use `Adb`, `adb`, `AdbClient`, and AllDebrid language.
- **Secrets/local state:** never commit `.claude/`, `.cora/`, `.vscode/`, `.codex/`, app data, logs, local DBs, or local install output.

<!-- ============================================================ -->

Self-hosted AllDebrid torrent manager. Angular frontend, .NET 9 backend, SQLite data store, Docker release path.

## Commands

```powershell
# Terminal menu / one-file launcher
.\dev.ps1

# Main local path: verify, then run backend if checks pass
.\dev.ps1 run

# Full local verification
.\tools\check-project.ps1

# Frontend
cd client
npm ci
npm run build
npm run lint
npm run format:check

# Backend
dotnet restore server
dotnet build --no-restore server
dotnet test --no-build server

# Create local Windows publish output (defaults to ignored publish/)
.\publish.ps1
```

## Layout

- `client/` — Angular app. Builds into `server/AdbClient.Web/wwwroot/`.
- `server/AdbClient.Web/` — ASP.NET Core host, controllers, auth, static frontend serving.
- `server/AdbClient.Service/` — app logic, torrent orchestration, AllDebrid client, download/unpack/background services.
- `server/AdbClient.Data/` — EF Core context, migrations, repositories, data models.
- `server/AdbClient.Service.Test/` — backend tests.
- `root/` — Docker s6-overlay service definitions.
- `tools/` — local Docker and verification helpers.
- `.github/workflows/` — test, release zip, and Docker image automation.

## Architecture

- Web depends on Service and Data.
- Service depends on Data.
- Data owns EF Core, migrations, and persistence models.
- Controllers should stay thin: validate/shape requests, call services, return responses.
- Tested filesystem code uses `System.IO.Abstractions` (`IFileSystem`) instead of direct `System.IO` access.
- Angular SPA routes must have a canonical landing route plus explicit aliases/fallbacks for externally opened or refreshed links.
- Database/API fields with legacy `Rd` names are compatibility contracts; user-facing and internal implementation language uses AllDebrid/provider terminology.

## Automation

- `dotnet-test.yml` validates backend restore/build/test and frontend lint/format checks.
- `build-release.yaml` creates the Windows release zip from `vX.Y.Z` tags.
- `build-docker-image.yml` publishes Docker Hub and GHCR images from tags.
- Dependabot configuration is present but new PRs are paused with an open-PR limit of zero.

## Docker

- Runtime image exposes port `6500`.
- Persistent paths are `/data/db` and `/data/downloads`.
- Primary images are `lekrakin/alldebrid-client` and `ghcr.io/krkn-dev/alldebrid-client`.

## Maintenance Boundaries

- Keep service logic modular; avoid adding more responsibilities to `Torrents.cs`.
- Prefer small service classes with clear interfaces over broad static helpers.
- Do not ship self-update scripts; releases are installed explicitly or updated through the container runtime.
