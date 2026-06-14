using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// WebGL 경량화 도구.
// - 빌드 씬(Wedding / Lobby)이 실제 참조하는 에셋을 기준으로 JagyeongjeonComplex 내 미사용 에셋을 식별
// - 사용 중인(또는 전체) 텍스처에 WebGL 플랫폼 오버라이드(512 + Crunch) 적용
// 원본 PNG/FBX 파일은 건드리지 않으며 임포트 설정(.meta)만 변경한다.
public static class WebGLOptimizer
{
    private const string TargetRoot = "Assets/JagyeongjeonComplex";
    private const string TextureRoot = "Assets/JagyeongjeonComplex/Textures";

    private static readonly string[] BuildScenes =
    {
        "Assets/Stylized Water 3/Scenes/Wedding.unity",
        "Assets/Stylized Water 3/Scenes/Lobby.unity",
    };

    private static long SafeFileSize(string assetPath)
    {
        try
        {
            var full = Path.GetFullPath(assetPath);
            return File.Exists(full) ? new FileInfo(full).Length : 0;
        }
        catch { return 0; }
    }

    // 빌드 씬이 참조하는 에셋들의 의존성 집합(GUID 기반, Unity 내부 API 사용)
    private static HashSet<string> GetUsedAssetPaths()
    {
        var scenes = BuildScenes.Where(s => File.Exists(s)).ToArray();
        var deps = AssetDatabase.GetDependencies(scenes, true);
        return new HashSet<string>(deps);
    }

    [MenuItem("Tools/WebGL/1. Report Unused Jagyeongjeon Assets")]
    public static void ReportUnused()
    {
        var used = GetUsedAssetPaths();

        var allAssets = AssetDatabase.FindAssets("", new[] { TargetRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct()
            .Where(p => !AssetDatabase.IsValidFolder(p))
            .OrderBy(p => p)
            .ToList();

        var sb = new StringBuilder();
        long usedBytes = 0, unusedBytes = 0;
        int usedCount = 0, unusedCount = 0;
        var unusedList = new List<string>();

        foreach (var path in allAssets)
        {
            long size = SafeFileSize(path);
            bool isUsed = used.Contains(path);
            if (isUsed) { usedBytes += size; usedCount++; }
            else { unusedBytes += size; unusedCount++; unusedList.Add(path); }
        }

        sb.AppendLine("=== JagyeongjeonComplex 사용/미사용 분석 (씬: Wedding, Lobby) ===");
        sb.AppendLine($"사용 에셋:   {usedCount}개, {usedBytes / 1048576f:F1} MB");
        sb.AppendLine($"미사용 에셋: {unusedCount}개, {unusedBytes / 1048576f:F1} MB");
        sb.AppendLine();
        sb.AppendLine("--- 미사용 에셋 목록 ---");
        foreach (var p in unusedList.OrderByDescending(SafeFileSize))
            sb.AppendLine($"{SafeFileSize(p) / 1048576f,8:F2} MB  {p}");

        string outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "WebGL_UnusedReport.txt"));
        File.WriteAllText(outPath, sb.ToString());

        Debug.Log($"[WebGLOptimizer] 분석 완료 → 사용 {usedCount}개({usedBytes / 1048576f:F1}MB) / 미사용 {unusedCount}개({unusedBytes / 1048576f:F1}MB). 상세: {outPath}");
    }

    [MenuItem("Tools/WebGL/2. Optimize Jagyeongjeon Textures (512 + Crunch)")]
    public static void OptimizeTextures()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureRoot });
        int changed = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                var settings = new TextureImporterPlatformSettings
                {
                    name = "WebGL",
                    overridden = true,
                    maxTextureSize = 512,
                    resizeAlgorithm = TextureResizeAlgorithm.Mitchell,
                    format = TextureImporterFormat.Automatic,
                    textureCompression = TextureImporterCompression.Compressed,
                    crunchedCompression = true,
                    compressionQuality = 50,
                };
                importer.SetPlatformTextureSettings(settings);
                importer.SaveAndReimport();
                changed++;

                if (i % 25 == 0)
                    EditorUtility.DisplayProgressBar("WebGL 텍스처 최적화", path, (float)i / guids.Length);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[WebGLOptimizer] WebGL 텍스처 오버라이드 적용 완료: {changed}개 (maxSize 512, Crunch, 품질 50)");
    }
}
