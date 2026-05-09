param(
    [string]$InstallPath = "G:\Programs\adbclient\AllDebridClient"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

# Back up user config so dotnet publish doesn't overwrite it
$settingsPath = Join-Path $InstallPath "appsettings.json"
$settingsBackup = if (Test-Path $settingsPath) { Get-Content $settingsPath -Raw } else { $null }

Write-Host "==> Building frontend"
Push-Location (Join-Path $root "client")
npm run build
Pop-Location

Write-Host "==> Publishing to $InstallPath"
dotnet publish (Join-Path $root "server\AdbClient.Web\AdbClient.Web.csproj") -c Release -o $InstallPath

if ($settingsBackup) {
    Set-Content -Path $settingsPath -Value $settingsBackup -NoNewline
    Write-Host "==> Restored appsettings.json"
}

Write-Host "==> Done: $InstallPath"
