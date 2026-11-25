using UnityEngine;

namespace HorseRanch
{
    /// <summary>깊은 숲 한가운데 서 있는 천둥. 목장으로 데려오면 사라진다</summary>
    public class ThunderSpot : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer horse;
        [SerializeField] private Sprite[] idleFrames;
        [Tooltip("숨 쉬듯 고개를 까딱이는 속도")]
        [SerializeField] [Range(0.2f, 4f)] private float idleSpeed = 0.8f;

        private float t;

        public void Show(bool on) { horse.enabled = on; }

        private void Update()
        {
            if (!horse.enabled || idleFrames == null || idleFrames.Length == 0) return;
            t += Time.deltaTime * idleSpeed;
            horse.sprite = idleFrames[(int)t % idleFrames.Length];
        }

#if UNITY_EDITOR
        public void EditorSetup(SpriteRenderer h, Sprite[] frames) { horse = h; idleFrames = frames; }
#endif
    }
}
