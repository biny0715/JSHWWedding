// BugchonMaterialFix.cs  (Editor-only)
// Assets/Bugchonmunhwa 의 머티리얼이 Built-in Standard 셰이더라 URP에서 핑크로 나오는 문제 해결.
// 1순위: Unity 공식 변환기(Convert Selected Built-in Materials to URP) 메뉴 실행(텍스처 슬롯 리맵 정확).
// 2순위(폴백): 남은 Standard 머티리얼을 직접 URP/Lit 로 리맵.

using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public static class BugchonMaterialFix
{
    const string Folder = "Assets/Bugchonmunhwa";   // FixDemoFloor 의 M_Demo_Floor 경로용
    // 머티리얼 URP 변환 + 텍스처/메시 최적화를 적용할 에셋 팩 폴더들 (재실행 시 이미 처리된 건 자동 skip).
    static readonly string[] Folders = { "Assets/Bugchonmunhwa", "Assets/TirgamesAssets" };

    [MenuItem("Tools/Hanok/Fix Asset-Pack Materials (URP)")]
    public static void Fix()
    {
        var guids = AssetDatabase.FindAssets("t:Material", Folders);
        var mats = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(m => m != null)
            .ToArray();
        if (mats.Length == 0) { Debug.LogError($"[BugchonFix] No materials under {string.Join(", ", Folders)}"); return; }

        // 직접 URP/Lit 로 변환 (공식 변환기 메뉴는 모달 다이얼로그로 헤드리스에서 멈출 수 있어 사용 안 함).
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) { Debug.LogError("[BugchonFix] URP/Lit shader not found."); return; }

        int manual = 0, already = 0;
        foreach (var m in mats)
        {
            string sn = m.shader != null ? m.shader.name : "";
            if (sn.StartsWith("Universal Render Pipeline/")) { already++; continue; }
            if (!sn.StartsWith("Standard") && sn != "Legacy Shaders/Diffuse" && sn != "Legacy Shaders/Bumped Diffuse")
                continue; // 알 수 없는 셰이더는 건드리지 않음

            ManualConvert(m, lit);
            manual++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BugchonFix] DONE. total={mats.Length}, manualConverted={manual}, alreadyURP={already}");
    }

    // Demo 씬의 Floor 는 Unity 내장 Default-Material(URP에서 핑크) 사용 → URP/Lit 회색 머티리얼로 교체.
    [MenuItem("Tools/Hanok/Fix Demo Floor Material (URP)")]
    public static void FixDemoFloor()
    {
        var floor = GameObject.Find("Floor");
        if (floor == null) { Debug.LogError("[BugchonFix] 'Floor' not found in open scene."); return; }
        var mr = floor.GetComponent<MeshRenderer>();
        if (mr == null) { Debug.LogError("[BugchonFix] Floor has no MeshRenderer."); return; }

        const string matPath = Folder + "/Materials/M_Demo_Floor.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(lit);
            mat.SetColor("_BaseColor", new Color(0.6f, 0.6f, 0.6f, 1f));
            mat.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mr.sharedMaterial = mat;
        EditorUtility.SetDirty(floor);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(floor.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(floor.scene);
        Debug.Log("[BugchonFix] Demo Floor -> URP material assigned + scene saved.");
    }

    // 더 이상 안 쓰는 JagyeongjeonComplex(2.2GB) 전체 삭제.
    [MenuItem("Tools/Hanok/Delete JagyeongjeonComplex")]
    public static void DeleteJagyeongjeon()
    {
        const string p = "Assets/JagyeongjeonComplex";
        if (!AssetDatabase.IsValidFolder(p)) { Debug.LogWarning($"[Cleanup] {p} not found (already deleted?)."); return; }
        bool ok = AssetDatabase.DeleteAsset(p);
        AssetDatabase.Refresh();
        Debug.Log($"[Cleanup] DeleteAsset {p} => {ok}");
    }

    // Bugchonmunhwa 텍스처/메시를 모바일 WebGL 용으로 축소(임포트 설정 = 에디터+빌드 공통, 1회 캐시 → 빌드 단축).
    [MenuItem("Tools/Hanok/Optimize Asset-Packs for Mobile WebGL")]
    public static void OptimizeBugchon()
    {
        const int maxTex = 1024;
        int tex = 0, mesh = 0;

        // 텍스처: 최대 1024, 압축(Compressed). 노멀맵 타입 등은 건드리지 않음.
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", Folders))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            bool changed = false;
            if (ti.maxTextureSize > maxTex) { ti.maxTextureSize = maxTex; changed = true; }
            if (ti.textureCompression != TextureImporterCompression.Compressed) { ti.textureCompression = TextureImporterCompression.Compressed; changed = true; }
            if (!ti.streamingMipmaps && ti.mipmapEnabled) { ti.streamingMipmaps = true; changed = true; }
            if (changed) { ti.SaveAndReimport(); tex++; }
        }

        // 메시: 메시 압축 Medium, Read/Write off(런타임 메모리 절약), 메시 최적화 on.
        foreach (var guid in AssetDatabase.FindAssets("t:Model", Folders))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) continue;
            bool changed = false;
            if (mi.meshCompression == ModelImporterMeshCompression.Off) { mi.meshCompression = ModelImporterMeshCompression.Medium; changed = true; }
            if (mi.isReadable) { mi.isReadable = false; changed = true; }
            if (mi.meshOptimizationFlags != MeshOptimizationFlags.Everything) { mi.meshOptimizationFlags = MeshOptimizationFlags.Everything; changed = true; }
            if (changed) { mi.SaveAndReimport(); mesh++; }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[Optimize] mobile-WebGL ({string.Join(", ", Folders)}): textures<= {maxTex}+Compressed reimported={tex}, meshes(Medium+noR/W)={mesh}");
    }

    static void ManualConvert(Material m, Shader lit)
    {
        bool isSpecular = m.shader.name.Contains("Specular");

        // --- 기존 값 백업 (속성 존재 시) ---
        Texture mainTex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
        Vector2 mScale = mainTex ? m.GetTextureScale("_MainTex") : Vector2.one;
        Vector2 mOffset = mainTex ? m.GetTextureOffset("_MainTex") : Vector2.zero;
        Color baseColor = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;

        Texture metalMap = m.HasProperty("_MetallicGlossMap") ? m.GetTexture("_MetallicGlossMap") : null;
        Texture specMap = m.HasProperty("_SpecGlossMap") ? m.GetTexture("_SpecGlossMap") : null;
        float metallic = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;
        float glossiness = m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : 0.5f;
        float glossScale = m.HasProperty("_GlossMapScale") ? m.GetFloat("_GlossMapScale") : 1f;

        Texture bump = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
        float bumpScale = m.HasProperty("_BumpScale") ? m.GetFloat("_BumpScale") : 1f;
        Texture occ = m.HasProperty("_OcclusionMap") ? m.GetTexture("_OcclusionMap") : null;
        float occStr = m.HasProperty("_OcclusionStrength") ? m.GetFloat("_OcclusionStrength") : 1f;

        bool emission = m.IsKeywordEnabled("_EMISSION");
        Texture emMap = m.HasProperty("_EmissionMap") ? m.GetTexture("_EmissionMap") : null;
        Color emColor = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;

        float mode = m.HasProperty("_Mode") ? m.GetFloat("_Mode") : 0f; // 0 opaque,1 cutout,2 fade,3 transparent
        float cutoff = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f;

        // --- 셰이더 교체 ---
        m.shader = lit;

        // 알베도
        if (mainTex) { m.SetTexture("_BaseMap", mainTex); m.SetTextureScale("_BaseMap", mScale); m.SetTextureOffset("_BaseMap", mOffset); }
        m.SetColor("_BaseColor", baseColor);

        // 워크플로/메탈릭/스무스니스
        m.SetFloat("_WorkflowMode", isSpecular ? 0f : 1f);
        if (isSpecular && specMap) { m.SetTexture("_SpecGlossMap", specMap); m.EnableKeyword("_METALLICSPECGLOSSMAP"); }
        if (!isSpecular && metalMap) { m.SetTexture("_MetallicGlossMap", metalMap); m.EnableKeyword("_METALLICSPECGLOSSMAP"); }
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", (metalMap || specMap) ? glossScale : glossiness);

        // 노멀
        if (bump) { m.SetTexture("_BumpMap", bump); m.SetFloat("_BumpScale", bumpScale); m.EnableKeyword("_NORMALMAP"); }

        // AO
        if (occ) { m.SetTexture("_OcclusionMap", occ); m.SetFloat("_OcclusionStrength", occStr); m.EnableKeyword("_OCCLUSIONMAP"); }

        // 이미시브
        if (emission) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emColor); if (emMap) m.SetTexture("_EmissionMap", emMap); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }

        // 서피스 타입 (opaque vs transparent/cutout)
        if (mode == 1f) // cutout
        {
            m.SetFloat("_Surface", 0f);
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", cutoff);
            m.EnableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.AlphaTest;
        }
        else if (mode == 2f || mode == 3f) // fade/transparent
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
        else // opaque
        {
            m.SetFloat("_Surface", 0f);
            m.SetFloat("_AlphaClip", 0f);
        }

        EditorUtility.SetDirty(m);
    }
}
