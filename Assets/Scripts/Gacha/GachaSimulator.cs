// 이 파일은 "설계한 확률표가 실제로 잘 맞는지" 검증하기 위해
// 뽑기를 N번(예: 1000번) 반복 실행해보는 시뮬레이터입니다.
//
// 기획서 8장의 검증 방법: "1,000회 이상 반복 뽑기를 실행해 설계한 확률표와
// 실제 결과 분포가 일치하는지 비교·검증한다" 를 그대로 코드로 옮긴 것입니다.
//
// static class(정적 클래스)로 만든 이유: 이 기능은 어떤 상태(state)도 유지할 필요 없이
// "확률표를 넣으면 결과가 나오는" 단순한 계산 함수 하나면 충분하기 때문입니다.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gacha
{
    public static class GachaSimulator
    {
        // table: 검증할 확률표, pullCount: 몇 번 반복해서 뽑아볼지
        public static GachaSimulationResult Run(GachaTableSO table, int pullCount, Random random = null)
        {
            // 실제 플레이(화면의 GachaUIController)와는 완전히 별개의 GachaDrawer를 새로 만듭니다.
            // 그래야 "검증용 시뮬레이션"이 플레이어의 실제 천장 진행 상황에 영향을 주지 않습니다.
            var drawer = new GachaDrawer(table, random ?? new Random());
            var rarestTier = table.GetRarestEntry()?.tier;

            // 등급별 횟수를 세기 위한 표. 처음엔 모든 등급을 0으로 초기화합니다.
            var counts = new Dictionary<GachaTier, int>();
            foreach (var entry in table.tiers)
                counts[entry.tier] = 0;

            int pityForcedCount = 0;
            int maxGap = 0;      // 지금까지 확인된 "최대" 연속 미획득 횟수
            int currentGap = 0;  // 마지막으로 최상위 등급을 뽑은 뒤 지금까지의 연속 횟수

            // "최상위 등급을 뽑기까지 몇 번 걸렸는지"를 5회 단위 구간으로 묶어서 셉니다.
            // (예: 천장이 60이면 구간은 12개: 1~5, 6~10, ... 56~60회)
            const int bucketSize = 5;
            int bucketCount = Math.Max(1, (int)Math.Ceiling(table.pityCount / (double)bucketSize));
            var histogram = new int[bucketCount];

            for (int i = 0; i < pullCount; i++)
            {
                var result = drawer.DrawOne();
                counts[result.tier] = counts.TryGetValue(result.tier, out int c) ? c + 1 : 1;

                if (result.pityTriggered) pityForcedCount++;

                if (rarestTier.HasValue && result.tier == rarestTier.Value)
                {
                    // 최상위 등급이 나왔으니 연속 미획득 기록을 갱신하고 다시 0부터 셉니다.
                    if (currentGap > maxGap) maxGap = currentGap;

                    int gapLength = currentGap + 1; // 이번 성공 뽑기까지 포함한 길이
                    int bucketIndex = Math.Min(bucketCount - 1, (gapLength - 1) / bucketSize);
                    histogram[bucketIndex]++;

                    currentGap = 0;
                }
                else
                {
                    currentGap++;
                }
            }
            if (currentGap > maxGap) maxGap = currentGap; // 시뮬레이션이 끝난 시점까지의 미획득 구간도 반영

            return new GachaSimulationResult(pullCount, counts, maxGap, pityForcedCount, histogram, bucketSize);
        }
    }
}
