# Changelog

<!-- Versioning: UPSTREAM_MAJOR.UPSTREAM_MINOR.UPSTREAM_PATCH.FORK_REVISION
     e.g. 2.0.116.1, 2.0.116.2 … reset fork revision to 1 on each upstream sync. -->

## [Unreleased]

---

## [2.0.116.1] - 2026-05-02

### Changed
- Torrent table: filter by name, sortable columns with direction indicators
- Settings: shorter descriptions, responsive CSS grid layout (compact numeric/bool fields)
- Navbar: simplified premium indicator — green/red dot + days remaining
- Server: C# type alias standardization throughout
- Server: controller DTOs extracted to `Models/Requests/`
- Server: explicit `Exception` types on all throw statements
- Server: `await using` on `IFormFile` streams
- Rebranded as AllDebrid Client (`Adb.Client.*` assemblies)

---

## [2.0.116] - 2025-08-04 (upstream base)

### Added
- Setting to ban certain trackers from being added.

### Changed
- Upgraded to Angular 20.

---

Forked from [rogerfar/rdt-client](https://github.com/rogerfar/rdt-client) at v2.0.116.
Prior upstream history: https://github.com/rogerfar/rdt-client/blob/main/CHANGELOG.md
