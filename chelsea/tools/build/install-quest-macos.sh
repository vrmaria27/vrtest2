#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."
[[ "$(uname -s)" == Darwin ]] || { echo 'Run this script on the Mac connected to your Quest.' >&2; exit 1; }
CHELSEA_ADB_BIN="${CHELSEA_ADB:-}"
if [[ -z "$CHELSEA_ADB_BIN" ]]; then
  for CHELSEA_CANDIDATE in \
    /Applications/Unity/Hub/Editor/6000.0.66f2/Unity.app/Contents/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb \
    /Applications/Unity/Hub/Editor/6000.0.66f2/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb; do
    if [[ -x "$CHELSEA_CANDIDATE" ]]; then CHELSEA_ADB_BIN="$CHELSEA_CANDIDATE"; break; fi
  done
fi
if [[ ! -x "$CHELSEA_ADB_BIN" ]]; then
  if command -v adb >/dev/null 2>&1; then CHELSEA_ADB_BIN="$(command -v adb)"; else
    echo 'ADB missing. Install Unity Android Build Support / SDK & NDK Tools, or set CHELSEA_ADB to your adb executable.' >&2; exit 1
  fi
fi
CHELSEA_APK="${1:-$(pwd)/unity/Builds/Chelsea-Diagnostic-Alpha.apk}"
[[ -s "$CHELSEA_APK" ]] || { echo 'APK missing. Build the diagnostic successfully first.' >&2; exit 1; }
CHELSEA_DEVICES="$($CHELSEA_ADB_BIN devices)"
if [[ -n "${CHELSEA_DEVICE_SERIAL:-}" ]]; then
  CHELSEA_DEVICE="$CHELSEA_DEVICE_SERIAL"
  [[ "$($CHELSEA_ADB_BIN -s "$CHELSEA_DEVICE" get-state)" == device ]] || { echo 'Selected device is not authorised and ready.' >&2; exit 1; }
else
  CHELSEA_COUNT="$(printf '%s\n' "$CHELSEA_DEVICES" | awk 'NR>1 && $2=="device" {n++} END {print n+0}')"
  if [[ "$CHELSEA_COUNT" != 1 ]]; then
    printf '%s\n' "$CHELSEA_DEVICES"
    echo 'Connect exactly one Quest by USB-C; enable Developer Mode and accept USB debugging inside the headset.' >&2
    echo 'If multiple devices are connected, set CHELSEA_DEVICE_SERIAL to the intended headset serial.' >&2
    exit 1
  fi
  CHELSEA_DEVICE="$(printf '%s\n' "$CHELSEA_DEVICES" | awk 'NR>1 && $2=="device" {print $1}')"
fi
"$CHELSEA_ADB_BIN" -s "$CHELSEA_DEVICE" install -r "$CHELSEA_APK"
# Resolve the actual launcher; do not assume UnityPlayerActivity vs GameActivity.
"$CHELSEA_ADB_BIN" -s "$CHELSEA_DEVICE" shell am start -W \
  -a android.intent.action.MAIN -c android.intent.category.LAUNCHER \
  -p com.holopolis.chelsea.alpha
echo 'Launch command sent. Confirm inside Quest: synthetic label, tracked head view, controller rays, selection, scale and return.'
echo 'A successful ADB launch command is not a passed comfort or performance test.'
