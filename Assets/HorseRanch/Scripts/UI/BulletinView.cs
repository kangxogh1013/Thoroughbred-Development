using System.Text;
using TMPro;
using UnityEngine;

namespace HorseRanch
{
    /// <summary>
    /// 마을 게시판: 어제 우승마, 내일 출주마(가장 높은 스탯 하나만 공개), 마을 소식.
    /// 오늘 배당은 도박장 전광판에서 본다.
    /// </summary>
    public class BulletinView : RanchViewBase
    {
        [SerializeField] private TMP_Text todayText;
        [SerializeField] private TMP_Text resultText;

        private static readonly string[] Rumors =
        {
            "동쪽 공원 끝 숲속에 도박 고수가 산다는 소문. 나무 사이로 길이 있다나?",
            "광장 분수에 동전을 던지면 그날은 마권 운이 좋아진다고 한다.",
            "마시장 상인 왈: 한 스탯만 10을 넘는 특화마를 모으는 수집가가 있다.",
            "트레이너 김코치: 근성 훈련은 막판 스퍼트에서 진가를 발휘한다.",
            "7일마다 열리는 대상경주(G1)는 상금이 세 배!",
            "피로가 쌓인 말은 경주에서 느려진다. 마구간에서 쉬게 하자.",
        };

        public override void Refresh()
        {
            var g = Game;
            var s = g.State;
            var sb = new StringBuilder(1024);

            sb.Append("<size=115%><b>어제의 우승마</b></size>\n");
            if (s.lastResults.Count == 0) sb.Append("<color=#566458>아직 기록이 없습니다.</color>\n");
            for (int i = 0; i < s.lastResults.Count; i++) sb.Append(s.lastResults[i]).Append('\n');

            int d = g.DaysToGrandPrix;
            sb.Append("\n<size=115%><b>내일(").Append(s.day + 1).Append("일차) 출주마 예고</b></size>  <color=#566458>가장 높은 스탯만 공개</color>\n");
            for (int r = 0; r < s.tomorrow.Count && r < Data.races.Length; r++)
            {
                var rs = s.tomorrow[r];
                sb.Append("<b>").Append(Data.races[r].displayName).Append(' ').Append(Data.races[r].distance.ToString("N0")).Append("m</b>")
                  .Append(rs.grandPrix ? "  <color=#B8860B>G1 대상경주</color>" : "").Append('\n');
                for (int i = 0; i < rs.field.Count; i++)
                {
                    var h = rs.field[i];
                    int best = 0;
                    for (int k = 1; k < RanchConst.StatCount; k++) if (h.stats[k] > h.stats[best]) best = k;
                    sb.Append("  <color=#").Append(UITheme.SilkHex[i]).Append(">■</color> ").Append(i + 1).Append("번 ").Append(h.name)
                      .Append("<pos=52%>").Append(Data.stats[best].displayName).Append(" <b>").Append(Mathf.RoundToInt(h.stats[best])).Append("</b>\n");
                }
            }
            todayText.text = sb.ToString();

            sb.Length = 0;
            sb.Append("<size=115%><b>마을 소식</b></size>\n");
            sb.Append(d == 0 ? "<color=#B8860B>· 오늘은 대상경주(G1) 개최일!</color>\n" : "· 다음 대상경주까지 " + d + "일\n");
            if (g.IsLucky) sb.Append("<color=#2A6A3D>· 분수의 행운이 함께하는 날</color>\n");
            sb.Append("· 소문: ").Append(Rumors[s.day % Rumors.Length]).Append("\n\n");
            sb.Append("<size=115%><b>목장 일지</b></size>\n");
            for (int i = 0; i < s.logs.Count && i < 9; i++)
                sb.Append("<color=#566458>").Append(s.logs[i].day).Append("일</color> ").Append(s.logs[i].text).Append('\n');
            resultText.text = sb.ToString();
        }
    }
}
