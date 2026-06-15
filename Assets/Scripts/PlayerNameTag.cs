// PlayerNameTag.cs
// 플레이어 머리 위에 이름을 표시한다. (월드공간 TextMeshPro + 카메라 빌보드)
// 한글은 TMP 기본 폰트의 "메이플스토리 SDF" 폴백으로 렌더된다(TMP Settings 폴백 등록 필요).
// Player_2 프리팹 루트에 부착. 생성 즉시 동작하므로 씬 로드 타이밍 문제 없음.

using UnityEngine;
using TMPro;
using Photon.Pun;

namespace JSHWWedding
{
    public class PlayerNameTag : MonoBehaviourPun
    {
        [Header("이름표 설정")]
        [Tooltip("머리 위 높이(월드 단위) — 캐릭터 크기에 맞춰 조정")]
        [SerializeField] private float heightOffset = 2.0f;
        [SerializeField] private float fontSize = 4f;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color outlineColor = new Color(0.27f, 0.22f, 0.24f, 1f);

        private Transform tagTf;
        private Camera cam;

        private void Start()
        {
            string playerName =
                (photonView != null && photonView.Owner != null && !string.IsNullOrEmpty(photonView.Owner.NickName))
                    ? photonView.Owner.NickName
                    : PhotonNetwork.NickName;
            if (string.IsNullOrEmpty(playerName)) playerName = "하객";

            var go = new GameObject("NameTag");
            tagTf = go.transform;
            tagTf.SetParent(transform, false);
            tagTf.localPosition = new Vector3(0f, heightOffset, 0f);

            var tmp = go.AddComponent<TextMeshPro>();   // 월드공간 3D 텍스트
            tmp.text = playerName;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = fontSize;
            tmp.color = textColor;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.18f;
            tmp.outlineColor = outlineColor;

            var rt = tmp.rectTransform;
            rt.sizeDelta = new Vector2(8f, 2f);
        }

        private void LateUpdate()
        {
            if (tagTf == null) return;
            if (cam == null) cam = ResolveCamera();   // 한 번 잡으면 캐시
            if (cam == null) return;

            // 카메라 평면과 나란히 → 캐릭터 회전과 무관하게 항상 카메라 정면을 향함(읽기 좋음)
            tagTf.rotation = cam.transform.rotation;
        }

        // 씬에 카메라가 여러 개(물 반사 등, 태그까지 복제될 수 있음).
        // 반사 카메라는 RenderTexture 에 그리므로, 화면에 직접 그리는(targetTexture==null)
        // 카메라 중 MainCamera 태그를 우선 선택해 실제 게임플레이 카메라를 잡는다.
        private static Camera ResolveCamera()
        {
            var all = Camera.allCameras;
            foreach (var c in all)
                if (c != null && c.targetTexture == null && c.CompareTag("MainCamera")) return c;
            foreach (var c in all)
                if (c != null && c.targetTexture == null) return c;
            return Camera.main;
        }
    }
}
