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
   * 예식장에서 '비니'에게 말 걸면(말걸기) 나오는 대사. 동물의 숲처럼 한 줄씩 넘어갑니다.
   * lines 배열만 바꾸면 대사를 수정할 수 있어요.
   */
  npc: {
    name: "비니",
    emoji: "🌊",
    lines: [
      "안녕! 나는 이 예식장을 안내하는 비니라고 해. 🌊",
      "화면을 꾹 누르면 그 방향으로 걸어가. 손을 떼면 멈춰!",
      "꽃 장식 근처에선 방명록을, 사진 앞에서는 앨범을 열 수 있어.",
      "형원이랑 지수의 새 출발을 축하해줘서 고마워. 즐겁게 놀다 가! 💐",
    ],
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
