// guestbook.js — 공유 방명록 모듈 (청첩장 첫 페이지 + 예식장 오버레이 공용)
// Firestore 백엔드. 목록 5개씩 + 페이징 + 카드 클릭 시 전체 펼침. write 옵션으로 글쓰기 폼 표시.
// lockedName 을 주면(유니티 예식장) 이름칸 자동입력 + 수정불가.
import { initializeApp } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-app.js";
import {
  getFirestore, collection, addDoc, getDocs, query, orderBy, serverTimestamp, deleteDoc, doc, onSnapshot
} from "https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js";
import {
  getAuth, signInWithEmailAndPassword, onAuthStateChanged, signOut
} from "https://www.gstatic.com/firebasejs/10.12.5/firebase-auth.js";

// 공개돼도 안전한 클라이언트 설정 (보안은 Firestore 규칙으로). 프로젝트: hwjswedding
const firebaseConfig = {
  apiKey: "AIzaSyA0PT7VDzovTPiYKOruK-yOhZjWz-zpIF8",
  authDomain: "hwjswedding.firebaseapp.com",
  projectId: "hwjswedding",
  storageBucket: "hwjswedding.firebasestorage.app",
  messagingSenderId: "592953144182",
  appId: "1:592953144182:web:221b5c449fa1b7eed0057",
};

const app = initializeApp(firebaseConfig);
const db = getFirestore(app);
const COL = "guestbook";
const PAGE = 5;
const MAX_NAME = 30, MAX_MSG = 300;

function toEntry(d) {
  const x = d.data({ serverTimestamps: "estimate" });   // 방금 쓴 글도 시간이 바로 보이게
  return {
    id: d.id,
    name: x.name || "",
    message: x.message || "",
    at: x.createdAt && x.createdAt.toDate ? x.createdAt.toDate() : null,
  };
}

export async function fetchEntries() {
  const snap = await getDocs(query(collection(db, COL), orderBy("createdAt", "desc")));
  return snap.docs.map(toEntry);
}

// ===== 미리 받아두기(실시간 구독) — 예식장에서 방명록 창을 열기 전에 목록을 준비해 둔다 =====
// startGuestbookSync() 를 한 번 부르면 이후 목록이 바뀔 때마다 cache 가 갱신되고, 마운트된 위젯이 다시 그린다.
// 부르지 않은 페이지(청첩장 등)는 기존처럼 창을 열 때 한 번 받아온다.
let cache = null;            // 마지막으로 받은 목록(null = 아직 없음)
let syncing = false;
const cacheListeners = new Set();
export function startGuestbookSync() {
  if (syncing) return;
  syncing = true;
  onSnapshot(
    query(collection(db, COL), orderBy("createdAt", "desc")),
    (snap) => {
      cache = snap.docs.map(toEntry);
      cacheListeners.forEach((fn) => fn());
    },
    (err) => {
      console.error("[guestbook] 구독 실패 — 창을 열 때 직접 불러오기로 대체", err);
      syncing = false;
      cacheListeners.forEach((fn) => fn());   // 첫 목록을 기다리던 위젯이 직접 불러오도록
    }
  );
}

export async function addEntry(name, message) {
  name = (name || "").trim();
  message = (message || "").trim();
  if (!name || !message) throw new Error("이름과 메시지를 모두 입력해주세요.");
  if (name.length > MAX_NAME) throw new Error("이름이 너무 깁니다.");
  if (message.length > MAX_MSG) throw new Error("메시지가 너무 깁니다. (최대 " + MAX_MSG + "자)");
  await addDoc(collection(db, COL), { name, message, createdAt: serverTimestamp() });
}

// 관리자 전용 — 로그인(인증)된 상태에서만 Firestore 규칙이 삭제를 허용.
export async function deleteEntry(id) {
  await deleteDoc(doc(db, COL, id));
}
export function adminLogin(email, password) {
  return signInWithEmailAndPassword(getAuth(app), email, password);
}
export function adminLogout() {
  return signOut(getAuth(app));
}
export function onAdmin(cb) {
  return onAuthStateChanged(getAuth(app), cb);
}

function esc(s) {
  return (s || "").replace(/[&<>"']/g, (c) =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
}
function fmt(d) {
  if (!d) return "방금";
  return d.getFullYear() + "." + (d.getMonth() + 1) + "." + d.getDate();
}

/**
 * 방명록 위젯을 root 엘리먼트에 마운트.
 * opts: { write?:boolean, lockedName?:string, onPosted?:fn }
 * 반환: { reload }
 */
export function mountGuestbook(root, opts = {}) {
  const write = !!opts.write;
  const admin = !!opts.admin;
  const locked = opts.lockedName || "";
  root.classList.add("gb-widget");
  root.innerHTML =
    '<ul class="gb-list" data-gb-list></ul>' +
    '<div class="gb-pager" data-gb-pager hidden>' +
      '<button type="button" class="gb-pg" data-gb-prev aria-label="이전">‹</button>' +
      '<span class="gb-page" data-gb-page></span>' +
      '<button type="button" class="gb-pg" data-gb-next aria-label="다음">›</button>' +
    "</div>" +
    (write
      ? '<form class="gb-form" data-gb-form>' +
          '<input class="gb-name" data-gb-name type="text" maxlength="' + MAX_NAME + '" placeholder="이름" ' +
            (locked ? 'readonly value="' + esc(locked) + '"' : "required") + ">" +
          '<textarea class="gb-msg" data-gb-msg maxlength="' + MAX_MSG + '" rows="2" placeholder="축하 메시지" required></textarea>' +
          '<button type="submit" data-gb-submit>메시지 남기기</button>' +
        "</form>"
      : "");

  const listEl = root.querySelector("[data-gb-list]");
  const pagerEl = root.querySelector("[data-gb-pager]");
  const pageEl = root.querySelector("[data-gb-page]");
  const prevB = root.querySelector("[data-gb-prev]");
  const nextB = root.querySelector("[data-gb-next]");

  let entries = [], page = 0;
  const totalPages = () => Math.max(1, Math.ceil(entries.length / PAGE));

  function renderList() {
    if (!entries.length) {
      listEl.innerHTML = '<li class="gb-empty">첫 번째 축하 메시지를 남겨주세요 🌿</li>';
      pagerEl.hidden = true;
      return;
    }
    const start = page * PAGE;
    listEl.innerHTML = entries.slice(start, start + PAGE).map((m) =>
      '<li class="gb-card" tabindex="0">' +
        '<div class="gb-row">' +
          '<span class="gb-who">' + esc(m.name) + "</span>" +
          '<span class="gb-meta"><span class="gb-when">' + fmt(m.at) + "</span>" +
          '<span class="gb-more">▾</span></span>' +
        "</div>" +
        '<p class="gb-text">' + esc(m.message) + "</p>" +
        (admin ? '<button type="button" class="gb-x" data-del="' + m.id + '" aria-label="삭제">✕</button>' : "") +
      "</li>").join("");
    pagerEl.hidden = totalPages() <= 1;
    pageEl.textContent = page + 1 + " / " + totalPages();
    prevB.disabled = page <= 0;
    nextB.disabled = page >= totalPages() - 1;
  }

  function show(list, toFirst) {
    entries = list;
    if (toFirst) page = 0;
    if (page > totalPages() - 1) page = totalPages() - 1;
    renderList();
  }

  // 받아둔 목록(cache)이 있으면 기다림 없이 즉시 표시. 구독 중이면 갱신은 구독이 알아서 반영하고,
  // 구독이 없으면 화면은 이전 목록을 유지한 채 뒤에서 새로 받아 바꾼다("불러오는 중…"은 처음 한 번만).
  async function reload(toFirst) {
    if (cache) show(cache, toFirst);
    else if (!entries.length) listEl.innerHTML = '<li class="gb-empty">불러오는 중…</li>';
    else show(entries, toFirst);
    if (syncing) return;                 // 구독이 첫 목록/갱신을 cacheListeners 로 전달
    try {
      cache = await fetchEntries();
    } catch (e) {
      console.error("[guestbook] load failed", e);
      if (!entries.length) listEl.innerHTML = '<li class="gb-empty">목록을 불러오지 못했어요.</li>';
      return;
    }
    show(cache, toFirst);
  }
  // 구독 갱신 → 열려 있는 목록 다시 그림(페이지 유지). 구독이 실패해 끊겼으면 직접 불러오기.
  cacheListeners.add(() => { if (cache) show(cache, false); else if (!syncing) reload(false); });

  listEl.addEventListener("click", (e) => {
    const x = e.target.closest(".gb-x");
    if (x) {
      e.stopPropagation();
      if (confirm("삭제하시겠습니까?")) {
        deleteEntry(x.getAttribute("data-del"))
          .then(() => reload())
          .catch((err) => { console.error(err); alert("삭제에 실패했어요. (로그인/권한 확인)"); });
      }
      return;
    }
    const card = e.target.closest(".gb-card");
    if (card) card.classList.toggle("open");
  });
  prevB.addEventListener("click", () => { if (page > 0) { page--; renderList(); } });
  nextB.addEventListener("click", () => { if (page < totalPages() - 1) { page++; renderList(); } });

  if (write) {
    const formEl = root.querySelector("[data-gb-form]");
    const nameEl = root.querySelector("[data-gb-name]");
    const msgEl = root.querySelector("[data-gb-msg]");
    const subBtn = root.querySelector("[data-gb-submit]");
    formEl.addEventListener("submit", async (e) => {
      e.preventDefault();
      const nm = locked || nameEl.value;
      const label = subBtn.textContent;
      subBtn.disabled = true;
      subBtn.textContent = "남기는 중…";
      try {
        await addEntry(nm, msgEl.value);
        msgEl.value = "";
        if (!locked) nameEl.value = "";
        if (opts.onPosted) opts.onPosted();
        await reload(true);
      } catch (err) {
        alert(err.message || "저장에 실패했어요.");
      } finally {
        subBtn.disabled = false;
        subBtn.textContent = label;
      }
    });
  }

  reload(true);
  return { reload };
}
