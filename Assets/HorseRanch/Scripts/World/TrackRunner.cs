using UnityEngine;

namespace HorseRanch
{
    /// <summary>훈련장 트랙을 계속 도는 장식용 말</summary>
    public class TrackRunner : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer horse;
        [SerializeField] private Sprite[] gallopFrames;
        [Tooltip("말이 도는 사각형 경로 (월드)")]
        [SerializeField] private Rect loop;
        [SerializeField] [Range(0.5f, 10f)] private float speed = 3.5f;

        [Tooltip("시작 위치 (경로 위 거리)")] [SerializeField] private float dist;

#if UNITY_EDITOR
        public void EditorSetup(SpriteRenderer h, Sprite[] frames, Rect l, float s, float startDist) { horse = h; gallopFrames = frames; loop = l; speed = s; dist = startDist; }
#endif

        private void Update()
        {
            float per = 2f * (loop.width + loop.height);
            if (per <= 0f) return;
            dist = (dist + speed * Time.deltaTime) % per;
            float d = dist;
            Vector2 p; bool left = false;
            if (d < loop.width) p = new Vector2(loop.xMin + d, loop.yMin);
            else if ((d -= loop.width) < loop.height) p = new Vector2(loop.xMax, loop.yMin + d);
            else if ((d -= loop.height) < loop.width) { p = new Vector2(loop.xMax - d, loop.yMax); left = true; }
            else { d -= loop.width; p = new Vector2(loop.xMin, loop.yMax - d); left = true; }
            horse.transform.position = p;
            horse.flipX = left;
            horse.sprite = gallopFrames[((int)(Time.time * 8f) & 1)];
            horse.sortingOrder = YSort.Order(p.y);
        }
    }
}
