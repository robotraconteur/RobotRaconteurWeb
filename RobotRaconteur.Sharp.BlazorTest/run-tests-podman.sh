#!/bin/sh
# Build and run the Blazor WebAssembly browser tests in podman containers. See README.md.
#
# Both containers use slirp4netns without host loopback access: they can download packages, but
# cannot reach the host or the local network. The test server and browsers talk over the Playwright
# container's own loopback. Caches go in .work/ at the repository root.
#
# Usage: RobotRaconteur.Sharp.BlazorTest/run-tests-podman.sh [playwright test arguments]
#   e.g. RobotRaconteur.Sharp.BlazorTest/run-tests-podman.sh --project=chromium
set -eu

cd "$(dirname "$0")/.."
NETWORK="--network=slirp4netns:allow_host_loopback=false"
DOTNET_IMAGE="mcr.microsoft.com/dotnet/sdk:8.0"
PLAYWRIGHT_IMAGE="mcr.microsoft.com/playwright:v1.49.1-noble"

podman run --rm $NETWORK -v "$PWD":/src:Z -w /src \
    -e NUGET_PACKAGES=/src/.work/nuget -e DOTNET_CLI_HOME=/src/.work/dotnet-home \
    -e DOTNET_CLI_TELEMETRY_OPTOUT=1 -e DOTNET_NOLOGO=1 \
    "$DOTNET_IMAGE" sh -c '
        set -e
        dotnet publish RobotRaconteur.Sharp.BlazorTest/RobotRaconteur.Sharp.BlazorTest.csproj -c Release \
            -o RobotRaconteur.Sharp.BlazorTest/bin/publish-app
        dotnet publish RobotRaconteur.Sharp.Test/RobotRaconteur.Sharp.Test.csproj -c Release -r linux-x64 \
            --self-contained -o RobotRaconteur.Sharp.BlazorTest/bin/publish-server'

podman run --rm $NETWORK --shm-size=1g -v "$PWD":/src:Z -w /src/RobotRaconteur.Sharp.BlazorTest/e2e \
    -e npm_config_cache=/src/.work/npm -e npm_config_update_notifier=false -e CI=1 \
    "$PLAYWRIGHT_IMAGE" sh -c 'npm ci --no-audit --no-fund && npx playwright test "$@"' sh "$@"
