// 이 스크립트는 "뽑기 버튼을 눌렀을 때 화면(UI)에 결과를 보여주는" 역할을 합니다.
// 확률 계산을 담당하는 GachaDrawer와, 화면을 그리는 UI Toolkit(UXML/USS) 사이를 이어주는
// "다리" 역할이라고 생각하면 됩니다.
//
// MonoBehaviour는 씬(Scene) 안의 GameObject에 붙여서 동작시키는 컴포넌트입니다.
// (2~3단계에서 만든 GachaTableSO, GachaDrawer는 씬과 상관없는 순수 데이터/로직이었지만,
//  이 스크립트는 "화면에 실제로 뭔가를 보여줘야" 하므로 MonoBehaviour로 만듭니다.)
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

        // 화면에 그려진 UI 요소(버튼/카드/글자)를 찾아서 담아둘 변수들
        private Button drawButton;
        private VisualElement card;
        private Label cardLabel;
        private Label pityLabel;

        // 실제 뽑기 확률 계산을 담당하는 객체.
        // 버튼을 누를 때마다 새로 만들지 않고 딱 1번만 만들어서 계속 재사용해야
        // 천장 카운터(PullsSincePity)가 클릭할 때마다 0으로 초기화되지 않고 이어집니다.
        private GachaDrawer drawer;

        // 등급마다 화면에 어떤 글자를 보여주고, 어떤 색상 클래스(USS class)를 입힐지 정의한 표입니다.
        // (string label, string cssClass) 처럼 괄호로 묶은 건 "튜플"이라고 하며,
        // 값 2개를 하나의 변수에 임시로 묶어서 담는 방법입니다.
        private static readonly Dictionary<GachaTier, (string label, string cssClass)> TierDisplay =
            new Dictionary<GachaTier, (string, string)>
            {
                { GachaTier.R, ("R", "tier-r") },
                { GachaTier.SR, ("SR", "tier-sr") },
                { GachaTier.SSR, ("SSR", "tier-ssr") },
                { GachaTier.LR, ("LR", "tier-lr") },
            };

        // OnEnable은 이 컴포넌트가 활성화될 때 유니티가 자동으로 호출해주는 함수입니다.
        // (Start와 비슷하지만, 오브젝트가 껐다 켜질 때마다 다시 호출된다는 차이가 있습니다.)
        private void OnEnable()
        {
            // UIDocument는 UXML(화면 구조 파일)이 실제로 그려진 결과를 들고 있는 컴포넌트입니다.
            // rootVisualElement.Q<T>("이름")으로, UXML에 적어둔 name="..." 을 기준으로
            // 원하는 화면 요소를 하나씩 찾아옵니다. (Q는 Query의 줄임말)
            var root = GetComponent<UIDocument>().rootVisualElement;
            drawButton = root.Q<Button>("draw-button");
            card = root.Q<VisualElement>("card");
            cardLabel = root.Q<Label>("card-label");
            pityLabel = root.Q<Label>("pity-label");

            drawer = new GachaDrawer(gachaTable);

            // 버튼이 클릭되면 OnDrawClicked 함수를 실행하도록 "구독"(등록)합니다.
            // += 는 "이 이벤트가 발생하면 이 함수도 같이 실행해줘"라는 의미입니다.
            drawButton.clicked += OnDrawClicked;

            UpdatePityLabel();
        }

        // OnDisable은 이 컴포넌트가 비활성화될 때(꺼질 때) 자동으로 호출됩니다.
        private void OnDisable()
        {
            // 등록했던 이벤트는 반드시 해제(-=)해줘야 합니다.
            // 해제하지 않으면 화면이 없어져도 클릭 이벤트가 계속 남아있어 오류나 메모리 낭비의 원인이 됩니다.
            if (drawButton != null) drawButton.clicked -= OnDrawClicked;
        }

        // "1회 뽑기" 버튼을 눌렀을 때 실제로 실행되는 함수입니다.
        private void OnDrawClicked()
        {
            // 1) 확률표에 따라 뽑기 결과 하나를 계산
            var result = drawer.DrawOne();
            var display = TierDisplay[result.tier];

            // 2) 이전에 붙어있던 등급 색상 클래스를 전부 지움 (R/SR/SSR/LR 클래스 중 남아있는 걸 제거)
            foreach (var entry in TierDisplay.Values)
            {
                card.RemoveFromClassList(entry.cssClass);
                cardLabel.RemoveFromClassList(entry.cssClass);
            }

            // 3) 이번에 뽑힌 등급의 색상 클래스를 새로 붙임 (USS의 .tier-lr 등이 적용됨)
            card.AddToClassList(display.cssClass);
            cardLabel.AddToClassList(display.cssClass);

            // 4) 카드에 등급 글자(R/SR/SSR/LR)를 표시
            cardLabel.text = display.label;

            // 5) 천장까지 남은 진행 상황 텍스트 갱신
            UpdatePityLabel();
        }

        private void UpdatePityLabel()
        {
            pityLabel.text = $"천장까지 {drawer.PullsSincePity} / {gachaTable.pityCount}";
        }
    }
}
