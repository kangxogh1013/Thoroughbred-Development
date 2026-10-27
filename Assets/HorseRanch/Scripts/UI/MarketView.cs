using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>
    /// 시장: 사람 음식(오늘 하루 효과) + 말 음식(고른 말에게 바로 먹임).
    /// 비싼 말 음식은 스탯을 영구히 올리지만 하루 구매 개수 제한이 있다.
    /// </summary>
    public class MarketView : RanchViewBase
    {
        [Header("먹일 말 고르기")]
        [SerializeField] private Button prevHorse;
        [SerializeField] private Button nextHorse;
        [SerializeField] private TMP_Text horseText;
        [Header("고급 사료용 스탯 고르기 (6개)")]
        [SerializeField] private Button[] statButtons;
        [SerializeField] private TMP_Text[] statLabels;
        [Header("음식 행 (texts: 0 이름·제한, 1 가격, 2 설명, 3 버튼)")]
        [SerializeField] private UIRow[] foodRows;
        [SerializeField] private TMP_Text noteText;

        private int horseIndex;
        private int stat;

        private void Awake()
        {
            prevHorse.onClick.AddListener(() => { horseIndex--; Refresh(); });
            nextHorse.onClick.AddListener(() => { horseIndex++; Refresh(); });
            for (int i = 0; i < statButtons.Length; i++)
            {
                int s = i;
                statButtons[i].onClick.AddListener(() => { stat = s; Refresh(); });
            }
            for (int i = 0; i < foodRows.Length; i++) foodRows[i].Bind(i, OnBuy);
        }

        private HorseData Selected
        {
            get
            {
                var hs = Game.State.horses;
                if (hs.Count == 0) return null;
                horseIndex = ((horseIndex % hs.Count) + hs.Count) % hs.Count;
                return hs[horseIndex];
            }
        }

        public override void Refresh()
        {
            var h = Selected;
            if (h == null) horseText.text = "<color=#B3342A>먹일 말이 없습니다</color>";
            else horseText.text = "<b>" + h.name + "</b>  <size=80%><color=#566458>합계 " + h.Total.ToString("0.0") + " · 컨디션 " + h.condition.ToString("0") + " · 피로 " + h.fatigue.ToString("0") + "</color></size>";

            for (int i = 0; i < statButtons.Length && i < RanchConst.StatCount; i++)
            {
                string v = h != null ? h.stats[i].ToString("0.0") : "-";
                statLabels[i].text = Data.stats[i].displayName + " " + v;
                ((Image)statButtons[i].targetGraphic).color = i == stat ? UITheme.Accent : UITheme.Panel2;
            }

            var foods = Data.foods;
            for (int i = 0; i < foodRows.Length; i++)
            {
                var row = foodRows[i];
                bool has = i < foods.Length;
                row.gameObject.SetActive(has);
                if (!has) continue;
                var f = foods[i];
                int left = Game.FoodLeftToday(i);
                string limit = f.dailyLimit > 0 ? "  <size=75%><color=#B8860B>오늘 " + left + "/" + f.dailyLimit + "개 남음</color></size>" : "";
                row.SetText(0, (f.forHorse ? "<color=#8A6A44>[말]</color> " : "<color=#2E6FB8>[나]</color> ") + f.displayName + limit);
                row.SetText(1, Gold(f.price));
                row.SetText(2, f.description + (f.effect == FoodEffect.PermanentStat ? " → <b>" + Data.stats[stat].displayName + "</b>" : ""));
                string reason;
                bool ok = Game.CanBuyFood(i, h != null ? h.id : 0, stat, out reason);
                row.SetText(3, ok ? (f.forHorse ? "사서 먹이기" : "사서 먹기") : reason);
                row.SetInteractable(ok);
                row.SetBackground(f.dailyLimit > 0 ? UITheme.Panel2 : UITheme.Panel);
            }
            noteText.text = "말 음식은 위에서 고른 말에게 바로 먹입니다. 비싼 사료는 스탯이 <b>영구히</b> 오르지만 하루 구매 개수가 정해져 있어요 (자고 나면 초기화).";
        }

        private void OnBuy(int i)
        {
            var h = Selected;
            Game.BuyFood(i, h != null ? h.id : 0, stat);
        }
    }
}
