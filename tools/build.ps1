#!/usr/bin/env pwsh
# Build script for Freddy

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

Write-Host "Building Freddy ($Configuration)..." -ForegroundColor Cyan
dotnet build Freddy.sln --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

Write-Host "Build completed successfully." -ForegroundColor Green
