using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>상점 탭: 블라인드 마시장(스탯 비공개) + 소모품</summary>
    public class ShopView : RanchViewBase
    {
        [Header("블라인드 마시장 (texts: 0 출품 번호, 1 성별·털색, 2 ??? 스탯, 3 안내, 4 버튼 / swatches: 0 털색)")]
        [SerializeField] private UIRow[] lotCards;
        [SerializeField] private Button refreshButton;
        [SerializeField] private TMP_Text refreshLabel;
        [SerializeField] private TMP_Text noteText;

        [Header("소모품 (texts: 0 이름·보유, 1 가격, 2 설명, 3 버튼)")]
        [SerializeField] private UIRow[] itemRows;

        private void Awake()
        {
            for (int i = 0; i < lotCards.Length; i++) lotCards[i].Bind(i, OnBuyLot);
            for (int i = 0; i < itemRows.Length; i++) itemRows[i].Bind(i, OnBuyItem);
            refreshButton.onClick.AddListener(() => Game.RefreshLots());
        }

        public override void Refresh()
        {
            var s = Game.State;
            var b = Data.balance;
            string hidden = "";
            for (int i = 0; i < RanchConst.StatCount; i++) hidden += Data.stats[i].shortName + " ?   " + (i == 2 ? "\n" : "");

            for (int i = 0; i < lotCards.Length; i++)
            {
                var card = lotCards[i];
                bool has = i < s.lots.Count;
                card.gameObject.SetActive(has);
                if (!has) continue;
                var lot = s.lots[i];
                card.SetText(0, "출품 " + lot.no + "번");
                card.SetText(1, (lot.male ? "수말" : "암말") + " · " + runner.Database.CoatName(lot.coat));
                card.SetText(2, hidden);
                card.SetText(3, "스탯 합계 " + b.blindStatMin + "~" + b.blindStatMax + " · 구매 시 공개");
                card.SetText(4, lot.sold ? "낙찰 완료" : Gold(lot.price) + " 구매");
                card.SetSwatch(0, runner.Database.CoatColor(lot.coat));
                card.SetInteractable(!lot.sold);
                card.SetBackground(lot.sold ? UITheme.Panel2 : UITheme.Panel);
            }
            refreshLabel.text = "출품 새로고침 " + Gold(b.refreshPrice);
            noteText.text = "출품마는 매일 바뀝니다. 한 스탯만 10 이상인 특화마 6종을 모으면 「말 도박꾼」 업적을 얻습니다. 환불은 없습니다.\n"
                + "특화마 도감 " + Game.SpecialistCount + "/6" + (s.discount ? " · <color=#B8860B>사령관 할인 적용 중</color>" : "");

            for (int i = 0; i < itemRows.Length && i < Data.items.Length; i++)
            {
                var it = Data.items[i];
                var row = itemRows[i];
                row.SetText(0, it.displayName + "  <size=75%><color=#566458>보유 " + s.items[(int)it.type] + "</color></size>");
                row.SetText(1, Gold(it.price));
                row.SetText(2, it.description);
                row.SetText(3, "구매");
                row.SetInteractable(s.gold >= it.price);
            }
        }

        private void OnBuyLot(int i) { Game.BuyLot(i); }
        private void OnBuyItem(int i) { Game.BuyItem(Data.items[i].type); }
    }
}
