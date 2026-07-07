// HwNpcSetup.cs (Editor)
// Tools/JSHW/Setup Hw NPC — 신랑(형원) 정적 Idle NPC 를 만들고 프리팹으로 저장한다. (JinyNpcSetup 과 동일 방식)
//  - 씬에 'HwNPC' 마커가 없으면 JinyNPC 옆(같은 부모/크기)에 새로 만든다 → 이후 원하는 위치로 이동.
//  - CharacterAssembler.ApplyFull 로 부위를 조립하고, JinyNPC 프리팹과 동일하게
//    루트에 IdleVariantDriver, 조립 루트(Hw_Char)에 PlayerNameTag 를 붙인다.
//  - 완료 후 Prefabs/HwNPC.prefab 으로 저장(씬 인스턴스는 프리팹에 연결됨).
//  - 이미 프리팹 인스턴스로 존재하면 재조립하지 않는다(수정하려면 프리팹에서).
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JSHWWedding.Customization.EditorTools
{
    public static class HwNpcSetup
    {
        const string ManifestPath = "Assets/Customization/GastroPartManifest.asset";
        const string PrefabPath = "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Prefabs/HwNPC.prefab";

        // 형원 룩 — 웹 커스텀 UI 표기(1부터) 기준:
        //   피부5 눈4 머리29 상의4 하의5 눈썹1 신발3 가방없음 수염없음 안경없음 모자없음
        // CharacterLook.parts 는 0-based(슬롯1..11 → 인덱스0..10), -1 = 없음.
        const int HwGender = 0;   // 0=male
        const string HwName = "형원";   // 머리 위 이름표
        static readonly int[] HwParts = { 4, 3, 28, 3, 4, 0, 2, -1, -1, -1, -1 };
        //                                skin eyes hair upper pants brows boots back beard glass hats

        [MenuItem("Tools/JSHW/Setup Hw NPC")]
        public static void Setup()
        {
            var manifest = AssetDatabase.LoadAssetAtPath<GastroPartManifest>(ManifestPath);
            if (manifest == null) { Debug.LogError("[HwNPC] 매니페스트를 찾을 수 없음: " + ManifestPath); return; }

            var marker = GameObject.Find("HwNPC");
            if (marker != null && PrefabUtility.IsPartOfPrefabInstance(marker))
            {
                Debug.Log("[HwNPC] 이미 프리팹 인스턴스로 존재합니다. 재조립하려면 씬의 HwNPC 를 삭제 후 다시 실행하세요.");
                Selection.activeGameObject = marker;
                return;
            }

            // 마커 찾기/생성 (없으면 JinyNPC 옆 — 같은 부모/회전/크기)
            bool created = false;
            if (marker == null)
            {
                marker = new GameObject("HwNPC");
                var jiny = GameObject.Find("JinyNPC");
                if (jiny != null)
                {
                    marker.transform.SetParent(jiny.transform.parent, false);
                    marker.transform.localPosition = jiny.transform.localPosition + new Vector3(-2f, 0f, 0f);
                    marker.transform.localRotation = jiny.transform.localRotation;
                    marker.transform.localScale    = jiny.transform.localScale;   // 같은 크기(0.5)
                }
                created = true;
            }

            // 기존 조립물 제거
            var old = marker.transform.Find("Hw_Char");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // 조립 루트 (마커 크기를 따라가도록 local identity)
            var charGO = new GameObject("Hw_Char");
            charGO.transform.SetParent(marker.transform, false);
            charGO.transform.localPosition = Vector3.zero;
            charGO.transform.localRotation = Quaternion.identity;
            charGO.transform.localScale    = Vector3.one;

            var assembler = charGO.AddComponent<CharacterAssembler>();
            assembler.manifest = manifest;
            assembler.freezeAnimators = false;   // 런타임 Idle 재생(지니와 동일)

            var look = new CharacterLook(HwGender, (int[])HwParts.Clone());
            assembler.ApplyFull(look);           // 부위 조립(에디터에서 Instantiate)

            // 머리 위 이름표(고정 이름) — 지니와 동일하게 기본 높이(스케일 자동 보정)
            var nameTag = charGO.AddComponent<PlayerNameTag>();
            nameTag.overrideName = HwName;

            // 아이들 변형 지휘자 — JinyNPC 프리팹과 동일하게 루트에
            if (marker.GetComponent<IdleVariantDriver>() == null)
                marker.AddComponent<IdleVariantDriver>();

            // 프리팹 저장 + 씬 인스턴스 연결
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(marker, PrefabPath, InteractionMode.AutomatedAction);
            if (prefab == null) { Debug.LogError("[HwNPC] 프리팹 저장 실패: " + PrefabPath); return; }

            EditorSceneManager.MarkSceneDirty(marker.scene);
            EditorSceneManager.SaveOpenScenes();
            Selection.activeGameObject = marker;

            string where = created
                ? "'HwNPC' 마커가 없어 JinyNPC 옆에 새로 만들었습니다. 원하는 위치로 옮기세요."
                : "기존 'HwNPC' 마커 위치에 조립했습니다.";
            Debug.Log($"[HwNPC] 'Hw_Char' 조립 + 프리팹 저장 완료({PrefabPath}). 부위 {charGO.transform.childCount}개. " +
                $"룩 CSV: {look.ToCsv()}\n{where}");
        }
    }
}
