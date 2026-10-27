using UnityEditor;
using UnityEngine;

namespace HorseRanch.EditorTools
{
    /// <summary>ScriptableObject 데이터 에셋 생성. 이미 있으면 값은 건드리지 않고 연결만 갱신 (overwrite=true면 기본값으로 덮어씀)</summary>
    public static class HorseRanchDataBuilder
    {
        public static GameDatabaseSO Build(bool overwrite)
        {
            bool created;
            var balance = GetOrCreate<BalanceConfigSO>(HorseRanchPaths.Data + "/BalanceConfig.asset", out created);
            if (overwrite && !created) { CopyDefaults(balance, ScriptableObject.CreateInstance<BalanceConfigSO>()); }

            var stats = GetOrCreate<StatTableSO>(HorseRanchPaths.Data + "/StatTable.asset", out created);
            if (overwrite && !created) CopyDefaults(stats, ScriptableObject.CreateInstance<StatTableSO>());

            var content = GetOrCreate<ContentTableSO>(HorseRanchPaths.Data + "/ContentTable.asset", out created);
            if (overwrite && !created) CopyDefaults(content, ScriptableObject.CreateInstance<ContentTableSO>());

            // 대회 2종
            var races = new[]
            {
                MakeRace("Race_Short", overwrite, "단거리", 1000f, 500, new[] { 8000, 3000, 1500 }, "반응속도 · 스피드", new[] { StatType.Speed, StatType.Agility }),
                MakeRace("Race_Long", overwrite, "장거리", 2400f, 800, new[] { 12000, 4500, 2000 }, "체력 · 지능 · 근성", new[] { StatType.Stamina, StatType.Wisdom, StatType.Grit }),
            };

            // 소모품 3종
            var items = new[]
            {
                MakeItem("Item_Medicine", overwrite, ItemType.Medicine, "치료약", 800, "부상 즉시 완치. 부상 강행으로 인한 사망 위험 제거"),
                MakeItem("Item_Nutrient", overwrite, ItemType.Nutrient, "영양제", 1200, "다음 훈련 성장량 1.5배"),
                MakeItem("Item_EnergyDrink", overwrite, ItemType.EnergyDrink, "에너지 드링크", 2000, "진행 중인 훈련 게이지 50% 단축"),
            };

            // 업적 8종
            var achievements = new[]
            {
                MakeAch(AchievementId.SilverHorse, overwrite, "콩의 말", "한 마리 말이 1위 없이 단거리·장거리에서 모두 2위 기록", "칭호 「만년 2등」 + 5,000G", 5000, "만년 2등"),
                MakeAch(AchievementId.KingOfGamblers, overwrite, "도박의 왕", "삼쌍승식(1·2·3위 순서대로) 적중", "황금 마구 (장착 시 전 스탯 +1)", 0, ""),
                MakeAch(AchievementId.HorseHunter, overwrite, "말 사냥꾼", "누적 사망 말 10두", "칭호 「사령관」 + 마시장 20% 할인", 0, "사령관"),
                MakeAch(AchievementId.HorseCollector, overwrite, "말 도박꾼", "마시장 구매 직후 기준, 한 스탯만 10 이상인 특화마 6종 획득", "최고급 혈통 씨수말 (전 스탯 12)", 0, ""),
                MakeAch(AchievementId.FirstWin, overwrite, "첫 우승", "대회 1위 1회", "3,000G", 3000, ""),
                MakeAch(AchievementId.Longshot, overwrite, "고배당 적중", "배당 50배 이상 마권 적중", "칭호 「복권 당첨자」", 0, "복권 당첨자"),
                MakeAch(AchievementId.MasterTrainer, overwrite, "명마 육성", "스탯 합계 90 이상 말 보유", "5,000G", 5000, ""),
                MakeAch(AchievementId.GrandSlam, overwrite, "그랑프리 제패", "단거리·장거리 대상경주(G1)를 모두 우승 — 게임 목표", "칭호 「명예 목장주」 + 20,000G", 20000, "명예 목장주"),
            };

            var db = GetOrCreate<GameDatabaseSO>(HorseRanchPaths.Data + "/GameDatabase.asset", out created);
            UIFactory.Set(db, "balance", balance);
            UIFactory.Set(db, "statTable", stats);
            UIFactory.Set(db, "content", content);
            UIFactory.SetArr(db, "races", races);
            UIFactory.SetArr(db, "items", items);
            UIFactory.SetArr(db, "achievements", achievements);

            AssetDatabase.SaveAssets();
            Debug.Log("[HorseRanch] 데이터 에셋 준비 완료: " + HorseRanchPaths.Data);
            return db;
        }

        private static T GetOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            created = a == null;
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        private static void CopyDefaults(ScriptableObject target, ScriptableObject source)
        {
            string keepName = target.name; // 에셋 이름 유지
            EditorUtility.CopySerialized(source, target);
            target.name = keepName;
            Object.DestroyImmediate(source);
            EditorUtility.SetDirty(target);
        }

        private static RaceDefinitionSO MakeRace(string file, bool overwrite, string name, float dist, int fee, int[] prizes, string key, StatType[] growth)
        {
            bool created;
            var so = GetOrCreate<RaceDefinitionSO>(HorseRanchPaths.Data + "/Races/" + file + ".asset", out created);
            if (created || overwrite)
            {
                var r = so.Race;
                r.displayName = name; r.distance = dist; r.entryFee = fee; r.prizes = prizes; r.keyStats = key; r.growthStats = growth;
                EditorUtility.SetDirty(so);
            }
            return so;
        }

        private static ItemDefinitionSO MakeItem(string file, bool overwrite, ItemType type, string name, int price, string desc)
        {
            bool created;
            var so = GetOrCreate<ItemDefinitionSO>(HorseRanchPaths.Data + "/Items/" + file + ".asset", out created);
            if (created || overwrite)
            {
                var it = so.Item;
                it.type = type; it.displayName = name; it.price = price; it.description = desc;
                EditorUtility.SetDirty(so);
            }
            return so;
        }

        private static AchievementDefinitionSO MakeAch(AchievementId id, bool overwrite, string name, string desc, string reward, int gold, string title)
        {
            bool created;
            var so = GetOrCreate<AchievementDefinitionSO>(HorseRanchPaths.Data + "/Achievements/Ach_" + (int)id + "_" + id + ".asset", out created);
            if (created || overwrite)
            {
                var a = so.Achievement;
                a.id = id; a.displayName = name; a.description = desc; a.rewardText = reward; a.rewardGold = gold; a.rewardTitle = title;
                EditorUtility.SetDirty(so);
            }
            return so;
        }
    }
}
