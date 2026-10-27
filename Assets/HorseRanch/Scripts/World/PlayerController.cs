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

        private WorldArea currentArea;
        private Interactable facingTarget;

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
            if (dir != Vector2Int.zero && !mover.IsMoving)
            {
                if (!mover.TryMove(dir, GameInput.Run)) mover.Face(dir);
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
            if (dir != Vector2Int.zero && !mover.TryMove(dir, GameInput.Run)) mover.Face(dir);
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
