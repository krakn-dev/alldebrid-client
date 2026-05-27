# Standards Handoff — AllDebrid Client

*One-time technical audit. All items below are actionable next steps with specific commands.*

---

## What Was Done in This Pass

| Change | Rationale |
|--------|-----------|
| `server/global.json` | Pins .NET SDK to 9.0.x; prevents silent upgrade to .NET 10 on developer machines and CI runners |
| `server/Directory.Build.props` | Removes 4-way duplication of `TargetFramework`, `Nullable`, `ImplicitUsings`, `LangVersion` across all .csproj files |
| Root `.editorconfig` | Enforces C# code style (4-space indent, brace placement, import ordering) for editors and Roslyn |
| `client/.prettierrc.json` | Makes Prettier config explicit rather than relying on `.editorconfig` inference |
| `client/.prettierignore` | Adds `package-lock.json` and `dist` — prevents accidental formatting of auto-generated files |
| `.github/dependabot.yml` | Weekly automated PRs for NuGet, npm, and GitHub Actions version bumps |
| CI `dotnet-test.yml` | Added parallel `lint` job (ESLint via `ng lint`); added `--collect:"XPlat Code Coverage"` to test step |
| CI `build-release.yaml` | Bumped `actions/setup-dotnet` from `@v3` to `@v4`; normalized `dotnet-version` to `9.0.x` |
| `Dockerfile` HEALTHCHECK | Tightened `--timeout` to 10s (was 30s); extended `--start-period` to 60s for .NET warm-up |
| `SECURITY.md` | GitHub security policy; enables private vulnerability reporting via GitHub Advisories |
| `CONTRIBUTING.md` | Standard GitHub contribution guide with setup steps and commit rules |

---

## Remaining Work — Priority Order

### P1 — TypeScript `strictNullChecks` (High impact, Moderate effort ~4h)

**Problem:** `client/tsconfig.json` has `"strictNullChecks": false` despite `"strict": true`. This silently lets `undefined`/`null` flow through typed values, negating TypeScript's primary safety guarantee.

**How to fix:**
1. In `client/tsconfig.json`, remove the line `"strictNullChecks": false`
2. Run `cd client && npx tsc --noEmit` to see all errors
3. Fix each error — most will require adding `| null`, `?.`, or non-null assertions (`!`)

Fixing this is high-leverage: it will surface real null-safety bugs and prevent future regressions.

---

### P2 — Prettier enforcement in CI (Low effort ~30min)

**Problem:** Prettier is installed and configured but not enforced in CI. Code can be merged with inconsistent formatting.

**How to fix — two steps:**

Step 1: Format existing code and commit it (do this first, otherwise CI fails immediately):
```bash
cd client
npm run prettier
git add -A
git commit -m "Client: apply prettier formatting"
```

Step 2: Add the check to the `lint` job in `.github/workflows/dotnet-test.yml`:
```yaml
      - name: Check formatting
        working-directory: client
        run: npx prettier --check "./**/*.{ts,html,json}"
```

---

### P3 — Frontend tests (High impact, High effort ~1–2 days)

**Problem:** No frontend tests exist. `skipTests: true` is in Angular schematics. CI only validates the backend.

**Recommended approach (Jest + Angular Testing Library):**
```bash
cd client
npm install --save-dev jest jest-preset-angular @testing-library/angular @testing-library/user-event @types/jest
```

Create `client/jest.config.js`:
```js
module.exports = {
  preset: 'jest-preset-angular',
  setupFilesAfterEach: ['<rootDir>/setup-jest.ts'],
};
```

Create `client/setup-jest.ts`:
```ts
import 'jest-preset-angular/setup-jest';
```

Add to `client/package.json` scripts:
```json
"test": "jest --passWithNoTests"
```

Minimum test targets (start here):
- `auth.service.ts` — login/logout state transitions
- `torrent.service.ts` — SignalR connection lifecycle, message dispatching
- `settings.service.ts` — settings load/save round-trip
- `torrent-table/` component — filter and sort logic

Add to the `lint` CI job:
```yaml
      - name: Test frontend
        working-directory: client
        run: npm test -- --ci --coverage
```

---

### P4 — Angular ESLint version alignment (Low effort ~15min)

**Problem:** `@angular-eslint/*` packages are at version `19.4.0` while Angular itself is at `21.x`. Angular ESLint should track the Angular major version.

**How to fix:**
```bash
cd client
ng add @angular-eslint/schematics
```

This runs the migration schematic and upgrades `@angular-eslint` to v21. Review any rule changes after the upgrade.

---

### P5 — SignalR client version bump (Low effort ~15min + testing)

**Problem:** `@microsoft/signalr` is at `8.0.7` (targeting .NET 8) while the server runs .NET 9. The correct version is `^9.0.0`.

**How to fix:**
```bash
cd client
npm install @microsoft/signalr@^9.0.0
```

After upgrading, manually test the real-time torrent status updates (add a torrent and verify the table updates without a page refresh).

---

### P6 — Code coverage reporting (Low effort ~1h)

**Problem:** `coverlet.collector` is installed and CI now collects coverage data into XML files, but they're not uploaded or tracked over time.

**How to fix (Coveralls — free for open source):**

Add to `.github/workflows/dotnet-test.yml` in the `build` job, after the Test step:
```yaml
      - name: Upload coverage to Coveralls
        uses: coverallsapp/github-action@v2
        with:
          github-token: ${{ secrets.GITHUB_TOKEN }}
          format: cobertura
          files: '**/coverage.cobertura.xml'
```

Set a baseline coverage threshold once you know current coverage levels.

---

### P7 — Orphaned package references in Service project (Medium effort ~1h)

**Problem:** `AdbClient.Service.csproj` still references NuGet packages for providers that were removed in v1.0.0 per the CHANGELOG:
- `TorBox.NET`
- `RD.NET`
- `Premiumize.NET`
- `DebridLinkFr.NET`
- `Synology.Api.Client`

These inflate build time, binary size, and the dependency attack surface.

**How to verify and fix:**
```bash
# Check for actual usages in the service project
grep -rn "TorBox\|Premiumize\|RealDebrid\|DebridLink\|Synology" server/AdbClient.Service/

# Also check for Downloader.NET vs Downloader (verify which is actually used)
grep -rn "Downloader" server/AdbClient.Service/
```

Remove each `<PackageReference>` that has zero usages. Run `dotnet build server` and `dotnet test server` to confirm nothing breaks.

---

### P8 — Health check API endpoint (Medium effort ~2h)

**Problem:** The Dockerfile HEALTHCHECK hits `/` (the Angular SPA root), which only tests that the static file server is alive — not the database, background services, or API layer.

**How to fix (ASP.NET Core built-in health checks):**

In `server/AdbClient.Web/Program.cs`:
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DbContext>("database");

// After app.UseRouting() or app.MapControllers():
app.MapHealthChecks("/health");
```

Then update `Dockerfile`:
```
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:6500/health || exit 1
```

Optionally add detailed health output with `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`.

---

### P9 — `aspNetCore.SpaServices.Extensions` deprecation (Medium effort ~2h)

**Problem:** `Microsoft.AspNetCore.SpaServices.Extensions` is marked as legacy/obsolete and will be removed in a future .NET release. It's unnecessary for production since Angular builds to `wwwroot/`.

**How to fix:** Remove `SpaServices.Extensions` from `AdbClient.Web.csproj` and any middleware registration in `Program.cs`. Replace with:
```csharp
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
```

The `client/angular-proxy.json` already handles dev server proxying — no changes needed for the dev workflow.

---

### P10 — Docker workflow: GHCR multi-arch manifest (Low effort ~30min)

**Problem:** `build-docker-image.yml` creates a multi-arch manifest for DockerHub but not for GHCR. GHCR ends up with only a single-arch image per push.

**How to fix:** In the `Create manifest list and push` step, add an equivalent `docker buildx imagetools create` call for `ghcr.io/...` images using the digests from both build jobs.

---

### P11 — `tsconfig.json` deprecation suppression (Low effort ~30min)

**Problem:** `"ignoreDeprecations": "5.0"` in `client/tsconfig.json` silences a TypeScript 5.0 transitional warning. It should be removed once the underlying code is updated.

**How to fix:** Remove the line, run `npx tsc --noEmit`, and fix any decorator-related warnings. Angular 21 supports the TC39 decorator standard — if `experimentalDecorators: true` is still set, it can likely be removed as well (Angular's own `@Component`, `@Injectable`, etc. use standard decorators in recent versions).

---

### P12 — `dotnet format` enforcement in CI (Low effort ~30min)

**Problem:** The `build` CI job doesn't check C# code formatting. Inconsistently formatted code can be merged.

**How to fix:** Add to `.github/workflows/dotnet-test.yml` in the `build` job, before the Test step:
```yaml
      - name: Check formatting
        run: dotnet format --verify-no-changes server
```

Note: Run `dotnet format server` locally first and commit the result, otherwise CI will fail on pre-existing formatting issues.

---

## Conventions to Maintain

- **Commits:** `Area: short imperative phrase` — no period, 72-char limit, one logical change
- **Versions:** `MAJOR.MINOR.PATCH` from git tags only — never edit `.csproj` `<Version>` manually
- **CHANGELOG:** Update `[Unreleased]` for every merged PR before the PR is merged
- **Naming:** No `Rdt`, `RDT`, `rdt` prefixes in new code — always `Adb`/`adb`/`AdbClient`
- **Architecture:** Web → Service → Data layering — controllers stay thin (parse + delegate), logic lives in Service
- **Tests:** Use `TestableIO` abstractions (`IFileSystem`, `IDirectory`, etc.) — never `System.IO` directly in tested code
- **Directory.Build.props:** Add new .NET projects to `server/` without repeating `TargetFramework`, `Nullable`, `ImplicitUsings`, or `LangVersion` — they're inherited automatically
