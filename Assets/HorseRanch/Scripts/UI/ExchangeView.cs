using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>도박장 교환소: 인벤토리 마권 목록, 적중 마권 교환, 꽝 마권 버리기</summary>
    public class ExchangeView : RanchViewBase
    {
        [SerializeField] private TMP_Text summaryText;
        [Tooltip("texts: 0 마권, 1 상태")]
        [SerializeField] private UIList ticketList;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private Button redeemButton;
        [SerializeField] private TMP_Text redeemLabel;
        [SerializeField] private Button discardButton;

        private readonly List<BetTicket> sorted = new List<BetTicket>();

        private void Awake()
        {
            redeemButton.onClick.AddListener(() => Game.RedeemWinningTickets());
            discardButton.onClick.AddListener(() => Game.DiscardLostTickets());
        }

        public override void Refresh()
        {
            var g = Game;
            sorted.Clear();
            sorted.AddRange(g.State.tickets);
            // 적중 → 대기 → 꽝 순
            sorted.Sort((a, b) => Rank(a).CompareTo(Rank(b)) != 0 ? Rank(a).CompareTo(Rank(b)) : b.id.CompareTo(a.id));
            ticketList.SetCount(sorted.Count);
            for (int i = 0; i < sorted.Count; i++)
            {
                var row = ticketList.Get(i);
                row.SetText(0, GameUI.TicketLabel(sorted[i], Data));
                row.SetText(1, GameUI.TicketStatusLabel(sorted[i]));
                row.SetBackground(sorted[i].status == TicketStatus.Won ? UITheme.Panel2 : UITheme.Panel);
            }
            emptyText.gameObject.SetActive(sorted.Count == 0);
            int won = g.CountTickets(TicketStatus.Won), lost = g.CountTickets(TicketStatus.Lost), pending = g.CountTickets(TicketStatus.Pending);
            summaryText.text = "적중 " + won + "장 · 대기 " + pending + "장 · 꽝 " + lost + "장   <color=#566458>(보관 최대 " + Data.balance.maxTickets + "장)</color>";
            redeemButton.interactable = won > 0;
            redeemLabel.text = won > 0 ? "적중 마권 교환 +" + Gold(g.PendingPayout) : "교환할 마권 없음";
            discardButton.interactable = lost > 0;
        }

        private static int Rank(BetTicket t) { return t.status == TicketStatus.Won ? 0 : t.status == TicketStatus.Pending ? 1 : 2; }
    }
}
