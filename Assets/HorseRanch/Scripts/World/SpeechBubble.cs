using TMPro;
using UnityEngine;

namespace HorseRanch
{
    /// <summary>
    /// 월드 말풍선 하나를 돌려 쓴다 (도박장 손님들의 고함 등). Show로 위치와 글을 바꾸고 잠시 뒤 숨는다.
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer plate;
        [SerializeField] private TMP_Text text;
        [Tooltip("글자 하나당 말풍선 폭 (월드 단위)")]
        [SerializeField] private float widthPerChar = 0.42f;

        private float timer;
        private Transform follow;
        private Vector3 followOffset;

        public bool IsShowing { get { return timer > 0f; } }
        public Transform Target { get { return follow; } }

        /// <summary>움직이는 사람 머리 위에 따라 붙는 말풍선</summary>
        public void ShowOn(Transform target, Vector3 offset, string message, float seconds)
        {
            Show(target.position + offset, message, seconds);
            follow = target; followOffset = offset;
        }

        private void Awake() { SetVisible(false); }

        public void Show(Vector3 worldPos, string message, float seconds)
        {
            follow = null;
            transform.position = worldPos;
            text.text = message;
            float w = Mathf.Max(2f, message.Length * widthPerChar + 0.8f);
            plate.size = new Vector2(w, plate.size.y);
            text.rectTransform.sizeDelta = new Vector2(w, plate.size.y);
            timer = seconds;
            SetVisible(true);
        }

        public void Hide() { timer = 0f; follow = null; SetVisible(false); }

        private void LateUpdate()
        {
            if (timer > 0f && follow != null)
            {
                if (!follow.gameObject.activeInHierarchy) { Hide(); return; }
                transform.position = follow.position + followOffset;
            }
        }

        private void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.deltaTime;
            if (timer <= 0f) SetVisible(false);
        }

        private void SetVisible(bool on) { plate.enabled = on; text.enabled = on; }

#if UNITY_EDITOR
        public void EditorSetup(SpriteRenderer p, TMP_Text t) { plate = p; text = t; }
#endif
    }
}
