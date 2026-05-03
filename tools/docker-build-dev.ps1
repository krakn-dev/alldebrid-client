#!/usr/bin/env pwsh

param(
    [string]$TempPath="c:/Temp/AdbClient",
    [string]$Dockerfile="Dockerfile",
    [switch]$AutoAttach,
    [switch]$SkipCache,
    [string]$BuildProgress="auto"
)

[string] $downloadPath = Join-Path -Path $TempPath -ChildPath "downloads"
[string] $dbPath = Join-Path -Path $TempPath -ChildPath "db"

Write-Host "Stopping Container (if already running)"
docker stop adbclientdev

Write-Host "removing Container (if exists)"
docker rm adbclientdev

Write-Host "Building Container"
$dockerArgs = @( "build", "--force-rm", "--tag", "adbclientdev", "--progress=$BuildProgress", "--file", $Dockerfile, "." )
if ($SkipCache.IsPresent) { $dockerArgs += @("--no-cache" ) }

& docker $dockerArgs

Write-Host "Starting Container"
& docker run --cap-add=NET_ADMIN -d -v ${$downloadPath}:/data/downloads -v ${$dbPath}:/data/db --log-driver json-file --log-opt max-size=10m -p 6500:6500 --name adbclientdev adbclientdev

if ($AutoAttach.IsPresent) {
    docker exec -it adbclientdev /bin/bash
}
