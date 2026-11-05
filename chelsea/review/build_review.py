"""Bundle the actual M0/M1 evidence into a dependency-free, offline HTML viewer."""
import json
from pathlib import Path
root = Path(__file__).resolve().parents[1]
def load(path): return json.loads((root/path).read_text())
data = {
    'fixture': load('data/processed/synthetic.meshes.json'),
    'boundary': load('data/boundary/chelsea-study.proposed.geojson'),
    'firstArea': load('data/boundary/duke-of-york-first-area.proposed.geojson'),
    'landmarks': load('data/landmarks/priority.json')['locations'],
    'report': (root/'docs/VERIFICATION.md').read_text(),
}
template = (root/'review/template.html').read_text()
# Script data is JSON, never interpreted as HTML.
bundle = json.dumps(data, separators=(',',':'), ensure_ascii=True).replace('<', '\\u003c')
(root/'review/index.html').write_text(template.replace('__REVIEW_DATA__',bundle))
print('Built standalone review/index.html from the current fixture, boundary and verification report.')
