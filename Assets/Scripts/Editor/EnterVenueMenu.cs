// EnterVenueMenu.cs (Editor)
// Tools/JSHW/Enter Venue — 에디터에서 웹 커스텀 UI(jslib) 없이 바로 입장(테스트용).
//  - Lobby 씬을 "재생(Play)" 한 상태에서 메뉴 실행 → 성별 기본룩으로 입장.
//  - WebLobbyBridge 의 기존 입장 흐름을 그대로 호출(SetPreviewGender → EnterVenue) →
//    Photon 접속 → 방 입장 → Wedding 씬 로드 → 그 성별 기본룩으로 캐릭터 스폰.
using UnityEditor;
using UnityEngine;

namespace JSHWWedding.Customization.EditorTools
{
    public static class EnterVenueMenu
    {
        [MenuItem("Tools/JSHW/Enter Venue (Female)")]
        public static void EnterFemale() => Enter("female");

        [MenuItem("Tools/JSHW/Enter Venue (Male)")]
        public static void EnterMale() => Enter("male");

        static void Enter(string gender)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[EnterVenue] 재생(Play) 중에 사용하세요. Lobby 씬을 ▶로 재생한 뒤 메뉴를 실행하세요.");
                return;
            }
            var bridge = WebLobbyBridge.Active;
            if (bridge == null)
            {
                Debug.LogError("[EnterVenue] WebLobbyBridge 인스턴스가 없습니다. Lobby 씬을 재생 중인지 확인하세요.");
                return;
            }
            bridge.SetPlayerName("테스트");
            bridge.SetPreviewGender(gender);   // 미리보기를 그 성별 기본룩으로 → 입장 시 그 룩으로 스폰
            bridge.EnterVenue();               // Photon 접속 → Wedding 로드
            Debug.Log($"[EnterVenue] {gender} 기본룩으로 입장 시작.");
        }
    }
}
