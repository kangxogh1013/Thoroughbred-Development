using System;

namespace HorseRanch
{
    /// <summary>
    /// 스토리 · 진행 규칙 (RanchGame partial)
    /// - 목표: 아버지가 사고로 놓친 장거리 대상경주(그랑프리)를 내 말로 우승 → 엔딩
    /// - 패배: 아버지의 빚을 상환일마다 갚지 못해 연체가 쌓이면 목장 압류 / 말도 돈도 없으면 파산
    /// - 튜토리얼: 단계별 목표 + 첫 구매 1회 무료
    /// - 돈 쓸 곳: 빚 상환, 목장 시설 3종 업그레이드
    /// </summary>
    public sealed partial class RanchGame
    {
        /// <summary>게임 오버 (UI가 패배 연출을 띄움)</summary>
        public event Action<GameOverReason> GameOver;
        /// <summary>스토리 알림 (제목, 본문): 상환·연체 같은 아침 소식</summary>
        public event Action<string, string> StoryNotice;
        /// <summary>튜토리얼 단계가 바뀜</summary>
        public event Action<TutorialStep> TutorialChanged;

        public const string RivalRanch = "흑철 목장";
        public const string RivalHorse = "흑풍";
        public const string FatherHorse = "천둥";

        // ===== 스토리 그랑프리 =====
        /// <summary>아버지가 사고를 당한 경주 = 가장 긴 경주 (장거리)</summary>
        public int StoryRace
        {
            get
            {
                int best = 0;
                for (int i = 1; i < Data.races.Length; i++) if (Data.races[i].distance > Data.races[best].distance) best = i;
                return best;
            }
        }

        public bool StoryCleared { get { return State.cleared; } }
        public bool IsOver { get { return State.gameOver != (int)GameOverReason.None; } }

        /// <summary>스토리 그랑프리 출주표에 라이벌 '흑풍'을 넣는다 (내 말 자리와 겹치지 않게)</summary>
        void AddRival(RaceDayState rs)
        {
            if (!rs.grandPrix || rs.raceIndex != StoryRace) return;
            int lane = (rs.playerSlot + 3) % RanchConst.RunnerCount;
            var h = rs.field[lane];
            float avg = 0f;
            for (int i = 0; i < rs.field.Count; i++) avg += rs.field[i].Total;
            avg /= rs.field.Count;
            float k = avg * B.rivalStatMultiplier / Math.Max(1f, h.Total);
            for (int i = 0; i < h.stats.Length; i++) h.stats[i] = (float)Math.Round(Math.Min(B.statCap, h.stats[i] * k) * 10f) / 10f;
            h.name = RivalHorse;
            h.coat = 0;
            h.condition = 90f;
            h.form.Clear(); h.form.Add(1); h.form.Add(1); h.form.Add(2);
        }

        /// <summary>내 말이 스토리 그랑프리를 우승했는지 판정 (RunRace에서 호출)</summary>
        bool CheckStoryClear(RaceDayState rs, HorseData me, int pos, System.Collections.Generic.List<NoticeData> notes)
        {
            if (pos != 1 || me == null || !rs.grandPrix || rs.raceIndex != StoryRace || State.cleared) return false;
            State.cleared = true;
            State.storyClearDay = State.day;
            State.storyWinner = me.name;
            notes.Add(new NoticeData("아버지가 꿈꾸던 그랑프리 우승! 「" + me.name + "」", NoticeTone.Good));
            AddLog("「" + me.name + "」 장거리 그랑프리 우승. 아버지의 꿈을 이뤘다.");
            return true;
        }

        // ===== 아버지의 빚 =====
        public bool HasDebt { get { return State.debt > 0; } }

        public bool IsPaymentDay(int day)
        {
            return B.debtInterval > 0 && day >= B.debtFirstDay && (day - B.debtFirstDay) % B.debtInterval == 0;
        }

        /// <summary>다음 상환일까지 남은 날 (0 = 오늘 아침에 정산됨, -1 = 빚 없음)</summary>
        public int DaysToPayment
        {
            get
            {
                if (!HasDebt) return -1;
                if (State.day < B.debtFirstDay) return B.debtFirstDay - State.day;
                int r = (State.day - B.debtFirstDay) % B.debtInterval;
                return r == 0 ? B.debtInterval : B.debtInterval - r;
            }
        }

        /// <summary>다음 상환일에 실제로 빠질 금액 (미리 갚은 만큼 줄어듦)</summary>
        public int NextPaymentDue { get { return Math.Max(0, Math.Min(B.debtPayment, State.debt + State.debtCredit) - State.debtCredit); } }

        /// <summary>은행에서 미리 갚기. 갚은 만큼 빚이 줄고, 다음 상환일 금액에서도 빠진다</summary>
        public int RepayDebt(int amount)
        {
            amount = Math.Min(amount, Math.Min(State.gold, State.debt));
            if (amount <= 0) { RaiseToast(State.debt <= 0 ? "갚을 빚이 없습니다" : "골드가 부족합니다"); return 0; }
            State.gold -= amount;
            State.debt -= amount;
            State.debtCredit += amount;
            AddLog("은행에 빚 " + Gold(amount) + "을 갚았다. 남은 빚 " + Gold(State.debt));
            RaiseToast("빚 상환 -" + Gold(amount) + " · 남은 빚 " + Gold(State.debt));
            CheckDebtFree();
            RaiseChanged();
            return amount;
        }

        void CheckDebtFree()
        {
            if (State.debt > 0) return;
            State.debt = 0; State.debtCredit = 0; State.missedPayments = 0;
            Unlock(AchievementId.DebtFree, null);
            AddLog("아버지가 남긴 빚을 모두 갚았다!");
        }

        /// <summary>아침 정산: 상환일이면 자동 출금, 돈이 모자라면 연체 (NextDay에서 호출)</summary>
        void CollectDebt()
        {
            if (!HasDebt || !IsPaymentDay(State.day)) return;
            int due = NextPaymentDue;
            if (due <= 0)
            {
                State.debtCredit -= Math.Min(B.debtPayment, State.debtCredit);
                Notice("상환일", "미리 갚아 둔 돈으로 이번 상환이 처리되었습니다.\n남은 빚 " + Gold(State.debt));
                return;
            }
            if (State.gold >= due)
            {
                State.gold -= due;
                State.debt -= due;
                State.debtCredit = 0;
                AddLog("상환일: " + Gold(due) + " 자동 출금. 남은 빚 " + Gold(State.debt));
                Notice("상환일", "은행에서 " + Gold(due) + "이 출금되었습니다.\n남은 빚 " + Gold(State.debt) + (State.debt > 0 ? " · 다음 상환 " + B.debtInterval + "일 뒤" : ""));
                CheckDebtFree();
                return;
            }
            State.missedPayments++;
            State.debt += B.latePenalty;
            State.debtCredit = 0;
            AddLog("상환일: 돈이 모자라 연체되었다 (" + State.missedPayments + "/" + B.maxMissedPayments + ")");
            if (State.missedPayments >= B.maxMissedPayments) { TriggerGameOver(GameOverReason.Foreclosure); return; }
            Notice("연체 경고 " + State.missedPayments + "/" + B.maxMissedPayments,
                "상환금 " + Gold(due) + "을 내지 못했습니다. 연체료 " + Gold(B.latePenalty) + "이 붙었습니다.\n"
                + B.maxMissedPayments + "번 연체하면 " + RivalRanch + "이 목장을 사들입니다. (광장 은행원에게 미리 갚을 수 있어요)");
        }

        void Notice(string title, string body) { if (StoryNotice != null) StoryNotice(title, body); }

        void TriggerGameOver(GameOverReason reason)
        {
            if (IsOver || State.cleared && reason == GameOverReason.Bankrupt) return;
            State.gameOver = (int)reason;
            AddLog(reason == GameOverReason.Foreclosure ? "목장이 압류되었다." : "파산했다.");
            RaiseChanged();
            if (GameOver != null) GameOver(reason);
        }

        // ===== 목장 시설 =====
        public int FacilityLevel(FacilityId f) { return State.facility[(int)f]; }
        public int FacilityMax { get { return B.facilityPrices.Length; } }
        /// <summary>다음 레벨 가격 (최대 레벨이면 -1)</summary>
        public int FacilityPrice(FacilityId f)
        {
            int lv = FacilityLevel(f);
            return lv >= FacilityMax ? -1 : B.facilityPrices[lv];
        }

        public static string FacilityName(FacilityId f)
        {
            switch (f) { case FacilityId.TrainingGear: return "훈련 장비"; case FacilityId.StableRoom: return "마구간 개량"; default: return "목초지 확장"; }
        }

        public string FacilityEffect(FacilityId f)
        {
            switch (f)
            {
                case FacilityId.TrainingGear: return "훈련 성장량 +" + (int)(B.gearGainPerLevel * 100) + "%";
                case FacilityId.StableRoom: return "하룻밤 피로 회복 +" + B.roomRecoveryPerLevel;
                default: return "매일 컨디션 +" + B.pastureConditionPerLevel;
            }
        }

        public bool UpgradeFacility(FacilityId f)
        {
            int price = FacilityPrice(f);
            if (price < 0) { RaiseToast("이미 최고 단계입니다"); return false; }
            if (State.gold < price) { RaiseToast("골드가 부족합니다 (" + Gold(price) + ")"); return false; }
            State.gold -= price;
            State.facility[(int)f]++;
            AddLog(FacilityName(f) + " " + State.facility[(int)f] + "단계 공사 완료 (" + FacilityEffect(f) + ")");
            RaiseToast(FacilityName(f) + " " + State.facility[(int)f] + "단계 · " + FacilityEffect(f));
            RaiseChanged();
            return true;
        }

        float TrainingGearBonus { get { return 1f + B.gearGainPerLevel * FacilityLevel(FacilityId.TrainingGear); } }
        float RoomRecoveryBonus { get { return B.roomRecoveryPerLevel * FacilityLevel(FacilityId.StableRoom); } }
        float PastureBonus { get { return B.pastureConditionPerLevel * FacilityLevel(FacilityId.Pasture); } }

        // ===== 튜토리얼 =====
        public TutorialStep Tutorial { get { return (TutorialStep)State.tutorialStep; } }
        public bool InTutorial { get { return State.tutorialStep < (int)TutorialStep.Done; } }

        /// <summary>지금 단계가 step이면 다음 단계로</summary>
        public void AdvanceTutorial(TutorialStep step)
        {
            if (State.tutorialStep != (int)step) return;
            State.tutorialStep++;
            if (TutorialChanged != null) TutorialChanged(Tutorial);
            RaiseChanged();
        }

        public void SkipTutorial()
        {
            State.tutorialStep = (int)TutorialStep.Done;
            for (int i = 0; i < State.freeUsed.Length; i++) State.freeUsed[i] = true;
            if (TutorialChanged != null) TutorialChanged(Tutorial);
            RaiseChanged();
        }

        public bool IsFree(FreeKind k) { return InTutorial && !State.freeUsed[(int)k]; }

        void UseFree(FreeKind k) { State.freeUsed[(int)k] = true; }

        /// <summary>처음 들르는 곳 안내를 봤는지 기록. 처음이면 true</summary>
        public bool MarkTipSeen(string key)
        {
            if (State.tipsSeen.Contains(key)) return false;
            State.tipsSeen.Add(key);
            return true;
        }

        public void MarkIntroSeen() { State.introSeen = true; RaiseChanged(); }
        public void MarkEndingSeen() { State.endingSeen = true; RaiseChanged(); }

        // ===== 시즌 (상대 말 수준) =====
        /// <summary>day일차의 시즌 단계 (0부터). npcTierDays마다 1씩 오른다</summary>
        public int NpcTier(int day) { return B.npcTierDays > 0 ? Math.Max(0, day / B.npcTierDays) : 0; }
        public int DaysToNextTier { get { return B.npcTierDays > 0 ? B.npcTierDays - State.day % B.npcTierDays : -1; } }

        // ===== 후반 콘텐츠: 아버지의 말 천둥 =====
        /// <summary>깊은 숲 덤불을 지날 수 있는지 (그랑프리 우승 후)</summary>
        public bool ForestOpen { get { return State.cleared; } }

        /// <summary>천둥을 목장으로 데려오기. 은퇴마(경주 불가)로 들어오고 교배하면 천둥 혈통 망아지가 태어난다</summary>
        public bool AdoptThunder()
        {
            if (State.thunderFound || !ForestOpen) return false;
            if (State.horses.Count >= B.maxStable) { RaiseToast("마구간이 가득 찼습니다. 자리를 비우고 다시 오세요"); return false; }
            var h = new HorseData { id = State.nextId++, name = FatherHorse, male = true, coat = 0, retired = true, thunderBlood = true, condition = 60f };
            for (int i = 0; i < h.stats.Length; i++) h.stats[i] = B.thunderStat;
            h.wins = 12; h.races = 20;
            State.horses.Add(h);
            State.thunderFound = true;
            AddLog("아버지의 말 「" + FatherHorse + "」이 목장으로 돌아왔다. 교배소에서 천둥 혈통의 망아지를 얻을 수 있다.");
            RaiseToast(FatherHorse + "이 목장으로 돌아왔습니다 (교배 전용)");
            RaiseChanged();
            return true;
        }

        // ===== 테스트용 =====
        public void DebugClearStory()
        {
            if (State.cleared || State.horses.Count == 0) return;
            State.cleared = true;
            State.storyClearDay = State.day;
            State.storyWinner = State.horses[0].name;
            RaiseChanged();
            RaiseGameCleared();
        }

        public void DebugGameOver() { TriggerGameOver(GameOverReason.Foreclosure); }

        /// <summary>구버전 세이브 보정</summary>
        void EnsureStoryState()
        {
            if (State.freeUsed == null || State.freeUsed.Length != RanchConst.FreeKindCount) State.freeUsed = new bool[RanchConst.FreeKindCount];
            if (State.facility == null || State.facility.Length != RanchConst.FacilityCount) State.facility = new int[RanchConst.FacilityCount];
            if (State.tipsSeen == null) State.tipsSeen = new System.Collections.Generic.List<string>();
        }
    }
}
