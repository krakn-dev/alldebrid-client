# Docker

AllDebrid Client runs as a LinuxServer-style container with s6-overlay and persistent `/data` mounts. Images are built locally from the versioned source, so no external container account is required.

## Compose

From the repository root:

```bash
docker compose -f tools/docker-compose.yml up -d --build
```

The equivalent Compose service is:

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

Browse to `http://<host>:6500`.

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

Check out the version you want, then rebuild the same service:

```bash
git pull --ff-only
docker compose -f tools/docker-compose.yml up -d --build
```

Release tags identify exact reproducible source versions. The release workflow publishes the Windows package; it does not push containers to Docker Hub or another registry.

## Windows helper

```powershell
.\tools\docker-build-dev.ps1
```
