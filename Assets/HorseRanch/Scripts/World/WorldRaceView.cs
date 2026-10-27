using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>
    /// 경기장 트랙 위에서 실제로 달리는 경주. 8마리가 출발 게이트에 일렬로 서 있다가
    /// 발주하면 결승선(오른쪽) 방향으로만 달린다. 위치는 RanchGame이 미리 계산한 프레임을 그대로 재생.
    /// </summary>
    public class WorldRaceView : MonoBehaviour
    {
        [Header("트랙 (월드 좌표)")]
        [SerializeField] private float startX = 304.6f;
        [SerializeField] private float finishX = 394.5f;
        [Tooltip("1번 레인 중심 y (아래로 1칸씩)")]
        [SerializeField] private float laneTopY = 18.5f;

        [Header("말 8마리")]
        [SerializeField] private Transform[] horses;
        [SerializeField] private SpriteRenderer[] bodies;
        [SerializeField] private SpriteRenderer[] silks;
        [SerializeField] private TMP_Text[] numbers;
        [SerializeField] private GameObject[] mineMarkers;
        [SerializeField] private Sprite[] gallopFrames;
        [Tooltip("카메라가 따라갈 지점 (선두 말)")]
        [SerializeField] private Transform focus;

        [Header("관람 UI")]
        [SerializeField] private GameObject overlay;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text standingsText;
        [SerializeField] private Button[] speedButtons;
        [SerializeField] private Image[] speedImages;
        [SerializeField] private Button skipButton;

        private static readonly float[] Speeds = { 1f, 2f, 4f };
        private RaceReplay replay;
        private Action onFinished;
        private float time, speed = 2f, endDelay, standingsTimer;
        private bool playing;
        private readonly float[] pos = new float[RanchConst.RunnerCount];
        private readonly int[] order = new int[RanchConst.RunnerCount];
        private readonly StringBuilder sb = new StringBuilder(512);

        public bool IsPlaying { get { return playing; } }
        public Transform Focus { get { return focus; } }

        private void Awake()
        {
            for (int i = 0; i < speedButtons.Length; i++)
            {
                int captured = i;
                speedButtons[i].onClick.AddListener(() => SetSpeed(captured));
            }
            skipButton.onClick.AddListener(() => { if (replay != null) time = replay.frameCount * replay.dt; });
            overlay.SetActive(false);
            ResetToGate();
        }

        private float LaneY(int i) { return laneTopY - i; }

        /// <summary>출발 게이트에 일렬로 정렬 (평소 상태)</summary>
        public void ResetToGate()
        {
            for (int i = 0; i < horses.Length; i++)
            {
                horses[i].position = new Vector3(startX, LaneY(i), 0f);
                bodies[i].sprite = gallopFrames[0];
                silks[i].color = UITheme.Silk[i];
                numbers[i].text = (i + 1).ToString();
                numbers[i].color = UITheme.SilkInk[i];
                mineMarkers[i].SetActive(false);
                SetOrder(i);
            }
            if (focus != null) focus.position = new Vector3(startX + 8f, LaneY(3) - 0.5f, 0f);
        }

        public void Play(RaceReplay r, GameDatabaseSO db, Action finished)
        {
            replay = r;
            onFinished = finished;
            time = 0f;
            endDelay = 1.2f;
            playing = true;
            for (int i = 0; i < horses.Length; i++)
            {
                bodies[i].color = db.CoatColor(r.coats[i]);
                silks[i].color = i == r.mine ? UITheme.BoardFg : UITheme.Silk[i];
                numbers[i].color = i == r.mine ? UITheme.Ink : UITheme.SilkInk[i];
                mineMarkers[i].SetActive(i == r.mine);
            }
            titleText.text = (r.distance >= 2000f ? "장거리 " : "단거리 ") + r.distance.ToString("N0") + "m" + (r.grandPrix ? "  G1 대상경주" : "");
            overlay.SetActive(true);
            SetSpeed(1);
            Draw(0);
        }

        private void SetSpeed(int i)
        {
            speed = Speeds[i];
            for (int k = 0; k < speedImages.Length; k++) speedImages[k].color = k == i ? UITheme.Ink : UITheme.Panel2;
        }

        private void Update()
        {
            if (!playing || replay == null) return;
            time += Time.deltaTime * speed;
            int fi = Mathf.Min(replay.frameCount - 1, (int)(time / replay.dt));
            Draw(fi);
            if (fi < replay.frameCount - 1) return;
            endDelay -= Time.deltaTime;
            if (endDelay > 0f) return;
            playing = false;
            overlay.SetActive(false);
            var cb = onFinished;
            onFinished = null;
            ResetToGate();
            if (cb != null) cb();
        }

        private void Draw(int fi)
        {
            float lead = 0f;
            for (int i = 0; i < horses.Length; i++)
            {
                pos[i] = replay.Position(fi, i);
                if (pos[i] > lead) lead = pos[i];
            }
            float scale = (finishX - startX) / replay.distance;
            for (int i = 0; i < horses.Length; i++)
            {
                float x = Mathf.Min(finishX + 4f, startX + pos[i] * scale);
                horses[i].position = new Vector3(x, LaneY(i), 0f);
                bodies[i].sprite = gallopFrames[pos[i] > 0.01f ? ((int)(pos[i] * 0.9f) & 1) : 0];
                SetOrder(i);
            }
            if (focus != null) focus.position = new Vector3(Mathf.Min(finishX, startX + lead * scale) + 2f, LaneY(3) - 0.5f, 0f);

            standingsTimer -= Time.unscaledDeltaTime;
            if (standingsTimer > 0f && fi > 0 && fi < replay.frameCount - 1) return;
            standingsTimer = 0.15f;
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = 1; i < order.Length; i++)
            {
                int k = order[i]; int j = i - 1;
                while (j >= 0 && pos[order[j]] < pos[k]) { order[j + 1] = order[j]; j--; }
                order[j + 1] = k;
            }
            sb.Length = 0;
            sb.Append("<b>현재 순위</b>\n");
            for (int r = 0; r < order.Length; r++)
            {
                int ix = order[r];
                bool mine = ix == replay.mine;
                if (mine) sb.Append("<b>");
                sb.Append(r + 1).Append("  <color=#").Append(UITheme.SilkHex[ix]).Append(">■</color> ").Append(ix + 1).Append("번 ").Append(replay.names[ix]);
                if (r > 0) sb.Append("<pos=78%><color=#8B927F>-").Append((pos[order[0]] - pos[ix]).ToString("0.0")).Append("m</color>");
                if (mine) sb.Append("</b>");
                sb.Append('\n');
            }
            standingsText.SetText(sb);
            progressText.text = "선두 잔여 " + Mathf.Max(0, Mathf.RoundToInt(replay.distance - lead)) + "m · " + (fi * replay.dt).ToString("0.0") + "초 · " + speed + "배속";
        }

        private void SetOrder(int i)
        {
            int o = YSort.Order(horses[i].position.y - 0.45f);
            bodies[i].sortingOrder = o;
            silks[i].sortingOrder = o + 1;
        }

#if UNITY_EDITOR
        public void EditorSetup(float start, float finish, float topLane, Transform[] h, SpriteRenderer[] b, SpriteRenderer[] s, TMP_Text[] n, GameObject[] m, Sprite[] frames, Transform f)
        {
            startX = start; finishX = finish; laneTopY = topLane; horses = h; bodies = b; silks = s; numbers = n; mineMarkers = m; gallopFrames = frames; focus = f;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            for (int i = 0; i < RanchConst.RunnerCount; i++)
            {
                Gizmos.color = UITheme.Silk[i];
                Gizmos.DrawLine(new Vector3(startX, LaneY(i), 0f), new Vector3(finishX, LaneY(i), 0f));
            }
        }
    }
}
