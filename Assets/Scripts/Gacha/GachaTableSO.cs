// 이 파일은 "가챠 확률표 전체"를 담는 데이터 애셋(ScriptableObject)입니다.
// ScriptableObject는 씬(Scene)과 별개로 프로젝트에 파일(.asset)로 저장할 수 있는
// 데이터 컨테이너입니다. 기획자가 인스펙터에서 값만 바꾸면
// 코드를 건드리지 않고도 확률/천장/재화 밸런스를 조정할 수 있습니다.
//
// [CreateAssetMenu]를 붙이면 유니티 에디터 메뉴에서
// "우클릭 > Create > Gacha > Gacha Table" 로 이 데이터 애셋을 새로 만들 수 있습니다.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gacha
{
    [CreateAssetMenu(fileName = "GachaTable", menuName = "Gacha/Gacha Table")]
    public class GachaTableSO : ScriptableObject
    {
        // 등급별 설정 목록 (R, SR, SSR, LR 각각의 확률/가치점수/색상)
        public List<GachaTierEntry> tiers = new List<GachaTierEntry>();

        [Header("Pity")] // 인스펙터에서 아래 항목들을 "Pity"라는 제목으로 묶어서 보여줌
        // 천장 횟수: 이 횟수만큼 뽑았는데도 최상위 등급이 안 나오면 강제로 지급
        public int pityCount = 60;

        [Header("Currency")]
        public int pullCost = 200;           // 뽑기 1회에 필요한 재화
        public int dailyFreeCurrency = 300;  // 무과금 유저가 하루에 얻는 재화

        // 아래는 모두 "계산된 값"(프로퍼티)입니다. 저장되는 값이 아니라
        // 위 tiers / pityCount / pullCost 값이 바뀔 때마다 자동으로 다시 계산됩니다.

        // 확률 합계 (검증용 — 100이어야 정상)
        public float TotalProbabilityPercent => tiers.Sum(t => t.probabilityPercent);

        // 확률 합계가 100%에 충분히 가까운지 (오차 0.05% 이내면 OK로 판정)
        public bool IsProbabilityValid => Mathf.Abs(TotalProbabilityPercent - 100f) < 0.05f;

        // 뽑기 1회의 기대값 = Σ(등급별 확률 × 가치점수)
        public float ExpectedValue => tiers.Sum(t => (t.probabilityPercent / 100f) * t.valueScore);

        // 특정 등급(tier)에 해당하는 설정을 찾아서 반환
        public GachaTierEntry GetEntry(GachaTier tier) => tiers.FirstOrDefault(t => t.tier == tier);

        // 가장 희귀한 등급(숫자가 가장 큰 등급, 보통 LR)을 찾아서 반환
        // 천장 시스템은 항상 "가장 희귀한 등급"을 보장 대상으로 삼기 때문에 필요합니다.
        public GachaTierEntry GetRarestEntry() => tiers.OrderByDescending(t => (int)t.tier).FirstOrDefault();

        // 천장 없이 순수 확률로만 N회 뽑았을 때, 최상위 등급을 "적어도 1번" 얻을 확률
        // 공식: 1 - (1 - 확률)^뽑은 횟수
        // 예) 확률 3%, 60회 시도 → 1 - (0.97)^60 ≈ 83.9%
        public float PityHitProbability(int pulls)
        {
            var rarest = GetRarestEntry();
            if (rarest == null) return 0f;
            float p = rarest.probabilityPercent / 100f;
            return 1f - Mathf.Pow(1f - p, pulls);
        }

        // 무과금 유저가 하루에 몇 번 뽑기를 할 수 있는지 (일일 재화 ÷ 뽑기 비용)
        public float PullsPerDay => pullCost > 0 ? (float)dailyFreeCurrency / pullCost : 0f;

        // 천장(pityCount)까지 도달하는 데 며칠이 걸리는지 (천장 횟수 ÷ 하루 뽑기 횟수)
        public float DaysToPity => PullsPerDay > 0f ? pityCount / PullsPerDay : float.PositiveInfinity;
    }
}
