# Docker

AllDebrid Client ships as a LinuxServer-style container with s6-overlay and persistent `/data` mounts.

Published images:

- Docker Hub: `lekrakin/alldebrid-client`
- GHCR: `ghcr.io/krkn-dev/alldebrid-client`

## Compose

```yaml
services:
  alldebrid-client:
    image: lekrakin/alldebrid-client:latest
    container_name: alldebrid-client
    environment:
      - PUID=1000
      - PGID=1000
      - TZ=Etc/UTC
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
docker run -d \
  --name alldebrid-client \
  -e PUID=1000 \
  -e PGID=1000 \
  -e TZ=Etc/UTC \
  -p 6500:6500 \
  -v ./data/db:/data/db \
  -v ./data/downloads:/data/downloads \
  --restart unless-stopped \
  lekrakin/alldebrid-client:latest
```

Browse to `http://<host>:6500`.

## Volumes

| Container path | Purpose |
| --- | --- |
| `/data/db` | SQLite database, settings, logs |
| `/data/downloads` | Downloaded files |

## Updating

```bash
docker compose pull
docker compose up -d
docker image prune
```

For a single container created with `docker run`:

```bash
docker pull lekrakin/alldebrid-client:latest
docker stop alldebrid-client
docker rm alldebrid-client
# Re-run the original docker run command with the same volume mounts.
```

## Local Image Build

```powershell
.\tools\docker-build-dev.ps1
```

For multi-arch release builds, use the GitHub Actions release workflow. It builds Docker Hub and GHCR images from `vX.Y.Z` tags.
