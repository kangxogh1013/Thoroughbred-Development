using System.Text;
using TMPro;
using UnityEngine;

namespace HorseRanch
{
    /// <summary>인벤토리 (Tab): 골드, 소모품, 마권, 보유 말 요약</summary>
    public class InventoryView : RanchViewBase
    {
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text itemsText;
        [Tooltip("texts: 0 마권, 1 상태")]
        [SerializeField] private UIList ticketList;
        [SerializeField] private TMP_Text emptyText;

        public override void Refresh()
        {
            var g = Game;
            var s = g.State;
            summaryText.text = "<size=130%><b>" + s.gold.ToString("N0") + " G</b></size>    말 " + s.horses.Count + "/" + Data.balance.maxStable + "두 · " + s.day + "일차"
                + (s.titles.Count > 0 ? "\n칭호: " + string.Join(", ", s.titles.ToArray()) : "");

            var sb = new StringBuilder(128);
            for (int i = 0; i < Data.items.Length; i++)
            {
                var it = Data.items[i];
                sb.Append("<b>").Append(it.displayName).Append("</b> × ").Append(s.items[(int)it.type]).Append("    ");
            }
            if (s.goldenTack > 0) sb.Append("<color=#B8860B><b>황금 마구</b> × ").Append(s.goldenTack).Append("</color>");
            sb.Append("\n<b>쓰레기</b> × ").Append(s.trash).Append("/").Append(Data.balance.trashCarryMax)
              .Append("    숨겨진 것 ").Append(g.SecretsFound).Append("/").Append(RanchConst.SecretCount);
            if (g.KnowsBinSecret) sb.Append("    <color=#B8860B>넝마주이 수첩</color>");
            sb.Append("\n<size=85%><color=#566458>소모품은 마구간·훈련장에서 말에게 사용합니다. 쓰레기는 쓰레기통에만 버릴 수 있어요.</color></size>");
            itemsText.text = sb.ToString();

            ticketList.SetCount(s.tickets.Count);
            for (int i = 0; i < s.tickets.Count; i++)
            {
                var t = s.tickets[s.tickets.Count - 1 - i]; // 최신 순
                var row = ticketList.Get(i);
                row.SetText(0, GameUI.TicketLabel(t, Data));
                row.SetText(1, GameUI.TicketStatusLabel(t));
                row.SetBackground(t.status == TicketStatus.Won ? UITheme.Panel2 : UITheme.Panel);
            }
            emptyText.gameObject.SetActive(s.tickets.Count == 0);
        }
    }
}
