// MeshCompressionTool.cs (Editor)
// Tools/JSHW/Mesh Compression High (party + character)
// 지정 폴더의 모델(FBX) 메시 압축을 High 로 설정 → Build.data 용량↓ (WebGL은 data를 메모리에 들고 있어 메모리도 일부↓).
//  - 손실 압축(정점/노멀/UV 정밀도 저하) — 파티 용품(정적)엔 안전. 스킨드 캐릭터는 미세 아티팩트 가능 → 빌드 후 확인.
//  - 빌드/다운로드 용량이 주 효과. 런타임 GPU 메시 버퍼는 안 줄어듦(그건 정점수/RW 설정 영역).
using UnityEditor;
using UnityEngine;

namespace JSHWWedding.Customization.EditorTools
{
    public static class MeshCompressionTool
    {
        static readonly string[] Folders = {
            "Assets/TirgamesAssets",
            "Assets/yaro.team/GASTRO_Character_Collection",
        };

        [MenuItem("Tools/JSHW/Mesh Compression High (party + character)")]
        public static void SetHigh()
        {
            var guids = AssetDatabase.FindAssets("t:Model", Folders);
            int scanned = 0, changed = 0;
            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (imp == null) continue;
                    scanned++;
                    if (imp.meshCompression == ModelImporterMeshCompression.High) continue;

                    if (EditorUtility.DisplayCancelableProgressBar("Mesh Compression → High",
                        $"{changed} changed …  {System.IO.Path.GetFileName(path)}", (float)i / guids.Length))
                        break;

                    imp.meshCompression = ModelImporterMeshCompression.High;
                    imp.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
            Debug.Log($"[MeshCompress] meshCompression=High. scanned={scanned}, changed={changed}\n폴더: {string.Join(", ", Folders)}");
        }
    }
}
