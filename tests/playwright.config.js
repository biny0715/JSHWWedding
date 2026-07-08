// playwright.config.js — 로컬 QA 자동화 설정
// - webServer: 의존성 없는 정적 서버로 ../WebHost 를 5177 포트에 서빙
// - 브라우저: chromium 만 사용(모바일 뷰포트 에뮬레이션). 실제 iOS Safari(WebKit)는
//   별도 수동/클라우드 검증 대상 — README 의 "자동화 vs 수동" 참고.
const { defineConfig } = require("@playwright/test");

module.exports = defineConfig({
  testDir: "./e2e",
  timeout: 30000,
  expect: { timeout: 5000 },
  fullyParallel: true,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: "http://localhost:5177",
    trace: "on-first-retry",
    screenshot: "only-on-failure",
  },
  webServer: {
    command: "node static-server.js",
    url: "http://localhost:5177",
    reuseExistingServer: true,
    timeout: 15000,
  },
  projects: [
    {
      name: "mobile-chromium",
      use: {
        browserName: "chromium",
        viewport: { width: 390, height: 844 },
        isMobile: true,
        hasTouch: true,
      },
    },
  ],
});
