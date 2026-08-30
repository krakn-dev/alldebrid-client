param(
    [string]$InstallPath
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true
$root = $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($InstallPath)) {
    $InstallPath = Join-Path $root "publish"
}

$InstallPath = [System.IO.Path]::GetFullPath($InstallPath)
$projectRoot = [System.IO.Path]::GetFullPath($root).TrimEnd('\', '/')
$driveRoot = [System.IO.Path]::GetPathRoot($InstallPath).TrimEnd('\', '/')
$normalizedInstallPath = $InstallPath.TrimEnd('\', '/')

if ($normalizedInstallPath -ieq $projectRoot -or $normalizedInstallPath -ieq $driveRoot) {
    throw "Refusing to publish over the project root or a drive root: $InstallPath"
}

$service = Get-CimInstance Win32_Service -Filter "Name='AllDebridClient'" -ErrorAction SilentlyContinue
if ($service.State -eq "Running" -and $service.PathName -like "*$normalizedInstallPath*") {
    throw "AllDebridClient is running from $InstallPath. Stop the service before publishing over its files."
}

$dataPath = Join-Path $InstallPath "data"
$settingsPath = Join-Path $InstallPath "appsettings.json"

Write-Host "==> Building frontend"
Push-Location (Join-Path $root "client")
try {
    npm run build
    if ($LASTEXITCODE -ne 0) {
        throw "Frontend build failed with exit code $LASTEXITCODE."
    }
} finally {
    Pop-Location
}

Write-Host "==> Cleaning install directory"
if (Test-Path $InstallPath) {
    Get-ChildItem -LiteralPath $InstallPath |
        Where-Object { $_.Name -ne "data" } |
        Remove-Item -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $InstallPath | Out-Null
}

Write-Host "==> Publishing to $InstallPath"
dotnet publish (Join-Path $root "server\AdbClient.Web\AdbClient.Web.csproj") -c Release -o $InstallPath
if ($LASTEXITCODE -ne 0) {
    throw "Backend publish failed with exit code $LASTEXITCODE."
}

Write-Host "==> Setting data path to $dataPath"
$settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
$settings.DataPath = $dataPath
$settings | ConvertTo-Json | Set-Content $settingsPath

New-Item -ItemType Directory -Path $dataPath -Force | Out-Null

Write-Host "==> Done: $InstallPath"
