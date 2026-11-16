<#
.SYNOPSIS
Shared helpers for the e2e site scripts (New-SqliteTestSite.ps1, Stop-SqliteTestSite.ps1).
Dot-source this file rather than running it directly.

.NOTES
Runs on Windows PowerShell 5.1 (local dev) and on pwsh 7+ on Windows or Linux.
Windows PowerShell 5.1 has no $IsWindows, hence the PSEdition check.
#>

$IsWindowsHost = ($PSVersionTable.PSEdition -eq "Desktop") -or $IsWindows

function Test-SiteReady {
    param([string]$Uri)

    # The real curl binary (curl.exe on Windows, where plain 'curl' is an Invoke-WebRequest alias
    # in Windows PowerShell 5.1) is used because Windows PowerShell 5.1 can't skip the self-signed
    # dev certificate check.
    #
    # This only confirms Kestrel is serving the login host page. It does NOT confirm Umbraco's
    # runtime has finished booting; tests/auth.setup.ts waits for that by retrying the login.
    $curl = if ($IsWindowsHost) { "curl.exe" } else { "curl" }
    $nullDevice = if ($IsWindowsHost) { "NUL" } else { "/dev/null" }
    $httpCode = & $curl --silent --insecure --max-time 5 --output $nullDevice --write-out "%{http_code}" $Uri
    return $httpCode -eq "200"
}

function Start-SiteProcess {
    param(
        [string]$SitePath,
        [string]$LogPath,
        [string]$ErrLogPath
    )

    # --no-launch-profile is required: otherwise `dotnet run` applies Properties/launchSettings.json's
    # applicationUrl, which silently overrides the ASPNETCORE_URLS env var set by the caller.
    $startArgs = @{
        FilePath               = "dotnet"
        ArgumentList           = @("run", "--no-build", "--no-launch-profile")
        WorkingDirectory       = $SitePath
        RedirectStandardOutput = $LogPath
        RedirectStandardError  = $ErrLogPath
        PassThru               = $true
    }
    # -WindowStyle is Windows-only; pwsh on Linux rejects it.
    if ($IsWindowsHost) { $startArgs.WindowStyle = "Hidden" }

    return Start-Process @startArgs
}

function Stop-ProcessTree {
    param([int]$ProcessId)

    # `dotnet run` starts the site as a child process, so the whole tree has to go, not just the
    # launcher.
    if ($IsWindowsHost) {
        taskkill /PID $ProcessId /T /F | Out-Null
        return
    }

    # Linux has no taskkill /T: walk the children first (pgrep -P lists direct children only),
    # since killing the parent first would reparent them to init and lose track of them.
    foreach ($childId in @(pgrep -P $ProcessId)) {
        Stop-ProcessTree -ProcessId ([int]$childId)
    }
    kill -9 $ProcessId 2>$null
}
