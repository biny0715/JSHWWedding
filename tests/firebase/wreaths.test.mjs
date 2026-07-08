// wreaths.test.mjs — 축하 화환 Firestore 통합 테스트 (에뮬레이터)
// 검증 대상(앱 규약, wreaths.js / index.html):
//   - 작성순: orderBy("createdAt","asc") → 화환 슬롯 0..14 = 작성 순서
//   - 15슬롯 표시 캡: SetWreathCount = min(len, 15) (초과분 저장은 되나 미표시)
//   - 보안 규칙(제안): 공개 읽기 / 길이 검증 생성 / 수정·삭제 불가
import { readFileSync } from "node:fs";
import { test, before, after, beforeEach } from "node:test";
import assert from "node:assert/strict";
import {
  initializeTestEnvironment,
  assertSucceeds,
  assertFails,
} from "@firebase/rules-unit-testing";
import {
  collection, addDoc, getDocs, query, orderBy, updateDoc, doc, Timestamp,
} from "firebase/firestore";

const COL = "wreaths";
const displayCount = (n) => Math.min(n, 15); // index.html: SetWreathCount(min(len,15))
const wreath = (author, message, createdAt) => ({ author, message, deviceId: "dev-x", createdAt });

let env;

before(async () => {
  env = await initializeTestEnvironment({
    projectId: "demo-jshw",
    firestore: {
      rules: readFileSync(new URL("./firestore.rules", import.meta.url), "utf8"),
      host: "127.0.0.1",
      port: 8080,
    },
  });
});
after(async () => { if (env) await env.cleanup(); });
beforeEach(async () => { await env.clearFirestore(); });

test("작성순: createdAt asc 로 작성 순서대로 반환된다", async () => {
  const db = env.unauthenticatedContext().firestore();
  const col = collection(db, COL);
  await addDoc(col, wreath("가", "첫번째", Timestamp.fromMillis(1000)));
  await addDoc(col, wreath("나", "두번째", Timestamp.fromMillis(2000)));
  await addDoc(col, wreath("다", "세번째", Timestamp.fromMillis(3000)));

  const snap = await getDocs(query(col, orderBy("createdAt", "asc")));
  assert.deepEqual(snap.docs.map((d) => d.data().author), ["가", "나", "다"]);
});

test("15슬롯 캡: 초과분은 표시에서 제외(저장은 됨)", () => {
  assert.equal(displayCount(3), 3);
  assert.equal(displayCount(15), 15);
  assert.equal(displayCount(20), 15);
});

test("규칙: 유효한 화환 생성은 허용된다", async () => {
  const db = env.unauthenticatedContext().firestore();
  await assertSucceeds(addDoc(collection(db, COL), wreath("하객", "축하해요 💐", Timestamp.now())));
});

test("규칙: 50자 초과 메시지는 거부된다", async () => {
  const db = env.unauthenticatedContext().firestore();
  await assertFails(addDoc(collection(db, COL), wreath("하객", "a".repeat(51), Timestamp.now())));
});

test("규칙: 빈 메시지는 거부된다", async () => {
  const db = env.unauthenticatedContext().firestore();
  await assertFails(addDoc(collection(db, COL), wreath("하객", "", Timestamp.now())));
});

test("규칙: 이름 30자 초과는 거부된다", async () => {
  const db = env.unauthenticatedContext().firestore();
  await assertFails(addDoc(collection(db, COL), wreath("이".repeat(31), "축하", Timestamp.now())));
});

test("규칙: 작성된 화환은 수정할 수 없다", async () => {
  let id;
  await env.withSecurityRulesDisabled(async (ctx) => {
    const ref = await addDoc(collection(ctx.firestore(), COL), wreath("하객", "원본", Timestamp.now()));
    id = ref.id;
  });
  const db = env.unauthenticatedContext().firestore();
  await assertFails(updateDoc(doc(db, COL, id), { message: "변조" }));
});
