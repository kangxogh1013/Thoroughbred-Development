using UnityEngine;

namespace HorseRanch
{
    /// <summary>게임 전체 수치 파라미터. 인스펙터에서 바로 밸런스 조정</summary>
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "HorseRanch/Balance Config")]
    public class BalanceConfigSO : ScriptableObject
    {
        [SerializeField] private BalanceConfig config = new BalanceConfig();
        public BalanceConfig Config { get { return config; } }
    }
}
