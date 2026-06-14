// WebGLBuildOptimize.cs
// 모바일 WebGL 배포 최적화용 PlayerSettings 일괄 적용 (에디터 전용).
// MCP editor_invoke_method 또는 메뉴(Tools/WebGL/Apply Mobile Settings)로 실행.

using UnityEditor;
using UnityEngine;

namespace JSHWWedding.EditorTools
{
    public static class WebGLBuildOptimize
    {
        [MenuItem("Tools/WebGL/Apply Mobile Settings")]
        public static void ApplyMobileWebGLSettings()
        {
            // 압축: Brotli (가장 작은 다운로드)
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            // GitHub Pages 등 Content-Encoding 헤더를 못 주는 정적 호스트에서도
            // 로더가 JS로 직접 압축해제하도록 폴백 활성화
            PlayerSettings.WebGL.decompressionFallback = true;
            // 재방문 시 캐싱
            PlayerSettings.WebGL.dataCaching = true;

            AssetDatabase.SaveAssets();

            // 참고: Managed Stripping을 High로 올리면 코드 용량이 더 줄지만 Photon 등
            // 리플렉션 기반 코드가 깨질 수 있어(link.xml 필요) 여기선 건드리지 않음.
            Debug.Log("[WebGLBuildOptimize] 적용됨 → compression=Brotli, decompressionFallback=true, dataCaching=true");
        }

        // 큰 텍스처에 WebGL 전용 maxTextureSize 오버라이드를 걸어 다운로드 용량을 줄인다.
        // (원본 에셋은 그대로, WebGL 빌드에서만 축소 → 데스크톱/에디터 품질 영향 없음)
        [MenuItem("Tools/WebGL/Shrink Heavy Textures (WebGL)")]
        public static void ShrinkHeavyTexturesForWebGL()
        {
            // path → WebGL 최대 텍스처 크기
            var targets = new (string path, int max)[]
            {
                // 24MB HDR 스카이박스 → 스카이/반사용으로 1024면 충분
                ("Assets/Stylized Water 3/_Demo/DemoAssets/Materials/SkyboxNoon.exr", 1024),
            };

            int changed = 0;
            foreach (var t in targets)
            {
                var imp = AssetImporter.GetAtPath(t.path) as TextureImporter;
                if (imp == null) { Debug.LogWarning($"[WebGLBuildOptimize] 임포터 없음: {t.path}"); continue; }

                var s = imp.GetPlatformTextureSettings("WebGL");
                s.overridden = true;
                s.maxTextureSize = t.max;
                s.format = TextureImporterFormat.Automatic;
                imp.SetPlatformTextureSettings(s);
                imp.SaveAndReimport();
                changed++;
                Debug.Log($"[WebGLBuildOptimize] WebGL maxSize={t.max} 적용: {t.path}");
            }
            Debug.Log($"[WebGLBuildOptimize] 텍스처 {changed}개 축소 완료");
        }
    }
}
