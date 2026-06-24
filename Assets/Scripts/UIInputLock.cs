// UIInputLock.cs
// 웹 오버레이(방명록/사진앨범 등)가 열려있는 동안 플레이어 이동 입력을 막는 전역 플래그.
// 웹이 창을 실제로 열면 WebLobbyBridge.OnVenueOverlayOpened()→Locked=true, 닫으면 OnVenueOverlayClosed()→Locked=false.
// (Unity는 직접 잠그지 않음 → 에디터/창이 안 뜨는 경우엔 잠기지 않아 이동 가능)
public static class UIInputLock
{
    public static bool Locked;
}
