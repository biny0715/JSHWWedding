// QuestManager.cs
// 보물찾기 퀘스트(비니) 상태 관리. Wedding 씬 로드 시 자동 생성(부트스트랩, 씬 배치 불필요).
//  - 진행도(찾은 개수)는 메모리에만 유지 → 씬을 떠나면 리셋(요구사항: 나가면 처음부터 다시).
//  - '완료(화환 제출)' 여부는 웹이 localStorage 로 관리(디바이스 기준 1회 제한).
//  - 대화창을 열 때 상태 JSON 을 웹에 넘기고(VenueWeb.OpenNpcDialog), 수락/줍기 때 HUD 를 갱신한다.
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JSHWWedding
{
    public class QuestManager : MonoBehaviour
    {
        static QuestManager instance;
        public static QuestManager Instance => instance;

        /// <summary>퀘스트 수락 여부(이 씬 세션 한정).</summary>
        public bool Accepted { get; private set; }
        /// <summary>찾은 물건 수.</summary>
        public int Found { get; private set; }
        /// <summary>전체 물건 수(씬의 활성 PickupZone 개수).</summary>
        public int Total { get; private set; }

        [System.Serializable]
        struct StateDto { public bool accepted; public int found; public int total; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            SceneManager.sceneLoaded += (s, m) => { if (s.name == "Wedding") Spawn(); };
            if (SceneManager.GetActiveScene().name == "Wedding") Spawn();
        }
        static void Spawn()
        {
            if (instance != null) return;
            instance = new GameObject("QuestManager").AddComponent<QuestManager>();
        }

        void Awake() { instance = this; RecountTargets(); }
        void OnDestroy() { if (instance == this) instance = null; }

        /// <summary>씬의 활성 PickupZone 개수를 다시 센다.</summary>
        public void RecountTargets()
        {
            Total = FindObjectsByType<PickupZone>(FindObjectsSortMode.None).Length;
        }

        /// <summary>웹 대화창에 넘길 상태 JSON. 인스턴스가 없으면(로비 등) 기본값.</summary>
        public static string StateJson()
        {
            var q = instance;
            var dto = new StateDto
            {
                accepted = q != null && q.Accepted,
                found = q != null ? q.Found : 0,
                total = q != null ? q.Total : 0,
            };
            return JsonUtility.ToJson(dto);
        }

        /// <summary>웹 대화창 '수락' → WebLobbyBridge.OnNpcQuestAccepted 가 호출.
        /// (에디터 테스트: 플레이 중 QuestManager 오브젝트에서 우클릭 컨텍스트 메뉴)</summary>
        [ContextMenu("수락 (에디터 테스트)")]
        public void Accept()
        {
            if (Accepted) return;
            Accepted = true;
            Found = 0;
            RecountTargets();
            if (Total == 0) Debug.LogWarning("[QuestManager] 씬에 PickupZone 이 없습니다 — 보물(빈 GO + PickupZone)을 배치하세요.");
            PushHud();
            Debug.Log($"[QuestManager] 퀘스트 수락 — 목표 {Total}개");
        }

        /// <summary>PickupZone '줍기' → 카운트 증가 + HUD 갱신 + 획득 팝업(웹).</summary>
        public void OnPickedUp(string itemKey)
        {
            if (!Accepted) return;
            Found = Mathf.Min(Found + 1, Total);
            PushHud();
            // 웹 획득 팝업: "'OOO'을 획득했습니다 / 남은 물건 n개" (+ config.js 의 아이템 그림)
            VenueWeb.ItemPickup($"{{\"item\":\"{itemKey}\",\"found\":{Found},\"total\":{Total}}}");
            Debug.Log($"[QuestManager] 물건 획득 '{itemKey}' {Found}/{Total}");
        }

        [ContextMenu("물건 획득 (에디터 테스트)")]
        void TestPickup() => OnPickedUp("TestItem");

        /// <summary>모두 찾았는지(수락 상태에서).</summary>
        public bool AllFound => Accepted && Total > 0 && Found >= Total;

        void PushHud()
        {
            string state = !Accepted ? "hidden" : (AllFound ? "complete" : "active");
            VenueWeb.QuestHud($"{{\"state\":\"{state}\",\"found\":{Found},\"total\":{Total}}}");
        }
    }
}
