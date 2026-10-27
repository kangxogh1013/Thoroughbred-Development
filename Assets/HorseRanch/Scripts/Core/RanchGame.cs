using System;
using System.Collections.Generic;
using System.Text;

namespace HorseRanch
{
    /// <summary>
    /// 게임 규칙 전체 (UI·MonoBehaviour와 분리된 순수 C#).
    /// UI는 아래 이벤트를 구독해 갱신하고, 플레이어 입력은 public 메서드로 전달한다.
    /// </summary>
    public sealed class RanchGame
    {
        // ===== 이벤트 (느슨한 결합) =====
        public event Action Changed;                                        // 상태 변경 → UI 새로고침 + 저장
        public event Action<string> Toast;                                  // 짧은 알림
        public event Action<AchievementDefinition> AchievementUnlocked;
        public event Action<int, TrainingEventDefinition> TrainingEventTriggered; // (말 id, 이벤트)
        public event Action<RaceReplay> RaceFinished;                       // 관람한 경주 (재생 데이터)
        public event Action<int, RaceResultData> RaceAutoRun;               // 관람하지 않아 자동 진행된 경주
        public event Action<HorseData, List<NoticeData>> LotRevealed;       // 블라인드 말 감정 결과
        public event Action<HorseData> FoalBorn;
        public event Action GameCleared;
        public event Action Bankrupt;

        public GameState State { get; private set; }
        public GameData Data { get; private set; }
        BalanceConfig B { get { return Data.balance; } }

        readonly Random rng;
        readonly Dictionary<int, ParimutuelMarket> markets = new Dictionary<int, ParimutuelMarket>();
        readonly List<HorseData> runBuffer = new List<HorseData>(RanchConst.RunnerCount);
        bool suppressChanged;

        public RanchGame(GameData data, int seed)
        {
            Data = data;
            rng = new Random(seed);
        }

        // ===== 시작 / 불러오기 =====
        public void NewGame()
        {
            State = new GameState { gold = B.startGold };
            State.items[(int)ItemType.Medicine] = 1;
            var h = MakeHorse(true);
            for (int i = 0; i < RanchConst.StatCount; i++) h.stats[i] = B.startStat;
            h.name = "새싹";
            State.horses.Add(h);
            markets.Clear();
            GenerateDay();
            AddLog("목장을 열었습니다. 첫 말 「새싹」이 도착했습니다. (전 스탯 " + B.startStat + ")");
            RaiseChanged();
        }

        public void LoadState(GameState s)
        {
            State = s;
            markets.Clear();
            // 구버전 세이브 보정
            if (State.items == null || State.items.Length < RanchConst.ItemCount) State.items = new int[RanchConst.ItemCount];
            if (State.achievementDays == null) State.achievementDays = new int[RanchConst.AchievementCount];
            else if (State.achievementDays.Length < RanchConst.AchievementCount)
            {
                var a = State.achievementDays; Array.Resize(ref a, RanchConst.AchievementCount); State.achievementDays = a; // 기존 기록 유지
            }
            if (State.tickets == null) State.tickets = new List<BetTicket>();
            if (State.tomorrow == null) State.tomorrow = new List<RaceDayState>();
            if (State.masterTips == null) State.masterTips = new List<string>();
            if (State.specialists == null || State.specialists.Length < RanchConst.StatCount) State.specialists = new bool[RanchConst.StatCount];
            if (State.grandPrixWon == null || State.grandPrixWon.Length < 2) State.grandPrixWon = new bool[2];
            if (State.races == null || State.races.Count != Data.races.Length) GenerateDay();
            RaiseChanged();
        }

        // ===== 조회 =====
        public HorseData FindHorse(int id)
        {
            if (id <= 0) return null;
            for (int i = 0; i < State.horses.Count; i++) if (State.horses[i].id == id) return State.horses[i];
            return null;
        }

        public int SellPrice(HorseData h) { return (int)Math.Round(B.sellBase + h.Total * B.sellPerStat + h.wins * B.sellPerWin); }
        public int LotPrice { get { return (int)Math.Round(B.horsePrice * (State.discount ? B.hunterDiscount : 1f)); } }
        public bool IsGrandPrixDay(int day) { return B.grandPrixInterval > 0 && day % B.grandPrixInterval == 0; }
        public int DaysToGrandPrix
        {
            get
            {
                if (B.grandPrixInterval <= 0) return -1;
                int r = State.day % B.grandPrixInterval;
                return r == 0 ? 0 : B.grandPrixInterval - r;
            }
        }
        public RaceDayState Race(int raceIndex) { return State.races[raceIndex]; }

        public int EntryFee(int raceIndex)
        {
            var def = Data.races[raceIndex];
            return Race(raceIndex).grandPrix ? (int)(def.entryFee * B.grandPrixFeeMultiplier) : def.entryFee;
        }

        public int Prize(int raceIndex, int place)
        {
            var def = Data.races[raceIndex];
            if (place < 1 || place > def.prizes.Length) return 0;
            return Race(raceIndex).grandPrix ? (int)(def.prizes[place - 1] * B.grandPrixPrizeMultiplier) : def.prizes[place - 1];
        }

        public bool IsAchieved(AchievementId id) { return State.achievementDays[(int)id] > 0; }

        public int SpecialistCount
        {
            get { int c = 0; for (int i = 0; i < State.specialists.Length; i++) if (State.specialists[i]) c++; return c; }
        }

        public bool IsEnteredAnywhere(int horseId)
        {
            for (int i = 0; i < State.races.Count; i++)
                if (!State.races[i].done && State.races[i].entryId == horseId) return true;
            return false;
        }

        /// <summary>출전 가능 여부와 불가 사유</summary>
        public bool CanEnter(HorseData h, int raceIndex, out string reason)
        {
            reason = null;
            if (h == null) { reason = "말이 없습니다"; return false; }
            if (h.retired) { reason = "은퇴마"; return false; }
            if (h.IsTraining) { reason = "훈련 중"; return false; }
            if (h.lastRaceDay == State.day) { reason = "오늘 이미 출전"; return false; }
            for (int i = 0; i < State.races.Count; i++)
                if (i != raceIndex && !State.races[i].done && State.races[i].entryId == h.id) { reason = "다른 경주에 등록됨"; return false; }
            return true;
        }

        public IList<HorseData> Runners(int raceIndex)
        {
            var rs = Race(raceIndex);
            runBuffer.Clear();
            for (int i = 0; i < rs.field.Count; i++)
            {
                HorseData mine = (i == rs.playerSlot && rs.entryId > 0) ? FindHorse(rs.entryId) : null;
                runBuffer.Add(mine ?? rs.field[i]);
            }
            return runBuffer;
        }

        public ParimutuelMarket Market(int raceIndex)
        {
            ParimutuelMarket m;
            if (markets.TryGetValue(raceIndex, out m)) return m;
            var rs = Race(raceIndex);
            m = new ParimutuelMarket();
            // 같은 시드 → 저장/불러오기 후에도 같은 NPC 풀
            m.Build(Runners(raceIndex), Data.races[raceIndex].distance, B, new Random(rs.marketSeed));
            for (int i = 0; i < rs.slip.Count; i++) m.Add(rs.slip[i].type, rs.slip[i].key, rs.slip[i].amount);
            markets[raceIndex] = m;
            return m;
        }

        public float Odds(int raceIndex, BetType t, string key, float extra)
        {
            return Market(raceIndex).Odds(t, key, extra, B.takeRate);
        }

        // ===== 하루 진행 =====
        public void NextDay()
        {
            // 경주는 매일 열린다: 관람하지 않은 경주는 하루가 끝날 때 자동 진행 (마권도 정산됨)
            for (int i = 0; i < State.races.Count; i++)
                if (!State.races[i].done) RunRace(i, false);

            State.day++;
            int eventHorse = 0;
            for (int i = 0; i < State.horses.Count; i++)
            {
                var h = State.horses[i];
                if (h.IsTraining)
                {
                    h.fatigue = Math.Max(0f, h.fatigue - B.trainingRecovery);
                    h.trainLeft--;
                    if (h.trainLeft <= 0) FinishTraining(h);
                    else if (eventHorse == 0 && RngUtil.Next(rng) < B.trainingEventChance) eventHorse = h.id;
                }
                else h.fatigue = Math.Max(0f, h.fatigue - (h.retired ? B.retiredRecovery : B.restRecovery));

                h.condition = RngUtil.Clamp(h.condition + (70f - h.condition) * 0.2f + RngUtil.Range(rng, -6, 6), 20f, 100f);

                if (h.injury > 0)
                {
                    h.injuryDays++;
                    if (h.injury == 1 && h.injuryDays >= B.minorHealDays && !h.IsTraining)
                    { h.injury = 0; h.injuryDays = 0; AddLog("「" + h.name + "」 부상이 자연 회복되었습니다."); }
                    else if (h.injury == 2 && h.injuryDays >= B.majorImproveDays)
                    { h.injury = 1; h.injuryDays = 0; AddLog("「" + h.name + "」 중상이 경상으로 호전되었습니다."); }
                }
            }
            GenerateDay();
            if (IsGrandPrixDay(State.day)) AddLog("오늘은 대상경주(G1) 개최일입니다! 상금 " + B.grandPrixPrizeMultiplier + "배.");
            RaiseChanged();

            if (eventHorse != 0 && Data.events.Length > 0 && TrainingEventTriggered != null)
                TrainingEventTriggered(eventHorse, Data.events[rng.Next(Data.events.Length)]);
            CheckBankrupt();
        }

        void GenerateDay()
        {
            // 게시판용: 어제 결과 보관
            if (State.races.Count > 0)
            {
                State.lastResults.Clear();
                for (int r = 0; r < State.races.Count; r++)
                {
                    var rs = State.races[r];
                    if (!rs.done || rs.result.rows.Count < 3) continue;
                    var sb = new StringBuilder();
                    sb.Append(Data.races[r].displayName).Append(rs.grandPrix ? " G1" : "").Append(" (").Append(State.day).Append("일차)  ");
                    for (int k = 0; k < 3; k++)
                    {
                        var row = rs.result.rows[k];
                        sb.Append(k + 1).Append("착 ").Append(row.lane).Append("번 ").Append(row.name).Append(k < 2 ? " / " : "");
                    }
                    State.lastResults.Add(sb.ToString());
                }
            }
            State.lots.Clear();
            for (int i = 0; i < B.lotCount; i++) State.lots.Add(MakeLot());

            State.races.Clear();
            markets.Clear();
            // 어제 게시판에 예고한 출주마가 오늘 그대로 나온다
            if (State.tomorrow != null && State.tomorrow.Count == Data.races.Length && State.tomorrow[0].field.Count == RanchConst.RunnerCount)
                State.races.AddRange(State.tomorrow);
            else
                for (int r = 0; r < Data.races.Length; r++) State.races.Add(MakeRaceDay(r, State.day));
            State.tomorrow = new List<RaceDayState>();
            for (int r = 0; r < Data.races.Length; r++) State.tomorrow.Add(MakeRaceDay(r, State.day + 1));
        }

        RaceDayState MakeRaceDay(int r, int day)
        {
            bool gp = IsGrandPrixDay(day);
            var rs = new RaceDayState { raceIndex = r, grandPrix = gp, playerSlot = rng.Next(RanchConst.RunnerCount), marketSeed = rng.Next(), raceSeed = rng.Next() };
            for (int i = 0; i < RanchConst.RunnerCount; i++) rs.field.Add(MakeNpc(r, gp, i, day));
            return rs;
        }

        LotData MakeLot()
        {
            State.lotSerial++;
            return new LotData
            {
                no = State.lotSerial, male = rng.Next(2) == 0, coat = rng.Next(Data.coats.Length),
                stats = BlindStats(), price = LotPrice
            };
        }

        /// <summary>블라인드 말 스탯: 합계 25~35, 개별 1~12. 가중치를 제곱해 가끔 특화형이 나오게 한다.</summary>
        float[] BlindStats()
        {
            int total = RngUtil.Range(rng, B.blindStatMin, B.blindStatMax);
            var s = new float[RanchConst.StatCount];
            var w = new float[RanchConst.StatCount];
            float sw = 0f;
            for (int i = 0; i < s.Length; i++) { s[i] = 1f; w[i] = (float)Math.Pow(rng.NextDouble(), 2.2) + 0.05f; sw += w[i]; }
            int left = total - s.Length, guard = 0;
            while (left > 0 && guard++ < 1000)
            {
                float r = RngUtil.Next(rng) * sw; int k = 0;
                while (k < w.Length - 1 && r > w[k]) { r -= w[k]; k++; }
                if (s[k] < B.blindStatEach) { s[k]++; left--; }
            }
            return s;
        }

        HorseData MakeNpc(int raceIndex, bool gp, int lane, int day)
        {
            // 날짜에 따라 강해지되 상한에서 멈춤 + 대상경주 보너스
            float baseTotal = Math.Min(B.npcMaxTotal, B.npcBaseTotal + day * B.npcGrowthPerDay) + (gp ? B.grandPrixNpcBonus : 0f);
            float total = RngUtil.Clamp(baseTotal + RngUtil.Range(rng, -(int)B.npcSpread, (int)B.npcSpread), 18f, B.statCap * RanchConst.StatCount * 0.9f);
            var w = new float[RanchConst.StatCount];
            for (int i = 0; i < w.Length; i++) w[i] = 0.5f + RngUtil.Next(rng);
            // 경주 성격에 맞는 스탯을 키운 NPC
            var growth = Data.races[raceIndex].growthStats;
            for (int i = 0; i < growth.Length; i++) w[(int)growth[i]] *= 1.35f;
            float sw = 0f; for (int i = 0; i < w.Length; i++) sw += w[i];

            var h = new HorseData { id = -(lane + 1), name = GenName(), male = rng.Next(2) == 0, coat = rng.Next(Data.coats.Length) };
            for (int i = 0; i < w.Length; i++)
                h.stats[i] = (float)Math.Round(RngUtil.Clamp(1f + (total - 6f) * w[i] / sw, 1f, B.statCap) * 10f) / 10f;
            h.condition = RngUtil.Range(rng, 55, 90);
            h.fatigue = RngUtil.Range(rng, 0, 20);
            float lvl = (total - baseTotal) / 8f;
            for (int i = 0; i < 3; i++) h.form.Add((int)RngUtil.Clamp((float)Math.Round(4.5f - lvl * 2f + RngUtil.Gauss(rng) * 1.8f), 1f, 8f));
            return h;
        }

        HorseData MakeHorse(bool? male)
        {
            return new HorseData
            {
                id = State.nextId++, name = GenName(), male = male.HasValue ? male.Value : rng.Next(2) == 0,
                coat = rng.Next(Data.coats.Length)
            };
        }

        string GenName()
        {
            string n = "말";
            for (int tries = 0; tries < 20; tries++)
            {
                n = Data.namePrefixes[rng.Next(Data.namePrefixes.Length)] + Data.nameSuffixes[rng.Next(Data.nameSuffixes.Length)];
                bool dup = false;
                if (State != null)
                    for (int i = 0; i < State.horses.Count; i++) if (State.horses[i].name == n) { dup = true; break; }
                if (!dup) break;
            }
            return n;
        }

        // ===== 훈련 =====
        /// <summary>훈련비: 스탯이 높을수록 비싸진다</summary>
        public int TrainingCost(HorseData h, int stat)
        {
            return B.trainCostBase + B.trainCostPerStat * (int)Math.Floor(h.stats[stat]);
        }

        public TrainResult StartTraining(int horseId, int stat, bool force)
        {
            var h = FindHorse(horseId);
            if (h == null) return TrainResult.NotFound;
            if (h.retired) return TrainResult.Retired;
            if (h.IsTraining) { RaiseToast("이미 훈련 중입니다. 게이지가 찰 때까지 기다리세요."); return TrainResult.Busy; }
            if (IsEnteredAnywhere(h.id)) { RaiseToast("경주에 등록된 말은 훈련할 수 없습니다."); return TrainResult.Busy; }
            int cost = TrainingCost(h, stat);
            if (State.gold < cost) { RaiseToast("훈련비가 부족합니다 (" + Gold(cost) + ")"); return TrainResult.Busy; }
            if (h.injury > 0 && !force) return TrainResult.NeedsConfirm;
            State.gold -= cost;
            if (h.injury > 0)
            {
                float p = h.injury == 2 ? B.forcedTrainDeathMajor : B.forcedTrainDeathMinor;
                if (RngUtil.Next(rng) < p)
                {
                    Kill(h, "부상 상태로 훈련을 강행하다 쓰러져 죽었습니다.");
                    RaiseToast(h.name + " 사망");
                    RaiseChanged();
                    CheckBankrupt();
                    return TrainResult.Died;
                }
            }
            h.trainStat = stat;
            h.trainLeft = B.trainDays;
            h.trainTotal = B.trainDays;
            h.trainBoost = h.nutrient ? 1.5f : 1f;
            h.nutrient = false;
            AddLog("「" + h.name + "」 " + Data.stats[stat].trainingName + " 훈련 시작 (-" + Gold(cost) + ")");
            RaiseChanged();
            return TrainResult.Started;
        }

        void FinishTraining(HorseData h)
        {
            int k = h.trainStat;
            float cur = h.stats[k];
            // 상한에 가까울수록 성장 둔화, 컨디션·영양제 보정, 피로 70 이상이면 -30%
            float g = B.trainBaseGain * (1f - cur / (B.statCap + 2f)) * (0.85f + h.condition / 100f * 0.3f) * h.trainBoost;
            if (h.fatigue >= 70f) g *= 0.7f;
            h.stats[k] = Math.Min(B.statCap, (float)Math.Round((cur + g) * 10f) / 10f);
            h.fatigue = RngUtil.Clamp(h.fatigue + B.fatigueOnTrain * (1f - h.stats[(int)StatType.Durability] * 0.025f), 0f, 100f);
            h.trainStat = -1; h.trainLeft = 0; h.trainBoost = 1f;
            string msg = "「" + h.name + "」 " + Data.stats[k].displayName + " +" + g.ToString("0.0") + " (훈련 완료)";
            if (h.fatigue >= 90f && RngUtil.Next(rng) < B.overworkInjuryChance)
            {
                h.injury = Math.Max(h.injury, 1); h.injuryDays = 0;
                msg += " · 과로로 경상을 입었습니다";
            }
            AddLog(msg);
            CheckMaster();
        }

        /// <summary>훈련 돌발 이벤트 선택 결과. risky = 50% 도박</summary>
        public string ResolveTrainingEvent(int horseId, bool risky)
        {
            var h = FindHorse(horseId);
            if (h == null) return "말이 더 이상 마구간에 없습니다.";
            string t;
            if (risky)
            {
                if (RngUtil.Next(rng) < 0.5f)
                {
                    int k = h.IsTraining ? h.trainStat : (int)StatType.Speed;
                    h.stats[k] = Math.Min(B.statCap, h.stats[k] + 1f);
                    h.condition = RngUtil.Clamp(h.condition + 5f, 0f, 100f);
                    t = "성공! " + Data.stats[k].displayName + " +1, 컨디션 +5";
                }
                else { h.condition = RngUtil.Clamp(h.condition - 15f, 0f, 100f); t = "실패… 컨디션 -15"; }
            }
            else
            {
                if (RngUtil.Next(rng) < 0.8f) { h.condition = RngUtil.Clamp(h.condition + 10f, 0f, 100f); t = "말이 안정을 찾았습니다. 컨디션 +10"; }
                else { h.condition = RngUtil.Clamp(h.condition - 5f, 0f, 100f); t = "효과가 없었습니다. 컨디션 -5"; }
            }
            AddLog("「" + h.name + "」 돌발 이벤트: " + t);
            CheckMaster();
            RaiseChanged();
            return t;
        }

        // ===== 아이템 =====
        public bool BuyItem(ItemType type)
        {
            var it = Data.GetItem(type);
            if (it == null) return false;
            if (State.gold < it.price) { RaiseToast("골드가 부족합니다"); return false; }
            State.gold -= it.price;
            State.items[(int)type]++;
            RaiseToast(it.displayName + " 구매");
            RaiseChanged();
            return true;
        }

        public bool UseItem(int horseId, ItemType type)
        {
            var h = FindHorse(horseId);
            if (h == null || State.items[(int)type] <= 0) return false;
            switch (type)
            {
                case ItemType.Medicine:
                    if (h.injury == 0) { RaiseToast("부상이 없습니다"); return false; }
                    h.injury = 0; h.injuryDays = 0; RaiseToast(h.name + " 부상 완치");
                    break;
                case ItemType.Nutrient:
                    if (h.nutrient || h.retired) return false;
                    h.nutrient = true; RaiseToast("다음 훈련 성장량 1.5배");
                    break;
                case ItemType.EnergyDrink:
                    if (!h.IsTraining) { RaiseToast("훈련 중일 때만 쓸 수 있습니다"); return false; }
                    h.trainLeft = h.trainLeft / 2; // 남은 게이지 50% 단축
                    RaiseToast("훈련 게이지 50% 단축");
                    if (h.trainLeft <= 0) FinishTraining(h);
                    break;
            }
            State.items[(int)type]--;
            RaiseChanged();
            return true;
        }

        public bool UseGoldenTack(int horseId)
        {
            var h = FindHorse(horseId);
            if (h == null || h.tack || State.goldenTack <= 0) return false;
            State.goldenTack--;
            h.tack = true;
            for (int i = 0; i < h.stats.Length; i++) h.stats[i] = Math.Min(B.statCap, h.stats[i] + 1f);
            RaiseToast(h.name + " 황금 마구 장착 · 전 스탯 +1");
            CheckMaster();
            RaiseChanged();
            return true;
        }

        // ===== 마시장 / 판매 =====
        public bool BuyLot(int index)
        {
            if (index < 0 || index >= State.lots.Count) return false;
            var lot = State.lots[index];
            if (lot.sold) return false;
            if (State.horses.Count >= B.maxStable) { RaiseToast("마구간이 가득 찼습니다 (최대 " + B.maxStable + "두)"); return false; }
            if (State.gold < lot.price) { RaiseToast("골드가 부족합니다"); return false; }

            State.gold -= lot.price;
            lot.sold = true;
            var h = MakeHorse(lot.male);
            h.coat = lot.coat;
            Array.Copy(lot.stats, h.stats, RanchConst.StatCount);
            State.horses.Add(h);
            AddLog("마시장에서 「" + h.name + "」 구매 (합계 " + h.Total.ToString("0") + ")");

            var notes = new List<NoticeData>();
            int sp = SpecialistOf(lot.stats);
            if (sp >= 0)
            {
                bool first = !State.specialists[sp];
                State.specialists[sp] = true;
                notes.Add(new NoticeData("특화마 발견! " + Data.stats[sp].displayName + "형 " + (first ? "(신규 도감 등록)" : "(이미 등록됨)") + " · " + SpecialistCount + "/6", NoticeTone.Good));
                if (SpecialistCount >= RanchConst.StatCount) Unlock(AchievementId.HorseCollector, notes);
            }
            RaiseChanged();
            if (LotRevealed != null) LotRevealed(h, notes);
            return true;
        }

        /// <summary>말 도박꾼 판정: 정확히 한 스탯만 10 이상</summary>
        public static int SpecialistOf(float[] stats)
        {
            int found = -1;
            for (int i = 0; i < stats.Length; i++)
            {
                if (stats[i] < 10f) continue;
                if (found >= 0) return -1;
                found = i;
            }
            return found;
        }

        public bool RefreshLots()
        {
            if (State.gold < B.refreshPrice) { RaiseToast("골드가 부족합니다"); return false; }
            State.gold -= B.refreshPrice;
            State.lots.Clear();
            for (int i = 0; i < B.lotCount; i++) State.lots.Add(MakeLot());
            RaiseChanged();
            return true;
        }

        public bool Sell(int horseId)
        {
            var h = FindHorse(horseId);
            if (h == null) return false;
            if (h.IsTraining || IsEnteredAnywhere(h.id)) { RaiseToast("훈련 중이거나 출전 등록된 말은 팔 수 없습니다"); return false; }
            int price = SellPrice(h);
            State.gold += price;
            State.horses.Remove(h);
            AddLog("「" + h.name + "」 판매 " + Gold(price));
            RaiseChanged();
            CheckBankrupt();
            return true;
        }

        // ===== 교배 =====
        public bool CanBreed(HorseData h) { return h != null && h.retired && State.day - h.lastBreedDay >= B.breedCooldown; }
        public int BreedCooldownLeft(HorseData h) { return Math.Max(0, h.lastBreedDay + B.breedCooldown - State.day); }

        public bool Breed(int sireId, int damId)
        {
            var a = FindHorse(sireId); var b = FindHorse(damId);
            if (a == null || b == null || !a.male || b.male) { RaiseToast("수말과 암말을 한 마리씩 고르세요"); return false; }
            if (!CanBreed(a) || !CanBreed(b)) { RaiseToast("휴식 중인 말이 있습니다"); return false; }
            if (State.gold < B.breedFee) { RaiseToast("골드가 부족합니다"); return false; }
            if (State.horses.Count >= B.maxStable) { RaiseToast("마구간이 가득 찼습니다"); return false; }
            State.gold -= B.breedFee;
            var f = MakeHorse(null);
            for (int i = 0; i < RanchConst.StatCount; i++)
            {
                float v = (a.stats[i] + b.stats[i]) * 0.5f * B.foalInherit + RngUtil.Next(rng) * B.foalRandomMax;
                f.stats[i] = (float)Math.Round(Math.Min(B.foalStatCap, v) * 10f) / 10f;
            }
            State.horses.Add(f);
            a.lastBreedDay = b.lastBreedDay = State.day;
            AddLog("「" + a.name + "」×「" + b.name + "」 사이에서 망아지 「" + f.name + "」 탄생 (합계 " + f.Total.ToString("0.0") + ")");
            CheckMaster();
            RaiseChanged();
            if (FoalBorn != null) FoalBorn(f);
            return true;
        }

        // ===== 출전 / 마권 =====
        public bool SetEntry(int raceIndex, int horseId)
        {
            var rs = Race(raceIndex);
            if (rs.done) return false;
            if (rs.slip.Count > 0) { RaiseToast("마권 구매 후에는 출전마를 바꿀 수 없습니다"); return false; }
            int fee = EntryFee(raceIndex);
            if (horseId > 0)
            {
                var h = FindHorse(horseId);
                string reason;
                if (!CanEnter(h, raceIndex, out reason)) { RaiseToast("출전 불가: " + reason); return false; }
                int refund = rs.entryId > 0 ? fee : 0;
                if (State.gold + refund < fee) { RaiseToast("참가비가 부족합니다"); return false; }
                State.gold += refund - fee;
                rs.entryId = horseId;
            }
            else
            {
                if (rs.entryId > 0) State.gold += fee;
                rs.entryId = 0;
            }
            markets.Remove(raceIndex); // 출주마가 바뀌면 배당 재계산
            RaiseChanged();
            return true;
        }

        public bool PlaceBet(int raceIndex, BetType type, IList<int> picks, int amount)
        {
            var rs = Race(raceIndex);
            if (rs.done) return false;
            if (picks.Count != ParimutuelMarket.PickCount(type)) { RaiseToast("마번을 " + ParimutuelMarket.PickCount(type) + "개 고르세요"); return false; }
            for (int i = 0; i < picks.Count; i++)
            {
                if (picks[i] < 1 || picks[i] > RanchConst.RunnerCount) return false;
                for (int j = i + 1; j < picks.Count; j++) if (picks[i] == picks[j]) return false;
            }
            amount = amount / 100 * 100;
            if (amount < B.minBet) { RaiseToast("최소 배팅은 " + Gold(B.minBet) + "입니다"); return false; }
            if (amount > B.maxBetPerTicket) { RaiseToast("마권 1장 최대 " + Gold(B.maxBetPerTicket) + "입니다"); return false; }
            if (State.gold < amount) { RaiseToast("골드가 부족합니다"); return false; }

            if (State.tickets.Count >= B.maxTickets) { RaiseToast("마권이 너무 많습니다. 교환소에서 정리하세요 (최대 " + B.maxTickets + "장)"); return false; }

            string key = ParimutuelMarket.Key(type, picks);
            Market(raceIndex).Add(type, key, amount);
            State.gold -= amount;
            int id = State.nextTicketId++;
            // 배당 풀 기록용(slip)과 인벤토리용(tickets)을 따로 보관
            rs.slip.Add(new BetTicket { id = id, day = State.day, raceIndex = raceIndex, grandPrix = rs.grandPrix, type = type, key = key, amount = amount });
            State.tickets.Add(new BetTicket { id = id, day = State.day, raceIndex = raceIndex, grandPrix = rs.grandPrix, type = type, key = key, amount = amount });
            RaiseToast("마권 구매 · " + ParimutuelMarket.DisplayName(type) + " " + key + " · " + Gold(amount) + " (인벤토리 보관)");
            RaiseChanged();
            return true;
        }

        /// <summary>발주 전 마권 환불</summary>
        public bool CancelTicket(int ticketId)
        {
            var t = State.tickets.Find(x => x.id == ticketId);
            if (t == null || t.status != TicketStatus.Pending || t.day != State.day) return false;
            var rs = Race(t.raceIndex);
            if (rs.done) return false;
            rs.slip.RemoveAll(x => x.id == ticketId);
            State.tickets.Remove(t);
            Market(t.raceIndex).Add(t.type, t.key, -t.amount);
            State.gold += t.amount;
            RaiseToast("마권 환불 " + Gold(t.amount));
            RaiseChanged();
            return true;
        }

        public int CountTickets(TicketStatus status)
        {
            int c = 0;
            for (int i = 0; i < State.tickets.Count; i++) if (State.tickets[i].status == status) c++;
            return c;
        }

        public int PendingPayout
        {
            get { int s = 0; for (int i = 0; i < State.tickets.Count; i++) if (State.tickets[i].status == TicketStatus.Won) s += State.tickets[i].payout; return s; }
        }

        /// <summary>교환소: 적중 마권을 모두 골드로 교환</summary>
        public int RedeemWinningTickets()
        {
            int total = 0, count = 0;
            for (int i = State.tickets.Count - 1; i >= 0; i--)
            {
                var t = State.tickets[i];
                if (t.status != TicketStatus.Won) continue;
                total += t.payout; count++;
                State.tickets.RemoveAt(i);
            }
            if (count == 0) { RaiseToast("교환할 적중 마권이 없습니다"); return 0; }
            State.gold += total;
            AddLog("교환소에서 적중 마권 " + count + "장 교환 +" + Gold(total));
            RaiseToast("적중 마권 " + count + "장 교환 +" + Gold(total));
            RaiseChanged();
            return total;
        }

        /// <summary>교환소: 꽝 마권 버리기</summary>
        public int DiscardLostTickets()
        {
            int n = State.tickets.RemoveAll(t => t.status == TicketStatus.Lost);
            if (n > 0) { RaiseToast("꽝 마권 " + n + "장을 버렸습니다"); RaiseChanged(); }
            return n;
        }

        /// <summary>정보통 NPC: 배당과 컨디션으로 본 예상 (유료). 골드 부족이나 종료된 경주면 null</summary>
        public string BuyTip(int raceIndex)
        {
            var rs = Race(raceIndex);
            if (rs.done) return null;
            if (State.gold < B.tipPrice) { RaiseToast("골드가 부족합니다"); return null; }
            State.gold -= B.tipPrice;
            var run = Runners(raceIndex);
            var idx = new List<int>();
            var odds = new float[run.Count];
            for (int i = 0; i < run.Count; i++) { idx.Add(i); odds[i] = Odds(raceIndex, BetType.Win, (i + 1).ToString(), 0f); }
            idx.Sort((a, b) => odds[a].CompareTo(odds[b]));
            // 복병: 배당 6배 이상 중 (스탯 합계 + 컨디션) 최고
            int dark = -1; float best = float.MinValue;
            for (int i = 0; i < run.Count; i++)
            {
                if (odds[i] < 6f) continue;
                float score = run[i].Total + run[i].condition * 0.2f;
                if (score > best) { best = score; dark = i; }
            }
            var sb = new StringBuilder();
            sb.Append(Data.races[raceIndex].displayName).Append(" 정보야.\n");
            sb.Append("인기 1위는 ").Append(idx[0] + 1).Append("번 ").Append(run[idx[0]].name).Append(" (단승 ").Append(odds[idx[0]].ToString("0.0")).Append("배), ");
            sb.Append("2위는 ").Append(idx[1] + 1).Append("번 ").Append(run[idx[1]].name).Append(".\n");
            if (dark >= 0) sb.Append("근데 말이야, ").Append(dark + 1).Append("번 ").Append(run[dark].name).Append(" 컨디션이 ").Append(Math.Round(run[dark].condition)).Append("이라더군. 배당 ").Append(odds[dark].ToString("0.0")).Append("배면 노려볼 만해.");
            else sb.Append("이번엔 이변 없을 거야. 인기마 위주로 가.");
            RaiseChanged();
            return sb.ToString();
        }

        /// <summary>등록된 내 말의 부상 단계 (0이면 경고 없이 출발 가능)</summary>
        public int EntryInjury(int raceIndex)
        {
            var h = FindHorse(Race(raceIndex).entryId);
            return h == null ? 0 : h.injury;
        }

        /// <summary>경주 진행. watched=false면 재생 없이 결과만 (하루가 끝날 때 자동 진행)</summary>
        public void RunRace(int raceIndex, bool watched = true)
        {
            var rs = Race(raceIndex);
            if (rs.done) return;
            var def = Data.races[raceIndex];
            var market = Market(raceIndex); // 배당 확정 (정산 전에 풀 확보)
            var run = new List<HorseData>(Runners(raceIndex));
            var me = FindHorse(rs.entryId);
            int mi = me != null ? rs.playerSlot : -1;

            // 행운(분수 동전): luckChance 확률로 내 마권 하나가 맞도록 '순풍'을 불어 준다.
            // 마권에 적힌 말들에게 순서대로 힘을 실어 주고(1착 > 2착 > 3착), 그 마권이 맞는 시드를 찾는다.
            int seed = rs.raceSeed;
            bool lucky = false;
            if (State.luckDay == State.day && RngUtil.Next(rng) < B.luckChance)
            {
                var mine = State.tickets.FindAll(t => t.status == TicketStatus.Pending && t.day == State.day && t.raceIndex == raceIndex);
                if (mine.Count > 0)
                {
                    var target = mine[rng.Next(mine.Count)];
                    var boosted = WithTailwind(run, target);
                    for (int k = 1; k <= 400 && !lucky; k++)
                    {
                        int trialSeed = unchecked(seed + k * 7919);
                        var trial = SimulateSeeded(boosted, def.distance, mi, me, trialSeed, false);
                        if (IsWinningTicket(target, Finishers(trial))) { seed = trialSeed; run = boosted; lucky = true; }
                    }
                }
            }
            int collapseIdx;
            var res = SimulateSeeded(run, def.distance, mi, me, seed, true, out collapseIdx);
            var result = new RaceResultData();
            for (int p = 0; p < res.order.Length; p++)
            {
                int ix = res.order[p];
                result.rows.Add(new ResultRow
                {
                    position = res.dnf[ix] ? "중지" : (p + 1).ToString(),
                    lane = ix + 1, name = run[ix].name, mine = ix == mi,
                    time = res.dnf[ix] ? "-" : FormatTime(res.times[ix])
                });
            }
            var notes = result.notes;
            string raceName = def.displayName + (rs.grandPrix ? " 대상경주(G1)" : "");
            if (lucky) notes.Add(new NoticeData("분수의 행운이 따랐다! 마권이 맞아떨어졌다.", NoticeTone.Good));

            // 내 말 정산
            if (me != null)
            {
                int pos = Array.IndexOf(res.order, mi) + 1;
                if (collapseIdx >= 0)
                {
                    Kill(me, raceName + " 경주 도중 쓰러져 죽었습니다.");
                    notes.Add(new NoticeData(me.name + "이(가) 부상을 안고 달리다 경주 중 쓰러졌습니다. 사망.", NoticeTone.Bad));
                }
                else
                {
                    me.races++;
                    me.lastRaceDay = State.day;
                    me.form.Add(pos);
                    if (me.form.Count > 10) me.form.RemoveAt(0);
                    int prize = Prize(raceIndex, pos);
                    if (prize > 0) { State.gold += prize; notes.Add(new NoticeData(me.name + " " + pos + "착 · 상금 " + Gold(prize), NoticeTone.Good)); }
                    else notes.Add(new NoticeData(me.name + " " + pos + "착", NoticeTone.Normal));

                    if (pos == 1)
                    {
                        me.wins++;
                        me.condition = RngUtil.Clamp(me.condition + 5f, 0f, 100f);
                        Unlock(AchievementId.FirstWin, notes);
                        if (rs.grandPrix)
                        {
                            me.grandPrixWins++;
                            State.grandPrixWon[raceIndex] = true;
                            notes.Add(new NoticeData(def.displayName + " 대상경주 제패!", NoticeTone.Good));
                        }
                    }
                    else me.condition = RngUtil.Clamp(me.condition - 8f, 0f, 100f);

                    if (pos == 2)
                    {
                        me.seconds[raceIndex]++;
                        // 콩의 말: 한 마리가 1위 없이 모든 대회에서 2위
                        bool allSeconds = me.wins == 0;
                        for (int k = 0; k < me.seconds.Length; k++) if (me.seconds[k] == 0) allSeconds = false;
                        if (allSeconds) Unlock(AchievementId.SilverHorse, notes);
                    }

                    me.fatigue = RngUtil.Clamp(me.fatigue + B.fatigueOnRace * (1f - me.stats[(int)StatType.Durability] * 0.025f), 0f, 100f);
                    for (int k = 0; k < def.growthStats.Length; k++)
                    {
                        int s = (int)def.growthStats[k];
                        me.stats[s] = Math.Min(B.statCap, (float)Math.Round((me.stats[s] + B.raceStatGrowth) * 10f) / 10f);
                    }
                    // 에너지 고갈 상태로 달린 시간이 길수록 부상 위험 (내구력이 방어)
                    float ip = (0.02f + res.exhausted[mi] * 0.006f) * (1f - me.stats[(int)StatType.Durability] / 25f);
                    if (RngUtil.Next(rng) < ip)
                    {
                        int sev = RngUtil.Next(rng) < 0.3f + res.exhausted[mi] * 0.01f ? 2 : 1;
                        me.injury = Math.Max(me.injury, sev); me.injuryDays = 0;
                        notes.Add(new NoticeData(me.name + " " + (sev == 2 ? "중상" : "경상") + "을 입었습니다. 치료약 없이 무리하면 사망할 수 있습니다.", NoticeTone.Bad));
                    }
                    if (me.races >= B.retireAt)
                    {
                        me.retired = true;
                        notes.Add(new NoticeData(me.name + "이(가) " + B.retireAt + "전을 채우고 은퇴했습니다. 교배소에서 교배할 수 있습니다.", NoticeTone.Normal));
                    }
                    AddLog(raceName + " 출전 「" + me.name + "」 " + pos + "착");
                    CheckMaster();
                }
            }

            // 마권 정산: 인벤토리의 마권 상태만 바꾸고, 골드는 교환소에서 받는다
            var fin = new List<int>();
            for (int p = 0; p < res.order.Length; p++) if (!res.dnf[res.order[p]]) fin.Add(res.order[p] + 1);
            int spent = 0, got = 0, count = 0;
            for (int i = 0; i < State.tickets.Count; i++)
            {
                var b = State.tickets[i];
                if (b.status != TicketStatus.Pending || b.day != State.day || b.raceIndex != raceIndex) continue;
                count++;
                spent += b.amount;
                if (!IsWinningTicket(b, fin)) { b.status = TicketStatus.Lost; continue; }
                float od = market.Odds(b.type, b.key, 0f, B.takeRate);
                b.status = TicketStatus.Won;
                b.odds = od;
                b.payout = (int)Math.Floor(b.amount * od);
                got += b.payout;
                notes.Add(new NoticeData("적중! " + ParimutuelMarket.DisplayName(b.type) + " " + b.key + " · " + od.ToString("0.0") + "배 · " + Gold(b.payout), NoticeTone.Good));
                if (b.type == BetType.Trifecta) Unlock(AchievementId.KingOfGamblers, notes);
                if (od >= 50f) Unlock(AchievementId.Longshot, notes);
            }
            if (count > 0)
                notes.Add(new NoticeData("마권 " + count + "장 · 구매 " + Gold(spent) + " · 적중 " + Gold(got) + (got > 0 ? " → 도박장 교환소에서 교환하세요" : ""), NoticeTone.Normal));

            // 게임 목표: 단거리·장거리 대상경주 모두 제패
            bool allGp = true;
            for (int i = 0; i < State.grandPrixWon.Length; i++) if (!State.grandPrixWon[i]) allGp = false;
            bool justCleared = false;
            if (allGp && !State.cleared)
            {
                State.cleared = true; justCleared = true;
                Unlock(AchievementId.GrandSlam, notes);
            }

            rs.done = true;
            rs.result = result;
            AddLog(raceName + " 결과: 1착 " + fin[0] + "번 " + run[fin[0] - 1].name);

            var replay = new RaceReplay
            {
                raceIndex = raceIndex, grandPrix = rs.grandPrix, distance = def.distance, dt = res.dt,
                frameCount = res.frameCount, frames = res.frames, mine = mi, result = result
            };
            for (int i = 0; i < run.Count; i++) { replay.names[i] = run[i].name; replay.coats[i] = run[i].coat; }

            RaiseChanged();
            if (watched) { if (RaceFinished != null) RaceFinished(replay); }
            else if (RaceAutoRun != null) RaceAutoRun(raceIndex, result);
            if (justCleared && GameCleared != null) GameCleared();
            CheckBankrupt();
        }

        // ===== 시드 고정 경주 =====
        RaceSimResult SimulateSeeded(IList<HorseData> run, float distance, int mi, HorseData me, int seed, bool record)
        {
            int c;
            return SimulateSeeded(run, distance, mi, me, seed, record, out c);
        }

        /// <summary>같은 시드면 같은 결과 (부상마 낙마 판정 포함). 탐색용(record=false)은 거친 틱으로 빠르게</summary>
        RaceSimResult SimulateSeeded(IList<HorseData> run, float distance, int mi, HorseData me, int seed, bool record, out int collapseIdx)
        {
            var r = new Random(seed);
            collapseIdx = -1; float collapseAt = 0f;
            if (me != null && me.injury > 0)
            {
                float p = me.injury == 2 ? B.forcedRaceDeathMajor : B.forcedRaceDeathMinor;
                if (RngUtil.Next(r) < p) { collapseIdx = mi; collapseAt = 0.35f + RngUtil.Next(r) * 0.45f; }
            }
            return RaceSimulator.Simulate(run, distance, 0.1f, record, collapseIdx, collapseAt, B.statCap, r);
        }

        /// <summary>행운용 복사본: 마권에 적힌 말을 순서대로 강하게 (원본 데이터는 그대로)</summary>
        List<HorseData> WithTailwind(IList<HorseData> run, BetTicket t)
        {
            var picks = new List<int>();
            foreach (var part in t.key.Split('>', '-')) picks.Add(int.Parse(part) - 1);
            var copy = new List<HorseData>(run.Count);
            for (int i = 0; i < run.Count; i++)
            {
                int rank = picks.IndexOf(i);
                if (rank < 0) { copy.Add(run[i]); continue; }
                var h = new HorseData { id = run[i].id, name = run[i].name, coat = run[i].coat, male = run[i].male, condition = 100f, fatigue = 0f };
                float k = rank == 0 ? 1.45f : rank == 1 ? 1.3f : 1.15f;
                // 순서 없는 승식은 고른 말 모두 비슷하게
                if (!ParimutuelMarket.IsOrdered(t.type)) k = 1.35f;
                for (int s = 0; s < RanchConst.StatCount; s++) h.stats[s] = run[i].stats[s] * k + 2f;
                copy.Add(h);
            }
            return copy;
        }

        static List<int> Finishers(RaceSimResult res)
        {
            var fin = new List<int>();
            for (int p = 0; p < res.order.Length; p++) if (!res.dnf[res.order[p]]) fin.Add(res.order[p] + 1);
            return fin;
        }

        /// <summary>오늘 이 경주의 실제 1착 마번 (도박 고수용, 행운 효과는 반영하지 않음)</summary>
        public int PredictWinner(int raceIndex)
        {
            var rs = Race(raceIndex);
            var me = FindHorse(rs.entryId);
            var run = new List<HorseData>(Runners(raceIndex));
            var res = SimulateSeeded(run, Data.races[raceIndex].distance, me != null ? rs.playerSlot : -1, me, rs.raceSeed, false);
            var fin = Finishers(res);
            return fin.Count > 0 ? fin[0] : 1;
        }

        // ===== 분수 / 도박 고수 =====
        public bool IsLucky { get { return State.luckDay == State.day; } }

        public bool ThrowCoin()
        {
            if (IsLucky) { RaiseToast("오늘은 이미 동전을 던졌습니다"); return false; }
            if (State.gold < B.coinPrice) { RaiseToast("동전 던질 돈도 없습니다"); return false; }
            State.gold -= B.coinPrice;
            State.luckDay = State.day;
            AddLog("분수에 동전을 던졌다. 오늘은 운이 좋을 것 같다!");
            RaiseChanged();
            return true;
        }

        /// <summary>숨은 도박 고수의 예언. 하루 1번, 경주마다 masterTruthChance 확률로만 진짜</summary>
        public string AskMaster()
        {
            if (!IsAchieved(AchievementId.HiddenMaster)) Unlock(AchievementId.HiddenMaster, null);
            if (State.masterDay != State.day)
            {
                State.masterDay = State.day;
                State.masterTips.Clear();
                for (int r = 0; r < Data.races.Length; r++)
                {
                    if (Race(r).done) { State.masterTips.Add(Data.races[r].displayName + "은 이미 끝났지."); continue; }
                    int truth = PredictWinner(r);
                    int said = truth;
                    if (RngUtil.Next(rng) >= B.masterTruthChance)
                        while (said == truth) said = rng.Next(RanchConst.RunnerCount) + 1;
                    var run = Runners(r);
                    State.masterTips.Add(Data.races[r].displayName + "은 " + said + "번 " + run[said - 1].name + "이(가) 1등이야.");
                }
                AddLog("숲속의 도박 고수에게 예언을 들었다.");
                RaiseChanged();
            }
            return string.Join("\n", State.masterTips.ToArray()) + "\n...뭐, 셋 중 하나는 맞더라고. 흐흐.";
        }

        static bool IsWinningTicket(BetTicket b, List<int> fin)
        {
            switch (b.type)
            {
                case BetType.Win: return fin.Count > 0 && b.key == fin[0].ToString();
                case BetType.Place:
                    for (int i = 0; i < Math.Min(3, fin.Count); i++) if (b.key == fin[i].ToString()) return true;
                    return false;
                case BetType.Quinella:
                case BetType.Exacta:
                    return fin.Count >= 2 && b.key == ParimutuelMarket.Key(b.type, fin.GetRange(0, 2));
                default:
                    return fin.Count >= 3 && b.key == ParimutuelMarket.Key(b.type, fin.GetRange(0, 3));
            }
        }

        // ===== 업적 / 공통 =====
        void Unlock(AchievementId id, List<NoticeData> notes)
        {
            if (IsAchieved(id)) return;
            State.achievementDays[(int)id] = State.day;
            var a = Data.GetAchievement(id);
            if (a == null) return;
            if (a.rewardGold > 0) State.gold += a.rewardGold;
            if (!string.IsNullOrEmpty(a.rewardTitle)) State.titles.Add(a.rewardTitle);
            // 업적별 특수 보상
            switch (id)
            {
                case AchievementId.KingOfGamblers: State.goldenTack++; break;
                case AchievementId.HorseHunter:
                    State.discount = true;
                    for (int i = 0; i < State.lots.Count; i++) if (!State.lots[i].sold) State.lots[i].price = LotPrice;
                    break;
                case AchievementId.HorseCollector:
                    var h = MakeHorse(true);
                    h.name = "명문혈통 " + h.name;
                    for (int i = 0; i < RanchConst.StatCount; i++) h.stats[i] = 12f;
                    State.horses.Add(h);
                    break;
            }
            AddLog("업적 달성: 「" + a.displayName + "」 · " + a.rewardText);
            if (notes != null) notes.Add(new NoticeData("업적 달성: 「" + a.displayName + "」 · 보상 " + a.rewardText, NoticeTone.Good));
            if (AchievementUnlocked != null) AchievementUnlocked(a);
        }

        void CheckMaster()
        {
            for (int i = 0; i < State.horses.Count; i++)
                if (State.horses[i].Total >= 90f) { Unlock(AchievementId.MasterTrainer, null); return; }
        }

        void Kill(HorseData h, string why)
        {
            State.horses.Remove(h);
            State.deadCount++;
            for (int i = 0; i < State.races.Count; i++)
                if (State.races[i].entryId == h.id && !State.races[i].done) { State.races[i].entryId = 0; markets.Remove(i); }
            AddLog("「" + h.name + "」이(가) " + why);
            if (State.deadCount >= 10) Unlock(AchievementId.HorseHunter, null);
        }

        void CheckBankrupt()
        {
            if (State.horses.Count == 0 && State.gold < LotPrice && Bankrupt != null) Bankrupt();
        }

        void AddLog(string text)
        {
            State.logs.Insert(0, new LogEntry { day = State.day, text = text });
            if (State.logs.Count > 40) State.logs.RemoveAt(State.logs.Count - 1);
        }

        void RaiseChanged() { if (!suppressChanged && Changed != null) Changed(); }
        void RaiseToast(string t) { if (Toast != null) Toast(t); }

        public static string Gold(int v) { return v.ToString("N0") + "G"; }

        public static string FormatTime(float sec)
        {
            int m = (int)(sec / 60f);
            float s = sec - m * 60f;
            return m + ":" + s.ToString("00.0");
        }

        // ===== 디버그 (ContextMenu에서 호출) =====
        public void DebugAddGold(int v) { State.gold += v; RaiseChanged(); }

        public void DebugSkipDays(int days)
        {
            suppressChanged = true;
            for (int i = 0; i < days; i++) NextDay();
            suppressChanged = false;
            RaiseChanged();
        }

        public string Summary()
        {
            var sb = new StringBuilder();
            sb.Append("Day ").Append(State.day).Append(" Gold ").Append(State.gold).Append(" Horses ").Append(State.horses.Count);
            return sb.ToString();
        }
    }
}
