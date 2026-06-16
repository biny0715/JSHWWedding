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
  }

});
