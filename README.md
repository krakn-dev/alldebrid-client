# AllDebrid Client

[![Build](https://github.com/krakn-dev/alldebrid-client/actions/workflows/release.yml/badge.svg?branch=main)](https://github.com/krakn-dev/alldebrid-client/actions/workflows/release.yml)
[![Latest release](https://img.shields.io/github/v/release/krakn-dev/alldebrid-client)](https://github.com/krakn-dev/alldebrid-client/releases/latest)
[![Docker pulls](https://img.shields.io/docker/pulls/krakal/alldebrid-client)](https://hub.docker.com/r/krakal/alldebrid-client)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

AllDebrid Client is a self-hosted web application for sending torrents to [AllDebrid](https://alldebrid.com), downloading completed files to local storage, and exposing the [qBittorrent](https://www.qbittorrent.org/) Web API surface used by [Sonarr](https://sonarr.tv/), [Radarr](https://radarr.video/), and [Logpose](https://github.com/jasanpreetn9/logpose).

This is an independent community project and is not affiliated with AllDebrid or qBittorrent. An AllDebrid account and API key are required.

## Features

- Add magnet links and `.torrent` files from the web interface, a watch folder, or a compatible application.
- Select files, apply filters, set priorities, and control retry and retention behavior.
- Download completed content to the host with bounded parallel transfers.
- Connect Sonarr, Radarr, and Logpose through their normal qBittorrent configuration.
- Run as a Docker container, Windows service, or framework-dependent .NET application.

## Quick start with Docker

```yaml
services:
  alldebrid-client:
    image: krakal/alldebrid-client:latest
    container_name: alldebrid-client
    environment:
      PUID: 1000
      PGID: 1000
      TZ: Etc/UTC
    volumes:
      - ./data/db:/data/db
      - ./data/downloads:/data/downloads
    ports:
      - "6500:6500"
    restart: unless-stopped
```

Start the container, then open `http://<host>:6500`. The first credentials entered become the application login. Add the AllDebrid API key under **Settings → AllDebrid**, then review the download defaults and paths before adding a torrent.

> [!IMPORTANT]
> Authentication is disabled by default. Enable it before exposing the application beyond a trusted network.

See the [Docker guide](docs/docker.md) for health checks, updates, immutable version tags, and local source builds. Windows and native Linux instructions are in the [installation guide](docs/installation.md).

## Integrations

AllDebrid Client provides a focused qBittorrent-compatible API for download-client integrations; it is not a general qBittorrent daemon or replacement Web UI.

| Application | Connection method           | Default category   |
| ----------- | --------------------------- | ------------------ |
| Sonarr      | qBittorrent download client | `sonarr`           |
| Radarr      | qBittorrent download client | `radarr`           |
| Logpose     | qBittorrent configuration   | Created by Logpose |

Integrated jobs use the same download defaults exposed in AllDebrid Client. **Local download path** is where AllDebrid Client writes files; **Reported download path** is the path returned to the connecting application. They can be identical when both applications see the same filesystem path. A Remote Path Mapping is needed only when the connecting application sees that directory under a different path.

Follow the [integration guide](docs/integrations.md) for exact settings, container path examples, and deletion and retention behavior.

## Documentation

- [Installation and updates](docs/installation.md)
- [Docker](docs/docker.md)
- [Sonarr, Radarr, and Logpose](docs/integrations.md)
- [Contributing and local development](CONTRIBUTING.md)
- [Changelog](CHANGELOG.md)
- [Security policy](SECURITY.md)

## Support

Use [GitHub Issues](https://github.com/krakn-dev/alldebrid-client/issues) for reproducible bugs. Remove API keys, credentials, private links, and local personal data from logs before posting them. Report vulnerabilities privately as described in the [security policy](SECURITY.md).

## License

AllDebrid Client is distributed under the [MIT License](LICENSE).
