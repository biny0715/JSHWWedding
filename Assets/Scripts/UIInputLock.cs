// UIInputLock.cs
// 웹 오버레이(방명록/사진앨범 등)가 열려있는 동안 플레이어 이동 입력을 막는 전역 플래그.
// InteractionZone 이 버튼 클릭 시 Locked=true, WebLobbyBridge.OnVenueOverlayClosed() 에서 Locked=false.
public static class UIInputLock
{
    public static bool Locked;
}
