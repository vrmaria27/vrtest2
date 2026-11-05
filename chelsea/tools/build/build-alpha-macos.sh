#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."
if [[ "$(uname -s)" != Darwin ]]; then
  echo 'Run this script on your Mac. The cloud machine has no licensed Unity editor.' >&2
  exit 1
fi
CHELSEA_EDITOR="${CHELSEA_UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.0.66f2/Unity.app/Contents/MacOS/Unity}"
if [[ ! -x "$CHELSEA_EDITOR" ]]; then
  echo 'Install Unity 6000.0.66f2 using Unity Hub, with Android Build Support, SDK & NDK Tools, and OpenJDK.' >&2
  echo 'Open the editor once and activate your eligible Unity licence locally; never put login details in this script.' >&2
  exit 1
fi
if [[ "${1:-}" != --diagnostic ]]; then
  echo 'Usage: bash tools/build/build-alpha-macos.sh --diagnostic' >&2
  echo 'This explicitly builds a synthetic controller diagnostic, NOT the Chelsea game.' >&2
  exit 1
fi
CHELSEA_BUILD_DIR="${CHELSEA_BUILD_DIR:-$(pwd)/unity/Builds}"
mkdir -p "$CHELSEA_BUILD_DIR"
CHELSEA_RUN_ID="$(uuidgen)"
CHELSEA_LOG="$CHELSEA_BUILD_DIR/diagnostic-build-$CHELSEA_RUN_ID.log"
CHELSEA_APK="$CHELSEA_BUILD_DIR/Chelsea-Diagnostic-Alpha.apk"
echo 'Building synthetic diagnostic. No real Chelsea content or street exploration is claimed.'
"$CHELSEA_EDITOR" -batchmode -quit -buildTarget Android \
  -projectPath "$(pwd)/unity" \
  -executeMethod Chelsea.Editor.AlphaBuild.BuildDiagnostic \
  -chelseaOutput "$CHELSEA_APK" -chelseaRunId "$CHELSEA_RUN_ID" -logFile "$CHELSEA_LOG"
[[ -s "$CHELSEA_APK" ]] || { echo "No APK produced. Read $CHELSEA_LOG" >&2; exit 1; }
[[ -f "$CHELSEA_LOG" ]] && grep -Fq "CHELSEA_BUILD_RUN=$CHELSEA_RUN_ID" "$CHELSEA_LOG" || { echo "No success marker for this build run. Read $CHELSEA_LOG" >&2; exit 1; }
[[ -f "$CHELSEA_APK.build.json" ]] && grep -Fq "$CHELSEA_RUN_ID" "$CHELSEA_APK.build.json" || { echo "Missing current-run build evidence; refusing a possibly stale APK." >&2; exit 1; }
echo "APK: $CHELSEA_APK"
echo "Build log: $CHELSEA_LOG"
shasum -a 256 "$CHELSEA_APK"
echo 'Device install is a separate step: bash tools/build/install-quest-macos.sh'
