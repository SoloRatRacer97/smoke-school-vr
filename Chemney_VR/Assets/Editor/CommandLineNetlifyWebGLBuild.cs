using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class CommandLineNetlifyWebGLBuild
{
    private const string ScenePath = "Assets/Scenes/ChimneyScene.unity";
    private const string OutputPath = "VR Smoke School";
    private const string TemplateName = "PROJECT:WebXR2020";

    public static void Build()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        string previousTemplate = PlayerSettings.WebGL.template;
        PlayerSettings.WebGL.template = TemplateName;

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
        }

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception("Netlify WebGL build failed: " + report.summary.result);
        }

        WriteNetlifyHeaders();
        WriteNetlifyRedirects();
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

    private static void WriteNetlifyRedirects()
    {
        File.WriteAllText(Path.Combine(OutputPath, "_redirects"),
            "/api/auth/login /.netlify/functions/auth-login 200\n" +
            "/api/auth/me /.netlify/functions/auth-me 200\n" +
            "/api/auth/logout /.netlify/functions/auth-logout 200\n");
    }
}
