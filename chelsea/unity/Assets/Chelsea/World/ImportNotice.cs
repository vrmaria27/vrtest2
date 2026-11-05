using UnityEngine;

namespace Chelsea.World
{
    /// <summary>Persistent provenance notice for the M1 inspection scene.</summary>
    public sealed class ImportNotice : MonoBehaviour
    {
        [SerializeField] private string datasetKind;
        [SerializeField] private string sourceFile;
        [SerializeField] private double originLongitude;
        [SerializeField] private double originLatitude;
        [Tooltip("Exact imported JSON retained for provenance: manifest, hashes, source coordinates, height evidence, and warnings.")]
        [SerializeField] private TextAsset originalDocument;
        [SerializeField] private string originalDocumentSha256;
        [SerializeField] private string[] importedWarnings;
        private bool showAttribution;
        private Vector2 sourceScroll;

        public void Configure(GeographicDocument document, string importedFile, TextAsset retainedDocument, string documentSha256)
        {
            datasetKind = document.datasetKind;
            sourceFile = importedFile;
            originLongitude = document.origin.longitude;
            originLatitude = document.origin.latitude;
            originalDocument = retainedDocument;
            originalDocumentSha256 = documentSha256;
            importedWarnings = document.warnings ?? System.Array.Empty<string>();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, Mathf.Min(Screen.width - 32, 650), Mathf.Min(Screen.height - 32, 460)), GUI.skin.box);
            GUILayout.Label("HOLOPOLIS · M1 geographic inspection");
            GUILayout.Label(datasetKind == "synthetic-test-fixture"
                ? "SYNTHETIC TEST FIXTURE — this is not Chelsea."
                : "Imported OSM source geometry — visual fidelity and public access unverified.");
            GUILayout.Label("World metres · +X east · +Z north · no XR or walking controls in this milestone.");
            GUILayout.Label("Diagnostic road ribbons are not pavements or walkable surfaces. Extruded heights are source claims or estimates; no surveyed-height or landmark-fidelity claim.");
            if (GUILayout.Button(showAttribution ? "Hide source details" : "Source details & attribution"))
                showAttribution = !showAttribution;
            if (showAttribution)
            {
                sourceScroll = GUILayout.BeginScrollView(sourceScroll);
                GUILayout.Label("Imported file: " + sourceFile);
                GUILayout.Label("Retained JSON: " + (originalDocument != null ? originalDocument.name : "MISSING — provenance is incomplete"));
                GUILayout.Label("Imported document SHA-256: " + originalDocumentSha256);
                GUILayout.Label($"WGS84 origin: {originLongitude:F7}, {originLatitude:F7}");
                GUILayout.Label(datasetKind == "osm-source"
                    ? "© OpenStreetMap contributors · ODbL 1.0 · https://www.openstreetmap.org/copyright"
                    : "Original synthetic geometry for software tests only. No surveyed or OSM geography.");
                GUILayout.Label("Google Maps / Street View were not used for these assets. See docs/ATTRIBUTION.md.");
                foreach (string warning in importedWarnings ?? System.Array.Empty<string>())
                    GUILayout.Label("Import warning: " + warning);
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }
    }
}
