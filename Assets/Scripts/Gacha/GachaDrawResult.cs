// 이 파일은 "뽑기 1회의 결과"를 담는 작은 데이터 상자입니다.
// struct(구조체)로 만들어서 가볍게(값 타입으로) 여러 번 생성해도 부담이 적도록 했습니다.
namespace Gacha
{
    public readonly struct GachaDrawResult
    {
        public readonly GachaTier tier;      // 이번에 뽑힌 등급
        public readonly int valueScore;      // 뽑힌 아이템의 가치 점수
        public readonly bool pityTriggered;  // 천장(보장) 덕분에 나온 결과인지 여부

        public GachaDrawResult(GachaTier tier, int valueScore, bool pityTriggered)
        {
            this.tier = tier;
            this.valueScore = valueScore;
            this.pityTriggered = pityTriggered;
        }
    }
}
