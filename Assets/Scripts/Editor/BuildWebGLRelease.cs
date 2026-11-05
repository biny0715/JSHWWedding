using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 맥에서 R2 배포용 WebGL 릴리스 빌드.
// 출력 폴더 이름이 "Build" 여야 파일명이 Build.*.unityweb 가 되어 WebHost/index.html 과 맞는다.
public static class BuildWebGLRelease
{
    const string OutputPath = "/Users/biny/Builds/venue-r2/Build";

    [MenuItem("Tools/Build WebGL (R2 Release)")]
    public static void Build()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Directory.CreateDirectory(OutputPath);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });
        var s = report.summary;
        Debug.Log($"[BuildWebGLRelease] {s.result} | {s.totalErrors} errors | {s.totalSize / (1024 * 1024)} MB | {s.totalTime} | {OutputPath}");
    }
}
