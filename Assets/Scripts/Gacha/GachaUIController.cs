// 이 스크립트는 "뽑기 버튼을 눌렀을 때 화면(UI)에 결과를 보여주는" 역할을 합니다.
// 확률 계산(GachaDrawer), 재화 관리(PlayerWallet), 검증 시뮬레이터(GachaSimulator)와
// 화면을 그리는 UI Toolkit(UXML/USS) 사이를 이어주는 "다리" 역할이라고 생각하면 됩니다.
//
// MonoBehaviour는 씬(Scene) 안의 GameObject에 붙여서 동작시키는 컴포넌트입니다.
// (GachaTableSO, GachaDrawer, PlayerWallet, GachaSimulator는 씬과 상관없는 순수 데이터/로직이었지만,
//  이 스크립트는 "화면에 실제로 뭔가를 보여줘야" 하므로 MonoBehaviour로 만듭니다.)
using System;
using System.Collections.Generic;
using Gacha;
using UnityEngine;
using UnityEngine.UIElements;

namespace GachaGame
{
    // [RequireComponent]를 붙이면, 이 스크립트를 GameObject에 붙일 때
    // UIDocument 컴포넌트가 없으면 유니티가 자동으로 같이 추가해줍니다.
    [RequireComponent(typeof(UIDocument))]
    public class GachaUIController : MonoBehaviour
    {
        [Tooltip("확률·천장·재화 값이 들어있는 데이터 애셋 (DefaultGachaTable.asset을 연결하세요)")]
        [SerializeField] private GachaTableSO gachaTable;

        [Tooltip("게임을 처음 시작할 때 가지고 있는 재화 (프로토타입 테스트용, 기본 0)")]
        [SerializeField] private int startingCurrency = 0;

        [Tooltip("'검증 시뮬레이션' 버튼을 누르면 몇 번 반복 뽑기를 실행할지")]
        [SerializeField] private int simulationPullCount = 1000;

        // 화면에 그려진 UI 요소(버튼/카드/글자)를 찾아서 담아둘 변수들
        private Button drawButton;
        private Button claimDailyButton;
        private Button simulateButton;
        private VisualElement card;
        private Label cardLabel;
        private Label pityLabel;
        private Label currencyLabel;
        private Label messageLabel;
        private Label simPityCheckLabel;

        // 등급별 시뮬레이터 막대(설계값/실측값)와 텍스트를 담아둘 표
        private Dictionary<GachaTier, VisualElement> simDesignedBars;
        private Dictionary<GachaTier, VisualElement> simActualBars;
        private Dictionary<GachaTier, Label> simTexts;

        // 실제 뽑기 확률 계산을 담당하는 객체.
        // 버튼을 누를 때마다 새로 만들지 않고 딱 1번만 만들어서 계속 재사용해야
        // 천장 카운터(PullsSincePity)가 클릭할 때마다 0으로 초기화되지 않고 이어집니다.
        private GachaDrawer drawer;

        // 재화(뽑기 비용)를 관리하는 지갑. drawer와 마찬가지로 1번만 만들어서 계속 재사용합니다.
        private PlayerWallet wallet;

        // 등급마다 화면에 어떤 글자를 보여주고, 어떤 색상 클래스(USS class)를 입힐지 정의한 표입니다.
        private static readonly Dictionary<GachaTier, (string label, string cssClass)> TierDisplay =
            new Dictionary<GachaTier, (string, string)>
            {
                { GachaTier.R, ("R", "tier-r") },
                { GachaTier.SR, ("SR", "tier-sr") },
                { GachaTier.SSR, ("SSR", "tier-ssr") },
                { GachaTier.LR, ("LR", "tier-lr") },
            };

        // OnEnable은 이 컴포넌트가 활성화될 때 유니티가 자동으로 호출해주는 함수입니다.
        private void OnEnable()
        {
            // UIDocument는 UXML(화면 구조 파일)이 실제로 그려진 결과를 들고 있는 컴포넌트입니다.
            // rootVisualElement.Q<T>("이름")으로, UXML에 적어둔 name="..." 을 기준으로
            // 원하는 화면 요소를 하나씩 찾아옵니다. (Q는 Query의 줄임말)
            var root = GetComponent<UIDocument>().rootVisualElement;
            drawButton = root.Q<Button>("draw-button");
            claimDailyButton = root.Q<Button>("claim-daily-button");
            simulateButton = root.Q<Button>("simulate-button");
            card = root.Q<VisualElement>("card");
            cardLabel = root.Q<Label>("card-label");
            pityLabel = root.Q<Label>("pity-label");
            currencyLabel = root.Q<Label>("currency-label");
            messageLabel = root.Q<Label>("message-label");
            simPityCheckLabel = root.Q<Label>("sim-pity-check-label");

            // 등급 4개(R/SR/SSR/LR)에 대해 "sim-designed-R", "sim-actual-R", "sim-text-R" 처럼
            // 이름 규칙이 일정하므로, 반복문으로 한 번에 찾아서 표에 저장해둡니다.
            simDesignedBars = new Dictionary<GachaTier, VisualElement>();
            simActualBars = new Dictionary<GachaTier, VisualElement>();
            simTexts = new Dictionary<GachaTier, Label>();
            foreach (var tier in TierDisplay.Keys)
            {
                string tierName = tier.ToString(); // 예: GachaTier.LR -> "LR"
                simDesignedBars[tier] = root.Q<VisualElement>($"sim-designed-{tierName}");
                simActualBars[tier] = root.Q<VisualElement>($"sim-actual-{tierName}");
                simTexts[tier] = root.Q<Label>($"sim-text-{tierName}");
            }

            drawer = new GachaDrawer(gachaTable);
            wallet = new PlayerWallet(startingCurrency);

            // 버튼 글자에 실제 비용/획득량을 데이터 그대로 표시 (숫자를 코드에 직접 적지 않기 위함)
            drawButton.text = $"1회 뽑기 ({gachaTable.pullCost})";
            claimDailyButton.text = $"일일 재화 받기 (+{gachaTable.dailyFreeCurrency})";
            simulateButton.text = $"{simulationPullCount:N0}회 시뮬레이션";

            // 버튼이 클릭되면 각각의 함수를 실행하도록 "구독"(등록)합니다.
            // += 는 "이 이벤트가 발생하면 이 함수도 같이 실행해줘"라는 의미입니다.
            drawButton.clicked += OnDrawClicked;
            claimDailyButton.clicked += OnClaimDailyClicked;
            simulateButton.clicked += OnSimulateClicked;

            UpdateCurrencyLabel();
            UpdatePityLabel();
        }

        // OnDisable은 이 컴포넌트가 비활성화될 때(꺼질 때) 자동으로 호출됩니다.
        private void OnDisable()
        {
            // 등록했던 이벤트는 반드시 해제(-=)해줘야 합니다.
            // 해제하지 않으면 화면이 없어져도 클릭 이벤트가 계속 남아있어 오류나 메모리 낭비의 원인이 됩니다.
            if (drawButton != null) drawButton.clicked -= OnDrawClicked;
            if (claimDailyButton != null) claimDailyButton.clicked -= OnClaimDailyClicked;
            if (simulateButton != null) simulateButton.clicked -= OnSimulateClicked;
        }

        // "1회 뽑기" 버튼을 눌렀을 때 실제로 실행되는 함수입니다.
        private void OnDrawClicked()
        {
            // 1) 재화가 충분한지 먼저 확인하고 차감을 시도합니다.
            //    TrySpend가 false를 반환하면(재화 부족) 뽑기를 진행하지 않고 안내 메시지만 보여줍니다.
            if (!wallet.TrySpend(gachaTable.pullCost))
            {
                messageLabel.text = $"재화가 부족합니다! (필요 {gachaTable.pullCost} / 보유 {wallet.CurrentCurrency})";
                return;
            }
            messageLabel.text = "";

            // 2) 확률표에 따라 뽑기 결과 하나를 계산
            var result = drawer.DrawOne();
            var display = TierDisplay[result.tier];

            // 3) 이전에 붙어있던 등급 색상 클래스를 전부 지움 (R/SR/SSR/LR 클래스 중 남아있는 걸 제거)
            foreach (var entry in TierDisplay.Values)
            {
                card.RemoveFromClassList(entry.cssClass);
                cardLabel.RemoveFromClassList(entry.cssClass);
            }

            // 4) 이번에 뽑힌 등급의 색상 클래스를 새로 붙임 (USS의 .tier-lr 등이 적용됨)
            card.AddToClassList(display.cssClass);
            cardLabel.AddToClassList(display.cssClass);

            // 5) 카드에 등급 글자(R/SR/SSR/LR)를 표시
            cardLabel.text = display.label;

            // 6) 재화 잔액과 천장 진행 상황 텍스트 갱신
            UpdateCurrencyLabel();
            UpdatePityLabel();
        }

        // "일일 재화 받기" 버튼을 눌렀을 때 실행되는 함수입니다.
        // (실제 게임이라면 "하루에 한 번만" 같은 제한이 필요하지만,
        //  이 프로토타입은 재화 흐름 자체를 확인하는 것이 목적이라 제한 없이 반복 지급되게 두었습니다.)
        private void OnClaimDailyClicked()
        {
            wallet.Add(gachaTable.dailyFreeCurrency);
            messageLabel.text = $"일일 재화 {gachaTable.dailyFreeCurrency}를 받았습니다.";
            UpdateCurrencyLabel();
        }

        // "N회 시뮬레이션" 버튼을 눌렀을 때 실행되는 함수입니다.
        // 실제 재화/천장 진행 상황(drawer, wallet)과는 완전히 별개로,
        // GachaSimulator가 독립된 가상의 뽑기를 N번 돌려서 "설계값과 실측값이 얼마나 비슷한지" 보여줍니다.
        private void OnSimulateClicked()
        {
            var result = GachaSimulator.Run(gachaTable, simulationPullCount);

            foreach (var tier in TierDisplay.Keys)
            {
                float designedPercent = gachaTable.GetEntry(tier)?.probabilityPercent ?? 0f;
                float actualPercent = result.GetActualPercent(tier);

                // 두 막대 중 더 긴 쪽을 기준으로 잡아서, 막대 길이가 트랙을 벗어나지 않게 비율을 맞춥니다.
                float scaleMax = Mathf.Max(designedPercent, actualPercent, 1f) * 1.15f;

                simDesignedBars[tier].style.width = Length.Percent(designedPercent / scaleMax * 100f);
                simActualBars[tier].style.width = Length.Percent(actualPercent / scaleMax * 100f);
                simTexts[tier].text = $"설계 {designedPercent:F1}% / 실측 {actualPercent:F1}%";
            }

            // 천장(피티) 시스템이 실제로도 지켜졌는지 함께 보여줍니다.
            // MaxPullsWithoutRarest(최대 연속 미획득 횟수)가 pityCount를 넘지 않으면 정상입니다.
            bool pityHeld = result.MaxPullsWithoutRarest <= gachaTable.pityCount;
            simPityCheckLabel.text = pityHeld
                ? $"천장 보장 확인됨: LR 미획득 최대 연속 {result.MaxPullsWithoutRarest}회 (천장 {gachaTable.pityCount}회 이내), 천장 발동 {result.PityForcedCount}회"
                : $"⚠ 천장 위반 감지: 최대 연속 {result.MaxPullsWithoutRarest}회 (천장 {gachaTable.pityCount}회 초과 — 로직을 점검하세요)";
        }

        private void UpdateCurrencyLabel()
        {
            currencyLabel.text = $"보유 재화: {wallet.CurrentCurrency}";
        }

        private void UpdatePityLabel()
        {
            pityLabel.text = $"천장까지 {drawer.PullsSincePity} / {gachaTable.pityCount}";
        }
    }
}
