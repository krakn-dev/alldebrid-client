param(
    [string]$InstallPath = "G:\Programs\adbclient\AllDebridClient"
)

$ErrorActionPreference = "Stop"
$PSNativeCommandErrorActionPreference = "Stop"
$root = $PSScriptRoot
$dataPath = Join-Path $InstallPath "data"
$settingsPath = Join-Path $InstallPath "appsettings.json"

Write-Host "==> Building frontend"
Push-Location (Join-Path $root "client")
npm run build
Pop-Location

Write-Host "==> Cleaning install directory"
if (Test-Path $InstallPath) {
    Get-ChildItem $InstallPath -Exclude "data" | Remove-Item -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $InstallPath | Out-Null
}

Write-Host "==> Publishing to $InstallPath"
dotnet publish (Join-Path $root "server\AdbClient.Web\AdbClient.Web.csproj") -c Release -o $InstallPath

Write-Host "==> Setting data path to $dataPath"
$settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
$settings.DataPath = $dataPath
$settings | ConvertTo-Json | Set-Content $settingsPath

New-Item -ItemType Directory -Path $dataPath -Force | Out-Null

Write-Host "==> Done: $InstallPath"
