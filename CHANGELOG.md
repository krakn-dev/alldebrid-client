# Changelog

## [Unreleased]

### Changed
- Torrent table: filter by name, sortable columns with direction indicators
- Settings: shorter descriptions, responsive CSS grid layout (compact numeric/bool fields)
- Navbar: simplified premium indicator — green/red dot + days remaining
- Server: C# type alias standardization throughout
- Server: controller DTOs extracted to `Models/Requests/`
- Server: explicit `Exception` types on all throw statements
- Server: `await using` on `IFormFile` streams

---

## [2.0.116] - 2025-08-04 (upstream base)

### Added
- Setting to ban certain trackers from being added.

### Changed
- Upgraded to Angular 20.

---

Forked from [rogerfar/rdt-client](https://github.com/rogerfar/rdt-client) at v2.0.116.
Prior upstream history: https://github.com/rogerfar/rdt-client/blob/main/CHANGELOG.md
