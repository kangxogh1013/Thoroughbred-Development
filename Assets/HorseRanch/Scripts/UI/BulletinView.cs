using System.Text;
using TMPro;
using UnityEngine;

namespace HorseRanch
{
    /// <summary>게시판/전광판: 오늘의 경주 출마표 요약(인기 순), 어제 결과, 대상경주 안내</summary>
    public class BulletinView : RanchViewBase
    {
        [SerializeField] private TMP_Text todayText;
        [SerializeField] private TMP_Text resultText;

        public override void Refresh()
        {
            var g = Game;
            var sb = new StringBuilder(1024);
            int d = g.DaysToGrandPrix;
            sb.Append("<b>").Append(g.State.day).Append("일차 경주</b>  ").Append(d == 0 ? "<color=#B8860B>오늘은 대상경주(G1)!</color>" : "<color=#566458>다음 대상경주까지 " + d + "일</color>").Append("\n\n");
            for (int r = 0; r < Data.races.Length; r++)
            {
                var rs = g.Race(r);
                sb.Append("<b>").Append(Data.races[r].displayName).Append(" ").Append(Data.races[r].distance.ToString("N0")).Append("m</b>")
                  .Append(rs.done ? "  <color=#566458>(종료)</color>" : "").Append('\n');
                if (rs.done)
                {
                    for (int k = 0; k < 3 && k < rs.result.rows.Count; k++)
                    {
                        var row = rs.result.rows[k];
                        sb.Append("  ").Append(row.position).Append("착 <color=#").Append(UITheme.SilkHex[row.lane - 1]).Append(">■</color> ").Append(row.lane).Append("번 ").Append(row.name).Append('\n');
                    }
                }
                else
                {
                    var run = g.Runners(r);
                    for (int i = 0; i < run.Count; i++)
                    {
                        var h = run[i];
                        sb.Append("  <color=#").Append(UITheme.SilkHex[i]).Append(">■</color> ").Append(i + 1).Append("번 ").Append(h.name)
                          .Append(h.IsNpc ? "" : " <color=#B8860B>(내 말)</color>")
                          .Append("<pos=55%>단승 ").Append(g.Odds(r, BetType.Win, (i + 1).ToString(), 0f).ToString("0.0")).Append("배\n");
                    }
                }
                sb.Append('\n');
            }
            todayText.text = sb.ToString();

            sb.Length = 0;
            sb.Append("<b>어제 결과</b>\n");
            if (g.State.lastResults.Count == 0) sb.Append("<color=#566458>아직 기록이 없습니다.</color>\n");
            for (int i = 0; i < g.State.lastResults.Count; i++) sb.Append(g.State.lastResults[i]).Append('\n');
            sb.Append("\n<b>목장 소식</b>\n");
            for (int i = 0; i < g.State.logs.Count && i < 8; i++)
                sb.Append("<color=#566458>").Append(g.State.logs[i].day).Append("일</color> ").Append(g.State.logs[i].text).Append('\n');
            resultText.text = sb.ToString();
        }
    }
}
