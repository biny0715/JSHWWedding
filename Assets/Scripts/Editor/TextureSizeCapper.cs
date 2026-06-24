// TextureSizeCapper.cs (Editor)
// Tools/JSHW/Cap WebGL Textures to 256 — iOS Safari WebGL 메모리 한도 대응(2차: 512→256).
//  - 256보다 큰 텍스처에 'WebGL 플랫폼 오버라이드'로 maxTextureSize=256 을 건다.
//  - 기본(default)/에디터/타 플랫폼 설정은 그대로 → 에디터 표시는 원본 해상도, WebGL 빌드만 축소.
//  - 예외: GASTRO 캐릭터 아틀라스(단일 공유 텍스처)는 256 절감이 1MB 미만인데 프리뷰에서
//    캐릭터가 뭉개지므로 512 유지(SkipContains).
// 한 번 실행 후 WebGL 재빌드하면 적용된다.
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace JSHWWedding.Customization.EditorTools
{
    public static class TextureSizeCapper
    {
        const int Cap = 256;
        const string Platform = "WebGL";

        // 이 문자열이 경로에 포함되면 캡 제외(원본 해상도/기존 오버라이드 유지)
        static readonly string[] SkipContains = { "GASTRO_Characters_Texture" };

        [MenuItem("Tools/JSHW/Cap WebGL Textures to 256")]
        public static void Cap256()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
            int scanned = 0, changed = 0, skipped = 0;
            var report = new List<string>();

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp == null) continue;
                    scanned++;

                    if (SkipContains.Any(s => path.Contains(s))) { skipped++; continue; }

                    var web = imp.GetPlatformTextureSettings(Platform);
                    int eff = web.overridden ? web.maxTextureSize : imp.maxTextureSize;
                    if (eff <= Cap) continue;   // 이미 작음

                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Cap WebGL textures → 256", $"{changed} capped …  {System.IO.Path.GetFileName(path)}",
                        (float)i / guids.Length))
                        break;

                    web.overridden = true;
                    web.maxTextureSize = Cap;
                    imp.SetPlatformTextureSettings(web);
                    imp.SaveAndReimport();
                    changed++;
                    report.Add($"{eff,5} -> {Cap}  {path}");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            report.Sort();
            report.Reverse();   // 큰 것부터
            Debug.Log($"[TexCap] WebGL maxSize={Cap}px 오버라이드. scanned={scanned}, capped={changed}, skipped(예외)={skipped}.\n" +
                string.Join("\n", report.Take(50)) +
                (report.Count > 50 ? $"\n… (+{report.Count - 50} more)" : ""));
        }
    }
}
