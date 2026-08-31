[CmdletBinding(SupportsShouldProcess, ConfirmImpact = "High")]
param(
    [string]$InstallRoot,
    [ValidatePattern('^[A-Za-z0-9_.-]+$')]
    [string]$ServiceName = "AllDebridClient",
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true
$projectRoot = [System.IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\', '/')

function Get-ServiceApplicationPath([string]$PathName) {
    $tokens = [regex]::Matches($PathName, '"([^"]+)"|(\S+)') | ForEach-Object {
        if ($_.Groups[1].Success) { $_.Groups[1].Value } else { $_.Groups[2].Value }
    }

    return $tokens | Where-Object {
        $_ -match '(?i)AdbClient\.Web\.(dll|exe)$'
    } | Select-Object -First 1
}

function Assert-ChildPath([string]$Parent, [string]$Child) {
    $prefix = $Parent.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if (-not $Child.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside $Parent`: $Child"
    }
}

function Wait-ForHealth([int]$Port) {
    $deadline = [DateTime]::UtcNow.AddSeconds(45)
    $uri = "http://127.0.0.1:$Port/health"

    do {
        try {
            $response = Invoke-WebRequest -Uri $uri -TimeoutSec 5 -UseBasicParsing
            if ($response.StatusCode -eq 200) {
                return
            }
        } catch {
            Start-Sleep -Seconds 2
        }
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "The service did not become healthy at $uri within 45 seconds."
}

$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run this deployment from an Administrator PowerShell session."
}

$service = Get-CimInstance Win32_Service -Filter "Name='$ServiceName'" -ErrorAction SilentlyContinue
if ($null -eq $service) {
    throw "Windows service '$ServiceName' was not found. Install it before using the in-place deployment command."
}

$serviceApplicationPath = Get-ServiceApplicationPath $service.PathName
if ([string]::IsNullOrWhiteSpace($serviceApplicationPath)) {
    throw "Could not identify AdbClient.Web.dll or AdbClient.Web.exe in the service command line."
}

$serviceApplicationPath = [System.IO.Path]::GetFullPath($serviceApplicationPath)
$serviceAppDirectory = Split-Path -Parent $serviceApplicationPath

if ([string]::IsNullOrWhiteSpace($InstallRoot)) {
    if ((Split-Path -Leaf $serviceAppDirectory) -ne "App") {
        throw "The service is not using the expected <install-root>\App layout. Pass -InstallRoot explicitly after verifying the installation."
    }

    $InstallRoot = Split-Path -Parent $serviceAppDirectory
}

$InstallRoot = [System.IO.Path]::GetFullPath($InstallRoot).TrimEnd('\', '/')
$driveRoot = [System.IO.Path]::GetPathRoot($InstallRoot).TrimEnd('\', '/')
if ($InstallRoot -ieq $driveRoot -or $InstallRoot -ieq $projectRoot) {
    throw "Refusing to deploy to a drive root or the project root: $InstallRoot"
}

$appDirectory = [System.IO.Path]::GetFullPath((Join-Path $InstallRoot "App"))
$dataDirectory = [System.IO.Path]::GetFullPath((Join-Path $InstallRoot "Data"))
$backupsDirectory = [System.IO.Path]::GetFullPath((Join-Path $InstallRoot "Backups"))
$stagingDirectory = [System.IO.Path]::GetFullPath((Join-Path $InstallRoot ".staging-$([Guid]::NewGuid().ToString('N'))"))

Assert-ChildPath $InstallRoot $appDirectory
Assert-ChildPath $InstallRoot $dataDirectory
Assert-ChildPath $InstallRoot $backupsDirectory
Assert-ChildPath $InstallRoot $stagingDirectory

if ($serviceAppDirectory -ine $appDirectory) {
    throw "Service '$ServiceName' runs from $serviceAppDirectory, not the expected $appDirectory."
}

if (-not (Test-Path -LiteralPath $appDirectory -PathType Container)) {
    throw "Application directory not found: $appDirectory"
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = (Get-Content -LiteralPath (Join-Path $projectRoot "version.txt") -Raw).Trim()
}

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Invalid stable semantic version: $Version"
}

$wasRunning = $service.State -eq "Running"
$backupDirectory = Join-Path $backupsDirectory "App-$((Get-Date).ToString('yyyyMMdd-HHmmss'))-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
$failedDirectory = Join-Path $backupsDirectory "Failed-$((Get-Date).ToString('yyyyMMdd-HHmmss'))-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
$backupCreated = $false

try {
    Write-Host "==> Building version $Version into staging"
    & (Join-Path $projectRoot "publish.ps1") -InstallPath $stagingDirectory -DataPath $dataDirectory -Version $Version

    $currentSettings = Join-Path $appDirectory "appsettings.json"
    if (Test-Path -LiteralPath $currentSettings -PathType Leaf) {
        Copy-Item -LiteralPath $currentSettings -Destination (Join-Path $stagingDirectory "appsettings.json") -Force
    }

    if (-not $PSCmdlet.ShouldProcess($appDirectory, "Stop $ServiceName, back up the current App directory, deploy version $Version, and restart")) {
        return
    }

    New-Item -ItemType Directory -Path $backupsDirectory -Force | Out-Null

    if ($wasRunning) {
        Stop-Service -Name $ServiceName
    }

    Move-Item -LiteralPath $appDirectory -Destination $backupDirectory
    $backupCreated = $true
    Move-Item -LiteralPath $stagingDirectory -Destination $appDirectory

    if ($wasRunning) {
        Start-Service -Name $ServiceName
        $settings = Get-Content -LiteralPath (Join-Path $appDirectory "appsettings.json") -Raw | ConvertFrom-Json
        Wait-ForHealth ([int]$settings.Port)
    }

    Write-Host "==> Deployment complete: $appDirectory"
    Write-Host "==> Previous version retained at: $backupDirectory"
} catch {
    $deploymentError = $_

    if ($backupCreated -and (Test-Path -LiteralPath $backupDirectory -PathType Container)) {
        Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue

        if (Test-Path -LiteralPath $appDirectory -PathType Container) {
            Move-Item -LiteralPath $appDirectory -Destination $failedDirectory
        }

        Move-Item -LiteralPath $backupDirectory -Destination $appDirectory

        if ($wasRunning) {
            Start-Service -Name $ServiceName
        }
    }

    throw $deploymentError
} finally {
    if (Test-Path -LiteralPath $stagingDirectory) {
        Assert-ChildPath $InstallRoot $stagingDirectory
        Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
    }
}
