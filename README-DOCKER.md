# Docker

AllDebrid Client runs as a LinuxServer-style container with s6-overlay and persistent `/data` mounts. Versioned multi-platform images are published to [`krakal/alldebrid-client`](https://hub.docker.com/r/krakal/alldebrid-client), and the same source can be built locally.

## Compose

To use the published image:

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

Browse to `http://<host>:6500`.

### Local source build

From the repository root:

```bash
docker compose -f tools/docker-compose.yml up -d --build
```

The local-development Compose service is equivalent to:

```yaml
services:
  alldebrid-client:
    build: .
    image: alldebrid-client:local
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

## Docker CLI

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

## Volumes

| Container path    | Purpose                             |
| ----------------- | ----------------------------------- |
| `/data/db`        | SQLite database, settings, and logs |
| `/data/downloads` | Downloaded files                    |

## Updating

Published releases provide immutable full-version tags and rolling major, minor, and `latest` tags. To update to the latest stable release:

```bash
docker compose pull
docker compose up -d
```

For a local source build, check out the version you want and rebuild the same service:

```bash
git pull --ff-only
docker compose -f tools/docker-compose.yml up -d --build
```

Each image is built from its matching Git release tag for `linux/amd64` and `linux/arm64`. Pin the full version tag, such as `1.5.0`, when reproducibility is more important than automatic stable updates.

## Windows helper

```powershell
.\tools\docker-build-dev.ps1
```
