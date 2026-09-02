# AllDebrid Client

A self-hosted web interface for managing torrents through [AllDebrid](https://alldebrid.com). Add torrents via magnet link or file, download them to your host automatically, and connect [Logpose](https://github.com/jasanpreetn9/logpose) directly.

Built with Angular 21 and .NET 10 LTS.

---

## Docker Setup

Build and run the container from this checkout:

```bash
docker compose -f tools/docker-compose.yml up -d --build
```

See [README-DOCKER.md](README-DOCKER.md) for the full Docker guide.

---

## Windows Service

1. Install [ASP.NET Core Runtime 10.0](https://dotnet.microsoft.com/download/dotnet/10.0).
2. Download the [latest Windows release](https://github.com/krkn-dev/alldebrid-client/releases/latest) and extract it.
3. In `appsettings.json`, set `DataPath` to a writable persistent directory. Use escaped backslashes, e.g. `D:\\AllDebridClient\\data`.
4. Run `AdbClient.Web.exe` directly, or run `service-install.bat` to install it as a background service.

The application checks the latest completed GitHub release and shows an update notice. It does not replace its own executable or restart its service automatically.

---

## Linux Service

1. Install [.NET 10](https://learn.microsoft.com/dotnet/core/install/linux).
2. Download and extract the latest release archive.
3. In `appsettings.json`, set `DataPath` to a writable persistent directory.
4. Test it runs: `dotnet AdbClient.Web.dll` — browse to `http://<host>:6500`.
5. Create a systemd service:

```ini
[Unit]
Description=AllDebrid Client

[Service]
WorkingDirectory=/opt/alldebrid-client
ExecStart=/usr/bin/dotnet AdbClient.Web.dll
SyslogIdentifier=AllDebridClient
User=<username>

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable alldebrid-client
sudo systemctl start alldebrid-client
```

---

## First-Time Setup

1. Browse to `http://127.0.0.1:6500`.
2. The first credentials you enter become your login.
3. Go to **Settings → AllDebrid** and enter your API key (found at [alldebrid.com/apikeys](https://alldebrid.com/apikeys/)).
4. Set the **Download path** to where you want files saved.
5. Save settings.

---

## Logpose Integration

AllDebrid Client implements the qBittorrent Web API subset used by Logpose. This is a focused Logpose integration, not a general qBittorrent replacement for Sonarr or Radarr.

1. In AllDebrid Client, set **Download path** to the physical shared download directory.
2. Set **Mapped path** to that same directory as Logpose sees it. For containers sharing `/media/downloads`, use `/media/downloads` in both applications.
3. Point Logpose's normal qBittorrent configuration at AllDebrid Client:

```yaml
downloadPath: "/media/downloads"

qbittorrent:
  enabled: true
  host: "http://alldebrid-client:6500/"
  username: "your-alldebrid-client-username"
  password: "your-alldebrid-client-password"
```

Use the AllDebrid Client login when username/password authentication is enabled. When authentication is disabled, the values may be blank. Logpose creates and uses the `logpose` category automatically, and files are downloaded under `<Download path>/logpose`. These jobs use the regular exposed AllDebrid Client download defaults, including file selection, host-download action, filters, retries, finished action, retention, and priority. Logpose's non-destructive post-import callback does not override those settings or remove the AllDebrid Client/provider record; it removes only safe empty staging directories.

---

## Build

**Prerequisites:** Node.js 24, npm, and the .NET 10 SDK. Docker Desktop is optional for container builds. A global Angular CLI install is not required.

For local development on Windows, start with the root launcher:

```powershell
.\dev.ps1
```

It opens a terminal menu where option `1` is the main path: build, verify, then run the backend if checks pass.

Direct commands are also available:

```powershell
.\dev.ps1 rebuild
.\dev.ps1 verify
.\dev.ps1 run
.\dev.ps1 frontend
.\dev.ps1 backend
.\dev.ps1 docker
```

Development commands never install or replace the Windows service. `dev.ps1 publish` writes a framework-dependent Windows build to the ignored `publish/` directory. To create a package elsewhere, pass an explicit path:

```powershell
.\dev.ps1 publish -InstallPath "D:\Apps\AllDebridClient"
```

For an existing service installed with the `<install-root>\App`, `Data`, and `Runtime` layout, use the guarded deployment command from an Administrator PowerShell session:

```powershell
.\deploy.ps1
```

It discovers the service location, builds into a staging directory, preserves `appsettings.json` and `Data`, asks for confirmation, retains the previous `App` directory under `Backups`, and rolls back if the restarted service fails its health check.

## Releases

This project uses Semantic Versioning and Conventional Commits. Verified commits on `main` update a release pull request automatically:

- `fix:` proposes a patch version, such as `1.1.0` to `1.1.1`.
- `feat:` proposes a minor version, such as `1.1.0` to `1.2.0`.
- A breaking change marked with `!` proposes the next major version.

Merging the release pull request creates the `vX.Y.Z` tag, GitHub release, Windows ZIP, and checksum. Docker builds remain reproducible from the same tagged source. Do not create release tags by hand.

---

## Troubleshooting

- **Forgot password:** Delete `adbclient.db` and restart.
- **Logs:** Set log level to `Debug` in Settings. The log file is written to your configured persistent path as `adbclient.log`.
