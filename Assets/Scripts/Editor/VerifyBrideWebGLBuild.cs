using System.Linq;using UnityEditor;using UnityEditor.Build.Reporting;using UnityEngine;using System.IO;
public static class VerifyBrideWebGLBuild {
 public static void Run(){
 var path=@"C:\Users\Pc\Documents\Codex\2026-10-02\blender-1-windows-pc-blender-blender\outputs\bride_webgl_v3\WebGL";
 var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=path,target=BuildTarget.WebGL,options=BuildOptions.Development});
 File.WriteAllText(Path.Combine(Path.GetDirectoryName(path),"WebGL_build_report.txt"),"Result: "+report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nSize: "+report.summary.totalSize);
 Debug.Log("BRIDE_WEBGL_BUILD "+report.summary.result+" errors="+report.summary.totalErrors);
 }
}

