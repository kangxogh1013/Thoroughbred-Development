using System;
using System.Collections.Generic;
using System.Text;

namespace HorseRanch
{
    /// <summary>
    /// 마을 생활 2: 시간대(시계) · 주민 의뢰 · 말과의 교감(쓰다듬기/이름/타기) · 기록실.
    /// 순수 C# (UnityEngine 없음). 시계는 GameUI가 TickClock으로 흘려 보낸다.
    /// </summary>
    public sealed partial class RanchGame
    {
        void EnsureLifeState()
        {
            if (State.requests == null) State.requests = new List<RequestData>();
            if (State.records == null) State.records = new List<RaceRecord>();
            if (State.trackRecords == null) State.trackRecords = new List<RaceRecord>();
            if (State.clock < B.dayStartMinute || State.clock >= B.passOutMinute) State.clock = B.dayStartMinute;
            for (int i = 0; i < State.horses.Count; i++)
                if (State.horses[i].best == null) State.horses[i].best = new float[0];
        }

        // =====================================================================
        // 시간대
        // =====================================================================
        public int ClockMinute { get { return (int)State.clock; } }
        public bool IsEvening { get { return State.clock >= B.eveningMinute; } }
        public bool IsNight { get { return State.clock >= B.closeMinute; } }
        /// <summary>밤 10시 이후: 집에 가서 자야 하는 시간 (주민 없음, 집 말고 문 잠김)</summary>
        public bool IsBedtime { get { return State.clock >= B.bedtimeMinute; } }
        /// <summary>튜토리얼 중엔 시계가 멈춘다 (경주 관람 단계를 놓치지 않게)</summary>
        public bool ClockRunning { get { return !InTutorial && State.gameOver == 0; } }

        /// <summary>"오전 9:05" 형식 (자정 넘으면 새벽)</summary>
        public static string ClockLabel(int minute)
        {
            int m = minute % 1440;
            int h = m / 60, mm = m % 60;
            string part = minute >= 1440 ? "새벽" : h < 12 ? "오전" : "오후";
            int h12 = minute >= 1440 ? h : h % 12 == 0 ? 12 : h % 12;
            return part + " " + h12 + ":" + mm.ToString("00");
        }

        /// <summary>실제 시간만큼 시계를 돌린다. 경계(저녁·마감·자정·기절)를 넘은 순간 그 이벤트를 돌려준다 (Changed는 부르지 않음)</summary>
        public ClockEvent TickClock(float realSeconds)
        {
            if (!ClockRunning || realSeconds <= 0f) return ClockEvent.None;
            float before = State.clock;
            State.clock = Math.Min(State.clock + realSeconds * B.minutesPerSecond, B.passOutMinute);
            if (Crossed(before, B.passOutMinute)) return ClockEvent.PassOut;
            if (Crossed(before, B.lateMinute)) return ClockEvent.Late;
            if (Crossed(before, B.bedtimeMinute)) return ClockEvent.Bedtime;
            if (Crossed(before, B.closeMinute)) { CloseRaces(); return ClockEvent.Closing; }
            if (Crossed(before, B.eveningMinute)) return ClockEvent.Evening;
            return ClockEvent.None;
        }

        bool Crossed(float before, int mark) { return before < mark && State.clock >= mark; }

        /// <summary>새벽 2시까지 안 자면 길에서 기절: 소지금 10% (최대 1,000G) 분실. 잃은 금액 반환</summary>
        public int PassOutPenalty()
        {
            int lost = Math.Min(1000, State.gold / 10);
            State.gold -= lost;
            AddLog("밤새 돌아다니다 길에서 잠들었다." + (lost > 0 ? " " + Gold(lost) + " 분실." : ""));
            return lost;
        }

        /// <summary>테스트 · 기절 후 늦잠용</summary>
        public void SetClock(int minute) { State.clock = Math.Max(B.dayStartMinute, Math.Min(minute, B.passOutMinute)); }

        /// <summary>밤 8시: 아직 안 열린 오늘 경주는 결과만 발표 (마권도 정산)</summary>
        void CloseRaces()
        {
            for (int i = 0; i < State.races.Count; i++)
                if (!State.races[i].done) RunRace(i, false);
        }

        // =====================================================================
        // 주민 의뢰
        // =====================================================================
        static readonly string[] DeliverGivers = { "넝마주이 할아버지", "낚시꾼", "화가", "은퇴한 기수" };
        static readonly string[] TrashGivers = { "넝마주이 할아버지", "시장 상인", "엄마" };
        static readonly string[] HorseGivers = { "은퇴한 기수", "화가", "트레이너 김코치" };
        static readonly string[] RaceGivers = { "트레이너 김코치", "은퇴한 기수", "마구간지기 박씨" };
        static readonly string[] BetGivers = { "정보통", "낚시꾼", "시장 상인" };

        public IEnumerable<RequestData> Offers { get { foreach (var r in State.requests) if (!r.accepted) yield return r; } }
        public IEnumerable<RequestData> ActiveRequests { get { foreach (var r in State.requests) if (r.accepted) yield return r; } }
        public int ActiveRequestCount { get { int n = 0; foreach (var r in State.requests) if (r.accepted) n++; return n; } }

        RequestData FindRequest(int id)
        {
            for (int i = 0; i < State.requests.Count; i++) if (State.requests[i].id == id) return State.requests[i];
            return null;
        }

        /// <summary>아침마다: 기한 지난 의뢰 실패 처리 + 안 받은 제안은 새것으로 교체</summary>
        void RefreshRequests()
        {
            for (int i = State.requests.Count - 1; i >= 0; i--)
            {
                var r = State.requests[i];
                if (!r.accepted) { State.requests.RemoveAt(i); continue; }
                if (State.day > r.deadline)
                {
                    State.requests.RemoveAt(i);
                    AddLog("의뢰 기한 초과: " + r.giver + "의 부탁을 들어주지 못했다.");
                }
            }
            for (int i = 0; i < B.requestOffers; i++) State.requests.Add(MakeRequest());
        }

        RequestData MakeRequest()
        {
            var r = new RequestData { id = State.nextRequestId++ };
            int baseReward = B.requestBaseReward + State.day * B.requestRewardPerDay;
            float best = 0f, bestStat = 0f;
            for (int i = 0; i < State.horses.Count; i++) best = Math.Max(best, State.horses[i].Total);

            var type = (RequestType)rng.Next(5);
            r.type = (int)type;
            switch (type)
            {
                case RequestType.Deliver:
                    r.param = rng.Next(RanchConst.ItemCount);
                    r.amount = 1 + (rng.Next(3) == 0 ? 1 : 0);
                    var it = Data.GetItem((ItemType)r.param);
                    r.reward = (int)Math.Round((it != null ? it.price : 200) * r.amount * 1.2f / 10f) * 10 + baseReward / 2;
                    r.giver = DeliverGivers[rng.Next(DeliverGivers.Length)];
                    r.story = "몸이 안 좋은 말이 있어서 그래. 상점에서 사다 줄 수 있겠나?";
                    break;
                case RequestType.Trash:
                    r.amount = 3 + rng.Next(4);
                    r.reward = baseReward + r.amount * 40;
                    r.giver = TrashGivers[rng.Next(TrashGivers.Length)];
                    r.story = "골목에 쓰레기가 너무 많아. 주워서 쓰레기통에 좀 버려 줘.";
                    break;
                case RequestType.ShowHorse:
                    // 지금 가진 가장 좋은 말보다 조금 높은 기준 (훈련하면 닿는 정도)
                    if (rng.Next(2) == 0)
                    {
                        r.param = -1;
                        r.amount = (int)Math.Round(Math.Max(36f, best) + 2 + rng.Next(7));
                    }
                    else
                    {
                        r.param = rng.Next(RanchConst.StatCount);
                        for (int i = 0; i < State.horses.Count; i++) bestStat = Math.Max(bestStat, State.horses[i].stats[r.param]);
                        r.amount = (int)Math.Round(Math.Max(7f, bestStat) + 1 + rng.Next(3));
                    }
                    r.reward = baseReward * 2;
                    r.giver = HorseGivers[rng.Next(HorseGivers.Length)];
                    r.story = "멋진 말을 한번 보고 싶구먼. 그 말을 타고 게시판 앞으로 와 주게.";
                    break;
                case RequestType.RaceTop3:
                    r.amount = 3;
                    r.reward = baseReward * 2 + 200;
                    r.giver = RaceGivers[rng.Next(RaceGivers.Length)];
                    r.story = "자네 말이 달리는 걸 보고 싶네. 아무 경주에서나 3착 안에 들어 보게.";
                    break;
                default:
                    r.type = (int)RequestType.BetHit;
                    r.amount = 1;
                    r.reward = baseReward + 150;
                    r.giver = BetGivers[rng.Next(BetGivers.Length)];
                    r.story = "내 감이 맞는지 시험해 보고 싶어. 아무 마권이나 한 번 맞혀 와.";
                    break;
            }
            // 가끔 보상의 일부를 아이템으로
            if (rng.Next(10) < 3) { r.rewardItem = rng.Next(RanchConst.ItemCount); r.reward = r.reward * 6 / 10 / 10 * 10; }
            return r;
        }

        /// <summary>게시판에 붙는 한 줄 제목</summary>
        public string RequestTitle(RequestData r)
        {
            switch ((RequestType)r.type)
            {
                case RequestType.Deliver: return ItemName(r.param) + " " + r.amount + "개 가져다주기";
                case RequestType.Trash: return "쓰레기 " + r.amount + "개 버리기";
                case RequestType.ShowHorse: return (r.param < 0 ? "능력 총합 " : Data.stats[r.param].displayName + " ") + r.amount + " 이상인 말 타고 오기";
                case RequestType.RaceTop3: return "경주에서 3착 안에 들기";
                default: return "마권 한 번 적중시키기";
            }
        }

        public string RequestRewardText(RequestData r)
        {
            return Gold(r.reward) + (r.rewardItem >= 0 ? " + " + ItemName(r.rewardItem) : "");
        }

        string ItemName(int type)
        {
            var it = Data.GetItem((ItemType)type);
            return it != null ? it.displayName : "물건";
        }

        /// <summary>진행 상황 문자열 (예: 2/5)</summary>
        public string RequestProgress(RequestData r)
        {
            switch ((RequestType)r.type)
            {
                case RequestType.Deliver: return Math.Min(State.items[r.param], r.amount) + "/" + r.amount;
                case RequestType.Trash: return Math.Min(State.trashDisposed - r.baseline, r.amount) + "/" + r.amount;
                case RequestType.ShowHorse: return RidingHorse != null ? "타는 말 " + HorseValue(RidingHorse, r) : "말을 타고 오세요";
                default: return r.progress > 0 ? "완료" : "아직";
            }
        }

        int HorseValue(HorseData h, RequestData r) { return (int)Math.Floor(r.param < 0 ? h.Total : h.stats[r.param]); }

        public bool CanComplete(RequestData r)
        {
            if (r == null || !r.accepted) return false;
            switch ((RequestType)r.type)
            {
                case RequestType.Deliver: return State.items[r.param] >= r.amount;
                case RequestType.Trash: return State.trashDisposed - r.baseline >= r.amount;
                case RequestType.ShowHorse: return RidingHorse != null && HorseValue(RidingHorse, r) >= r.amount;
                default: return r.progress > 0;
            }
        }

        public bool AcceptRequest(int id)
        {
            var r = FindRequest(id);
            if (r == null || r.accepted) return false;
            if (ActiveRequestCount >= B.requestMaxActive) { RaiseToast("의뢰는 동시에 " + B.requestMaxActive + "개까지 맡을 수 있습니다"); return false; }
            r.accepted = true;
            r.deadline = State.day + B.requestDays - 1;
            r.baseline = State.trashDisposed;
            r.progress = 0;
            AddLog("의뢰 수락: " + r.giver + " - " + RequestTitle(r));
            RaiseChanged();
            return true;
        }

        public bool AbandonRequest(int id)
        {
            var r = FindRequest(id);
            if (r == null || !r.accepted) return false;
            State.requests.Remove(r);
            AddLog("의뢰 포기: " + RequestTitle(r));
            RaiseChanged();
            return true;
        }

        /// <summary>보고: 보상 지급 (배달은 물건이 빠진다). 성공하면 감사 인사 문자열</summary>
        public string CompleteRequest(int id)
        {
            var r = FindRequest(id);
            if (!CanComplete(r)) return null;
            if ((RequestType)r.type == RequestType.Deliver) State.items[r.param] -= r.amount;
            State.gold += r.reward;
            if (r.rewardItem >= 0) State.items[r.rewardItem]++;
            State.requests.Remove(r);
            State.requestsDone++;
            AddLog("의뢰 완료: " + r.giver + " - 보상 " + RequestRewardText(r));
            RaiseChanged();
            return r.giver + ": 정말 고맙네! 약속한 보상이야.\n<b>" + RequestRewardText(r) + "</b> 을(를) 받았다.";
        }

        /// <summary>RunRace에서: 내 말 착순 / 마권 적중 반영</summary>
        void NotifyRequests(int myPos, bool betHit)
        {
            for (int i = 0; i < State.requests.Count; i++)
            {
                var r = State.requests[i];
                if (!r.accepted || r.progress > 0) continue;
                if (r.type == (int)RequestType.RaceTop3 && myPos >= 1 && myPos <= r.amount) { r.progress = 1; RaiseToast("의뢰 달성: " + RequestTitle(r) + " (게시판에서 보고)"); }
                if (r.type == (int)RequestType.BetHit && betHit) { r.progress = 1; RaiseToast("의뢰 달성: " + RequestTitle(r) + " (게시판에서 보고)"); }
            }
        }

        // =====================================================================
        // 말과의 교감
        // =====================================================================
        public bool PettedToday(HorseData h) { return h != null && h.petDay == State.day; }

        public bool PetHorse(int horseId)
        {
            var h = FindHorse(horseId);
            if (h == null) return false;
            if (PettedToday(h)) { RaiseToast("오늘은 이미 쓰다듬어 줬다"); return false; }
            h.petDay = State.day;
            h.bond = Math.Min(B.bondMax, h.bond + 1);
            h.condition = Math.Min(100f, h.condition + B.petCondition);
            RaiseChanged();
            return true;
        }

        /// <summary>새 이름 후보 (이름 짓기 창 대신 3개 중 고르기)</summary>
        public string[] NameSuggestions(int count)
        {
            var list = new List<string>();
            for (int tries = 0; tries < 40 && list.Count < count; tries++)
            {
                string n = GenName();
                if (!list.Contains(n)) list.Add(n);
            }
            return list.ToArray();
        }

        public bool RenameHorse(int horseId, string name)
        {
            var h = FindHorse(horseId);
            if (h == null || string.IsNullOrEmpty(name) || h.name == name) return false;
            if (h.name == FatherHorse) { RaiseToast("천둥의 이름은 바꿀 수 없다"); return false; }
            AddLog("「" + h.name + "」의 이름을 「" + name + "」(으)로 바꿨다.");
            h.name = name;
            RaiseChanged();
            return true;
        }

        public static string BondLabel(int bond)
        {
            return bond >= 8 ? "둘도 없는 단짝" : bond >= 5 ? "잘 따름" : bond >= 2 ? "조금 친해짐" : "어색함";
        }

        /// <summary>지금 타고 있는 말 (훈련에 들어갔거나 팔렸으면 자동으로 내림)</summary>
        public HorseData RidingHorse
        {
            get
            {
                if (State.ridingId == 0) return null;
                var h = FindHorse(State.ridingId);
                if (h == null || h.IsTraining || h.injury > 0) { State.ridingId = 0; return null; }
                return h;
            }
        }

        public bool CanRide(HorseData h, out string reason)
        {
            reason = null;
            if (h == null) reason = "말이 없습니다";
            else if (h.IsTraining) reason = "훈련장에 가 있는 말입니다";
            else if (h.injury > 0) reason = "다친 말은 탈 수 없습니다";
            return reason == null;
        }

        public bool StartRiding(int horseId)
        {
            var h = FindHorse(horseId);
            string why;
            if (!CanRide(h, out why)) { RaiseToast(why); return false; }
            State.ridingId = h.id;
            RaiseChanged();
            return true;
        }

        public void StopRiding()
        {
            if (State.ridingId == 0) return;
            State.ridingId = 0;
            RaiseChanged();
        }

        // =====================================================================
        // 기록실
        // =====================================================================
        void RecordRace(int raceIndex, bool gp, string winner, float time, HorseData me, int myPos, bool mineWon, List<NoticeData> notes)
        {
            var rec = new RaceRecord { day = State.day, raceIndex = raceIndex, grandPrix = gp, winner = winner, time = time, mineWon = mineWon, mine = me != null ? me.name : "", myPos = myPos };
            State.records.Insert(0, rec);
            if (State.records.Count > 60) State.records.RemoveAt(State.records.Count - 1);

            // 트랙 레코드
            RaceRecord best = null;
            for (int i = 0; i < State.trackRecords.Count; i++) if (State.trackRecords[i].raceIndex == raceIndex) best = State.trackRecords[i];
            if (time > 0f && (best == null || time < best.time))
            {
                if (best != null) State.trackRecords.Remove(best);
                State.trackRecords.Add(rec);
                if (best != null && notes != null)
                    notes.Add(new NoticeData("트랙 레코드 경신! " + winner + " " + FormatTime(time) + (mineWon ? " (내 말)" : ""), mineWon ? NoticeTone.Good : NoticeTone.Normal));
            }
        }

        /// <summary>내 말 개인 최고 기록 갱신</summary>
        void RecordPersonalBest(HorseData h, int raceIndex, float time)
        {
            if (h == null || time <= 0f) return;
            if (h.best == null || h.best.Length < Data.races.Length)
            {
                var b = h.best ?? new float[0];
                Array.Resize(ref b, Data.races.Length);
                h.best = b;
            }
            if (h.best[raceIndex] <= 0f || time < h.best[raceIndex]) h.best[raceIndex] = time;
        }

        public RaceRecord TrackRecord(int raceIndex)
        {
            for (int i = 0; i < State.trackRecords.Count; i++) if (State.trackRecords[i].raceIndex == raceIndex) return State.trackRecords[i];
            return null;
        }

        /// <summary>기록실 본문 (왼쪽: 트랙 레코드 + 최근 우승마)</summary>
        public string RecordsText()
        {
            var sb = new StringBuilder(2048);
            sb.Append("<size=115%><b>트랙 레코드</b></size>\n");
            for (int i = 0; i < Data.races.Length; i++)
            {
                var r = TrackRecord(i);
                sb.Append(Data.races[i].displayName).Append(' ').Append(Data.races[i].distance.ToString("N0")).Append("m<pos=38%>");
                if (r == null) sb.Append("<color=#566458>기록 없음</color>\n");
                else sb.Append("<b>").Append(FormatTime(r.time)).Append("</b>  ").Append(r.winner).Append(r.mineWon ? " <color=#2A6A3D>(내 말)</color>" : "")
                       .Append(" <color=#566458>").Append(r.day).Append("일차</color>\n");
            }
            sb.Append("\n<size=115%><b>최근 우승마</b></size>\n");
            if (State.records.Count == 0) sb.Append("<color=#566458>아직 열린 경주가 없습니다.</color>\n");
            for (int i = 0; i < State.records.Count && i < 16; i++)
            {
                var r = State.records[i];
                sb.Append("<color=#566458>").Append(r.day).Append("일</color> ").Append(Data.races[r.raceIndex].displayName)
                  .Append(r.grandPrix ? " <color=#B8860B>G1</color>" : "").Append("<pos=34%>")
                  .Append(r.mineWon ? "<color=#2A6A3D><b>" + r.winner + "</b></color>" : r.winner)
                  .Append("<pos=62%>").Append(FormatTime(r.time));
                if (!string.IsNullOrEmpty(r.mine) && !r.mineWon) sb.Append("<pos=78%><color=#566458>").Append(r.mine).Append(' ').Append(r.myPos > 0 ? r.myPos + "착" : "중지").Append("</color>");
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>기록실 본문 (오른쪽: 내 말 기록 + 목장 연대기)</summary>
        public string MyHorsesText()
        {
            var sb = new StringBuilder(1536);
            sb.Append("<size=115%><b>내 말 기록</b></size>\n");
            if (State.horses.Count == 0) sb.Append("<color=#566458>말이 없습니다.</color>\n");
            for (int i = 0; i < State.horses.Count; i++)
            {
                var h = State.horses[i];
                sb.Append("<b>").Append(h.name).Append("</b>").Append(h.retired ? " <color=#566458>(은퇴)</color>" : "")
                  .Append("<pos=40%>").Append(h.races).Append("전 ").Append(h.wins).Append("승");
                if (h.grandPrixWins > 0) sb.Append(" <color=#B8860B>G1 ").Append(h.grandPrixWins).Append("승</color>");
                sb.Append("<pos=72%>교감 ").Append(h.bond).Append('\n');
                if (h.best != null)
                    for (int k = 0; k < h.best.Length && k < Data.races.Length; k++)
                        if (h.best[k] > 0f) sb.Append("  <color=#566458>").Append(Data.races[k].displayName).Append(" 최고 ").Append(FormatTime(h.best[k])).Append("</color>\n");
            }
            sb.Append("\n<size=115%><b>목장 연대기</b></size>\n");
            int wins = 0, entries = 0;
            for (int i = 0; i < State.records.Count; i++) { if (!string.IsNullOrEmpty(State.records[i].mine)) entries++; if (State.records[i].mineWon) wins++; }
            sb.Append("최근 기록 중 출전 ").Append(entries).Append("회 - 우승 ").Append(wins).Append("회\n");
            sb.Append("떠나보낸 말 ").Append(State.deadCount).Append("마리 - 완료한 의뢰 ").Append(State.requestsDone).Append("건\n");
            if (State.cleared) sb.Append("<color=#B8860B>").Append(State.storyClearDay).Append("일차 그랑프리 우승: ").Append(State.storyWinner).Append("</color>\n");
            return sb.ToString();
        }
    }
}
