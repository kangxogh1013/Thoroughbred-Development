using UnityEngine;

namespace HorseRanch
{
    /// <summary>업적 1종. 판정 조건은 id로 RanchGame에서 처리하고, 이름·보상은 여기서 조정</summary>
    [CreateAssetMenu(fileName = "Achievement", menuName = "HorseRanch/Achievement Definition")]
    public class AchievementDefinitionSO : ScriptableObject
    {
        [SerializeField] private AchievementDefinition achievement = new AchievementDefinition();
        public AchievementDefinition Achievement { get { return achievement; } }
    }
}
