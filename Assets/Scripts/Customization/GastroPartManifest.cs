// GastroPartManifest.cs
// 런타임에 인스턴스화할 GASTRO 부위 프리팹(=Meshes/<Cat>/*.fbx)들을 카테고리별로 들고 있는 ScriptableObject.
// 직렬화된 참조라 빌드에 포함됨(Resources 이동/Addressable 불필요).
// Editor: Tools/JSHW/Build Gastro Manifest (GastroManifestBuilder)로 자동 채움.
using UnityEngine;

namespace JSHWWedding.Customization
{
    [CreateAssetMenu(menuName = "JSHW/Gastro Part Manifest", fileName = "GastroPartManifest")]
    public class GastroPartManifest : ScriptableObject
    {
        [Tooltip("모든 부위 Animator에 넣을 컨트롤러 (Character_Movement)")]
        public RuntimeAnimatorController characterController;

        // 카테고리 슬롯 1..11 순서
        public GameObject[] skin;      // 1 (5종 고정: Brown_07,08 / Pink_06,07,08)
        public GameObject[] eyes;      // 2
        public GameObject[] hair;      // 3 (남/녀 공유 풀)
        public GameObject[] upper;     // 4
        public GameObject[] pants;     // 5
        public GameObject[] brows;     // 6
        public GameObject[] boots;     // 7  (선택)
        public GameObject[] backpack;  // 8  (선택)
        public GameObject[] beard;     // 9  (선택)
        public GameObject[] glasses;   // 10 (선택)
        public GameObject[] hats;      // 11 (선택)

        [Header("성별 기본 룩 (부위 인덱스 11개, -1=없음)")]
        public int[] maleDefaultParts = new int[CharacterLook.CategoryCount];
        public int[] femaleDefaultParts = new int[CharacterLook.CategoryCount];

        public GameObject[] ByCategory(int catSlot)
        {
            switch (catSlot)
            {
                case 1: return skin;     case 2: return eyes;     case 3: return hair;
                case 4: return upper;    case 5: return pants;    case 6: return brows;
                case 7: return boots;    case 8: return backpack; case 9: return beard;
                case 10: return glasses; case 11: return hats;
            }
            return null;
        }

        public int Count(int catSlot)
        {
            var a = ByCategory(catSlot);
            return a != null ? a.Length : 0;
        }

        // boots(7)~hats(11)는 '없음' 선택 가능
        public static bool IsOptional(int catSlot) => catSlot >= 7;

        public GameObject Prefab(int catSlot, int index)
        {
            var a = ByCategory(catSlot);
            if (a == null || index < 0 || index >= a.Length) return null;
            return a[index];
        }

        public CharacterLook DefaultFor(int gender)
        {
            var src = gender == 0 ? maleDefaultParts : femaleDefaultParts;
            var parts = new int[CharacterLook.CategoryCount];
            if (src != null) System.Array.Copy(src, parts, System.Math.Min(src.Length, parts.Length));
            return new CharacterLook(gender, parts);
        }

        // 웹에 보낼 프리뷰 정보 JSON: 카테고리별 개수 + 성별 기본 룩(부위 11개)
        // {"counts":{"skin":5,...},"male":[..11],"female":[..11]}
        public string PreviewJson()
        {
            string counts = "{" +
                "\"skin\":" + Count(1) + ",\"eyes\":" + Count(2) + ",\"hair\":" + Count(3) +
                ",\"upper\":" + Count(4) + ",\"pants\":" + Count(5) + ",\"brows\":" + Count(6) +
                ",\"boots\":" + Count(7) + ",\"backpack\":" + Count(8) + ",\"beard\":" + Count(9) +
                ",\"glasses\":" + Count(10) + ",\"hats\":" + Count(11) + "}";
            return "{\"counts\":" + counts +
                ",\"male\":" + IntArrayJson(maleDefaultParts) +
                ",\"female\":" + IntArrayJson(femaleDefaultParts) + "}";
        }

        static string IntArrayJson(int[] a)
        {
            if (a == null) return "[]";
            var sb = new System.Text.StringBuilder("[");
            for (int i = 0; i < a.Length; i++) { if (i > 0) sb.Append(','); sb.Append(a[i]); }
            sb.Append(']');
            return sb.ToString();
        }
    }
}
