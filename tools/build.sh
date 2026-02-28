#!/usr/bin/env bash
# Build script for Freddy
set -euo pipefail

CONFIGURATION="${1:-Debug}"

echo "Building Freddy ($CONFIGURATION)..."
dotnet build Freddy.sln --configuration "$CONFIGURATION" --no-restore

echo "Build completed successfully."
