using UnityEngine;

namespace HorseRanch
{
    /// <summary>정해진 영역 안을 무작위로 걷는 NPC. 말을 걸면 멈추고 플레이어 쪽을 본다</summary>
    [RequireComponent(typeof(GridMover))]
    public class NpcController : MonoBehaviour
    {
        [SerializeField] private GridMover mover;
        [Tooltip("돌아다닐 수 있는 전역 타일 영역")]
        [SerializeField] private RectInt wanderArea;
        [SerializeField] private bool stationary;
        [SerializeField] [Range(0.3f, 6f)] private float minWait = 0.8f;
        [SerializeField] [Range(0.3f, 8f)] private float maxWait = 3f;

        private static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        private float timer;
        private float pauseTimer;

#if UNITY_EDITOR
        public void EditorSetup(GridMover m, RectInt area, bool stay)
        {
            mover = m; wanderArea = area; stationary = stay;
        }
#endif

        private void Start() { timer = Random.Range(minWait, maxWait); }

        private void Update()
        {
            if (pauseTimer > 0f) { pauseTimer -= Time.deltaTime; return; }
            if (stationary || mover.IsMoving) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Random.Range(minWait, maxWait);
            var d = Dirs[Random.Range(0, Dirs.Length)];
            if (wanderArea.Contains(mover.Cell + d)) mover.TryMove(d, false);
            else mover.Face(d);
        }

        /// <summary>대화 시작: 잠시 멈추고 플레이어를 바라봄</summary>
        public void TalkTo(Vector2Int playerCell, float pauseSeconds)
        {
            pauseTimer = pauseSeconds;
            var d = playerCell - mover.Cell;
            if (d.x != 0 && d.y != 0) d.y = 0;
            if (d != Vector2Int.zero) mover.Face(new Vector2Int(Mathf.Clamp(d.x, -1, 1), Mathf.Clamp(d.y, -1, 1)));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.6f);
            Gizmos.DrawWireCube(new Vector3(wanderArea.x + wanderArea.width * 0.5f, wanderArea.y + wanderArea.height * 0.5f, 0f), new Vector3(wanderArea.width, wanderArea.height, 0f));
        }
    }
}
