using System;
using System.IO;
using System.Linq;
using DepthAnything.Samples;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DepthPackageTools
{
    const string BasicPath = "Assets/DepthDemo/DepthDemo.unity";
    const string BodyPath = "Assets/BodyPixDemo/BodyPixDemo.unity";

    [MenuItem("Depth Anything/Rebuild demo scene")]
    public static void Prepare()
    {
        PlayerSettings.companyName = "DepthAnythingUnity";
        PlayerSettings.productName = "Depth Anything V2 Demo";
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.defaultScreenWidth = 1440;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        var image = AssetImporter.GetAtPath("Assets/DepthDemo/Waterfall.jpg") as TextureImporter;
        image.sRGBTexture = true; image.mipmapEnabled = false; image.textureCompression = TextureImporterCompression.Uncompressed;
        image.npotScale = TextureImporterNPOTScale.None;
        image.maxTextureSize = 4096; image.SaveAndReimport();
        CreateScene(BasicPath, false);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BasicPath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("DEPTH_PACKAGE_PREPARED");
    }

    public static void PrepareBodyPix()
    {
        Directory.CreateDirectory("Assets/BodyPixDemo");
        CreateScene(BodyPath, true);
        AssetDatabase.SaveAssets();
        Debug.Log("DEPTH_BODYPIX_PREPARED");
    }

    static void CreateScene(string path, bool body)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.transform.position = new Vector3(0, 0, -10);
        var go = new GameObject(body ? "BodyPix + Depth Anything" : "Depth Anything");
        var demo = go.AddComponent<DepthDemo>();
        demo.sampleImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/DepthDemo/Waterfall.jpg");
        demo.displayShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/DepthDemo/DepthDisplay.shader");
        demo.reliefShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/DepthDemo/DepthRelief.shader");
        demo.overviewShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/DepthDemo/DepthOverview.shader");
        // Start full-scene relief at a modest angle: railing/background gaps are unobserved.
        if (!body) { demo.reliefStrength = .35f; demo.orbit = -6; }
        if (body)
        {
            var type = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(t => t.FullName == "DepthAnything.Samples.BodyPixDepth");
            if (type == null) throw new InvalidOperationException("Install BodyPix and import its sample before preparing this scene.");
            var adapter = go.AddComponent(type);
            var serialized = new SerializedObject(adapter);
            serialized.FindProperty("resources").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Packages/jp.keijiro.bodypix/ResourceSet/MobileNetV1-x100-stride8.asset");
            serialized.FindProperty("maskShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>("Assets/BodyPixDemo/BodyPixMask.shader");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.SaveScene(scene, path);
    }

    public static void BuildBasic() { Build(BasicPath, "Builds/Basic/DepthDemo.exe"); }
    public static void PrepareAndBuildBasic() { Prepare(); BuildBasic(); }
    public static void PrepareAndBuildBodyPix() { Prepare(); PrepareBodyPix(); BuildBodyPix(); }
    public static void BuildBodyPix() { Build(BodyPath, "Builds/BodyPix/BodyPixDemo.exe"); }

    static void Build(string scene, string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { scene }, locationPathName = output,
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
        });
        File.WriteAllText("Validation/build-" + (scene == BasicPath ? "basic" : "bodypix") + ".txt",
            $"{report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nSize: {report.summary.totalSize}\n");
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Depth demo build failed.");
        Debug.Log("DEPTH_BUILD_SUCCEEDED " + output);
    }
}
