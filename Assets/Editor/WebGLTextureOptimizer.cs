// WebGLTextureOptimizer.cs
// 3D 콘텐츠 텍스처에 WebGL 전용 Import 오버라이드를 규칙 기반으로 일괄 적용.
//   - Crunch 압축 ON (노멀맵 제외 / 다운로드 용량↓)
//   - 카테고리별 Max Size 하향 (히어로/소품/물 구분)
//   - 데이터맵(_AO/_R/_S/_M/_Mask…) sRGB OFF (Linear)
//   - UI/Sprite Mipmap OFF
// 원본 에셋·기본 플랫폼 설정은 그대로 두고 WebGL 빌드에만 영향 → 에디터/데스크톱 품질 유지.
//
// 메뉴:
//   Tools/WebGL/Optimize Textures - DRY RUN  → 변경 예정 내역만 로그(적용 안 함)
//   Tools/WebGL/Optimize Textures - APPLY    → 실제 적용(재임포트, 수백 장이면 수 분 소요)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JSHWWedding.EditorTools
{
    public static class WebGLTextureOptimizer
    {
        // 처리 대상 루트(3D 콘텐츠). 폰트/네트워킹/에디터 등은 건드리지 않음.
        static readonly string[] ProcessRoots =
        {
            "Assets/Bugchonmunhwa",
            "Assets/TirgamesAssets",
            "Assets/Stylized Water 3",
            "Assets/yaro.team",
            "Assets/Picture_Frames_FREE",
        };

        static readonly string[] SkipContains =
        {
            "/Editor/", "TextMesh Pro", "/Photon", "TutorialInfo", "StreamingAssets",
        };

        // 접미사(suffix) 기반 역할 판별
        static readonly string[] NormalSuffix = { "_n", "_nm", "_normal", "_norm", "_nrm" };
        static readonly string[] DataSuffix =
        {
            "_ao", "_r", "_s", "_m", "_ms", "_mr", "_orm", "_mask", "_metallic",
            "_metallicsmoothness", "_smoothness", "_roughness", "_occlusion",
            "_spec", "_specular", "_gloss", "_glossiness", "_h", "_height", "_disp",
        };

        enum Role { Color, Normal, Data, UI }

        [MenuItem("Tools/WebGL/Optimize Textures - DRY RUN")]
        public static void DryRun() => Run(false);

        [MenuItem("Tools/WebGL/Optimize Textures - APPLY")]
        public static void Apply() => Run(true);

        static void Run(bool apply)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", ProcessRoots);
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath).Distinct()
                             .Where(p => !SkipContains.Any(s => p.Contains(s)))
                             .ToList();

            int processed = 0, crunched = 0, srgbFixed = 0, mipOff = 0;
            var tally = new Dictionary<string, int>();

            if (apply) AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    string path = paths[i];
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp == null) continue;

                    Role role = Classify(imp, path);
                    int tier = SizeTier(path, role);
                    bool isNormal = role == Role.Normal;

                    if (EditorUtility.DisplayCancelableProgressBar(
                        apply ? "Optimizing Textures (APPLY)" : "Optimizing Textures (DRY RUN)",
                        Path.GetFileName(path), (float)i / paths.Count)) break;

                    string key = $"{Root(path)} | {role} | max{tier} | crunch={(isNormal ? "no" : "yes")}";
                    tally[key] = tally.TryGetValue(key, out var c) ? c + 1 : 1;
                    processed++;

                    bool wantSrgbOff = (role == Role.Data) && imp.sRGBTexture;
                    bool wantMipOff = (role == Role.UI) && imp.mipmapEnabled;
                    if (!isNormal) crunched++;
                    if (wantSrgbOff) srgbFixed++;
                    if (wantMipOff) mipOff++;

                    if (!apply) continue;

                    if (wantSrgbOff) imp.sRGBTexture = false;
                    if (wantMipOff) imp.mipmapEnabled = false;

                    var s = imp.GetPlatformTextureSettings("WebGL");
                    s.overridden = true;
                    s.maxTextureSize = tier;
                    s.format = TextureImporterFormat.Automatic;
                    s.textureCompression = TextureImporterCompression.Compressed;
                    s.crunchedCompression = !isNormal; // 노멀맵은 크런치 시 아티팩트 → 제외
                    s.compressionQuality = 75;
                    imp.SetPlatformTextureSettings(s);
                    imp.SaveAndReimport();
                }
            }
            finally
            {
                if (apply) AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[TextureOptimizer] {(apply ? "APPLIED" : "DRY RUN")} — 대상 {processed}개 " +
                          $"(crunch {crunched}, sRGB→linear {srgbFixed}, mipOff {mipOff})");
            foreach (var kv in tally.OrderByDescending(k => k.Value))
                sb.AppendLine($"  {kv.Value,4} × {kv.Key}");
            Debug.Log(sb.ToString());
            if (apply) AssetDatabase.Refresh();
        }

        static Role Classify(TextureImporter imp, string path)
        {
            if (imp.textureType == TextureImporterType.Sprite) return Role.UI;
            if (imp.textureType == TextureImporterType.NormalMap) return Role.Normal;

            string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (NormalSuffix.Any(suf => name.EndsWith(suf))) return Role.Normal;
            if (DataSuffix.Any(suf => name.EndsWith(suf))) return Role.Data;
            return Role.Color; // 베이스컬러/디퓨즈 등 기본
        }

        static int SizeTier(string path, Role role)
        {
            if (role == Role.UI) return 1024;
            bool hero = path.Contains("Bugchonmunhwa") || path.Contains("yaro.team") ||
                        path.Contains("Stylized Water 3");
            if (hero)
                return role == Role.Color ? 1024 : 512;        // 한옥/캐릭터/물: 컬러 1024, 그 외 512
            // 소품(Tirgames/PictureFrames 등): 컬러 512, 노멀 512, 데이터 256
            return role == Role.Color ? 512 : (role == Role.Normal ? 512 : 256);
        }

        static string Root(string path)
        {
            var parts = path.Split('/');
            return parts.Length >= 2 ? parts[1] : path;
        }
    }
}
