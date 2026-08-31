# Contributing

## Local setup

Prerequisites are Node.js 24, npm, and the .NET 10 SDK.

```powershell
.\dev.ps1

# Or run each side directly:
cd client
npm ci
npm start

dotnet run --project server/AdbClient.Web
```

The Angular development server listens on port 4200 and proxies API and SignalR requests to the backend on port 6500.

## Verification

Run the complete local check before opening a pull request:

```powershell
.\dev.ps1 verify
```

Continuous integration repeats the backend build and tests, frontend lint/format/build/audit, and an `amd64` container build.

## Commits and pull requests

Use [Conventional Commits](https://www.conventionalcommits.org/):

```text
fix(downloads): handle an interrupted transfer
feat(settings): add a configurable retry limit
docs: clarify the Docker volume layout
feat(api)!: remove a deprecated endpoint
```

Keep each commit focused. Use the body to explain non-obvious behavior or migration details. Pull requests target `main` and must pass CI.

## Versions and releases

Versions follow Semantic Versioning:

- `fix:` increments the patch version.
- `feat:` increments the minor version.
- A `!` or `BREAKING CHANGE:` footer increments the major version.
- Documentation, tests, refactors, build, CI, and chores do not create a release unless they include a breaking change.

Release Please maintains a release pull request from commits merged since the latest release. That pull request updates `CHANGELOG.md`, `version.txt`, the .NET assembly version, frontend package metadata, and Docker defaults together. Merging it creates the Git tag and GitHub release; the same verified workflow then uploads the Windows package and checksum.

Do not manually edit managed version fields or push release tags.
