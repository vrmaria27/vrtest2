using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Chelsea.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Chelsea.Editor
{
    /// <summary>Imports deterministic CLI output; never invents missing geography.</summary>
    public static class GeographicImportMenu
    {
        private const string GeneratedRoot = "Assets/Chelsea/Generated";
        private const int PeakTriangleBudget = 1300000;

        [MenuItem("Chelsea/Create inspection URP configuration")]
        public static void CreateInspectionPipeline()
        {
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                EditorUtility.DisplayDialog("Render pipeline already configured",
                    "Existing Graphics pipeline preserved. Verify it is URP and check Quality overrides before importing.", "OK");
                return;
            }
            EnsureGeneratedRoot();
            string directory = AssetDatabase.GenerateUniqueAssetPath(GeneratedRoot + "/Rendering");
            AssetDatabase.CreateFolder(GeneratedRoot, Path.GetFileName(directory));
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, directory + "/InspectionRenderer.asset");
            // Public Editor factory verified against pinned Unity Graphics branch source.
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, directory + "/InspectionPipeline.asset");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            Selection.activeObject = pipeline;
            Debug.Log("Inspection URP asset created. Verify Forward renderer, current Quality override, and rendering in the pinned Editor.");
        }

        [MenuItem("Chelsea/Import geographic JSON for inspection…")]
        public static void ChooseDocument()
        {
            string path = EditorUtility.OpenFilePanel("Select generated geographic JSON", "../data/processed", "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                // Validate before creating a scene or assets. Empty sources never become a mock city.
                byte[] sourceJsonBytes = File.ReadAllBytes(path);
                string json = new UTF8Encoding(false, true).GetString(sourceJsonBytes).TrimStart('\uFEFF');
                GeographicDocument document = JsonUtility.FromJson<GeographicDocument>(json);
                Validate(document);
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidDataException("URP/Lit shader unavailable. Restore the pinned URP package first.");
                RenderPipelineAsset pipeline = QualitySettings.renderPipeline != null
                    ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;
                if (!(pipeline is UniversalRenderPipelineAsset))
                    throw new InvalidDataException("Configure a URP asset in Graphics/Quality first; see unity/README.md.");
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Import(document, path, sourceJsonBytes, shader);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Geographic import failed", exception.Message, "OK");
            }
        }

        private static void Validate(GeographicDocument document)
        {
            if (document == null || document.schemaVersion != 1)
                throw new InvalidDataException("Expected geographic JSON schemaVersion 1.");
            if (document.datasetKind != "synthetic-test-fixture" && document.datasetKind != "osm-source")
                throw new InvalidDataException("Unknown datasetKind: provenance must be explicit.");
            if (document.origin == null || !Finite(document.origin.longitude) || !Finite(document.origin.latitude)
                || Math.Abs(document.origin.longitude) > 180 || Math.Abs(document.origin.latitude) > 90)
                throw new InvalidDataException("Missing or invalid WGS84 origin.");
            if (document.meshes == null || document.meshes.Length == 0)
                throw new InvalidDataException("No meshes. Supply a valid extract; no substitute geometry is generated.");

            long triangles = 0;
            foreach (GeographicMesh mesh in document.meshes)
            {
                if (mesh == null || string.IsNullOrWhiteSpace(mesh.sourceId) || string.IsNullOrWhiteSpace(mesh.kind))
                    throw new InvalidDataException("Each mesh needs a sourceId and kind.");
                if (mesh.vertices == null || mesh.vertices.Length < 3 || mesh.triangles == null
                    || mesh.triangles.Length < 3 || mesh.triangles.Length % 3 != 0)
                    throw new InvalidDataException("Malformed mesh: " + mesh.sourceId);
                foreach (Vector3 vertex in mesh.vertices)
                    if (!Finite(vertex.x) || !Finite(vertex.y) || !Finite(vertex.z))
                        throw new InvalidDataException("Non-finite vertex in " + mesh.sourceId);
                foreach (int index in mesh.triangles)
                    if (index < 0 || index >= mesh.vertices.Length)
                        throw new InvalidDataException("Out-of-range triangle index in " + mesh.sourceId);
                for (int i = 0; i < mesh.triangles.Length; i += 3)
                {
                    Vector3 a = mesh.vertices[mesh.triangles[i]];
                    Vector3 b = mesh.vertices[mesh.triangles[i + 1]];
                    Vector3 c = mesh.vertices[mesh.triangles[i + 2]];
                    if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-12f)
                        throw new InvalidDataException("Degenerate triangle in " + mesh.sourceId);
                }
                triangles += mesh.triangles.Length / 3;
            }
            if (triangles > PeakTriangleBudget)
                throw new InvalidDataException($"{triangles:N0} triangles exceed the initial 1.3 M peak budget. Reduce source detail explicitly.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static void EnsureGeneratedRoot()
        {
            if (!AssetDatabase.IsValidFolder(GeneratedRoot))
                AssetDatabase.CreateFolder("Assets/Chelsea", "Generated");
        }

        private static void Import(GeographicDocument document, string path, byte[] sourceJsonBytes, Shader shader)
        {
            EnsureGeneratedRoot();
            string directory = AssetDatabase.GenerateUniqueAssetPath(GeneratedRoot + "/Inspection");
            AssetDatabase.CreateFolder(GeneratedRoot, Path.GetFileName(directory));
            // Preserve the complete original bytes, including fields not mapped to the render DTO.
            // This retains source/manifest hashes, boundary hash, source coordinates, warnings,
            // and per-building height evidence beside the scene instead of silently dropping them.
            string retainedPath = directory + "/ImportedDocument.json";
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            File.WriteAllBytes(Path.Combine(projectRoot, retainedPath), sourceJsonBytes);
            AssetDatabase.ImportAsset(retainedPath, ImportAssetOptions.ForceSynchronousImport);
            TextAsset retainedDocument = AssetDatabase.LoadAssetAtPath<TextAsset>(retainedPath);
            if (retainedDocument == null)
                throw new IOException("Could not import the retained provenance document: " + retainedPath);
            string documentSha256;
            using (SHA256 sha256 = SHA256.Create())
                documentSha256 = BitConverter.ToString(sha256.ComputeHash(sourceJsonBytes)).Replace("-", "").ToLowerInvariant();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(document.datasetKind == "synthetic-test-fixture"
                ? "SYNTHETIC FIXTURE — NOT CHELSEA" : "OSM geometry — M1 inspection only");
            root.AddComponent<ImportNotice>().Configure(document, Path.GetFileName(path), retainedDocument, documentSha256);

            var materials = document.meshes.Select(mesh => mesh.kind).Distinct().OrderBy(kind => kind)
                .ToDictionary(kind => kind, kind => CreateMaterial(kind, directory, shader));
            Bounds bounds = new Bounds(document.meshes[0].vertices[0], Vector3.zero);
            for (int i = 0; i < document.meshes.Length; i++)
            {
                GeographicMesh source = document.meshes[i];
                var mesh = new Mesh
                {
                    name = source.sourceId,
                    indexFormat = source.vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                    vertices = source.vertices,
                    triangles = source.triangles
                };
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                AssetDatabase.CreateAsset(mesh, $"{directory}/mesh-{i:D5}.asset");
                var feature = new GameObject(source.sourceId);
                feature.transform.SetParent(root.transform, false);
                feature.AddComponent<MeshFilter>().sharedMesh = mesh;
                feature.AddComponent<MeshRenderer>().sharedMaterial = materials[source.kind];
                // No colliders: source geometry is not verified public walkable space.
                foreach (Vector3 vertex in source.vertices) bounds.Encapsulate(vertex);
            }

            var cameraObject = new GameObject("Inspection camera (no XR)");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.orthographic = true;
            float radius = Mathf.Max(bounds.size.magnitude * .5f, 10f);
            camera.orthographicSize = radius * .9f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = radius * 10f;
            camera.backgroundColor = new Color(.78f, .8f, .81f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.transform.position = bounds.center + new Vector3(0, radius * 2, -radius * 1.7f);
            cameraObject.transform.LookAt(bounds.center, Vector3.up);
            var light = new GameObject("Inspection directional light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(48, -25, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.45f, .45f, .45f);
            string scenePath = directory + "/GeographicInspection.unity";
            if (!EditorSceneManager.SaveScene(scene, scenePath))
                throw new IOException("Could not save inspection scene: " + scenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.Frame(new Bounds(bounds.center, Vector3.one * radius * 2), false);
            Debug.Log($"Imported {document.meshes.Length} meshes to {scenePath}. Dataset: {document.datasetKind}. "
                + "No Unity/device test or geographic accuracy claim is established by import.");
        }

        private static Material CreateMaterial(string kind, string directory, Shader shader)
        {
            var material = new Material(shader) { name = "inspection-" + kind };
            Color color = kind.Contains("building") ? new Color(.61f, .52f, .44f)
                : kind.Contains("pavement") ? new Color(.65f, .64f, .6f)
                : kind.Contains("road") ? new Color(.23f, .24f, .24f)
                : new Color(.5f, .52f, .51f);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .05f);
            string safeName = new string(kind.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(directory + "/material-" + safeName + ".mat");
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }
    }
}
