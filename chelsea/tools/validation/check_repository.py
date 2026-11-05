"""Validate source/coverage metadata and proposed GeoJSON, not geographic accuracy."""
import csv
import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
for path in sorted(root.rglob('*.json')):
    if any(part in ('obj', 'bin', 'Library', 'Temp', 'source_local', 'processed') for part in path.parts):
        continue
    json.loads(path.read_text())
for path in sorted((root / 'unity/Assets').rglob('*.asmdef')):
    json.loads(path.read_text())
for path in sorted((root / 'data/boundary').glob('*.geojson')):
    collection = json.loads(path.read_text())
    assert collection['type'] == 'FeatureCollection'
    for feature in collection['features']:
        assert feature['properties']['status'] == 'proposed-unreviewed'
        assert feature['geometry']['type'] == 'Polygon'
        ring = feature['geometry']['coordinates'][0]
        assert len(ring) >= 4 and ring[0] == ring[-1]
        assert all(len(p) == 2 and -0.19 < p[0] < -0.14 and 51.47 < p[1] < 51.50 for p in ring)
coverage = json.loads((root / 'data/coverage.geojson').read_text())
assert all(f['properties']['status'] == 'unavailable' for f in coverage['features'])
route = json.loads((root / 'data/routes/discovery.pending.geojson').read_text())
assert route['features'] == []
landmarks = json.loads((root / 'data/landmarks/priority.json').read_text())['locations']
assert len(landmarks) == 8 and len({x['id'] for x in landmarks}) == 8
with (root / 'docs/REFERENCE_REGISTER.csv').open(newline='') as f:
    references = list(csv.DictReader(f))
assert len(references) >= 8
assert all(None not in row for row in references), 'Malformed reference-register CSV'
print('PASS repository metadata: JSON/asmdef syntax, candidate boundary envelopes, eight landmarks, unavailable coverage, no unverified route, reference CSV')
print('These checks do not certify boundary accuracy, licensing compliance, Unity compilation or device behavior.')
