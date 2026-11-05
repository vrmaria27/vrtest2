#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."
if [[ -n "${CHELSEA_DOTNET:-}" ]]; then
  DOTNET_EXE="$CHELSEA_DOTNET"
elif command -v dotnet >/dev/null 2>&1; then
  DOTNET_EXE="$(command -v dotnet)"
else
  DOTNET_EXE=/workspace/.tools/dotnet/dotnet
fi
[[ -x "$DOTNET_EXE" ]] || { echo 'Install pinned SDK: bash tools/build/bootstrap-dotnet.sh' >&2; exit 1; }
export DOTNET_CLI_HOME="${CHELSEA_CLI_HOME:-/workspace/.tools/dotnet-home}"
export NUGET_PACKAGES="${CHELSEA_NUGET_CACHE:-/workspace/.tools/nuget}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
"$DOTNET_EXE" run --project dotnet/Chelsea.Core.Tests -c Release
"$DOTNET_EXE" build tools/import/Chelsea.Import.csproj -c Release --nologo
python3 tools/import/test_import.py --dotnet "$DOTNET_EXE" --dll tools/import/bin/Release/net8.0/Chelsea.Import.dll
python3 tools/validation/check_repository.py

bash -n tools/build/build-alpha-macos.sh tools/build/install-quest-macos.sh
python3 tools/build/test-macos-launchers.py
