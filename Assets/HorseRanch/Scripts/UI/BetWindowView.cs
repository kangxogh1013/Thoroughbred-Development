using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>도박장 마권 판매소: 출마표(배당 전광판) + 승식/마번/금액 선택 → 마권은 인벤토리에 보관</summary>
    public class BetWindowView : RanchViewBase
    {
        [Header("경주 선택 (texts: 0 이름, 1 정보, 2 상태)")]
        [SerializeField] private UIRow[] raceButtons;

        [Header("출마표 8행 (texts: 0 마번, 1 마명, 2 성, 3 최근, 4 합계, 5 스탯, 6 컨디션, 7 단승, 8 연승)")]
        [SerializeField] private UIRow[] boardRows;
        [SerializeField] private TMP_Text boardFooter;

        [Header("마권")]
        [SerializeField] private UIRow[] betTypeButtons;
        [SerializeField] private TMP_Text betDescText;
        [Tooltip("마번 버튼 8개 (texts: 0 번호, 1 선택 순서 / swatches: 0 선택 표시)")]
        [SerializeField] private UIRow[] laneButtons;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Button add100Button;
        [SerializeField] private Button add1000Button;
        [SerializeField] private Button add10000Button;
        [SerializeField] private Button resetAmountButton;
        [SerializeField] private TMP_Text estLabel;
        [SerializeField] private TMP_Text estOdds;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyLabel;
        [Tooltip("오늘 이 경주에 산 마권 (texts: 0 내용, 1 금액×배당 / button: 환불)")]
        [SerializeField] private UIList ticketList;

        private int raceIndex;
        private BetType betType = BetType.Win;
        private readonly List<int> picks = new List<int>(3);
        private readonly List<BetTicket> todays = new List<BetTicket>();
        private int amount = 1000;
        private readonly StringBuilder sb = new StringBuilder(128);

        private void Awake()
        {
            for (int i = 0; i < raceButtons.Length; i++) raceButtons[i].Bind(i, OnSelectRace);
            for (int i = 0; i < betTypeButtons.Length; i++) betTypeButtons[i].Bind(i, OnSelectBetType);
            for (int i = 0; i < laneButtons.Length; i++) laneButtons[i].Bind(i + 1, OnPick);
            add100Button.onClick.AddListener(() => AddAmount(100));
            add1000Button.onClick.AddListener(() => AddAmount(1000));
            add10000Button.onClick.AddListener(() => AddAmount(10000));
            resetAmountButton.onClick.AddListener(() => { amount = 100; Refresh(); });
            buyButton.onClick.AddListener(OnBuy);
        }

        public override void Refresh()
        {
            var g = Game;
            if (raceIndex >= g.State.races.Count) raceIndex = 0;
            var rs = g.Race(raceIndex);

            for (int i = 0; i < raceButtons.Length && i < Data.races.Length; i++)
            {
                var def = Data.races[i];
                var r = g.Race(i);
                var row = raceButtons[i];
                row.SetText(0, def.displayName + (r.grandPrix ? "  <color=#B8860B>G1 대상경주</color>" : ""));
                row.SetText(1, def.distance.ToString("N0") + "m · 1착 상금 " + Gold(g.Prize(i, 1)) + " · 핵심 " + def.keyStats);
                row.SetText(2, r.done ? "발매 마감 (경주 종료)" : "발매 중" + (r.entryId > 0 ? " · 내 말 출전" : ""));
                row.SetTextColor(2, r.done ? UITheme.Mute : UITheme.Good);
                row.SetBackground(i == raceIndex ? UITheme.Panel2 : UITheme.Panel);
            }

            RefreshBoard(rs);
            RefreshBet(rs);
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
            boardFooter.text = rs.done
                ? "<color=#F2BF37>이 경주는 끝났습니다. 결과는 게시판에서, 적중 마권은 교환소에서.</color>"
                : "배당은 배팅 풀 기준 실시간 추정치 (공제율 " + Mathf.RoundToInt(Data.balance.takeRate * 100f) + "%). 핵심 스탯: " + Data.races[raceIndex].keyStats;
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
                row.SetSwatch(0, ix >= 0 ? UITheme.Accent : Color.clear);
                row.SetInteractable(!rs.done);
            }

            amountText.text = Gold(amount);
            bool ready = picks.Count == need && !rs.done;
            if (ready)
            {
                string key = ParimutuelMarket.Key(betType, picks);
                estLabel.text = ParimutuelMarket.DisplayName(betType) + " " + key;
                estOdds.text = g.Odds(raceIndex, betType, key, amount).ToString("0.0") + "배";
            }
            else
            {
                estLabel.text = rs.done ? "발매 마감" : "마번을 선택하세요";
                estOdds.text = "-";
            }
            buyButton.interactable = ready && amount <= g.State.gold;
            buyLabel.text = rs.done ? "발매 마감" : "마권 구매 (인벤토리 보관)";

            todays.Clear();
            for (int i = 0; i < g.State.tickets.Count; i++)
            {
                var t = g.State.tickets[i];
                if (t.day == g.State.day && t.raceIndex == raceIndex) todays.Add(t);
            }
            ticketList.SetCount(todays.Count);
            for (int i = 0; i < todays.Count; i++)
            {
                var t = todays[i];
                var row = ticketList.Get(i);
                row.Bind(t.id, OnCancelTicket);
                row.SetText(0, "<b>" + ParimutuelMarket.DisplayName(t.type) + "</b>  " + t.key);
                row.SetText(1, t.status == TicketStatus.Pending ? Gold(t.amount) + " × " + g.Odds(raceIndex, t.type, t.key, 0f).ToString("0.0") : GameUI.TicketStatusLabel(t));
                row.SetText(2, "환불");
                row.SetInteractable(t.status == TicketStatus.Pending && !rs.done);
            }
        }

        private void OnSelectRace(int i) { raceIndex = i; picks.Clear(); Refresh(); }
        private void OnSelectBetType(int i) { betType = (BetType)i; picks.Clear(); Refresh(); }

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

        private void OnBuy()
        {
            if (Game.PlaceBet(raceIndex, betType, picks, amount)) picks.Clear();
            Refresh();
        }

        private void OnCancelTicket(int ticketId) { Game.CancelTicket(ticketId); }
    }
}
