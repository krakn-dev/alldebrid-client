# Sonarr, Radarr, and Logpose

AllDebrid Client implements the qBittorrent Web API surface required by Sonarr, Radarr, and Logpose. It accepts magnets and uploaded `.torrent` files, reports progress and paths, and supports the category operations these applications use. It is not a general qBittorrent daemon or replacement Web UI.

## Shared AllDebrid Client settings

Configure these under **Settings → Download** before connecting another application:

- **Local download path** is the physical directory where AllDebrid Client writes files.
- **Reported download path** is the path returned through the qBittorrent API. Leave it blank to report the local path.
- **Post torrent download action** must download files to the host when another application needs to import them.
- **Completed record action** controls whether completed AllDebrid Client and provider records are retained. It does not prevent an external client from explicitly removing a successfully imported local payload.

Jobs added through the qBittorrent API inherit the regular exposed download defaults, including file selection, filters, retries, priority, finished action, and retention. Each category receives its own directory under the local download path. Direct integrations do not use the torrent-blackhole watch folder and do not create loose magnet or `.torrent` files unless **Copy added torrent files** is configured.

Use the AllDebrid Client login when username and password authentication is enabled. Leave client credentials blank when authentication is disabled.

## Paths and containers

No Remote Path Mapping is needed when AllDebrid Client and the connecting application see the download directory under the same path.

When they see different paths, set **Reported download path** to the path returned to the connecting application and configure that application's Remote Path Mapping to its accessible local path. The mapping host must exactly match the host configured on the download client.

For example, if AllDebrid Client writes to `D:\Programs\AllDebridClient\Data\downloads` but a container sees that directory as `/media/downloads`:

- AllDebrid Client **Local download path**: `D:\Programs\AllDebridClient\Data\downloads`
- AllDebrid Client **Reported download path**: `/media/downloads`
- Sonarr or Radarr mapping remote path: `/media/downloads`
- Sonarr or Radarr mapping local path: the path of the same mount inside that container

## Sonarr and Radarr

Add AllDebrid Client as a qBittorrent download client with the following values:

| Setting        | Value                                                    |
| -------------- | -------------------------------------------------------- |
| Host           | AllDebrid Client host name or address                    |
| Port           | `6500` unless changed in `appsettings.json`              |
| URL base       | Blank unless AllDebrid Client has a base path configured |
| Category       | `sonarr` or `radarr`                                     |
| Initial state  | Started                                                  |
| Content layout | Default                                                  |

Leave sequential download, first/last-piece priority, and post-import category options disabled or blank. Test the client before removing an existing download client.

Enable completed-download removal when successfully imported source data should be cleaned up. AllDebrid Client still applies the job's configured **Completed record action** to its own and the provider's records. Failed-download removal is independent and can remain disabled when failures should stay visible for diagnosis.

Sonarr and Radarr normally hardlink torrent payloads into their libraries when the download and library directories are on the same filesystem. When they are on different volumes, hardlinks are impossible and the application copies the files; completed-download removal then removes the source after a successful import.

## Logpose

Set **Reported download path** to the same directory as Logpose sees it. For containers sharing `/media/downloads`, use `/media/downloads` in both applications.

Point Logpose's qBittorrent configuration at AllDebrid Client:

```yaml
downloadPath: "/media/downloads"

qbittorrent:
  enabled: true
  host: "http://alldebrid-client:6500/"
  username: "your-alldebrid-client-username"
  password: "your-alldebrid-client-password"
```

Logpose creates and uses its category automatically, and files are downloaded beneath `<Local download path>/logpose`. After importing, Logpose's `deleteFiles=false` callback moves the job out of its active category while leaving the AllDebrid Client and provider records under the configured retention policy. Only safe empty staging directories are removed.

## Deletion behavior

External applications complete their workflows by calling the qBittorrent delete endpoint. AllDebrid Client handles that request according to both the request and the job's **Completed record action**:

- `deleteFiles=false` does not delete the local payload.
- `deleteFiles=true` permits removal of the imported local payload.
- **No Action** retains the AllDebrid Client and provider records after any requested local-file cleanup.
- Other completed-record actions remove only the records selected by that setting.

This separation lets an external application clean its imported source without silently overriding the configured provider and history retention policy.
