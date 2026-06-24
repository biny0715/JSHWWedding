// PlayerCustomizationApply.cs
// Male/FemaleCharacter 프리팹에 붙인다. 스폰 시 PhotonView.InstantiationData(11 int)를 읽어
// 모든 클라이언트(소유자 + 원격)에서 동일하게 외형을 조립한다 → 다른 사람도 내 커스텀 옷을 본다.
// 데이터가 없으면(에디터 직접 플레이 등) 프리팹 기본 외형을 유지.
using UnityEngine;
using Photon.Pun;

namespace JSHWWedding.Customization
{
    [RequireComponent(typeof(PhotonView))]
    public class PlayerCustomizationApply : MonoBehaviourPun
    {
        [Tooltip("이 프리팹의 성별 (0=Male, 1=Female)")]
        public int gender = 1;
        [Tooltip("부위 매니페스트 (Editor 스크립트로 연결)")]
        public GastroPartManifest manifest;

        void Start()
        {
            object[] data = photonView.InstantiationData;
            if (data == null || data.Length == 0) return;   // 커스텀 없음 → 프리팹 기본 유지

            var look = CharacterLook.FromInstantiationData(data, gender);

            var assembler = GetComponent<CharacterAssembler>();
            if (assembler == null) assembler = gameObject.AddComponent<CharacterAssembler>();
            assembler.manifest = manifest;
            assembler.ApplyFull(look);   // 베이크된 부위 제거 후 룩대로 재조립(+RefreshAnimators)
        }
    }
}
