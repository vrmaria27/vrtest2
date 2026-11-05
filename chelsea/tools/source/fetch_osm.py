#!/usr/bin/env python3
"""Explicit acquisition, never called at build/runtime. Requires approved OSM egress."""
import argparse
import datetime
import hashlib
import json
import os
from pathlib import Path
import tempfile
import urllib.request
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output-dir', type=Path, required=True, help='New immutable snapshot directory; existing paths refused')
args = parser.parse_args()
url = 'https://api.openstreetmap.org/api/0.6/map?bbox=-0.164,51.487,-0.155,51.493'
target = args.output_dir.resolve()
if target.exists():
    parser.error('Snapshot directory exists. Reuse its raw hash, or explicitly choose a new versioned directory.')
request = urllib.request.Request(url, headers={'User-Agent': 'Holopolis-Chelsea-development/0.1 (bounded offline source research)'})
# Uses inherited HTTPS proxy and CA trust. Do not add unverified SSL contexts.
with urllib.request.urlopen(request, timeout=60) as response:
    raw = response.read(25_000_001)
if len(raw) > 25_000_000:
    raise SystemExit('Source exceeds first-area 25MB limit; nothing saved')
if b'<!DOCTYPE' in raw.upper() or b'<!ENTITY' in raw.upper():
    raise SystemExit('Unsafe XML declaration; nothing saved')
root = ET.fromstring(raw)
if root.tag != 'osm' or not root.findall('node'):
    raise SystemExit('Response is not a nonempty OSM XML extract; nothing saved')
now = datetime.datetime.now(datetime.timezone.utc)
manifest = {
    'datasetKind': 'osm-source', 'licence': 'ODbL-1.0', 'sourceUrl': url,
    'snapshotDate': now.date().isoformat(), 'acquiredAtUtc': now.isoformat(),
    'sha256': hashlib.sha256(raw).hexdigest(),
    'reviewStatus': 'unreviewed',
    'coverageWarning': 'Bounding-box acquisition is not release coverage. Full source ways/relations may extend beyond AOI; inspect diagnostics. Public access, heights and landmark appearance unverified.',
    'attribution': '© OpenStreetMap contributors; https://www.openstreetmap.org/copyright',
}
target.parent.mkdir(parents=True, exist_ok=True)
staging = Path(tempfile.mkdtemp(prefix='.chelsea-source-', dir=target.parent))
try:
    (staging / 'extract.osm').write_bytes(raw)
    (staging / 'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
    (staging / 'ATTRIBUTION.txt').write_text('© OpenStreetMap contributors\nOpen Database Licence 1.0\nhttps://www.openstreetmap.org/copyright\nPreserve source and derivative-database obligations. See docs/ATTRIBUTION.md.\n')
    # Refuse overwrite even if another acquisition created the destination meanwhile.
    if target.exists():
        raise FileExistsError(target)
    os.rename(staging, target)
finally:
    if staging.exists():
        for file in staging.iterdir(): file.unlink()
        staging.rmdir()
print(f'Saved unreviewed OSM snapshot: {target}; SHA-256 {manifest["sha256"]}')
print('Review source coverage and licence obligations before import/release. No accuracy gate passed.')
