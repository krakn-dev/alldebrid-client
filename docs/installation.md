# Installation and updates

Docker is the recommended installation method on Linux. A framework-dependent release package is available for Windows, and the application can also run directly with the .NET runtime.

## First-time setup

1. Open `http://127.0.0.1:6500`, or replace `127.0.0.1` with the host address.
2. The first credentials entered become the application login.
3. Open **Settings → AllDebrid** and enter an API key from [alldebrid.com/apikeys](https://alldebrid.com/apikeys/).
4. Under **Settings → Download**, review the local download path and the default download and retention actions. The platform default is usable without editing it.
5. Save the settings before adding a torrent or configuring an integration.

The default authentication mode is **No Authentication**. Enable username and password authentication before exposing the application beyond a trusted network. AllDebrid Client does not provide TLS termination; use a trusted reverse proxy when HTTPS is required.

## Docker

Use the published multi-platform image and persist both `/data/db` and `/data/downloads`. The complete Compose example and update procedure are in the [Docker guide](docker.md).

## Windows service

1. Install the [.NET 10 ASP.NET Core Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).
2. Download and extract the [latest release ZIP](https://github.com/krakn-dev/alldebrid-client/releases/latest) to its permanent location.
3. Test the application by running `AdbClient.Web.exe` and opening `http://127.0.0.1:6500`.
4. To run it in the background, stop the test process and run `service-install.bat` as Administrator.

The installer creates an automatically started `AllDebridClient` Windows service and an inbound firewall rule for the executable. Run `service-remove.bat` as Administrator to remove both.

The default persistent data directory is `C:\ProgramData\AllDebridClient`. To use another location, edit `appsettings.json` before first launch and set `DataPath` to a writable directory. JSON backslashes must be escaped, for example `D:\\AllDebridClient\\Data`.

Keep application files and persistent data in separate directories.

### Updating a Windows service

Double-click `update.cmd` in the application directory. Approve the Windows administrator prompt and confirm the update. The updater:

- accepts stable releases from this repository only;
- verifies the release ZIP against its published SHA-256 checksum and, when available, the digest recorded by GitHub;
- preserves `appsettings.json` and requires persistent data to be outside the application directory;
- stops and restarts the service only when it was already running;
- retains the previous application directory in a sibling backup directory; and
- restores the previous version automatically if the service does not become healthy.

Check for an update without changing the installation:

```powershell
.\update.ps1 -CheckOnly
```

Download, verify, and inspect the latest package without changing the installation:

```powershell
.\update.ps1 -ValidateOnly
```

Running the updater again when the current release is installed makes no changes. Use `-Force` only to reinstall that same release.

Repository maintainers with a checkout and the standard `<install-root>\App`, `Data`, and `Backups` layout can use `deploy.ps1` from an Administrator PowerShell session. It builds into staging, preserves configuration and data, retains the previous application directory, and rolls back when the restarted service fails its health check.

## Native Linux service

Native Linux installations are built from source; use Docker when a prebuilt Linux package is preferred. Install Node.js 24, npm, and the .NET 10 SDK, then build and publish from a checkout:

```bash
npm --prefix client ci
npm --prefix client run build
dotnet restore server
dotnet publish server/AdbClient.Web/AdbClient.Web.csproj \
  --configuration Release \
  --no-restore \
  --output publish
```

Copy the publish output to `/opt/alldebrid-client`. Configure a writable Linux `DataPath` in `appsettings.json`, install the .NET 10 ASP.NET Core Runtime on the target host, then verify the application starts:

```bash
dotnet AdbClient.Web.dll
```

A minimal systemd unit is:

```ini
[Unit]
Description=AllDebrid Client
Wants=network-online.target
After=network-online.target

[Service]
Type=simple
User=alldebrid-client
WorkingDirectory=/opt/alldebrid-client
ExecStart=/usr/bin/dotnet /opt/alldebrid-client/AdbClient.Web.dll
Restart=on-failure

[Install]
WantedBy=multi-user.target
```

Ensure the service user owns the configured data and download directories, then enable and start the unit:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now alldebrid-client
```
