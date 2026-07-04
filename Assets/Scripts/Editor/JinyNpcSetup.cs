// JinyNpcSetup.cs (Editor)
// Tools/JSHW/Setup Jiny NPC — 지정한 룩을 가진 정적 Idle NPC(지니)를 만든다. (GMNPCSetup 과 동일 방식)
//  - 씬에 'JinyNPC' 마커가 없으면 BrideNPC 옆(같은 부모/크기)에 새로 만든다 → 이후 원하는 위치로 이동.
//  - CharacterAssembler.ApplyFull 로 부위(Meshes FBX)를 조립한다(에디터/비플레이에서도 동작).
//    각 부위 Animator(Character_Movement)가 런타임에 Speed 미설정 → Idle 블렌드로 서 있음(비니/BrideNPC와 동일).
//  - 재실행하면 기존 Jiny_Char 를 갈아끼움.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JSHWWedding.Customization.EditorTools
{
    public static class JinyNpcSetup
    {
        const string ManifestPath = "Assets/Customization/GastroPartManifest.asset";

        // 지니 룩 — 웹 커스텀 UI 표기(1부터) 기준:
        //   피부4 눈1 머리18 상의11 하의7 눈썹20 신발5 가방없음 수염없음 안경2 모자없음
        // CharacterLook.parts 는 0-based(슬롯1..11 → 인덱스0..10), -1 = 없음.
        const int JinyGender = 0;   // 0=male. gender 는 조립에 영향 없음(메타데이터)
        const string JinyName = "지니";   // 머리 위 이름표
        static readonly int[] JinyParts = { 3, 0, 17, 10, 6, 19, 4, -1, -1, 1, -1 };
        //                                  skin eyes hair upper pants brows boots back beard glass hats

        [MenuItem("Tools/JSHW/Setup Jiny NPC")]
        public static void Setup()
        {
            var manifest = AssetDatabase.LoadAssetAtPath<GastroPartManifest>(ManifestPath);
            if (manifest == null) { Debug.LogError("[JinyNPC] 매니페스트를 찾을 수 없음: " + ManifestPath); return; }

            // 마커 찾기/생성 (없으면 BrideNPC 옆에 — 같은 부모/회전/크기)
            var marker = GameObject.Find("JinyNPC");
            bool created = false;
            if (marker == null)
            {
                marker = new GameObject("JinyNPC");
                var bride = GameObject.Find("BrideNPC");
                if (bride != null)
                {
                    marker.transform.SetParent(bride.transform.parent, false);
                    marker.transform.localPosition = bride.transform.localPosition + new Vector3(-2f, 0f, 0f);
                    marker.transform.localRotation = bride.transform.localRotation;
                    marker.transform.localScale    = bride.transform.localScale;   // 같은 크기(0.5)
                }
                created = true;
            }

            // 기존 조립물 제거
            var old = marker.transform.Find("Jiny_Char");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // 조립 루트 (마커 크기를 따라가도록 local identity)
            var charGO = new GameObject("Jiny_Char");
            charGO.transform.SetParent(marker.transform, false);
            charGO.transform.localPosition = Vector3.zero;
            charGO.transform.localRotation = Quaternion.identity;
            charGO.transform.localScale    = Vector3.one;

            var assembler = charGO.AddComponent<CharacterAssembler>();
            assembler.manifest = manifest;
            assembler.freezeAnimators = false;   // 런타임 Idle 재생(비니/BrideNPC와 동일)

            var look = new CharacterLook(JinyGender, (int[])JinyParts.Clone());
            assembler.ApplyFull(look);           // 부위 조립(에디터에서 Instantiate)

            // 머리 위 이름표(고정 이름) — PhotonView 없이 overrideName 으로 표시. heightOffset 은 lossyScale 자동 보정.
            var nameTag = charGO.AddComponent<PlayerNameTag>();
            nameTag.overrideName = JinyName;

            int partCount = charGO.transform.childCount;
            EditorSceneManager.MarkSceneDirty(marker.scene);
            Selection.activeGameObject = marker;

            string where = created
                ? "'JinyNPC' 마커가 없어 BrideNPC 옆에 새로 만들었습니다. 원하는 위치로 옮기세요."
                : "기존 'JinyNPC' 마커 위치에 조립했습니다.";
            Debug.Log($"[JinyNPC] 'Jiny_Char' 조립 완료(JinyNPC 자식). 부위 {partCount}개. " +
                $"룩 CSV: {look.ToCsv()}\n{where} → 런타임에 Idle 자세. 위치 확인 후 씬 저장하세요.");
        }
    }
}
