# Docker

Stable multi-platform images are published to [`krakal/alldebrid-client`](https://hub.docker.com/r/krakal/alldebrid-client) for `linux/amd64` and `linux/arm64`.

## Docker Compose

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
    healthcheck:
      test: ["CMD", "curl", "--fail", "http://localhost:6500/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
```

Run `docker compose up -d`, then open `http://<host>:6500`.

`PUID` and `PGID` should identify the host user that owns the mounted directories. Set `TZ` to an [IANA time zone](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones), such as `America/New_York`.

## Persistent storage

| Container path    | Purpose                             |
| ----------------- | ----------------------------------- |
| `/data/db`        | SQLite database, settings, and logs |
| `/data/downloads` | Downloaded files                    |

Both paths must use persistent mounts. Back up `/data/db` before replacing or migrating an installation.

For integrations running in containers, mount the same host download directory into each container. Using `/data/downloads` in AllDebrid Client and `/media/downloads` in another container is valid, but the **Reported download path** and any Remote Path Mapping must describe that difference. See the [integration guide](integrations.md).

## Updating

```bash
docker compose pull
docker compose up -d
```

Releases publish immutable full-version tags plus rolling major, minor, and `latest` tags. Pin a full tag such as `1.5.2` when reproducibility is more important than following stable updates automatically.

Every release image is built from its matching Git tag. Release images include provenance and a software bill of materials.

## Local source build

From the repository root:

```bash
docker compose -f tools/docker-compose.yml up -d --build
```

On Windows, the project launcher provides the same operation:

```powershell
.\dev.ps1 docker
```

The development Compose file stores data under the ignored `data/` directory in the repository and tags the image as `alldebrid-client:local`.

To rebuild without Docker's layer cache:

```powershell
.\dev.ps1 docker -SkipCache
```

## Docker CLI

Compose is recommended because it records the complete configuration. The equivalent direct commands are:

```bash
docker build -t alldebrid-client:local .

docker run -d \
  --name alldebrid-client \
  -e PUID=1000 \
  -e PGID=1000 \
  -e TZ=Etc/UTC \
  -p 6500:6500 \
  -v ./data/db:/data/db \
  -v ./data/downloads:/data/downloads \
  --restart unless-stopped \
  alldebrid-client:local
```
