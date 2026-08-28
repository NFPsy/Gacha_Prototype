// 이 파일은 "시뮬레이션(N회 반복 뽑기)을 다 돌리고 난 뒤의 결과"를 담는 데이터 상자입니다.
// 등급별로 "실제로 몇 번 나왔는지"와, 천장 시스템이 실제로 잘 지켜졌는지를 함께 보관합니다.
using System.Collections.Generic;

namespace Gacha
{
    public class GachaSimulationResult
    {
        // 이번에 몇 번 뽑았는지 (예: 1000)
        public int TotalPulls { get; }

        // 등급별로 실제 뽑힌 횟수 (예: R -> 601, SR -> 249, ...)
        public IReadOnlyDictionary<GachaTier, int> Counts { get; }

        // 가장 희귀한 등급(LR)이 나오지 않고 버틴 "최대 연속 횟수".
        // 천장이 제대로 동작한다면 이 값은 절대 pityCount(60)를 넘지 않아야 합니다.
        public int MaxPullsWithoutRarest { get; }

        // 천장(확률이 아니라 강제 지급)으로 나온 횟수
        public int PityForcedCount { get; }

        public GachaSimulationResult(
            int totalPulls,
            Dictionary<GachaTier, int> counts,
            int maxPullsWithoutRarest,
            int pityForcedCount)
        {
            TotalPulls = totalPulls;
            Counts = counts;
            MaxPullsWithoutRarest = maxPullsWithoutRarest;
            PityForcedCount = pityForcedCount;
        }

        // 특정 등급이 "실제로" 나온 비율(%)을 계산합니다.
        // 설계값(예: LR 3%)과 이 실측값을 나란히 비교하는 것이 검증의 핵심입니다.
        public float GetActualPercent(GachaTier tier)
        {
            if (TotalPulls <= 0) return 0f;
            return Counts.TryGetValue(tier, out int count) ? (count * 100f / TotalPulls) : 0f;
        }
    }
}
