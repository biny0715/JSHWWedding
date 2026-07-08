using NUnit.Framework;
using JSHWWedding.Customization;

namespace JSHWWedding.Tests.EditMode
{
    // CharacterLook(캐릭터 커스텀 룩) 직렬화/파싱 로직 단위 테스트.
    // 룩 CSV: "gender,skin,eyes,hair,upper,pants,brows,boots,backpack,beard,glasses,hats"
    //   필수 부위(skin..brows) 기본 0, 선택 부위(boots..hats) 기본 -1(없음).
    public class CharacterLookTests
    {
        const string SampleCsv = "1,3,2,5,4,6,1,0,-1,2,-1,7";

        [Test]
        public void Parse_정상12필드_성별과부위가_반영된다()
        {
            var look = CharacterLook.Parse(SampleCsv);
            Assert.AreEqual(1, look.gender);
            Assert.AreEqual(3, look.Get(1));    // skin
            Assert.AreEqual(6, look.Get(5));    // pants
            Assert.AreEqual(1, look.Get(6));    // brows
            Assert.AreEqual(7, look.Get(11));   // hats
            Assert.AreEqual(CharacterLook.CategoryCount, look.parts.Length);
        }

        [Test]
        public void Parse_빈문자열_기본생성자로_전부0()
        {
            // 빈 입력은 기본 생성자를 반환 → parts 전부 0 (fallback 경로 이전)
            var look = CharacterLook.Parse("");
            Assert.AreEqual(0, look.gender);
            for (int slot = 1; slot <= CharacterLook.CategoryCount; slot++)
                Assert.AreEqual(0, look.Get(slot), $"slot {slot}");
        }

        [Test]
        public void Parse_필수만제공_선택부위는_없음으로_채운다()
        {
            // 성별 + 필수 6개만 제공 → 선택 부위(7..11)는 -1(없음)로 방어
            var look = CharacterLook.Parse("0,1,1,1,1,1,1");
            Assert.AreEqual(1, look.Get(6));    // brows (제공됨)
            for (int slot = 7; slot <= CharacterLook.CategoryCount; slot++)
                Assert.AreEqual(-1, look.Get(slot), $"slot {slot}");
        }

        [TestCase("abc")]
        [TestCase("9")]
        [TestCase("1,x,y")]
        public void Parse_비정상값_예외없이_기본값으로_방어한다(string csv)
        {
            CharacterLook look = null;
            Assert.DoesNotThrow(() => look = CharacterLook.Parse(csv));
            Assert.IsNotNull(look);
            Assert.AreEqual(CharacterLook.CategoryCount, look.parts.Length);
        }

        [Test]
        public void ToCsv_Parse_왕복이_동일하다()
        {
            Assert.AreEqual(SampleCsv, CharacterLook.Parse(SampleCsv).ToCsv());
        }

        [Test]
        public void InstantiationData_왕복_부위가_보존된다()
        {
            var look = CharacterLook.Parse(SampleCsv);
            var restored = CharacterLook.FromInstantiationData(look.ToInstantiationData(), look.gender);
            Assert.AreEqual(look.ToCsv(), restored.ToCsv());
        }
    }
}
