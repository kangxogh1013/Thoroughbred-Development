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
        [Tooltip("실내에서는 밤 색을 이만큼만 (0~1)")]
        [SerializeField] [Range(0f, 1f)] private float indoorDarkness = 0.35f;

        [Header("말 타기")]
        [SerializeField] private PlayerMount mount;

        private int shownClock = -1;
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
            bool hidden = title.IsOpen || cutscene.IsOpen || inEnding || gameOverShown;
            if (hidden) { SetOverlay(Color.clear); return; }

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
                if (!RideAreas.Contains(currentArea) || currentArea == "마구간") c.a *= indoorDarkness;
                SetOverlay(c);
            }
        }

        private void SetOverlay(Color c)
        {
            if (nightOverlay == null) return;
            if (nightOverlay.color != c) nightOverlay.color = c;
            bool on = c.a > 0.001f;
            if (nightOverlay.enabled != on) nightOverlay.enabled = on;
        }

        /// <summary>시각별 화면 색: 새벽 푸르스름 → 낮 없음 → 노을 주황 → 밤 남색</summary>
        private static Color SkyTint(int minute)
        {
            var dawn = new Color(0.45f, 0.55f, 0.85f, 0.18f);
            var dusk = new Color(1f, 0.5f, 0.18f, 0.22f);
            var night = new Color(0.04f, 0.07f, 0.24f, 0.5f);
            var deep = new Color(0.02f, 0.03f, 0.14f, 0.6f);
            if (minute < 420) return Color.Lerp(dawn, Color.clear, (minute - 360) / 60f);            // 06~07시
            if (minute < 1020) return Color.clear;                                                     // 낮
            if (minute < 1140) return Color.Lerp(Color.clear, dusk, (minute - 1020) / 120f);          // 17~19시 노을
            if (minute < 1260) return Color.Lerp(dusk, night, (minute - 1140) / 120f);                // 19~21시
            return Color.Lerp(night, deep, Mathf.Clamp01((minute - 1260) / 240f));                    // 21시~
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
            if (g.IsNight && ClosesAtNight(it.Kind))
            {
                dialogue.Show(it.DisplayName, "오늘 영업은 끝났어요. 내일 아침 6시에 다시 와 주세요.\n<size=80%><color=#566458>밤 8시부터 아침 6시까지 상점 · 시장 · 은행 · 교배소는 문을 닫습니다. 도박장은 밤에도 열려 있어요.</color></size>");
                return true;
            }
            switch (it.Kind)
            {
                case InteractKind.StallHorse: AskStallHorse(it.Param); return true;
                case InteractKind.RequestBoard: AskRequests(it.DisplayName); return true;
                case InteractKind.Records:
                    dialogue.Show(it.DisplayName, "역대 우승마의 이름과 기록이 새겨진 트로피가 늘어서 있다.\n<size=80%><color=#566458>K 키로 어디서든 기록실을 열 수 있어요.</color></size>",
                        new ModalAction("기록 보기", () => OpenWindow(WindowId.Records, "기록실"), ModalStyle.Primary), new ModalAction("나가기", null));
                    return true;
                case InteractKind.Bench:
                    if (g.RidingHorse == null) return false;
                    toast.Show("말에서 내려야 앉을 수 있다 (R)", false);
                    return true;
            }
            return false;
        }

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
            if (h.id == g.State.ridingId) { dialogue.Show(h.name, "지금 타고 있는 말의 칸이다. 말에서 내리면(R) 알아서 돌아온다."); return; }
            var b = g.Data.balance;
            string why;
            bool canRide = g.CanRide(h, out why);
            string body = "교감 <b>" + h.bond + "/" + b.bondMax + "</b> (" + RanchGame.BondLabel(h.bond) + ")  -  컨디션 " + Mathf.RoundToInt(h.condition) + "  -  피로 " + Mathf.RoundToInt(h.fatigue)
                + "\n<size=80%><color=#566458>하루 한 번 쓰다듬으면 교감 +1, 컨디션 +" + b.petCondition.ToString("0") + ". 교감이 높을수록 컨디션이 잘 유지돼요."
                + (canRide ? " 타고 나가면 마을을 " + b.rideSpeedScale.ToString("0.0") + "배 빠르게 달립니다." : " " + why + ".") + "</color></size>";
            dialogue.Show(h.name, body,
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
            dialogue.Show(h.name, PetLines[i] + "\n교감 " + h.bond + " (" + RanchGame.BondLabel(h.bond) + ")");
        }

        private void AskRename(HorseData h)
        {
            var names = Game.NameSuggestions(3);
            var opts = new List<ModalAction>();
            for (int i = 0; i < names.Length; i++)
            {
                string n = names[i];
                opts.Add(new ModalAction("「" + n + "」", () => { if (Game.RenameHorse(h.id, n)) { audioManager.PlaySfx(SfxId.Coin); dialogue.Show(n, "(새 이름이 마음에 드는지 고개를 끄덕인다.)"); } }));
            }
            opts.Add(new ModalAction("그대로 두기", null));
            dialogue.Show(h.name, "새 이름을 골라 주세요. 마음에 드는 게 없으면 다시 물어보면 새 후보가 나와요.", opts.ToArray());
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
