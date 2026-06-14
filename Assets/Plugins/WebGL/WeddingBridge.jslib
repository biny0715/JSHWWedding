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
  }

});
