using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Chelsea.Input;
using Chelsea.Runtime;
using Chelsea.World;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace Chelsea.Editor
{
    /// <summary>Explicit diagnostic build, never a release or real-Chelsea acceptance claim.</summary>
    public static class AlphaBuild
    {
        private const string FixturePath = "Assets/Chelsea/Fixtures/SyntheticDiagnostic.json";
        private const string GeneratedRoot = "Assets/Chelsea/Generated";
        private const string ApplicationId = "com.holopolis.chelsea.alpha";

        [MenuItem("Chelsea/Quest alpha/1 Create diagnostic launch scene")]
        public static void PrepareDiagnostic()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string scene = CreateDiagnosticScene();
            Debug.Log("Diagnostic scene saved: " + scene + ". Press Play for desktop inspection. This is NOT Chelsea. "
                + "Configure Android OpenXR, Meta Quest support and Oculus Touch before an APK build. See docs/LAUNCH_MAC.md.");
        }

        [MenuItem("Chelsea/Quest alpha/2 Check Android OpenXR configuration")]
        public static void CheckAndroidConfiguration()
        {
            ValidateQuestConfiguration();
            Debug.Log("Android loader/profile configuration checks passed. This does not verify a build or device.");
        }

        [MenuItem("Chelsea/Quest alpha/3 Build diagnostic APK")]
        public static void BuildDiagnostic()
        {
            string staging = null;
            try
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                    throw new InvalidOperationException("Install Android Build Support, Android SDK & NDK Tools, and OpenJDK for the pinned Editor in Unity Hub.");
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    throw new InvalidOperationException("Switch the active Build Profile to Android first, or use the Mac build script (-buildTarget Android).");
                ConfigureAndroidPlayer();
                ValidateQuestConfiguration();
                string scene = CreateDiagnosticScene();
                string requestedOutput = Argument("-chelseaOutput");
                string output = string.IsNullOrWhiteSpace(requestedOutput)
                    ? Path.GetFullPath("Builds/Chelsea-Diagnostic-Alpha.apk") : Path.GetFullPath(requestedOutput);
                if (!output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Diagnostic output must end in .apk.");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                staging = Path.Combine(Path.GetDirectoryName(output), "Chelsea-staging-" + Guid.NewGuid().ToString("N") + ".apk");
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { scene }, locationPathName = staging, target = BuildTarget.Android,
                    options = BuildOptions.Development
                });
                if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0 || !File.Exists(staging))
                    throw new InvalidOperationException("Unity diagnostic build failed. Result: " + report.summary.result + ". Inspect the complete Editor log.");
                if (new FileInfo(staging).Length >= 500L * 1024 * 1024)
                    throw new InvalidOperationException("Diagnostic APK exceeds the initial 500 MB budget; inspect contents before installation.");
                if (File.Exists(output)) File.Replace(staging, output, null); else File.Move(staging, output);
                staging = null;
                var evidence = new DiagnosticBuildEvidence
                {
                    runId = Argument("-chelseaRunId") ?? Guid.NewGuid().ToString("N"),
                    diagnostic = true, datasetKind = "synthetic-test-fixture", unityVersion = Application.unityVersion,
                    builtAtUtc = DateTime.UtcNow.ToString("O"), apkBytes = new FileInfo(output).Length,
                    apkSha256 = Sha256(File.ReadAllBytes(output)), deviceTest = "NOT RUN by this build method",
                    notes = "Synthetic controller diagnostic only. Not Chelsea; no street, hands, MR, accuracy, comfort or performance acceptance."
                };
                File.WriteAllText(output + ".build.json", JsonUtility.ToJson(evidence, true));
                Debug.Log("CHELSEA_BUILD_RUN=" + evidence.runId + "\nCHELSEA_DIAGNOSTIC_APK=" + output + "\nSHA-256=" + evidence.apkSha256
                    + "\nBuild succeeded. Device verification and real-Chelsea content remain separate gates.");
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
            finally
            {
                if (staging != null && File.Exists(staging)) File.Delete(staging);
            }
        }

        private static string CreateDiagnosticScene()
        {
            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(FixturePath);
            if (source == null) throw new InvalidDataException("Bundled diagnostic fixture missing: " + FixturePath);
            GeographicDocument document = JsonUtility.FromJson<GeographicDocument>(source.text);
            ChelseaExperience.ValidateDocument(document, JsonUtility.FromJson<ChelseaExperience.SourceEnvelope>(source.text));
            if (document.datasetKind != "synthetic-test-fixture") throw new InvalidDataException("Diagnostic build accepts only the explicitly synthetic fixture.");
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (lit == null || unlit == null) throw new InvalidOperationException("Restore the pinned URP package before creating the scene.");
            if (!AssetDatabase.IsValidFolder(GeneratedRoot)) AssetDatabase.CreateFolder("Assets/Chelsea", "Generated");
            string directory = AssetDatabase.GenerateUniqueAssetPath(GeneratedRoot + "/QuestAlpha");
            AssetDatabase.CreateFolder(GeneratedRoot, Path.GetFileName(directory));
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, directory + "/AlphaRenderer.asset");
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.msaaSampleCount = 4;
            pipeline.supportsHDR = false;
            pipeline.shadowDistance = 20;
            AssetDatabase.CreateAsset(pipeline, directory + "/AlphaPipeline.asset");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            var material = new Material(lit) { name = "Diagnostic source material", enableInstancing = true };
            material.SetFloat("_Smoothness", .05f);
            AssetDatabase.CreateAsset(material, directory + "/SourceMaterial.mat");
            var rayMaterial = new Material(unlit) { name = "Controller ray", enableInstancing = true };
            rayMaterial.SetColor("_BaseColor", new Color(.4f, .85f, .7f));
            AssetDatabase.CreateAsset(rayMaterial, directory + "/ControllerRay.mat");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rig = new GameObject("Tracking origin — no artificial locomotion");
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(rig.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 1.6f, 0);
            Camera head = cameraObject.AddComponent<Camera>();
            head.nearClipPlane = .05f;
            head.farClipPlane = 100;
            head.stereoTargetEye = StereoTargetEyeMask.Both;
            head.clearFlags = CameraClearFlags.SolidColor;
            head.backgroundColor = new Color(.075f, .12f, .115f);
            var sun = new GameObject("Single directional light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48, -25, 0);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Viewing room floor — NOT Chelsea terrain or teleport surface";
            floor.transform.position = new Vector3(0, -.025f, 0);
            floor.transform.localScale = new Vector3(.6f, 1, .6f);
            UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            var floorMaterial = new Material(material) { name = "Viewing room floor" };
            floorMaterial.SetColor("_BaseColor", new Color(.15f, .20f, .18f));
            AssetDatabase.CreateAsset(floorMaterial, directory + "/Floor.mat");
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;
            var app = new GameObject("Chelsea — synthetic diagnostic alpha").AddComponent<ChelseaExperience>();
            app.Configure(source, material, head.transform, sun);
            rig.AddComponent<ChelseaControllerInput>().Configure(app, head, rayMaterial);
            string scenePath = directory + "/QuestDiagnostic.unity";
            if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException("Could not save diagnostic scene.");
            AssetDatabase.SaveAssets();
            return scenePath;
        }

        private static void ConfigureAndroidPlayer()
        {
            PlayerSettings.companyName = "Holopolis";
            PlayerSettings.productName = "Chelsea — SYNTHETIC diagnostic";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EditorUserBuildSettings.buildAppBundle = false;
        }

        private static void ValidateQuestConfiguration()
        {
            XRGeneralSettings general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (general == null || general.Manager == null || !general.InitManagerOnStart
                || !general.Manager.activeLoaders.Any(loader => loader != null && loader.GetType().FullName == "UnityEngine.XR.OpenXR.OpenXRLoader"))
                throw new InvalidOperationException("Enable OpenXR under XR Plug-in Management > Android, with Initialize XR on Startup. See docs/LAUNCH_MAC.md.");
            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null) throw new InvalidOperationException("Android OpenXR settings are missing; configure them in the pinned Editor first.");
            OculusTouchControllerProfile touch = settings.GetFeature<OculusTouchControllerProfile>();
            MetaQuestFeature quest = settings.GetFeature<MetaQuestFeature>();
            if (touch == null || !touch.enabled || quest == null || !quest.enabled)
                throw new InvalidOperationException("Enable the Oculus Touch Controller interaction profile and Meta Quest Support for Android OpenXR. Select your actual Quest model in the feature UI.");
            if (settings.renderMode != OpenXRSettings.RenderMode.SinglePassInstanced)
                throw new InvalidOperationException("Set Android OpenXR Render Mode to Single Pass Instanced before building.");
#if !ENABLE_INPUT_SYSTEM
            throw new InvalidOperationException("Set Player > Other Settings > Active Input Handling to Input System Package (New), accept the Editor restart, then retry. Legacy desktop controls are optional.");
#endif
        }

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }
        private static string Sha256(byte[] bytes)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        [Serializable] private sealed class DiagnosticBuildEvidence
        {
            public string runId;
            public bool diagnostic;
            public string datasetKind;
            public string unityVersion;
            public string builtAtUtc;
            public long apkBytes;
            public string apkSha256;
            public string deviceTest;
            public string notes;
        }
    }
}
