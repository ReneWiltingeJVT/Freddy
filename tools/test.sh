#!/usr/bin/env bash
# Test script for Freddy
set -euo pipefail

CONFIGURATION="${1:-Debug}"
COVERAGE="${2:-false}"

echo "Running tests ($CONFIGURATION)..."

TEST_ARGS="test Freddy.sln --configuration $CONFIGURATION --no-build --verbosity normal"

if [ "$COVERAGE" = "true" ]; then
    TEST_ARGS="$TEST_ARGS --collect:\"XPlat Code Coverage\" --results-directory ./TestResults"
fi

eval dotnet $TEST_ARGS

echo "All tests passed."
