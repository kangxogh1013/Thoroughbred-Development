using System.Collections.Generic;
using UnityEngine;

namespace HorseRanch
{
    /// <summary>모든 데이터 SO를 묶는 진입점. RanchGameRunner에 이 에셋 하나만 할당한다</summary>
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "HorseRanch/Game Database")]
    public class GameDatabaseSO : ScriptableObject
    {
        [SerializeField] private BalanceConfigSO balance;
        [SerializeField] private StatTableSO statTable;
        [SerializeField] private ContentTableSO content;
        [Tooltip("0 = 단거리, 1 = 장거리 (순서 유지)")]
        [SerializeField] private RaceDefinitionSO[] races;
        [SerializeField] private ItemDefinitionSO[] items;
        [SerializeField] private AchievementDefinitionSO[] achievements;

        public ContentTableSO Content { get { return content; } }

        /// <summary>SO 참조를 게임 로직용 순수 데이터로 변환</summary>
        public GameData BuildData()
        {
            var d = new GameData
            {
                balance = balance != null ? balance.Config : new BalanceConfig(),
                stats = statTable != null ? statTable.Stats : new StatInfo[0],
                events = content != null ? content.TrainingEvents : new TrainingEventDefinition[0],
                namePrefixes = content != null ? content.NamePrefixes : new[] { "무명" },
                nameSuffixes = content != null ? content.NameSuffixes : new[] { "마" },
                coats = content != null ? content.Coats : new[] { new CoatDefinition("밤색", Color.white) },
                foods = content != null && content.Foods != null ? content.Foods : new FoodDefinition[0],
            };
            var r = new List<RaceDefinition>();
            foreach (var x in races) if (x != null) r.Add(x.Race);
            d.races = r.ToArray();
            var it = new List<ItemDefinition>();
            foreach (var x in items) if (x != null) it.Add(x.Item);
            d.items = it.ToArray();
            var a = new List<AchievementDefinition>();
            foreach (var x in achievements) if (x != null) a.Add(x.Achievement);
            d.achievements = a.ToArray();
            return d;
        }

        public Color CoatColor(int index)
        {
            if (content == null || index < 0 || index >= content.Coats.Length) return Color.white;
            return content.Coats[index].color;
        }

        public string CoatName(int index)
        {
            if (content == null || index < 0 || index >= content.Coats.Length) return "";
            return content.Coats[index].name;
        }
    }
}
