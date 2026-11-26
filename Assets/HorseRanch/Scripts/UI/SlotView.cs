using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>
    /// 도박장 슬롯머신: 판돈 고르기 → 돌리기 → 릴 3개가 차례로 멈춤 → 배당.
    /// 결과는 RanchGame.SpinSlot이 먼저 정하고, 화면은 그 결과에 맞춰 멈추는 연출만 한다.
    /// </summary>
    public class SlotView : RanchViewBase
    {
        [SerializeField] private TMP_Text[] reelTexts;
        [SerializeField] private Image[] reelBacks;
        [SerializeField] private Button[] betButtons;
        [SerializeField] private TMP_Text[] betLabels;
        [SerializeField] private Button spinButton;
        [SerializeField] private TMP_Text spinLabel;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text payTableText;
        [SerializeField] private AudioManager audioManager;

        [Header("연출")]
        [Tooltip("그림이 바뀌는 간격 (초)")]
        [SerializeField] [Range(0.02f, 0.2f)] private float flickerTime = 0.06f;
        [Tooltip("릴이 멈추는 시각 (초): 첫째, 둘째, 셋째")]
        [SerializeField] private float[] stopTimes = { 0.7f, 1.05f, 1.45f };

        // 그림별 글자 색 (체리 빨강, 말굽 회색, 종 금색, BAR 검정, 말 갈색, 7 진홍)
        private static readonly string[] SymbolColor = { "#C8283C", "#6E7378", "#C89B1E", "#2B2B30", "#8A5A2E", "#D21E1E" };

        private int betIndex;
        private bool spinning;
        private readonly int[] reels = new int[3];

        private void Awake()
        {
            for (int i = 0; i < betButtons.Length; i++)
            {
                int k = i;
                betButtons[i].onClick.AddListener(() => { if (!spinning) { betIndex = k; Refresh(); } });
            }
            spinButton.onClick.AddListener(Spin);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            spinning = false;
            StopAllCoroutines();
        }

        public override void Refresh()
        {
            var b = Data.balance;
            betIndex = Mathf.Clamp(betIndex, 0, b.slotBets.Length - 1);
            for (int i = 0; i < betButtons.Length; i++)
            {
                bool on = i < b.slotBets.Length;
                betButtons[i].gameObject.SetActive(on);
                if (!on) continue;
                betLabels[i].text = (i == betIndex ? "> " : "") + Gold(b.slotBets[i]);
                betButtons[i].interactable = !spinning;
            }
            int bet = b.slotBets[betIndex];
            spinButton.interactable = !spinning && Game.State.gold >= bet;
            spinLabel.text = spinning ? "돌아가는 중…" : Game.State.gold >= bet ? "돌리기 (" + Gold(bet) + ")" : "판돈이 부족해요";
            if (!spinning && string.IsNullOrEmpty(reelTexts[0].text)) for (int i = 0; i < 3; i++) SetReel(i, 5 - i);

            var sb = new System.Text.StringBuilder();
            sb.Append("<b>배당표</b> (판돈의 몇 배)\n");
            for (int s = RanchGame.SlotSymbols.Length - 1; s >= 0; s--)
                sb.Append(Sym(s)).Append(' ').Append(Sym(s)).Append(' ').Append(Sym(s)).Append("<pos=62%>").Append(b.slotTriple[s]).Append("배\n");
            sb.Append(Sym(0)).Append(" 2개<pos=62%>").Append(b.slotCherryPair.ToString("0.#")).Append("배\n");
            sb.Append("<size=85%><color=#566458>오늘 돌린 횟수와 상관없이 매번 같은 확률이에요. 지금까지 ").Append(Game.State.slotSpins).Append("번, 최고 ").Append(Gold(Game.State.slotBestWin)).Append("</color></size>");
            payTableText.text = sb.ToString();
        }

        private static string Sym(int s) { return "<color=" + SymbolColor[s] + "><b>" + RanchGame.SlotSymbols[s] + "</b></color>"; }

        private void SetReel(int i, int s)
        {
            reelTexts[i].text = RanchGame.SlotSymbols[s];
            reelTexts[i].color = ColorFrom(SymbolColor[s]);
        }

        private static Color ColorFrom(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.black;
        }

        private void Spin()
        {
            if (spinning) return;
            int bet = Data.balance.slotBets[betIndex];
            int win = Game.SpinSlot(bet, reels);   // 결과 먼저 결정 (Changed → Refresh)
            if (win < 0) return;
            spinning = true;
            Refresh();
            resultText.text = "";
            StartCoroutine(SpinRoutine(bet, win));
        }

        private IEnumerator SpinRoutine(int bet, int win)
        {
            float t = 0f, flick = 0f;
            bool[] stopped = new bool[3];
            for (int i = 0; i < 3; i++) reelBacks[i].color = Color.white;
            while (!stopped[2])
            {
                t += Time.deltaTime; flick += Time.deltaTime;
                for (int i = 0; i < 3; i++)
                {
                    if (stopped[i]) continue;
                    if (t >= stopTimes[i]) { stopped[i] = true; SetReel(i, reels[i]); if (audioManager != null) audioManager.PlaySfx(SfxId.Click); continue; }
                    if (flick >= flickerTime) SetReel(i, Random.Range(0, RanchGame.SlotSymbols.Length));
                }
                if (flick >= flickerTime) flick = 0f;
                yield return null;
            }
            spinning = false;
            if (win > 0)
            {
                bool big = win >= bet * 20;
                if (audioManager != null) { audioManager.PlaySfx(SfxId.Coin); if (big) audioManager.PlaySfx(SfxId.Fanfare); }
                resultText.text = big ? "<color=#B8860B><b>대박! " + Gold(win) + "</b></color>" : "<color=#2A6A3D><b>당첨! " + Gold(win) + "</b></color>";
                var hit = new Color(1f, 0.92f, 0.6f);
                for (int i = 0; i < 3; i++) reelBacks[i].color = hit;
            }
            else resultText.text = "<color=#8B3A3A>꽝… 다시 한번?</color>";
            Refresh();
        }
    }
}
