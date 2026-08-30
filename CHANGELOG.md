# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Forked from [rogerfar/rdt-client](https://github.com/rogerfar/rdt-client) at v2.0.116.
Prior upstream history: <https://github.com/rogerfar/rdt-client/blob/main/CHANGELOG.md>

## [Unreleased]

### Added

- Root `dev.ps1` / `dev.cmd` launcher with a terminal menu for rebuild, verify, local server, Docker dev, and publish workflows.
- Main `dev.ps1 run` path that builds, verifies, and starts the backend only after checks pass.
- Lightweight `/health` endpoint used by Docker health checks.

### Changed

- Setup, profile, torrent details, and provider links now consistently describe the supported AllDebrid-only workflow.
- Angular packages updated to 21.2.22, Angular CDK to 21.2.14, and Angular ESLint to 21.4.0.
- ASP.NET serves the SPA with built-in static-file and fallback routing instead of the legacy SpaServices package.
- JSON serialization now uses the built-in `System.Text.Json` stack instead of an accidental transitive Newtonsoft dependency.
- Docker images copy the maintained .NET 9 runtime image instead of downloading hard-coded 9.0.0 archives.
- The .NET SDK selector now lives at the repository root and accepts current .NET 9 feature-band updates.
- Local publish output defaults to the ignored `publish/` directory; publishing over a running service is rejected.
- GitHub release and test workflows now enforce frontend linting and formatting; release builds also run backend tests.

### Fixed

- Windows service and s6 launchers now reference the actual `AdbClient.Web` executable and assembly names.
- Nested download-path tests now assert platform-native separators on Windows and Linux.
- The AllDebrid HTTP client now receives the configured timeout and retry policy.
- Docker Desktop is discovered when its CLI is installed outside `PATH`.
- Docker Compose uses the current GHCR image, persistent paths, and health endpoint.
- GitHub URLs now point at the `krkn-dev/alldebrid-client` repository.

### Removed

- Unused Real-Debrid, Premiumize, TorBox, DebridLink, Aria2, Synology, and legacy downloader packages.
- Unused Angular animation/dynamic-bootstrap packages, `curray`, and redundant file-saver typings.
- Dead self-update scripts, obsolete multi-architecture Docker helper, and the one-time standards handoff document.

### Security

- Refreshed the frontend dependency lockfile to pick up patched Angular and build-tool releases.

## [1.1.0] - 2026-05-09

### Added

- Settings: Profile tab for changing login username and password
- `publish.ps1` — single script to build frontend + backend and deploy to a local install path

### Changed

- Docker: runtime base image bumped to Alpine 3.22
- Docker: `tools/docker-compose.yml` image name corrected to `krakal/alldebrid-client`
- DataPath now stamped into `appsettings.json` by `publish.ps1`; data stored under `<install>/data/`

### Fixed

- Downloader: `OutOfMemoryException` caused by chunk size never being applied to `DownloaderNET.Settings`
- Server: `GET /Api/Settings/Profile` returned HTTP 500 when API key was not set; now returns `null`
- Client: settings tabs and navbar (version, premium days) did not render on page refresh until a click triggered change detection
- Client: torrent list did not update from SignalR events because the hub callback ran outside Angular's zone
- CI: Docker image workflow now produces correct semver tags (`latest`, `1`, `1.1`, `1.1.0`)

## [1.0.0] - 2026-05-03

### Added

- Torrent table: filter by name
- Torrent table: sortable columns with direction indicators

### Changed

- Rebranded to AllDebrid Client — new package name `alldebrid-client`, assemblies renamed to `AdbClient.*`
- Database file renamed from `rdtclient.db` to `adbclient.db`
- Settings UI: shorter descriptions, responsive CSS grid layout for compact fields
- Navbar: simplified premium indicator (green/red dot + days remaining)
- Default authentication mode changed to `None`
- Path configuration: `DataPath` moved to `appsettings.json`; Windows defaults retained in settings UI descriptions
- Server: C# type alias standardization throughout
- Server: controller DTOs extracted to `Models/Requests/`
- Server: explicit `Exception` types on all throw statements
- Server: `await using` on `IFormFile` streams

### Removed

- All torrent providers except AllDebrid (removed Real-Debrid, Premiumize, Torbox, etc.)
- qBittorrent download client integration
- Sonarr/Radarr (*arr) API layer
- External download client selector — internal downloader only
- Download client selector from the add-torrent dialog

### Fixed

- Test suite updated for AllDebrid-only provider configuration
- Cross-platform path handling in `RunTorrentComplete` test (was failing on Linux CI)

[unreleased]: https://github.com/krkn-dev/alldebrid-client/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/krkn-dev/alldebrid-client/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/krkn-dev/alldebrid-client/releases/tag/v1.0.0
