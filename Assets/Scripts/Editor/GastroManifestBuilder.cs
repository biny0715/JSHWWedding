// GastroManifestBuilder.cs (Editor)
// Tools/JSHW/Build Gastro Manifest — Meshes/<Cat>/*.fbx 를 스캔해 GastroPartManifest 를 자동 생성/갱신.
//  - skin: 허용된 5종만 고정 순서
//  - hair: Male_Hair_* + Female_Hair_* (남녀 공유 풀)
//  - 컨트롤러: Character_Movement (guid)
//  - 성별 기본 룩: Male/FemaleCharacter 프리팹의 부위 source guid를 읽어 각 카테고리 인덱스로 변환
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace JSHWWedding.Customization.EditorTools
{
    public static class GastroManifestBuilder
    {
        const string MeshRoot = "Assets/yaro.team/GASTRO_Character_Collection/Meshes";
        const string ManifestDir = "Assets/Customization";
        const string ManifestPath = ManifestDir + "/GastroPartManifest.asset";
        const string ControllerGuid = "fac54571ff27f2b46b28cd6d82cd3c4c";
        const string FemalePrefab = "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Resources/FemaleCharacter.prefab";
        const string MalePrefab = "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Resources/MaleCharacter.prefab";

        static readonly string[] SkinAllowed = {
            "Body_Brown_07", "Body_Brown_08", "Body_Pink_06", "Body_Pink_07", "Body_Pink_08"
        };

        // ===== 부위 풀 축소(빌드 크기) — 이름 규칙 필터 =====
        // 피부 = 위 SkinAllowed 고정 5종. 나머지는 Build()에서 이름 규칙으로 거른다:
        //  · hair  : Male_Hair_*/Female_Hair_* 만 (→ Hair_Plant_* / Hair_for_Hat 자동 제외) + 아래 색/번호 제외
        //  · upper/pants/boots/backpack/hats/glasses : "*_Regular_*" 만 (음식테마 Fish/Fruit/Vegetable 제외)
        //  · beard : 음식테마 없음(색상 4종뿐) → 색상별 번호 6 이하만 (BeardFilter)
        //  · eyes/brows : 전체 유지(규칙 미지정)
        // ⚠️ 기본 부위가 걸러져 빠지면 기본 외형이 바뀜 — 빌드 로그의 default 이름 확인.
        //    현재 기본 머리(Female_Hair_Black_14 / Male_Hair_Black_09)는 제외 목록에 없어 안전.
        static readonly HashSet<string> HairExclude = new HashSet<string> {
            "Female_Hair_Blue_01", "Female_Hair_Pink_01", "Female_Hair_Violet_01", "Female_Hair_Yellow_01",
            "Male_Hair_Blue_01",   "Male_Hair_Pink_01",   "Male_Hair_Violet_01",   "Male_Hair_Yellow_01",
            "Female_Hair_Black_08", "Female_Hair_Blond_08", "Female_Hair_Brown_08",
            "Female_Hair_Black_09", "Female_Hair_Blond_09", "Female_Hair_Brown_09",
        };
        // Copper(여)/Cooper(남, 오타 네이밍) 색상 머리는 색상 통째로 제외 + 위 개별 제외.
        static bool HairFilter(string n) =>
            (n.StartsWith("Male_Hair_") || n.StartsWith("Female_Hair_"))
            && !n.Contains("Copper") && !n.Contains("Cooper")
            && !HairExclude.Contains(n);

        // 수염: Beard_<Color>_<NN> 중 번호 6 이하만(색상 4종 × ~6 = ~24). Regular/음식 테마가 없어 개수로 솎음.
        static bool BeardFilter(string n)
        {
            var mt = Regex.Match(n, @"_(\d+)$");
            return mt.Success && int.TryParse(mt.Groups[1].Value, out int num) && num <= 6;
        }

        // ===== 추가 축소(메모리/요청) =====
        // 상의: 표시번호(=파일번호) 이 8개 제거 → 20→12
        static readonly HashSet<string> UpperExclude = new HashSet<string> {
            "Upper_Wear_Regular_01", "Upper_Wear_Regular_02", "Upper_Wear_Regular_07", "Upper_Wear_Regular_09",
            "Upper_Wear_Regular_10", "Upper_Wear_Regular_14", "Upper_Wear_Regular_15", "Upper_Wear_Regular_20",
        };
        // 머리 70% 균등 축소 시에도 성별 기본 머리는 반드시 보존(아니면 기본 외형이 바뀜).
        static readonly HashSet<string> HairKeep = new HashSet<string> { "Female_Hair_Black_14", "Male_Hair_Black_09" };

        [MenuItem("Tools/JSHW/Build Gastro Manifest")]
        public static void Build()
        {
            if (!Directory.Exists(ManifestDir)) Directory.CreateDirectory(ManifestDir);
            var m = AssetDatabase.LoadAssetAtPath<GastroPartManifest>(ManifestPath);
            if (m == null)
            {
                m = ScriptableObject.CreateInstance<GastroPartManifest>();
                AssetDatabase.CreateAsset(m, ManifestPath);
            }

            m.characterController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(ControllerGuid));

            m.skin     = LoadCurated("Bodys", SkinAllowed);
            m.eyes     = LoadFolder("Eyes");
            m.hair     = SampleFraction(LoadFolder("Hairs", HairFilter), 0.70f, HairKeep);   // 70%로 균등 축소
            m.upper    = LoadFolder("Upper_Wear", n => n.StartsWith("Upper_Wear_Regular_") && !UpperExclude.Contains(n));
            m.pants    = LoadFolder("Pants", n => n.StartsWith("Pants_Regular_"));
            m.brows    = TakeFirst(LoadFolder("Brows"), 20);          // 표시 1~20만
            m.boots    = LoadFolder("Boots", n => n.StartsWith("Boot_Regular_"));
            m.backpack = LoadFolder("Backpacks", n => n.StartsWith("Backpack_Regular_"));
            m.beard    = TakeFirst(LoadFolder("Beards", BeardFilter), 18);   // 표시 1~18만(Copper 제외)
            m.glasses  = LoadFolder("Glasses", n => n.StartsWith("Glass_Regular_"));
            m.hats     = System.Array.Empty<GameObject>();   // 모자 전체 제외(요청). 빈 풀 → 웹에서 탭도 자동 숨김.

            m.femaleDefaultParts = DeriveDefault(m, FemalePrefab);
            m.maleDefaultParts   = DeriveDefault(m, MalePrefab);

            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GastroManifest] controller={(m.characterController ? "OK" : "MISSING")} | " +
                $"skin={m.skin.Length} eyes={m.eyes.Length} hair={m.hair.Length} upper={m.upper.Length} pants={m.pants.Length} brows={m.brows.Length} " +
                $"boots={m.boots.Length} backpack={m.backpack.Length} beard={m.beard.Length} glasses={m.glasses.Length} hats={m.hats.Length}\n" +
                $"femaleDefault=[{string.Join(",", m.femaleDefaultParts)}]  이름: {DefaultNames(m, m.femaleDefaultParts)}\n" +
                $"maleDefault=[{string.Join(",", m.maleDefaultParts)}]  이름: {DefaultNames(m, m.maleDefaultParts)}");
        }

        // 화이트리스트가 있으면(>0) 그 이름·순서대로만 로드, 비어있으면 폴더 전체(LoadFolder).
        static GameObject[] LoadCurated(string folder, string[] allowed, System.Func<string, bool> nameFilter = null)
        {
            if (allowed != null && allowed.Length > 0)
            {
                var picked = allowed
                    .Select(n => new { n, go = AssetDatabase.LoadAssetAtPath<GameObject>($"{MeshRoot}/{folder}/{n}.fbx") })
                    .ToArray();
                foreach (var p in picked.Where(x => x.go == null))
                    Debug.LogWarning($"[GastroManifest] {folder} 화이트리스트에 없는 이름: {p.n}.fbx (오타?)");
                return picked.Where(x => x.go != null).Select(x => x.go).ToArray();
            }
            return LoadFolder(folder, nameFilter);
        }

        // 기본 룩(파생된 인덱스)에 해당하는 부위 "이름"을 보여줘 화이트리스트로 빠지지 않았는지 검증용.
        static string DefaultNames(GastroPartManifest m, int[] def)
        {
            var sb = new System.Text.StringBuilder();
            for (int cat = 1; cat <= CharacterLook.CategoryCount; cat++)
            {
                var arr = m.ByCategory(cat);
                int idx = def[cat - 1];
                string nm = (idx >= 0 && arr != null && idx < arr.Length && arr[idx] != null) ? arr[idx].name : "없음";
                if (cat > 1) sb.Append(", ");
                sb.Append(nm);
            }
            return sb.ToString();
        }

        // 정렬된 배열에서 앞 n개(표시번호 1..n). 부족하면 그대로.
        static GameObject[] TakeFirst(GameObject[] arr, int n) => arr.Length <= n ? arr : arr.Take(n).ToArray();

        // 균등 분포로 frac 비율만 남김(순서 유지). mustKeep 이름은 비율과 무관하게 반드시 보존.
        static GameObject[] SampleFraction(GameObject[] arr, float frac, HashSet<string> mustKeep)
        {
            int n = arr.Length;
            if (n == 0) return arr;
            int k = Mathf.Clamp(Mathf.RoundToInt(n * frac), 1, n);
            var keep = new HashSet<int>();
            for (int j = 0; j < k; j++) keep.Add((int)((long)j * n / k));
            for (int i = 0; i < n; i++) if (arr[i] != null && mustKeep.Contains(arr[i].name)) keep.Add(i);
            var result = new List<GameObject>();
            for (int i = 0; i < n; i++) if (keep.Contains(i)) result.Add(arr[i]);
            return result.ToArray();
        }

        static GameObject[] LoadFolder(string folder, System.Func<string, bool> nameFilter = null)
        {
            string dir = $"{MeshRoot}/{folder}";
            if (!AssetDatabase.IsValidFolder(dir)) { Debug.LogWarning($"[GastroManifest] folder not found: {dir}"); return new GameObject[0]; }
            return AssetDatabase.FindAssets("t:GameObject", new[] { dir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                .Where(p => string.Equals(Path.GetDirectoryName(p).Replace('\\', '/'), dir, System.StringComparison.Ordinal))
                .Select(p => new { name = Path.GetFileNameWithoutExtension(p), go = AssetDatabase.LoadAssetAtPath<GameObject>(p) })
                .Where(x => x.go != null && (nameFilter == null || nameFilter(x.name)))
                .OrderBy(x => x.name, System.StringComparer.Ordinal)
                .Select(x => x.go)
                .ToArray();
        }

        // 캐릭터 프리팹의 부위 source FBX guid를 읽어 각 카테고리 기본 인덱스로 변환 (필수=0, 선택=-1 기본)
        static int[] DeriveDefault(GastroPartManifest m, string prefabPath)
        {
            var def = new int[CharacterLook.CategoryCount];
            for (int i = 0; i < def.Length; i++) def[i] = (i < 6) ? 0 : -1;

            if (!File.Exists(prefabPath)) { Debug.LogWarning($"[GastroManifest] prefab not found: {prefabPath}"); return def; }

            var guids = new HashSet<string>();
            foreach (Match mt in Regex.Matches(File.ReadAllText(prefabPath), @"m_SourcePrefab: \{fileID: \d+, guid: ([0-9a-f]{32})"))
                guids.Add(mt.Groups[1].Value);

            foreach (var guid in guids)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (go == null) continue;
                for (int cat = 1; cat <= CharacterLook.CategoryCount; cat++)
                {
                    var arr = m.ByCategory(cat);
                    if (arr == null) continue;
                    int idx = System.Array.IndexOf(arr, go);
                    if (idx >= 0) { def[cat - 1] = idx; break; }
                }
            }
            return def;
        }
    }
}
