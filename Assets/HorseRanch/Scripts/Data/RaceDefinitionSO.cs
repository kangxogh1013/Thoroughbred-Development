using UnityEngine;

namespace HorseRanch
{
    /// <summary>대회 1종 (단거리 / 장거리)</summary>
    [CreateAssetMenu(fileName = "Race", menuName = "HorseRanch/Race Definition")]
    public class RaceDefinitionSO : ScriptableObject
    {
        [SerializeField] private RaceDefinition race = new RaceDefinition();
        public RaceDefinition Race { get { return race; } }
    }
}
