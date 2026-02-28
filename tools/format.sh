#!/usr/bin/env bash
# Format & lint script for Freddy
set -euo pipefail

CHECK="${1:-false}"

if [ "$CHECK" = "--check" ]; then
    echo "Checking code format..."
    dotnet format Freddy.sln --verify-no-changes --verbosity normal
    echo "Format check passed."
else
    echo "Formatting code..."
    dotnet format Freddy.sln --verbosity normal
    echo "Formatting complete."
fi
