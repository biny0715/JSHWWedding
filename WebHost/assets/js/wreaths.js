// wreaths.js — 축하 화환 모듈 (Firestore)
// 퀘스트(보물찾기)를 완료한 하객이 남기는 화환 메시지. 작성순으로 슬롯 0~14가 예식장에 세워진다.
// 15개가 넘어도 저장은 되며(신랑신부에게 전달), 예식장에는 첫 15개만 표시된다.
import { initializeApp, getApps, getApp } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-app.js";
import {
  getFirestore, collection, addDoc, query, orderBy, onSnapshot, serverTimestamp
} from "https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js";

// guestbook.js 와 같은 프로젝트(hwjswedding). 이미 초기화돼 있으면 재사용(중복 초기화 방지).
const firebaseConfig = {
  apiKey: "AIzaSyA0PT7VDzovTPiYKOruK-yOhZjWz-zpIF8",
  authDomain: "hwjswedding.firebaseapp.com",
  projectId: "hwjswedding",
  storageBucket: "hwjswedding.firebasestorage.app",
  messagingSenderId: "592953144182",
  appId: "1:592953144182:web:221b5c449fa1b7eed0057",
};
const app = getApps().length ? getApp() : initializeApp(firebaseConfig);
const db = getFirestore(app);
const COL = "wreaths";
const MAX_NAME = 30, MAX_MSG = 50;

/** 실시간 구독(작성순). cb(list) — list[i] = {id, author, message, deviceId}, i = 화환 슬롯(0~14만 표시됨).
 *  화환이 추가/삭제될 때마다 cb 가 다시 불린다. 반환값 = 구독 해제 함수. */
export function subscribeWreaths(cb) {
  return onSnapshot(
    query(collection(db, COL), orderBy("createdAt", "asc")),
    (snap) => {
      cb(snap.docs.map((d) => {
        const x = d.data();
        return { id: d.id, author: x.author || "", message: x.message || "", deviceId: x.deviceId || "" };
      }));
    },
    (err) => console.error("[wreaths] 구독 실패:", err)
  );
}

/** 화환 추가. author = 캐릭터 이름(자동), message ≤ 50자, deviceId = 기기 식별(중복 참여 확인용 기록). */
export async function addWreath(author, message, deviceId) {
  author = (author || "").trim() || "하객";
  message = (message || "").trim();
  if (!message) throw new Error("축하 문구를 입력해주세요.");
  if (author.length > MAX_NAME) author = author.slice(0, MAX_NAME);
  if (message.length > MAX_MSG) throw new Error("문구가 너무 깁니다. (최대 " + MAX_MSG + "자)");
  await addDoc(collection(db, COL), {
    author, message, deviceId: deviceId || "", createdAt: serverTimestamp(),
  });
}
