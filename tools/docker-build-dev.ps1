#!/usr/bin/env pwsh

param(
    [string]$TempPath="c:/Temp/AdbClient",
    [string]$Dockerfile="Dockerfile",
    [string]$DockerPath="docker",
    [switch]$AutoAttach,
    [switch]$SkipCache,
    [string]$BuildProgress="auto"
)

[string] $downloadPath = Join-Path -Path $TempPath -ChildPath "downloads"
[string] $dbPath = Join-Path -Path $TempPath -ChildPath "db"

if (Test-Path -LiteralPath $DockerPath) {
    $dockerDirectory = Split-Path -Parent $DockerPath
    $env:Path = "$dockerDirectory;$env:Path"
}

New-Item -ItemType Directory -Path $downloadPath -Force | Out-Null
New-Item -ItemType Directory -Path $dbPath -Force | Out-Null

$existingContainer = & $DockerPath ps --all --filter "name=^adbclientdev$" --format "{{.Names}}"
if ($existingContainer -contains "adbclientdev") {
    Write-Host "Stopping existing container"
    & $DockerPath stop adbclientdev
    if ($LASTEXITCODE -ne 0) { throw "Unable to stop adbclientdev." }

    Write-Host "Removing existing container"
    & $DockerPath rm adbclientdev
    if ($LASTEXITCODE -ne 0) { throw "Unable to remove adbclientdev." }
}

Write-Host "Building Container"
$dockerArgs = @( "build", "--force-rm", "--tag", "adbclientdev", "--progress=$BuildProgress", "--file", $Dockerfile, "." )
if ($SkipCache.IsPresent) { $dockerArgs += @("--no-cache" ) }

& $DockerPath $dockerArgs
if ($LASTEXITCODE -ne 0) { throw "Docker build failed with exit code $LASTEXITCODE." }

Write-Host "Starting Container"
& $DockerPath run --cap-add=NET_ADMIN -d -v "${downloadPath}:/data/downloads" -v "${dbPath}:/data/db" --log-driver json-file --log-opt max-size=10m -p 6500:6500 --name adbclientdev adbclientdev
if ($LASTEXITCODE -ne 0) { throw "Docker run failed with exit code $LASTEXITCODE." }

if ($AutoAttach.IsPresent) {
    & $DockerPath exec -it adbclientdev /bin/bash
}
