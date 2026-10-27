using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    public enum WindowId { Stable = 0, Shop = 1, Breed = 2, Achievements = 3, Inventory = 4, Bet = 5, Exchange = 6, Entry = 7, Bulletin = 8, Map = 9, Market = 10 }

    /// <summary>
    /// 마을 탐험형 UI 총괄: HUD, 상호작용 안내, 대화창, 기능 창, 경주 관람, 화면 전환(페이드), 게임 이벤트 연결.
    /// 창이나 대화가 열려 있는 동안(IsBusy) 플레이어는 움직이지 않는다.
    /// </summary>
    public class GameUI : MonoBehaviour
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

        public ModalView Modal { get { return modal; } }
        public bool IsFading { get { return fadeT >= 0f; } }
        public bool IsBusy
        {
            get { return openWindow >= 0 || dialogue.IsOpen || modal.IsOpen || race.IsPlaying || IsFading || search.IsOpen || title.IsOpen || settings.IsOpen; }
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
            g.Bankrupt += OnBankrupt;
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
            g.Bankrupt -= OnBankrupt;
        }

        private void Start()
        {
            player.WarpTo(spawnCell, Vector2Int.down);
            cameraFollow.Snap();
            RefreshHud();
            title.Show();   // 타이틀 화면부터 (이어하기 / 새 게임 / 설정 / 종료)
        }

        /// <summary>타이틀에서 시작. fresh면 새 게임 + 안내, 아니면 저장된 곳부터</summary>
        public void BeginPlay(bool fresh)
        {
            runner.MarkPlayed();
            audioManager.PlayBgm(BgmId.Town);
            if (fresh)
            {
                RestartGame();
                dialogue.Show("안내", "질주마 목장에 오신 것을 환영합니다! 소지금 " + Game.State.gold.ToString("N0") + "G로 시작합니다.\n방향키/WASD 이동 · E 상호작용 · Shift 달리기 · Tab 인벤토리 · J 업적 · M 지도 · Esc 메뉴\n경주는 매일 열립니다. 도박장에서 마권을 사고 경기장에서 관람하세요.");
            }
            else
            {
                RefreshHud();
                toast.Show(Game.State.day + "일차부터 이어합니다", true);
            }
        }

        /// <summary>Esc (아무 창도 없을 때): 일시정지 메뉴</summary>
        private void OpenPauseMenu()
        {
            SetPrompt(null);
            audioManager.PlaySfx(SfxId.Click);
            modal.Show("메뉴", Game.State.day + "일차 · " + Game.State.gold.ToString("N0") + "G\n<size=85%><color=#566458>진행 상황은 자동 저장됩니다.</color></size>",
                new ModalAction("계속하기", null, ModalStyle.Primary),
                new ModalAction("설정", () => settings.Open(null)),
                new ModalAction("저장하고 타이틀로", () => { runner.Save(); player.Mover.Stand(); title.Show(); }));
        }

        public void PlaySfx(SfxId id) { audioManager.PlaySfx(id); }

        private void Update()
        {
            UpdateFade();
            if (modal.IsOpen || race.IsPlaying || IsFading || dialogue.IsOpen || search.IsOpen || title.IsOpen || settings.IsOpen) { busyFrame = Time.frameCount; return; }

            if (openWindow >= 0)
            {
                if (GameInput.Cancel) CloseWindow();
                else if (GameInput.Inventory && openWindow == (int)WindowId.Inventory) CloseWindow();
                else if (GameInput.Achievements && openWindow == (int)WindowId.Achievements) CloseWindow();
                else if (GameInput.Map && openWindow == (int)WindowId.Map) CloseWindow();
                return;
            }
            // 같은 Esc로 다른 창을 닫은 직후에는 메뉴를 열지 않음
            if (GameInput.Cancel && !player.Mover.IsSeated && busyFrame < Time.frameCount - 1) OpenPauseMenu();
            else if (GameInput.Inventory) OpenWindow(WindowId.Inventory, null);
            else if (GameInput.Achievements) OpenWindow(WindowId.Achievements, null);
            else if (GameInput.Map) OpenWindow(WindowId.Map, null);
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
                + (g.IsLucky ? "   <color=#7FD38A>★ 행운</color>" : "")
                + (g.IsSearchBoosted ? "  <color=#E8C040>김밥</color>" : "")
                + (g.HasCoffee ? "  <color=#C8A060>커피</color>" : "")
                + (s.trash > 0 ? "  <color=#8B927F>쓰레기 " + s.trash + "</color>" : "");
            int d = g.DaysToGrandPrix;
            var sb = new StringBuilder(64);
            sb.Append(d == 0 ? "<color=#F2BF37>오늘 대상경주(G1)!</color>" : "G1까지 " + d + "일").Append("   목표 ");
            for (int i = 0; i < s.grandPrixWon.Length && i < g.Data.races.Length; i++)
                sb.Append(g.Data.races[i].displayName).Append(s.grandPrixWon[i] ? "●" : "○").Append(' ');
            goalText.text = sb.ToString();
        }

        public void ShowLocation(string name) { locationText.text = name; }

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
            audioManager.PlaySfx(SfxId.Click);
            SetPrompt(null);
        }

        public void CloseWindow()
        {
            if (openWindow < 0) return;
            windows[openWindow].Close();
            openWindow = -1;
        }

        // ===== 상호작용 =====
        public void Interact(Interactable it, PlayerController p)
        {
            var npc = it.GetComponent<NpcController>();
            if (npc != null) npc.TalkTo(p.Mover.Cell, 4f);
            if (it.Kind != InteractKind.Door) audioManager.PlaySfx(SfxId.Click);
            var g = Game;
            switch (it.Kind)
            {
                case InteractKind.Door:
                    Warp(it.WarpCell, it.WarpFacing);
                    break;
                case InteractKind.Sleep:
                    dialogue.Show(it.DisplayName, "푹신한 침대다. 자고 내일을 맞이할까요?\n<size=80%><color=#566458>관람하지 않은 오늘 경주는 자동으로 진행되고, 마권도 정산됩니다.</color></size>",
                        new ModalAction("잠자기", () => Sleep(it.WarpCell)), new ModalAction("그만두기", null));
                    break;
                case InteractKind.Stable:
                    dialogue.Show(it.DisplayName, it.RandomLine(), new ModalAction("말 관리하기", () => OpenWindow(WindowId.Stable, "마구간")), new ModalAction("나중에", null));
                    break;
                case InteractKind.Training:
                    dialogue.Show(it.DisplayName, it.RandomLine(), new ModalAction("훈련 시키기", () => OpenWindow(WindowId.Stable, "훈련장")), new ModalAction("나중에", null));
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
                    if (g.IsLucky) dialogue.Show(it.DisplayName, "물속에서 내가 던진 동전이 반짝인다. 오늘은 운이 좋을 것 같다.");
                    else dialogue.Show(it.DisplayName, "맑은 물속에 동전이 가득하다. 소원을 빌며 동전을 던질까요?\n<size=80%><color=#566458>" + price + "G · 오늘 하루 행운 (경주마다 3분의 1 확률로 내 마권 하나가 적중)</color></size>",
                        new ModalAction("동전 던지기 (" + price + "G)", () => { if (g.ThrowCoin()) audioManager.PlaySfx(SfxId.Coin); if (g.IsLucky) dialogue.Show(it.DisplayName, "퐁당! 동전이 반짝이며 가라앉았다.\n어쩐지 오늘은 운이 좋을 것 같다!"); }, ModalStyle.Primary, g.State.gold >= price),
                        new ModalAction("그만두기", null));
                    break;
                case InteractKind.GambleMaster:
                    dialogue.Show(it.DisplayName, "허허, 여기까지 찾아오다니. 내 예언 한번 들어볼 텐가? 하루에 한 번이네.",
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
                    dialogue.Show(it.DisplayName, it.RandomLine());
                    break;
            }
        }

        /// <summary>책장·볏짚·연못: 살펴보면 대부분 아무것도 없고, 정해진 한 곳에만 숨겨진 것이 있다</summary>
        private void AskSearch(Interactable it)
        {
            var lines = it.Lines;
            string intro = lines != null && lines.Length > 0 ? lines[0] : "무언가 있을 것 같다.";
            dialogue.Show(it.DisplayName, intro, new ModalAction("살펴보기", () =>
            {
                string found = it.Param >= 0 ? Game.FindSecret((SecretId)it.Param) : null;
                if (found != null) { audioManager.PlaySfx(SfxId.Fanfare); dialogue.Show(it.DisplayName, found); toast.Show("숨겨진 것을 찾았다! (" + Game.SecretsFound + "/" + RanchConst.SecretCount + ")", true); return; }
                string none = lines != null && lines.Length > 1 ? lines[UnityEngine.Random.Range(1, lines.Length)] : "아무것도 없다.";
                if (it.Param >= 0) none = "여긴 이미 다 뒤졌다. 더는 아무것도 없다.";
                dialogue.Show(it.DisplayName, none);
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
            dialogue.Show(it.DisplayName, body,
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
            StartFade(() => { player.WarpTo(cell, facing); cameraFollow.Snap(); });
        }

        private void Sleep(Vector2Int wakeCell)
        {
            StartFade(() =>
            {
                Game.NextDay();
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
            race.Play(replay, runner.Database, () =>
            {
                cameraFollow.SetTarget(player.transform, true);
                audioManager.PlayBgm(BgmId.Town);
                audioManager.PlaySfx(SfxId.Fanfare);
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
            CloseWindow();
            dialogue.Close(null);
            runner.RestartGame();
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

        /// <summary>마권 한 줄 설명</summary>
        public static string TicketLabel(BetTicket t, GameData data)
        {
            return t.day + "일차 " + data.races[t.raceIndex].displayName + (t.grandPrix ? " G1" : "") + " · <b>" + ParimutuelMarket.DisplayName(t.type) + " " + t.key + "</b> · " + t.amount.ToString("N0") + "G";
        }

        public static string TicketStatusLabel(BetTicket t)
        {
            switch (t.status)
            {
                case TicketStatus.Won: return "<color=#2A6A3D><b>적중 " + t.odds.ToString("0.0") + "배 → " + t.payout.ToString("N0") + "G</b></color>";
                case TicketStatus.Lost: return "<color=#B3342A>꽝</color>";
                default: return "<color=#566458>경주 대기</color>";
            }
        }
    }
}
