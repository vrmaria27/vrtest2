#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."
export DOTNET_CLI_HOME="${CHELSEA_CLI_HOME:-/workspace/.tools/dotnet-home}"
export NUGET_PACKAGES="${CHELSEA_NUGET_CACHE:-/workspace/.tools/nuget}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
if [[ -n "${CHELSEA_DOTNET:-}" ]]; then
  exec "$CHELSEA_DOTNET" "$@"
elif command -v dotnet >/dev/null 2>&1; then
  exec dotnet "$@"
else
  exec /workspace/.tools/dotnet/dotnet "$@"
fi
