// 이 파일은 "등급 하나"에 대한 설정 데이터를 정의합니다.
// 예를 들어 "SSR 등급은 확률 12%, 가치점수 16, 보라색으로 표시" 같은 정보를 담습니다.
//
// [Serializable]을 붙이면 유니티 인스펙터(Inspector) 창에서 이 값들을
// 눈으로 보고 직접 수정할 수 있게 됩니다.
using System;
using UnityEngine;

namespace Gacha
{
    [Serializable]
    public class GachaTierEntry
    {
        // 이 항목이 어떤 등급(R/SR/SSR/LR)인지
        public GachaTier tier;

        // 뽑힐 확률 (0~100 사이의 %, 예: 60 이면 60%)
        // [Range(0f, 100f)]를 붙이면 인스펙터에서 슬라이더로 표시되어
        // 실수로 100을 넘는 값을 입력하는 것을 방지해줍니다.
        [Range(0f, 100f)]
        public float probabilityPercent;

        // 이 등급 아이템 1개의 "가치 점수" (기대값 계산에 사용, 등급이 높을수록 큰 값)
        public int valueScore;

        // UI에서 이 등급을 표시할 때 사용할 색상 (예: LR = 금색)
        public Color displayColor = Color.white;
    }
}
