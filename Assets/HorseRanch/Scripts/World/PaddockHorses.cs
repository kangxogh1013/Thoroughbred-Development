using UnityEngine;

namespace HorseRanch
{
    /// <summary>목장 울타리 안에 내 말들을 띄워 돌아다니게 한다 (장식). 보유 말 수·털색과 동기화</summary>
    public class PaddockHorses : MonoBehaviour
    {
        [SerializeField] private RanchGameRunner runner;
        [Tooltip("말이 돌아다니는 월드 영역")]
        [SerializeField] private Rect area;
        [SerializeField] private SpriteRenderer[] horses;
        [SerializeField] private Sprite[] gallopFrames;
        [SerializeField] [Range(0.3f, 4f)] private float speed = 1.3f;

        private Vector2[] targets;
        private float[] waits;

#if UNITY_EDITOR
        public void EditorSetup(RanchGameRunner r, Rect a, SpriteRenderer[] h, Sprite[] frames) { runner = r; area = a; horses = h; gallopFrames = frames; }
#endif

        private void Awake()
        {
            targets = new Vector2[horses.Length];
            waits = new float[horses.Length];
            for (int i = 0; i < horses.Length; i++)
            {
                horses[i].transform.position = RandomPoint();
                targets[i] = RandomPoint();
            }
        }

        private void OnEnable()
        {
            if (runner.Game == null) return;
            runner.Game.Changed += Sync;
            Sync();
        }

        private void OnDisable()
        {
            if (runner != null && runner.Game != null) runner.Game.Changed -= Sync;
        }

        private void Sync()
        {
            var list = runner.Game.State.horses;
            for (int i = 0; i < horses.Length; i++)
            {
                bool on = i < list.Count;
                if (horses[i].gameObject.activeSelf != on) horses[i].gameObject.SetActive(on);
                if (on) horses[i].color = runner.Database.CoatColor(list[i].coat);
            }
        }

        private void Update()
        {
            for (int i = 0; i < horses.Length; i++)
            {
                var h = horses[i];
                if (!h.gameObject.activeSelf) continue;
                if (waits[i] > 0f) { waits[i] -= Time.deltaTime; h.sprite = gallopFrames[0]; continue; }
                Vector2 p = h.transform.position;
                Vector2 d = targets[i] - p;
                if (d.sqrMagnitude < 0.01f) { targets[i] = RandomPoint(); waits[i] = Random.Range(0.5f, 3f); continue; }
                Vector2 step = d.normalized * speed * Time.deltaTime;
                if (step.sqrMagnitude > d.sqrMagnitude) step = d;
                h.transform.position = p + step;
                h.flipX = d.x < 0f;
                h.sprite = gallopFrames[((int)(Time.time * 6f + i) & 1)];
                h.sortingOrder = YSort.Order(h.transform.position.y);
            }
        }

        private Vector2 RandomPoint()
        {
            return new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(area.center, area.size);
        }
    }
}
