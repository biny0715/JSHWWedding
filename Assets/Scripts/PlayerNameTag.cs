// PlayerNameTag.cs
// 플레이어 머리 위 이름표(월드 TextMeshPro 빌보드). 캐릭터 Animator의 Optimize Game Objects
// 영향을 피하려고 자식이 아닌 독립 오브젝트로 두고 위치/회전을 매 프레임 직접 갱신한다.
// ★ 주의: AddComponent<TextMeshPro>() 가 Transform 을 RectTransform 으로 "교체"하므로
//   그 전에 잡은 transform 참조는 무효가 된다. 반드시 AddComponent 이후에 transform 을 캐시할 것.
// 한글은 TMP Settings 폴백(메이플스토리 SDF)으로 렌더. Player_2 프리팹 루트에 부착.

using UnityEngine;
using TMPro;
using Photon.Pun;

namespace JSHWWedding
{
    public class PlayerNameTag : MonoBehaviourPun
    {
        [Header("이름표 설정")]
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
            var tmp = go.AddComponent<TextMeshPro>();   // 이 호출이 Transform→RectTransform 교체
            tagTf = go.transform;                        // ★ 교체 이후에 캐시
            tagTf.position = transform.position + Vector3.up * heightOffset;

            tmp.text = playerName;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = fontSize;
            tmp.color = textColor;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.18f;
            tmp.outlineColor = outlineColor;
            tmp.rectTransform.sizeDelta = new Vector2(8f, 2f);
        }

        private void LateUpdate()
        {
            if (tagTf == null) return;
            tagTf.position = transform.position + Vector3.up * heightOffset;  // 머리 위 따라가기
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            tagTf.rotation = cam.transform.rotation;                          // 항상 카메라 정면
        }

        private void OnDestroy()
        {
            if (tagTf != null) Destroy(tagTf.gameObject);   // 독립 오브젝트라 직접 정리
        }
    }
}
