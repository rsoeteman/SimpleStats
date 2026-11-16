<#
.SYNOPSIS
Provisions, builds, and starts a throwaway Umbraco 18 site backed by SQLite, referencing this
repo's SimpleStats project, for use by the Playwright e2e suite in tests/SimpleStats.E2ETests.

.OUTPUTS
Writes a compact JSON object { url, processId, sitePath, logPath } to -InfoOutputPath.

.NOTES
Deliberately does NOT hand the JSON back through stdout: this script starts a detached process
(the site) that keeps running, and on Windows that child inherits a handle to this script's
stdout. A caller that captures stdout through a pipe (e.g. Node's execFileSync with the default
'pipe' stdio) then waits forever for an EOF that never comes. Callers must use 'inherit' stdio and
read the result from -InfoOutputPath instead.
#>
[CmdletBinding()]
param(
    [string]$SiteName = "SimpleStatsE2E_$(Get-Date -Format yyyyMMddHHmmss)",
    [string]$DestinationRoot = (Join-Path ([System.IO.Path]::GetTempPath()) "SimpleStatsE2E"),
    [int]$HttpsPort = 44351,
    [string]$TemplateVersion = "18.3.1",
    [switch]$SkipTemplateInstall,
    # Skip `npm run build` of the backoffice client, e.g. in CI where an earlier step already built it.
    [switch]$SkipClientBuild,
    # Covers the whole script (template install, build and boot).
    [int]$TimeoutSeconds = 600,
    # How long Umbraco's unattended upgrade on first boot may take before the site counts as stalled
    # (exit code 3, so global-setup.ts retries on a new site).
    [int]$UpgradeTimeoutSeconds = 120,
    [Parameter(Mandatory = $true)][string]$InfoOutputPath
)

$ErrorActionPreference = "Stop"
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)

. (Join-Path $PSScriptRoot "Common.ps1")

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
# SimpleStats.csproj references SimpleStats.Client, so the backoffice assets come along, the same
# way they do for a site that installs the NuGet package.
$packageProject = Join-Path $RepoRoot "src/SimpleStats/SimpleStats.csproj"
$clientPath = Join-Path $RepoRoot "src/SimpleStats.Client"

if (-not $SkipClientBuild) {
    Push-Location $clientPath
    try {
        if (-not (Test-Path (Join-Path $clientPath "node_modules"))) {
            Write-Host "Installing SimpleStats.Client npm packages..."
            npm ci
            if ($LASTEXITCODE -ne 0) { throw "npm ci failed with exit code $LASTEXITCODE" }
        }
        Write-Host "Building SimpleStats.Client backoffice assets..."
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "npm run build failed with exit code $LASTEXITCODE" }
    }
    finally {
        Pop-Location
    }
}

New-Item -ItemType Directory -Force -Path $DestinationRoot | Out-Null

# The templates go into a private template hive instead of the machine-wide one, so the site is
# always created from the Umbraco $TemplateVersion template, whatever other Umbraco versions are
# installed globally on the machine.
$templateHive = Join-Path $DestinationRoot "template-hive"
if (-not $SkipTemplateInstall -or -not (Test-Path $templateHive)) {
    Write-Host "Installing Umbraco.Templates@$TemplateVersion into '$templateHive'..."
    dotnet new install "Umbraco.Templates@$TemplateVersion" --force --debug:custom-hive $templateHive | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Installing Umbraco.Templates failed with exit code $LASTEXITCODE" }
}
$sitePath = Join-Path $DestinationRoot $SiteName

Write-Host "Creating site '$SiteName' at '$sitePath'..."
dotnet new umbraco -n $SiteName -o $sitePath --development-database-type SQLite --starter-kit Umbraco.TheStarterKit --force --debug:custom-hive $templateHive
if ($LASTEXITCODE -ne 0) { throw "dotnet new umbraco failed with exit code $LASTEXITCODE" }

Push-Location $sitePath
try {
    # Unattended install: Umbraco creates the database and this backoffice user on first boot.
    # Keep the user in sync with tests/SimpleStats.E2ETests/e2e-user.ts.
    $Env:UMBRACO__CMS__UNATTENDED__INSTALLUNATTENDED = "true"
    $Env:UMBRACO__CMS__UNATTENDED__UNATTENDEDUSERNAME = "E2E Test User"
    $Env:UMBRACO__CMS__UNATTENDED__UNATTENDEDUSEREMAIL = "e2e@example.com"
    $Env:UMBRACO__CMS__UNATTENDED__UNATTENDEDUSERPASSWORD = "E2eTest1234!"
    # Commit buffered page views every 5 seconds instead of every minute, so tests don't wait long
    # before visits show up on the dashboard.
    $Env:SIMPLESTATS__COMMITINTERVALSECONDS = "5"
    $Env:ASPNETCORE_URLS = "https://localhost:$HttpsPort"
    # --no-launch-profile (Start-SiteProcess) skips Properties/launchSettings.json, so the
    # Development environment it would normally set must be provided here.
    $Env:ASPNETCORE_ENVIRONMENT = "Development"

    dotnet add "$SiteName.csproj" reference $packageProject
    if ($LASTEXITCODE -ne 0) { throw "Failed to add reference to $packageProject" }

    Write-Host "Building site..."
    dotnet build --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }

    $logPath = Join-Path $sitePath "e2e-site.log"
    $errLogPath = Join-Path $sitePath "e2e-site.err.log"

    Write-Host "Starting site (detached), logging to '$logPath'..."
    $proc = Start-SiteProcess -SitePath $sitePath -LogPath $logPath -ErrLogPath $errLogPath

    $url = "https://localhost:$HttpsPort"
    $readyUrl = "$url/umbraco/login"
    $ready = $false

    $upgradeDeadline = (Get-Date).AddSeconds($UpgradeTimeoutSeconds)
    if ($upgradeDeadline -gt $deadline) { $upgradeDeadline = $deadline }
    Write-Host "Waiting for Umbraco's unattended upgrade to finish (up to $UpgradeTimeoutSeconds seconds)..."
    if (-not (Wait-UnattendedUpgrade -SitePath $sitePath -Process $proc -Deadline $upgradeDeadline)) {
        # Normally this takes 10-20 seconds. A stalled upgrade doesn't recover, so stop this site and
        # exit with a dedicated code: global-setup.ts then retries on a brand-new site.
        Write-Warning "Umbraco's unattended upgrade did not finish within $UpgradeTimeoutSeconds seconds. Log tail:"
        if (Test-Path $logPath) { Get-Content $logPath -Tail 20 }
        try { Stop-ProcessTree -ProcessId $proc.Id } catch {}
        exit 3
    }

    Write-Host "Waiting for site to become ready at $readyUrl (deadline: $($deadline.ToString('HH:mm:ss')))..."
    while ((Get-Date) -lt $deadline) {
        if ($proc.HasExited) {
            Write-Warning "Site process exited early (exit code $($proc.ExitCode)). Log tail:"
            if (Test-Path $logPath) { Get-Content $logPath -Tail 50 }
            if (Test-Path $errLogPath) { Get-Content $errLogPath -Tail 50 }
            throw "Site process exited before becoming ready."
        }

        if (Test-SiteReady -Uri $readyUrl) {
            $ready = $true
            break
        }
        Start-Sleep -Seconds 2
    }

    if (-not $ready) {
        Write-Warning "Site did not become ready within $TimeoutSeconds seconds. Log tail:"
        if (Test-Path $logPath) { Get-Content $logPath -Tail 50 }
        if (Test-Path $errLogPath) { Get-Content $errLogPath -Tail 50 }
        try { Stop-ProcessTree -ProcessId $proc.Id } catch {}
        throw "Site failed to become ready."
    }

    Write-Host "Site is ready."

    $info = [PSCustomObject]@{
        url       = $url
        processId = $proc.Id
        sitePath  = $sitePath
        logPath   = $logPath
    }

    $infoOutputDir = Split-Path -Parent $InfoOutputPath
    if ($infoOutputDir -and -not (Test-Path $infoOutputDir)) {
        New-Item -ItemType Directory -Force -Path $infoOutputDir | Out-Null
    }
    $info | ConvertTo-Json -Compress | Out-File -FilePath $InfoOutputPath -Encoding utf8
}
finally {
    Pop-Location
}
