// rsvp.js — 참석 여부 확인 모듈 (Firestore)
// 하객이 남기는 참석 의사(이름/신랑측·신부측/인원). guestbook.js·wreaths.js 와 같은 프로젝트(hwjswedding) 사용.
import { initializeApp, getApps, getApp } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-app.js";
import {
  getFirestore, collection, addDoc, serverTimestamp
} from "https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js";

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
const COL = "rsvp";
const MAX_NAME = 30;

/** 참석 의사 등록. side = "groom" | "bride", count = 참석 인원(1~20). */
export async function addRsvp(name, side, count) {
  name = (name || "").trim();
  if (!name) throw new Error("이름을 입력해주세요.");
  if (name.length > MAX_NAME) name = name.slice(0, MAX_NAME);
  if (side !== "groom" && side !== "bride") throw new Error("신랑측/신부측을 선택해주세요.");
  count = Math.max(1, Math.min(20, parseInt(count, 10) || 1));
  await addDoc(collection(db, COL), { name, side, count, createdAt: serverTimestamp() });
}
