# AllDebrid Client

A self-hosted web interface for managing torrents through [AllDebrid](https://alldebrid.com). Add torrents via magnet link or file, download them to your host automatically, and integrate with Sonarr/Radarr.

Built with Angular 21 and .NET 9. Forked from [rogerfar/rdt-client](https://github.com/rogerfar/rdt-client) at v2.0.116 and reduced to an AllDebrid-only client.

---

## Docker Setup

Images are published to both Docker Hub and GitHub Container Registry on every tagged release:

- Docker Hub: [`lekrakin/alldebrid-client`](https://hub.docker.com/r/lekrakin/alldebrid-client)
- GHCR: [`ghcr.io/krkn-dev/alldebrid-client`](https://github.com/krkn-dev/alldebrid-client/pkgs/container/alldebrid-client)

```bash
docker pull lekrakin/alldebrid-client:latest
# or
docker pull ghcr.io/krkn-dev/alldebrid-client:latest
```

See [README-DOCKER.md](README-DOCKER.md) for the full Docker guide.

---

## Windows Service

1. Install [ASP.NET Core Runtime 9.0](https://dotnet.microsoft.com/download/dotnet/9.0).
2. Download the latest release zip and extract it.
3. In `appsettings.json`, set `DataPath` to a writable persistent directory. Use escaped backslashes, e.g. `D:\\AllDebridClient\\data`.
4. Run `AdbClient.Web.exe` directly, or run `service-install.bat` to install it as a background service.

---

## Linux Service

1. Install [.NET 9](https://docs.microsoft.com/en-us/dotnet/core/install/linux).
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

## Sonarr / Radarr Integration

AllDebrid Client emulates the qBittorrent web API, so Sonarr and Radarr connect to it natively.

1. In Sonarr/Radarr go to **Settings → Download Clients → Add → qBittorrent**.
2. Set **Host** to your server IP, **Port** to `6500`.
3. Enter your username and password.
4. Set **Category** to `sonarr` or `radarr`.
5. Hit **Test** then **Save**.

Files download to a subfolder named after the category under your configured download path.

---

## Build

**Prerequisites:** Node.js 20.19+, 22.12+, or 24+; npm; and the .NET 9 SDK. Docker Desktop is optional for container builds. A global Angular CLI install is not required.

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

`dev.ps1 publish` writes a framework-dependent Windows build to the ignored `publish/` directory. To publish elsewhere, pass an explicit path:

```powershell
.\dev.ps1 publish -InstallPath "D:\Apps\AllDebridClient"
```

---

## Troubleshooting

- **Forgot password:** Delete `adbclient.db` and restart.
- **Logs:** Set log level to `Debug` in Settings. The log file is written to your configured persistent path as `adbclient.log`.
