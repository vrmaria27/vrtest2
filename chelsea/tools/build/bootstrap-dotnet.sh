#!/usr/bin/env bash
set -euo pipefail
# Local installation; no sudo, shell profile edits or disabled TLS/checksums.
SDK_VERSION=8.0.427
SDK_SHA512=85725692f465f5024b9b42a159a29d9f73e8c57ad9e8bfe1e7028a505d18031d68931e8aa3f9de44145b08170dde897c8a78e1aa18d4219698b6891af7095d39
SDK_ROOT="${CHELSEA_DOTNET_ROOT:-/workspace/.tools/dotnet}"
if [[ -x "$SDK_ROOT/dotnet" ]] && [[ "$("$SDK_ROOT/dotnet" --version)" == "$SDK_VERSION" ]]; then
  echo "Using retained .NET SDK $SDK_VERSION at $SDK_ROOT"
  exit 0
fi
[[ "$(uname -s)" == Linux && "$(uname -m)" == x86_64 ]] || { echo 'This bootstrap pins Linux x64; install the pinned SDK for your platform.' >&2; exit 1; }
SDK_TMP="$(mktemp -d)"
trap 'rm -rf "$SDK_TMP"' EXIT
curl --fail --show-error --location --retry 2 --max-time 180 \
  "https://builds.dotnet.microsoft.com/dotnet/Sdk/$SDK_VERSION/dotnet-sdk-$SDK_VERSION-linux-x64.tar.gz" \
  -o "$SDK_TMP/sdk.tar.gz"
printf '%s  %s\n' "$SDK_SHA512" "$SDK_TMP/sdk.tar.gz" | sha512sum --check --status
mkdir -p "$SDK_ROOT"
tar -xzf "$SDK_TMP/sdk.tar.gz" -C "$SDK_ROOT"
[[ "$("$SDK_ROOT/dotnet" --version)" == "$SDK_VERSION" ]]
echo "Installed verified .NET SDK $SDK_VERSION at $SDK_ROOT"
