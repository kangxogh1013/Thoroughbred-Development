using UnityEngine;

namespace HorseRanch
{
    /// <summary>6대 스탯 이름·역할·훈련명 (순서 = StatType)</summary>
    [CreateAssetMenu(fileName = "StatTable", menuName = "HorseRanch/Stat Table")]
    public class StatTableSO : ScriptableObject
    {
        [SerializeField] private StatInfo[] stats =
        {
            new StatInfo(StatType.Speed, "스피드", "속", "최고 속도", "트랙 전력 질주"),
            new StatInfo(StatType.Stamina, "체력", "체", "에너지 총량", "장거리 지구력 구보"),
            new StatInfo(StatType.Durability, "내구력", "내", "피로·부상 방어", "타이어 끌기·언덕"),
            new StatInfo(StatType.Agility, "반응속도", "반", "출발 가속", "게이트 반응 훈련"),
            new StatInfo(StatType.Grit, "근성", "근", "스퍼트·몸싸움", "장애물·경쟁 병주"),
            new StatInfo(StatType.Wisdom, "지능", "지", "페이스 배분", "페이스 조절 훈련"),
        };
        public StatInfo[] Stats { get { return stats; } }
    }
}
