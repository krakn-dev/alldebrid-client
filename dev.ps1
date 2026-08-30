param(
    [string]$Command = "menu",
    [string]$InstallPath,
    [switch]$SkipNpmCi,
    [switch]$SkipCache
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$tool = Join-Path $root "tools\dev.ps1"
$toolParams = @{
    Command = $Command
}

if ($PSBoundParameters.ContainsKey("InstallPath")) {
    $toolParams.InstallPath = $InstallPath
}

if ($SkipNpmCi) {
    $toolParams.SkipNpmCi = $true
}

if ($SkipCache) {
    $toolParams.SkipCache = $true
}

try {
    & $tool @toolParams
}
catch {
    Write-Host ""
    Write-Host "Dev command did not complete." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Run '.\dev.ps1 info' to check prerequisites and local paths."
    exit 1
}
