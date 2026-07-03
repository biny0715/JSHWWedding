// VenueWeb.cs
// 유니티(C#) → 웹(JS) 호출 래퍼. Plugins/WebGL/WeddingBridge.jslib 의 함수와 1:1.
// 에디터에서는 jslib 가 없으므로 로그만 출력(스텁).
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace JSHWWedding
{
    public static class VenueWeb
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void OpenWeddingGuestbook(string name);
        [DllImport("__Internal")] private static extern void OpenWeddingAlbum();
        [DllImport("__Internal")] private static extern void OpenWeddingNpcDialog(string name, string mode, string stateJson);
        [DllImport("__Internal")] private static extern void WeddingQuestHud(string json);
        [DllImport("__Internal")] private static extern void WeddingItemPickup(string json);
        [DllImport("__Internal")] private static extern void OpenWeddingWreath(int slot);
#endif

        public static void OpenGuestbook(string name)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            OpenWeddingGuestbook(name ?? "");
#else
            Debug.Log("[VenueWeb] (에디터) 방명록 창 열기 — name=" + name);
#endif
        }

        public static void OpenAlbum()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            OpenWeddingAlbum();
#else
            Debug.Log("[VenueWeb] (에디터) 사진 앨범 창 열기");
#endif
        }

        public static void OpenNpcDialog(string name, string mode)
        {
            // 비니(npc) 대화에는 퀘스트 진행 상태를 함께 넘겨 웹이 대사를 분기한다.
            string state = (mode == "npc") ? QuestManager.StateJson() : "";
#if UNITY_WEBGL && !UNITY_EDITOR
            OpenWeddingNpcDialog(name ?? "", mode ?? "npc", state);
#else
            Debug.Log("[VenueWeb] (에디터) 대화창 열기 — name=" + name + ", mode=" + mode + ", state=" + state);
#endif
        }

        /// <summary>퀘스트 HUD 갱신(우측 상단). json: {"state":"hidden|active|complete","found":n,"total":n}</summary>
        public static void QuestHud(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WeddingQuestHud(json ?? "");
#else
            Debug.Log("[VenueWeb] (에디터) 퀘스트 HUD: " + json);
#endif
        }

        /// <summary>물건 획득 팝업(웹). json: {"item":"MoneyBag","found":n,"total":n}</summary>
        public static void ItemPickup(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WeddingItemPickup(json ?? "");
#else
            Debug.Log("[VenueWeb] (에디터) 물건 획득: " + json);
#endif
        }

        /// <summary>화환 '보기' → 웹 팝업(작성자/축하 문구). slot: 0~14.</summary>
        public static void OpenWreath(int slot)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            OpenWeddingWreath(slot);
#else
            Debug.Log("[VenueWeb] (에디터) 화환 보기 — slot=" + slot);
#endif
        }
    }
}
