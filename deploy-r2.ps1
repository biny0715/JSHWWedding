# deploy-r2.ps1
# Unity WebGL 빌드 / 웹 레이어를 Cloudflare R2 버킷(jshw-venue)에 배포한다.
#
# 기본 (빌드 배포):
#   - D:\Build 의 Build/ 와 TemplateData/ 만 덮어쓴다(순수 Unity 빌드 결과물).
#   - index.html, assets/ 는 건드리지 않는다.
#
# -Web (웹 레이어 배포):
#   - 이 저장소의 WebHost/ 에 있는 index.html + assets/ 를 올린다.
#     (방명록·앨범·비니 대화 등 Unity<->웹 브릿지 UI. WebHost/ 가 source-of-truth)
#   - copy(삭제 없음)라 버킷의 Build/·TemplateData/ 및 기타 파일은 그대로 둔다.
#
# 사용법:
#   .\deploy-r2.ps1              # 빌드 배포 (Build/ + TemplateData/)
#   .\deploy-r2.ps1 -Web         # 웹 레이어 배포 (index.html + assets/)
#   .\deploy-r2.ps1 -DryRun      # 미리보기 (-Web 과 조합 가능)
#   .\deploy-r2.ps1 -Web -DryRun

param(
    [switch]$Web,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$BuildDir  = 'D:\Build'
$WebDir    = Join-Path $PSScriptRoot 'WebHost'
$Remote    = 'r2:jshw-venue'
$PublicUrl = 'https://pub-e2f0473c1db44b0ea4d9059179c8ff75.r2.dev/index.html'

# --- rclone 실행파일 찾기 (PATH -> winget 패키지 폴더 순) ---
$rclone = (Get-Command rclone -ErrorAction SilentlyContinue).Source
if (-not $rclone) {
    $pkgRoot = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages'
    $rclone = Get-ChildItem -Path $pkgRoot -Recurse -Filter 'rclone.exe' -ErrorAction SilentlyContinue |
              Select-Object -First 1 -ExpandProperty FullName
}
if (-not $rclone) {
    Write-Error 'rclone 를 찾을 수 없습니다. `winget install Rclone.Rclone` 로 설치하세요.'
    exit 1
}

$flags = @('--progress', '--transfers', '16', '--checkers', '16')
if ($DryRun) { $flags += '--dry-run' }
if ($DryRun) { Write-Host '[DRY-RUN] 실제 업로드는 하지 않습니다.' -ForegroundColor Yellow }
Write-Host "rclone : $rclone"

if ($Web) {
    # ---------- 웹 레이어 배포 (WebHost/) ----------
    if (-not (Test-Path (Join-Path $WebDir 'index.html'))) {
        Write-Error "웹 소스가 없습니다: $WebDir\index.html"; exit 1
    }
    Write-Host "대상   : $Remote  (WebHost/ 의 index.html + assets/ · Build/·TemplateData/ 보존)"
    Write-Host ''
    Write-Host '== index.html 업로드 ==' -ForegroundColor Cyan
    & $rclone copyto "$WebDir\index.html" "$Remote/index.html" @flags
    if ($LASTEXITCODE -ne 0) { Write-Error 'index.html 업로드 실패'; exit 1 }
    Write-Host '== assets/ 업로드(copy, 삭제 없음) ==' -ForegroundColor Cyan
    & $rclone copy "$WebDir\assets" "$Remote/assets" @flags
    if ($LASTEXITCODE -ne 0) { Write-Error 'assets/ 업로드 실패'; exit 1 }
}
else {
    # ---------- Unity 빌드 배포 (D:\Build) ----------
    if (-not (Test-Path (Join-Path $BuildDir 'Build'))) {
        Write-Error "빌드 폴더가 없습니다: $BuildDir\Build  (Unity에서 WebGL 빌드를 먼저 하세요)"; exit 1
    }
    Write-Host "대상   : $Remote  (Build/ + TemplateData/ 동기화 · index.html/assets/ 보존)"
    Write-Host ''
    Write-Host '== Build/ 동기화 ==' -ForegroundColor Cyan
    & $rclone sync "$BuildDir\Build" "$Remote/Build" @flags
    if ($LASTEXITCODE -ne 0) { Write-Error 'Build/ 동기화 실패'; exit 1 }
    Write-Host '== TemplateData/ 동기화 ==' -ForegroundColor Cyan
    & $rclone sync "$BuildDir\TemplateData" "$Remote/TemplateData" @flags
    if ($LASTEXITCODE -ne 0) { Write-Error 'TemplateData/ 동기화 실패'; exit 1 }
}

Write-Host ''
if ($DryRun) {
    Write-Host '[DRY-RUN] 미리보기 완료. 실제 배포는 -DryRun 없이 실행하세요.' -ForegroundColor Yellow
} else {
    Write-Host "배포 완료 → $PublicUrl" -ForegroundColor Green
}
