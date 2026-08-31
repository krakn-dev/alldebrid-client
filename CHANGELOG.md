# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Native Logpose support through the exact qBittorrent Web API operations it uses: SID login, version probing, category creation, magnet and torrent-file adds, progress/path reporting, and post-import cleanup.

### Fixed

- Convert legacy One Pace `https://nyaa.si/?q=<info-hash>` metadata links to canonical magnets instead of attempting to parse Nyaa search HTML as a torrent file.

## [1.2.0](https://github.com/krakn-dev/alldebrid-client/compare/v1.1.0...v1.2.0) (2026-08-31)


### Features

* **platform:** move to .NET 10 and harden runtime ([95d5f2b](https://github.com/krakn-dev/alldebrid-client/commit/95d5f2b7d668858b4095edc330c676bf944ab1fa))
* **release:** automate verified semantic releases ([6773cad](https://github.com/krakn-dev/alldebrid-client/commit/6773cad168be6b3c745a63e436048febc3fa31c8))
* **windows:** add guarded service deployment ([4a83881](https://github.com/krakn-dev/alldebrid-client/commit/4a83881879afc1a3e8f25deebe60500c44fd33ad))


### Bug Fixes

* **client:** repair landing route and release notices ([4b64ccd](https://github.com/krakn-dev/alldebrid-client/commit/4b64ccd689b95b77cf55c394609c8509f45402a3))
* **docker:** use local persistent compose builds ([8a48a2a](https://github.com/krakn-dev/alldebrid-client/commit/8a48a2aeba2fe9d95288618b2b8964ff80348d39))
* **release:** preserve notes and project formatting ([34970e9](https://github.com/krakn-dev/alldebrid-client/commit/34970e95ad28918b3bef18c7f9335e824f54ce48))
* **service:** treat shutdown cancellation normally ([9cf582d](https://github.com/krakn-dev/alldebrid-client/commit/9cf582d68f2cfd3c05af1e4cbfa792169111c558))
* **web:** resolve runtime files from executable ([10901eb](https://github.com/krakn-dev/alldebrid-client/commit/10901ebe23e41430bf5f22fb0cb29c7ae4130fcb))

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

- All torrent providers except AllDebrid
- qBittorrent download client integration
- Sonarr/Radarr (*arr) API layer
- External download client selector — internal downloader only
- Download client selector from the add-torrent dialog

### Fixed

- Test suite updated for AllDebrid-only provider configuration
- Cross-platform path handling in `RunTorrentComplete` test (was failing on Linux CI)

[1.1.0]: https://github.com/krkn-dev/alldebrid-client/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/krkn-dev/alldebrid-client/releases/tag/v1.0.0
