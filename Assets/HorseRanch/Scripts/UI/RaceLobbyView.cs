using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>경마장 탭: 대회 선택, 출전 등록, 출마표(배당 전광판), 마권 구매, 발주</summary>
    public class RaceLobbyView : RanchViewBase
    {
        [Header("대회 선택 (texts: 0 이름, 1 정보, 2 상태)")]
        [SerializeField] private UIRow[] raceButtons;

        [Header("상태별 루트")]
        [SerializeField] private GameObject lobbyRoot;
        [SerializeField] private GameObject resultRoot;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button replayButton;

        [Header("출전 등록")]
        [SerializeField] private Button entryPrevButton;
        [SerializeField] private Button entryNextButton;
        [SerializeField] private Button entryToggleButton;
        [SerializeField] private TMP_Text entryNameText;
        [SerializeField] private TMP_Text entryToggleLabel;
        [SerializeField] private TMP_Text entryHintText;

        [Header("출마표 8행 (texts: 0 마번, 1 마명, 2 성, 3 최근, 4 합계, 5 스탯, 6 컨디션, 7 단승, 8 연승)")]
        [SerializeField] private UIRow[] boardRows;
        [SerializeField] private TMP_Text boardFooter;

        [Header("마권")]
        [SerializeField] private UIRow[] betTypeButtons;
        [SerializeField] private TMP_Text betDescText;
        [Tooltip("마번 버튼 8개 (texts: 0 번호, 1 선택 순서)")]
        [SerializeField] private UIRow[] laneButtons;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Button add100Button;
        [SerializeField] private Button add1000Button;
        [SerializeField] private Button add10000Button;
        [SerializeField] private Button resetAmountButton;
        [SerializeField] private TMP_Text estLabel;
        [SerializeField] private TMP_Text estOdds;
        [SerializeField] private Button buyButton;
        [SerializeField] private UIList ticketList;
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text startLabel;

        private int raceIndex;
        private BetType betType = BetType.Win;
        private readonly List<int> picks = new List<int>(3);
        private int amount = 1000;
        private int entryCursor;
        private readonly List<HorseData> candidates = new List<HorseData>();
        private readonly StringBuilder sb = new StringBuilder(128);

        private void Awake()
        {
            for (int i = 0; i < raceButtons.Length; i++) raceButtons[i].Bind(i, OnSelectRace);
            for (int i = 0; i < betTypeButtons.Length; i++) betTypeButtons[i].Bind(i, OnSelectBetType);
            for (int i = 0; i < laneButtons.Length; i++) laneButtons[i].Bind(i + 1, OnPick);
            entryPrevButton.onClick.AddListener(() => MoveCursor(-1));
            entryNextButton.onClick.AddListener(() => MoveCursor(1));
            entryToggleButton.onClick.AddListener(OnToggleEntry);
            add100Button.onClick.AddListener(() => AddAmount(100));
            add1000Button.onClick.AddListener(() => AddAmount(1000));
            add10000Button.onClick.AddListener(() => AddAmount(10000));
            resetAmountButton.onClick.AddListener(() => { amount = 100; Refresh(); });
            buyButton.onClick.AddListener(OnBuy);
            startButton.onClick.AddListener(OnStart);
            replayButton.onClick.AddListener(() => ui.PlayReplay(raceIndex));
        }

        public override void Refresh()
        {
            var g = Game;
            if (raceIndex >= g.State.races.Count) raceIndex = 0;
            var rs = g.Race(raceIndex);

            // 대회 버튼
            for (int i = 0; i < raceButtons.Length && i < Data.races.Length; i++)
            {
                var def = Data.races[i];
                var r = g.Race(i);
                var row = raceButtons[i];
                row.SetText(0, def.displayName + (r.grandPrix ? "  <color=#B8860B>G1 대상경주</color>" : ""));
                row.SetText(1, def.distance.ToString("N0") + "m · 참가비 " + Gold(g.EntryFee(i)) + " · 1착 " + Gold(g.Prize(i, 1)) + " · 핵심 " + def.keyStats);
                row.SetText(2, r.done ? "오늘 경주 종료" : "오늘 " + g.State.day + "일차 발주 대기" + (r.entryId > 0 ? " · 내 말 출전" : ""));
                row.SetTextColor(2, r.done ? UITheme.Mute : UITheme.Good);
                row.SetBackground(i == raceIndex ? UITheme.Panel2 : UITheme.Panel);
            }

            lobbyRoot.SetActive(!rs.done);
            resultRoot.SetActive(rs.done);
            if (rs.done)
            {
                resultText.text = RanchUI.ResultBody(rs.result);
                replayButton.interactable = ui.HasReplay(raceIndex);
                return;
            }

            RefreshEntry(rs);
            RefreshBoard(rs);
            RefreshBet(rs);
        }

        private void RefreshEntry(RaceDayState rs)
        {
            var g = Game;
            candidates.Clear();
            string reason;
            for (int i = 0; i < g.State.horses.Count; i++)
                if (g.CanEnter(g.State.horses[i], raceIndex, out reason)) candidates.Add(g.State.horses[i]);

            bool locked = rs.slip.Count > 0;
            var entered = g.FindHorse(rs.entryId);
            if (entered != null)
            {
                entryNameText.text = "<color=#2A6A3D>출전</color>  " + entered.name + "  <size=75%>합계 " + entered.Total.ToString("0") + (entered.injury > 0 ? " · <color=#B3342A>부상!</color>" : "") + "</size>";
                entryToggleLabel.text = "등록 취소";
                entryToggleButton.interactable = !locked;
                entryPrevButton.interactable = entryNextButton.interactable = false;
            }
            else if (candidates.Count > 0)
            {
                entryCursor = (entryCursor % candidates.Count + candidates.Count) % candidates.Count;
                var c = candidates[entryCursor];
                entryNameText.text = c.name + "  <size=75%>합계 " + c.Total.ToString("0") + (c.injury > 0 ? " · <color=#B3342A>부상!</color>" : "") + "  (" + (entryCursor + 1) + "/" + candidates.Count + ")</size>";
                entryToggleLabel.text = "출전 등록 · " + Gold(g.EntryFee(raceIndex));
                entryToggleButton.interactable = !locked;
                entryPrevButton.interactable = entryNextButton.interactable = candidates.Count > 1;
            }
            else
            {
                entryNameText.text = "<color=#566458>출전 가능한 말이 없습니다 (관전·배팅만 가능)</color>";
                entryToggleLabel.text = "출전 등록";
                entryToggleButton.interactable = false;
                entryPrevButton.interactable = entryNextButton.interactable = false;
            }
            var shown = entered ?? (candidates.Count > 0 ? candidates[entryCursor] : null);
            string fatigueNote = "";
            if (shown != null && shown.fatigue > 30f)
                fatigueNote = "<color=#B06D12>피로 " + Mathf.RoundToInt(shown.fatigue) + " → 최고 속도 -" + ((1f - RaceSimulator.FatigueFactor(shown.fatigue)) * 100f).ToString("0.0") + "%. 쉬게 하면 하루 피로 -" + Data.balance.restRecovery + "</color>  ";
            entryHintText.text = fatigueNote + (locked ? "마권 구매 후에는 출전마를 바꿀 수 없습니다." : "훈련 중·은퇴마·오늘 이미 달린 말은 출전할 수 없습니다. 말 1두는 하루 1경주.");
        }

        private void RefreshBoard(RaceDayState rs)
        {
            var g = Game;
            var run = g.Runners(raceIndex);
            for (int i = 0; i < boardRows.Length && i < run.Count; i++)
            {
                var h = run[i];
                var row = boardRows[i];
                bool mine = !h.IsNpc;
                row.SetSwatch(0, UITheme.Silk[i]);
                row.SetText(0, (i + 1).ToString());
                row.SetTextColor(0, UITheme.SilkInk[i]);
                row.SetText(1, h.name + (mine ? "  <color=#F2BF37><size=75%>내 말</size></color>" : ""));
                row.SetText(2, h.male ? "수" : "암");
                string form = h.RecentForm(3);
                row.SetText(3, form.Length > 0 ? form : "신마");
                row.SetText(4, h.Total.ToString("0"));
                sb.Length = 0;
                for (int s = 0; s < RanchConst.StatCount; s++) sb.Append(Data.stats[s].shortName).Append(Mathf.RoundToInt(h.stats[s])).Append(' ');
                row.SetText(5, sb.ToString());
                row.SetText(6, Mathf.RoundToInt(h.condition).ToString());
                row.SetText(7, g.Odds(raceIndex, BetType.Win, (i + 1).ToString(), 0f).ToString("0.0"));
                row.SetText(8, g.Odds(raceIndex, BetType.Place, (i + 1).ToString(), 0f).ToString("0.0"));
                row.SetBackground(mine ? UITheme.BoardMine : (i % 2 == 0 ? UITheme.Board : UITheme.BoardRow));
            }
            boardFooter.text = "배당은 배팅 풀 기준 실시간 추정치이며 공제율 " + Mathf.RoundToInt(Data.balance.takeRate * 100f) + "%가 적용됩니다. 핵심 스탯: " + Data.races[raceIndex].keyStats;
        }

        private void RefreshBet(RaceDayState rs)
        {
            var g = Game;
            for (int i = 0; i < betTypeButtons.Length; i++)
            {
                betTypeButtons[i].SetText(0, ParimutuelMarket.DisplayName((BetType)i));
                betTypeButtons[i].SetBackground(i == (int)betType ? UITheme.Ink : UITheme.Panel2);
                betTypeButtons[i].SetTextColor(0, i == (int)betType ? UITheme.White : UITheme.Ink);
            }
            int need = ParimutuelMarket.PickCount(betType);
            betDescText.text = ParimutuelMarket.Description(betType) + " · 마번 " + need + "개" + (ParimutuelMarket.IsOrdered(betType) ? " 순서대로" : "") + " 선택";

            for (int i = 0; i < laneButtons.Length; i++)
            {
                int ix = picks.IndexOf(i + 1);
                var row = laneButtons[i];
                row.SetText(0, (i + 1).ToString());
                row.SetTextColor(0, UITheme.SilkInk[i]);
                row.SetBackground(UITheme.Silk[i]);
                row.SetText(1, ix >= 0 ? (need > 1 ? (ix + 1).ToString() : "●") : "");
                row.SetSwatch(0, ix >= 0 ? UITheme.Accent : Color.clear); // 선택 표시 바
            }

            amountText.text = Gold(amount);
            bool ready = picks.Count == need;
            if (ready)
            {
                string key = ParimutuelMarket.Key(betType, picks);
                estLabel.text = ParimutuelMarket.DisplayName(betType) + " " + key;
                estOdds.text = g.Odds(raceIndex, betType, key, amount).ToString("0.0") + "배";
            }
            else
            {
                estLabel.text = "마번을 선택하세요";
                estOdds.text = "-";
            }
            buyButton.interactable = ready && amount <= g.State.gold;

            ticketList.SetCount(rs.slip.Count);
            for (int i = 0; i < rs.slip.Count; i++)
            {
                var t = rs.slip[i];
                var row = ticketList.Get(i);
                row.Bind(i, OnCancelTicket);
                row.SetText(0, "<b>" + ParimutuelMarket.DisplayName(t.type) + "</b>  " + t.key);
                row.SetText(1, Gold(t.amount) + " × " + g.Odds(raceIndex, t.type, t.key, 0f).ToString("0.0"));
            }
            startLabel.text = rs.grandPrix ? "발주! 대상경주 시작" : "발주! 경주 시작";
        }

        // ===== 입력 =====
        private void OnSelectRace(int i)
        {
            raceIndex = i;
            picks.Clear();
            Refresh();
        }

        private void OnSelectBetType(int i)
        {
            betType = (BetType)i;
            picks.Clear();
            Refresh();
        }

        private void OnPick(int lane)
        {
            int ix = picks.IndexOf(lane);
            if (ix >= 0) picks.RemoveAt(ix);
            else
            {
                if (picks.Count >= ParimutuelMarket.PickCount(betType)) picks.RemoveAt(0);
                picks.Add(lane);
            }
            Refresh();
        }

        private void AddAmount(int v)
        {
            amount = Mathf.Min(Data.balance.maxBetPerTicket, amount + v);
            Refresh();
        }

        private void MoveCursor(int d)
        {
            entryCursor += d;
            Refresh();
        }

        private void OnToggleEntry()
        {
            var rs = Game.Race(raceIndex);
            if (rs.entryId > 0) Game.SetEntry(raceIndex, 0);
            else if (candidates.Count > 0) Game.SetEntry(raceIndex, candidates[entryCursor % candidates.Count].id);
        }

        private void OnBuy()
        {
            if (Game.PlaceBet(raceIndex, betType, picks, amount)) picks.Clear();
            Refresh();
        }

        private void OnCancelTicket(int i) { Game.CancelBet(raceIndex, i); }

        private void OnStart()
        {
            int injury = Game.EntryInjury(raceIndex);
            if (injury == 0) { Game.RunRace(raceIndex); return; }

            var h = Game.FindHorse(Game.Race(raceIndex).entryId);
            var b = Data.balance;
            int pct = Mathf.RoundToInt((injury == 2 ? b.forcedRaceDeathMajor : b.forcedRaceDeathMinor) * 100f);
            int medicine = Game.State.items[(int)ItemType.Medicine];
            int race = raceIndex, id = h.id;
            ui.Modal.Show("부상마 출전",
                "「" + h.name + "」은(는) " + (injury == 2 ? "중상" : "경상") + " 상태입니다.\n그대로 달리면 <color=#B3342A>" + pct + "%</color> 확률로 경주 중 쓰러져 죽습니다.",
                new ModalAction("치료약 사용 후 출전 (" + medicine + ")", () => { if (Game.UseItem(id, ItemType.Medicine)) Game.RunRace(race); }, ModalStyle.Primary, medicine > 0),
                new ModalAction("강행", () => Game.RunRace(race), ModalStyle.Danger),
                new ModalAction("취소", null));
        }
    }
}
