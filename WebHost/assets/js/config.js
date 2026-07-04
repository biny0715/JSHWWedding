/* =====================================================================
 * config.js — 청첩장 콘텐츠 한 곳에서 관리
 * ---------------------------------------------------------------------
 * 실제 사진/정보가 준비되면 이 파일만 수정하면 됩니다.
 * 사진은 assets/images/ 에 넣고 아래 경로만 교체하세요.
 * "TODO" 표시된 곳은 실제 정보로 반드시 바꿔주세요.
 * ===================================================================== */

window.WEDDING = {
  /* ---- 기본 정보 ---- */
  groom: {
    name: "김형원",
    short: "형원",
    role: "신랑",
    desc: "목포 바다처럼 넓은 마음을 가진", // TODO: 소개 문구 다듬기
    phone: "010-2820-2131", // TODO
  },
  bride: {
    name: "박지수",
    short: "지수",
    role: "신부",
    desc: "누구보다 따뜻한 마음을 가진", // TODO: 소개 문구 다듬기
    phone: "010-0000-0000", // TODO
  },

  date: {
    iso: "2026-11-08T11:00:00+09:00", // TODO: 정확한 예식 시간
    text: "2026년 11월 8일 일요일",
    timeText: "오전 11시", // TODO
  },

  venue: {
    name: "운림제",
    detail: "광주광역시 운림제 · 야외 한옥 예식장",
    address: "광주 동구 동산길 29",
    lat: 35.1308,
    lng: 126.9483,
    mapQuery: "운림제 광주",
    // 모바일에서 각 앱으로 바로 열리는 공유 링크 (앱 미설치 시 웹으로 폴백)
    mapLinks: {
      kakao: "https://place.map.kakao.com/9308441",
      naver: "https://naver.me/xSFCVLXf",
      tmap: "https://tmap.life/d868f2fa",
    },
    // 네이버 지도(Web Dynamic Map) Client ID — NCP 발급. 공개돼도 안전(등록 도메인에서만 동작)
    naverClientId: "84o8zgwogu",
  },

  /* ---- 혼주 정보 ---- */
  parents: {
    groom: { father: "김○○", mother: "○○○" }, // TODO
    bride: { father: "○○○", mother: "김경미" }, // TODO
  },

  /* ---- 마음 전하기 (계좌) ---- */
  // TODO: 실제 계좌번호로 교체
  accounts: {
    groom: [
      {
        label: "신랑 김형원",
        bank: "○○은행",
        number: "000-0000-0000-00",
        holder: "김형원",
      },
      {
        label: "신랑 측 혼주",
        bank: "○○은행",
        number: "000-0000-0000-00",
        holder: "김○○",
      },
    ],
    bride: [
      {
        label: "신부 박지수",
        bank: "○○은행",
        number: "000-0000-0000-00",
        holder: "박지수",
      },
      {
        label: "신부 측 혼주",
        bank: "○○은행",
        number: "000-0000-0000-00",
        holder: "김경미",
      },
    ],
  },

  /* ---- 사진 자리 ----
   * src 가 비어("")있으면 회색 플레이스홀더가 표시됩니다.
   * 실제 사진을 assets/images/ 에 넣고 경로를 채우면 자동으로 사진이 나옵니다.
   * 예: src: "assets/images/main.jpg"
   */
  mainPhoto: { src: "", alt: "메인 웨딩 사진", ratio: "3 / 4" },
  groomPhoto: { src: "", alt: "신랑 사진", ratio: "1 / 1" },
  bridePhoto: { src: "", alt: "신부 사진", ratio: "1 / 1" },

  gallery: [
    { src: "", alt: "웨딩 사진 1", ratio: "3 / 4" },
    { src: "", alt: "웨딩 사진 2", ratio: "3 / 4" },
    { src: "", alt: "웨딩 사진 3", ratio: "3 / 4" },
    { src: "", alt: "웨딩 사진 4", ratio: "3 / 4" },
    { src: "", alt: "웨딩 사진 5", ratio: "3 / 4" },
  ],

  /* ---- 모바일 예식장(Unity) 외부 호스트 주소 ----
   * 무거운 WebGL 빌드는 GitHub Pages(100MB 제한) 대신 외부 호스트(Netlify 등)에 올립니다.
   * 배포 후 받은 주소로 교체하세요. (비워두면 같은 사이트의 venue.html 로 이동)
   */
  venueUrl: "https://pub-e2f0473c1db44b0ea4d9059179c8ff75.r2.dev/index.html", // Cloudflare R2 (무료 무제한 트래픽)

  /* ---- NPC(비니) 대화 ----
   * 비니(퀘스트 NPC) 대사. 동물의 숲처럼 한 줄씩 넘어갑니다. 문구는 여기서 수정.
   * questProgress 의 {found}/{total}/{remain} 토큰은 진행 상황으로 자동 치환됩니다.
   */
  npc: {
    name: "비니",
    emoji: "🌊",
    // 퀘스트 제안(처음 or 거절 후 다시 말걸기) — 끝나면 수락/거절 선택지가 뜸
    questIntro: [
      "안녕, 나는 이 모바일 예식장을 만든 신랑, 신부의 지인 '비니'라고 해.",
      "축하해주러 와서 고마워!",
      "그런데, 문제가 생겨서 어쩌지...",
      "결혼식 물품들을 잃어 버렸어...",
      "혹시 네가 찾아 줄 수 있을까?",
      "모두 찾아주면 신부대기실에 가는 길에 화환과 축하 문구를 넣을 수 있게 해줄게",
    ],
    // 수락 직후
    questAccepted: [
      "고마워, 물품들은 이 모바일 예식장 어딘가에 숨겨져 있을거야, 가까이 가면 획득 가능해",
      "주의해야할 점은 찾는 도중에 나가면 다시 잃어버리니까 처음부터 다시 찾아야해",
      "다 찾으면 다시 여기로 와",
    ],
    // 진행 중 다시 말걸기 (힌트 포함)
    questProgress: [
      "잘 찾고 있어? 아직 {remain}개 남았어! ({found}/{total})",
      "힌트! 동쪽 해변, 서쪽 해변 바위, 연못, 남쪽 비탈길... 나머지는 이 근처에 있어~",
      "다 찾으면 다시 여기로 와!",
    ],
    // 모두 찾음 → 마지막 줄 뒤에 화환 문구 입력폼이 열림
    questComplete: [
      "우와, 전부 찾아줬구나! 정말 고마워!",
      "약속대로 화환을 걸어줄게. 축하 문구를 아래에 적어줘!",
    ],
    // 화환 제출 완료(자리가 남아 있었을 때)
    wreathThanks: [
      "화환 잘 받았어! 신부대기실 가는 길에 걸어둘게 💐",
      "정성스러운 축하는 신랑신부에게 잘 전달할게. 고마워!",
    ],
    // 화환 제출 완료(그 사이 15자리가 모두 찬 경우) — 저장은 되고 메시지는 전달됨
    wreathFullThanks: [
      "앗... 그 사이에 화환 자리 15개가 모두 차버렸어.",
      "그래도 걱정 마! 축하 메시지는 신랑신부에게 꼭 전달할게 💌",
    ],
    // 진행 중 대화에 덧붙는 화환 자리 안내 ({wreathRemain} = 남은 자리 수)
    questProgressWreath: "지금 화환 자리는 {wreathRemain}개 남아있어!",
    // 퀘스트 제안 때 화환 자리가 이미 다 찼으면 덧붙는 안내
    questIntroFullNote: "(지금은 화환 자리가 다 찼지만, 축하 메시지는 신랑신부에게 꼭 전달해줄게!)",
    // 이미 퀘스트를 끝낸 기기(화환 제출 완료)
    questDone: [
      "화환은 잘 걸어뒀어! 와줘서 정말 고마워 💐",
      "예식장 구경 편하게 하다 가!",
    ],
    // 방명록 존(축하 감사) — 대화가 끝나면 방명록 창이 열림
    celebrate: [
      "축하해주러 와줘서 고마워!",
      "방명록에 축하 메시지를 남겨줄래?",
    ],
  },

  /* ---- 도움말 NPC(지니) 대화 ----
   * 지니(신부의 동생) = 도움말 안내 NPC. 가까이 가면 '도움말' 버튼이 뜨고, 누르면 아래 대사를 한 줄씩 안내.
   * 퀘스트/선택지 없이 읽고 닫힘. 문구는 여기서 수정.
   */
  jiny: {
    name: "지니",
    emoji: "🎀",
    lines: [
      "안녕! 나는 신부의 동생 '지니'라고 해~",
      "여긴 우리 언니 결혼식을 그대로 옮겨 담은 '모바일 예식장'이야. 천천히 둘러보며 축하해줘!",
      "먼저 조작법! 화면을 꾹 누른 채 가고 싶은 방향을 향하면 캐릭터가 그쪽으로 계속 걸어가. 손을 떼면 멈춰.",
      "방명록이나 사진처럼 특별한 곳에 가까이 가면 머리 위에 버튼이 떠. 그걸 누르면 기능이 열려!",
      "맵에는 예식장, 신부대기실, 연회장이 있어. 화면 밖에 있는 목적지는 화면 가장자리 화살표가 거리와 함께 알려줘.",
      "저기 있는 비니한테 말 걸면 보물찾기 퀘스트를 받을 수 있어. 숨은 물건을 다 찾으면 축하 화환도 걸 수 있지!",
      "궁금한 게 있으면 언제든 다시 말 걸어줘. 즐거운 시간 보내~ 💐",
    ],
  },

  /* ---- 퀘스트 아이템(보물찾기) ----
   * 키 = Wedding 씬 MiniGame 하위 PickupZone 오브젝트 이름과 1:1.
   * label = 획득 팝업에 표시되는 이름, svg = 아이템 그림(인라인 SVG).
   */
  questItems: {
    MoneyBag: {
      label: "축의금 봉투",
      svg: "<svg viewBox='0 0 120 120' xmlns='http://www.w3.org/2000/svg'><ellipse cx='60' cy='106' rx='34' ry='6' fill='#ede4d3'/><rect x='34' y='16' width='52' height='84' rx='6' fill='#fbf7ef' stroke='#ddd2bd' stroke-width='2.5'/><path d='M34 22 L60 44 L86 22' fill='none' stroke='#ddd2bd' stroke-width='2.5' stroke-linejoin='round'/><rect x='34' y='56' width='52' height='11' fill='#e9a39c'/><circle cx='60' cy='61' r='13' fill='#fbf7ef' stroke='#e9a39c' stroke-width='3'/><text x='60' y='67' text-anchor='middle' font-size='15' font-weight='700' fill='#c9766e' font-family='sans-serif'>축</text><circle cx='85' cy='93' r='11' fill='#f0cf7d' stroke='#d9b154' stroke-width='2.5'/><text x='85' y='98' text-anchor='middle' font-size='12' font-weight='700' fill='#a5802f' font-family='sans-serif'>₩</text></svg>",
    },
    BrideShoes: {
      label: "신부의 구두",
      svg: "<svg viewBox='0 0 120 120' xmlns='http://www.w3.org/2000/svg'><ellipse cx='60' cy='104' rx='42' ry='6' fill='#ede4d3'/><path d='M28 62 Q42 60 52 50 Q58 44 62 46 Q66 49 62 56 Q55 68 40 72 L58 72 Q62 72 62 76 L62 80 L24 80 Q22 70 28 62 Z' fill='#f7d9d5'/><rect x='26' y='78' width='7' height='16' rx='3' fill='#e0a49d'/><path d='M48 76 Q64 74 76 62 Q83 55 88 57 Q93 60 88 68 Q80 82 62 86 L82 86 Q87 86 87 90 L87 94 L44 94 Q41 84 48 76 Z' fill='#ffffff' stroke='#eadfd0' stroke-width='2'/><rect x='46' y='92' width='8' height='17' rx='3' fill='#e9a39c'/><circle cx='87' cy='60' r='4.5' fill='#e9a39c'/><path d='M83 57 Q80 53 84 51 M91 57 Q94 53 90 51' stroke='#e9a39c' stroke-width='2.5' fill='none' stroke-linecap='round'/></svg>",
    },
    SmartPhone: {
      label: "비니의 스마트폰",
      svg: "<svg viewBox='0 0 120 120' xmlns='http://www.w3.org/2000/svg'><ellipse cx='60' cy='106' rx='30' ry='6' fill='#ede4d3'/><rect x='37' y='12' width='46' height='92' rx='11' fill='#4a443d'/><rect x='41' y='20' width='38' height='70' rx='5' fill='#cdeaf3'/><circle cx='70' cy='34' r='7' fill='#f0cf7d'/><path d='M41 64 Q49 57 57 64 T73 64 Q77 61 79 63 L79 90 L41 90 Z' fill='#8fc5d6'/><path d='M41 72 Q51 66 61 72 T79 72 L79 90 L41 90 Z' fill='#5b9fb3' opacity='.85'/><circle cx='60' cy='97' r='4' fill='#8a8378'/><rect x='52' y='15' width='16' height='3' rx='1.5' fill='#8a8378'/></svg>",
    },
    AirPassTicket: {
      label: "신혼여행 항공권",
      svg: "<svg viewBox='0 0 120 120' xmlns='http://www.w3.org/2000/svg'><ellipse cx='60' cy='104' rx='42' ry='6' fill='#ede4d3'/><g transform='rotate(-8 52 56)'><rect x='12' y='40' width='80' height='34' rx='6' fill='#fbf7ef' stroke='#ddd2bd' stroke-width='2'/><line x1='68' y1='42' x2='68' y2='72' stroke='#ddd2bd' stroke-width='2' stroke-dasharray='4 4'/><rect x='18' y='48' width='30' height='4' rx='2' fill='#e5dac5'/><rect x='18' y='58' width='22' height='4' rx='2' fill='#e5dac5'/></g><g transform='rotate(5 66 66)'><rect x='26' y='50' width='82' height='36' rx='6' fill='#8fc5d6'/><rect x='84' y='50' width='24' height='36' rx='6' fill='#5b9fb3'/><line x1='84' y1='52' x2='84' y2='84' stroke='#eaf6fa' stroke-width='2' stroke-dasharray='4 4'/><path d='M36 74 L66 58 L58 72 L66 76 L48 78 Z' fill='#ffffff'/><rect x='32' y='58' width='6' height='6' rx='2' fill='#eaf6fa'/></g></svg>",
    },
    Flower: {
      label: "부케",
      svg: "<svg viewBox='0 0 120 120' xmlns='http://www.w3.org/2000/svg'><ellipse cx='60' cy='106' rx='30' ry='5' fill='#ede4d3'/><path d='M44 60 L60 102 L76 60 Z' fill='#f6ecd8' stroke='#e2d4b8' stroke-width='2'/><path d='M37 42 Q30 34 33 25 Q42 29 43 39 Z' fill='#a8cfa4'/><path d='M83 42 Q90 34 87 25 Q78 29 77 39 Z' fill='#a8cfa4'/><circle cx='42' cy='48' r='11' fill='#e9a39c'/><circle cx='60' cy='40' r='12' fill='#f2b8b1'/><circle cx='78' cy='48' r='11' fill='#e9a39c'/><circle cx='50' cy='57' r='10' fill='#f2b8b1'/><circle cx='70' cy='57' r='10' fill='#e9a39c'/><circle cx='42' cy='48' r='4' fill='#fbf7ef'/><circle cx='60' cy='40' r='4.5' fill='#fbf7ef'/><circle cx='78' cy='48' r='4' fill='#fbf7ef'/><circle cx='50' cy='57' r='3.5' fill='#fbf7ef'/><circle cx='70' cy='57' r='3.5' fill='#fbf7ef'/><path d='M50 76 Q60 83 70 76 L65 88 Q60 92 55 88 Z' fill='#e9a39c'/><circle cx='60' cy='78' r='4.5' fill='#d9857d'/></svg>",
    },
    RestrauntTickets: {
      label: "식권 다발",
      svg: "<svg viewBox='0 0 120 120' xmlns='http://www.w3.org/2000/svg'><ellipse cx='60' cy='104' rx='40' ry='6' fill='#ede4d3'/><rect x='24' y='64' width='74' height='26' rx='5' fill='#e3d9c4' transform='rotate(-7 61 77)'/><rect x='23' y='57' width='75' height='26' rx='5' fill='#efe6d2' transform='rotate(-3 60 70)'/><rect x='22' y='48' width='76' height='28' rx='5' fill='#fbf7ef' stroke='#ddd2bd' stroke-width='2.5'/><circle cx='22' cy='62' r='5' fill='#ede4d3'/><circle cx='98' cy='62' r='5' fill='#ede4d3'/><line x1='36' y1='54' x2='36' y2='70' stroke='#ddd2bd' stroke-width='2' stroke-dasharray='3 3'/><line x1='84' y1='54' x2='84' y2='70' stroke='#ddd2bd' stroke-width='2' stroke-dasharray='3 3'/><rect x='47' y='38' width='26' height='48' rx='5' fill='#8fc5d6'/><text x='60' y='66' text-anchor='middle' font-size='13' font-weight='700' fill='#fff' font-family='sans-serif'>식권</text></svg>",
    },
  },

  /* ---- 제작자 ---- */
  makers: [
    { role: "개발", name: "어경빈", contact: "" },
    {
      role: "문의",
      name: "aa0715@naver.com",
      contact: "mailto:aa0715@naver.com",
    },
  ],

  /* ---- 인사말 ---- */
  greeting:
    "바다 건너 섬 하나,\n" +
    "그 위에 작은 한옥 마당을 지었습니다.\n" +
    "두 사람이 함께 걷기로 한 첫날,\n" +
    "귀한 걸음으로 축복해 주세요.",
};
