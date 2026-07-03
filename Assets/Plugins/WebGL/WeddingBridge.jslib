// WeddingBridge.jslib
// 유니티(C#) → 웹(JS) 호출용 플러그인.
// C# 의 [DllImport("__Internal")] 함수와 이름이 1:1로 매칭된다.
// 웹 페이지(venue.js)에서 window.OnWeddingLobbyReady / window.OnWeddingEntering 를 정의해두면 호출된다.
mergeInto(LibraryManager.library, {

  // 로비가 준비되어 웹으로부터 이름/입장 신호를 받을 수 있는 상태가 됨
  WeddingLobbyReady: function () {
    try {
      if (typeof window !== "undefined" && typeof window.OnWeddingLobbyReady === "function") {
        window.OnWeddingLobbyReady();
      }
    } catch (e) { console.error("WeddingLobbyReady error:", e); }
  },

  // 입장 처리 시작(접속 중) — 웹에서 로딩 표시 등을 띄울 수 있음
  WeddingEntering: function () {
    try {
      if (typeof window !== "undefined" && typeof window.OnWeddingEntering === "function") {
        window.OnWeddingEntering();
      }
    } catch (e) { console.error("WeddingEntering error:", e); }
  },

  // Wedding(예식장) 3D 씬 로드 완료 — 웹이 로딩/커튼을 걷고 3D를 노출
  WeddingSceneReady: function () {
    try {
      if (typeof window !== "undefined" && typeof window.OnWeddingSceneReady === "function") {
        window.OnWeddingSceneReady();
      }
    } catch (e) { console.error("WeddingSceneReady error:", e); }
  },

  // Photon 연결 끊김 — 사유(cause)를 웹에 전달(원인 진단/안내용)
  WeddingDisconnected: function (causePtr) {
    try {
      var cause = UTF8ToString(causePtr);
      if (typeof window !== "undefined" && typeof window.OnWeddingDisconnected === "function") {
        window.OnWeddingDisconnected(cause);
      }
    } catch (e) { console.error("WeddingDisconnected error:", e); }
  },

  // FlowerDecoZone 버튼 → 웹 방명록 창 열기 (이름 자동입력 + 수정불가)
  OpenWeddingGuestbook: function (namePtr) {
    try {
      var name = UTF8ToString(namePtr);
      if (typeof window !== "undefined" && typeof window.OnOpenGuestbook === "function") {
        window.OnOpenGuestbook(name);
      }
    } catch (e) { console.error("OpenWeddingGuestbook error:", e); }
  },

  // PictureZone 버튼 → 웹 사진 앨범 창 열기
  OpenWeddingAlbum: function () {
    try {
      if (typeof window !== "undefined" && typeof window.OnOpenAlbum === "function") {
        window.OnOpenAlbum();
      }
    } catch (e) { console.error("OpenWeddingAlbum error:", e); }
  },

  // 로비 캐릭터 프리뷰 준비 + 카테고리별 부위 개수(JSON) → 웹이 커스텀 버튼 생성/표시
  PreviewReady: function (jsonPtr) {
    try {
      var json = UTF8ToString(jsonPtr);
      if (typeof window !== "undefined" && typeof window.OnPreviewReady === "function") {
        window.OnPreviewReady(json);
      }
    } catch (e) { console.error("PreviewReady error:", e); }
  },

  // NPC(비니) 말걸기 → 웹 대화창 열기 (동물의 숲 스타일)
  // mode: "npc"(비니 퀘스트) / "celebrate"(감사 대화 → 방명록). stateJson: 퀘스트 진행 상태.
  OpenWeddingNpcDialog: function (namePtr, modePtr, statePtr) {
    try {
      var name = UTF8ToString(namePtr);
      var mode = UTF8ToString(modePtr);
      var state = UTF8ToString(statePtr);
      if (typeof window !== "undefined" && typeof window.OnOpenNpcDialog === "function") {
        window.OnOpenNpcDialog(name, mode, state);
      }
    } catch (e) { console.error("OpenWeddingNpcDialog error:", e); }
  },

  // 퀘스트 HUD 갱신 (우측 상단 '물건을 찾아라 X/n') — QuestManager 가 수락/줍기 때 호출
  WeddingQuestHud: function (jsonPtr) {
    try {
      var json = UTF8ToString(jsonPtr);
      if (typeof window !== "undefined" && typeof window.OnWeddingQuestHud === "function") {
        window.OnWeddingQuestHud(json);
      }
    } catch (e) { console.error("WeddingQuestHud error:", e); }
  },

  // 물건 획득 → 웹 획득 팝업("'OOO'을 획득했습니다 / 남은 물건 n개" + 그림)
  WeddingItemPickup: function (jsonPtr) {
    try {
      var json = UTF8ToString(jsonPtr);
      if (typeof window !== "undefined" && typeof window.OnWeddingItemPickup === "function") {
        window.OnWeddingItemPickup(json);
      }
    } catch (e) { console.error("WeddingItemPickup error:", e); }
  },

  // 화환(WreathViewZone) '보기' → 웹 팝업(작성자/축하 문구). slot: 0~14
  OpenWeddingWreath: function (slot) {
    try {
      if (typeof window !== "undefined" && typeof window.OnOpenWreathView === "function") {
        window.OnOpenWreathView(slot | 0);
      }
    } catch (e) { console.error("OpenWeddingWreath error:", e); }
  }

});
