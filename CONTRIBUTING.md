# Contributing

## Local Setup

```powershell
# Frontend (requires Node 22+)
cd client
npm install
npm start          # dev server on :4200, proxies /Api and /hub to :6500

# Backend (requires .NET 9 SDK)
dotnet run --project server/AdbClient.Web
```

## Before Submitting

```powershell
# Run backend tests
dotnet test server

# Run frontend lint
cd client && npm run lint

# Format frontend code
cd client && npm run prettier
```

## Commit Format

```
Area: short imperative phrase
```

Areas: `Server`, `Client`, `Data`, `Tests`, `Docs`, `Repo`, `CI`

Rules: no period at the end, 72-character subject-line limit, one logical change per commit.

## Pull Requests

- Target the `main` branch
- Add an entry to `[Unreleased]` in `CHANGELOG.md`
- Keep PRs focused — one feature or fix per PR

## Versioning

Semantic Versioning (`MAJOR.MINOR.PATCH`). Releases are tagged `vX.Y.Z`; CI builds and publishes automatically from the tag. Never edit `<Version>` in `.csproj` files manually.
