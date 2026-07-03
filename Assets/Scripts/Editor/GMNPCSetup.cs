// GMNPCSetup.cs (Editor)
// Tools/JSHW/Setup GM NPC — 매니페스트로 지정한 룩(개발자 GM)을 가진 정적 Idle NPC를 만든다.
//  - 씬에 'GMNPC' 마커가 없으면 BrideNPC 옆(같은 부모/크기)에 새로 만든다 → 이후 원하는 위치로 이동.
//  - CharacterAssembler.ApplyFull 로 부위(Meshes FBX)를 조립한다(에디터/비플레이에서도 동작).
//    각 부위 Animator(Character_Movement)가 런타임에 Speed 미설정 → Idle 블렌드로 서 있음(BrideNPC와 동일).
//  - 재실행하면 기존 GM_Char 를 갈아끼움.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JSHWWedding.Customization.EditorTools
{
    public static class GMNPCSetup
    {
        const string ManifestPath = "Assets/Customization/GastroPartManifest.asset";

        // 개발자(GM) 룩 — 웹 커스텀 UI 표기(1부터) 기준:
        //   피부5 눈6 머리35 상의4 하의1 눈썹1 신발20 가방없음 수염없음 안경15 모자없음
        // CharacterLook.parts 는 0-based(슬롯1..11 → 인덱스0..10), -1 = 없음.
        const int GMGender = 0;   // 0=male (개발자=신랑). gender 는 조립에 영향 없음(메타데이터)
        const string GMName = "비니";   // 머리 위 이름표(개발자 핸들)
        static readonly int[] GMParts = { 4, 5, 34, 3, 0, 0, 19, -1, -1, 14, -1 };
        //                                skin eyes hair upper pants brows boots back beard glass hats

        [MenuItem("Tools/JSHW/Setup GM NPC")]
        public static void Setup()
        {
            var manifest = AssetDatabase.LoadAssetAtPath<GastroPartManifest>(ManifestPath);
            if (manifest == null) { Debug.LogError("[GMNPC] 매니페스트를 찾을 수 없음: " + ManifestPath); return; }

            // 마커 찾기/생성 (없으면 BrideNPC 옆에 — 같은 부모/회전/크기)
            var marker = GameObject.Find("GMNPC");
            bool created = false;
            if (marker == null)
            {
                marker = new GameObject("GMNPC");
                var bride = GameObject.Find("BrideNPC");
                if (bride != null)
                {
                    marker.transform.SetParent(bride.transform.parent, false);
                    marker.transform.localPosition = bride.transform.localPosition + new Vector3(2f, 0f, 0f);
                    marker.transform.localRotation = bride.transform.localRotation;
                    marker.transform.localScale    = bride.transform.localScale;   // 같은 크기(0.5)
                }
                created = true;
            }

            // 기존 조립물 제거
            var old = marker.transform.Find("GM_Char");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // 조립 루트 (마커 크기를 따라가도록 local identity)
            var charGO = new GameObject("GM_Char");
            charGO.transform.SetParent(marker.transform, false);
            charGO.transform.localPosition = Vector3.zero;
            charGO.transform.localRotation = Quaternion.identity;
            charGO.transform.localScale    = Vector3.one;

            var assembler = charGO.AddComponent<CharacterAssembler>();
            assembler.manifest = manifest;
            assembler.freezeAnimators = false;   // 런타임 Idle 재생(BrideNPC와 동일)

            var look = new CharacterLook(GMGender, (int[])GMParts.Clone());
            assembler.ApplyFull(look);           // 부위 조립(에디터에서 Instantiate)

            // 머리 위 이름표(고정 이름) — PhotonView 없이 overrideName 으로 표시. heightOffset 은 lossyScale 자동 보정.
            var nameTag = charGO.AddComponent<PlayerNameTag>();
            nameTag.overrideName = GMName;

            int partCount = charGO.transform.childCount;
            EditorSceneManager.MarkSceneDirty(marker.scene);
            Selection.activeGameObject = marker;

            string where = created
                ? "'GMNPC' 마커가 없어 BrideNPC 옆에 새로 만들었습니다. 원하는 위치로 옮기세요."
                : "기존 'GMNPC' 마커 위치에 조립했습니다.";
            Debug.Log($"[GMNPC] 'GM_Char' 조립 완료(GMNPC 자식). 부위 {partCount}개(가방·수염·모자 없음 → 8개 예상). " +
                $"룩 CSV: {look.ToCsv()}\n{where} → 런타임에 Idle 자세. 위치 확인 후 씬 저장하세요.");
        }
    }
}
