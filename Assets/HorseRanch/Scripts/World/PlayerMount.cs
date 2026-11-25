using UnityEngine;

namespace HorseRanch
{
    /// <summary>
    /// 말 타기 연출: 플레이어 밑에 말 스프라이트를 붙이고, 플레이어 그림은 위로 올린 사본(rider)으로 그린다.
    /// 원래 몸(body)은 숨기고, GridMover가 바꾸는 프레임을 매 프레임 rider에 복사한다. 속도는 GridMover.SpeedScale로.
    /// </summary>
    public class PlayerMount : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private GridMover mover;
        [Tooltip("플레이어 원래 몸 (타는 동안 숨김)")]
        [SerializeField] private SpriteRenderer body;
        [Tooltip("말 위에 올라탄 플레이어 (body 프레임 복사)")]
        [SerializeField] private SpriteRenderer rider;
        [SerializeField] private SpriteRenderer horse;
        [SerializeField] private Sprite[] gallopFrames;

        [Header("모양 (말은 플레이어보다 약 1.5배 크게)")]
        [Tooltip("말 크기 배율 (1.4 = 폭 약 1.9칸)")]
        [SerializeField] [Range(0.5f, 2.5f)] private float horseScale = 1.4f;
        [Tooltip("말 스프라이트 중심 높이 (발이 바닥에 닿게)")]
        [SerializeField] [Range(0f, 2f)] private float horseCenterY = 0.76f;
        [Tooltip("말 등 위로 올라가는 플레이어 높이 (타일)")]
        [SerializeField] [Range(0f, 2f)] private float riderLift = 0.68f;
        [Tooltip("안장 위치: 말 중심에서 꼬리 쪽으로 (타일)")]
        [SerializeField] [Range(0f, 0.5f)] private float saddleOffset = 0.14f;
        [Tooltip("걸을 때 말 다리 프레임이 바뀌는 간격 (초)")]
        [SerializeField] [Range(0.05f, 0.5f)] private float gallopFrameTime = 0.12f;

        private bool riding;
        private float animT;
        private int frame;
        private bool faceLeft;

        public bool IsRiding { get { return riding; } }

#if UNITY_EDITOR
        public void EditorSetup(GridMover m, SpriteRenderer b, SpriteRenderer r, SpriteRenderer h, Sprite[] frames)
        {
            mover = m; body = b; rider = r; horse = h; gallopFrames = frames;
        }
#endif

        private void Awake()
        {
            rider.enabled = false;
            horse.enabled = false;
            ApplyShape();
        }

        /// <summary>말에 오르기 (coat = 털색, speedScale = 이동 속도 배율)</summary>
        public void Mount(Color coat, float speedScale)
        {
            riding = true;
            horse.color = coat;
            horse.enabled = rider.enabled = true;
            body.enabled = false;
            mover.SpeedScale = speedScale;
            ApplyShape();
            LateUpdate();
        }

        /// <summary>인스펙터 값 → 말 크기 · 위치</summary>
        private void ApplyShape()
        {
            horse.transform.localScale = new Vector3(horseScale, horseScale, 1f);
            horse.transform.localPosition = new Vector3(0f, horseCenterY, 0f);
            rider.transform.localPosition = new Vector3(faceLeft ? saddleOffset : -saddleOffset, riderLift, 0f);
        }

        public void Dismount()
        {
            if (!riding) return;
            riding = false;
            horse.enabled = rider.enabled = false;
            body.enabled = true;
            mover.SpeedScale = 1f;
        }

        private void LateUpdate()
        {
            if (!riding) return;
            rider.sprite = body.sprite;
            // 말은 옆모습만 있으므로 좌우만 뒤집고, 위·아래를 볼 땐 마지막 좌우 방향 유지
            if (mover.Facing.x != 0) faceLeft = mover.Facing.x < 0;
            horse.flipX = faceLeft;
            // 말은 오른쪽을 보고 그려져 있음: 안장은 머리 반대쪽
            rider.transform.localPosition = new Vector3(faceLeft ? saddleOffset : -saddleOffset, riderLift, 0f);
            if (mover.IsMoving)
            {
                animT += Time.deltaTime;
                if (animT >= gallopFrameTime) { animT = 0f; frame = 1 - frame; }
            }
            else frame = 0;
            if (gallopFrames != null && gallopFrames.Length > 1) horse.sprite = gallopFrames[frame];
            int order = YSort.Order(transform.position.y);
            horse.sortingOrder = order;
            rider.sortingOrder = order + 1;
        }
    }
}
