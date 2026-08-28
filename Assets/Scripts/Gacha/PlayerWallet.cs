// 이 파일은 "재화(뽑기에 쓰는 재화)"를 관리하는 아주 단순한 지갑(Wallet) 클래스입니다.
// 실제 상용 게임이라면 서버 저장이나 로컬 세이브 파일이 필요하지만,
// 이 프로토타입은 "확률·천장·재화 밸런스 검증"이 목적이므로
// 메모리에서만 유지되는(앱을 끄면 사라지는) 가장 단순한 형태로 만들었습니다.
//
// GachaDrawer(3단계)와 마찬가지로 MonoBehaviour가 아닌 순수 C# 클래스입니다.
// "재화 계산"은 화면(UI)과 상관없는 로직이라, UI를 몰라도 되게 분리해둔 것입니다.
namespace Gacha
{
    public class PlayerWallet
    {
        // { get; private set; } 는 "외부에서는 값을 읽을 수만 있고,
        // 이 클래스 안에서만 값을 바꿀 수 있다"는 뜻입니다.
        // → 재화는 반드시 Add/TrySpend를 통해서만 바뀌게 해서, 실수로 아무 데서나
        //   숫자를 조작해버리는 것을 막기 위함입니다.
        public int CurrentCurrency { get; private set; }

        public PlayerWallet(int startingCurrency = 0)
        {
            CurrentCurrency = startingCurrency;
        }

        // 재화를 더합니다 (예: 일일 무과금 재화 획득)
        public void Add(int amount)
        {
            CurrentCurrency += amount;
        }

        // 재화가 충분하면 실제로 차감하고 true를 반환합니다.
        // 재화가 부족하면 아무것도 바꾸지 않고 false만 반환합니다.
        // (호출하는 쪽에서 "성공했는지"를 if문으로 바로 확인할 수 있어 편리합니다.)
        public bool TrySpend(int amount)
        {
            if (CurrentCurrency < amount) return false;
            CurrentCurrency -= amount;
            return true;
        }
    }
}
