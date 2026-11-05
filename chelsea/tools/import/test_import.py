#!/usr/bin/env python3
"""Offline integration checks using original synthetic fixtures, never Chelsea evidence."""
import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--dotnet", default=shutil.which("dotnet") or "/workspace/.tools/dotnet/dotnet")
parser.add_argument("--dll", default=str(ROOT / "tools/import/bin/Debug/net8.0/Chelsea.Import.dll"))
OPTIONS, OTHER = parser.parse_known_args()


class ImportTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="chelsea-synthetic-")
        self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name)
        for name in ["synthetic.osm", "synthetic-boundary.geojson", "synthetic.manifest.json"]:
            shutil.copyfile(ROOT / "tests/fixtures" / name, self.path / name)
        self.input = self.path / "synthetic.osm"
        self.boundary = self.path / "synthetic-boundary.geojson"
        self.manifest = self.path / "synthetic.manifest.json"
        self.output = self.path / "meshes.json"

    def run_import(self):
        return subprocess.run([OPTIONS.dotnet, OPTIONS.dll, "--input", str(self.input),
            "--boundary", str(self.boundary), "--manifest", str(self.manifest),
            "--output", str(self.output)], capture_output=True, text=True, timeout=30)

    def success(self):
        result = self.run_import()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertTrue(self.output.is_file())
        return json.loads(self.output.read_text())

    def failure(self, text):
        result = self.run_import()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn(text.lower(), result.stderr.lower())
        self.assertFalse(self.output.exists(), "A rejected import wrote output")

    def edit_source(self, edit):
        self.input.write_text(edit(self.input.read_text()))
        # Only original synthetic fixture data is altered in these failure tests.
        manifest = json.loads(self.manifest.read_text())
        manifest["sha256"] = hashlib.sha256(self.input.read_bytes()).hexdigest()
        self.manifest.write_text(json.dumps(manifest))

    def test_deterministic_meshes_and_provenance(self):
        data = self.success()
        original = self.output.read_bytes()
        self.success()
        self.assertEqual(self.output.read_bytes(), original)
        self.assertEqual(data["schemaVersion"], 1)
        self.assertEqual(data["datasetKind"], "synthetic-test-fixture")
        self.assertFalse(data["complete"])
        self.assertEqual(data["source"]["sha256"], hashlib.sha256(self.input.read_bytes()).hexdigest())
        self.assertEqual(data["boundarySha256"], hashlib.sha256(self.boundary.read_bytes()).hexdigest())
        self.assertEqual(data["origin"], {"longitude": -0.1589, "latitude": 51.4905})
        self.assertEqual(len(data["contentHash"]), 64)
        self.assertTrue(any("SYNTHETIC TEST FIXTURE" in w for w in data["warnings"]))

    def test_source_id_graph_connectivity_no_coordinate_snapping(self):
        data = self.success()
        graph = data["graph"]
        self.assertEqual(len(graph["nodes"]), 7)
        self.assertEqual(len(graph["edges"]), 5)
        self.assertEqual(graph["connectedComponents"], 2)
        nodes = {n["sourceId"]: n for n in graph["nodes"]}
        self.assertEqual(nodes["osm:node:1001"]["position"], nodes["osm:node:1006"]["position"])
        self.assertNotEqual(nodes["osm:node:1001"]["sourceId"], nodes["osm:node:1006"]["sourceId"])
        self.assertEqual(sum(e["from"] == "osm:node:1002" or e["to"] == "osm:node:1002" for e in graph["edges"]), 4)

    def test_mesh_indices_budget_and_explicit_fidelity(self):
        data = self.success()
        self.assertEqual(data["summary"]["importedRoads"], 3)
        self.assertEqual(data["summary"]["importedBuildings"], 1)
        self.assertEqual(data["summary"]["triangleCount"], 22)
        self.assertLess(data["summary"]["triangleCount"], 1_300_000)
        for mesh in data["meshes"]:
            self.assertEqual(len(mesh["triangles"]) % 3, 0)
            self.assertTrue(all(0 <= i < len(mesh["vertices"]) for i in mesh["triangles"]))
            self.assertTrue(all(math.isfinite(v[k]) for v in mesh["vertices"] for k in ["x", "y", "z"]))
        self.assertTrue(all(not road["walkable"] and road["diagnosticWidthMetres"] == 0.4 for road in data["roads"]))
        self.assertIn("unverified measurement", data["buildings"][0]["heightEvidence"])
        self.assertEqual(data["buildings"][0]["heightMetres"], 8)
        self.assertEqual(data["summary"]["unsupportedRelations"], 1)
        self.assertTrue(any("osm:relation:9001" in w for w in data["warnings"]))

    def test_hash_mismatch_rejected(self):
        self.input.write_text(self.input.read_text() + "\n")
        self.failure("SHA-256")

    def test_missing_provenance_rejected(self):
        value = json.loads(self.manifest.read_text())
        del value["licence"]
        self.manifest.write_text(json.dumps(value))
        self.failure("licence")

    def test_unknown_dataset_kind_rejected(self):
        value = json.loads(self.manifest.read_text())
        value["datasetKind"] = "verified-Chelsea"
        self.manifest.write_text(json.dumps(value))
        self.failure("datasetKind")

    def test_missing_source_node_rejected(self):
        self.edit_source(lambda s: s.replace('ref="1003"', 'ref="999999"'))
        self.failure("missing node")

    def test_self_intersecting_footprint_rejected(self):
        self.edit_source(lambda s: s.replace('<nd ref="2001"/><nd ref="2002"/><nd ref="2003"/><nd ref="2004"/><nd ref="2001"/>',
            '<nd ref="2001"/><nd ref="2003"/><nd ref="2002"/><nd ref="2004"/><nd ref="2001"/>'))
        self.failure("polygon")

    def test_zero_length_road_rejected(self):
        self.edit_source(lambda s: s.replace('<nd ref="1006"/><nd ref="1007"/>', '<nd ref="1006"/><nd ref="1006"/>'))
        self.failure("zero-length")

    def test_building_crossing_boundary_rejected(self):
        boundary = json.loads(self.boundary.read_text())
        boundary["geometry"]["coordinates"] = [[[-0.161, 51.489], [-0.1584, 51.489], [-0.1584, 51.492], [-0.161, 51.492], [-0.161, 51.489]]]
        self.boundary.write_text(json.dumps(boundary))
        self.failure("crosses the boundary")

    def test_unsafe_xml_rejected(self):
        self.edit_source(lambda s: s.replace('<osm ', '<!DOCTYPE osm [<!ENTITY external SYSTEM "file:///nonexistent">]>\n<osm ', 1))
        self.failure("DTD")

    def test_output_preserved_on_failed_import(self):
        self.success()
        previous = self.output.read_bytes()
        self.input.write_text(self.input.read_text() + "\n")
        result = self.run_import()
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(previous, self.output.read_bytes())

    def test_malformed_boundaries_fail_cleanly(self):
        for data in [{}, {"type": "Polygon", "coordinates": []},
                {"type": "Polygon", "coordinates": [[]]},
                {"type": "Polygon", "coordinates": [[[], [], [], []]]},
                {"type": "FeatureCollection", "features": []},
                {"type": "Feature", "geometry": None}]:
            with self.subTest(boundary=data):
                self.boundary.write_text(json.dumps(data))
                result = self.run_import()
                self.assertEqual(result.returncode, 1, result.stderr)
                self.assertIn("IMPORT FAILED", result.stderr)
                self.assertNotIn("Unhandled exception", result.stderr)
                self.assertFalse(self.output.exists())

    def test_source_order_independence(self):
        original = self.success()
        # Reorder top-level XML source elements without changing their content.
        import xml.etree.ElementTree as ET
        root = ET.fromstring(self.input.read_text())
        root[:] = list(reversed(list(root)))
        self.edit_source(lambda _: ET.tostring(root, encoding="unicode"))
        result = self.success()
        self.assertEqual(original["meshes"], result["meshes"])
        self.assertEqual(original["contentHash"], result["contentHash"])
        self.assertEqual(original["graph"], result["graph"])


if __name__ == "__main__":
    unittest.main(argv=[sys.argv[0], *OTHER], verbosity=2)
