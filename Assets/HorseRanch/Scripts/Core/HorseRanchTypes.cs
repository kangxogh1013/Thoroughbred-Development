using System;
using System.Collections.Generic;
using UnityEngine;

namespace HorseRanch
{
    // ===== 열거형 =====
    public enum StatType { Speed = 0, Stamina = 1, Durability = 2, Agility = 3, Grit = 4, Wisdom = 5 }
    public enum ItemType { Medicine = 0, Nutrient = 1, EnergyDrink = 2 }
    public enum BetType { Win = 0, Place = 1, Quinella = 2, Exacta = 3, Trio = 4, Trifecta = 5 }
    public enum AchievementId { SilverHorse = 0, KingOfGamblers = 1, HorseHunter = 2, HorseCollector = 3, FirstWin = 4, Longshot = 5, MasterTrainer = 6, GrandSlam = 7 }
    public enum TrainResult { Started, NotFound, Busy, Retired, NeedsConfirm, Died }
    public enum NoticeTone { Normal = 0, Good = 1, Bad = 2 }

    public static class RanchConst
    {
        public const int StatCount = 6;
        public const int RunnerCount = 8;
        public const int BetTypeCount = 6;
        public const int AchievementCount = 8;
        public const int ItemCount = 3;
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
        [Tooltip("배당 계산용 몬테카를로 시뮬레이션 횟수")] [Range(50, 600)] public int marketSimulations = 200;
        [Tooltip("승식별 NPC 가상 배팅 풀 (단승, 연승, 복승, 쌍승, 삼복승, 삼쌍승)")]
        public float[] npcPool = { 120000f, 80000f, 90000f, 90000f, 100000f, 140000f };
        public float npcBaseTotal = 26f;
        [Tooltip("하루마다 NPC 스탯 합계 증가량")] public float npcGrowthPerDay = 0.4f;
        [Tooltip("NPC 평균 스탯 합계 상한 (플레이어가 따라잡을 수 있게)")] public float npcMaxTotal = 56f;
        public float npcSpread = 8f;
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

    [Serializable]
    public class BetTicket
    {
        public BetType type;
        public string key;
        public int amount;
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
