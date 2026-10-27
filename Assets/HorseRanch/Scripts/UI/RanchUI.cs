using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HorseRanch
{
    /// <summary>
    /// 화면 총괄: 상단 바, 탭 전환, 게임 이벤트 → 팝업/토스트/경주 재생 연결
    /// </summary>
    public class RanchUI : MonoBehaviour
    {
        public const int TabStable = 0, TabRace = 1, TabShop = 2, TabBreed = 3, TabAchievement = 4;

        [Header("참조")]
        [SerializeField] private RanchGameRunner runner;
        [SerializeField] private ModalView modal;
        [SerializeField] private ToastView toast;
        [SerializeField] private RacePlaybackView playback;

        [Header("상단 바")]
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text titlesText;
        [SerializeField] private TMP_Text goalText;
        [SerializeField] private Button nextDayButton;

        [Header("탭 (순서: 마구간, 경마장, 상점, 교배소, 업적)")]
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private Image[] tabUnderlines;
        [SerializeField] private TMP_Text[] tabLabels;
        [SerializeField] private GameObject[] tabPanels;

        private int currentTab;
        private readonly Dictionary<int, RaceReplay> lastReplays = new Dictionary<int, RaceReplay>();
        private readonly StringBuilder sb = new StringBuilder(256);

        public ModalView Modal { get { return modal; } }
        public bool IsPlaying { get { return playback != null && playback.IsPlaying; } }
        public RanchGameRunner Runner { get { return runner; } }

        private void Awake()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int captured = i;
                tabButtons[i].onClick.AddListener(() => ShowTab(captured));
            }
            nextDayButton.onClick.AddListener(OnNextDay);
            if (playback != null) playback.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            var g = runner.Game;
            if (g == null) return;
            g.Changed += RefreshTopBar;
            g.Toast += OnToast;
            g.AchievementUnlocked += OnAchievement;
            g.TrainingEventTriggered += OnTrainingEvent;
            g.RaceFinished += OnRaceFinished;
            g.LotRevealed += OnLotRevealed;
            g.FoalBorn += OnFoalBorn;
            g.GameCleared += OnGameCleared;
            g.Bankrupt += OnBankrupt;
        }

        private void OnDisable()
        {
            var g = runner != null ? runner.Game : null;
            if (g == null) return;
            g.Changed -= RefreshTopBar;
            g.Toast -= OnToast;
            g.AchievementUnlocked -= OnAchievement;
            g.TrainingEventTriggered -= OnTrainingEvent;
            g.RaceFinished -= OnRaceFinished;
            g.LotRevealed -= OnLotRevealed;
            g.FoalBorn -= OnFoalBorn;
            g.GameCleared -= OnGameCleared;
            g.Bankrupt -= OnBankrupt;
        }

        private void Start()
        {
            ShowTab(TabStable);
            RefreshTopBar();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            // 단축키 (New Input System): 1~5 탭 전환, N 다음 날
            var kb = Keyboard.current;
            if (kb == null || modal.IsOpen || IsPlaying) return;
            if (kb.digit1Key.wasPressedThisFrame) ShowTab(0);
            else if (kb.digit2Key.wasPressedThisFrame) ShowTab(1);
            else if (kb.digit3Key.wasPressedThisFrame) ShowTab(2);
            else if (kb.digit4Key.wasPressedThisFrame) ShowTab(3);
            else if (kb.digit5Key.wasPressedThisFrame) ShowTab(4);
            else if (kb.nKey.wasPressedThisFrame) OnNextDay();
#endif
        }

        public void ShowTab(int index)
        {
            if (IsPlaying) return;
            currentTab = index;
            for (int i = 0; i < tabPanels.Length; i++)
            {
                bool on = i == index;
                if (tabPanels[i].activeSelf != on) tabPanels[i].SetActive(on);
                tabUnderlines[i].color = on ? UITheme.Turf : Color.clear;
                tabLabels[i].color = on ? UITheme.Ink : UITheme.Mute;
            }
        }

        public void RefreshTopBar()
        {
            var g = runner.Game;
            var s = g.State;
            dayText.text = s.day.ToString();
            goldText.text = s.gold.ToString("N0") + " G";
            titlesText.text = s.titles.Count > 0 ? "칭호  " + string.Join(" · ", s.titles.ToArray()) : "";

            sb.Length = 0;
            int d = g.DaysToGrandPrix;
            if (d == 0) sb.Append("<color=#B8860B>오늘 대상경주(G1) 개최!</color>");
            else if (d > 0) sb.Append("다음 대상경주까지 ").Append(d).Append("일");
            sb.Append("   목표: G1 제패 ");
            for (int i = 0; i < s.grandPrixWon.Length; i++)
                sb.Append(i < g.Data.races.Length ? g.Data.races[i].displayName : "?").Append(s.grandPrixWon[i] ? " ●" : " ○").Append(i < s.grandPrixWon.Length - 1 ? " / " : "");
            goalText.text = sb.ToString();

            bool playing = IsPlaying;
            nextDayButton.interactable = !playing;
            for (int i = 0; i < tabButtons.Length; i++) tabButtons[i].interactable = !playing;
        }

        private void OnNextDay()
        {
            if (IsPlaying) return;
            runner.Game.NextDay();
        }

        // ===== 경주 재생 =====
        public bool HasReplay(int raceIndex) { return lastReplays.ContainsKey(raceIndex); }

        public void PlayReplay(int raceIndex)
        {
            RaceReplay r;
            if (lastReplays.TryGetValue(raceIndex, out r)) StartPlayback(r, false);
        }

        private void OnRaceFinished(RaceReplay replay)
        {
            lastReplays[replay.raceIndex] = replay;
            StartPlayback(replay, true);
        }

        private void StartPlayback(RaceReplay replay, bool showResult)
        {
            for (int i = 0; i < tabPanels.Length; i++) tabPanels[i].SetActive(false);
            playback.gameObject.SetActive(true);
            RefreshTopBarInteractable(false);
            playback.Play(replay, runner.Database, () =>
            {
                playback.gameObject.SetActive(false);
                RefreshTopBarInteractable(true);
                ShowTab(TabRace);
                if (showResult) modal.Show(ResultTitle(replay), ResultBody(replay.result));
            });
        }

        private void RefreshTopBarInteractable(bool on)
        {
            nextDayButton.interactable = on;
            for (int i = 0; i < tabButtons.Length; i++) tabButtons[i].interactable = on;
        }

        private string ResultTitle(RaceReplay r)
        {
            return runner.Game.Data.races[r.raceIndex].displayName + (r.grandPrix ? " 대상경주(G1)" : "") + " 결과";
        }

        /// <summary>결과표 (TMP 리치 텍스트, &lt;pos&gt;로 열 정렬)</summary>
        public static string ResultBody(RaceResultData r)
        {
            var b = new StringBuilder(512);
            for (int i = 0; i < r.rows.Count; i++)
            {
                var row = r.rows[i];
                if (row.mine) b.Append("<b>");
                b.Append(row.position).Append(row.position == "중지" ? "" : "착")
                 .Append("<pos=12%><color=#").Append(UITheme.SilkHex[row.lane - 1]).Append(">■</color> ").Append(row.lane).Append("번  ")
                 .Append(row.name).Append(row.mine ? " (내 말)" : "")
                 .Append("<pos=78%>").Append(row.time);
                if (row.mine) b.Append("</b>");
                b.Append('\n');
            }
            if (r.notes.Count > 0) b.Append('\n');
            for (int i = 0; i < r.notes.Count; i++)
                b.Append("<color=").Append(UITheme.Tone(r.notes[i].tone)).Append(">• ").Append(r.notes[i].text).Append("</color>\n");
            return b.ToString();
        }

        // ===== 게임 이벤트 → 팝업 =====
        private void OnToast(string message) { toast.Show(message, false); }

        private void OnAchievement(AchievementDefinition a) { toast.Show("업적 달성 · " + a.displayName, true); }

        private void OnTrainingEvent(int horseId, TrainingEventDefinition e)
        {
            var h = runner.Game.FindHorse(horseId);
            if (h == null) return;
            modal.Show("돌발 이벤트 · " + h.name, e.text,
                new ModalAction(e.riskyChoice + " (도박 50%)", () => ShowEventResult(h, true), ModalStyle.Primary),
                new ModalAction(e.safeChoice + " (안전)", () => ShowEventResult(h, false)));
        }

        private void ShowEventResult(HorseData h, bool risky)
        {
            string result = runner.Game.ResolveTrainingEvent(h.id, risky);
            modal.Show(h.name, result);
        }

        private void OnLotRevealed(HorseData h, List<NoticeData> notes)
        {
            var b = new StringBuilder(256);
            b.Append(h.male ? "수말" : "암말").Append(" · ").Append(runner.Database.CoatName(h.coat))
             .Append(" · 스탯 합계 <b>").Append(h.Total.ToString("0")).Append("</b>\n\n");
            var data = runner.Game.Data;
            for (int i = 0; i < RanchConst.StatCount; i++)
            {
                bool hi = h.stats[i] >= 10f;
                b.Append(hi ? "<color=#B8860B><b>" : "").Append(data.stats[i].displayName).Append("<pos=30%>")
                 .Append(h.stats[i].ToString("0")).Append(hi ? "</b></color>" : "").Append('\n');
            }
            for (int i = 0; i < notes.Count; i++)
                b.Append("\n<color=").Append(UITheme.Tone(notes[i].tone)).Append(">").Append(notes[i].text).Append("</color>");
            modal.Show("감정 결과 · " + h.name, b.ToString(), new ModalAction("마구간에 들이기", null, ModalStyle.Primary));
        }

        private void OnFoalBorn(HorseData f)
        {
            var b = new StringBuilder(256);
            b.Append(f.male ? "수말" : "암말").Append(" · 합계 <b>").Append(f.Total.ToString("0.0")).Append("</b>\n\n");
            var data = runner.Game.Data;
            for (int i = 0; i < RanchConst.StatCount; i++)
                b.Append(data.stats[i].displayName).Append("<pos=30%>").Append(f.stats[i].ToString("0.0")).Append('\n');
            modal.Show("망아지 탄생 · " + f.name, b.ToString());
        }

        private void OnGameCleared()
        {
            modal.Show("목표 달성!", "단거리와 장거리 대상경주를 모두 제패했습니다.\n최고의 목장주가 되었습니다. 계속 플레이할 수 있습니다.",
                new ModalAction("계속하기", null, ModalStyle.Primary));
        }

        private void OnBankrupt()
        {
            modal.Show("파산", "말도 없고 새 말을 살 골드도 없습니다.\n목장을 처음부터 다시 시작합니다.",
                new ModalAction("새 게임", RestartGame, ModalStyle.Danger));
        }

        public void RestartGame()
        {
            modal.CloseAll();
            lastReplays.Clear();
            runner.RestartGame();
            ShowTab(TabStable);
        }
    }
}
