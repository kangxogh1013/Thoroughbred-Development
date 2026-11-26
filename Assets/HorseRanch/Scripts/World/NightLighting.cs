using UnityEngine;

namespace HorseRanch
{
    /// <summary>
    /// 밤 조명: 구역마다 '어둠 덮개' 스프라이트(가로등 자리만 구멍이 뚫린 텍스처)를 깔고,
    /// 가로등 불빛 · 창문 불빛 스프라이트를 그 위에 켠다. GameUI가 시각에 맞춰 Apply를 부른다.
    /// 어둠 텍스처는 에디터 생성 때 가로등 위치로 미리 구워 둔다 (실행 중 계산 없음).
    /// </summary>
    public class NightLighting : MonoBehaviour
    {
        [Header("구역별 어둠 덮개")]
        [SerializeField] private SpriteRenderer[] shades;
        [Tooltip("구역별 어둠 배율 (실외 1, 실내는 불이 켜져 있어 약하게, 숲은 달빛만)")]
        [SerializeField] private float[] shadeScale;

        [Header("불빛 (가로등 빛 웅덩이 · 등 머리 · 창문)")]
        [SerializeField] private SpriteRenderer[] glows;

        [Header("색")]
        [Tooltip("밤 어둠 색 (짙은 남색)")]
        [SerializeField] private Color nightColor = new Color(0.02f, 0.03f, 0.1f, 1f);

        private float lastDark = -1f;
        private bool lastLamps;
        private float[] glowAlpha;

#if UNITY_EDITOR
        public void EditorSetup(SpriteRenderer[] s, float[] scale, SpriteRenderer[] g) { shades = s; shadeScale = scale; glows = g; }
#endif

        private void Awake()
        {
            glowAlpha = new float[glows.Length];
            for (int i = 0; i < glows.Length; i++) { glowAlpha[i] = glows[i].color.a; glows[i].enabled = false; }
            for (int i = 0; i < shades.Length; i++) shades[i].enabled = false;
        }

        /// <summary>darkness 0~1 (밤 깊이), lampsOn = 가로등 · 창문 불 켜짐</summary>
        public void Apply(float darkness, bool lampsOn)
        {
            if (Mathf.Abs(darkness - lastDark) < 0.002f && lampsOn == lastLamps) return;
            lastDark = darkness; lastLamps = lampsOn;
            for (int i = 0; i < shades.Length; i++)
            {
                float a = darkness * (shadeScale != null && i < shadeScale.Length ? shadeScale[i] : 1f);
                var c = nightColor; c.a = a;
                shades[i].color = c;
                shades[i].enabled = a > 0.004f;
            }
            // 불빛은 어두울수록 또렷하게 (해 질 녘엔 은은하게)
            float glowK = Mathf.Clamp01(0.35f + darkness * 0.9f);
            for (int i = 0; i < glows.Length; i++)
            {
                glows[i].enabled = lampsOn;
                if (!lampsOn) continue;
                var c = glows[i].color; c.a = glowAlpha[i] * glowK;
                glows[i].color = c;
            }
        }
    }
}
