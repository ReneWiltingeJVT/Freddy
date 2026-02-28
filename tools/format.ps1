#!/usr/bin/env pwsh
# Format & lint script for Freddy

param(
    [switch]$Check  # Verify only, don't fix
)

$ErrorActionPreference = 'Stop'

if ($Check) {
    Write-Host "Checking code format..." -ForegroundColor Cyan
    dotnet format Freddy.sln --verify-no-changes --verbosity normal
    if ($LASTEXITCODE -ne 0) { throw "Formatting issues found. Run tools/format.ps1 to fix." }
    Write-Host "Format check passed." -ForegroundColor Green
} else {
    Write-Host "Formatting code..." -ForegroundColor Cyan
    dotnet format Freddy.sln --verbosity normal
    Write-Host "Formatting complete." -ForegroundColor Green
}
