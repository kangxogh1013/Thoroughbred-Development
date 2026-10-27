using UnityEngine;

namespace HorseRanch
{
    /// <summary>화면 우하단 짧은 알림. 슬롯을 재사용해 할당 없이 동작</summary>
    public class ToastView : MonoBehaviour
    {
        [SerializeField] private UIRow[] slots;
        [Tooltip("표시 시간 (초)")]
        [SerializeField] [Range(0.5f, 8f)] private float duration = 2.6f;

        private float[] timers;

        private void Awake()
        {
            timers = new float[slots.Length];
            for (int i = 0; i < slots.Length; i++) slots[i].gameObject.SetActive(false);
        }

        public void Show(string message, bool highlight)
        {
            if (timers == null) Awake();
            // 가장 오래된 슬롯을 덮어쓴다
            int pick = 0; float min = float.MaxValue;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].gameObject.activeSelf) { pick = i; break; }
                if (timers[i] < min) { min = timers[i]; pick = i; }
            }
            var s = slots[pick];
            s.SetText(0, message);
            s.SetBackground(highlight ? UITheme.Accent : UITheme.Ink);
            s.SetTextColor(0, highlight ? UITheme.Ink : UITheme.White);
            s.transform.SetAsLastSibling();
            s.gameObject.SetActive(true);
            timers[pick] = highlight ? duration * 1.6f : duration;
        }

        private void Update()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].gameObject.activeSelf) continue;
                timers[i] -= Time.unscaledDeltaTime;
                if (timers[i] <= 0f) slots[i].gameObject.SetActive(false);
            }
        }
    }
}
