# QA 자동화 테스트 — 모바일 WebGL 청첩장

로비 웹↔유니티 **상태머신 회귀 테스트**(Playwright). 실제 서비스에서 발생했던
커스텀 시트 소실 버그(BUG-01)가 재발하지 않는지를 자동으로 고정한다.

## 설계 원칙 — "버그가 사는 곳"에서 테스트한다

핵심 결함(커스텀 시트 소실·상태 전이)은 Unity 캔버스가 아니라 그 위의 HTML/JS
오버레이(`WebHost/assets/js/venue.js`)에서 발생한다. 오버레이는 실제 DOM
(`#lobby-step2` 등)과 브릿지 콜백(`window.OnWeddingLobbyReady` 등)으로 이뤄져 있어,
**Unity WebGL 자동화의 최대 난관(캔버스 내부는 DOM 셀렉터로 접근 불가)을 우회**해
브라우저 자동화로 직접 검증할 수 있다. 무거운 Unity 빌드 없이 브릿지 콜백을 직접
발화해 BUG-01 을 그대로 재현한다.

## 무엇을 검증하나 (`e2e/lobby-statemachine.spec.js`)

| 테스트 | 검증 내용 | 대응 버그 |
| --- | --- | --- |
| 중복 LobbyReady | 중복 신호에도 커스텀 시트(step2) 유지 | BUG-01 (멱등 가드) |
| 뒤늦은 SceneReady | 입장 전 SceneReady 가 시트를 숨기지 않음 | BUG-01 (entered 가드) |
| 다음 버튼 활성화 | 이름+성별 모두 채워질 때만 활성화 | 입력 흐름 |
| 뒤로가기 커튼 복원 | step2→step1 복귀 시 커튼 복원 | 상태 전이 |

## 실행 방법

Node 18+ 필요.

```bash
cd tests
npm install
npx playwright install chromium
npm test          # 테스트 실행
npm run report    # HTML 리포트 열기
```

정적 서버(`static-server.js`)가 `../WebHost` 를 5177 포트에 서빙하도록 Playwright
설정(`webServer`)에 연결돼 있어, 별도로 서버를 띄울 필요는 없다.

> Unity WebGL 빌드 산출물(`Build/*`, `TemplateData/*`)은 R2 에만 있고 로컬엔 없다.
> 그 요청은 404 가 되지만 로비 상태머신은 그와 독립적으로 동작하므로 Tier 1 테스트에는
> 영향이 없다. Firebase(CDN)는 테스트 중 차단해 오프라인·결정적으로 유지한다.

## 자동화 vs 수동 — 의도적 경계

| 대상 | 방법 | 구분 |
| --- | --- | --- |
| 로비 상태 전이·커스텀 시트 (BUG-01) | Playwright + 브릿지 콜백 | 🟢 자동 (이 저장소) |
| Unity C# 로직(퀘스트·룩·화환 수) | Unity Test Framework | 🟡 예정 |
| 화환 실시간 정렬·보안 규칙 | Firebase 에뮬레이터 | 🟡 예정 |
| 실빌드 클린 로드·콘솔 에러 0 | Playwright @ **R2 라이브 URL**(읽기 전용) | 🟡 예정 |
| 실제 iOS Safari(WebKit) 렌더링 | 실기기 / 클라우드 | 🔴 수동 |
| 카카오톡·인스타 인앱 브라우저 | 실기기 | 🔴 수동(자동화 불가) |

자동화의 목적은 "전부 자동"이 아니라, 반복·회귀에 강한 영역을 자동으로 묶어 수동
검증의 여력을 인앱 브라우저 같은 고난도 영역에 집중시키는 것이다.
