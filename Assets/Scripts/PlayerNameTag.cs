// PlayerNameTag.cs
// 플레이어 머리 위 이름표. 자식(child)으로 두어 플레이어와 함께 생성/파괴되고 위치는 자동으로 따라감.
// 매 프레임 회전만 카메라를 향하게 갱신(부모 회전 무시) → 캐릭터가 돌아도 항상 카메라 정면.
// 한글은 TMP 폴백(메이플스토리 SDF). Player_2 프리팹 루트에 부착.

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
            tagTf = go.transform;
            tagTf.SetParent(transform, false);
            tagTf.localPosition = new Vector3(0f, heightOffset, 0f);

            var tmp = go.AddComponent<TextMeshPro>();
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
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            tagTf.rotation = cam.transform.rotation;   // 항상 카메라 정면(빌보드)
        }
    }
}
