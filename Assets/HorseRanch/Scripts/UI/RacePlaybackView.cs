using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>
    /// 8레인 횡스크롤 경주 재생. RanchGame이 미리 계산한 프레임(위치)을 시간에 맞춰 보여주기만 한다.
    /// 매 프레임 GetComponent/new 없이 미리 연결된 RectTransform만 움직인다.
    /// </summary>
    public class RacePlaybackView : MonoBehaviour
    {
        [Header("트랙")]
        [SerializeField] private RectTransform track;
        [Tooltip("위쪽 거리 표지 영역 높이 (px)")]
        [SerializeField] private float topMargin = 34f;
        [SerializeField] private float bottomMargin = 14f;
        [Tooltip("화면에 보이는 거리 (m)")]
        [SerializeField] [Range(20f, 150f)] private float viewMeters = 68f;

        [Header("주자 8마리")]
        [SerializeField] private RectTransform[] runners;
        [SerializeField] private Image[] runnerBodies;
        [SerializeField] private Image[] runnerSilks;
        [SerializeField] private TMP_Text[] runnerNumbers;
        [SerializeField] private Image[] runnerMarkers;
        [Tooltip("질주 모션 2프레임 (흰색 실루엣, 털색으로 틴트)")]
        [SerializeField] private Sprite[] gallopFrames;

        [Header("트랙 장식")]
        [SerializeField] private RectTransform[] laneLines;
        [SerializeField] private RectTransform[] stripes;
        [SerializeField] private RectTransform[] markers;
        [SerializeField] private TMP_Text[] markerTexts;
        [SerializeField] private RectTransform finishLine;

        [Header("HUD")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text standingsText;
        [SerializeField] private Button[] speedButtons;
        [SerializeField] private Image[] speedButtonImages;
        [SerializeField] private Button skipButton;

        private static readonly float[] Speeds = { 1f, 2f, 4f };

        private RaceReplay replay;
        private Action onFinished;
        private float time;
        private float speed = 2f;
        private bool playing;
        private float endDelay;
        private float standingsTimer;
        private float lastLaneHeight = -1f;
        private readonly int[] order = new int[RanchConst.RunnerCount];
        private readonly float[] pos = new float[RanchConst.RunnerCount];
        private readonly int[] markerValues = new int[16];
        private readonly StringBuilder sb = new StringBuilder(512);

        public bool IsPlaying { get { return playing; } }

        private void Awake()
        {
            for (int i = 0; i < speedButtons.Length; i++)
            {
                int captured = i;
                speedButtons[i].onClick.AddListener(() => SetSpeed(captured));
            }
            skipButton.onClick.AddListener(Skip);
        }

        public void Play(RaceReplay r, GameDatabaseSO db, Action finished)
        {
            replay = r;
            onFinished = finished;
            time = 0f;
            endDelay = 0.8f;
            playing = true;
            lastLaneHeight = -1f;
            for (int i = 0; i < markerValues.Length; i++) markerValues[i] = int.MinValue;

            for (int i = 0; i < runners.Length; i++)
            {
                runnerBodies[i].color = db.CoatColor(r.coats[i]);
                runnerSilks[i].color = i == r.mine ? UITheme.BoardFg : UITheme.Silk[i];
                runnerNumbers[i].text = (i + 1).ToString();
                runnerNumbers[i].color = i == r.mine ? UITheme.Ink : UITheme.SilkInk[i];
                runnerMarkers[i].gameObject.SetActive(i == r.mine); // 내 말 머리 위 표시
            }
            titleText.text = (r.distance >= 2000f ? "장거리 " : "단거리 ") + r.distance.ToString("N0") + "m" + (r.grandPrix ? "  G1" : "");
            SetSpeed(1);
            Canvas.ForceUpdateCanvases(); // 막 켜진 패널의 레이아웃 크기 확정
            Draw(0);
        }

        private void SetSpeed(int i)
        {
            speed = Speeds[i];
            for (int k = 0; k < speedButtonImages.Length; k++)
                speedButtonImages[k].color = k == i ? UITheme.Ink : UITheme.Panel2;
        }

        private void Skip()
        {
            if (replay == null) return;
            time = replay.frameCount * replay.dt;
        }

        private void Update()
        {
            if (!playing || replay == null) return;
            time += Time.deltaTime * speed;
            int fi = Mathf.Min(replay.frameCount - 1, (int)(time / replay.dt));
            Draw(fi);

            if (fi >= replay.frameCount - 1)
            {
                endDelay -= Time.deltaTime;
                if (endDelay <= 0f)
                {
                    playing = false;
                    var cb = onFinished;
                    onFinished = null;
                    if (cb != null) cb();
                }
            }
        }

        private void Draw(int fi)
        {
            Rect rect = track.rect;
            float w = rect.width, h = rect.height;
            if (w <= 1f || h <= 1f) return; // 레이아웃 계산 전
            float laneH = (h - topMargin - bottomMargin) / RanchConst.RunnerCount;
            if (!Mathf.Approximately(laneH, lastLaneHeight)) Layout(laneH, w);

            float view = w < 900f ? viewMeters * 0.62f : viewMeters;
            float ppm = w / view;
            float lead = 0f;
            for (int i = 0; i < RanchConst.RunnerCount; i++)
            {
                pos[i] = replay.Position(fi, i);
                if (pos[i] > lead) lead = pos[i];
            }
            float cam = Mathf.Max(-6f, Mathf.Min(lead, replay.distance + 6f) - view * 0.72f);

            // 잔디 줄무늬 (20m 간격)
            float stripeStart = Mathf.Floor(cam / 20f) * 20f;
            for (int i = 0; i < stripes.Length; i++)
            {
                float m = stripeStart + i * 20f;
                stripes[i].anchoredPosition = new Vector2((m - cam) * ppm, -topMargin);
                stripes[i].sizeDelta = new Vector2(10f * ppm, h - topMargin - bottomMargin);
            }

            // 거리 표지 (100m마다 남은 거리)
            float markStart = Mathf.Ceil(cam / 100f) * 100f;
            for (int i = 0; i < markers.Length; i++)
            {
                float m = markStart + i * 100f;
                bool show = m > 0f && m < replay.distance && m < cam + view + 100f;
                if (markers[i].gameObject.activeSelf != show) markers[i].gameObject.SetActive(show);
                if (!show) continue;
                markers[i].anchoredPosition = new Vector2((m - cam) * ppm, 0f);
                int remain = Mathf.RoundToInt(replay.distance - m);
                if (markerValues[i] != remain) { markerValues[i] = remain; markerTexts[i].text = remain.ToString(); }
            }

            finishLine.anchoredPosition = new Vector2((replay.distance - cam) * ppm, -topMargin);

            // 주자
            for (int i = 0; i < runners.Length; i++)
            {
                float x = Mathf.Max(24f, (pos[i] - cam) * ppm);
                float phase = pos[i] * 1.1f;
                float y = -(topMargin + laneH * (i + 0.5f)) + Mathf.Sin(phase) * laneH * 0.04f;
                runners[i].anchoredPosition = new Vector2(x, y);
                if (gallopFrames != null && gallopFrames.Length >= 2)
                    runnerBodies[i].sprite = gallopFrames[((int)(phase / Mathf.PI) & 1)];
            }

            // HUD (0.15초마다)
            standingsTimer -= Time.unscaledDeltaTime;
            if (standingsTimer <= 0f || fi == 0 || fi >= replay.frameCount - 1)
            {
                standingsTimer = 0.15f;
                UpdateStandings(fi, lead);
            }
        }

        private void Layout(float laneH, float w)
        {
            lastLaneHeight = laneH;
            for (int i = 0; i < runners.Length; i++) runners[i].sizeDelta = new Vector2(laneH * 1.45f, laneH * 1.0f);
            for (int i = 0; i < laneLines.Length; i++)
            {
                laneLines[i].anchoredPosition = new Vector2(0f, -(topMargin + laneH * (i + 1)));
                laneLines[i].sizeDelta = new Vector2(w, 2f);
            }
            finishLine.sizeDelta = new Vector2(10f, laneH * RanchConst.RunnerCount);
        }

        private void UpdateStandings(int fi, float lead)
        {
            for (int i = 0; i < order.Length; i++) order[i] = i;
            // 삽입 정렬 (8개, 할당 없음)
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
                sb.Append(r + 1).Append("  <color=#").Append(UITheme.SilkHex[ix]).Append(">■</color> ").Append(ix + 1).Append("번 ")
                  .Append(replay.names[ix]);
                if (r > 0) sb.Append("<pos=78%><color=#566458>-").Append((pos[order[0]] - pos[ix]).ToString("0.0")).Append("m</color>");
                if (mine) sb.Append("</b>");
                sb.Append('\n');
            }
            standingsText.SetText(sb);
            progressText.text = "선두 잔여 " + Mathf.Max(0, Mathf.RoundToInt(replay.distance - lead)) + "m · " + (fi * replay.dt).ToString("0.0") + "초 · " + speed + "배속";
        }

        private void OnDrawGizmosSelected()
        {
            // 씬 뷰에서 레인 영역 확인
            if (track == null) return;
            var corners = new Vector3[4];
            track.GetWorldCorners(corners); // 0 좌하, 1 좌상, 2 우상, 3 우하
            float worldH = corners[1].y - corners[0].y;
            float scale = track.rect.height > 0f ? worldH / track.rect.height : 1f;
            float top = corners[1].y - topMargin * scale;
            float laneH = (worldH - (topMargin + bottomMargin) * scale) / RanchConst.RunnerCount;
            for (int i = 0; i < RanchConst.RunnerCount; i++)
            {
                Gizmos.color = UITheme.Silk[i];
                float y = top - laneH * (i + 0.5f);
                Gizmos.DrawWireCube(new Vector3((corners[0].x + corners[2].x) * 0.5f, y, corners[0].z), new Vector3(corners[2].x - corners[0].x, laneH * 0.95f, 0f));
            }
        }
    }
}
