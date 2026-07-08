// static-server.js — 의존성 없는 정적 파일 서버 (../WebHost 를 서빙, 로컬 QA 테스트용)
// Unity WebGL 빌드 산출물(Build/*, TemplateData/*)은 R2 에만 있고 로컬엔 없다.
// 그 요청은 자연히 404 가 되지만 venue.js(로비 상태머신)는 그와 독립적으로 동작하므로
// Tier 1 상태머신 테스트에는 영향이 없다. (실빌드 스모크는 R2 라이브 URL 대상 — README 참고)
const http = require("http");
const fs = require("fs");
const path = require("path");

const ROOT = path.join(__dirname, "..", "WebHost");
const PORT = process.env.PORT || 5177;
const TYPES = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".mjs": "text/javascript; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".svg": "image/svg+xml",
  ".json": "application/json; charset=utf-8",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".ico": "image/x-icon",
};

http
  .createServer((req, res) => {
    let urlPath = decodeURIComponent(req.url.split("?")[0]);
    if (urlPath === "/") urlPath = "/index.html";
    const filePath = path.join(ROOT, path.normalize(urlPath));
    if (!filePath.startsWith(ROOT)) {
      res.writeHead(403);
      return res.end("forbidden");
    }
    fs.readFile(filePath, (err, data) => {
      if (err) {
        res.writeHead(404);
        return res.end("not found");
      }
      res.writeHead(200, {
        "Content-Type": TYPES[path.extname(filePath).toLowerCase()] || "application/octet-stream",
      });
      res.end(data);
    });
  })
  .listen(PORT, () => console.log("static server on http://localhost:" + PORT));
