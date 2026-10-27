using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>경기장 출전 접수: 경주 선택 + 출전마 선택(◀ ▶) + 등록/취소. 내 말 없이도 경주는 매일 열린다</summary>
    public class EntryWindowView : RanchViewBase
    {
        [Header("경주 선택 (texts: 0 이름, 1 정보, 2 상태)")]
        [SerializeField] private UIRow[] raceButtons;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button toggleButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text toggleLabel;
        [SerializeField] private TMP_Text hintText;

        private int raceIndex;
        private int cursor;
        private readonly List<HorseData> candidates = new List<HorseData>();

        private void Awake()
        {
            for (int i = 0; i < raceButtons.Length; i++) raceButtons[i].Bind(i, r => { raceIndex = r; Refresh(); });
            prevButton.onClick.AddListener(() => { cursor--; Refresh(); });
            nextButton.onClick.AddListener(() => { cursor++; Refresh(); });
            toggleButton.onClick.AddListener(OnToggle);
        }

        public override void Refresh()
        {
            var g = Game;
            for (int i = 0; i < raceButtons.Length && i < Data.races.Length; i++)
            {
                var def = Data.races[i];
                var r = g.Race(i);
                var row = raceButtons[i];
                var h = g.FindHorse(r.entryId);
                row.SetText(0, def.displayName + (r.grandPrix ? "  <color=#B8860B>G1</color>" : ""));
                row.SetText(1, def.distance.ToString("N0") + "m · 참가비 " + Gold(g.EntryFee(i)) + " · 상금 " + Gold(g.Prize(i, 1)) + "/" + Gold(g.Prize(i, 2)) + "/" + Gold(g.Prize(i, 3)));
                row.SetText(2, r.done ? "경주 종료" : h != null ? "출전: " + h.name : "출전마 없음 (NPC만 출주)");
                row.SetTextColor(2, r.done ? UITheme.Mute : UITheme.Good);
                row.SetBackground(i == raceIndex ? UITheme.Panel2 : UITheme.Panel);
                row.SetInteractable(true);
            }

            var rs = g.Race(raceIndex);
            candidates.Clear();
            string reason;
            for (int i = 0; i < g.State.horses.Count; i++)
                if (g.CanEnter(g.State.horses[i], raceIndex, out reason)) candidates.Add(g.State.horses[i]);

            bool locked = rs.slip.Count > 0 || rs.done;
            var entered = g.FindHorse(rs.entryId);
            HorseData shown = null;
            if (entered != null)
            {
                shown = entered;
                nameText.text = "<color=#2A6A3D>출전</color>  " + entered.name + "  <size=75%>합계 " + entered.Total.ToString("0") + "</size>";
                toggleLabel.text = "등록 취소 (환불)";
                toggleButton.interactable = !locked;
                prevButton.interactable = nextButton.interactable = false;
            }
            else if (candidates.Count > 0)
            {
                cursor = (cursor % candidates.Count + candidates.Count) % candidates.Count;
                shown = candidates[cursor];
                nameText.text = shown.name + "  <size=75%>합계 " + shown.Total.ToString("0") + "  (" + (cursor + 1) + "/" + candidates.Count + ")</size>";
                toggleLabel.text = "출전 등록 · " + Gold(g.EntryFee(raceIndex));
                toggleButton.interactable = !locked;
                prevButton.interactable = nextButton.interactable = candidates.Count > 1;
            }
            else
            {
                nameText.text = "<color=#566458>출전 가능한 말이 없습니다</color>";
                toggleLabel.text = "출전 등록";
                toggleButton.interactable = false;
                prevButton.interactable = nextButton.interactable = false;
            }

            string note = "";
            if (shown != null)
            {
                note = "스탯 ";
                for (int s = 0; s < RanchConst.StatCount; s++) note += Data.stats[s].shortName + Mathf.RoundToInt(shown.stats[s]) + " ";
                note += "· 컨디션 " + Mathf.RoundToInt(shown.condition);
                if (shown.fatigue > 30f) note += " · <color=#B06D12>피로 " + Mathf.RoundToInt(shown.fatigue) + " (속도 -" + ((1f - RaceSimulator.FatigueFactor(shown.fatigue)) * 100f).ToString("0.0") + "%)</color>";
                if (shown.injury > 0) note += " · <color=#B3342A>" + (shown.injury == 2 ? "중상" : "경상") + "</color>";
                note += "\n";
            }
            hintText.text = note + (rs.done ? "이미 끝난 경주입니다." : rs.slip.Count > 0 ? "이 경주는 마권이 팔려 출전마를 바꿀 수 없습니다." : "훈련 중·은퇴마·오늘 이미 달린 말은 출전할 수 없습니다. 말 1두는 하루 1경주.");
        }

        private void OnToggle()
        {
            var rs = Game.Race(raceIndex);
            if (rs.entryId > 0) Game.SetEntry(raceIndex, 0);
            else if (candidates.Count > 0) Game.SetEntry(raceIndex, candidates[cursor % candidates.Count].id);
        }
    }
}
