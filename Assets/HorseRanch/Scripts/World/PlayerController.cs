using UnityEngine;

namespace HorseRanch
{
    /// <summary>방향키 이동, 바라보는 칸 상호작용(E), 문 밟기, 구역 이름 표시</summary>
    [RequireComponent(typeof(GridMover))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private GridMover mover;
        [SerializeField] private WorldMap map;
        [SerializeField] private GameUI ui;

        [Header("벤치")]
        [Tooltip("앉아 있으면 이 시간마다 주민 수다가 들린다 (초)")]
        [SerializeField] [Range(1f, 10f)] private float gossipInterval = 3f;

        private WorldArea currentArea;
        private Interactable facingTarget;
        private float sitTimer;

        public GridMover Mover { get { return mover; } }

#if UNITY_EDITOR
        public void EditorSetup(GridMover m, WorldMap wm) { mover = m; map = wm; }
#endif

        private void OnEnable() { mover.Arrived += OnArrived; }
        private void OnDisable() { mover.Arrived -= OnArrived; }

        private void Update()
        {
            if (ui.IsBusy) { ui.SetPrompt(null); return; }

            var dir = GameInput.Move;
            if (mover.IsSeated)
            {
                // 앉아 있는 동안: 방향키 / E로 일어나기, 일정 시간마다 수다
                if (dir != Vector2Int.zero || GameInput.Interact) { mover.Stand(); mover.Face(dir != Vector2Int.zero ? dir : Vector2Int.down); return; }
                ui.SetPrompt("일어나기 (방향키)");
                sitTimer += Time.deltaTime;
                if (sitTimer >= gossipInterval) { sitTimer = 0f; ui.BenchGossip(); }
                return;
            }
            if (dir != Vector2Int.zero && !mover.IsMoving)
            {
                if (!mover.TryMove(dir, Running)) mover.Face(dir);
            }

            // 바라보는 칸의 대상 → 하단 안내
            facingTarget = mover.IsMoving ? null : map.InteractableAt(mover.Cell + mover.Facing);
            ui.SetPrompt(facingTarget != null ? facingTarget.DisplayName : null);

            if (facingTarget != null && GameInput.Interact) ui.Interact(facingTarget, this);
            UpdateArea();
        }

        private void OnArrived(Vector2Int cell)
        {
            var it = map.InteractableAt(cell);
            if (it != null && it.TriggerOnStep) { ui.Interact(it, this); return; }
            if (ui.IsBusy) return;
            // 키를 누르고 있으면 멈추지 않고 바로 다음 칸으로 (방향 전환도 즉시 반영)
            var dir = GameInput.Move;
            if (dir != Vector2Int.zero && !mover.TryMove(dir, Running)) mover.Face(dir);
        }

        /// <summary>Shift 또는 커피 효과</summary>
        private bool Running { get { return GameInput.Run || ui.FastWalk; } }

        /// <summary>벤치에 앉기 (seat = 벤치 위치)</summary>
        public void SitAt(Vector3 seat)
        {
            sitTimer = 0f;
            mover.Sit(seat);
        }

        private void UpdateArea()
        {
            var a = map.AreaAt(mover.Cell);
            if (a == currentArea) return;
            currentArea = a;
            if (a != null) ui.ShowLocation(a.AreaName);
        }

        public void WarpTo(Vector2Int cell, Vector2Int facing)
        {
            mover.Teleport(cell, facing);
            UpdateArea();
        }
    }
}
