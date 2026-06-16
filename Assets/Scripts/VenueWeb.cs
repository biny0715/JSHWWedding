// VenueWeb.cs
// 유니티(C#) → 웹(JS) 호출 래퍼. Plugins/WebGL/WeddingBridge.jslib 의 함수와 1:1.
// 에디터에서는 jslib 가 없으므로 로그만 출력(스텁).
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace JSHWWedding
{
    public static class VenueWeb
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void OpenWeddingGuestbook(string name);
        [DllImport("__Internal")] private static extern void OpenWeddingAlbum();
#endif

        public static void OpenGuestbook(string name)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            OpenWeddingGuestbook(name ?? "");
#else
            Debug.Log("[VenueWeb] (에디터) 방명록 창 열기 — name=" + name);
#endif
        }

        public static void OpenAlbum()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            OpenWeddingAlbum();
#else
            Debug.Log("[VenueWeb] (에디터) 사진 앨범 창 열기");
#endif
        }
    }
}
