// lobby-statemachine.spec.js
// Tier 1 — 로비 웹↔유니티 상태머신 회귀 테스트 (venue.js)
//
// 핵심: 버그(커스텀 시트 소실)가 Unity 캔버스가 아니라 그 위 HTML/JS 오버레이에서
// 발생하므로, 브릿지 콜백(window.On*)을 직접 발화해 무거운 Unity 빌드 없이 검증한다.
//
// 외부 의존(Firebase CDN)은 차단해 오프라인·결정적으로 유지한다.
// venue.js 상태머신은 Firebase 와 독립적으로 동작한다.
const { test, expect } = require("@playwright/test");

test.beforeEach(async ({ page }) => {
  await page.route(/gstatic\.com|googleapis\.com|firebaseio\.com/, (r) => r.abort());
});

// 로비 준비 신호 → 이름 입력 화면(step1) 노출
async function gotoLobby(page) {
  await page.goto("/");
  await page.evaluate(() => window.OnWeddingLobbyReady());
  await expect(page.locator("#lobby-step1")).not.toHaveClass(/lobby--hidden/);
}

// step1(이름/성별) → step2(커스텀 시트)
async function advanceToStep2(page) {
  await page.fill("#visitor-name", "테스트하객");
  await page.click(".gender-btn"); // 성별 선택 → '다음' 활성화
  await expect(page.locator("#lobby-next-btn")).toBeEnabled();
  await page.click("#lobby-next-btn");
  await expect(page.locator("#lobby-step2")).not.toHaveClass(/lobby--hidden/);
}

test("BUG-01: 중복 LobbyReady 신호가 커스텀 시트를 숨기지 않는다", async ({ page }) => {
  await gotoLobby(page);
  await advanceToStep2(page);

  // 재발 조건 재현: 유니티가 LobbyReady 를 중복 발화
  await page.evaluate(() => window.OnWeddingLobbyReady());

  // 멱등 가드로 시트가 유지되어야 함 (수정 전엔 hide(step2)로 사라짐)
  await expect(page.locator("#lobby-step2")).not.toHaveClass(/lobby--hidden/);
  await expect(page.locator("#lobby-step1")).toHaveClass(/lobby--hidden/);
});

test("BUG-01: 입장 전 도착한 SceneReady 가 커스텀 시트를 숨기지 않는다", async ({ page }) => {
  await gotoLobby(page);
  await advanceToStep2(page);

  // 입장(entered) 전 뒤늦은 SceneReady → reveal()의 state.entered 가드로 무시돼야 함
  await page.evaluate(() => window.OnWeddingSceneReady());

  await expect(page.locator("#lobby-step2")).not.toHaveClass(/lobby--hidden/);
});

test("로비 입력: 이름+성별이 모두 채워질 때만 '다음'이 활성화된다", async ({ page }) => {
  await gotoLobby(page);
  await expect(page.locator("#lobby-next-btn")).toBeDisabled();

  await page.fill("#visitor-name", "지수");
  await expect(page.locator("#lobby-next-btn")).toBeDisabled(); // 성별 미선택

  await page.click('.gender-btn[data-gender="female"]');
  await expect(page.locator("#lobby-next-btn")).toBeEnabled();
});

test("로비 뒤로가기: step2 → step1 복귀 시 커튼이 복원된다", async ({ page }) => {
  await gotoLobby(page);
  await advanceToStep2(page);

  await page.click("#lobby-prev-btn");
  await expect(page.locator("#lobby-step1")).not.toHaveClass(/lobby--hidden/);
  await expect(page.locator("#lobby-step2")).toHaveClass(/lobby--hidden/);
  await expect(page.locator("#venue-curtain")).not.toHaveClass(/curtain--hidden/);
});
