using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>업적 탭</summary>
    public class AchievementView : RanchViewBase
    {
        [Header("업적 카드 (texts: 0 상태, 1 이름, 2 조건, 3 보상)")]
        [SerializeField] private UIRow[] cards;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private Button resetButton;

        private void Awake()
        {
            resetButton.onClick.AddListener(() =>
                ui.Modal.Show("새 게임", "현재 목장의 모든 기록이 사라집니다.",
                    new ModalAction("새로 시작", ui.RestartGame, ModalStyle.Danger),
                    new ModalAction("취소", null)));
        }

        public override void Refresh()
        {
            var s = Game.State;
            for (int i = 0; i < cards.Length; i++)
            {
                bool has = i < Data.achievements.Length;
                cards[i].gameObject.SetActive(has);
                if (!has) continue;
                var a = Data.achievements[i];
                int day = s.achievementDays[(int)a.id];
                string progress = "";
                if (a.id == AchievementId.HorseHunter) progress = " · " + s.deadCount + "/10";
                else if (a.id == AchievementId.HorseCollector) progress = " · " + Game.SpecialistCount + "/6 " + SpecialistList();
                else if (a.id == AchievementId.GrandSlam) progress = " · " + (s.grandPrixWon[0] ? "●" : "○") + (s.grandPrixWon[1] ? "●" : "○");

                var c = cards[i];
                c.SetText(0, (day > 0 ? "<color=#B8860B>" + day + "일차 달성</color>" : "미달성") + progress);
                c.SetText(1, a.displayName);
                c.SetText(2, a.description);
                c.SetText(3, "보상: " + a.rewardText);
                c.SetBackground(day > 0 ? UITheme.Panel2 : UITheme.Panel);
                c.SetSwatch(0, day > 0 ? UITheme.Accent : UITheme.Line);
            }
            summaryText.text = "누적 사망 " + s.deadCount + "두 · 칭호 " + s.titles.Count + "개 · 플레이 " + s.day + "일차";
        }

        private string SpecialistList()
        {
            var s = Game.State.specialists;
            string r = "";
            for (int i = 0; i < s.Length; i++) r += s[i] ? "<color=#2A6A3D>" + Data.stats[i].shortName + "</color>" : "<color=#C6CFBF>" + Data.stats[i].shortName + "</color>";
            return r;
        }
    }
}
