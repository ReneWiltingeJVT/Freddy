#!/usr/bin/env pwsh
# Test script for Freddy

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$Coverage
)

$ErrorActionPreference = 'Stop'

Write-Host "Running tests ($Configuration)..." -ForegroundColor Cyan

$testArgs = @(
    'test', 'Freddy.sln',
    '--configuration', $Configuration,
    '--no-build',
    '--verbosity', 'normal'
)

if ($Coverage) {
    $testArgs += '--collect:"XPlat Code Coverage"'
    $testArgs += '--results-directory', './TestResults'
}

dotnet @testArgs
if ($LASTEXITCODE -ne 0) { throw "Tests failed" }

Write-Host "All tests passed." -ForegroundColor Green
