using System;
using System.Collections.Generic;
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
        // 저녁에 집(가까운 문)으로 걸어가는 경로
        private List<Vector2Int> route;
        private int routeIndex;
        private float blockedTime;
        private Action routeDone;

        public bool OnRoute { get { return route != null; } }
        public GridMover Mover { get { return mover; } }

        /// <summary>경로를 따라 걷고 끝나면 done 호출 (막혀서 2.5초 못 움직여도 끝난 것으로)</summary>
        public void FollowRoute(List<Vector2Int> path, Action done)
        {
            route = path; routeIndex = 0; blockedTime = 0f; routeDone = done;
        }

        public void CancelRoute() { route = null; routeDone = null; }

#if UNITY_EDITOR
        public void EditorSetup(GridMover m, RectInt area, bool stay)
        {
            mover = m; wanderArea = area; stationary = stay;
        }
#endif

        private void Start() { timer = UnityEngine.Random.Range(minWait, maxWait); }

        private void Update()
        {
            if (pauseTimer > 0f) { pauseTimer -= Time.deltaTime; return; }
            if (route != null) { StepRoute(); return; }
            if (stationary || mover.IsMoving) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = UnityEngine.Random.Range(minWait, maxWait);
            var d = Dirs[UnityEngine.Random.Range(0, Dirs.Length)];
            if (wanderArea.Contains(mover.Cell + d)) mover.TryMove(d, false);
            else mover.Face(d);
        }

        private void StepRoute()
        {
            if (mover.IsMoving) return;
            if (routeIndex >= route.Count) { FinishRoute(); return; }
            var d = route[routeIndex] - mover.Cell;
            if (d == Vector2Int.zero) { routeIndex++; return; }
            if (mover.TryMove(d, false)) { routeIndex++; blockedTime = 0f; return; }
            blockedTime += Time.deltaTime;
            if (blockedTime > 2.5f) FinishRoute();
        }

        private void FinishRoute()
        {
            var done = routeDone;
            route = null; routeDone = null;
            if (done != null) done();
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
