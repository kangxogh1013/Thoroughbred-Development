using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>게이지 바. fill의 anchorMax.x로 길이를 조절 (스프라이트 없이 동작)</summary>
    public class UIBar : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [SerializeField] private Image fillImage;

        public void Set(float value01)
        {
            if (fill == null) return;
            fill.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
        }

        public void SetColor(Color c)
        {
            if (fillImage != null) fillImage.color = c;
        }
    }
}
