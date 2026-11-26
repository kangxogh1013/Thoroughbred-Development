using System;
using System.Collections.Generic;
using UnityEngine;

namespace HorseRanch
{
    // ===== 열거형 =====
    public enum StatType { Speed = 0, Stamina = 1, Durability = 2, Agility = 3, Grit = 4, Wisdom = 5 }
    public enum ItemType { Medicine = 0, Nutrient = 1, EnergyDrink = 2 }
    public enum BetType { Win = 0, Place = 1, Quinella = 2, Exacta = 3, Trio = 4, Trifecta = 5 }
    public enum AchievementId { SilverHorse = 0, KingOfGamblers = 1, HorseHunter = 2, HorseCollector = 3, FirstWin = 4, Longshot = 5, MasterTrainer = 6, GrandSlam = 7, HiddenMaster = 8, TreasureHunter = 9, CleanTown = 10, DebtFree = 11 }
    public enum TrainResult { Started, NotFound, Busy, Retired, NeedsConfirm, Died }
    public enum NoticeTone { Normal = 0, Good = 1, Bad = 2 }
    public enum TicketStatus { Pending = 0, Won = 1, Lost = 2 }
    /// <summary>마을 곳곳의 숨겨진 요소 (한 번만 찾을 수 있음)</summary>
    public enum SecretId { HomeShelf = 0, Notebook = 1, BarnHay = 2, Pond = 3 }
    /// <summary>시장 음식 효과</summary>
    public enum FoodEffect { Condition = 0, Fatigue = 1, PermanentStat = 2, PermanentAll = 3, SearchBoost = 4, Coffee = 5 }
    /// <summary>쓰레기통 뒤지기 결과</summary>
    public enum TrashResult { Trash = 0, Money = 1, Full = 2, Exhausted = 3 }
    /// <summary>튜토리얼 순서 (처음 시작 시 길 안내). Done = 튜토리얼 끝, 이후 목표는 그랑프리 우승</summary>
    public enum TutorialStep { TalkMom = 0, VisitStable = 1, Train = 2, Shop = 3, Market = 4, Bet = 5, Watch = 6, Bank = 7, Sleep = 8, Done = 9 }
    /// <summary>튜토리얼 '첫 구매 1회 무료' 종류</summary>
    public enum FreeKind { Training = 0, Shop = 1, Market = 2, Bet = 3 }
    /// <summary>목장 시설 (돈을 쓸 곳): 훈련 장비 · 마구간 개량 · 목초지</summary>
    public enum FacilityId { TrainingGear = 0, StableRoom = 1, Pasture = 2 }
    /// <summary>게임 오버 사유</summary>
    public enum GameOverReason { None = 0, Foreclosure = 1, Bankrupt = 2 }
    /// <summary>주민 의뢰 종류</summary>
    public enum RequestType { Deliver = 0, Trash = 1, ShowHorse = 2, RaceTop3 = 3, BetHit = 4 }
    /// <summary>시계가 넘어간 시점 (GameUI가 연출 처리)</summary>
    public enum ClockEvent { None = 0, Evening = 1, Closing = 2, Late = 3, PassOut = 4, Bedtime = 5 }

    public static class RanchConst
    {
        public const int StatCount = 6;
        public const int RunnerCount = 8;
        public const int BetTypeCount = 6;
        public const int AchievementCount = 12;
        public const int FacilityCount = 3;
        public const int FreeKindCount = 4;
        public const int ItemCount = 3;
        public const int SecretCount = 4;
    }

    // ===== 밸런스 파라미터 (BalanceConfigSO가 보관) =====
    [Serializable]
    public class BalanceConfig
    {
        [Header("시작 조건")]
        [Tooltip("새 게임 시작 골드")] public int startGold = 20000;
        [Tooltip("첫 말의 6개 스탯 초기값")] public float startStat = 5f;

        [Header("스탯 / 훈련")]
        [Tooltip("스탯 상한")] public float statCap = 20f;
        [Tooltip("훈련 1회에 걸리는 날짜 (게이지 칸 수)")] [Range(1, 7)] public int trainDays = 2;
        [Tooltip("훈련 기본 성장량 (상한에 가까울수록 감소)")] public float trainBaseGain = 1.6f;
        [Tooltip("훈련비 = 기본 + 현재 스탯 × 가중치")] public int trainCostBase = 200;
        public int trainCostPerStat = 40;
        public float fatigueOnTrain = 14f;
        public float fatigueOnRace = 20f;
        [Tooltip("훈련하지 않는 날의 피로 회복량")] public float restRecovery = 20f;
        [Tooltip("훈련 중인 날의 피로 회복량")] public float trainingRecovery = 5f;
        public float retiredRecovery = 30f;
        [Range(0f, 1f)] public float trainingEventChance = 0.08f;
        [Tooltip("피로 90 이상에서 훈련 종료 시 부상 확률")] [Range(0f, 1f)] public float overworkInjuryChance = 0.25f;
        public int minorHealDays = 4;
        public int majorImproveDays = 7;

        [Header("부상 강행 시 사망 확률")]
        [Range(0f, 1f)] public float forcedTrainDeathMinor = 0.2f;
        [Range(0f, 1f)] public float forcedTrainDeathMajor = 0.45f;
        [Range(0f, 1f)] public float forcedRaceDeathMinor = 0.25f;
        [Range(0f, 1f)] public float forcedRaceDeathMajor = 0.5f;

        [Header("마구간 / 마시장")]
        public int maxStable = 8;
        public int horsePrice = 5000;
        public int refreshPrice = 500;
        [Range(1, 3)] public int lotCount = 3;
        public int blindStatMin = 25;
        public int blindStatMax = 35;
        [Tooltip("블라인드 말 개별 스탯 최대치")] public int blindStatEach = 12;
        [Tooltip("말 사냥꾼 업적 보상 할인율")] [Range(0.1f, 1f)] public float hunterDiscount = 0.8f;

        [Header("판매가 = 기본 + 스탯합×가중치 + 우승×프리미엄")]
        public int sellBase = 2000;
        public int sellPerStat = 150;
        public int sellPerWin = 5000;

        [Header("경주 / 마권")]
        [Tooltip("공제율 (운영 수수료)")] [Range(0f, 0.5f)] public float takeRate = 0.25f;
        public int minBet = 100;
        [Tooltip("마권 1장 최대 금액")] public int maxBetPerTicket = 50000;
        [Tooltip("인벤토리 마권 최대 보관 수")] public int maxTickets = 40;
        [Tooltip("정보통 NPC 정보 가격")] public int tipPrice = 300;
        [Tooltip("분수에 던지는 동전 가격")] public int coinPrice = 100;
        [Tooltip("행운 상태에서 경주마다 내 마권 하나가 적중할 확률")] [Range(0f, 1f)] public float luckChance = 1f / 3f;
        [Tooltip("도박 고수 아저씨가 진짜 정답을 말할 확률")] [Range(0f, 1f)] public float masterTruthChance = 1f / 3f;
        [Tooltip("배당 계산용 몬테카를로 시뮬레이션 횟수")] [Range(50, 600)] public int marketSimulations = 200;
        [Tooltip("승식별 NPC 가상 배팅 풀 (단승, 연승, 복승, 쌍승, 삼복승, 삼쌍승)")]
        public float[] npcPool = { 120000f, 80000f, 90000f, 90000f, 100000f, 140000f };
        public float npcBaseTotal = 26f;
        [Tooltip("하루마다 NPC 스탯 합계 증가량")] public float npcGrowthPerDay = 0.4f;
        [Tooltip("NPC 평균 스탯 합계 상한 (플레이어가 따라잡을 수 있게)")] public float npcMaxTotal = 56f;
        public float npcSpread = 8f;
        [Tooltip("N일마다 상대 말 수준이 한 단계 오른다 (시즌)")] public int npcTierDays = 10;
        [Tooltip("시즌마다 상대 말 스탯 합계 +")] public float npcTierBonus = 3f;
        [Tooltip("시즌마다 상대 말 스탯 합계 상한 +")] public float npcTierCapBonus = 3f;
        [Tooltip("출전 시 경주 관련 스탯 성장량")] public float raceStatGrowth = 0.25f;
        public int retireAt = 15;

        [Header("대상경주 (G1) — 게임 목표")]
        [Tooltip("N일마다 대상경주 개최")] public int grandPrixInterval = 7;
        public float grandPrixPrizeMultiplier = 3f;
        public float grandPrixFeeMultiplier = 2f;
        [Tooltip("대상경주 NPC 스탯 합계 보너스")] public float grandPrixNpcBonus = 3f;

        [Header("교배")]
        public int breedFee = 3000;
        public int breedCooldown = 5;
        public float foalInherit = 0.6f;
        public float foalRandomMax = 3f;
        public float foalStatCap = 15f;

        [Header("마을 생활 · 쓰레기통")]
        [Tooltip("쓰레기통별로 나오는 돈 (인덱스 = 쓰레기통 번호). 100/500/1000/2000 중에서")]
        public int[] trashBinValues = { 100, 500, 100, 1000, 500, 2000, 100, 1000 };
        [Tooltip("뒤졌을 때 돈이 나올 확률 (나머지는 쓰레기)")] [Range(0f, 1f)] public float trashMoneyChance = 0.05f;
        [Tooltip("들고 다닐 수 있는 쓰레기 최대 개수 (쓰레기통에만 버릴 수 있음)")] public int trashCarryMax = 10;
        [Tooltip("쓰레기통 하나를 하루에 뒤질 수 있는 횟수")] public int trashSearchPerDay = 12;
        [Tooltip("연타 1회당 게이지 증가량 (1 = 한 번에 완료)")] [Range(0.02f, 1f)] public float searchPerPress = 0.12f;
        [Tooltip("초당 게이지 감소량 (느리게 누르면 손해)")] [Range(0f, 2f)] public float searchDecay = 0.25f;
        [Tooltip("청소왕 업적: 버린 쓰레기 개수")] public int cleanTownGoal = 50;

        [Header("숨겨진 요소 보상")]
        public int secretShelfGold = 3000;
        public int secretPondGold = 5000;

        [Header("스토리 · 아버지의 빚 (게임 오버 조건)")]
        [Tooltip("시작할 때 남아 있는 빚")] public int startDebt = 120000;
        [Tooltip("첫 상환일 (일차)")] public int debtFirstDay = 10;
        [Tooltip("상환 주기 (일)")] public int debtInterval = 7;
        [Tooltip("한 번에 갚아야 하는 금액")] public int debtPayment = 8000;
        [Tooltip("연체 시 빚에 더해지는 연체료")] public int latePenalty = 3000;
        [Tooltip("이 횟수만큼 연체하면 목장 압류, 게임 오버")] public int maxMissedPayments = 3;

        [Header("튜토리얼")]
        [Tooltip("첫 마권 무료 한도 (이 금액까지 공짜)")] public int freeBetMax = 1000;

        [Header("목장 시설 (레벨 1~3 가격)")]
        public int[] facilityPrices = { 15000, 30000, 60000 };
        [Tooltip("훈련 장비 레벨당 훈련 성장량 +")] [Range(0f, 0.5f)] public float gearGainPerLevel = 0.15f;
        [Tooltip("마구간 개량 레벨당 하룻밤 피로 회복 +")] public float roomRecoveryPerLevel = 8f;
        [Tooltip("목초지 레벨당 매일 컨디션 +")] public float pastureConditionPerLevel = 4f;

        [Header("스토리 그랑프리 라이벌 (흑철 목장 '흑풍')")]
        [Tooltip("라이벌 말 스탯 배율 (같은 날 NPC 평균 대비)")] public float rivalStatMultiplier = 1.18f;

        [Header("후반 콘텐츠: 아버지의 말 '천둥' (그랑프리 우승 후 깊은 숲에서 발견)")]
        [Tooltip("천둥의 스탯 (전 스탯 같은 값, 은퇴마라 경주는 못 함)")] public float thunderStat = 17f;
        [Tooltip("천둥 혈통 망아지: 스탯 보너스")] public float thunderFoalBonus = 2f;
        [Tooltip("천둥 혈통 망아지: 스탯 상한")] public float thunderFoalCap = 18f;

        [Header("시간대 (분 단위, 360 = 오전 6시)")]
        [Tooltip("하루 시작 시각")] public int dayStartMinute = 360;
        [Tooltip("저녁 알림 (노을)")] public int eveningMinute = 1080;
        [Tooltip("가게·시장·은행 문 닫는 시각, 남은 경주도 이때 마감")] public int closeMinute = 1200;
        [Tooltip("취침 시간: 주민이 모두 사라지고 집 말고는 문이 잠김 (1320 = 밤 10시)")] public int bedtimeMinute = 1320;
        [Tooltip("자정 경고")] public int lateMinute = 1440;
        [Tooltip("이 시각이 되면 쓰러져 다음 날 집에서 깸 (26:00 = 새벽 2시)")] public int passOutMinute = 1560;
        [Tooltip("쓰러진 다음 날 일어나는 시각 (늦잠)")] public int oversleepMinute = 480;
        [Tooltip("실제 1초당 흐르는 게임 분 (2 = 하루 약 10분)")] [Range(0.5f, 10f)] public float minutesPerSecond = 2f;

        [Header("주민 의뢰")]
        [Tooltip("게시판에 매일 붙는 의뢰 수")] public int requestOffers = 3;
        [Tooltip("동시에 맡을 수 있는 의뢰 수")] public int requestMaxActive = 2;
        [Tooltip("수락 후 기한 (일)")] public int requestDays = 3;
        [Tooltip("기본 보상")] public int requestBaseReward = 300;
        [Tooltip("날짜가 지날수록 보상 +")] public int requestRewardPerDay = 25;

        [Header("말과의 교감")]
        [Tooltip("교감 최대치")] public int bondMax = 10;
        [Tooltip("쓰다듬기: 컨디션 +")] public float petCondition = 5f;
        [Tooltip("교감 1당 매일 컨디션이 돌아가는 기준값 +")] public float bondConditionBonus = 1.5f;
        [Tooltip("말을 타면 이동 속도 배율")] [Range(1f, 3f)] public float rideSpeedScale = 1.8f;

        [Header("슬롯머신 (도박장)")]
        [Tooltip("고를 수 있는 판돈")] public int[] slotBets = { 100, 300, 500 };
        [Tooltip("릴 그림이 나올 확률 비중 (체리, 말굽, 종, BAR, 말, 7)")] public int[] slotWeights = { 6, 5, 4, 3, 2, 1 };
        [Tooltip("같은 그림 3개 배당 (배)")] public int[] slotTriple = { 8, 12, 20, 35, 70, 150 };
        [Tooltip("체리 2개 배당 (배, 1.5 = 판돈의 1.5배)")] public float slotCherryPair = 1.5f;
    }

    /// <summary>시장에서 파는 음식 1종. forHorse면 고른 말에게 바로 먹인다</summary>
    [Serializable]
    public class FoodDefinition
    {
        public string displayName;
        public int price;
        public bool forHorse;
        public FoodEffect effect;
        [Tooltip("효과 크기 (컨디션 +, 피로 -, 스탯 +, 배율 등)")] public float amount;
        [Tooltip("하루 구매 제한 (0 = 무제한)")] public int dailyLimit;
        [TextArea] public string description;

        public FoodDefinition() { }
        public FoodDefinition(string name, int price, bool horse, FoodEffect effect, float amount, int limit, string desc)
        {
            displayName = name; this.price = price; forHorse = horse; this.effect = effect; this.amount = amount; dailyLimit = limit; description = desc;
        }
    }

    [Serializable]
    public class StatInfo
    {
        public StatType type;
        public string displayName;
        [Tooltip("출마표용 한 글자 약칭")] public string shortName;
        [Tooltip("레이스에서의 역할")] public string role;
        public string trainingName;

        public StatInfo() { }
        public StatInfo(StatType type, string displayName, string shortName, string role, string trainingName)
        {
            this.type = type; this.displayName = displayName; this.shortName = shortName; this.role = role; this.trainingName = trainingName;
        }
    }

    [Serializable]
    public class RaceDefinition
    {
        public string displayName = "단거리";
        [Tooltip("거리 (m)")] public float distance = 1000f;
        public int entryFee = 500;
        [Tooltip("1·2·3착 상금")] public int[] prizes = { 8000, 3000, 1500 };
        public string keyStats = "반응속도 · 스피드";
        [Tooltip("출전 시 성장하는 스탯")] public StatType[] growthStats = { StatType.Speed, StatType.Agility };
    }

    [Serializable]
    public class ItemDefinition
    {
        public ItemType type;
        public string displayName;
        public int price;
        [TextArea] public string description;
    }

    [Serializable]
    public class AchievementDefinition
    {
        public AchievementId id;
        public string displayName;
        [TextArea] public string description;
        public string rewardText;
        public int rewardGold;
        [Tooltip("비워 두면 칭호 없음")] public string rewardTitle;
    }

    [Serializable]
    public class TrainingEventDefinition
    {
        [TextArea] public string text;
        [Tooltip("50% 확률 도박 선택지")] public string riskyChoice;
        [Tooltip("안전한 선택지")] public string safeChoice;

        public TrainingEventDefinition() { }
        public TrainingEventDefinition(string text, string risky, string safe) { this.text = text; riskyChoice = risky; safeChoice = safe; }
    }

    [Serializable]
    public class CoatDefinition
    {
        public string name;
        public Color color = Color.white;

        public CoatDefinition() { }
        public CoatDefinition(string name, Color color) { this.name = name; this.color = color; }
    }

    // ===== 게임 로직이 쓰는 순수 데이터 묶음 (SO에서 조립) =====
    public class GameData
    {
        public BalanceConfig balance;
        public StatInfo[] stats;
        public RaceDefinition[] races;
        public ItemDefinition[] items;
        public AchievementDefinition[] achievements;
        public TrainingEventDefinition[] events;
        public string[] namePrefixes;
        public string[] nameSuffixes;
        public CoatDefinition[] coats;
        public FoodDefinition[] foods = new FoodDefinition[0];

        public ItemDefinition GetItem(ItemType type)
        {
            for (int i = 0; i < items.Length; i++) if (items[i].type == type) return items[i];
            return null;
        }

        public AchievementDefinition GetAchievement(AchievementId id)
        {
            for (int i = 0; i < achievements.Length; i++) if (achievements[i].id == id) return achievements[i];
            return null;
        }

        public string StatName(int stat) { return stats[stat].displayName; }
    }

    // ===== 저장되는 런타임 상태 =====
    [Serializable]
    public class HorseData
    {
        public int id;                // 0 이하 = NPC
        public string name;
        public bool male;
        public int coat;
        public float[] stats = new float[RanchConst.StatCount];
        public float condition = 70f; // 0~100
        public float fatigue;         // 0~100
        public int injury;            // 0 정상, 1 경상, 2 중상
        public int injuryDays;
        public int races;
        public int wins;
        public int[] seconds = new int[2]; // 경주별 2착 횟수
        public List<int> form = new List<int>();
        public int trainStat = -1;    // -1 = 훈련 안 함
        public int trainLeft;
        public int trainTotal;
        public float trainBoost = 1f;
        public bool nutrient;
        public bool retired;
        public int lastBreedDay = -99;
        public bool tack;
        public int lastRaceDay = -1;
        public int grandPrixWins;
        [Tooltip("천둥 혈통 (천둥 본인 또는 그 망아지)")] public bool thunderBlood;
        [Tooltip("교감 (쓰다듬을 때마다 +1)")] public int bond;
        [Tooltip("마지막으로 쓰다듬은 날")] public int petDay = -1;
        [Tooltip("경주별 개인 최고 기록 (초, 0 = 없음)")] public float[] best = new float[0];

        public bool IsNpc { get { return id <= 0; } }
        public bool IsTraining { get { return trainStat >= 0; } }

        public float Total
        {
            get { float s = 0f; for (int i = 0; i < stats.Length; i++) s += stats[i]; return s; }
        }

        public string RecentForm(int count)
        {
            if (form.Count == 0) return "";
            int start = Math.Max(0, form.Count - count);
            var parts = new string[form.Count - start];
            for (int i = start; i < form.Count; i++) parts[i - start] = form[i].ToString();
            return string.Join("-", parts);
        }
    }

    [Serializable]
    public class LotData
    {
        public int no;
        public bool male;
        public int coat;
        public float[] stats = new float[RanchConst.StatCount];
        public int price;
        public bool sold;
    }

    /// <summary>마권 1장. 구매 즉시 인벤토리에 보관되고, 경주 후 적중이면 교환소에서 골드로 바꾼다</summary>
    [Serializable]
    public class BetTicket
    {
        public int id;
        public int day;
        public int raceIndex;
        public bool grandPrix;
        public BetType type;
        public string key;
        public int amount;
        [Tooltip("튜토리얼 무료로 낸 금액 (환불 시 돈 대신 무료권을 돌려줌)")] public int freeAmount;
        public TicketStatus status;
        public float odds;     // 확정 배당 (경주 후)
        public int payout;     // 적중 시 교환 금액
    }

    [Serializable]
    public class NoticeData
    {
        public string text;
        public NoticeTone tone;
        public NoticeData() { }
        public NoticeData(string text, NoticeTone tone) { this.text = text; this.tone = tone; }
    }

    [Serializable]
    public class ResultRow
    {
        public string position;
        public int lane;
        public string name;
        public bool mine;
        public string time;
    }

    [Serializable]
    public class RaceResultData
    {
        public List<ResultRow> rows = new List<ResultRow>();
        public List<NoticeData> notes = new List<NoticeData>();
    }

    [Serializable]
    public class RaceDayState
    {
        public int raceIndex;
        public bool grandPrix;
        public List<HorseData> field = new List<HorseData>();
        public int playerSlot;
        public int entryId;           // 0 = 출전 없음
        public bool done;
        public List<BetTicket> slip = new List<BetTicket>();
        public RaceResultData result = new RaceResultData();
        public int marketSeed;
        [Tooltip("경주 결과를 정하는 시드 (도박 고수 예언용으로 미리 고정)")] public int raceSeed;
    }

    [Serializable]
    public class LogEntry
    {
        public int day;
        public string text;
    }

    [Serializable]
    public class GameState
    {
        public int version = 2;
        public int day = 1;
        public int gold;
        public List<HorseData> horses = new List<HorseData>();
        public int[] items = new int[RanchConst.ItemCount];
        public int goldenTack;
        public int deadCount;
        public bool[] specialists = new bool[RanchConst.StatCount];
        public int[] achievementDays = new int[RanchConst.AchievementCount]; // 0 = 미달성
        public List<string> titles = new List<string>();
        public bool discount;
        public List<LogEntry> logs = new List<LogEntry>();
        public int nextId = 1;
        public int lotSerial;
        public List<LotData> lots = new List<LotData>();
        public List<RaceDayState> races = new List<RaceDayState>();
        public bool[] grandPrixWon = new bool[2];
        public bool cleared;
        [Tooltip("인벤토리의 마권")] public List<BetTicket> tickets = new List<BetTicket>();
        public int nextTicketId = 1;
        [Tooltip("어제 경주 결과 (게시판용)")] public List<string> lastResults = new List<string>();
        [Tooltip("내일 경주 출주마 (게시판 예고용)")] public List<RaceDayState> tomorrow = new List<RaceDayState>();
        [Tooltip("분수에 동전을 던진 날 (그날 하루 행운)")] public int luckDay = -1;
        [Tooltip("도박 고수에게 예언을 들은 날")] public int masterDay = -1;
        public List<string> masterTips = new List<string>();

        [Header("마을 생활")]
        [Tooltip("들고 있는 쓰레기")] public int trash;
        public int trashDisposed;
        public int trashMoneyFound;
        [Tooltip("쓰레기통별 오늘 뒤진 횟수 (자고 나면 초기화)")] public int[] binUses = new int[0];
        public bool[] secrets = new bool[RanchConst.SecretCount];
        [Tooltip("시장 음식별 오늘 구매 수 (자고 나면 초기화)")] public int[] foodBought = new int[0];
        public int searchBoostDay = -1;
        public int coffeeDay = -1;

        [Header("스토리 · 진행")]
        public int tutorialStep;                    // TutorialStep
        public bool introSeen;
        public List<string> tipsSeen = new List<string>();
        public bool[] freeUsed = new bool[RanchConst.FreeKindCount];
        public int debt;
        public int debtCredit;                      // 미리 갚아서 다음 상환일에 빠지는 금액
        public int missedPayments;
        public int[] facility = new int[RanchConst.FacilityCount];
        public int lastPlayerWinDay = -99;
        public int storyClearDay;
        public string storyWinner;                  // 그랑프리를 우승한 내 말 이름
        public bool endingSeen;
        public bool thunderFound;                   // 깊은 숲에서 천둥을 데려옴
        public int lastTierNotice;
        public int gameOver;                        // GameOverReason

        [Header("마을 생활 2: 시간 · 의뢰 · 기록")]
        [Tooltip("지금 시각 (분, 360 = 06:00, 1440 넘으면 다음 날 새벽)")] public float clock = 360f;
        public List<RequestData> requests = new List<RequestData>();
        public int nextRequestId = 1;
        public int requestsDone;
        [Tooltip("지금 타고 있는 말 id (0 = 걷는 중, 저장 후 불러오면 내림)")] public int ridingId;
        [Tooltip("역대 경주 결과 (최근 60개)")] public List<RaceRecord> records = new List<RaceRecord>();
        [Tooltip("경주별 트랙 레코드")] public List<RaceRecord> trackRecords = new List<RaceRecord>();
        public int slotSpins;
        public int slotBestWin;
    }

    /// <summary>주민 의뢰 1건 (게시판 제안 + 수락한 것)</summary>
    [Serializable]
    public class RequestData
    {
        public int id;
        public int type;                // RequestType
        public string giver;
        public string story;            // 의뢰 사연 한 줄
        public int param = -1;          // 배달: ItemType / 말 보여주기: 스탯 (-1 = 능력 총합)
        public int amount;              // 개수 / 기준치
        public int progress;
        public int baseline;            // 쓰레기: 수락할 때의 누적 버린 수
        public int reward;
        public int rewardItem = -1;     // 보상 아이템 (ItemType, -1 = 없음)
        public int deadline;            // 이 날까지 (포함)
        public bool accepted;
    }

    /// <summary>경주 1회 기록 (기록실)</summary>
    [Serializable]
    public class RaceRecord
    {
        public int day;
        public int raceIndex;
        public bool grandPrix;
        public string winner;
        public float time;
        public bool mineWon;
        public string mine;             // 내 출전마 이름 (없으면 빈 문자열)
        public int myPos;
    }

    /// <summary>경주 재생용 데이터 (저장하지 않음)</summary>
    public class RaceReplay
    {
        public int raceIndex;
        public bool grandPrix;
        public float distance;
        public float dt;
        public int frameCount;
        public float[] frames;   // frameCount × 8, 프레임별 각 말의 위치(m)
        public string[] names = new string[RanchConst.RunnerCount];
        public int[] coats = new int[RanchConst.RunnerCount];
        public int mine = -1;    // 내 말의 마번 인덱스
        public RaceResultData result;

        public float Position(int frame, int lane) { return frames[frame * RanchConst.RunnerCount + lane]; }
    }
}
