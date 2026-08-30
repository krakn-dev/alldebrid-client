param(
    [switch]$SkipNpmCi
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

if ($SkipNpmCi) {
    & (Join-Path $root "dev.ps1") verify -SkipNpmCi
} else {
    & (Join-Path $root "dev.ps1") verify
}
