using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class CommandLineMainStockWebXRBuild
{
    private const string ScenePath = "Assets/Scenes/ChimneyScene.unity";
    private const string OutputPath = "VR Smoke School Stock WebXR";
    private const string TemplateName = "PROJECT:WebXR2020";
    private const string WebXRRuntimePath = "Library/PackageCache/com.de-panther.webxr@fab01af98209/Runtime/Plugins/WebGL/webxr.jspre";
    private const string WebXRTemplatePath = "Assets/WebGLTemplates/WebXR2020/index.html";
    private const string AuthConfigPath = "Assets/WebGLTemplates/WebXR2020/auth-config.js";
    private const string AuthScriptPath = "Assets/Scripts/DataInput_Fields.cs";
    private static readonly string[] ExperimentalWebGLPluginPaths =
    {
        "Assets/Plugins/SmokeMediaLayer.jslib",
        "Assets/Plugins/VideoFidelityLab.jslib"
    };

    private struct StubbedPlugin
    {
        public string path;
        public string originalPath;
    }

    public static void Build()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        ValidateStockWebXRRuntime();
        ValidateBrowserFirstAuthentication();

        string previousTemplate = PlayerSettings.WebGL.template;
        PlayerSettings.WebGL.template = TemplateName;
        List<StubbedPlugin> stubbedPlugins = StubExperimentalWebGLPlugins();

        if (Directory.Exists(OutputPath))
        {
            Directory.Delete(OutputPath, true);
        }

        BuildReport report;
        try
        {
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
        }
        finally
        {
            PlayerSettings.WebGL.template = previousTemplate;
            RestoreExperimentalWebGLPlugins(stubbedPlugins);
        }

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new System.Exception("Main Smoke School stock WebXR build failed: " + report.summary.result);
        }

        WriteNetlifyHeaders();
        ValidateBuiltBrowserFirstAuthentication();
    }

    private static List<StubbedPlugin> StubExperimentalWebGLPlugins()
    {
        List<StubbedPlugin> stubbedPlugins = new List<StubbedPlugin>();

        foreach (string path in ExperimentalWebGLPluginPaths)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            string originalPath = path + ".production-source";

            if (File.Exists(originalPath))
            {
                throw new System.Exception("Cannot stub experimental WebGL plugin because a saved source copy already exists: " + originalPath);
            }

            File.Move(path, originalPath);
            File.WriteAllText(path, BuildProductionStub(path));

            stubbedPlugins.Add(new StubbedPlugin
            {
                path = path,
                originalPath = originalPath
            });
        }

        if (stubbedPlugins.Count > 0)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        return stubbedPlugins;
    }

    private static void RestoreExperimentalWebGLPlugins(List<StubbedPlugin> stubbedPlugins)
    {
        if (stubbedPlugins == null || stubbedPlugins.Count == 0)
        {
            return;
        }

        foreach (StubbedPlugin plugin in stubbedPlugins)
        {
            if (File.Exists(plugin.path))
            {
                File.Delete(plugin.path);
            }
            if (File.Exists(plugin.originalPath))
            {
                File.Move(plugin.originalPath, plugin.path);
            }
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    private static string BuildProductionStub(string path)
    {
        if (path.EndsWith("SmokeMediaLayer.jslib", System.StringComparison.Ordinal))
        {
            return @"mergeInto(LibraryManager.library, {
  SmokeMediaLayer_GetSupportStatus: function() { return 99; },
  SmokeMediaLayer_CreateQuad: function() { return 99; },
  SmokeMediaLayer_SetTransform: function() { return 99; },
  SmokeMediaLayer_SetVisible: function() { return 99; },
  SmokeMediaLayer_Play: function() { return 99; },
  SmokeMediaLayer_Pause: function() { return 99; },
  SmokeMediaLayer_Stop: function() { return 99; },
  SmokeMediaLayer_Destroy: function() { return 99; },
  SmokeMediaLayer_GetVideoReadyState: function() { return -1; },
  SmokeMediaLayer_IsVideoPaused: function() { return 1; }
});
";
        }

        if (path.EndsWith("VideoFidelityLab.jslib", System.StringComparison.Ordinal))
        {
            return @"mergeInto(LibraryManager.library, {
  VideoFidelityLab_EnterVR: function() { return 99; },
  VideoFidelityLab_ShowNativeVideo: function() { return 99; },
  VideoFidelityLab_HideNativeVideo: function() { return 99; },
  VideoFidelityLab_CreateMediaQuad: function() { return 99; },
  VideoFidelityLab_CreateMediaCylinder: function() { return 99; },
  VideoFidelityLab_DestroyMediaLayer: function() { return 99; },
  VideoFidelityLab_ReplayBrowserVideo: function() { return 99; },
  VideoFidelityLab_GetMediaLayerSupportStatus: function() { return 99; },
  VideoFidelityLab_IsWebXRSessionActive: function() { return 0; },
  VideoFidelityLab_GetCanvasClientWidth: function() { return 0; },
  VideoFidelityLab_GetCanvasClientHeight: function() { return 0; },
  VideoFidelityLab_GetCanvasBufferWidth: function() { return 0; },
  VideoFidelityLab_GetCanvasBufferHeight: function() { return 0; },
  VideoFidelityLab_GetDevicePixelRatio100: function() { return 100; },
  VideoFidelityLab_GetBrowserVideoWidth: function() { return -1; },
  VideoFidelityLab_GetBrowserVideoHeight: function() { return -1; },
  VideoFidelityLab_GetBrowserDroppedFrames: function() { return -1; },
  VideoFidelityLab_SetHud: function(message) {}
});
";
        }

        throw new System.Exception("No production stub defined for experimental WebGL plugin: " + path);
    }

    private static void ValidateStockWebXRRuntime()
    {
        if (File.Exists(WebXRTemplatePath))
        {
            string template = File.ReadAllText(WebXRTemplatePath);
            if (template.Contains("patchWebXRLayerRequest") || template.Contains("__smokeSchoolLayersPatched"))
            {
                throw new System.Exception("WebXR2020 template still contains the experimental layers request patch.");
            }
        }

        if (File.Exists(WebXRRuntimePath))
        {
            string runtime = File.ReadAllText(WebXRRuntimePath);
            if (runtime.Contains("optionalFeatures.concat(['layers'])") ||
                runtime.Contains("Module.WebXR.unityBaseLayer") ||
                runtime.Contains("renderState.baseLayer ||"))
            {
                throw new System.Exception("WebXR runtime still contains experimental media-layer/base-layer changes.");
            }
        }
    }

    private static void ValidateBrowserFirstAuthentication()
    {
        if (!File.Exists(WebXRTemplatePath) || !File.Exists(AuthConfigPath) || !File.Exists(AuthScriptPath))
        {
            throw new System.Exception("Browser-first authentication source files are missing.");
        }

        string template = File.ReadAllText(WebXRTemplatePath);
        string script = File.ReadAllText(AuthScriptPath);
        if (!template.Contains("auth-config.js") ||
            !template.Contains("id=\"unity-login-overlay\"") ||
            !template.Contains("fetch(apiUrl") ||
            !template.Contains("sanitizeApprovedResponse") ||
            !template.Contains("startUnity(approvedPayload)") ||
            !template.Contains("CompleteApprovedLogin") ||
            !template.Contains("createUnityInstance") ||
            !template.Contains("document.body.appendChild(script)") ||
            template.Contains("localStorage") ||
            template.Contains("sessionStorage") ||
            template.Contains("ReceiveBrowserLogin") ||
            !script.Contains("sessionReference") ||
            !script.Contains("userId") ||
            script.Contains("UnityWebRequest") ||
            script.Contains("PlayerPrefs") ||
            script.Contains("password"))
        {
            throw new System.Exception("Browser-first signed authentication is not configured correctly.");
        }
    }

    private static void ValidateBuiltBrowserFirstAuthentication()
    {
        string builtIndex = Path.Combine(OutputPath, "index.html");
        string builtConfig = Path.Combine(OutputPath, "auth-config.js");
        if (!File.Exists(builtIndex) || !File.Exists(builtConfig))
        {
            throw new System.Exception("Built WebXR output is missing browser authentication configuration.");
        }

        string index = File.ReadAllText(builtIndex);
        if (!index.Contains("id=\"unity-login-overlay\"") ||
            !index.Contains("fetch(apiUrl") ||
            !index.Contains("startUnity(approvedPayload)") ||
            !index.Contains("CompleteApprovedLogin") ||
            !index.Contains("document.body.appendChild(script)") ||
            index.Contains("ReceiveBrowserLogin") ||
            index.Contains("localStorage") ||
            index.Contains("sessionStorage"))
        {
            throw new System.Exception("Built WebXR output is not using the browser-first authentication gate.");
        }
    }

    private static void WriteNetlifyHeaders()
    {
        string encodedName = OutputPath.Replace(" ", "%20");
        File.WriteAllText(Path.Combine(OutputPath, "_headers"),
            "/Build/" + encodedName + ".wasm.br\n" +
            "  Content-Type: application/wasm\n" +
            "  Content-Encoding: br\n\n" +
            "/Build/" + encodedName + ".framework.js.br\n" +
            "  Content-Type: application/javascript\n" +
            "  Content-Encoding: br\n\n" +
            "/Build/" + encodedName + ".data.br\n" +
            "  Content-Type: application/octet-stream\n" +
            "  Content-Encoding: br\n\n" +
            "/Build/*.data.br\n" +
            "  Content-Encoding: br\n" +
            "  Content-Type: application/octet-stream\n" +
            "/Build/*.wasm.br\n" +
            "  Content-Encoding: br\n" +
            "  Content-Type: application/wasm\n" +
            "/Build/*.js.br\n" +
            "  Content-Encoding: br\n" +
            "  Content-Type: application/javascript\n");
    }
}
