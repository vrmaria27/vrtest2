using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Chelsea.World;
using Chelsea.XR;
using UnityEngine;
using UnityEngine.Rendering;

namespace Chelsea.Runtime
{
    public enum LightingPreset { Day, Overcast, Dusk }

    /// <summary>
    /// Offline developer inspection foundation, not a finished Chelsea game.
    /// This central component has no per-feature Update methods. XR adapters supply
    /// normalized actions; no source mesh grants public access or teleport permission.
    /// </summary>
    public sealed class ChelseaExperience : MonoBehaviour, IWorldPlacement
    {
        [SerializeField] private TextAsset geographicData;
        [SerializeField] private Material baseMaterial;
        [SerializeField] private Transform head;
        [SerializeField] private Light sun;

        private const int TriangleBudget = 1300000;
        private readonly List<Mesh> ownedMeshes = new List<Mesh>();
        private readonly List<Material> ownedMaterials = new List<Material>();
        private readonly Dictionary<string, List<MeshRenderer>> features = new Dictionary<string, List<MeshRenderer>>(StringComparer.Ordinal);
        private readonly Dictionary<MeshRenderer, Material> originalMaterials = new Dictionary<MeshRenderer, Material>();
        private Transform geometryRoot;
        private Transform menuRoot;
        private TextMesh title;
        private TextMesh statusText;
        private TextMesh sourceText;
        private Material highlightMaterial;
        private GeographicDocument document;
        private SourceEnvelope provenance;
        private ExperienceSettings settings;
        private bool initialized;
        private bool focused = true;
        private bool tracked = true;
        private bool showSources;
        private float geographicWidth = 1;
        private string selectedSource;
        private string settingsPath;
        private bool settingsDirty;
        private float saveAfter;
        private bool menuDirty;
        private float renderMenuAfter;

        public Transform WorldRoot { get; private set; }
        public float TabletopWidthMetres => settings != null ? settings.tabletopWidthMetres : 1.2f;
        public bool IsSynthetic => document != null && document.datasetKind == "synthetic-test-fixture";
        public bool HasData => document != null && WorldRoot != null;
        public bool IsInteractionPaused => !focused || !tracked;
        public string Status { get; private set; } = "Not initialized";
        public string SelectedSource => selectedSource;
        public string DatasetKind => document != null ? document.datasetKind : "unavailable";

        /// <summary>Called by the Editor scene builder; references survive scene save.</summary>
        public void Configure(TextAsset data, Material material, Transform headTransform, Light directionalLight)
        {
            if (initialized) throw new InvalidOperationException("Configure before initialization.");
            geographicData = data;
            baseMaterial = material;
            head = headTransform;
            sun = directionalLight;
        }

        private void Start() => Initialize();

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            settingsPath = Path.Combine(Application.persistentDataPath, "chelsea-developer-settings-v1.json");
            settings = ExperienceSettingsStore.Load(settingsPath);
            if (head == null && Camera.main != null) head = Camera.main.transform;
            try
            {
                if (geographicData == null) throw new InvalidDataException("No offline geographic document is bundled. No replacement city will be generated.");
                if (baseMaterial == null) throw new InvalidDataException("The scene needs its pinned URP material reference.");
                document = JsonUtility.FromJson<GeographicDocument>(geographicData.text);
                provenance = JsonUtility.FromJson<SourceEnvelope>(geographicData.text);
                ValidateDocument(document, provenance);
                BuildWorld();
                Status = IsSynthetic ? "Synthetic developer fixture loaded. Not Chelsea." : "OSM diagnostic geometry loaded. Source and access unverified.";
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidDataException || error is InvalidOperationException)
            {
                document = null;
                Status = "LOAD BLOCKED: " + error.Message;
                Debug.LogError(Status);
            }
            BuildMenu();
            ReturnToTabletop();
            ApplyLighting((LightingPreset)settings.lightingPreset, false);
            RefreshMenu();
        }

        public static void ValidateDocument(GeographicDocument data, SourceEnvelope source)
        {
            if (data == null || data.schemaVersion != 1) throw new InvalidDataException("Expected geographic schemaVersion 1.");
            if (data.datasetKind != "synthetic-test-fixture" && data.datasetKind != "osm-source")
                throw new InvalidDataException("Explicit known datasetKind is required.");
            if (data.origin == null || !Finite(data.origin.longitude) || !Finite(data.origin.latitude)
                || Math.Abs(data.origin.longitude + .1589) > .0000001 || Math.Abs(data.origin.latitude - 51.4905) > .0000001)
                throw new InvalidDataException("Document must use the fixed Chelsea computational origin.");
            if (source == null || source.source == null || !Hash(source.source.sha256) || !Hash(source.boundarySha256)
                || string.IsNullOrWhiteSpace(source.source.sourceUrl) || string.IsNullOrWhiteSpace(source.source.snapshotDate))
                throw new InvalidDataException("Retained source URL, snapshot date and hashes are required.");
            string expectedLicence = data.datasetKind == "osm-source" ? "ODbL-1.0" : "CC0-1.0";
            if (source.source.licence != expectedLicence) throw new InvalidDataException("Source licence does not match the dataset kind.");
            if (!DateTime.TryParseExact(source.source.snapshotDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _)) throw new InvalidDataException("Snapshot date must be ISO yyyy-MM-dd.");
            if (data.datasetKind == "osm-source" && (!Uri.TryCreate(source.source.sourceUrl, UriKind.Absolute, out Uri sourceUri)
                || sourceUri.Scheme != "https")) throw new InvalidDataException("OSM source requires its retained HTTPS source URL.");
            if (data.meshes == null || data.meshes.Length == 0) throw new InvalidDataException("No source meshes; an empty scene is not a reconstruction.");
            long triangleCount = 0;
            foreach (GeographicMesh mesh in data.meshes)
            {
                if (mesh == null || string.IsNullOrWhiteSpace(mesh.sourceId) || string.IsNullOrWhiteSpace(mesh.kind)
                    || mesh.vertices == null || mesh.vertices.Length < 3 || mesh.triangles == null || mesh.triangles.Length < 3
                    || mesh.triangles.Length % 3 != 0) throw new InvalidDataException("Malformed source mesh.");
                foreach (Vector3 point in mesh.vertices)
                    if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z) || Math.Abs(point.x) > 5000
                        || Math.Abs(point.z) > 5000 || Math.Abs(point.y) > 500)
                        throw new InvalidDataException("Mesh vertex is invalid or outside the first-area domain.");
                foreach (int index in mesh.triangles)
                    if (index < 0 || index >= mesh.vertices.Length) throw new InvalidDataException("Mesh index is outside its vertex array.");
                for (int i = 0; i < mesh.triangles.Length; i += 3)
                {
                    Vector3 a = mesh.vertices[mesh.triangles[i]], b = mesh.vertices[mesh.triangles[i + 1]], c = mesh.vertices[mesh.triangles[i + 2]];
                    if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-12f) throw new InvalidDataException("Degenerate mesh triangle.");
                }
                triangleCount += mesh.triangles.Length / 3;
            }
            if (triangleCount > TriangleBudget) throw new InvalidDataException("Document exceeds initial 1.3M diagnostic triangle budget.");
        }

        private void BuildWorld()
        {
            WorldRoot = new GameObject("Tabletop world — viewing transform only").transform;
            WorldRoot.SetParent(transform, false);
            geometryRoot = new GameObject("Original metric geometry").transform;
            geometryRoot.SetParent(WorldRoot, false);
            var buildingMaterial = OwnedMaterial(new Color(.60f, .46f, .34f), "Diagnostic footprint material");
            var roadMaterial = OwnedMaterial(new Color(.73f, .55f, .22f), "Diagnostic centreline material");
            var otherMaterial = OwnedMaterial(new Color(.45f, .56f, .49f), "Diagnostic source material");
            highlightMaterial = OwnedMaterial(new Color(.30f, .77f, .64f), "Selected source highlight");
            Bounds bounds = new Bounds(document.meshes[0].vertices[0], Vector3.zero);
            foreach (GeographicMesh source in document.meshes)
            {
                var mesh = new Mesh { name = source.sourceId,
                    indexFormat = source.vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                    vertices = source.vertices, triangles = source.triangles };
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                ownedMeshes.Add(mesh);
                var feature = new GameObject(source.sourceId);
                feature.transform.SetParent(geometryRoot, false);
                feature.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = feature.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = source.kind.Contains("building") ? buildingMaterial : source.kind.Contains("road") ? roadMaterial : otherMaterial;
                originalMaterials.Add(renderer, renderer.sharedMaterial);
                if (!features.TryGetValue(source.sourceId, out List<MeshRenderer> renderers))
                    features.Add(source.sourceId, renderers = new List<MeshRenderer>());
                renderers.Add(renderer);
                // Enlarged selection boxes are interaction aids, not geographic geometry or walkable surfaces.
                var selection = feature.AddComponent<BoxCollider>();
                selection.center = mesh.bounds.center;
                Vector3 hitSize = mesh.bounds.size;
                selection.size = new Vector3(Mathf.Max(hitSize.x, .6f), Mathf.Max(hitSize.y, .6f), Mathf.Max(hitSize.z, .6f));
                feature.AddComponent<ExperienceSelectionTarget>().Configure(this, source.sourceId);
                foreach (Vector3 vertex in source.vertices) bounds.Encapsulate(vertex);
            }
            geographicWidth = Mathf.Max(bounds.size.x, bounds.size.z, 1f);
            geometryRoot.localPosition = -new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            WorldRoot.localScale = Vector3.one * (settings.tabletopWidthMetres / geographicWidth);
        }

        public bool TryPlace(Pose pose, float tabletopWidthMetres)
        {
            if (!HasData || !Finite(pose.position.x) || !Finite(pose.position.y) || !Finite(pose.position.z)
                || !Finite(pose.rotation.x) || !Finite(pose.rotation.y) || !Finite(pose.rotation.z) || !Finite(pose.rotation.w)
                || !Finite(tabletopWidthMetres)
                || pose.rotation.x * pose.rotation.x + pose.rotation.y * pose.rotation.y + pose.rotation.z * pose.rotation.z + pose.rotation.w * pose.rotation.w < 1e-8f) return false;
            WorldRoot.SetPositionAndRotation(pose.position, Quaternion.Euler(0, pose.rotation.eulerAngles.y, 0));
            settings.tabletopYawDegrees = WorldRoot.eulerAngles.y;
            RequestSettingsSave();
            SetTabletopWidth(tabletopWidthMetres);
            return true;
        }

        public void MoveTabletop(Vector3 deltaWorldMetres)
        {
            if (!HasData || IsInteractionPaused || !Finite(deltaWorldMetres.x) || !Finite(deltaWorldMetres.y) || !Finite(deltaWorldMetres.z)) return;
            WorldRoot.position += deltaWorldMetres;
        }

        public void RotateTabletop(float degrees)
        {
            if (!HasData || IsInteractionPaused || !Finite(degrees)) return;
            settings.tabletopYawDegrees = Mathf.Repeat(settings.tabletopYawDegrees + degrees, 360f);
            WorldRoot.rotation = Quaternion.Euler(0, settings.tabletopYawDegrees, 0);
            RequestSettingsSave();
            RefreshMenu();
        }

        public void SetTabletopWidth(float metres)
        {
            if (settings == null || !Finite(metres)) return;
            float width = Mathf.Clamp(metres, .3f, 4f);
            if (Mathf.Abs(settings.tabletopWidthMetres - width) < .0001f && WorldRoot != null) return;
            settings.tabletopWidthMetres = width;
            if (WorldRoot != null) WorldRoot.localScale = Vector3.one * (settings.tabletopWidthMetres / geographicWidth);
            RequestSettingsSave();
            RefreshMenu();
        }

        /// <summary>Always reachable via a mapped return action. No street transition exists yet.</summary>
        public void ReturnToTabletop()
        {
            if (WorldRoot != null)
            {
                Vector3 forward = FlatForward();
                Vector3 position = HeadPosition() + forward * 1.2f;
                position.y = Mathf.Clamp(HeadPosition().y - .65f, .55f, 1.1f);
                WorldRoot.SetPositionAndRotation(position, Quaternion.Euler(0, settings.tabletopYawDegrees, 0));
            }
            RecenterMenu();
            RefreshMenu();
        }

        public void RecenterMenu()
        {
            if (menuRoot == null) return;
            Vector3 forward = FlatForward();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 position = HeadPosition() + forward * 1.15f - right * .9f;
            position.y = Mathf.Clamp(HeadPosition().y - .12f, .8f, 1.8f);
            Vector3 facing = position - HeadPosition();
            facing.y = 0;
            menuRoot.SetPositionAndRotation(position, Quaternion.LookRotation(facing.normalized, Vector3.up));
        }

        public void SelectSource(string sourceId)
        {
            if (IsInteractionPaused || !HasData || string.IsNullOrWhiteSpace(sourceId) || !features.ContainsKey(sourceId)) return;
            if (selectedSource != null && features.TryGetValue(selectedSource, out List<MeshRenderer> previous))
                foreach (MeshRenderer renderer in previous) renderer.sharedMaterial = originalMaterials[renderer];
            selectedSource = sourceId;
            foreach (MeshRenderer renderer in features[sourceId]) renderer.sharedMaterial = highlightMaterial;
            Status = "Selected " + sourceId + ". Diagnostic source feature, not a completed location.";
            RefreshMenu();
        }

        public void SetTracking(bool isTracked)
        {
            if (tracked == isTracked) return;
            tracked = isTracked;
            RefreshMenu();
        }

        public void Activate(ExperienceMenuAction action)
        {
            if (action == ExperienceMenuAction.Return) { ReturnToTabletop(); return; }
            if (IsInteractionPaused) return;
            switch (action)
            {
                case ExperienceMenuAction.RotateLeft: RotateTabletop(-15); break;
                case ExperienceMenuAction.RotateRight: RotateTabletop(15); break;
                case ExperienceMenuAction.Smaller: SetTabletopWidth(TabletopWidthMetres - .15f); break;
                case ExperienceMenuAction.Larger: SetTabletopWidth(TabletopWidthMetres + .15f); break;
                case ExperienceMenuAction.Day: ApplyLighting(LightingPreset.Day); break;
                case ExperienceMenuAction.Overcast: ApplyLighting(LightingPreset.Overcast); break;
                case ExperienceMenuAction.Dusk: ApplyLighting(LightingPreset.Dusk); break;
                case ExperienceMenuAction.Sources: showSources = !showSources; RefreshMenu(); break;
            }
        }

        public void ApplyLighting(LightingPreset preset) => ApplyLighting(preset, true);

        private void ApplyLighting(LightingPreset preset, bool save)
        {
            if (settings == null || !Enum.IsDefined(typeof(LightingPreset), preset)) return;
            settings.lightingPreset = (int)preset;
            Color ambient = preset == LightingPreset.Dusk ? new Color(.20f, .23f, .31f)
                : preset == LightingPreset.Overcast ? new Color(.60f, .62f, .63f) : new Color(.55f, .61f, .63f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            if (sun != null)
            {
                sun.color = preset == LightingPreset.Dusk ? new Color(1f, .65f, .40f) : Color.white;
                sun.intensity = preset == LightingPreset.Dusk ? .65f : preset == LightingPreset.Overcast ? .55f : 1.2f;
                sun.transform.rotation = Quaternion.Euler(preset == LightingPreset.Dusk ? 12 : 48, -25, 0);
            }
            if (save) RequestSettingsSave();
            RefreshMenu();
        }

        private void BuildMenu()
        {
            menuRoot = new GameObject("Persistent developer menu — return and evidence").transform;
            menuRoot.SetParent(transform, false);
            Material panelMaterial = OwnedMaterial(new Color(.06f, .13f, .12f), "Developer menu panel");
            if (panelMaterial.HasProperty("_Cull")) panelMaterial.SetFloat("_Cull", 0);
            Panel(menuRoot, new Vector3(0, -.20f, .025f), new Vector3(.94f, .77f, 1), panelMaterial);
            title = Label("Title", new Vector3(-.43f, .135f, -.006f), .045f, Color.white);
            statusText = Label("Status", new Vector3(-.43f, .055f, -.006f), .026f, new Color(.82f, .9f, .86f));
            sourceText = Label("Source and limitations", new Vector3(-.43f, -.35f, -.006f), .022f, new Color(.77f, .85f, .81f));
            AddButton("Return / recenter", ExperienceMenuAction.Return, -.30f, -.14f, .26f);
            AddButton("Rotate -15", ExperienceMenuAction.RotateLeft, 0, -.14f, .24f);
            AddButton("Rotate +15", ExperienceMenuAction.RotateRight, .29f, -.14f, .24f);
            AddButton("Smaller", ExperienceMenuAction.Smaller, -.30f, -.205f, .26f);
            AddButton("Larger", ExperienceMenuAction.Larger, 0, -.205f, .24f);
            AddButton("Sources", ExperienceMenuAction.Sources, .29f, -.205f, .24f);
            AddButton("Day", ExperienceMenuAction.Day, -.30f, -.27f, .26f);
            AddButton("Overcast", ExperienceMenuAction.Overcast, 0, -.27f, .24f);
            AddButton("Dusk", ExperienceMenuAction.Dusk, .29f, -.27f, .24f);
            RecenterMenu();
        }

        private void RefreshMenu() => menuDirty = true;

        private void RenderMenu()
        {
            if (title == null) return;
            title.text = IsSynthetic ? "SYNTHETIC FIXTURE — NOT CHELSEA" : HasData ? "CHELSEA — DIAGNOSTIC SOURCE VIEW" : "CHELSEA — SOURCE DATA UNAVAILABLE";
            string pause = !focused ? "PAUSED: application focus lost." : !tracked ? "PAUSED: tracking unavailable. Return remains mapped." : Status;
            statusText.text = Wrap(pause, 62) + "\nTable width " + TabletopWidthMetres.ToString("0.00", CultureInfo.InvariantCulture)
                + " m  |  " + ((LightingPreset)settings.lightingPreset) + "\nStreet mode blocked: no reviewed public walking targets.";
            if (showSources && provenance != null && provenance.source != null)
                sourceText.text = "SOURCE RECORD (bundled JSON retained)\n" + Wrap(provenance.source.sourceUrl, 70)
                    + "\n" + provenance.source.licence + " | " + provenance.source.snapshotDate
                    + "\nSHA-256 " + provenance.source.sha256.Substring(0, 24) + "..."
                    + "\n" + (IsSynthetic ? "Original synthetic tests. No real Chelsea geography." : "© OpenStreetMap contributors | openstreetmap.org/copyright")
                    + "\nPublic access, facade fidelity and device performance unverified.";
            else
                sourceText.text = "DEVELOPER INSPECTION — NOT A FINISHED VR RELEASE\n"
                    + "Trigger/click selects; grips move/scale via input adapter.\n"
                    + "Road ribbons are diagnostic, not walkable pavements.\n"
                    + "Heights are source claims/estimates, not surveyed evidence.\n"
                    + "No passthrough, anchors, hands or safe street mode claim.";
        }

        private void AddButton(string caption, ExperienceMenuAction action, float x, float y, float width)
        {
            var button = new GameObject(caption);
            button.transform.SetParent(menuRoot, false);
            button.transform.localPosition = new Vector3(x, y, 0);
            Material material = OwnedMaterial(new Color(.17f, .29f, .25f), "Menu " + action);
            Panel(button.transform, new Vector3(0, 0, .006f), new Vector3(width, .052f, 1), material);
            var collider = button.AddComponent<BoxCollider>();
            collider.size = new Vector3(width, .052f, .025f);
            button.AddComponent<ExperienceActionTarget>().Configure(this, action);
            TextMesh label = NewText(button.transform, "Caption", .029f, Color.white);
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.transform.localPosition = new Vector3(0, 0, -.009f);
            label.text = caption;
        }

        private TextMesh Label(string name, Vector3 position, float size, Color color)
        {
            TextMesh label = NewText(menuRoot, name, size, color);
            label.transform.localPosition = position;
            return label;
        }

        private static TextMesh NewText(Transform parent, string name, float size, Color color)
        {
            var text = new GameObject(name);
            text.transform.SetParent(parent, false);
            var label = text.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 48;
            label.characterSize = size;
            label.anchor = TextAnchor.UpperLeft;
            label.alignment = TextAlignment.Left;
            label.color = color;
            text.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            return label;
        }

        private void Panel(Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panel.name = "Visual background (not a selectable surface)";
            panel.transform.SetParent(parent, false);
            panel.transform.localPosition = position;
            panel.transform.localScale = scale;
            panel.GetComponent<MeshRenderer>().sharedMaterial = material;
            Destroy(panel.GetComponent<Collider>());
        }

        private Material OwnedMaterial(Color color, string name)
        {
            Material material;
            if (baseMaterial != null) material = new Material(baseMaterial);
            else
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) throw new InvalidOperationException("URP/Unlit shader is unavailable for developer menu.");
                material = new Material(shader);
            }
            material.name = name;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            ownedMaterials.Add(material);
            return material;
        }

        private Vector3 HeadPosition() => head != null ? head.position : new Vector3(0, 1.6f, 0);
        private Vector3 FlatForward()
        {
            Vector3 forward = head != null ? head.forward : Vector3.forward;
            forward.y = 0;
            return forward.sqrMagnitude > .01f ? forward.normalized : Vector3.forward;
        }

        private void RequestSettingsSave()
        {
            settingsDirty = true;
            saveAfter = Time.unscaledTime + 1.5f;
        }

        private void FlushSettings()
        {
            if (!settingsDirty || settings == null || string.IsNullOrEmpty(settingsPath)) return;
            ExperienceSettingsStore.Save(settingsPath, settings);
            settingsDirty = false;
        }

        private void LateUpdate()
        {
            // No per-feature loops or I/O during continuous manipulation. Text refresh
            // is coalesced, and a settings write occurs only after an idle interval.
            float now = Time.unscaledTime;
            if (menuDirty && now >= renderMenuAfter)
            {
                RenderMenu();
                menuDirty = false;
                renderMenuAfter = now + .25f;
            }
            if (settingsDirty && now >= saveAfter) FlushSettings();
        }

        private void OnApplicationFocus(bool hasFocus) { focused = hasFocus; RefreshMenu(); }
        private void OnApplicationPause(bool pause) { if (pause) FlushSettings(); }
        private void OnApplicationQuit() => FlushSettings();

        private void OnDestroy()
        {
            FlushSettings();
            foreach (Mesh mesh in ownedMeshes) if (mesh != null) Destroy(mesh);
            foreach (Material material in ownedMaterials) if (material != null) Destroy(material);
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool Hash(string text)
        {
            if (text == null || text.Length != 64) return false;
            foreach (char c in text) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return false;
            return true;
        }
        private static string Wrap(string text, int length)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var result = new StringBuilder();
            int line = 0;
            foreach (string word in text.Split(' '))
            {
                if (line > 0 && line + word.Length + 1 > length) { result.Append('\n'); line = 0; }
                if (line > 0) { result.Append(' '); line++; }
                int start = 0;
                while (start < word.Length)
                {
                    int count = Math.Min(length - line, word.Length - start);
                    result.Append(word, start, count);
                    start += count;
                    line += count;
                    if (start < word.Length) { result.Append('\n'); line = 0; }
                }
            }
            return result.ToString();
        }

        [Serializable] public sealed class SourceEnvelope
        {
            public ProvenanceSource source;
            public string boundarySha256;
        }
        [Serializable] public sealed class ProvenanceSource
        {
            public string sourceUrl;
            public string licence;
            public string snapshotDate;
            public string sha256;
        }
    }
}
