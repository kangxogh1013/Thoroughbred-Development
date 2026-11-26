using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    public enum WindowId { Stable = 0, Shop = 1, Breed = 2, Achievements = 3, Inventory = 4, Bet = 5, Exchange = 6, Entry = 7, Bulletin = 8, Map = 9, Market = 10, Records = 11, Slot = 12 }

    /// <summary>
    /// 마을 탐험형 UI 총괄: HUD, 상호작용 안내, 대화창, 기능 창, 경주 관람, 화면 전환(페이드), 게임 이벤트 연결.
    /// 창이나 대화가 열려 있는 동안(IsBusy) 플레이어는 움직이지 않는다.
    /// </summary>
    public partial class GameUI : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private RanchGameRunner runner;
        [SerializeField] private PlayerController player;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private ModalView modal;
        [SerializeField] private ToastView toast;
        [SerializeField] private DialogueView dialogue;
        [Tooltip("경기장 트랙 위 실경주")]
        [SerializeField] private WorldRaceView race;
        [Tooltip("쓰레기통 연타 뒤지기")]
        [SerializeField] private SearchView search;
        [Header("타이틀 / 설정 / 사운드")]
        [SerializeField] private TitleView title;
        [SerializeField] private SettingsView settings;
        [SerializeField] private AudioManager audioManager;
        [Header("스토리 (오프닝 · 튜토리얼 길 안내 · 엔딩)")]
        [SerializeField] private CutsceneView cutscene;
        [SerializeField] private NavigationGuide guide;
        [SerializeField] private MemorialSite memorial;
        [SerializeField] private WorldMap map;
        [Tooltip("깊은 숲의 천둥 (데려오면 사라짐)")]
        [SerializeField] private ThunderSpot thunderSpot;
        [Header("구역 분위기 (도박장 말풍선)")]
        [SerializeField] private SpeechBubble bubble;
        [Tooltip("도박장 손님이 소리치는 간격 (초, 최소~최대)")]
        [SerializeField] private Vector2 hallShoutInterval = new Vector2(0.6f, 1.6f);
        [Tooltip("마구간 말 소리 간격 (초, 최소~최대)")]
        [SerializeField] private Vector2 barnSoundInterval = new Vector2(4f, 9f);
        [Tooltip("경주 중 관중 함성 간격 (초, 최소~최대)")]
        [SerializeField] private Vector2 raceCheerInterval = new Vector2(2f, 4f);

        [Header("HUD")]
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text ticketText;
        [SerializeField] private TMP_Text locationText;
        [SerializeField] private TMP_Text goalText;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private Image fade;

        [Header("창 (WindowId 순서: 마구간, 상점, 교배, 업적, 인벤토리, 마권, 교환, 출전, 게시판, 지도, 시장)")]
        [SerializeField] private WindowView[] windows;

        [Header("시작 위치 (집 앞)")]
        [SerializeField] private Vector2Int spawnCell;

        private int openWindow = -1;
        private float fadeT = -1f;           // -1 = 페이드 없음
        private Action fadeMiddle;
        private bool fadeMidDone;
        private string lastPrompt;
        private int busyFrame;
        private readonly Queue<string> autoRaceNews = new Queue<string>();
        private readonly System.Random talkRng = new System.Random();
        private Interactable[] allInteractables;
        private float guideTimer;
        private bool pendingEnding;
        private bool inEnding;
        private bool gameOverShown;
        private string currentArea = "";
        private float ambientTimer = 3f;
        private bool racing;
        private List<Interactable> hallGuests;

        public ModalView Modal { get { return modal; } }
        public bool IsFading { get { return fadeT >= 0f; } }
        public bool IsBusy
        {
            get { return openWindow >= 0 || dialogue.IsOpen || modal.IsOpen || race.IsPlaying || IsFading || search.IsOpen || title.IsOpen || settings.IsOpen || cutscene.IsOpen || inEnding; }
        }
        /// <summary>커피 효과: Shift 없이도 달리기 속도</summary>
        public bool FastWalk { get { return Game != null && Game.HasCoffee; } }
        private RanchGame Game { get { return runner.Game; } }

        private void Awake()
        {
            for (int i = 0; i < windows.Length; i++)
            {
                windows[i].CloseButton.onClick.AddListener(CloseWindow);
                windows[i].gameObject.SetActive(false);
            }
            fade.gameObject.SetActive(false);
            promptRoot.SetActive(false);
        }

        private void OnEnable()
        {
            var g = Game;
            if (g == null) return;
            g.Changed += RefreshHud;
            g.Toast += OnToast;
            g.AchievementUnlocked += OnAchievement;
            g.TrainingEventTriggered += OnTrainingEvent;
            g.RaceFinished += OnRaceFinished;
            g.RaceAutoRun += OnRaceAutoRun;
            g.LotRevealed += OnLotRevealed;
            g.FoalBorn += OnFoalBorn;
            g.GameCleared += OnGameCleared;
            g.GameOver += OnGameOver;
            g.StoryNotice += OnStoryNotice;
            g.TutorialChanged += OnTutorialChanged;
        }

        private void OnDisable()
        {
            var g = runner != null ? runner.Game : null;
            if (g == null) return;
            g.Changed -= RefreshHud;
            g.Toast -= OnToast;
            g.AchievementUnlocked -= OnAchievement;
            g.TrainingEventTriggered -= OnTrainingEvent;
            g.RaceFinished -= OnRaceFinished;
            g.RaceAutoRun -= OnRaceAutoRun;
            g.LotRevealed -= OnLotRevealed;
            g.FoalBorn -= OnFoalBorn;
            g.GameCleared -= OnGameCleared;
            g.GameOver -= OnGameOver;
            g.StoryNotice -= OnStoryNotice;
            g.TutorialChanged -= OnTutorialChanged;
        }

        private void Start()
        {
            player.WarpTo(spawnCell, Vector2Int.down);
            cameraFollow.Snap();
            allInteractables = map.GetComponentsInChildren<Interactable>(true);
            guide.Clear();
            RefreshHud();
            title.Show();   // 타이틀 화면부터 (이어하기 / 새 게임 / 설정 / 종료)
        }

        /// <summary>타이틀에서 시작. fresh면 새 게임 + 안내, 아니면 저장된 곳부터</summary>
        public void BeginPlay(bool fresh)
        {
            runner.MarkPlayed();
            gameOverShown = false;
            if (fresh)
            {
                RestartGame();
                audioManager.PlayBgm(BgmId.GameOver);   // 오프닝은 쓸쓸한 단조
                audioManager.PlayAmbience(AmbienceId.None, 0f);
                // 오프닝: 아버지의 사고와 남겨진 목장 → 튜토리얼 시작 여부
                cutscene.Play(StoryText.Intro(Game), () =>
                {
                    Game.MarkIntroSeen();
                    RefreshAreaAudio();
                    dialogue.Show("안내", "질주마 목장에 오신 것을 환영합니다! 소지금 " + Game.State.gold.ToString("N0") + "G로 시작합니다.\n"
                        + "방향키/WASD 이동 - E 상호작용 - Shift 달리기 - Tab 인벤토리 - J 업적 - M 지도 - Esc 메뉴\n"
                        + "처음이라면 튜토리얼을 추천해요. 머리 위 화살표가 갈 곳을 알려주고, 가게마다 <b>첫 구매 1회 무료</b>예요.",
                        new ModalAction("튜토리얼 시작", () => { RefreshHud(); UpdateGuide(); }, ModalStyle.Primary),
                        new ModalAction("건너뛰기", () => { Game.SkipTutorial(); UpdateGuide(); }));
                });
            }
            else
            {
                RefreshHud();
                RefreshAreaAudio();
                toast.Show(Game.State.day + "일차부터 이어합니다", true);
            }
        }

        // ===== 구역별 소리 =====
        /// <summary>지금 있는 구역에 맞는 배경음 + 환경음 (도박장 / 경기장 / 깊은 숲 / 그 외 마을)</summary>
        private void RefreshAreaAudio()
        {
            if (title.IsOpen || inEnding || gameOverShown || racing || cutscene.IsOpen) return;
            switch (currentArea)
            {
                // 도박장 · 경기장은 평소엔 배경음만 (관중 소리 · 고함은 경주 중에만)
                case "마권 도박장": audioManager.PlayBgm(BgmId.Hall); audioManager.PlayAmbience(AmbienceId.None, 0f); break;
                case "경기장": audioManager.PlayBgm(BgmId.Stadium); audioManager.PlayAmbience(AmbienceId.None, 0f); break;
                case "깊은 숲": audioManager.PlayBgm(BgmId.Ending); audioManager.PlayAmbience(AmbienceId.None, 0f); break;
                default: audioManager.PlayBgm(BgmId.Town); audioManager.PlayAmbience(AmbienceId.None, 0f); break;
            }
        }

        /// <summary>도박장 고함 · 마구간 말 소리 · 경주 중 함성 (Update에서 호출)</summary>
        private void UpdateAmbientEvents()
        {
            if (title.IsOpen || cutscene.IsOpen || gameOverShown) return;
            ambientTimer -= Time.deltaTime;
            if (ambientTimer > 0f) return;
            if (racing)
            {
                // 경주 중: 함성 + 가끔 관중의 고함 · 야유
                float roll = UnityEngine.Random.value;
                if (roll < 0.6f) audioManager.PlaySfx(SfxId.Cheer, UnityEngine.Random.Range(0.45f, 0.8f));
                else if (roll < 0.88f) audioManager.PlaySfx(SfxId.Shout, UnityEngine.Random.Range(0.35f, 0.6f));
                else audioManager.PlaySfx(SfxId.Boo, UnityEngine.Random.Range(0.3f, 0.5f));
                ambientTimer = UnityEngine.Random.Range(raceCheerInterval.x, raceCheerInterval.y);
                return;
            }
            if (inEnding) { ambientTimer = 2f; return; }
            if (currentArea == "마권 도박장")
            {
                HallShout();
                ambientTimer = UnityEngine.Random.Range(hallShoutInterval.x, hallShoutInterval.y);
            }
            else if (currentArea == "마구간")
            {
                audioManager.PlaySfx(UnityEngine.Random.value < 0.55f ? SfxId.Neigh : SfxId.Snort, UnityEngine.Random.Range(0.5f, 0.85f));
                ambientTimer = UnityEngine.Random.Range(barnSoundInterval.x, barnSoundInterval.y);
            }
            else ambientTimer = 2f;
        }

        // 도박장 손님들의 고함 (말풍선 + 소리)
        private static readonly string[] HallShouts =
        {
            "아 진짜! 또 꽝이야!", "코 차이로 졌어, 코 차이로!", "가라 가라 가라!", "누가 7번 찍으래!", "삼쌍승 왔다! 크하하!",
            "배당이 왜 이래!", "환불해 줘, 환불!", "이번엔 진짜 느낌 온다!", "아이고 내 월급…", "에잇, 마권 다 찢어버려야지!",
            "또 흑풍이야? 짜고 치는 거 아냐?", "한 판만 더! 딱 한 판만!", "우와아아!", "야! 거기 줄 좀 서!",
            "어제 3번이 왔으니 오늘은 4번이야.", "장거리는 체력! 무조건 체력이라고!", "배당 30배? 이건 사야 돼.",
            "마누라한테는 비밀이다…", "정보통 말만 믿었다가 쫄딱 망했네.", "1번 2번 3번, 순서대로 오면 대박!",
            "전광판 좀 봐, 배당 또 바뀌었어!", "오늘 컨디션 좋은 말이 누구야?", "복승이 안전하다니까.",
            "단승 한 방이 사나이의 로망이지!", "어휴, 차라리 내가 뛰고 말지.", "저 말 다리 굵은 거 봐, 내구력 좋겠다.",
            "교환소 줄이 왜 이렇게 길어?", "오늘 분수에 동전 던졌어? 난 던졌지!", "다음 경주까지 몇 분 남았지?",
            "막판에 치고 나오는 놈이 진짜야.", "아깝다! 2착이었는데!", "이거 맞으면 저녁은 내가 쏜다!",
            "초짜는 단승부터 사 보라고.", "이번 주 대상경주는 누가 이길까?", "한 장만 더 사고 집에 갈 거야, 진짜로.",
            "흑철 목장 말은 왜 이렇게 빨라?", "오늘은 슬롯이 잘 터진대.", "어이 사장님, 커피 한 잔 더!", "내일은 꼭 본전 찾는다.",
            "연승식 세 장 다 맞았다!", "어제 그 말, 결국 꼴찌 했지?", "배당판 보는 것만으로도 재밌어.", "이 동네에서 제일 시끄러운 곳이 여기야.",
            "누가 내 마권 봤어? 분명 여기 뒀는데!", "4번 말 오늘 컨디션 최고래!", "초반에 뛰쳐나가는 말은 막판에 지쳐.", "삼쌍승은 꿈이야, 꿈.",
            "기수 모자 색만 보고 골랐는데 맞았어!", "교환소 아가씨, 오늘도 잘 부탁해요!", "아직 경주 안 끝났다, 끝까지 봐!",
        };

        private void HallShout()
        {
            if (hallGuests == null)
            {
                hallGuests = new List<Interactable>();
                for (int i = 0; i < allInteractables.Length; i++)
                {
                    var it = allInteractables[i];
                    if ((it.DisplayName == "손님" || it.DisplayName == "슬롯 손님") && it.GetComponent<NpcController>() != null)
                    {
                        var ar = map.AreaAt(CellOf(it));
                        if (ar != null && ar.AreaName == "마권 도박장") hallGuests.Add(it);
                    }
                }
            }
            // 말풍선만 (소리 없음, 도박장은 배경음만). 여러 개가 동시에 떠 있을 수 있음
            if (hallGuests.Count == 0) return;
            var free = FreeBubble();
            if (free == null) return;
            Interactable who = null;
            for (int tries = 0; tries < 6 && who == null; tries++)
            {
                var c = hallGuests[UnityEngine.Random.Range(0, hallGuests.Count)];
                if (c.gameObject.activeInHierarchy && !BubbleOn(c.transform)) who = c;
            }
            if (who == null) return;
            var pool = who.DisplayName == "슬롯 손님" ? SlotShouts : HallShouts;
            string line = pool[UnityEngine.Random.Range(0, pool.Length)];
            free.ShowOn(who.transform, new Vector3(0f, 1.9f, 0f), line, UnityEngine.Random.Range(2f, 3.2f));
        }

        private SpeechBubble FreeBubble()
        {
            if (!bubble.IsShowing) return bubble;
            if (extraBubbles != null) for (int i = 0; i < extraBubbles.Length; i++) if (!extraBubbles[i].IsShowing) return extraBubbles[i];
            return null;
        }

        private bool BubbleOn(Transform t)
        {
            if (bubble.IsShowing && bubble.Target == t) return true;
            if (extraBubbles != null) for (int i = 0; i < extraBubbles.Length; i++) if (extraBubbles[i].IsShowing && extraBubbles[i].Target == t) return true;
            return false;
        }

        private void HideBubbles()
        {
            bubble.Hide();
            if (extraBubbles != null) for (int i = 0; i < extraBubbles.Length; i++) extraBubbles[i].Hide();
        }

        private static readonly string[] SlotShouts =
        {
            "체리 두 개! 본전은 건졌다!", "7이 두 개 섰는데… 마지막이 BAR라니!", "이 기계 오늘 뜨거워!", "한 번만 더 돌리고 간다.",
            "잭팟 터지면 말 한 마리 산다!", "왜 나만 꽝이야!", "옆 기계가 터졌대, 자리 바꿀까?", "말 말 말! 말이 세 개!",
            "종소리 좀 울려 봐라!", "BAR 세 개면 35배라고!",
        };

        /// <summary>Esc (아무 창도 없을 때): 일시정지 메뉴</summary>
        private void OpenPauseMenu()
        {
            SetPrompt(null);
            audioManager.PlaySfx(SfxId.Click);
            modal.Show("메뉴", Game.State.day + "일차 · " + Game.State.gold.ToString("N0") + "G\n<size=85%><color=#566458>진행 상황은 자동 저장됩니다.</color></size>",
                new ModalAction("계속하기", null, ModalStyle.Primary),
                new ModalAction("설정", () => settings.Open(null)),
                new ModalAction("저장하고 타이틀로", () => { runner.Save(); player.Mover.Stand(); guide.Clear(); HideBubbles(); audioManager.PlayAmbience(AmbienceId.None, 0f); title.Show(); }));
        }

        public void PlaySfx(SfxId id) { audioManager.PlaySfx(id); }

        private void Update()
        {
            UpdateFade();
            UpdateAmbientEvents();
            UpdateLife();
            guideTimer -= Time.deltaTime;
            if (guideTimer <= 0f) { guideTimer = 0.2f; UpdateGuide(); }
            if (pendingEnding && !IsBusy) { pendingEnding = false; PlayEnding(); return; }
            if (cutscene.IsOpen || inEnding) { busyFrame = Time.frameCount; return; }
            if (modal.IsOpen || race.IsPlaying || IsFading || dialogue.IsOpen || search.IsOpen || title.IsOpen || settings.IsOpen) { busyFrame = Time.frameCount; return; }

            if (openWindow >= 0)
            {
                if (GameInput.Cancel) CloseWindow();
                else if (GameInput.Inventory && openWindow == (int)WindowId.Inventory) CloseWindow();
                else if (GameInput.Achievements && openWindow == (int)WindowId.Achievements) CloseWindow();
                else if (GameInput.Map && openWindow == (int)WindowId.Map) CloseWindow();
                else if (GameInput.Records && openWindow == (int)WindowId.Records) CloseWindow();
                return;
            }
            // 같은 Esc로 다른 창을 닫은 직후에는 메뉴를 열지 않음
            if (GameInput.Cancel && !player.Mover.IsSeated && busyFrame < Time.frameCount - 1) OpenPauseMenu();
            else if (GameInput.Inventory) OpenWindow(WindowId.Inventory, null);
            else if (GameInput.Achievements) OpenWindow(WindowId.Achievements, null);
            else if (GameInput.Map) OpenWindow(WindowId.Map, null);
            else if (GameInput.Records) OpenWindow(WindowId.Records, null);
            else if (GameInput.Dismount && Game.RidingHorse != null) GetOff(true);
        }

        // ===== HUD =====
        public void RefreshHud()
        {
            var g = Game;
            var s = g.State;
            dayText.text = s.day + "일차";
            goldText.text = s.gold.ToString("N0") + " G";
            int won = g.CountTickets(TicketStatus.Won), pending = g.CountTickets(TicketStatus.Pending);
            ticketText.text = "마권 " + s.tickets.Count + "장" + (won > 0 ? "  <color=#F2BF37>적중 " + won + "장 (" + g.PendingPayout.ToString("N0") + "G)</color>" : pending > 0 ? "  <color=#8B927F>대기 " + pending + "</color>" : "")
                + (g.IsLucky ? "   <color=#7FD38A>행운</color>" : "")
                + (g.IsSearchBoosted ? "  <color=#E8C040>김밥</color>" : "")
                + (g.HasCoffee ? "  <color=#C8A060>커피</color>" : "")
                + (s.trash > 0 ? "  <color=#8B927F>쓰레기 " + s.trash + "</color>" : "");
            // 오른쪽 위: 지금 목표 (튜토리얼 단계 또는 그랑프리) + 아버지의 빚
            var sb = new StringBuilder(96);
            sb.Append(g.InTutorial ? "<color=#7FD38A>튜토리얼 " + ((int)g.Tutorial + 1) + "/" + (int)TutorialStep.Done + "</color>  " : "<color=#F2BF37>목표</color>  ")
              .Append(StoryText.Goal(g)).Append('\n');
            if (g.HasDebt)
            {
                int dp = g.DaysToPayment;
                bool short_ = s.gold < g.NextPaymentDue && dp <= 2;
                sb.Append("<size=85%>빚 ").Append(s.debt.ToString("N0")).Append("G - 상환 ")
                  .Append(short_ ? "<color=#E06050>" : "").Append(dp).Append("일 뒤 ").Append(g.NextPaymentDue.ToString("N0")).Append("G").Append(short_ ? "</color>" : "")
                  .Append(s.missedPayments > 0 ? "  <color=#E06050>연체 " + s.missedPayments + "/" + g.Data.balance.maxMissedPayments + "</color>" : "").Append("</size>");
            }
            else sb.Append("<size=85%><color=#7FD38A>빚 모두 청산</color></size>");
            goalText.text = sb.ToString();
            if (!inEnding) memorial.ShowTrophy(s.cleared);
            thunderSpot.Show(!s.thunderFound);
            SyncMount();
        }

        // ===== 튜토리얼 길 안내 =====
        /// <summary>지금 튜토리얼 단계의 목표 (상호작용 대상)</summary>
        private Interactable TutorialTarget()
        {
            var g = Game;
            if (!g.InTutorial || allInteractables == null) return null;
            switch (g.Tutorial)
            {
                case TutorialStep.TalkMom: return FindTarget(InteractKind.Talk, "엄마");
                case TutorialStep.VisitStable: return FindTarget(InteractKind.Stable, null);
                case TutorialStep.Train: return FindTarget(InteractKind.Training, null);
                case TutorialStep.Shop: return FindTarget(InteractKind.Shop, "상점 주인");
                case TutorialStep.Market: return FindTarget(InteractKind.Market, "사료 가게 아저씨");
                case TutorialStep.Bet: return FindTarget(InteractKind.BetCounter, "마권 판매소");
                case TutorialStep.Watch: return FindTarget(InteractKind.RaceStaff, null);
                case TutorialStep.Bank: return FindTarget(InteractKind.Bank, null);
                case TutorialStep.Sleep: return FindTarget(InteractKind.Sleep, null);
            }
            return null;
        }

        private Interactable FindTarget(InteractKind kind, string name)
        {
            for (int i = 0; i < allInteractables.Length; i++)
            {
                var it = allInteractables[i];
                if (it.Kind == kind && (name == null || it.DisplayName == name)) return it;
            }
            return null;
        }

        private static Vector2Int CellOf(Interactable it)
        {
            var m = it.GetComponent<GridMover>();
            return m != null ? m.Cell : it.FirstCell;
        }

        /// <summary>목표가 다른 구역이면 그쪽으로 가는 문(없으면 마을로 나가는 문)을 가리킨다</summary>
        private void UpdateGuide()
        {
            if (Game == null || title.IsOpen || inEnding) { guide.Clear(); return; }
            var target = TutorialTarget() ?? BedtimeTarget();   // 튜토리얼 목표, 없으면 밤엔 집 침대
            if (target == null) { guide.Clear(); return; }
            var here = map.AreaAt(player.Mover.Cell);
            var goalCell = CellOf(target);
            var there = map.AreaAt(goalCell);
            if (here != null && there != null && here != there)
            {
                Interactable toTarget = null, toTown = null;
                var town = map.Area(0);
                for (int i = 0; i < allInteractables.Length; i++)
                {
                    var d = allInteractables[i];
                    if (d.Kind != InteractKind.Door || map.AreaAt(d.FirstCell) != here) continue;
                    var dest = map.AreaAt(d.WarpCell);
                    if (dest == there) toTarget = d;
                    else if (dest == town) toTown = d;
                }
                var door = toTarget ?? toTown;
                if (door != null) goalCell = door.FirstCell;
            }
            guide.SetTarget(GridMover.CellToWorld(goalCell));
        }

        private void OnTutorialChanged(TutorialStep step)
        {
            UpdateGuide();
            if (step == TutorialStep.Done)
            {
                toast.Show("튜토리얼 완료! 이제 " + Game.Data.races[Game.StoryRace].displayName + " 그랑프리 우승을 노려 보세요", true);
                return;
            }
            toast.Show("다음 목표: " + StoryText.Goal(Game), true);
        }

        /// <summary>처음 들른 곳이면 안내부터 보여주고 원래 상호작용을 이어간다</summary>
        private static string TipKey(InteractKind k)
        {
            switch (k)
            {
                case InteractKind.Stable: return "stable";
                case InteractKind.Training: return "training";
                case InteractKind.Shop: return "shop";
                case InteractKind.Market: return "market";
                case InteractKind.BetCounter: return "bet";
                case InteractKind.Exchange: return "exchange";
                case InteractKind.RaceStaff: return "watch";
                case InteractKind.Entry: return "entry";
                case InteractKind.Bank: return "bank";
            }
            return null;
        }

        public void ShowLocation(string name)
        {
            locationText.text = name;
            if (name == currentArea) return;
            currentArea = name;
            HideBubbles();
            if (name == "은행") audioManager.PlaySfx(SfxId.Bell);   // 문 열면 띠링
            ambientTimer = 1.5f;
            RefreshAreaAudio();
        }

        public void SetPrompt(string name)
        {
            if (name == lastPrompt) return;
            lastPrompt = name;
            promptRoot.SetActive(name != null);
            if (name != null) promptText.text = "<b>[E]</b>  " + name;
        }

        // ===== 창 =====
        public void OpenWindow(WindowId id, string title)
        {
            CloseWindow();
            openWindow = (int)id;
            windows[openWindow].Open(title);
            audioManager.PlaySfx(SfxId.WindowOpen);
            SetPrompt(null);
        }

        public void CloseWindow()
        {
            if (openWindow < 0) return;
            windows[openWindow].Close();
            openWindow = -1;
            audioManager.PlaySfx(SfxId.WindowClose, 0.8f);
        }

        // ===== 상호작용 =====
        public void Interact(Interactable it, PlayerController p)
        {
            var npc = it.GetComponent<NpcController>();
            if (npc != null) npc.TalkTo(p.Mover.Cell, 4f);
            if (it.Kind != InteractKind.Door) audioManager.PlaySfx(SfxId.Click);
            var g = Game;
            if (LifeInteract(it, p)) return;   // 밤 영업 종료 · 마구간 말 · 의뢰 게시판 · 기록실 · 말 탄 채 벤치
            string tipKey = TipKey(it.Kind);
            if (tipKey != null && g.MarkTipSeen(tipKey))
            {
                dialogue.ShowThen("처음 오셨군요", StoryText.Tip(tipKey, g), () => Interact(it, p));
                return;
            }
            switch (it.Kind)
            {
                case InteractKind.Door:
                    Warp(it.WarpCell, it.WarpFacing);
                    break;
                case InteractKind.Sleep:
                    dialogue.Show("", "푹신한 침대다. 자고 내일을 맞이할까요?\n<size=80%><color=#566458>관람하지 않은 오늘 경주는 자동으로 진행되고, 마권도 정산됩니다.</color></size>",
                        new ModalAction("잠자기", () => Sleep(it.WarpCell)), new ModalAction("그만두기", null));
                    break;
                case InteractKind.Stable:
                    g.AdvanceTutorial(TutorialStep.VisitStable);
                    dialogue.Show(it.DisplayName, StoryText.Line(it.DisplayName, g, talkRng, it.RandomLine()),
                        new ModalAction("말 관리하기 (판매 포함)", () => OpenWindow(WindowId.Stable, "마구간"), ModalStyle.Primary),
                        new ModalAction("시설 개선", () => AskFacility(it.DisplayName)),
                        new ModalAction("나중에", null));
                    break;
                case InteractKind.Bank:
                    AskBank(it.DisplayName);
                    break;
                case InteractKind.Thicket:
                    if (g.ForestOpen)
                        dialogue.Show("", StoryText.Thicket(g),
                            new ModalAction("덤불을 헤치고 들어가기", () => Warp(it.WarpCell, it.WarpFacing), ModalStyle.Primary),
                            new ModalAction("그만두기", null));
                    else { audioManager.PlaySfx(SfxId.Neigh, 0.35f); dialogue.Show("", StoryText.Thicket(g)); }
                    break;
                case InteractKind.Thunder:
                    if (g.State.thunderFound) { dialogue.Show("", "(천둥이 쉬던 자리. 풀이 동그랗게 눌려 있다.)"); break; }
                    audioManager.PlaySfx(SfxId.Neigh);
                    StoryLines(StoryText.ThunderMeet(g), 0, () =>
                        dialogue.Show(RanchGame.FatherHorse, "(천둥이 가만히 당신을 바라본다.)",
                            new ModalAction("목장으로 데려가기", () =>
                            {
                                if (!g.AdoptThunder()) return;
                                thunderSpot.Show(false);
                                audioManager.PlaySfx(SfxId.Fanfare);
                                dialogue.Show(RanchGame.FatherHorse, StoryText.ThunderHome);
                            }, ModalStyle.Primary),
                            new ModalAction("조금 더 지켜보기", null)));
                    break;
                case InteractKind.Grave:
                    dialogue.Show("", StoryText.Line(it.DisplayName, g, talkRng, null));
                    break;
                case InteractKind.Training:
                    dialogue.Show(it.DisplayName, StoryText.Line(it.DisplayName, g, talkRng, it.RandomLine()), new ModalAction("훈련 시키기", () => OpenWindow(WindowId.Stable, "훈련장")), new ModalAction("나중에", null));
                    break;
                case InteractKind.Shop:
                    dialogue.Show(it.DisplayName, it.RandomLine(), new ModalAction("물건 보기", () => OpenWindow(WindowId.Shop, "상점 · 마시장")), new ModalAction("나가기", null));
                    break;
                case InteractKind.Breed:
                    dialogue.Show(it.DisplayName, it.RandomLine(), new ModalAction("교배 상담", () => OpenWindow(WindowId.Breed, "교배소")), new ModalAction("나중에", null));
                    break;
                case InteractKind.BetCounter:
                    OpenWindow(WindowId.Bet, it.DisplayName);
                    break;
                case InteractKind.Exchange:
                    int won = g.CountTickets(TicketStatus.Won);
                    dialogue.Show(it.DisplayName, won > 0 ? "적중 마권 " + won + "장, 모두 " + g.PendingPayout.ToString("N0") + "G 입니다. 교환해 드릴까요?" : "교환할 적중 마권이 없네요. 결과가 나온 경주의 마권만 교환됩니다.",
                        new ModalAction("모두 교환", () => { g.RedeemWinningTickets(); audioManager.PlaySfx(SfxId.Coin); }, ModalStyle.Primary, won > 0),
                        new ModalAction("마권 목록 보기", () => OpenWindow(WindowId.Exchange, "교환소")),
                        new ModalAction("나가기", null));
                    break;
                case InteractKind.Bulletin:
                    OpenWindow(WindowId.Bulletin, it.DisplayName);
                    break;
                case InteractKind.Entry:
                    dialogue.Show(it.DisplayName, it.RandomLine(), new ModalAction("출전 등록", () => OpenWindow(WindowId.Entry, "출전 접수")), new ModalAction("나중에", null));
                    break;
                case InteractKind.RaceStaff:
                    AskWatchRace(it.DisplayName);
                    break;
                case InteractKind.HallStaff:
                    dialogue.Show(it.DisplayName, it.RandomLine(), new ModalAction("경기장으로 이동", () => Warp(it.WarpCell, it.WarpFacing), ModalStyle.Primary), new ModalAction("괜찮습니다", null));
                    break;
                case InteractKind.Tipster:
                    AskTip(it.DisplayName);
                    break;
                case InteractKind.Fountain:
                    int price = g.Data.balance.coinPrice;
                    if (g.IsLucky) dialogue.Show("", "물속에서 내가 던진 동전이 반짝인다. 오늘은 운이 좋을 것 같다.");
                    else dialogue.Show("", "맑은 물속에 동전이 가득하다. 소원을 빌며 동전을 던질까요?\n<size=80%><color=#566458>" + price + "G · 오늘 하루 행운 (경주마다 3분의 1 확률로 내 마권 하나가 적중)</color></size>",
                        new ModalAction("동전 던지기 (" + price + "G)", () => { if (g.ThrowCoin()) audioManager.PlaySfx(SfxId.Coin); if (g.IsLucky) dialogue.Show("", "퐁당! 동전이 반짝이며 가라앉았다.\n어쩐지 오늘은 운이 좋을 것 같다!"); }, ModalStyle.Primary, g.State.gold >= price),
                        new ModalAction("그만두기", null));
                    break;
                case InteractKind.GambleMaster:
                    dialogue.Show(it.DisplayName, (g.ForestOpen && !g.State.thunderFound
                            ? "그랑프리 봤네, 장하구먼. 하나 알려 주지. 자네 아버지 말 천둥… 마을 북쪽 큰길 끝, 가시덤불 너머 깊은 숲에 산다네. 이제 자네라면 길이 보일 걸세.\n"
                            : "허허, 여기까지 찾아오다니. ") + "내 예언 한번 들어볼 텐가? 하루에 한 번이네.",
                        new ModalAction("예언 듣기", () => dialogue.Show(it.DisplayName, g.AskMaster()), ModalStyle.Primary),
                        new ModalAction("됐어요", null));
                    break;
                case InteractKind.Search:
                    AskSearch(it);
                    break;
                case InteractKind.TrashBin:
                    AskTrashBin(it);
                    break;
                case InteractKind.Bench:
                    var c = it.FirstCell;
                    p.SitAt(new Vector3(c.x + 0.5f, c.y + 0.12f, 0f));
                    toast.Show("벤치에 앉았다. 주민들 수다가 들린다… (방향키로 일어나기)", false);
                    break;
                case InteractKind.Market:
                    dialogue.Show(it.DisplayName, it.RandomLine(), new ModalAction("음식 보기", () => OpenWindow(WindowId.Market, "시장")), new ModalAction("나중에", null));
                    break;
                default:
                    if (it.DisplayName == "엄마" && g.Tutorial == TutorialStep.TalkMom)
                    {
                        dialogue.ShowThen("엄마", "일어났니? 아버지 일 이후로 목장 일이 손에 안 잡히지…\n"
                            + "그래도 마구간에서 새싹이가 너만 기다리고 있단다. 박씨 아저씨한테 먼저 가 보렴.\n"
                            + "<size=85%><color=#566458>머리 위 화살표가 가야 할 곳을 가리켜요.</color></size>", () => g.AdvanceTutorial(TutorialStep.TalkMom));
                        break;
                    }
                    // 사람이 아닌 대상(남의 침대 · 표지판 등)은 이름표 없이 혼잣말처럼
                    dialogue.Show(npc != null ? it.DisplayName : "", StoryText.Line(it.DisplayName, g, talkRng, it.RandomLine()));
                    break;
            }
        }

        /// <summary>광장 은행원: 아버지의 빚 확인 · 미리 갚기</summary>
        private void AskBank(string name)
        {
            var g = Game;
            var s = g.State;
            var b = g.Data.balance;
            g.AdvanceTutorial(TutorialStep.Bank);
            if (!g.HasDebt) { dialogue.Show(name, "빚을 모두 갚으셨네요. 아버님도 자랑스러워하실 겁니다."); return; }
            int next = g.NextPaymentDue;
            string body = "남은 빚 <b>" + s.debt.ToString("N0") + "G</b>\n다음 상환: " + g.DaysToPayment + "일 뒤 <b>" + next.ToString("N0") + "G</b> (아침에 자동 출금)"
                + "\n<size=85%><color=#566458>연체 " + s.missedPayments + "/" + b.maxMissedPayments + "회. " + b.maxMissedPayments + "번 연체하면 목장이 압류됩니다. 미리 갚은 돈은 다음 상환에서 빠져요.</color></size>";
            int ten = Math.Min(10000, s.debt);
            dialogue.Show(name, body,
                new ModalAction("다음 상환금 미리 갚기 (" + next.ToString("N0") + "G)", () => { if (g.RepayDebt(next) > 0) audioManager.PlaySfx(SfxId.Coin); }, ModalStyle.Primary, next > 0 && s.gold >= next),
                new ModalAction(ten.ToString("N0") + "G 갚기", () => { if (g.RepayDebt(ten) > 0) audioManager.PlaySfx(SfxId.Coin); }, ModalStyle.Normal, s.gold >= ten),
                new ModalAction("전부 갚기 (" + s.debt.ToString("N0") + "G)", () => { if (g.RepayDebt(s.debt) > 0) audioManager.PlaySfx(SfxId.Fanfare); }, ModalStyle.Normal, s.gold >= s.debt),
                new ModalAction("나가기", null));
        }

        /// <summary>마구간지기: 목장 시설 3종 업그레이드 (돈 쓸 곳)</summary>
        private void AskFacility(string name)
        {
            var g = Game;
            var opts = new List<ModalAction>();
            for (int i = 0; i < RanchConst.FacilityCount; i++)
            {
                var f = (FacilityId)i;
                int lv = g.FacilityLevel(f), price = g.FacilityPrice(f);
                string label = RanchGame.FacilityName(f) + " " + lv + "단계" + (price < 0 ? " (최고)" : " > " + (lv + 1) + "단계 " + price.ToString("N0") + "G");
                opts.Add(new ModalAction(label, () => { if (g.UpgradeFacility(f)) { audioManager.PlaySfx(SfxId.Fanfare); AskFacility(name); } }, ModalStyle.Normal, price >= 0 && g.State.gold >= price));
            }
            opts.Add(new ModalAction("나중에", null));
            var b = new StringBuilder("어디를 손볼까? 공사는 바로 끝나네.\n<size=85%><color=#566458>");
            for (int i = 0; i < RanchConst.FacilityCount; i++)
                b.Append(RanchGame.FacilityName((FacilityId)i)).Append(": 단계마다 ").Append(g.FacilityEffect((FacilityId)i)).Append(i < RanchConst.FacilityCount - 1 ? "  /  " : "");
            b.Append("</color></size>");
            dialogue.Show(name, b.ToString(), opts.ToArray());
        }

        /// <summary>책장·볏짚·연못: 살펴보면 대부분 아무것도 없고, 정해진 한 곳에만 숨겨진 것이 있다</summary>
        private void AskSearch(Interactable it)
        {
            var lines = it.Lines;
            string intro = lines != null && lines.Length > 0 ? lines[0] : "무언가 있을 것 같다.";
            dialogue.Show("", intro, new ModalAction("살펴보기", () =>
            {
                string found = it.Param >= 0 ? Game.FindSecret((SecretId)it.Param) : null;
                if (found != null) { audioManager.PlaySfx(SfxId.Fanfare); dialogue.Show("", found); toast.Show("숨겨진 것을 찾았다! (" + Game.SecretsFound + "/" + RanchConst.SecretCount + ")", true); return; }
                string none = lines != null && lines.Length > 1 ? lines[UnityEngine.Random.Range(1, lines.Length)] : "아무것도 없다.";
                if (it.Param >= 0) none = "여긴 이미 다 뒤졌다. 더는 아무것도 없다.";
                dialogue.Show("", none);
            }, ModalStyle.Primary), new ModalAction("그만두기", null));
        }

        /// <summary>쓰레기통: 연타로 뒤지기 / 들고 있는 쓰레기 버리기</summary>
        private void AskTrashBin(Interactable it)
        {
            var g = Game;
            var b = g.Data.balance;
            int bin = it.Param;
            int left = g.BinUsesLeft(bin);
            string body = "냄새나는 쓰레기통이다. 뒤져 보면 뭐가 나올지도…"
                + (g.KnowsBinSecret ? "\n<color=#B8860B>수첩 메모: 이 통에선 가끔 <b>" + g.BinValue(bin).ToString("N0") + "G</b>가 나온다.</color>" : "")
                + "\n<size=80%><color=#566458>오늘 남은 횟수 " + left + "/" + b.trashSearchPerDay + " · 들고 있는 쓰레기 " + g.State.trash + "/" + b.trashCarryMax + " (쓰레기는 쓰레기통에만 버릴 수 있어요)</color></size>";
            dialogue.Show("", body,
                new ModalAction("뒤지기 (E 연타)", () => search.Open(bin), ModalStyle.Primary, left > 0 && !g.TrashFull),
                new ModalAction("쓰레기 버리기 (" + g.State.trash + "개)", () => { g.DisposeTrash(); audioManager.PlaySfx(SfxId.Trash); }, ModalStyle.Normal, g.State.trash > 0),
                new ModalAction("그만두기", null));
        }

        // 벤치에 앉아 있으면 들리는 수다 (힌트 포함)
        private static readonly string[] Gossip =
        {
            "「넝마주이 할아버지네 책장엔 비밀 수첩이 있대. 쓰레기통 얘기가 적혀 있다나.」",
            "「마구간 구석 볏짚 더미에서 뭔가 반짝였다는 사람이 있어.」",
            "「북쪽 연못 오른쪽 아래 물속에 옛날 금화 주머니가 가라앉아 있다던데.」",
            "「너네 집 책장 두꺼운 책 사이… 아니다, 내가 무슨 말을.」",
            "「시장 황금 귀리 먹은 말은 다르더라. 하루에 하나밖에 안 팔지만.」",
            "「쓰레기통 뒤질 땐 빨리빨리 뒤져야 해. 손이 느리면 하세월이야.」",
            "「커피 한 잔 마시면 하루 종일 발걸음이 가볍다니까.」",
            "「오늘 단거리 1번 말 컨디션 좋아 보이던데… 아닌가?」",
            "「도박장 교환소 직원, 적중 마권 바꿔줄 때만 웃더라.」",
        };

        public void BenchGossip()
        {
            toast.Show(Gossip[UnityEngine.Random.Range(0, Gossip.Length)], false);
        }

        private void AskWatchRace(string staff)
        {
            var g = Game;
            var opts = new List<ModalAction>();
            for (int i = 0; i < g.Data.races.Length; i++)
            {
                int r = i;
                var rs = g.Race(i);
                string label = g.Data.races[i].displayName + (rs.grandPrix ? " G1" : "") + (rs.done ? " (종료)" : " 관람");
                opts.Add(new ModalAction(label, () => WatchRace(r), ModalStyle.Primary, !rs.done));
            }
            opts.Add(new ModalAction("나중에", null));
            dialogue.Show(staff, "오늘의 경주입니다. 발주하면 마권 판매가 마감됩니다.\n어느 경주를 관람하시겠어요?", opts.ToArray());
        }

        private void WatchRace(int raceIndex)
        {
            var g = Game;
            int injury = g.EntryInjury(raceIndex);
            if (injury == 0) { g.RunRace(raceIndex, true); return; }
            var h = g.FindHorse(g.Race(raceIndex).entryId);
            var b = g.Data.balance;
            int pct = Mathf.RoundToInt((injury == 2 ? b.forcedRaceDeathMajor : b.forcedRaceDeathMinor) * 100f);
            int medicine = g.State.items[(int)ItemType.Medicine];
            int id = h.id;
            modal.Show("부상마 출전", "「" + h.name + "」은(는) " + (injury == 2 ? "중상" : "경상") + " 상태입니다.\n그대로 달리면 <color=#B3342A>" + pct + "%</color> 확률로 경주 중 쓰러져 죽습니다.",
                new ModalAction("치료약 사용 후 출전 (" + medicine + ")", () => { if (g.UseItem(id, ItemType.Medicine)) g.RunRace(raceIndex, true); }, ModalStyle.Primary, medicine > 0),
                new ModalAction("강행", () => g.RunRace(raceIndex, true), ModalStyle.Danger),
                new ModalAction("취소", null));
        }

        private void AskTip(string name)
        {
            var g = Game;
            int price = g.Data.balance.tipPrice;
            var opts = new List<ModalAction>();
            for (int i = 0; i < g.Data.races.Length; i++)
            {
                int r = i;
                opts.Add(new ModalAction(g.Data.races[i].displayName + " 정보 (" + price + "G)", () =>
                {
                    string tip = g.BuyTip(r);
                    if (tip != null) dialogue.Show(name, tip);
                }, ModalStyle.Normal, !g.Race(i).done && g.State.gold >= price));
            }
            opts.Add(new ModalAction("필요 없어", null));
            dialogue.Show(name, "쉿, 좋은 정보 있어. 한 경주에 " + price + "G. 살 거야?", opts.ToArray());
        }

        // ===== 이동 / 잠자기 =====
        public void Warp(Vector2Int cell, Vector2Int facing)
        {
            audioManager.PlaySfx(SfxId.Door);
            StartFade(() => { player.WarpTo(cell, facing); cameraFollow.Snap(); AfterWarpRide(); });
        }

        private void Sleep(Vector2Int wakeCell) { Sleep(wakeCell, -1); }

        /// <summary>잠자기 → 다음 날. wakeMinute가 있으면 그 시각에 일어남 (밤샘 후 늦잠)</summary>
        private void Sleep(Vector2Int wakeCell, int wakeMinute)
        {
            if (Game.RidingHorse != null) GetOff(false);
            StartFade(() =>
            {
                Game.NextDay();
                if (wakeMinute > 0) Game.SetClock(wakeMinute);
                player.WarpTo(wakeCell, Vector2Int.down);
                cameraFollow.Snap();
                toast.Show(Game.State.day + "일차 아침이 밝았습니다", true);
                while (autoRaceNews.Count > 0) toast.Show(autoRaceNews.Dequeue(), false);
            });
        }

        private void StartFade(Action middle)
        {
            fadeMiddle = middle;
            fadeMidDone = false;
            fadeT = 0f;
            fade.gameObject.SetActive(true);
            SetPrompt(null);
        }

        private void UpdateFade()
        {
            if (fadeT < 0f) return;
            fadeT += Time.unscaledDeltaTime / 0.25f; // 0→1 어두워짐, 1→2 밝아짐
            if (fadeT >= 1f && !fadeMidDone)
            {
                fadeMidDone = true;
                var m = fadeMiddle; fadeMiddle = null;
                if (m != null) m();
            }
            float a = fadeT < 1f ? fadeT : 2f - fadeT;
            fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(a));
            if (fadeT >= 2f) { fadeT = -1f; fade.gameObject.SetActive(false); }
        }

        // ===== 게임 이벤트 =====
        private void OnToast(string message) { toast.Show(message, false); }
        private void OnAchievement(AchievementDefinition a) { audioManager.PlaySfx(SfxId.Fanfare); toast.Show("업적 달성 · " + a.displayName + "  (J)", true); }

        private void OnTrainingEvent(int horseId, TrainingEventDefinition e)
        {
            var h = Game.FindHorse(horseId);
            if (h == null) return;
            dialogue.Show("훈련장 소식 · " + h.name, e.text,
                new ModalAction(e.riskyChoice + " (도박 50%)", () => dialogue.Show(h.name, Game.ResolveTrainingEvent(h.id, true)), ModalStyle.Primary),
                new ModalAction(e.safeChoice + " (안전)", () => dialogue.Show(h.name, Game.ResolveTrainingEvent(h.id, false))));
        }

        private void OnRaceFinished(RaceReplay replay)
        {
            CloseWindow();
            SetPrompt(null);
            cameraFollow.SetTarget(race.Focus, true);   // 카메라를 트랙 선두로
            audioManager.PlaySfx(SfxId.Gate);
            audioManager.PlayBgm(BgmId.Race);
            audioManager.PlayAmbience(AmbienceId.StadiumCrowd, 1.2f);   // 관중 함성 크게
            audioManager.PlaySfx(SfxId.Cheer);
            racing = true;
            ambientTimer = 1.5f;
            race.Play(replay, runner.Database, () =>
            {
                racing = false;
                cameraFollow.SetTarget(player.transform, true);
                audioManager.PlaySfx(SfxId.Cheer);
                audioManager.PlaySfx(SfxId.Fanfare);
                RefreshAreaAudio();
                string title = Game.Data.races[replay.raceIndex].displayName + (replay.grandPrix ? " 대상경주(G1)" : "") + " 결과";
                modal.Show(title, ResultBody(replay.result));
            });
        }

        private void OnRaceAutoRun(int raceIndex, RaceResultData r)
        {
            if (r.rows.Count == 0) return;
            var w = r.rows[0];
            autoRaceNews.Enqueue(Game.Data.races[raceIndex].displayName + " 결과: 1착 " + w.lane + "번 " + w.name + " (게시판 확인)");
            for (int i = 0; i < r.notes.Count; i++)
                if (r.notes[i].tone != NoticeTone.Normal || r.notes[i].text.StartsWith("마권")) autoRaceNews.Enqueue(r.notes[i].text);
        }

        private void OnLotRevealed(HorseData h, List<NoticeData> notes)
        {
            var b = new StringBuilder(256);
            b.Append(h.male ? "수말" : "암말").Append(" · ").Append(runner.Database.CoatName(h.coat))
             .Append(" · 스탯 합계 <b>").Append(h.Total.ToString("0")).Append("</b>\n\n");
            var data = Game.Data;
            for (int i = 0; i < RanchConst.StatCount; i++)
            {
                bool hi = h.stats[i] >= 10f;
                b.Append(hi ? "<color=#B8860B><b>" : "").Append(data.stats[i].displayName).Append("<pos=30%>")
                 .Append(h.stats[i].ToString("0")).Append(hi ? "</b></color>" : "").Append('\n');
            }
            for (int i = 0; i < notes.Count; i++)
                b.Append("\n<color=").Append(UITheme.Tone(notes[i].tone)).Append(">").Append(notes[i].text).Append("</color>");
            b.Append("\n\n<size=85%><color=#566458>말은 목장 울타리로 보냈습니다. 마구간지기에게 말을 걸어 관리하세요.</color></size>");
            modal.Show("감정 결과 · " + h.name, b.ToString(), new ModalAction("확인", null, ModalStyle.Primary));
        }

        private void OnFoalBorn(HorseData f)
        {
            var b = new StringBuilder(256);
            b.Append(f.male ? "수말" : "암말").Append(" · 합계 <b>").Append(f.Total.ToString("0.0")).Append("</b>\n\n");
            for (int i = 0; i < RanchConst.StatCount; i++)
                b.Append(Game.Data.stats[i].displayName).Append("<pos=30%>").Append(f.stats[i].ToString("0.0")).Append('\n');
            modal.Show("망아지 탄생 · " + f.name, b.ToString());
        }

        /// <summary>그랑프리 우승: 경주 결과 창을 닫으면 엔딩 시작</summary>
        private void OnGameCleared() { pendingEnding = true; }

        private void OnStoryNotice(string head, string body)
        {
            if (head.StartsWith("연체")) audioManager.PlaySfx(SfxId.Warning);
            modal.Show(head, body, new ModalAction("확인", null, ModalStyle.Primary));
        }

        /// <summary>게임 오버: 세이브 삭제 → 패배 연출 → 처음부터 / 타이틀</summary>
        private void OnGameOver(GameOverReason reason)
        {
            runner.ClearSave();
            modal.CloseAll();
            CloseWindow();
            dialogue.Close(null);
            guide.Clear();
            pendingEnding = false;
            racing = false;
            gameOverShown = true;
            HideBubbles();
            audioManager.PlaySfx(SfxId.Warning);
            audioManager.PlayBgm(BgmId.GameOver);
            audioManager.PlayAmbience(AmbienceId.None, 0f);
            cutscene.Play(StoryText.GameOver(reason, Game), () =>
                modal.Show("GAME OVER", reason == GameOverReason.Foreclosure
                        ? "빚을 " + Game.Data.balance.maxMissedPayments + "번 연체해 목장을 잃었습니다.\n<size=85%><color=#566458>상환일 전에 광장 은행원에게 미리 갚거나, 경주 상금 · 마권 · 말 판매로 돈을 모아 두세요.</color></size>"
                        : "말도, 새 말을 살 돈도 남지 않았습니다.\n<size=85%><color=#566458>마지막 말은 팔기 전에 한 번 더 생각해 보세요.</color></size>",
                    new ModalAction("처음부터 다시", () => BeginPlay(true), ModalStyle.Primary),
                    new ModalAction("타이틀로", () => title.Show())));
        }

        // ===== 엔딩: 아버지의 묘에 트로피 → 다시 트랙을 달리는 장면 → 크레딧 =====
        private void PlayEnding()
        {
            inEnding = true;
            CloseWindow();
            guide.Clear();
            SetPrompt(null);
            HideBubbles();
            audioManager.PlayBgm(BgmId.Ending);
            audioManager.PlayAmbience(AmbienceId.None, 0f);
            StartFade(() =>
            {
                player.Mover.Stand();
                player.WarpTo(memorial.StandCell, Vector2Int.up);
                cameraFollow.Snap();
                memorial.ShowTrophy(false);
                memorial.ShowMom(true);
                GraveLine(StoryText.EndingGrave(Game), 0);
            });
        }

        /// <summary>(화자, 대사) 목록을 차례로 보여주고 done 실행</summary>
        private void StoryLines(string[][] lines, int i, Action done)
        {
            if (i >= lines.Length) { if (done != null) done(); return; }
            dialogue.ShowThen(lines[i][0], lines[i][1], () => StoryLines(lines, i + 1, done));
        }

        private void GraveLine(string[][] lines, int i)
        {
            if (i >= lines.Length) { StartFade(EndingRide); return; }
            if (lines[i][1] == StoryText.TrophyBeat) { memorial.ShowTrophy(true); audioManager.PlaySfx(SfxId.Fanfare); }
            dialogue.ShowThen(lines[i][0], lines[i][1], () => GraveLine(lines, i + 1));
        }

        private void EndingRide()
        {
            memorial.ShowMom(false);
            cameraFollow.SetTarget(race.Focus, true);
            audioManager.PlayBgm(BgmId.Race);
            audioManager.PlayAmbience(AmbienceId.StadiumCrowd, 1.2f);
            audioManager.PlaySfx(SfxId.Gate);
            audioManager.PlaySfx(SfxId.Cheer);
            racing = true;
            cutscene.Caption(StoryText.EndingRideCaption, 5f);
            race.Play(EndingReplay(), runner.Database, () =>
                StartFade(() =>
                {
                    racing = false;
                    cameraFollow.SetTarget(player.transform, true);
                    player.WarpTo(spawnCell, Vector2Int.down);
                    cameraFollow.Snap();
                    audioManager.PlayBgm(BgmId.Ending);
                    audioManager.PlayAmbience(AmbienceId.None, 0f);
                    cutscene.Play(StoryText.EndingCredits(Game), () =>
                    {
                        inEnding = false;
                        Game.MarkEndingSeen();
                        RefreshAreaAudio();
                        RefreshHud();
                        modal.Show("엔딩", "아버지의 꿈을 이뤘습니다.\n엔딩 후에도 목장을 계속 키울 수 있어요. 트로피는 언덕 위 아버지의 묘에 놓여 있습니다.",
                            new ModalAction("계속 플레이", null, ModalStyle.Primary),
                            new ModalAction("타이틀로", () => title.Show()));
                    });
                }));
        }

        /// <summary>엔딩용 질주: 우승마가 선두로 달리고 라이벌과 다른 말들이 뒤따른다 (실제 경주 아님, 약 15초)</summary>
        private RaceReplay EndingReplay()
        {
            var g = Game;
            float dist = g.Data.races[g.StoryRace].distance;
            var r = new RaceReplay { raceIndex = g.StoryRace, grandPrix = true, distance = dist, dt = 0.1f, frameCount = 150, mine = 3, result = new RaceResultData() };
            r.frames = new float[r.frameCount * RanchConst.RunnerCount];
            string[] others = { "들바람", RanchGame.RivalHorse, "은빛별", "", "바다새", "강철발", "꽃구름", "모래폭풍" };
            var winner = g.State.horses.Find(h => h.name == g.State.storyWinner);
            int coats = Mathf.Max(1, g.Data.coats.Length);
            float lead = dist / (r.frameCount * 0.85f);      // 우승마는 85% 지점에서 결승
            for (int lane = 0; lane < RanchConst.RunnerCount; lane++)
            {
                r.names[lane] = lane == r.mine ? (g.State.storyWinner ?? "우리 말") : others[lane];
                r.coats[lane] = lane == r.mine && winner != null ? winner.coat : lane % coats;
                float speed = lane == r.mine ? lead : lead * (0.86f + (lane * 37 % 5) * 0.02f);
                for (int f = 0; f < r.frameCount; f++)
                {
                    float t = f / (float)r.frameCount;
                    float ease = t < 0.1f ? t * t * 5f : t - 0.05f;   // 출발 가속
                    r.frames[f * RanchConst.RunnerCount + lane] = Mathf.Min(dist + 30f, ease * r.frameCount * speed);
                }
            }
            return r;
        }

        public void RestartGame()
        {
            inEnding = false;
            pendingEnding = false;
            modal.CloseAll();
            CloseWindow();
            dialogue.Close(null);
            runner.RestartGame();
            mount.Dismount();
            player.WarpTo(spawnCell, Vector2Int.down);
            cameraFollow.Snap();
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
                 .Append("<pos=12%><color=#").Append(UITheme.SilkHex[row.lane - 1]).Append(">").Append(row.lane).Append("번</color>  ")
                 .Append(row.name).Append(row.mine ? " (내 말)" : "")
                 .Append("<pos=78%>").Append(row.time);
                if (row.mine) b.Append("</b>");
                b.Append('\n');
            }
            if (r.notes.Count > 0) b.Append('\n');
            for (int i = 0; i < r.notes.Count; i++)
                b.Append("<color=").Append(UITheme.Tone(r.notes[i].tone)).Append(">- ").Append(r.notes[i].text).Append("</color>\n");
            return b.ToString();
        }

        /// <summary>마권 한 줄 설명</summary>
        public static string TicketLabel(BetTicket t, GameData data)
        {
            return t.day + "일차 " + data.races[t.raceIndex].displayName + (t.grandPrix ? " G1" : "") + " · <b>" + ParimutuelMarket.DisplayName(t.type) + " " + t.key + "</b> · " + t.amount.ToString("N0") + "G";
        }

        public static string TicketStatusLabel(BetTicket t)
        {
            switch (t.status)
            {
                case TicketStatus.Won: return "<color=#2A6A3D><b>적중 " + t.odds.ToString("0.0") + "배 > " + t.payout.ToString("N0") + "G</b></color>";
                case TicketStatus.Lost: return "<color=#B3342A>꽝</color>";
                default: return "<color=#566458>경주 대기</color>";
            }
        }
    }
}
