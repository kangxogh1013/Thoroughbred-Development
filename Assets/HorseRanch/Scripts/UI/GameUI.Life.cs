using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>
    /// GameUI 확장: 시간대(시계 · 조명 · 영업 종료 · 기절), 주민 의뢰 게시판, 마구간 말 교감 · 말 타기, 기록실, 발소리.
    /// </summary>
    public partial class GameUI
    {
        [Header("시간대")]
        [Tooltip("화면 전체를 덮는 색 (노을 · 밤). HUD보다 아래에 그려야 한다")]
        [SerializeField] private Image nightOverlay;
        [SerializeField] private TMP_Text clockText;
        [Tooltip("가로등 자리만 밝은 밤 어둠 + 가로등 · 창문 불빛")]
        [SerializeField] private NightLighting lighting;
        [Tooltip("밤이 가장 깊을 때 어둠 (0~1, 클수록 어두움)")]
        [SerializeField] [Range(0f, 1f)] private float maxDarkness = 0.86f;

        [Header("도박장 말풍선 (여러 개 동시에)")]
        [SerializeField] private SpeechBubble[] extraBubbles;

        [Header("저녁 귀가")]
        [Tooltip("밤에도 바깥에 남는 주민 비율 (산책 · 야간 경비 등)")]
        [SerializeField] [Range(0f, 1f)] private float nightOwlRatio = 0.25f;

        [Header("말 타기")]
        [SerializeField] private PlayerMount mount;

        private int shownClock = -1;
        // 주민 하루 일정: 저녁 6시 반 ~ 9시 50분 사이 한 명씩 가까운 문으로 걸어 들어감
        private class NpcPlan
        {
            public NpcController npc;
            public WorldArea area;
            public bool outdoor, owl, home;
            public int leaveMinute;
            public int state;   // 0 밖에 있음, 1 집으로 가는 중, 2 들어감
        }
        private List<NpcPlan> npcPlans;
        private int plannedDay = -1;
        private float scheduleTimer;
        private static readonly HashSet<string> OutdoorAreas = new HashSet<string> { "목장 마을", "경기장", "깊은 숲" };
        private string overlayArea;
        private bool stepParity;

        // 말을 타고 다닐 수 있는 구역 (그 외 실내로 들어가면 말은 마구간으로 돌아감)
        private static readonly HashSet<string> RideAreas = new HashSet<string> { "목장 마을", "경기장", "깊은 숲", "마구간" };
        // 밤 8시 이후 문 닫는 곳
        private static bool ClosesAtNight(InteractKind k) { return k == InteractKind.Shop || k == InteractKind.Market || k == InteractKind.Bank || k == InteractKind.Breed; }

        // =====================================================================
        // 시간대
        // =====================================================================
        private void UpdateLife()
        {
            var g = Game;
            if (g == null) return;
            // 주민 일정 (저녁 귀가 · 다음 날 복귀): 0.25초마다
            scheduleTimer -= Time.deltaTime;
            if (scheduleTimer <= 0f && !inEnding) { scheduleTimer = 0.25f; UpdateNpcSchedule(); }

            bool hidden = title.IsOpen || cutscene.IsOpen || inEnding || gameOverShown;
            if (hidden) { SetOverlay(Color.clear); if (lighting != null) lighting.Apply(0f, false); shownClock = -1; return; }

            // 창 · 대화 · 경주 · 페이드 중엔 시간이 멈춘다 (벤치에 앉아 있으면 흐름)
            if (!IsBusy)
            {
                var ev = g.TickClock(Time.deltaTime);
                if (ev != ClockEvent.None) OnClockEvent(ev);
            }

            int m = g.ClockMinute;
            if (m / 10 != shownClock || overlayArea != currentArea)
            {
                shownClock = m / 10;
                overlayArea = currentArea;
                clockText.text = (g.ClockRunning ? "" : "<color=#8B927F>") + RanchGame.ClockLabel(m / 10 * 10) + (g.ClockRunning ? "" : " (멈춤)</color>")
                    + (g.IsNight ? "  <color=#8FA8E0>밤</color>" : g.IsEvening ? "  <color=#F0A050>저녁</color>" : "");
                var c = SkyTint(m);
                if (!OutdoorAreas.Contains(currentArea)) c.a *= 0.4f;
                SetOverlay(c);
                // 어둠 + 가로등: 저녁 6시에 불이 켜짐
                if (lighting != null) lighting.Apply(Darkness(m) * maxDarkness, m >= g.Data.balance.eveningMinute);
            }
        }

        private void SetOverlay(Color c)
        {
            if (nightOverlay == null) return;
            if (nightOverlay.color != c) nightOverlay.color = c;
            bool on = c.a > 0.001f;
            if (nightOverlay.enabled != on) nightOverlay.enabled = on;
        }

        /// <summary>화면 전체 색조: 새벽 푸르스름, 노을 주황 (밤 어둠은 NightLighting이 가로등 자리만 빼고 덮음)</summary>
        private static Color SkyTint(int minute)
        {
            var dawn = new Color(0.45f, 0.55f, 0.85f, 0.16f);
            var dusk = new Color(1f, 0.5f, 0.18f, 0.2f);
            var night = new Color(0.1f, 0.12f, 0.35f, 0.08f);
            if (minute < 420) return Color.Lerp(dawn, Color.clear, (minute - 360) / 60f);            // 06~07시
            if (minute < 1020) return Color.clear;                                                     // 낮
            if (minute < 1140) return Color.Lerp(Color.clear, dusk, (minute - 1020) / 120f);          // 17~19시 노을
            if (minute < 1260) return Color.Lerp(dusk, night, (minute - 1140) / 120f);                // 19~21시
            return night;
        }

        /// <summary>밤 깊이 0~1: 6시 해 짐 시작 → 8시 0.75 → 10시 이후 1, 새벽 6~7시 걷힘</summary>
        private static float Darkness(int minute)
        {
            if (minute < 420) return Mathf.Lerp(0.55f, 0f, (minute - 360) / 60f);
            if (minute < 1080) return 0f;
            if (minute < 1200) return Mathf.Lerp(0f, 0.75f, (minute - 1080) / 120f);
            return Mathf.Lerp(0.75f, 1f, Mathf.Clamp01((minute - 1200) / 120f));
        }

        private void OnClockEvent(ClockEvent ev)
        {
            var b = Game.Data.balance;
            switch (ev)
            {
                case ClockEvent.Evening:
                    toast.Show("해가 저문다. 저녁 " + (b.closeMinute / 60 - 12) + "시에 가게와 은행이 문을 닫아요.", false);
                    break;
                case ClockEvent.Closing:
                    toast.Show("밤이 되었다. 가게 · 시장 · 은행 · 교배소 영업 종료", true);
                    while (autoRaceNews.Count > 0) toast.Show(autoRaceNews.Dequeue(), false);
                    break;
                case ClockEvent.Bedtime:
                    toast.Show("밤 10시 - 주민들이 모두 잠들었다. 집으로!", true);
                    dialogue.Show("", "밤이 깊었다. 마을 사람들도 모두 잠자리에 들었다.\n이제 집에 가서 잠을 자야 한다.\n<size=80%><color=#566458>머리 위 화살표가 집 침대를 가리켜요. 새벽 " + (b.passOutMinute / 60 - 24) + "시까지 안 자면 길에서 쓰러집니다.</color></size>");
                    UpdateGuide();
                    break;
                case ClockEvent.Late:
                    audioManager.PlaySfx(SfxId.Warning, 0.6f);
                    toast.Show("자정이 넘었다. 새벽 " + (b.passOutMinute / 60 - 24) + "시 전에 집에 가서 자야 한다!", true);
                    break;
                case ClockEvent.PassOut:
                    dialogue.ShowThen("", "눈꺼풀이 무겁다… 그대로 길바닥에서 잠들어 버렸다.", PassOut);
                    break;
            }
        }

        /// <summary>새벽 2시: 쓰러져 다음 날 집에서 늦잠 (지갑이 조금 가벼워짐)</summary>
        private void PassOut()
        {
            int lost = Game.PassOutPenalty();
            var bed = FindTarget(InteractKind.Sleep, null);
            var wake = bed != null ? bed.WarpCell : spawnCell;
            Sleep(wake, Game.Data.balance.oversleepMinute);
            toast.Show("마을 사람이 집까지 데려다줬다." + (lost > 0 ? " 그 사이 " + lost.ToString("N0") + "G를 잃어버렸다." : ""), true);
        }

        // =====================================================================
        // 상호작용 (GameUI.Interact 맨 앞에서 호출, 처리했으면 true)
        // =====================================================================
        private bool LifeInteract(Interactable it, PlayerController p)
        {
            var g = Game;
            if (g.IsBedtime && BedtimeBlock(it)) return true;
            if (g.IsNight && ClosesAtNight(it.Kind))
            {
                dialogue.Show(it.DisplayName, "오늘 영업은 끝났어요. 내일 아침 6시에 다시 와 주세요.\n<size=80%><color=#566458>밤 8시부터 아침 6시까지 상점 · 시장 · 은행 · 교배소는 문을 닫습니다. 도박장은 밤에도 열려 있어요.</color></size>");
                return true;
            }
            switch (it.Kind)
            {
                case InteractKind.StallHorse: AskStallHorse(it.Param); return true;
                case InteractKind.RequestBoard: AskRequests(it.DisplayName); return true;
                case InteractKind.SlotMachine: OpenWindow(WindowId.Slot, "슬롯머신"); return true;
                case InteractKind.Records:
                    dialogue.Show("", "역대 우승마의 이름과 기록이 새겨진 트로피가 늘어서 있다.\n<size=80%><color=#566458>K 키로 어디서든 기록실을 열 수 있어요.</color></size>",
                        new ModalAction("기록 보기", () => OpenWindow(WindowId.Records, "기록실"), ModalStyle.Primary), new ModalAction("나가기", null));
                    return true;
                case InteractKind.Bench:
                    if (g.RidingHorse == null) return false;
                    toast.Show("말에서 내려야 앉을 수 있다 (R)", false);
                    return true;
            }
            return false;
        }

        // ===== 취침 시간 (밤 10시 ~) =====
        /// <summary>집 침대와 '마을 · 우리 집'으로 가는 문만 허용. 나머지는 잠김 / 아무것도 없음</summary>
        private bool BedtimeBlock(Interactable it)
        {
            switch (it.Kind)
            {
                case InteractKind.Sleep: return false;
                case InteractKind.Door:
                    var dest = map.AreaAt(it.WarpCell);
                    string dn = dest != null ? dest.AreaName : "";
                    if (dn == "우리 집" || dn == "목장 마을") return false;
                    audioManager.PlaySfx(SfxId.Door, 0.4f);
                    dialogue.Show("", "문이 잠겨 있다. 다들 잠든 모양이다.\n<size=80%><color=#566458>밤에는 우리 집에만 들어갈 수 있어요.</color></size>");
                    return true;
                case InteractKind.Thicket:
                    dialogue.Show("", "밤의 숲은 칠흑같이 어둡다. 내일 다시 오자.");
                    return true;
                default:
                    if (it.GetComponent<NpcController>() != null)   // 밤에 남아 있는 주민
                        dialogue.Show(it.DisplayName, OwlLines[UnityEngine.Random.Range(0, OwlLines.Length)]);
                    else dialogue.Show("", "아무것도 없다. 밤이 깊었다… 이제 집에 가서 자야 한다.");
                    return true;
            }
        }

        /// <summary>밤엔 길 안내 화살표가 집 침대를 가리킨다</summary>
        private Interactable BedtimeTarget()
        {
            var g = Game;
            if (g == null || !g.IsBedtime || g.InTutorial || allInteractables == null) return null;
            return FindTarget(InteractKind.Sleep, null);
        }

        // ===== 주민 하루 일정 =====
        private void BuildNpcPlans()
        {
            npcPlans = new List<NpcPlan>();
            int n = 0;
            for (int i = 0; i < allInteractables.Length; i++)
            {
                var it = allInteractables[i];
                var npc = it.GetComponent<NpcController>();
                if (npc == null) continue;
                var area = map.AreaAt(npc.Mover.StartCell);
                if (area == null) continue;
                var p = new NpcPlan { npc = npc, area = area, outdoor = OutdoorAreas.Contains(area.AreaName), home = area.AreaName == "우리 집" };
                // 바깥 주민 일부는 밤에도 남음 (번호 기준으로 고르게)
                if (p.outdoor) { p.owl = (n * 0.37f) % 1f < nightOwlRatio; n++; }
                npcPlans.Add(p);
            }
        }

        /// <summary>날이 바뀌면 모두 제자리로, 저녁엔 바깥 주민이 하나씩 문으로 걸어 들어가고, 밤 10시엔 실내 주민도 퇴근</summary>
        private void UpdateNpcSchedule()
        {
            var g = Game;
            if (g == null || allInteractables == null) return;
            if (npcPlans == null) BuildNpcPlans();
            var b = g.Data.balance;
            int minute = g.ClockMinute;
            if (plannedDay != g.State.day)
            {
                plannedDay = g.State.day;
                var rng = new System.Random(g.State.day * 7919);
                for (int i = 0; i < npcPlans.Count; i++)
                {
                    var p = npcPlans[i];
                    p.leaveMinute = b.eveningMinute + 30 + rng.Next(Mathf.Max(1, b.bedtimeMinute - b.eveningMinute - 40));
                    if (p.state != 0) RestoreNpc(p);
                }
            }
            var playerArea = map.AreaAt(player.Mover.Cell);
            for (int i = 0; i < npcPlans.Count; i++)
            {
                var p = npcPlans[i];
                if (p.home || p.state != 0) continue;
                if (p.outdoor)
                {
                    if (p.owl || minute < p.leaveMinute) continue;
                    // 플레이어가 보고 있으면 걸어서 들어가고, 안 보거나 한참 지났으면 바로 들어간 것으로
                    if (playerArea != p.area || minute - p.leaveMinute > 40) { HideNpc(p); continue; }
                    var path = PathToDoor(p.npc.Mover.Cell, p.area);
                    if (path == null) { HideNpc(p); continue; }
                    p.state = 1;
                    var plan = p;
                    p.npc.FollowRoute(path, () => HideNpc(plan));
                }
                else if (g.IsBedtime && playerArea != p.area) HideNpc(p);   // 가게 주인 등은 밤 10시 퇴근 (보는 앞에서는 안 사라짐)
            }
        }

        private void HideNpc(NpcPlan p)
        {
            p.state = 2;
            p.npc.CancelRoute();
            p.npc.gameObject.SetActive(false);
        }

        private void RestoreNpc(NpcPlan p)
        {
            p.state = 0;
            p.npc.CancelRoute();
            if (!p.npc.gameObject.activeSelf) p.npc.gameObject.SetActive(true);
            p.npc.Mover.Teleport(p.npc.Mover.StartCell, p.npc.Mover.StartFacing);
        }

        /// <summary>같은 구역의 가장 가까운 건물 문까지 경로 (BFS, 다른 사람은 무시)</summary>
        private List<Vector2Int> PathToDoor(Vector2Int from, WorldArea area)
        {
            var doors = new HashSet<Vector2Int>();
            for (int i = 0; i < allInteractables.Length; i++)
            {
                var d = allInteractables[i];
                if (d.Kind == InteractKind.Door && map.AreaAt(d.FirstCell) == area) doors.Add(d.FirstCell);
            }
            if (doors.Count == 0) return null;
            var prev = new Dictionary<Vector2Int, Vector2Int>();
            var q = new Queue<Vector2Int>();
            q.Enqueue(from); prev[from] = from;
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (doors.Contains(c))
                {
                    var path = new List<Vector2Int>();
                    for (var k = c; k != from; k = prev[k]) path.Add(k);
                    path.Reverse();
                    return path;
                }
                if (prev.Count > 5000) break;
                for (int i = 0; i < 4; i++)
                {
                    var nb = c + dirs[i];
                    if (prev.ContainsKey(nb) || !map.IsWalkable(nb) || map.AreaAt(nb) != area) continue;
                    prev[nb] = c;
                    q.Enqueue(nb);
                }
            }
            return null;
        }

        private static readonly string[] OwlLines =
        {
            "이 시간까지 안 자고 뭐 해? 얼른 들어가.", "밤 산책은 좋지. 별이 참 많네.", "야간 순찰 중이야. 수상한 사람은 없겠지?",
            "잠이 안 와서 나왔어. 내일 경주 생각에…", "늦었어. 가로등 밑으로 다녀, 어두워서 넘어진다.",
        };

        // ===== 마구간 말: 쓰다듬기 · 이름 바꾸기 · 타고 나가기 =====
        private static readonly string[] PetLines =
        {
            "(조심스레 목을 쓰다듬자 귀를 쫑긋 세운다.)",
            "(코를 킁킁거리며 손바닥에 얼굴을 비빈다.)",
            "(기분 좋은 듯 푸르르 콧김을 내뿜는다.)",
            "(갈기를 빗어 주자 눈을 지그시 감는다.)",
            "(당신을 보자마자 고개를 쭉 내민다. 완전히 단짝이다.)",
        };

        private void AskStallHorse(int stall)
        {
            var g = Game;
            var list = g.State.horses;
            if (stall < 0 || stall >= list.Count) { dialogue.Show("빈 칸", "아무도 없는 빈 칸이다. 볏짚 냄새가 난다."); return; }
            var h = list[stall];
            if (h.id == g.State.ridingId) { dialogue.Show("", "지금 타고 있는 말의 칸이다. 말에서 내리면(R) 알아서 돌아온다."); return; }
            var b = g.Data.balance;
            string why;
            bool canRide = g.CanRide(h, out why);
            string body = "<b>" + h.name + "</b>  교감 <b>" + h.bond + "/" + b.bondMax + "</b> (" + RanchGame.BondLabel(h.bond) + ")  -  컨디션 " + Mathf.RoundToInt(h.condition) + "  -  피로 " + Mathf.RoundToInt(h.fatigue)
                + "\n<size=80%><color=#566458>하루 한 번 쓰다듬으면 교감 +1, 컨디션 +" + b.petCondition.ToString("0") + ". 교감이 높을수록 컨디션이 잘 유지돼요."
                + (canRide ? " 타고 나가면 마을을 " + b.rideSpeedScale.ToString("0.0") + "배 빠르게 달립니다." : " " + why + ".") + "</color></size>";
            dialogue.Show("", body,
                new ModalAction(g.PettedToday(h) ? "쓰다듬기 (오늘 함)" : "쓰다듬기", () => PetHorse(h), ModalStyle.Primary, !g.PettedToday(h)),
                new ModalAction("이름 바꾸기", () => AskRename(h), ModalStyle.Normal, h.name != RanchGame.FatherHorse),
                new ModalAction("타고 나가기", () => GetOn(h), ModalStyle.Normal, canRide),
                new ModalAction("그만두기", null));
        }

        private void PetHorse(HorseData h)
        {
            if (!Game.PetHorse(h.id)) return;
            audioManager.PlaySfx(UnityEngine.Random.value < 0.5f ? SfxId.Neigh : SfxId.Snort, 0.7f);
            int i = Mathf.Clamp(h.bond * PetLines.Length / (Game.Data.balance.bondMax + 1), 0, PetLines.Length - 1);
            dialogue.Show("", h.name + " " + PetLines[i] + "\n교감 " + h.bond + " (" + RanchGame.BondLabel(h.bond) + ")");
        }

        private void AskRename(HorseData h)
        {
            var names = Game.NameSuggestions(3);
            var opts = new List<ModalAction>();
            for (int i = 0; i < names.Length; i++)
            {
                string n = names[i];
                opts.Add(new ModalAction("「" + n + "」", () => { if (Game.RenameHorse(h.id, n)) { audioManager.PlaySfx(SfxId.Coin); dialogue.Show("", "「" + n + "」(이)가 새 이름이 마음에 드는지 고개를 끄덕인다."); } }));
            }
            opts.Add(new ModalAction("그대로 두기", null));
            dialogue.Show("", "「" + h.name + "」의 새 이름을 골라 주세요. 마음에 드는 게 없으면 다시 물어보면 새 후보가 나와요.", opts.ToArray());
        }

        private void GetOn(HorseData h)
        {
            if (!Game.StartRiding(h.id)) return;
            audioManager.PlaySfx(SfxId.Neigh);
            toast.Show(h.name + "에 올라탔다! 마을에서 빠르게 달린다 (R 내리기)", true);
        }

        /// <summary>말에서 내리기. 말은 스스로 마구간 칸으로 돌아간다</summary>
        private void GetOff(bool notify)
        {
            var h = Game.RidingHorse;
            Game.StopRiding();
            if (notify && h != null) { audioManager.PlaySfx(SfxId.Snort, 0.6f); toast.Show("말에서 내렸다. " + h.name + "은(는) 알아서 마구간으로 돌아갔다.", false); }
        }

        /// <summary>상태(ridingId)와 화면 맞추기 (Changed마다)</summary>
        private void SyncMount()
        {
            if (mount == null) return;
            var h = Game.RidingHorse;
            if (h == null) { if (mount.IsRiding) mount.Dismount(); return; }
            if (!mount.IsRiding) mount.Mount(runner.Database.CoatColor(h.coat), Game.Data.balance.rideSpeedScale);
        }

        /// <summary>문으로 실내에 들어가면 말은 마구간으로 (마을 · 경기장 · 숲 · 마구간만 탈 수 있음)</summary>
        private void AfterWarpRide()
        {
            if (Game.RidingHorse == null) return;
            if (RideAreas.Contains(player.AreaName)) return;
            GetOff(false);
            toast.Show("말은 밖에 둘 수 없어 마구간으로 돌려보냈다.", false);
        }

        /// <summary>한 칸 걸을 때마다: 걸음 소리 / 말발굽 소리</summary>
        public void OnPlayerStep()
        {
            stepParity = !stepParity;
            if (mount != null && mount.IsRiding) audioManager.PlaySfx(SfxId.Hoof, stepParity ? 0.32f : 0.26f);
            else audioManager.PlaySfx(SfxId.Step, stepParity ? 0.22f : 0.17f);
        }

        // =====================================================================
        // 주민 의뢰 게시판
        // =====================================================================
        private void AskRequests(string name)
        {
            var g = Game;
            var b = g.Data.balance;
            // 1) 끝낸 의뢰는 바로 보고 → 보상
            var done = new StringBuilder();
            var ids = new List<int>();
            foreach (var r in g.ActiveRequests) if (g.CanComplete(r)) ids.Add(r.id);
            for (int i = 0; i < ids.Count; i++)
            {
                string msg = g.CompleteRequest(ids[i]);
                if (msg != null) done.Append(msg).Append('\n');
            }
            if (done.Length > 0)
            {
                audioManager.PlaySfx(SfxId.Coin);
                dialogue.ShowThen(name, done.ToString().TrimEnd(), () => AskRequests(name));
                return;
            }

            // 2) 맡은 의뢰 현황
            var body = new StringBuilder();
            int active = g.ActiveRequestCount, offers = 0;
            foreach (var r in g.Offers) offers++;
            if (active == 0) body.Append("주민들의 부탁이 붙어 있다. 들어주면 돈이나 물건으로 보답해 준다.\n");
            else
            {
                body.Append("<b>맡은 의뢰</b> ").Append(active).Append('/').Append(b.requestMaxActive).Append('\n');
                foreach (var r in g.ActiveRequests)
                {
                    int left = r.deadline - g.State.day;
                    body.Append("- ").Append(g.RequestTitle(r)).Append("  <color=#566458>").Append(g.RequestProgress(r))
                        .Append(left <= 0 ? "  오늘까지" : "  " + (left + 1) + "일 남음").Append("</color>\n");
                }
            }
            body.Append("<size=80%><color=#566458>조건을 채운 뒤 게시판을 보면 바로 보고돼요. 말 보여주기는 그 말을 타고 와야 합니다.</color></size>");
            dialogue.Show(name, body.ToString(),
                new ModalAction("새 의뢰 보기 (" + offers + ")", () => ShowOffers(name), ModalStyle.Primary, offers > 0),
                new ModalAction("맡은 의뢰 포기", () => AskAbandon(name), ModalStyle.Normal, active > 0),
                new ModalAction("나가기", null));
        }

        private void ShowOffers(string name)
        {
            var g = Game;
            bool full = g.ActiveRequestCount >= g.Data.balance.requestMaxActive;
            var opts = new List<ModalAction>();
            var body = new StringBuilder();
            foreach (var r in g.Offers)
            {
                var req = r;
                body.Append("<b>").Append(r.giver).Append("</b>: ").Append(g.RequestTitle(r)).Append("  <color=#2A6A3D>").Append(g.RequestRewardText(r)).Append("</color>\n");
                if (opts.Count < 3)
                    opts.Add(new ModalAction("수락: " + g.RequestTitle(r), () =>
                    {
                        if (!g.AcceptRequest(req.id)) return;
                        audioManager.PlaySfx(SfxId.Click);
                        dialogue.ShowThen(req.giver, req.story + "\n<size=80%><color=#566458>" + g.Data.balance.requestDays + "일 안에 부탁해요. 보상 " + g.RequestRewardText(req) + "</color></size>", () => AskRequests(name));
                    }, ModalStyle.Normal, !full));
            }
            if (full) body.Append("<size=80%><color=#E06050>이미 의뢰를 ").Append(g.Data.balance.requestMaxActive).Append("개 맡고 있어요. 끝내거나 포기해야 새로 받을 수 있습니다.</color></size>");
            opts.Add(new ModalAction("돌아가기", () => AskRequests(name)));
            dialogue.Show(name, body.ToString().TrimEnd(), opts.ToArray());
        }

        private void AskAbandon(string name)
        {
            var g = Game;
            var opts = new List<ModalAction>();
            foreach (var r in g.ActiveRequests)
            {
                int id = r.id;
                if (opts.Count < 3) opts.Add(new ModalAction("포기: " + g.RequestTitle(r), () => { g.AbandonRequest(id); AskRequests(name); }));
            }
            opts.Add(new ModalAction("돌아가기", () => AskRequests(name)));
            dialogue.Show(name, "어떤 의뢰를 포기할까요? 보상은 받을 수 없어요.", opts.ToArray());
        }
    }
}
