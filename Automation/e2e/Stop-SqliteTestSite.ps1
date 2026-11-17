<#
.SYNOPSIS
Stops the detached Umbraco site process started by New-SqliteTestSite.ps1 and deletes its
throwaway site folder.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][int]$ProcessId,
    [Parameter(Mandatory = $true)][string]$SitePath,
    [switch]$KeepFiles
)

$ErrorActionPreference = "Continue"

. (Join-Path $PSScriptRoot "Common.ps1")

Write-Host "Stopping process tree for PID $ProcessId..."
try {
    Stop-ProcessTree -ProcessId $ProcessId
}
catch {
    Write-Warning "Could not stop process $ProcessId (it may have already exited): $_"
}

if ($KeepFiles) {
    Write-Host "Keeping site folder '$SitePath' (E2E_KEEP_SITE set)."
}
elseif (Test-Path $SitePath) {
    Write-Host "Removing site folder '$SitePath'..."
    # A killed process can hold its native DLLs (e_sqlite3, ICU) open for a moment after taskkill
    # returns, so wait for it to exit and retry, instead of leaving a half-deleted bin folder behind.
    Wait-Process -Id $ProcessId -Timeout 15 -ErrorAction SilentlyContinue
    for ($attempt = 1; $attempt -le 5 -and (Test-Path $SitePath); $attempt++) {
        Remove-Item -Recurse -Force -Path $SitePath -ErrorAction SilentlyContinue
        if (Test-Path $SitePath) { Start-Sleep -Seconds 1 }
    }
    if (Test-Path $SitePath) { Write-Warning "Could not fully remove '$SitePath'." }
}
