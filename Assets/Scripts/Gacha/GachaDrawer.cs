// 이 파일은 실제로 "뽑기 한 번"을 수행하는 로직입니다.
// - 등급별 확률에 맞춰 랜덤으로 등급을 고르고 (가중치 랜덤, weighted random)
// - 정해진 횟수(천장) 안에 최상위 등급이 안 나오면 강제로 지급합니다 (천장/피티 시스템)
//
// MonoBehaviour(씬에 붙이는 컴포넌트)로 만들지 않고 순수 C# 클래스로 만든 이유:
// UI 화면뿐 아니라 "검증 시뮬레이터"(수천 번 반복 뽑기)에서도 그대로 재사용하기 위함입니다.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gacha
{
    public class GachaDrawer
    {
        private readonly GachaTableSO table;                 // 확률/천장/가치점수가 담긴 데이터
        private readonly Random random;                       // 난수 생성기
        private readonly List<GachaTierEntry> orderedTiers;   // 희귀한 등급부터 정렬해둔 목록
        private readonly GachaTierEntry rarestEntry;          // 가장 희귀한 등급 (천장 보장 대상)

        // 마지막으로 최상위 등급을 뽑은 이후, 지금까지 몇 번을 뽑았는지 세는 카운터
        public int PullsSincePity { get; private set; }

        public GachaDrawer(GachaTableSO table, Random random = null)
        {
            this.table = table;
            // random을 넘겨주지 않으면 매번 다른 결과가 나오는 기본 난수 생성기를 사용하고,
            // 테스트/검증 시에는 시드(seed)를 고정한 Random을 넘겨 같은 결과를 재현할 수 있습니다.
            this.random = random ?? new Random();

            // 희귀한 등급(숫자가 큰 등급)부터 정렬 — 꼭 이 순서일 필요는 없지만
            // "희귀한 걸 먼저 확인한다"는 순서가 코드를 읽을 때 더 직관적입니다.
            orderedTiers = table.tiers.OrderByDescending(t => (int)t.tier).ToList();
            rarestEntry = table.GetRarestEntry();
        }

        // 뽑기를 1번 실행하고 결과를 돌려줍니다.
        public GachaDrawResult DrawOne()
        {
            // 이번 뽑기도 카운터에 포함시킴
            PullsSincePity++;

            // 카운터가 천장 횟수에 도달했다면 → 확률 무시하고 강제로 최상위 등급 지급
            bool forcedByPity = rarestEntry != null && PullsSincePity >= table.pityCount;

            GachaTierEntry result = forcedByPity ? rarestEntry : RollWeighted();

            // 이번에 최상위 등급이 나왔다면(천장이든 순수 확률이든) 카운터를 0으로 리셋
            if (rarestEntry != null && result.tier == rarestEntry.tier)
                PullsSincePity = 0;

            return new GachaDrawResult(result.tier, result.valueScore, forcedByPity);
        }

        // 뽑기를 여러 번 반복해서 결과 목록을 돌려줍니다. (검증 시뮬레이터에서 사용)
        public List<GachaDrawResult> DrawMany(int count)
        {
            var results = new List<GachaDrawResult>(count);
            for (int i = 0; i < count; i++) results.Add(DrawOne());
            return results;
        }

        // 확률표에 맞춰 등급 하나를 랜덤으로 골라내는 핵심 로직입니다.
        // 예) R 60%, SR 25%, SSR 12%, LR 3% 라면 0~100 사이의 난수를 하나 뽑아서
        //     그 값이 어느 "구간"에 들어가는지로 등급을 결정합니다.
        //     구간: [0~3)=LR, [3~15)=SSR, [15~40)=SR, [40~100]=R  (누적합 방식)
        private GachaTierEntry RollWeighted()
        {
            double roll = random.NextDouble() * 100.0; // 0.0 ~ 100.0 사이의 난수
            double cumulative = 0; // 누적 확률

            foreach (var entry in orderedTiers)
            {
                cumulative += entry.probabilityPercent;
                if (roll <= cumulative) return entry; // 난수가 누적 구간 안에 들어오면 이 등급으로 확정
            }

            // 부동소수점 오차 등으로 어떤 구간에도 안 걸리는 극히 드문 경우를 대비한 안전장치
            return orderedTiers[orderedTiers.Count - 1];
        }
    }
}
