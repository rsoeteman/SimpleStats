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
    Remove-Item -Recurse -Force -Path $SitePath -ErrorAction SilentlyContinue
}
