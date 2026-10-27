using UnityEngine;

namespace HorseRanch
{
    /// <summary>탭 패널 공통: 켜질 때 게임 이벤트 구독 + 새로고침, 꺼지면 구독 해제</summary>
    public abstract class RanchViewBase : MonoBehaviour
    {
        [Header("공통 참조")]
        [SerializeField] protected RanchGameRunner runner;
        [SerializeField] protected GameUI ui;

        protected RanchGame Game { get { return runner.Game; } }
        protected GameData Data { get { return runner.Game.Data; } }

        protected virtual void OnEnable()
        {
            if (runner == null || runner.Game == null) return;
            runner.Game.Changed += Refresh;
            Refresh();
        }

        protected virtual void OnDisable()
        {
            if (runner != null && runner.Game != null) runner.Game.Changed -= Refresh;
        }

        public abstract void Refresh();

        protected static string Gold(int v) { return RanchGame.Gold(v); }
    }
}
