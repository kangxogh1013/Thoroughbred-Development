using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>마구간 탭: 말 목록, 스탯/상태, 훈련(게이지), 소모품, 판매</summary>
    public class StableView : RanchViewBase
    {
        [Header("목록")]
        [SerializeField] private UIList rosterList;
        [SerializeField] private TMP_Text rosterLabel;
        [SerializeField] private TMP_Text newsText;

        [Header("빈 마구간")]
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private GameObject emptyRoot;
        [SerializeField] private Button emptyShopButton;

        [Header("상세")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_Text chipsText;
        [SerializeField] private TMP_Text totalText;
        [SerializeField] private UIBar conditionBar;
        [SerializeField] private UIBar fatigueBar;
        [SerializeField] private TMP_Text conditionValue;
        [SerializeField] private TMP_Text fatigueValue;

        [Header("스탯 행 6개 (texts: 0 이름, 1 역할, 2 값, 3 훈련 버튼 / bars: 0)")]
        [SerializeField] private UIRow[] statRows;

        [Header("훈련 게이지")]
        [SerializeField] private TMP_Text trainingTitle;
        [SerializeField] private TMP_Text trainingDesc;
        [SerializeField] private UIBar trainingBar;
        [SerializeField] private Button drinkButton;
        [SerializeField] private TMP_Text drinkLabel;

        [Header("소모품 / 판매")]
        [SerializeField] private Button healButton;
        [SerializeField] private TMP_Text healLabel;
        [SerializeField] private Button nutrientButton;
        [SerializeField] private TMP_Text nutrientLabel;
        [SerializeField] private Button tackButton;
        [SerializeField] private TMP_Text tackLabel;
        [SerializeField] private Button sellButton;
        [SerializeField] private TMP_Text sellLabel;

        private int selectedId;
        private readonly StringBuilder sb = new StringBuilder(512);

        private void Awake()
        {
            for (int i = 0; i < statRows.Length; i++) statRows[i].Bind(i, OnTrain);
            emptyShopButton.onClick.AddListener(() => ui.CloseWindow()); // 상점은 마을에서 직접 방문
            drinkButton.onClick.AddListener(() => Game.UseItem(selectedId, ItemType.EnergyDrink));
            healButton.onClick.AddListener(() => Game.UseItem(selectedId, ItemType.Medicine));
            nutrientButton.onClick.AddListener(() => Game.UseItem(selectedId, ItemType.Nutrient));
            tackButton.onClick.AddListener(() => Game.UseGoldenTack(selectedId));
            sellButton.onClick.AddListener(OnSell);
        }

        public override void Refresh()
        {
            var s = Game.State;
            var b = Data.balance;
            rosterLabel.text = "마구간 " + s.horses.Count + "/" + b.maxStable;

            // 뉴스 (최근 10개)
            sb.Length = 0;
            for (int i = 0; i < s.logs.Count && i < 10; i++)
                sb.Append("<color=#566458>").Append(s.logs[i].day).Append("일</color>  ").Append(s.logs[i].text).Append('\n');
            newsText.text = sb.ToString();

            bool empty = s.horses.Count == 0;
            emptyRoot.SetActive(empty);
            detailRoot.SetActive(!empty);
            rosterList.SetCount(s.horses.Count);
            if (empty) return;

            if (Game.FindHorse(selectedId) == null) selectedId = s.horses[0].id;

            // 목록
            for (int i = 0; i < s.horses.Count; i++)
            {
                var h = s.horses[i];
                var row = rosterList.Get(i);
                row.Bind(h.id, OnSelect);
                row.SetSwatch(0, runner.Database.CoatColor(h.coat));
                row.SetText(0, h.name + "  <size=70%><color=#566458>" + (h.male ? "수" : "암") + "</color></size>");
                row.SetText(1, Chips(h));
                row.SetText(2, h.Total.ToString("0"));
                row.SetBackground(h.id == selectedId ? UITheme.Panel2 : UITheme.Panel);
            }

            var sel = Game.FindHorse(selectedId);
            RefreshDetail(sel);
        }

        private void RefreshDetail(HorseData h)
        {
            var b = Data.balance;
            nameText.text = h.name;
            string form = h.RecentForm(5);
            infoText.text = (h.male ? "수말" : "암말") + " · " + runner.Database.CoatName(h.coat) + " · 출전 " + h.races + "/" + b.retireAt
                + "전 · 우승 " + h.wins + (h.grandPrixWins > 0 ? " (G1 " + h.grandPrixWins + ")" : "") + " · 최근 " + (form.Length > 0 ? form : "기록 없음");
            chipsText.text = Chips(h);
            totalText.text = h.Total.ToString("0.0");

            conditionBar.Set(h.condition / 100f);
            conditionValue.text = Mathf.RoundToInt(h.condition).ToString();
            fatigueBar.Set(h.fatigue / 100f);
            fatigueBar.SetColor(h.fatigue >= 90f ? UITheme.Bad : h.fatigue >= 70f ? UITheme.Warn : UITheme.Turf);
            fatigueValue.text = Mathf.RoundToInt(h.fatigue).ToString();
            // 피로 30 초과분은 경주 속도 감소 → 수치로 안내
            if (h.fatigue > 30f) fatigueValue.text += "  <size=70%><color=#B06D12>속도 -" + ((1f - RaceSimulator.FatigueFactor(h.fatigue)) * 100f).ToString("0.0") + "%</color></size>";

            bool busy = h.IsTraining || h.retired || Game.IsEnteredAnywhere(h.id);
            for (int i = 0; i < statRows.Length && i < RanchConst.StatCount; i++)
            {
                var info = Data.stats[i];
                var row = statRows[i];
                float v = h.stats[i];
                row.SetText(0, info.displayName);
                row.SetText(1, info.role);
                row.SetText(2, v.ToString("0.0"));
                row.SetText(3, info.trainingName + "  <size=80%>" + Game.TrainingCost(h, i).ToString("N0") + "G</size>");
                row.SetBar(0, v / b.statCap);
                row.SetInteractable(!busy);
                row.SetBackground(h.IsTraining && h.trainStat == i ? UITheme.Panel2 : UITheme.Panel);
            }

            // 훈련 게이지
            if (h.IsTraining)
            {
                int doneDays = h.trainTotal - h.trainLeft;
                trainingTitle.text = Data.stats[h.trainStat].displayName + " 훈련 중  <size=75%><color=#566458>" + doneDays + "/" + h.trainTotal + "일" + (h.trainBoost > 1f ? " · 영양제 1.5배" : "") + "</color></size>";
                trainingDesc.text = "게이지가 찰 때까지 다른 훈련을 할 수 없습니다. [다음 날]로 진행하세요.";
                trainingBar.Set((doneDays + 0.15f) / Mathf.Max(1, h.trainTotal));
            }
            else if (h.retired)
            {
                trainingTitle.text = "은퇴마";
                trainingDesc.text = "출전과 훈련이 끝났습니다. 교배소에서 망아지를 얻거나 판매할 수 있습니다.";
                trainingBar.Set(0f);
            }
            else
            {
                trainingTitle.text = "훈련 대기";
                trainingDesc.text = Game.IsEnteredAnywhere(h.id) ? "경주에 등록되어 오늘은 훈련할 수 없습니다." : "스탯 옆 버튼으로 훈련을 시작하세요 (훈련비는 스탯이 높을수록 비쌈). " + b.trainDays + "일 걸리고 한 번에 하나만 진행됩니다.";
                trainingBar.Set(0f);
            }
            var items = Game.State.items;
            drinkButton.interactable = h.IsTraining && items[(int)ItemType.EnergyDrink] > 0;
            drinkLabel.text = "에너지 드링크 (" + items[(int)ItemType.EnergyDrink] + ")";

            healButton.interactable = h.injury > 0 && items[(int)ItemType.Medicine] > 0;
            healLabel.text = "치료약 (" + items[(int)ItemType.Medicine] + ")";
            nutrientButton.interactable = !h.nutrient && !h.retired && items[(int)ItemType.Nutrient] > 0;
            nutrientLabel.text = "영양제 (" + items[(int)ItemType.Nutrient] + ")";
            tackButton.gameObject.SetActive(Game.State.goldenTack > 0 || h.tack);
            tackButton.interactable = !h.tack && Game.State.goldenTack > 0;
            tackLabel.text = h.tack ? "황금 마구 장착됨" : "황금 마구 장착 (" + Game.State.goldenTack + ")";
            sellButton.interactable = !h.IsTraining && !Game.IsEnteredAnywhere(h.id);
            sellLabel.text = "판매 " + Gold(Game.SellPrice(h));
        }

        private static string Chips(HorseData h)
        {
            var b = new StringBuilder(64);
            if (h.retired) b.Append("<color=#16201A>[은퇴]</color> ");
            if (h.IsTraining) b.Append("<color=#2A6A3D>[훈련 ").Append(h.trainTotal - h.trainLeft).Append('/').Append(h.trainTotal).Append("]</color> ");
            if (h.injury > 0) b.Append("<color=#B3342A>[").Append(h.injury == 2 ? "중상" : "경상").Append("]</color> ");
            if (h.fatigue >= 70f) b.Append("<color=#B06D12>[피로]</color> ");
            if (h.nutrient) b.Append("<color=#566458>[영양제]</color> ");
            if (h.tack) b.Append("<color=#B8860B>[황금 마구]</color> ");
            return b.ToString();
        }

        private void OnSelect(int horseId)
        {
            selectedId = horseId;
            Refresh();
        }

        private void OnTrain(int stat)
        {
            var result = Game.StartTraining(selectedId, stat, false);
            if (result != TrainResult.NeedsConfirm) return;

            // 부상 상태 → 치료 후 훈련 / 강행 / 취소
            var h = Game.FindHorse(selectedId);
            var b = Data.balance;
            int pct = Mathf.RoundToInt((h.injury == 2 ? b.forcedTrainDeathMajor : b.forcedTrainDeathMinor) * 100f);
            int medicine = Game.State.items[(int)ItemType.Medicine];
            int id = h.id;
            ui.Modal.Show("부상 상태입니다",
                "「" + h.name + "」은(는) " + (h.injury == 2 ? "중상" : "경상") + " 상태입니다.\n치료약 없이 훈련을 강행하면 <color=#B3342A>" + pct + "%</color> 확률로 사망합니다.",
                new ModalAction("치료약 사용 후 훈련 (" + medicine + ")", () => { if (Game.UseItem(id, ItemType.Medicine)) Game.StartTraining(id, stat, false); }, ModalStyle.Primary, medicine > 0),
                new ModalAction("강행", () => Game.StartTraining(id, stat, true), ModalStyle.Danger),
                new ModalAction("취소", null));
        }

        private void OnSell()
        {
            var h = Game.FindHorse(selectedId);
            if (h == null) return;
            var b = Data.balance;
            int id = h.id;
            ui.Modal.Show(h.name + " 판매",
                "판매가 = " + b.sellBase.ToString("N0") + " + 스탯 합계 " + h.Total.ToString("0.0") + " × " + b.sellPerStat + " + 우승 " + h.wins + " × " + b.sellPerWin.ToString("N0")
                + "\n\n<size=130%><b>" + Gold(Game.SellPrice(h)) + "</b></size>",
                new ModalAction("판매 확정", () => Game.Sell(id), ModalStyle.Danger),
                new ModalAction("취소", null));
        }
    }
}
