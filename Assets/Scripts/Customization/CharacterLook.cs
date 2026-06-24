// CharacterLook.cs
// 캐릭터 커스텀 1개를 표현하는 데이터: 성별 + 11개 부위 인덱스.
// 카테고리 슬롯(1..11) = parts 배열 인덱스(0..10):
//   1 skin(Body) 2 eyes 3 hair 4 upper 5 pants 6 brows  (필수)
//   7 boots 8 backpack 9 beard 10 glasses 11 hats        (선택, -1=없음)
// 직렬화: CSV("g,skin,..,hats") = 웹 localStorage/SendMessage,  object[11] = Photon instantiationData.
using System;

namespace JSHWWedding.Customization
{
    [Serializable]
    public class CharacterLook
    {
        public const int CategoryCount = 11;
        public int gender;               // 0=male, 1=female
        public int[] parts;              // length 11

        public CharacterLook() { parts = new int[CategoryCount]; }
        public CharacterLook(int gender, int[] parts)
        {
            this.gender = gender;
            this.parts = (parts != null && parts.Length == CategoryCount) ? parts : new int[CategoryCount];
        }

        // 카테고리 슬롯(1..11) 접근
        public int Get(int catSlot) => parts[catSlot - 1];
        public void Set(int catSlot, int value) => parts[catSlot - 1] = value;

        // CSV "g,skin,eyes,hair,upper,pants,brows,boots,backpack,beard,glasses,hats"
        public static CharacterLook Parse(string csv)
        {
            var look = new CharacterLook();
            if (string.IsNullOrEmpty(csv)) return look;
            var a = csv.Split(',');
            if (a.Length > 0) look.gender = ParseInt(a[0], 0);
            for (int i = 0; i < CategoryCount; i++)
                look.parts[i] = ParseInt(i + 1 < a.Length ? a[i + 1] : null, i < 6 ? 0 : -1);
            return look;
        }

        public string ToCsv()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(gender);
            for (int i = 0; i < CategoryCount; i++) { sb.Append(','); sb.Append(parts[i]); }
            return sb.ToString();
        }

        // Photon: 성별은 프리팹이 정하므로 부위 11개만 전송
        public object[] ToInstantiationData()
        {
            var d = new object[CategoryCount];
            for (int i = 0; i < CategoryCount; i++) d[i] = parts[i];
            return d;
        }

        public static CharacterLook FromInstantiationData(object[] d, int gender)
        {
            var look = new CharacterLook { gender = gender };
            if (d != null)
                for (int i = 0; i < CategoryCount && i < d.Length; i++) look.parts[i] = Convert.ToInt32(d[i]);
            return look;
        }

        public CharacterLook Clone() => new CharacterLook(gender, (int[])parts.Clone());

        static int ParseInt(string s, int fallback)
        {
            return int.TryParse((s ?? "").Trim(), out int v) ? v : fallback;
        }
    }
}
