/* =====================================================================
 * venue.js — 모바일 예식장 오버레이 로직 (로딩 게이팅 + 이름 기억/자동 재입장)
 *
 * 화면 흐름(상태):
 *   1) LOADING  : Unity 로비 씬이 준비될 때까지 로딩 화면 (입력칸 숨김)
 *   2) LOBBY    : 이름/성별 → 미리보기 → 입장하기
 *   3) ENTERING : 입장 신호 전송 후 Wedding 3D 씬 로드 대기 (로딩 화면)
 *   4) PLAYING  : Wedding 씬 로드 완료 → 커튼 제거(3D 노출)
 *
 * 모바일 복귀 대응:
 *   iOS 등에서 앱 전환 후 돌아오면 페이지가 통째로 리로드되는 경우가 많다.
 *   이름을 localStorage 에 기억해 두고, 다시 들어오면 입력을 건너뛰고 자동 입장한다.
 *   (이름 바꾸려면 로딩 화면의 "다른 이름으로 입장" 링크)
 *
 * 접속 지연 대응:
 *   저전력 모드/약한 기기에서 Photon 접속이 늦으면 무한 스피너처럼 보인다.
 *   일정 시간 후 안내문 + 새로고침 버튼을 노출한다.
 * ===================================================================== */
(function () {
  "use strict";

  var NAME_KEY = "jshw_visitor_name";
  var GENDER_KEY = "jshw_visitor_gender";
  var LOOK_KEY = "jshw_visitor_look";   // 캐릭터 커스텀(룩) CSV "g,skin,..,hats"

  function lsGet(k) { try { return window.localStorage.getItem(k) || ""; } catch (e) { return ""; } }
  function lsSet(k, v) { try { window.localStorage.setItem(k, v); } catch (e) {} }
  function lsDel(k) { try { window.localStorage.removeItem(k); } catch (e) {} }

  var state = { name: "", gender: "", entered: false };

  /* ---- 요소 ---- */
  var curtain = document.getElementById("venue-curtain");
  var loading = document.getElementById("venue-loading");
  var loadingText = document.getElementById("loading-text");
  var loadingSub = document.getElementById("loading-sub");
  var retryBtn = document.getElementById("loading-retry");
  var changeNameLink = document.getElementById("change-name-link");
  var lobbyNotice = document.getElementById("lobby-notice");
  var step1 = document.getElementById("lobby-step1");
  var step2 = document.getElementById("lobby-step2");
  var playUI = document.getElementById("play-ui");
  var menuBtn = document.getElementById("venue-menu-btn");

  var nameInput = document.getElementById("visitor-name");
  var nextBtn = document.getElementById("lobby-next-btn");
  var prevBtn = document.getElementById("lobby-prev-btn");
  var enterBtn = document.getElementById("lobby-enter-btn");
  var genderBtns = Array.prototype.slice.call(document.querySelectorAll(".gender-btn"));

  function hide(el) {
    // 계측: step2(커스텀 시트)가 숨겨지는 모든 경로를 스택과 함께 기록 — 재발 시 원인 특정용
    if (el === step2 && el && !el.classList.contains("lobby--hidden"))
      console.warn("[venue] step2 숨김 발생:", new Error().stack);
    if (el) el.classList.add("lobby--hidden");
  }
  function show(el) { if (el) el.classList.remove("lobby--hidden"); }
  function setDisplay(el, v) { if (el) el.style.display = v; }

  // 우상단 햄버거 메뉴 버튼: 예식장 입장(playing) 후에만 노출. 로비/로딩 복귀 시 닫고 숨김.
  function setMenuVisible(v) {
    if (menuBtn) menuBtn.hidden = !v;
    if (!v) {
      var m = document.getElementById("venue-menu");
      var b = document.getElementById("venue-menu-backdrop");
      if (m) m.hidden = true;
      if (b) b.hidden = true;
      if (menuBtn) { menuBtn.classList.remove("open"); menuBtn.setAttribute("aria-expanded", "false"); }
    }
  }

  var lobbyReady = false;
  var autoEnter = false;
  var hasAttemptedAuto = false;   // 자동입장은 첫 진입에만(끊김→재진입 루프 방지)
  var pendingNotice = "";         // 끊김 등 안내문(다음 입력화면에 표시)
  var lobbyFallbackTimer, slowTimer;

  // 플레이 중 유니티가 띄운 퀘스트 HUD 숨김. 연결 끊김/재입장으로 로딩·로비로 돌아오면
  // 유니티가 '숨김' 신호를 못 보내(끊김) HUD가 남는다 → 로딩/로비 진입 시 웹이 직접 정리.
  // (재입장 후 새 Wedding 세션의 QuestManager 가 다시 상태를 보내므로 필요하면 다시 뜬다)
  function hideQuestHud() {
    var qh = document.getElementById("quest-hud");
    if (qh) qh.hidden = true;
  }

  /* ===== 상태 전환 ===== */
  function showLoading(text, sub) {
    if (loadingText && text) loadingText.textContent = text;
    if (loadingSub) loadingSub.textContent = sub != null ? sub : "잠시만 기다려 주세요";
    setMenuVisible(false);
    hideQuestHud();
    show(loading); hide(step1); hide(step2);
  }
  function showLobby() {        // Unity 로비 (재)진입 → 입력(또는 자동 입장)
    hideQuestHud();
    // 멱등화: 이미 로비 UI 상태이고(커스텀 step2 포함) 입장 중도 아니며 표시할 안내문도 없으면
    // 아무것도 리셋하지 않는다 — 중복 LobbyReady 신호가 커스텀 중인 시트를 날리지 못하게.
    if (lobbyReady && !state.entered && !pendingNotice &&
        step2 && !step2.classList.contains("lobby--hidden")) return;
    lobbyReady = true;
    clearTimeout(lobbyFallbackTimer);
    // [핵심 버그 수정] 입장 실패로 로비에 복귀해도 doEnter 가 걸어둔 12s(지연안내) 타이머와
    // 입장 신호 재시도가 살아남지 못하게 로비 복귀 시 반드시 함께 해제한다.
    clearTimeout(slowTimer);
    clearTimeout(enterRetryTimer);
    state.entered = false;       // 로비로 (재)진입 → 입장 상태 리셋(재입장 가능)

    if (autoEnter && state.name.trim() && !hasAttemptedAuto) {
      hasAttemptedAuto = true;   // 기억된 이름 → 첫 진입엔 자동 입장
      doEnter();
      return;
    }

    // 입력 화면: 재진입이면 3D 커튼 복원 + 플레이 UI/메뉴 숨김
    if (curtain) curtain.classList.remove("curtain--hidden");
    if (playUI) playUI.classList.add("play-ui--hidden");
    setMenuVisible(false);
    setDisplay(retryBtn, "none");
    setDisplay(changeNameLink, "none");
    if (lobbyNotice) {
      lobbyNotice.textContent = pendingNotice;
      lobbyNotice.style.display = pendingNotice ? "" : "none";
    }
    pendingNotice = "";
    hide(loading); show(step1); hide(step2);
  }
  function reveal() {           // Wedding 씬 로드 완료 → 3D 노출
    // 입장 진행 중이 아니면 무시 — 뒤늦은 중복 SceneReady 가 로비 UI(커스텀 시트)를
    // 숨기지 못하게 봉인. (정상 입장은 doEnter 가 entered=true 를 먼저 세우므로 영향 없음)
    if (!state.entered) return;
    clearTimeout(slowTimer);
    clearTimeout(enterRetryTimer);
    hide(loading); hide(step1); hide(step2);
    if (curtain) curtain.classList.add("curtain--hidden");
    playUI.classList.remove("play-ui--hidden");
    setMenuVisible(true);   // 입장 완료 → 우상단 메뉴 노출
  }

  /* ===== 유니티 → 웹 콜백 ===== */
  window.OnWeddingLobbyReady = function () { console.log("[venue] Unity 로비 준비 완료"); showLobby(); };
  window.OnWeddingEntering = function () { console.log("[venue] Unity 입장(접속) 시작"); };
  window.OnWeddingSceneReady = function () { console.log("[venue] Wedding 씬 로드 완료"); reveal(); };
  window.OnWeddingDisconnected = function (cause) {
    console.warn("[venue] Photon 끊김:", cause);
    window.__lastDisconnect = cause;   // 콘솔에서 확인용
    pendingNotice = "연결이 끊겨 로비로 돌아왔어요 (사유: " + cause + "). 다시 입장해 주세요.";
    // [수정] 로비에서 접속 실패(자동입장 포함)하면 Unity는 씬을 다시 로드하지 않아
    // LobbyReady 가 다시 오지 않는다 → 여기서 직접 입력 화면으로 복귀(타이머 해제 포함).
    // 예식장에서 끊긴 경우에도 곧 도착할 LobbyReady 의 showLobby 는 멱등이라 안전.
    if (state.entered) showLobby();
  };

  /* ===== 웹 → 유니티 ===== */
  function sendEnterToUnity() {
    var u = window.unityInstance;
    if (!u) return false;
    try {
      u.SendMessage("WebBridge", "SetPlayerName", state.name.trim() || "하객");
      if (cz.look) u.SendMessage("WebBridge", "ApplyLook", cz.look.join(","));   // 커스텀 룩 전달
      u.SendMessage("WebBridge", "EnterVenue");
      return true;
    } catch (e) {
      console.warn("[venue] Unity SendMessage 실패:", e);
      return false;
    }
  }

  // [수정] unityInstance 준비 전(리로드 직후 등)에 입장 신호가 조용히 사라지던 문제:
  // 잠깐 재시도하고, 끝내 실패하면 안내와 함께 입력 화면으로 복귀시킨다.
  var enterRetryTimer;
  function sendEnterWithRetry(triesLeft) {
    if (sendEnterToUnity()) return;
    if (triesLeft > 0) {
      enterRetryTimer = setTimeout(function () { sendEnterWithRetry(triesLeft - 1); }, 500);
      return;
    }
    console.warn("[venue] Unity 입장 신호 전달 실패(unityInstance 없음)");
    pendingNotice = "예식장 연결에 실패했어요. 다시 입장해 주세요.";
    showLobby();
  }

  /* ===== 입장 처리(버튼 / 자동 공통) ===== */
  function doEnter() {
    if (state.entered) return;
    state.entered = true;
    if (curtain) curtain.classList.remove("curtain--hidden");  // 입장 로딩은 커튼 위(프리뷰 가림)

    // 이름/성별 기억 (다음 방문/복귀 시 자동 입장)
    lsSet(NAME_KEY, state.name.trim());
    if (state.gender) lsSet(GENDER_KEY, state.gender);

    sendEnterWithRetry(20);   // unityInstance 준비 전이면 최대 10초 재시도
    showLoading((state.name.trim() || "하객") + "님, 예식장에 입장하는 중…");
    setDisplay(changeNameLink, "");   // 입장 중에는 "다른 이름으로 입장" 노출

    // 접속 지연 안내. (기존의 25s 강제 reveal 폴백은 제거 — 입장 실패 시 모든 UI 를 숨겨
    // "커스텀/입력 UI 가 아예 없는 화면"에 사용자를 가두던 원인. SceneReady 는 jslib 직접
    // 호출이라 유실이 사실상 없고, 실패/지연 복구는 12s 안내(새로고침)와 끊김 신호가 담당)
    clearTimeout(slowTimer);
    slowTimer = setTimeout(showSlowGuide, 12000);
  }

  function showSlowGuide() {
    if (loadingSub) loadingSub.textContent = "접속이 지연되고 있어요. 저전력 모드를 끄고 새로고침 해 주세요.";
    setDisplay(retryBtn, "");
  }

  /* ===== 로비 입력 ===== */
  function refreshNext() {
    nextBtn.disabled = !(state.name.trim() && state.gender);
  }
  nameInput.addEventListener("input", function () { state.name = nameInput.value; refreshNext(); });
  genderBtns.forEach(function (btn) {
    btn.addEventListener("click", function () {
      state.gender = btn.getAttribute("data-gender");
      genderBtns.forEach(function (b) { b.setAttribute("aria-pressed", b === btn ? "true" : "false"); });
      refreshNext();
      initLookForGender();   // 성별 기본 룩으로 프리뷰 갱신
    });
  });

  nextBtn.addEventListener("click", function () {
    // 이름 입력 키보드가 남긴 뷰포트 팬(offsetTop) 청소 — 시트가 화면 밖에서 시작하는 것 방지
    // (iOS WebKit: 키보드 닫힘 후 visualViewport.offsetTop 이 복원되지 않는 버그 대응)
    if (document.activeElement && document.activeElement.blur) document.activeElement.blur();
    window.scrollTo(0, 0);
    hide(step1); show(step2);
    resetLift();                                             // 이전에 끌어올린 시트 위치 초기화
    if (curtain) curtain.classList.add("curtain--hidden");   // Unity 프리뷰 노출
    initLookForGender();                                     // 프리뷰 성별 룩 보장
  });
  prevBtn.addEventListener("click", function () {
    hide(step2); show(step1);
    if (curtain) curtain.classList.remove("curtain--hidden"); // 커튼 복원
  });
  enterBtn.addEventListener("click", doEnter);

  if (retryBtn) retryBtn.addEventListener("click", function () { location.reload(); });
  if (changeNameLink) changeNameLink.addEventListener("click", function () {
    lsDel(NAME_KEY); lsDel(GENDER_KEY); lsDel(LOOK_KEY);  // 기억 삭제 후 새로고침
    location.reload();
  });

  /* =====================================================================
   * iOS 뷰포트 복구 워치독 — 시트가 "시각적 뷰포트 밖으로 밀리는" 문제 복구
   *  원인 부류: 더블탭 스마트 줌(터치액션으로 예방), 키보드 잔여 팬, 툴바 토글 시
   *  visual viewport 가 layout viewport 에서 어긋나면 하단 시트가 화면 밖에 놓인다.
   *  → 어긋남(스크롤/팬)을 감지하면 지오메트리를 원위치로 복구한다.
   *  (이전의 display 토글 재페인트는 지오메트리를 못 고치고, 툴바 애니메이션 중
   *   120Hz 연속 발화로 탭을 씹는 부작용이 있어 폐기)
   * ===================================================================== */
  var czSheet = step2 ? step2.querySelector(".custom-sheet") : null;
  var repinLast = 0;
  function repinSheet() {
    if (!czSheet || !step2 || step2.classList.contains("lobby--hidden")) return;
    var now = Date.now();
    if (now - repinLast < 250) return;    // 툴바 애니메이션 중 연속 발화 제한
    repinLast = now;
    var vv = window.visualViewport;
    var panned = window.scrollY > 0 || (vv && (vv.offsetTop > 1 || vv.offsetLeft > 1));
    if (panned) {
      // 계측: 복구 순간의 뷰포트 상태 기록 (scale>1 이면 줌, offsetTop>0 이면 팬)
      console.warn("[venue] 시트 지오메트리 복구: scrollY=" + window.scrollY +
        (vv ? " vvTop=" + vv.offsetTop.toFixed(1) + " scale=" + vv.scale.toFixed(2) : ""));
      window.scrollTo(0, 0);
    }
  }
  if (window.visualViewport) {
    window.visualViewport.addEventListener("resize", repinSheet);
    window.visualViewport.addEventListener("scroll", repinSheet);
  }
  window.addEventListener("resize", repinSheet);
  window.addEventListener("orientationchange", function () { setTimeout(repinSheet, 300); });
  window.addEventListener("pageshow", repinSheet);
  document.addEventListener("visibilitychange", function () {
    if (!document.hidden) setTimeout(repinSheet, 100);   // 앱/탭 복귀 직후 점검
  });
  // 순수 탭 연타 중엔 위 이벤트들이 안 뜰 수 있어 시트 터치 자체로도 점검
  if (czSheet) czSheet.addEventListener("touchend", function () { setTimeout(repinSheet, 50); });

  /* ---- 시트 끌어올리기(수동 복구) ----
   * 일부 인앱 브라우저(카카오톡 등)는 하단 툴바가 시트 아래를 가려도 viewport 이벤트가
   * 오지 않아 자동 복구가 불가능하다. → 시트 상단(제목 영역)을 위로 드래그하면
   * 시트 전체를 끌어올릴 수 있게 한다. (아래로 드래그하면 원위치) */
  var czHead = czSheet ? czSheet.querySelector(".custom-head") : null;
  var sheetLift = 0, liftFromY = null, liftBase = 0;
  function applyLift() {
    if (czSheet) czSheet.style.transform = sheetLift > 0 ? "translateY(-" + sheetLift + "px)" : "";
  }
  function resetLift() { sheetLift = 0; applyLift(); }
  if (czHead) {
    czHead.addEventListener("touchstart", function (e) {
      if (e.touches.length !== 1) return;
      liftFromY = e.touches[0].clientY;
      liftBase = sheetLift;
    }, { passive: true });
    czHead.addEventListener("touchmove", function (e) {
      if (liftFromY === null) return;
      var dy = liftFromY - e.touches[0].clientY;                 // 위로 끌면 +
      var max = Math.round(window.innerHeight * 0.45);           // 과도한 이동 제한
      sheetLift = Math.max(0, Math.min(max, liftBase + dy));
      applyLift();
      if (e.cancelable) e.preventDefault();                      // 드래그가 화면 팬으로 번지지 않게
    }, { passive: false });
    var liftEnd = function () { liftFromY = null; };
    czHead.addEventListener("touchend", liftEnd);
    czHead.addEventListener("touchcancel", liftEnd);
  }

  /* =====================================================================
   * 캐릭터 커스텀 (step2)
   *  - 룩 상태는 웹이 소유: cz.look = [gender, p1..p11] (-1=없음)
   *  - Unity OnPreviewReady 로 카테고리 개수 + 성별 기본값을 받아 버튼/기본룩 구성
   *  - 변경 시 SendMessage 로 Unity 프리뷰 갱신, localStorage 저장
   *  - 입장 시 ApplyLook(csv) → Wedding 플레이어에 적용 + Photon 동기화
   * ===================================================================== */
  var CATS = [
    { slot: 1, key: "skin", label: "피부", opt: false },
    { slot: 2, key: "eyes", label: "눈", opt: false },
    { slot: 3, key: "hair", label: "머리", opt: false },
    { slot: 4, key: "upper", label: "상의", opt: false },
    { slot: 5, key: "pants", label: "하의", opt: false },
    { slot: 6, key: "brows", label: "눈썹", opt: false },
    { slot: 7, key: "boots", label: "신발", opt: true },
    { slot: 8, key: "backpack", label: "가방", opt: true },
    { slot: 9, key: "beard", label: "수염", opt: true },
    { slot: 10, key: "glasses", label: "안경", opt: true },
    { slot: 11, key: "hats", label: "모자", opt: true },
  ];
  var cz = { counts: null, defaults: null, look: null, activeCat: 1, ready: false };

  var tabsEl = document.getElementById("custom-tabs");
  var csPrev = document.getElementById("cs-prev");
  var csNext = document.getElementById("cs-next");
  var csLabel = document.getElementById("cs-label");

  function czSend(method, arg) {
    var u = window.unityInstance;
    if (!u) return;
    try { u.SendMessage("WebBridge", method, arg); } catch (e) {}
  }
  function genderIndex() { return state.gender === "male" ? 0 : 1; }
  function setGenderUI(g) {
    state.gender = g;
    genderBtns.forEach(function (b) { b.setAttribute("aria-pressed", b.getAttribute("data-gender") === g ? "true" : "false"); });
    refreshNext();
  }
  function catBySlot(slot) { for (var i = 0; i < CATS.length; i++) if (CATS[i].slot === slot) return CATS[i]; return CATS[0]; }
  function countOf(slot) { var c = catBySlot(slot); return (cz.counts && cz.counts[c.key]) || 0; }
  function saveLook() { if (cz.look) lsSet(LOOK_KEY, cz.look.join(",")); }

  // Unity → 웹: 프리뷰 준비 + 카테고리 개수 + 성별 기본값
  window.OnPreviewReady = function (json) {
    try { var d = JSON.parse(json); cz.counts = d.counts; cz.defaults = { 0: d.male, 1: d.female }; }
    catch (e) { console.warn("[venue] OnPreviewReady parse fail", e); return; }
    cz.ready = true;
    buildTabs();
    // 저장된 룩 있으면 복원
    if (cz.look) {
      if (state.gender === "") setGenderUI(cz.look[0] === 0 ? "male" : "female");
      czSend("ApplyLook", cz.look.join(","));
    }
    renderSelector();
  };

  function buildTabs() {
    if (!tabsEl || !cz.counts) return;
    tabsEl.innerHTML = "";
    CATS.forEach(function (c) {
      if (countOf(c.slot) <= 0) return;   // 부위가 0개인 카테고리(예: 모자 전체 제거)는 탭 숨김
      var b = document.createElement("button");
      b.type = "button";
      b.className = "cs-tab";
      b.textContent = c.label;
      b.setAttribute("data-slot", c.slot);
      b.addEventListener("click", function () { cz.activeCat = c.slot; renderSelector(); });
      tabsEl.appendChild(b);
    });
  }

  // 성별 확정 시 룩 초기화(같은 성별 저장룩 있으면 유지) + 프리뷰 반영
  function initLookForGender() {
    if (!cz.ready || !cz.defaults) return;
    var gi = genderIndex();
    if (!(cz.look && cz.look[0] === gi)) cz.look = [gi].concat(cz.defaults[gi].slice());
    czSend("ApplyLook", cz.look.join(","));
    saveLook();
    renderSelector();
  }

  function renderSelector() {
    if (!cz.ready || !cz.look) return;
    var cat = catBySlot(cz.activeCat);
    if (tabsEl) Array.prototype.forEach.call(tabsEl.children, function (b) {
      b.classList.toggle("active", +b.getAttribute("data-slot") === cz.activeCat);
    });
    var idx = cz.look[cz.activeCat];
    var n = countOf(cz.activeCat);
    if (csLabel) csLabel.textContent = (idx < 0) ? "없음" : (cat.label + " " + (idx + 1));
    if (csPrev) csPrev.disabled = n <= 0;
    if (csNext) csNext.disabled = n <= 0;
  }

  function cycle(dir) {
    if (!cz.look) return;
    var slot = cz.activeCat, cat = catBySlot(slot), n = countOf(slot);
    if (n <= 0) return;
    var idx = cz.look[slot];
    if (cat.opt) { idx += dir; if (idx < -1) idx = n - 1; if (idx > n - 1) idx = -1; }
    else { idx = (idx + dir + n) % n; }
    cz.look[slot] = idx;
    czSend("SetPreviewPart", slot + ":" + idx);
    saveLook();
    renderSelector();
  }

  if (csPrev) csPrev.addEventListener("click", function () { cycle(-1); });
  if (csNext) csNext.addEventListener("click", function () { cycle(1); });

  /* ===== 초기 상태 ===== */
  // 기억된 이름 복원 → 있으면 자동 입장 모드
  var savedName = lsGet(NAME_KEY), savedGender = lsGet(GENDER_KEY);
  if (savedName) {
    state.name = savedName;
    if (nameInput) nameInput.value = savedName;
    if (savedGender) {
      state.gender = savedGender;
      genderBtns.forEach(function (b) { b.setAttribute("aria-pressed", b.getAttribute("data-gender") === savedGender ? "true" : "false"); });
    }
    autoEnter = true;
    refreshNext();
  }

  // 저장된 커스텀 룩 미리 로드(프리뷰 준비 전/자동입장에도 사용)
  var savedLook = lsGet(LOOK_KEY);
  if (savedLook) {
    var la = savedLook.split(",").map(Number);
    if (la.length === 12 && !la.some(isNaN)) cz.look = la;
  }

  // 계측: WebGL 컨텍스트 손실(GPU 압박) 감지 — 시트 소실과의 상관 확인용
  var unityCanvas = document.getElementById("unity-canvas");
  if (unityCanvas) unityCanvas.addEventListener("webglcontextlost", function () {
    console.warn("[venue] WebGL context lost — GPU 압박/복귀 이벤트");
  });

  showLoading("예식장을 불러오는 중…");
  // Unity 로비 준비 신호가 끝내 안 오면(로드 실패 등) 최소한 입력은 할 수 있게 노출
  lobbyFallbackTimer = setTimeout(function () {
    if (!lobbyReady && !state.entered) {
      console.warn("[venue] 로비 준비 신호 지연 — 폴백으로 입력 화면 표시");
      autoEnter = false;          // 자동입장 대신 안전하게 입력 화면
      showLobby();
    }
  }, 40000);
})();
