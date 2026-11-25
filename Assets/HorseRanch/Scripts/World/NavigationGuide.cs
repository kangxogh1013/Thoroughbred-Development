using UnityEngine;

namespace HorseRanch
{
    /// <summary>
    /// 튜토리얼 길 안내: 플레이어 머리 위 화살표가 목표 방향을 가리키고, 목표 위에는 통통 튀는 표식이 뜬다.
    /// 목표가 다른 구역이면 GameUI가 그 구역으로 가는 문을 목표로 넘겨준다.
    /// </summary>
    public class NavigationGuide : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private SpriteRenderer arrow;
        [SerializeField] private SpriteRenderer marker;
        [Tooltip("화살표가 플레이어 머리 위에 떠 있는 높이")]
        [SerializeField] private float arrowHeight = 1.9f;
        [Tooltip("화살표가 플레이어에게서 목표 쪽으로 떨어진 거리")]
        [SerializeField] private float arrowRadius = 0.9f;
        [Tooltip("이 거리(타일)보다 가까우면 화살표를 숨기고 표식만 보여준다")]
        [SerializeField] private float hideDistance = 2.5f;

        private bool hasTarget;
        private Vector3 target;
        private float t;

        public bool HasTarget { get { return hasTarget; } }
        public Vector3 Target { get { return target; } }

        public void SetTarget(Vector3 worldPos)
        {
            hasTarget = true;
            target = worldPos;
        }

        public void Clear()
        {
            hasTarget = false;
            arrow.enabled = false;
            marker.enabled = false;
        }

        private void LateUpdate()
        {
            if (!hasTarget) { arrow.enabled = marker.enabled = false; return; }
            t += Time.deltaTime;
            Vector3 head = player.position + new Vector3(0f, arrowHeight * 0.5f, 0f);
            Vector3 d = target - head;
            d.z = 0f;
            float dist = d.magnitude;
            arrow.enabled = dist > hideDistance;
            if (arrow.enabled)
            {
                Vector3 dir = d / dist;
                arrow.transform.position = head + dir * arrowRadius + new Vector3(0f, arrowHeight * 0.5f, 0f);
                arrow.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }
            marker.enabled = true;
            marker.transform.position = target + new Vector3(0f, 1.6f + Mathf.Abs(Mathf.Sin(t * 4f)) * 0.35f, 0f);
        }

#if UNITY_EDITOR
        public void EditorSetup(Transform p, SpriteRenderer a, SpriteRenderer m) { player = p; arrow = a; marker = m; }
#endif
    }
}
